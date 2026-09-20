using System.Buffers.Binary;

namespace MosaicShell.Core.Services.BrowserBridge
{
    /// <summary>
    /// The framing browsers use for native messaging, and the one the relay and the Host use between themselves:
    /// a 4 byte little-endian length, then that many bytes of UTF-8 JSON.
    /// </summary>
    public static class NativeMessagingFraming
    {
        /// <summary>Browsers refuse a native host message larger than this.</summary>
        public const int MaxToBrowserBytes = 1024 * 1024;

        private const int LengthBytes = 4;

        public static byte[] Encode(ReadOnlySpan<byte> payload, int maxPayload = MaxToBrowserBytes)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(payload.Length, maxPayload, nameof(payload));

            byte[] frame = new byte[LengthBytes + payload.Length];
            BinaryPrimitives.WriteUInt32LittleEndian(frame, (uint)payload.Length);
            payload.CopyTo(frame.AsSpan(LengthBytes));
            return frame;
        }

        /// <summary>
        /// Reads one frame. Returns null when the stream ends cleanly between frames. A length above
        /// <paramref name="maxPayload"/> (or too large for an int) and a stream that ends inside a frame throw
        /// <see cref="InvalidDataException"/>; the payload of an oversized frame is never read or allocated.
        /// </summary>
        public static async ValueTask<byte[]?> ReadFrameAsync(Stream stream, int maxPayload, CancellationToken cancellationToken = default)
        {
            byte[] prefix = new byte[LengthBytes];
            int got = await ReadUpToAsync(stream, prefix, cancellationToken);
            if (got == 0)
            {
                return null;
            }

            if (got < LengthBytes)
            {
                throw new InvalidDataException("The stream ended inside a frame length.");
            }

            uint length = BinaryPrimitives.ReadUInt32LittleEndian(prefix);
            if (length > int.MaxValue || length > (uint)maxPayload)
            {
                throw new InvalidDataException($"Frame length {length} exceeds the limit of {maxPayload}.");
            }

            byte[] payload = new byte[length];
            int read = await ReadUpToAsync(stream, payload, cancellationToken);
            return read < payload.Length ? throw new InvalidDataException("The stream ended inside a frame payload.") : payload;
        }

        /// <summary>Reads until the buffer is full or the stream ends, and returns how many bytes it got.</summary>
        private static async ValueTask<int> ReadUpToAsync(Stream stream, byte[] buffer, CancellationToken cancellationToken)
        {
            int total = 0;
            while (total < buffer.Length)
            {
                int read = await stream.ReadAsync(buffer.AsMemory(total), cancellationToken);
                if (read == 0)
                {
                    break;
                }

                total += read;
            }

            return total;
        }
    }
}
