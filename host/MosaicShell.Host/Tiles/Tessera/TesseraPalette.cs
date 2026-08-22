using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media;
using MosaicShell.Core.Capabilities;

namespace MosaicShell.Host.Tiles.Tessera;

/// <summary>YourFlyouts Fluent palette - SysAccent when available; shell alpha from soft-frost material.</summary>
public static class TesseraPalette
{
    public static Color Crust { get; } = Color.FromRgb(0x11, 0x11, 0x1b);

    private static byte _shellAlpha = TesseraFlyoutMaterialFactory.SoftFrostShellAlpha;
    private static bool _edgeBlend = true;

    public static void ApplyMaterial(TesseraFlyoutMaterial material)
    {
        _shellAlpha = material.ShellAlpha;
        _edgeBlend = material.UseEdgeBlend;
    }

    public static byte ShellAlpha => _shellAlpha;
    public static bool UseEdgeBlend => _edgeBlend;

    public static Color Primary => Color.FromArgb(_shellAlpha, 0x11, 0x11, 0x1b);
    public static Color PrimarySolid => Color.FromArgb(
        (byte)Math.Min(255, _shellAlpha + 20), 0x11, 0x11, 0x1b);
    public static Color Font { get; } = Color.FromRgb(255, 255, 255);
    public static Color FontMuted { get; } = Color.FromRgb(150, 150, 150);
    public static Color TrackBack { get; } = Color.FromArgb(50, 255, 255, 255);
    public static Color Accent { get; private set; } = Color.FromRgb(2, 115, 205);

    static TesseraPalette() => RefreshAccent();

    public static void RefreshAccent()
    {
        try
        {
            if (DwmGetColorizationColor(out var color, out _) == 0)
            {
                var a = (byte)((color >> 24) & 0xFF);
                var r = (byte)((color >> 16) & 0xFF);
                var g = (byte)((color >> 8) & 0xFF);
                var b = (byte)(color & 0xFF);
                if (a == 0) a = 255;
                if (r + g + b > 30 && r + g + b < 750)
                    Accent = Color.FromArgb(a, r, g, b);
            }
        }
        catch { /* keep fallback */ }
    }

    /// <summary>Vertical soft frost fill - gradual alpha, no hard slab.</summary>
    public static IBrush SoftFrostFill()
    {
        var a0 = (byte)Math.Clamp(_shellAlpha + 18, 0, 255);
        var a1 = _shellAlpha;
        var a2 = (byte)Math.Clamp(_shellAlpha - 14, 40, 255);
        return new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
            GradientStops =
            {
                new GradientStop(Color.FromArgb(a0, 0x14, 0x14, 0x20), 0),
                new GradientStop(Color.FromArgb(a1, 0x11, 0x11, 0x1b), 0.4),
                new GradientStop(Color.FromArgb(a2, 0x0e, 0x0e, 0x16), 1)
            }
        };
    }

    public static IBrush PrimaryBrush => new SolidColorBrush(Primary);
    public static IBrush PrimarySolidBrush => new SolidColorBrush(PrimarySolid);
    public static IBrush FontBrush => new SolidColorBrush(Font);
    public static IBrush FontMutedBrush => new SolidColorBrush(FontMuted);
    public static IBrush AccentBrush => new SolidColorBrush(Accent);
    public static IBrush TrackBackBrush => new SolidColorBrush(TrackBack);
    public static IBrush StrokeBrush => new SolidColorBrush(Color.FromArgb(90, 255, 255, 255));

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmGetColorizationColor(out uint pcrColorization, out bool pfOpaqueBlend);
}
