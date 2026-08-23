namespace MosaicShell.Core.Capabilities.Ipc;

using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;

public static class CapabilityIpcControlCodec
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    public static byte[] Serialize(CapabilityControlMessage message)
    {
        var json = JsonSerializer.Serialize(message, JsonOptions);
        var payload = Encoding.UTF8.GetBytes(json);
        if (payload.Length > CapabilityIpcControlPolicy.MaxMessageBytes)
            throw new InvalidOperationException("Control IPC message too large.");

        var frame = new byte[4 + payload.Length];
        BinaryPrimitives.WriteInt32LittleEndian(frame.AsSpan(0, 4), payload.Length);
        payload.CopyTo(frame.AsSpan(4));
        return frame;
    }

    public static CapabilityControlMessage Deserialize(ReadOnlySpan<byte> payload) =>
        JsonSerializer.Deserialize<CapabilityControlMessage>(payload, JsonOptions)
        ?? throw new InvalidOperationException("Invalid control IPC payload.");

    public static async Task WriteAsync(Stream stream, CapabilityControlMessage message, CancellationToken ct)
    {
        var frame = Serialize(message);
        await stream.WriteAsync(frame, ct).ConfigureAwait(false);
        await stream.FlushAsync(ct).ConfigureAwait(false);
    }

    public static async Task<CapabilityControlMessage?> TryReadAsync(Stream stream, CancellationToken ct)
    {
        var lenBuf = new byte[4];
        if (!await ReadExactAsync(stream, lenBuf, ct).ConfigureAwait(false))
            return null;

        var len = BinaryPrimitives.ReadInt32LittleEndian(lenBuf);
        if (len <= 0 || len > CapabilityIpcControlPolicy.MaxMessageBytes)
            throw new InvalidOperationException($"Invalid control frame length {len}.");

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
