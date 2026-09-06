
using System.Buffers.Binary;
using System.Text;
using System.Text.Json;

namespace MosaicShell.Core.Capabilities.Ipc
{
    public static class CapabilityIpcControlCodec
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
        };

        public static byte[] Serialize(CapabilityControlMessage message)
        {
            string json = JsonSerializer.Serialize(message, JsonOptions);
            byte[] payload = Encoding.UTF8.GetBytes(json);
            if (payload.Length > CapabilityIpcControlPolicy.MaxMessageBytes)
            {
                throw new InvalidOperationException("Control IPC message too large.");
            }

            byte[] frame = new byte[4 + payload.Length];
            BinaryPrimitives.WriteInt32LittleEndian(frame.AsSpan(0, 4), payload.Length);
            payload.CopyTo(frame.AsSpan(4));
            return frame;
        }

        public static CapabilityControlMessage Deserialize(ReadOnlySpan<byte> payload)
        {
            return JsonSerializer.Deserialize<CapabilityControlMessage>(payload, JsonOptions)
            ?? throw new InvalidOperationException("Invalid control IPC payload.");
        }

        public static async Task WriteAsync(Stream stream, CapabilityControlMessage message, CancellationToken ct)
        {
            byte[] frame = Serialize(message);
            await stream.WriteAsync(frame, ct).ConfigureAwait(false);
            await stream.FlushAsync(ct).ConfigureAwait(false);
        }

        public static async Task<CapabilityControlMessage?> TryReadAsync(Stream stream, CancellationToken ct)
        {
            byte[] lenBuf = new byte[4];
            if (!await ReadExactAsync(stream, lenBuf, ct).ConfigureAwait(false))
            {
                return null;
            }

            int len = BinaryPrimitives.ReadInt32LittleEndian(lenBuf);
            if (len is <= 0 or > CapabilityIpcControlPolicy.MaxMessageBytes)
            {
                throw new InvalidOperationException($"Invalid control frame length {len}.");
            }

            byte[] payload = new byte[len];
            return !await ReadExactAsync(stream, payload, ct).ConfigureAwait(false) ? null : Deserialize(payload);
        }

        private static async Task<bool> ReadExactAsync(Stream stream, byte[] buffer, CancellationToken ct)
        {
            int offset = 0;
            while (offset < buffer.Length)
            {
                int read = await stream.ReadAsync(buffer.AsMemory(offset, buffer.Length - offset), ct)
                    .ConfigureAwait(false);
                if (read == 0)
                {
                    return offset > 0 ? throw new EndOfStreamException() : false;
                }

                offset += read;
            }

            return true;
        }
    }
}
