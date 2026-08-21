namespace MosaicShell.Core.Services;

/// <summary>Stable percent-space volume math — avoids float jitter (49.6% → 49/50 flip).</summary>
public static class VolumePercent
{
    public static int ToPercent(double v) =>
        (int)Math.Round(Math.Clamp(v, 0, 1) * 100, MidpointRounding.AwayFromZero);

    public static double FromPercent(int percent) =>
        Math.Clamp(percent, 0, 100) / 100.0;

    public static double Quantize(double v) => FromPercent(ToPercent(v));

    /// <summary>Step by whole percent points (e.g. +2 → Windows-like nudge).</summary>
    public static double Step(double v, int deltaPercent) =>
        FromPercent(ToPercent(v) + deltaPercent);
}
