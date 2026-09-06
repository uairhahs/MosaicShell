
using System.Buffers.Binary;
using System.Text;
using System.Text.Json;

namespace MosaicShell.Core.Capabilities.Ipc
{
    public static class CapabilityIpcCodec
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
        };

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
            string json = JsonSerializer.Serialize(message, JsonOptions);
            byte[] payload = Encoding.UTF8.GetBytes(json);
            if (payload.Length > CapabilityIpcPolicy.MaxMessageBytes)
            {
                throw new InvalidOperationException("IPC message too large.");
            }

            byte[] frame = new byte[4 + payload.Length];
            BinaryPrimitives.WriteInt32LittleEndian(frame.AsSpan(0, 4), payload.Length);
            payload.CopyTo(frame.AsSpan(4));
            return frame;
        }

        public static CapabilityIpcMessage Deserialize(ReadOnlySpan<byte> payload)
        {
            return JsonSerializer.Deserialize<CapabilityIpcMessage>(payload, JsonOptions)
            ?? throw new InvalidOperationException("Invalid IPC payload.");
        }

        public static async Task WriteMessageAsync(Stream stream, CapabilityIpcMessage message, CancellationToken ct)
        {
            byte[] frame = Serialize(message);
            await stream.WriteAsync(frame, ct).ConfigureAwait(false);
            await stream.FlushAsync(ct).ConfigureAwait(false);
        }

        public static async Task<CapabilityIpcMessage?> TryReadMessageAsync(Stream stream, CancellationToken ct)
        {
            byte[] lenBuf = new byte[4];
            if (!await ReadExactAsync(stream, lenBuf, ct).ConfigureAwait(false))
            {
                return null;
            }

            int len = BinaryPrimitives.ReadInt32LittleEndian(lenBuf);
            if (len is <= 0 or > CapabilityIpcPolicy.MaxMessageBytes)
            {
                throw new InvalidOperationException($"Invalid IPC frame length {len}.");
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
