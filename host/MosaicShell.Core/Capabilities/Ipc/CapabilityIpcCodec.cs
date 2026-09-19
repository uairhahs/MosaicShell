
namespace MosaicShell.Core.Capabilities.Ipc
{
    public static class CapabilityIpcCodec
    {
        public static FlyoutRequestDto ToDto(FlyoutRequest request)
        {
            return new(
                request.ModuleId,
                request.Kind,
                request.StyleId,
                request.Anchor,
                request.AutoDismissMs,
                request.Payload?.ToDictionary(
                    static kv => kv.Key,
                    static kv => kv.Value,
                    StringComparer.OrdinalIgnoreCase),
                request.MonitorIndex,
                request.XPad,
                request.YPad,
                request.Ani,
                request.AniDir,
                request.AniEase,
                request.AniSteps,
                request.AnimationDisplacement);
        }

        public static FlyoutRequest FromDto(FlyoutRequestDto dto)
        {
            return new(
                dto.ModuleId,
                dto.Kind,
                dto.StyleId,
                dto.Anchor,
                dto.AutoDismissMs,
                dto.Payload,
                dto.MonitorIndex,
                dto.XPad,
                dto.YPad,
                dto.Ani,
                dto.AniDir,
                dto.AniEase,
                dto.AniSteps,
                dto.AnimationDisplacement);
        }

        public static byte[] Serialize(CapabilityIpcMessage message)
        {
            return LengthPrefixedJsonCodec.Serialize(message, CapabilityIpcPolicy.MaxMessageBytes, "IPC message too large.");
        }

        public static CapabilityIpcMessage Deserialize(ReadOnlySpan<byte> payload)
        {
            return LengthPrefixedJsonCodec.Deserialize<CapabilityIpcMessage>(payload, "Invalid IPC payload.");
        }

        public static Task WriteMessageAsync(Stream stream, CapabilityIpcMessage message, CancellationToken ct)
        {
            return LengthPrefixedJsonCodec.WriteAsync(
                stream, message, CapabilityIpcPolicy.MaxMessageBytes, "IPC message too large.", ct);
        }

        public static Task<CapabilityIpcMessage?> TryReadMessageAsync(Stream stream, CancellationToken ct)
        {
            return LengthPrefixedJsonCodec.TryReadAsync<CapabilityIpcMessage>(
                stream, CapabilityIpcPolicy.MaxMessageBytes, "Invalid IPC frame length", "Invalid IPC payload.", ct);
        }
    }
}
