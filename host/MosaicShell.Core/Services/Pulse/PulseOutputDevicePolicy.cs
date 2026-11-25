using System.Text.Json;

namespace MosaicShell.Core.Services.Pulse
{
    /// <summary>
    /// Output devices from <c>pactl -f json list sinks</c> and <c>get-default-sink</c>: each sink by its name (what
    /// <c>set-default-sink</c> takes) and description (what Plasma shows). Kept pure so it is tested on every OS.
    /// </summary>
    public static class PulseOutputDevicePolicy
    {
        /// <summary>The outputs in pactl's order; empty when the JSON is unusable.</summary>
        public static IReadOnlyList<AudioOutputDevice> ParseOutputs(string? listJson, string? defaultSink)
        {
            if (string.IsNullOrWhiteSpace(listJson) || listJson.TrimStart()[0] != '[')
            {
                return [];
            }

            try
            {
                using JsonDocument doc = JsonDocument.Parse(listJson);
                List<AudioOutputDevice> outputs = [];
                foreach (JsonElement sink in doc.RootElement.EnumerateArray())
                {
                    if (sink.ValueKind != JsonValueKind.Object
                        || !sink.TryGetProperty("name", out JsonElement name)
                        || name.GetString() is not { Length: > 0 } id)
                    {
                        continue;
                    }

                    string label = sink.TryGetProperty("description", out JsonElement description)
                        && description.ValueKind == JsonValueKind.String
                        && description.GetString() is { Length: > 0 } text
                        ? text
                        : id;
                    outputs.Add(new AudioOutputDevice(id, label, id == defaultSink?.Trim()));
                }

                return outputs;
            }
            catch (JsonException)
            {
                return [];
            }
        }
    }
}
