using System.Runtime.InteropServices;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using MosaicShell.Core.Modules.Tessera;
using MosaicShell.Host.Capabilities;

namespace MosaicShell.Host.Diagnostics
{
    /// <summary>
    /// <c>--capture-glint &lt;dir&gt;</c>: writes the outside-click glint's reference images, so each OS's set can be compared
    /// for visual parity. The glint is frozen at each <see cref="TesseraOutsideClickRippleSpec.ReferenceProgressSteps"/>
    /// point over each flat <see cref="TesseraOutsideClickRippleSpec.ReferenceBackdrops"/> window of our own, and two images
    /// are written per point:
    /// <list type="bullet">
    /// <item><c>rendered-p0.30.png</c>: the glint's own pixels, rendered off-screen. These should match across OSes;
    /// a difference is in the drawing.</item>
    /// <item><c>screen-dark-p0.30.png</c>: what the screen shows in the glint window's rectangle, which holds only our
    /// backdrop and glint windows. Differences are the compositor's blending. Windows only for now: the Host is an
    /// XWayland client on KDE, and an X11 grab returns the X server's buffers, not KWin's composited output.</item>
    /// </list>
    /// </summary>
    internal static class GlintReferenceCapture
    {
        private const int SettleMs = 350;

        public static async Task RunAsync(string directory)
        {
            _ = Directory.CreateDirectory(directory);
            FocusDimRoute route = X11FocusDim.ResolveRoute();
            Window probe = new() { Width = 1, Height = 1, ShowActivated = false, ShowInTaskbar = false };
            Screen? screen = probe.Screens.Primary;
            double scaling = screen?.Scaling > 0.1 ? screen.Scaling : 1.0;
            PixelPoint center = screen is null
                ? new PixelPoint(400, 400)
                : new PixelPoint(screen.Bounds.X + 400, screen.Bounds.Y + 400);
            int sidePx = TesseraOutsideClickRippleSpec.WindowSizePx(scaling);
            (int x, int y) = TesseraOutsideClickRippleSpec.WindowOrigin(center.X, center.Y, scaling);
            PixelRect glintRect = new(x, y, sidePx, sidePx);
            bool canGrab = OperatingSystem.IsWindows();
            List<string> written = [];

            for (int b = 0; b < TesseraOutsideClickRippleSpec.ReferenceBackdrops.Count; b++)
            {
                (string name, (byte r, byte g, byte bl)) = TesseraOutsideClickRippleSpec.ReferenceBackdrops[b];
                // The backdrop covers the glint's rectangle with a margin, so the grab holds only our windows.
                int marginPx = (int)Math.Ceiling(40 * scaling);
                Window backdrop = new()
                {
                    Title = "MosaicShell - Glint reference backdrop",
                    WindowDecorations = WindowDecorations.None,
                    CanResize = false,
                    Topmost = true,
                    ShowInTaskbar = false,
                    ShowActivated = false,
                    Background = new SolidColorBrush(Color.FromRgb(r, g, bl)),
                    Width = (sidePx + (2 * marginPx)) / scaling,
                    Height = (sidePx + (2 * marginPx)) / scaling,
                    Position = new PixelPoint(x - marginPx, y - marginPx),
                };
                backdrop.Show();
                await Task.Delay(500);

                OutsideClickRippleWindow ripple = OutsideClickRippleWindow.ShowFrozen(center, route, scaling);
                await Task.Delay(500);
                foreach (double progress in TesseraOutsideClickRippleSpec.ReferenceProgressSteps)
                {
                    ripple.DrawAt(progress);
                    await Task.Delay(SettleMs);
                    string step = progress.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
                    if (b == 0)
                    {
                        string rendered = Path.Combine(directory, $"rendered-p{step}.png");
                        RenderStage(ripple.Stage, sidePx, scaling, rendered);
                        written.Add(Path.GetFileName(rendered));
                    }

                    if (canGrab)
                    {
                        string grabbed = Path.Combine(directory, $"screen-{name}-p{step}.png");
                        GrabWindowsScreenRect(glintRect, grabbed);
                        written.Add(Path.GetFileName(grabbed));
                    }
                }

                ripple.Close();
                backdrop.Close();
                await Task.Delay(300);
            }

            probe.Close();
            File.WriteAllText(Path.Combine(directory, "meta.json"), JsonSerializer.Serialize(new
            {
                os = RuntimeInformation.OSDescription,
                avalonia = typeof(Window).Assembly.GetName().Version?.ToString(),
                route = route.ToString(),
                scaling,
                windowSizePx = sidePx,
                steps = TesseraOutsideClickRippleSpec.ReferenceProgressSteps,
                backdrops = TesseraOutsideClickRippleSpec.ReferenceBackdrops.Select(bd => bd.Name),
                composited = canGrab
                    ? "screen-* are the composited screen in the glint window's rectangle"
                    : "not captured on this OS yet (XWayland grabs miss KWin's compositing); rendered-* only",
                files = written,
            }, new JsonSerializerOptions { WriteIndented = true }));
        }

