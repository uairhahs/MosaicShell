using System.Runtime.InteropServices;
using Avalonia.Media;

namespace MosaicShell.Host.Tiles.Tessera;

/// <summary>YourFlyouts Fluent palette — SysAccent when available.</summary>
public static class TesseraPalette
{
    // Catppuccin Mocha crust #11111b — translucent frost (not pure black)
    public static Color Crust { get; } = Color.FromRgb(0x11, 0x11, 0x1b);
    public static Color Primary { get; } = Color.FromArgb(200, 0x11, 0x11, 0x1b);
    public static Color PrimarySolid { get; } = Color.FromArgb(220, 0x11, 0x11, 0x1b);
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
                // COLORREF is 0xAARRGGBB from DWM on modern Windows (actually 0xAABBGGRR historically —
                // DwmGetColorizationColor returns 0xAARRGGBB).
                var a = (byte)((color >> 24) & 0xFF);
                var r = (byte)((color >> 16) & 0xFF);
                var g = (byte)((color >> 8) & 0xFF);
                var b = (byte)(color & 0xFF);
                if (a == 0) a = 255;
                // Ignore near-black / near-white junk
                if (r + g + b > 30 && r + g + b < 750)
                    Accent = Color.FromArgb(a, r, g, b);
            }
        }
        catch { /* keep fallback */ }
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
