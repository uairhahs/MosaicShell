using System.Runtime.InteropServices;
using Avalonia.Media;
using MosaicShell.Core.Modules.Tessera;
using MosaicShell.Core.Services;

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
            Material = material;
        }

        /// <summary>The material last applied; the default frost until a flyout applies one.</summary>
        public static TesseraFlyoutMaterial Material { get; private set; } = TesseraFlyoutMaterialFactory.Create(useAcrylic: true);

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

        /// <summary>
        /// The system accent (<see cref="SystemAccent.Resolve"/>): Windows' DWM colorization, the desktop's portal accent
        /// on Linux, else the Windows blue. Each flyout build calls this, so a changed accent shows on the next flyout.
        /// </summary>
        public static void RefreshAccentFromSystem()
        {
            uint? colorization = null;
            if (OperatingSystem.IsWindows())
            {
                try
                {
                    if (DwmGetColorizationColor(out uint color, out _) == 0)
                    {
                        colorization = color;
                    }
                }
                catch { /* keep the other answers */ }
            }

            (byte a, byte r, byte g, byte b) = SystemAccent.Resolve(colorization, SystemAccent.DesktopAccent);
            SetAccent(Color.FromArgb(a, r, g, b));
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