        private static void RenderStage(Control stage, int sidePx, double scaling, string path)
        {
            using RenderTargetBitmap bitmap = new(new PixelSize(sidePx, sidePx), new Vector(96 * scaling, 96 * scaling));
            bitmap.Render(stage);
            bitmap.Save(path, PngBitmapEncoderOptions.Default);
        }

        /// <summary>Copies the screen pixels in <paramref name="rect"/> (BitBlt from the screen DC) to a PNG.</summary>
        private static void GrabWindowsScreenRect(PixelRect rect, string path)
        {
            nint screenDc = GetDC(IntPtr.Zero);
            nint memDc = CreateCompatibleDC(screenDc);
            BitmapInfoHeader header = new()
            {
                Size = (uint)Marshal.SizeOf<BitmapInfoHeader>(),
                Width = rect.Width,
                Height = -rect.Height, // top-down rows
                Planes = 1,
                BitCount = 32,
            };
            nint dib = CreateDIBSection(memDc, ref header, 0, out nint bits, IntPtr.Zero, 0);
            nint old = SelectObject(memDc, dib);
            try
            {
                _ = BitBlt(memDc, 0, 0, rect.Width, rect.Height, screenDc, rect.X, rect.Y, SrcCopy | CaptureBlt);
                using WriteableBitmap bitmap = new(
                    new PixelSize(rect.Width, rect.Height), new Vector(96, 96), PixelFormats.Bgra8888, AlphaFormat.Opaque);
                using (ILockedFramebuffer target = bitmap.Lock())
                {
                    int rowBytes = rect.Width * 4;
                    byte[] row = new byte[rowBytes];
                    for (int y = 0; y < rect.Height; y++)
                    {
                        Marshal.Copy(bits + (y * rowBytes), row, 0, rowBytes);
                        for (int i = 3; i < rowBytes; i += 4)
                        {
                            row[i] = 255; // the screen is opaque; GDI leaves the alpha byte undefined
                        }

                        Marshal.Copy(row, 0, target.Address + (y * target.RowBytes), rowBytes);
                    }
                }

                bitmap.Save(path, PngBitmapEncoderOptions.Default);
            }
            finally
            {
                _ = SelectObject(memDc, old);
                _ = DeleteObject(dib);
                _ = DeleteDC(memDc);
                _ = ReleaseDC(IntPtr.Zero, screenDc);
            }
        }

        private const uint SrcCopy = 0x00CC0020;
        private const uint CaptureBlt = 0x40000000;

        [StructLayout(LayoutKind.Sequential)]
        private struct BitmapInfoHeader
        {
            public uint Size;
            public int Width;
            public int Height;
            public ushort Planes;
            public ushort BitCount;
            public uint Compression;
            public uint SizeImage;
            public int XPelsPerMeter;
            public int YPelsPerMeter;
            public uint ClrUsed;
            public uint ClrImportant;
        }

        [DllImport("user32.dll")]
        private static extern nint GetDC(nint hWnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(nint hWnd, nint hDc);

        [DllImport("gdi32.dll")]
        private static extern nint CreateCompatibleDC(nint hDc);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteDC(nint hDc);

        [DllImport("gdi32.dll")]
        private static extern nint CreateDIBSection(nint hDc, ref BitmapInfoHeader header, uint usage, out nint bits, nint section, uint offset);

        [DllImport("gdi32.dll")]
        private static extern nint SelectObject(nint hDc, nint obj);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(nint obj);

        [DllImport("gdi32.dll")]
        private static extern bool BitBlt(nint dest, int x, int y, int width, int height, nint src, int srcX, int srcY, uint rop);
    }
}
