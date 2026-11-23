using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace MosaicShell.Core.Services.Pulse
{
    /// <summary>One channel of a PulseAudio volume, in pactl's raw units (65536 is 100%).</summary>
    public readonly record struct PulseChannel(string Name, uint Value);

    /// <summary>
    /// The default output's volume and mute as <c>pactl -f json</c> reports them (PulseAudio, or PipeWire's PulseAudio
    /// layer), mapped to the 0..1 master volume the flyouts use, as Windows' master volume scalar is. Kept pure so it is
    /// tested on every OS.
    /// </summary>
    public static class PulseVolumePolicy
    {
        /// <summary>PA_VOLUME_NORM: 100%.</summary>
        public const uint Norm = 65536;

        /// <summary>Reads <c>get-sink-volume</c>: {"volume":{"front-left":{"value":36700,...},...},"balance":0}.</summary>
        public static bool TryParseVolume(string? json, out IReadOnlyList<PulseChannel> channels)
        {
            channels = [];
            if (!TryParse(json, out JsonDocument? doc))
            {
                return false;
            }

            using (doc)
            {
                return TryReadChannels(doc.RootElement, out channels);
            }
        }

        /// <summary>
        /// Reads one sink's volume and mute from <c>pactl -f json list sinks</c>: [{"name":"...","mute":false,
        /// "volume":{"front-left":{"value":20972,...},...},...},...]. Used for the default output because pactl 17.0 as
        /// some distributions ship it (NixOS, 2026-11-23) prints text for <c>get-sink-volume</c> and
        /// <c>get-sink-mute</c> even with <c>-f json</c>, while <c>list sinks</c> is JSON everywhere.
        /// </summary>
        public static bool TryParseSink(string? listJson, string sinkName, out IReadOnlyList<PulseChannel> channels, out bool muted)
        {
            channels = [];
            muted = false;
            if (string.IsNullOrEmpty(sinkName) || string.IsNullOrWhiteSpace(listJson) || listJson.TrimStart()[0] != '[')
            {
                return false;
            }

            try
            {
                using JsonDocument doc = JsonDocument.Parse(listJson);
                foreach (JsonElement sink in doc.RootElement.EnumerateArray())
                {
                    if (sink.ValueKind != JsonValueKind.Object
                        || !sink.TryGetProperty("name", out JsonElement name)
                        || name.ValueKind != JsonValueKind.String
                        || !string.Equals(name.GetString(), sinkName, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (!sink.TryGetProperty("mute", out JsonElement mute)
                        || mute.ValueKind is not (JsonValueKind.True or JsonValueKind.False)
                        || !TryReadChannels(sink, out channels))
                    {
                        channels = [];
                        return false;
                    }

                    muted = mute.GetBoolean();
                    return true;
                }

                return false;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        private static bool TryReadChannels(JsonElement owner, out IReadOnlyList<PulseChannel> channels)
        {
            channels = [];
            if (!owner.TryGetProperty("volume", out JsonElement volume) || volume.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            List<PulseChannel> list = [];
            foreach (JsonProperty channel in volume.EnumerateObject())
            {
                if (channel.Value.ValueKind == JsonValueKind.Object
                    && channel.Value.TryGetProperty("value", out JsonElement value)
                    && value.TryGetUInt32(out uint raw))
                {
                    list.Add(new PulseChannel(channel.Name, raw));
                }
            }

            channels = list;
            return list.Count > 0;
        }

        /// <summary>Reads <c>get-sink-mute</c>: {"mute":false}.</summary>
        public static bool TryParseMute(string? json, out bool muted)
        {
            muted = false;
            if (!TryParse(json, out JsonDocument? doc))
            {
                return false;
            }

            using (doc)
            {
                if (!doc.RootElement.TryGetProperty("mute", out JsonElement mute)
                    || mute.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                {
                    return false;
                }

                muted = mute.GetBoolean();
                return true;
            }
        }

        /// <summary>The loudest channel, as plasma-pa shows it; above 100% reads as full.</summary>
        public static double Master(IReadOnlyList<PulseChannel> channels)
        {
            uint loudest = channels.Count == 0 ? 0 : channels.Max(static c => c.Value);
            return Math.Clamp(loudest / (double)Norm, 0, 1);
        }

        /// <summary>
        /// New channel values for a master volume, keeping the balance: every channel scales by target / loudest, as
        /// Windows' master scalar keeps channel ratios. From silence every channel gets the target.
        /// </summary>
        public static IReadOnlyList<uint> ScaleTo(IReadOnlyList<PulseChannel> channels, double target)
        {
            double t = Math.Clamp(target, 0, 1);
            uint loudest = channels.Count == 0 ? 0 : channels.Max(static c => c.Value);
            return loudest == 0
                ? [.. channels.Select(_ => (uint)Math.Round(t * Norm))]
                : [.. channels.Select(c => (uint)Math.Round(c.Value * t * Norm / loudest))];
        }

        /// <summary>
        /// A <c>pactl subscribe</c> line that can change the default output's volume or mute: any event on a sink, or
        /// on the server (the default output changed). Clients, cards, streams and inputs cannot.
        /// </summary>
        public static bool NeedsRefresh(string? line)
        {
            return line is not null
                && line.StartsWith("Event '", StringComparison.Ordinal)
                && (line.Contains("' on sink #", StringComparison.Ordinal)
                    || line.Contains("' on server", StringComparison.Ordinal));
        }

        private static bool TryParse(string? json, [NotNullWhen(true)] out JsonDocument? doc)
        {
            doc = null;
            if (string.IsNullOrWhiteSpace(json) || json.TrimStart()[0] != '{')
            {
                return false;
            }

            try
            {
                doc = JsonDocument.Parse(json);
                return doc.RootElement.ValueKind == JsonValueKind.Object || Dispose(doc);
            }
            catch (JsonException)
            {
                return false;
            }
        }

        private static bool Dispose(JsonDocument doc)
        {
            doc.Dispose();
            return false;
        }
    }
}
