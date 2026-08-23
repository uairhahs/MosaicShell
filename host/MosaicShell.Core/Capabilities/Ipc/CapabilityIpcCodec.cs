namespace MosaicShell.Core.Capabilities.Ipc;

using System.Buffers.Binary;
using System.Text;
using System.Text.Json;
using MosaicShell.Core.Capabilities;

public static class CapabilityIpcCodec
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    public static FlyoutRequestDto ToDto(FlyoutRequest request) =>
        new(
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
            request.AniDir);

    public static FlyoutRequest FromDto(FlyoutRequestDto dto) =>
        new(
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
            dto.AniDir);

    public static byte[] Serialize(CapabilityIpcMessage message)
    {
        var json = JsonSerializer.Serialize(message, JsonOptions);
        var payload = Encoding.UTF8.GetBytes(json);
        if (payload.Length > CapabilityIpcPolicy.MaxMessageBytes)
            throw new InvalidOperationException("IPC message too large.");

        var frame = new byte[4 + payload.Length];
        BinaryPrimitives.WriteInt32LittleEndian(frame.AsSpan(0, 4), payload.Length);
        payload.CopyTo(frame.AsSpan(4));
        return frame;
    }

    public static CapabilityIpcMessage Deserialize(ReadOnlySpan<byte> payload) =>
        JsonSerializer.Deserialize<CapabilityIpcMessage>(payload, JsonOptions)
        ?? throw new InvalidOperationException("Invalid IPC payload.");

    public static async Task WriteMessageAsync(Stream stream, CapabilityIpcMessage message, CancellationToken ct)
    {
        var frame = Serialize(message);
        await stream.WriteAsync(frame, ct).ConfigureAwait(false);
        await stream.FlushAsync(ct).ConfigureAwait(false);
    }

    public static async Task<CapabilityIpcMessage?> TryReadMessageAsync(Stream stream, CancellationToken ct)
    {
        var lenBuf = new byte[4];
        if (!await ReadExactAsync(stream, lenBuf, ct).ConfigureAwait(false))
            return null;

        var len = BinaryPrimitives.ReadInt32LittleEndian(lenBuf);
        if (len <= 0 || len > CapabilityIpcPolicy.MaxMessageBytes)
            throw new InvalidOperationException($"Invalid IPC frame length {len}.");

        var payload = new byte[len];
        if (!await ReadExactAsync(stream, payload, ct).ConfigureAwait(false))
            return null;

        return Deserialize(payload);
    }

    private static async Task<bool> ReadExactAsync(Stream stream, byte[] buffer, CancellationToken ct)
    {
        var offset = 0;
        while (offset < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(offset, buffer.Length - offset), ct)
                .ConfigureAwait(false);
            if (read == 0) return offset > 0 ? throw new EndOfStreamException() : false;
            offset += read;
        }

        return true;
    }
}
