using System.Runtime.InteropServices;
using Avalonia.Media;
using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Host.Tiles.Tessera
{
    /// <summary>YourFlyouts Fluent palette - accent from settings or SysAccent; shell alpha from soft-frost material.</summary>
    public static class TesseraPalette
    {
        public static Color Crust { get; } = Color.FromRgb(0x11, 0x11, 0x1b);

        public static void ApplyMaterial(TesseraFlyoutMaterial material)
        {
            ShellAlpha = material.ShellAlpha;
            UseEdgeBlend = material.UseEdgeBlend;
        }

        public static byte ShellAlpha { get; private set; } = TesseraFlyoutMaterialFactory.SoftFrostShellAlpha;
        public static bool UseEdgeBlend { get; private set; } = true;

        public static Color Primary => Color.FromArgb(ShellAlpha, 0x11, 0x11, 0x1b);
        public static Color PrimarySolid => Color.FromArgb(
            (byte)Math.Min(255, ShellAlpha + 20), 0x11, 0x11, 0x1b);
        public static Color Font { get; } = Color.FromRgb(255, 255, 255);
        public static Color FontMuted { get; } = Color.FromRgb(150, 150, 150);
        public static Color TrackBack { get; } = Color.FromArgb(50, 255, 255, 255);
        public static Color Accent { get; private set; } = Color.FromRgb(2, 115, 205);
        public static Color OnAccent { get; private set; } = Color.FromRgb(255, 255, 255);

        static TesseraPalette()
        {
            RefreshAccentFromSystem();
        }

        /// <summary>Apply configured accent or fall back to Windows system accent.</summary>
        public static void ApplyAccentFromSettings(string? accentColor)
        {
            if (TesseraAccentColor.TryParse(accentColor, out byte r, out byte g, out byte b))
            {
                SetAccent(Color.FromRgb(r, g, b));
            }
            else
            {
                RefreshAccentFromSystem();
            }
        }

        public static void RefreshAccent()
        {
            RefreshAccentFromSystem();
        }

        public static void RefreshAccentFromSystem()
        {
            Color fallback = Color.FromRgb(2, 115, 205);
            try
            {
                if (DwmGetColorizationColor(out uint color, out _) == 0)
                {
                    byte a = (byte)((color >> 24) & 0xFF);
                    byte r = (byte)((color >> 16) & 0xFF);
                    byte g = (byte)((color >> 8) & 0xFF);
                    byte b = (byte)(color & 0xFF);
                    if (a == 0)
                    {
                        a = 255;
                    }

                    if (r + g + b is > 30 and < 750)
                    {
                        SetAccent(Color.FromArgb(a, r, g, b));
                        return;
                    }
                }
            }
            catch { /* keep fallback */ }

            SetAccent(fallback);
        }

        public static Color LightenAccent(double amount)
        {
            return Lighten(Accent, amount);
        }

        public static Color Lighten(Color c, double amount)
        {
            static byte L(byte v, double a)
            {
                return (byte)Math.Clamp(v + ((255 - v) * a), 0, 255);
            }

            return Color.FromRgb(L(c.R, amount), L(c.G, amount), L(c.B, amount));
        }

        private static void SetAccent(Color color)
        {
            Accent = color;
            OnAccent = ContrastOn(color);
        }

        private static Color ContrastOn(Color c)
        {
            double lum = (0.299 * c.R) + (0.587 * c.G) + (0.114 * c.B);
            return lum > 150 ? Color.FromRgb(27, 27, 30) : Color.FromRgb(255, 255, 255);
        }

        public static IBrush PrimaryBrush => new SolidColorBrush(Primary);
        public static IBrush PrimarySolidBrush => new SolidColorBrush(PrimarySolid);
        public static IBrush FontBrush => new SolidColorBrush(Font);
        public static IBrush FontMutedBrush => new SolidColorBrush(FontMuted);
        public static IBrush AccentBrush => new SolidColorBrush(Accent);
        public static IBrush OnAccentBrush => new SolidColorBrush(OnAccent);
        public static IBrush TrackBackBrush => new SolidColorBrush(TrackBack);
        public static IBrush StrokeBrush => new SolidColorBrush(Color.FromArgb(90, 255, 255, 255));

        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmGetColorizationColor(out uint pcrColorization, out bool pfOpaqueBlend);
    }
}
