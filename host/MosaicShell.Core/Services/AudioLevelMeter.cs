namespace MosaicShell.Core.Services;

/// <summary>
/// Pure IEEE-float loopback sample → peak/band metering used by <see cref="WindowsAudioLevelService"/>.
/// </summary>
public static class AudioLevelMeter
{
    public const int BandCount = 16;

    /// <summary>Multiplies mean absolute amplitude into the display peak (clamped 0..1).</summary>
    public const double PeakGain = 4;

    /// <summary>Multiplies per-band mean absolute amplitude into display bands (clamped 0..1).</summary>
    public const double BandGain = 6;

    /// <summary>
    /// Interprets <paramref name="buffer"/> as little-endian IEEE float samples and writes
    /// <see cref="BandCount"/> band levels into <paramref name="bands"/>.
    /// No-ops (peak 0, bands unchanged) when fewer than 4 bytes are present.
    /// </summary>
    public static void ProcessIeeeFloat(ReadOnlySpan<byte> buffer, Span<float> bands, out double peak)
    {
        if (bands.Length < BandCount)
            throw new ArgumentException($"bands must have length >= {BandCount}.", nameof(bands));

        if (buffer.Length < 4)
        {
            peak = 0;
            return;
        }

        var samples = buffer.Length / 4;
        double sum = 0;
        Span<double> bandAcc = stackalloc double[BandCount];
        Span<int> bandHits = stackalloc int[BandCount];
        bandAcc.Clear();
        bandHits.Clear();

        for (var i = 0; i < samples; i++)
        {
            var sample = BitConverter.ToSingle(buffer.Slice(i * 4, 4));
            var a = Math.Abs(sample);
            sum += a;
            var band = Math.Clamp(i * BandCount / Math.Max(1, samples), 0, BandCount - 1);
            bandAcc[band] += a;
            bandHits[band]++;
        }

        peak = Math.Clamp(sum / samples * PeakGain, 0, 1);
        for (var b = 0; b < BandCount; b++)
            bands[b] = (float)Math.Clamp(bandHits[b] == 0 ? 0 : bandAcc[b] / bandHits[b] * BandGain, 0, 1);
    }
}
