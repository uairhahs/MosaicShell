
using System.Buffers.Binary;
using System.Text;
using System.Text.Json;

namespace MosaicShell.Core.Capabilities.Ipc
{
    /// <summary>
    /// Shared 4-byte-length-prefixed JSON framing for MosaicShell's named-pipe IPC. Both
    /// <see cref="CapabilityIpcCodec"/> (Worker/Host flyout channel) and
    /// <see cref="CapabilityIpcControlCodec"/> (Worker/Host control channel) delegate their
    /// Serialize/Deserialize/Write/TryRead pairs here so the framing logic exists in one place.
    /// </summary>
    internal static class LengthPrefixedJsonCodec
    {
        internal static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
        };

        public static byte[] Serialize<T>(T message, int maxMessageBytes, string tooLargeMessage)
        {
            string json = JsonSerializer.Serialize(message, JsonOptions);
            byte[] payload = Encoding.UTF8.GetBytes(json);
            if (payload.Length > maxMessageBytes)
            {
                throw new InvalidOperationException(tooLargeMessage);
            }

            byte[] frame = new byte[4 + payload.Length];
            BinaryPrimitives.WriteInt32LittleEndian(frame.AsSpan(0, 4), payload.Length);
            payload.CopyTo(frame.AsSpan(4));
            return frame;
        }

        public static T Deserialize<T>(ReadOnlySpan<byte> payload, string invalidPayloadMessage)
        {
            return JsonSerializer.Deserialize<T>(payload, JsonOptions)
            ?? throw new InvalidOperationException(invalidPayloadMessage);
        }

        public static async Task WriteAsync<T>(
            Stream stream,
            T message,
            int maxMessageBytes,
            string tooLargeMessage,
            CancellationToken ct)
        {
            byte[] frame = Serialize(message, maxMessageBytes, tooLargeMessage);
            await stream.WriteAsync(frame, ct).ConfigureAwait(false);
            await stream.FlushAsync(ct).ConfigureAwait(false);
        }

        public static async Task<T?> TryReadAsync<T>(
            Stream stream,
            int maxMessageBytes,
            string invalidFrameLengthMessage,
            string invalidPayloadMessage,
            CancellationToken ct)
            where T : class
        {
            byte[] lenBuf = new byte[4];
            if (!await ReadExactAsync(stream, lenBuf, ct).ConfigureAwait(false))
            {
                return null;
            }

            int len = BinaryPrimitives.ReadInt32LittleEndian(lenBuf);
            if (len <= 0 || len > maxMessageBytes)
            {
                throw new InvalidOperationException($"{invalidFrameLengthMessage} {len}.");
            }

            byte[] payload = new byte[len];
            return !await ReadExactAsync(stream, payload, ct).ConfigureAwait(false)
                ? null
                : Deserialize<T>(payload, invalidPayloadMessage);
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
