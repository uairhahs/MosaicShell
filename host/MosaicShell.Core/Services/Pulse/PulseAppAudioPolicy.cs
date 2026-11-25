using System.Text.Json;

namespace MosaicShell.Core.Services.Pulse
{
    /// <summary>One playback stream (a PulseAudio sink input): its index, channel volumes and mute.</summary>
    public sealed record PulseStream(uint Index, IReadOnlyList<PulseChannel> Channels, bool Muted);

    /// <summary>One app's playback: every stream it has open, under one id and name.</summary>
    public sealed record PulseApp(string Id, string DisplayName, IReadOnlyList<PulseStream> Streams)
    {
        /// <summary>As Windows shows an app session: its loudest stream's volume, muted only when every stream is.</summary>
        public AppAudioSession ToSession()
        {
            double volume = Streams.Count == 0 ? 0 : Streams.Max(s => PulseVolumePolicy.Master(s.Channels));
            return new AppAudioSession(Id, DisplayName, volume, Streams.Count > 0 && Streams.All(s => s.Muted));
        }
    }

    /// <summary>
    /// Per-app audio from <c>pactl -f json list sink-inputs</c> (PulseAudio, or PipeWire's PulseAudio layer). Streams are
    /// grouped by the process that plays them, as Windows groups an app's audio into one session, so a browser with two
    /// playing tabs is one entry and a change applies to all of its streams. Kept pure so it is tested on every OS.
    /// </summary>
    public static class PulseAppAudioPolicy
    {
        /// <summary>The apps playing now, in the order their first stream appears; empty when the JSON is unusable.</summary>
        public static IReadOnlyList<PulseApp> ParseApps(string? listJson)
        {
            if (string.IsNullOrWhiteSpace(listJson) || listJson.TrimStart()[0] != '[')
            {
                return [];
            }

            try
            {
                using JsonDocument doc = JsonDocument.Parse(listJson);
                List<(string Key, string Name, List<PulseStream> Streams)> apps = [];
                foreach (JsonElement input in doc.RootElement.EnumerateArray())
                {
                    if (input.ValueKind != JsonValueKind.Object
                        || !input.TryGetProperty("index", out JsonElement index)
                        || !index.TryGetUInt32(out uint id)
                        || !input.TryGetProperty("mute", out JsonElement mute)
                        || mute.ValueKind is not (JsonValueKind.True or JsonValueKind.False)
                        || !PulseVolumePolicy.TryReadChannels(input, out IReadOnlyList<PulseChannel> channels))
                    {
                        continue;
                    }

                    JsonElement props = input.TryGetProperty("properties", out JsonElement p) && p.ValueKind == JsonValueKind.Object ? p : default;
                    string? name = Prop(props, "application.name");
                    string? binary = Prop(props, "application.process.binary");
                    string key = binary is not null ? "app:" + binary : name is not null ? "name:" + name : "stream:" + id;
                    PulseStream stream = new(id, channels, mute.GetBoolean());
                    int at = apps.FindIndex(a => a.Key == key);
                    if (at < 0)
                    {
                        apps.Add((key, name ?? binary ?? Prop(props, "media.name") ?? ("Stream " + id), [stream]));
                    }
                    else
                    {
                        apps[at].Streams.Add(stream);
                    }
                }

                return [.. apps.Select(a => new PulseApp(a.Key, a.Name, a.Streams))];
            }
            catch (JsonException)
            {
                return [];
            }
        }

        /// <summary>The pactl calls that set every stream of an app to <paramref name="volume"/>, each keeping its balance.</summary>
        public static IReadOnlyList<string[]> VolumeCommands(PulseApp app, double volume)
        {
            return [.. app.Streams.Select(s => (string[])["set-sink-input-volume", s.Index.ToString(System.Globalization.CultureInfo.InvariantCulture),
                .. PulseVolumePolicy.ScaleTo(s.Channels, volume).Select(v => v.ToString(System.Globalization.CultureInfo.InvariantCulture))])];
        }

        /// <summary>The pactl calls that mute or unmute every stream of an app.</summary>
        public static IReadOnlyList<string[]> MuteCommands(PulseApp app, bool muted)
        {
            return [.. app.Streams.Select(s => (string[])["set-sink-input-mute", s.Index.ToString(System.Globalization.CultureInfo.InvariantCulture), muted ? "1" : "0"])];
        }

        /// <summary>A <c>pactl subscribe</c> line about a playback stream: one opened, closed or changed.</summary>
        public static bool NeedsRefresh(string? line)
        {
            return line is not null
                && line.StartsWith("Event '", StringComparison.Ordinal)
                && line.Contains("' on sink-input #", StringComparison.Ordinal);
        }

        private static string? Prop(JsonElement props, string name)
        {
            return props.ValueKind == JsonValueKind.Object
                && props.TryGetProperty(name, out JsonElement value)
                && value.ValueKind == JsonValueKind.String
                && value.GetString() is { Length: > 0 } text
                ? text
                : null;
        }
    }
}
