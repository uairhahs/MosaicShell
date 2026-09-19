
namespace MosaicShell.Core.Capabilities.Ipc
{
    public static class CapabilityIpcControlCodec
    {
        public static byte[] Serialize(CapabilityControlMessage message)
        {
            return LengthPrefixedJsonCodec.Serialize(
                message, CapabilityIpcControlPolicy.MaxMessageBytes, "Control IPC message too large.");
        }

        public static CapabilityControlMessage Deserialize(ReadOnlySpan<byte> payload)
        {
            return LengthPrefixedJsonCodec.Deserialize<CapabilityControlMessage>(payload, "Invalid control IPC payload.");
        }

        public static Task WriteAsync(Stream stream, CapabilityControlMessage message, CancellationToken ct)
        {
            return LengthPrefixedJsonCodec.WriteAsync(
                stream, message, CapabilityIpcControlPolicy.MaxMessageBytes, "Control IPC message too large.", ct);
        }

        public static Task<CapabilityControlMessage?> TryReadAsync(Stream stream, CancellationToken ct)
        {
            return LengthPrefixedJsonCodec.TryReadAsync<CapabilityControlMessage>(
                stream, CapabilityIpcControlPolicy.MaxMessageBytes, "Invalid control frame length", "Invalid control IPC payload.", ct);
        }
    }
}
