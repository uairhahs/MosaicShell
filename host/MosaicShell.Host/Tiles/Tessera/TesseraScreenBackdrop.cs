using System.Runtime.InteropServices;
using Avalonia;
using SkiaSharp;

namespace MosaicShell.Host.Tiles.Tessera
{
    /// <summary>Captures pixels behind a flyout so Skia glass can blur real wallpaper/desktop color.</summary>
    internal static class TesseraScreenBackdrop
    {
        public static SKImage? TryCapture(Visual visual, Rect localBounds)
        {
            // GDI BitBlt is a Tessera-only fallback; Avalonia AcrylicBlur/Transparent is preferred.
            if (!TesseraGlass.AllowGdiScreenCapture || !TesseraGlass.UseBackdropBlur)
            {
                return null;
            }

            if (localBounds.Width < 1 || localBounds.Height < 1)
            {
                return null;
            }

            try
            {
                PixelPoint topLeft = visual.PointToScreen(localBounds.TopLeft);
                PixelPoint bottomRight = visual.PointToScreen(localBounds.BottomRight);
                int w = Math.Max(1, bottomRight.X - topLeft.X);
                int h = Math.Max(1, bottomRight.Y - topLeft.Y);
                return CaptureScreenRegion(topLeft.X, topLeft.Y, w, h);
            }
            catch
            {
                return null;
            }
        }

        private static SKImage? CaptureScreenRegion(int screenX, int screenY, int width, int height)
        {
            nint hdcScreen = GetDC(IntPtr.Zero);
            if (hdcScreen == IntPtr.Zero)
            {
                return null;
            }

            IntPtr hdcMem = IntPtr.Zero;
            IntPtr hBitmap = IntPtr.Zero;
            IntPtr oldObj = IntPtr.Zero;
            try
            {
                hdcMem = CreateCompatibleDC(hdcScreen);
                if (hdcMem == IntPtr.Zero)
                {
                    return null;
                }

                BitmapInfoHeader bi = new()
                {
                    Size = 40,
                    Width = width,
                    Height = -height, // top-down DIB
                    Planes = 1,
                    BitCount = 32,
                    Compression = 0
                };

                hBitmap = CreateDIBSection(hdcScreen, ref bi, 0, out nint bitsPtr, IntPtr.Zero, 0);
                if (hBitmap == IntPtr.Zero || bitsPtr == IntPtr.Zero)
                {
                    return null;
                }

                oldObj = SelectObject(hdcMem, hBitmap);
                if (!BitBlt(hdcMem, 0, 0, width, height, hdcScreen, screenX, screenY, Srccopy))
                {
                    return null;
                }

                SKImageInfo info = new(width, height, SKColorType.Bgra8888, SKAlphaType.Opaque);
                using SKBitmap skBmp = new(info);
                int byteCount = width * height * 4;
                byte[] buffer = new byte[byteCount];
                Marshal.Copy(bitsPtr, buffer, 0, byteCount);
                Marshal.Copy(buffer, 0, skBmp.GetPixels(), byteCount);

                return SKImage.FromBitmap(skBmp);
            }
            finally
            {
                if (oldObj != IntPtr.Zero && hdcMem != IntPtr.Zero)
                {
                    _ = SelectObject(hdcMem, oldObj);
                }

                if (hBitmap != IntPtr.Zero)
                {
                    _ = DeleteObject(hBitmap);
                }

                if (hdcMem != IntPtr.Zero)
                {
                    _ = DeleteDC(hdcMem);
                }

                _ = ReleaseDC(IntPtr.Zero, hdcScreen);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct BitmapInfoHeader
        {
            public int Size;
            public int Width;
            public int Height;
            public short Planes;
            public short BitCount;
            public int Compression;
            public int SizeImage;
            public int XPelsPerMeter;
            public int YPelsPerMeter;
            public int ClrUsed;
            public int ClrImportant;
        }

        private const int Srccopy = 0x00CC0020;

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hwnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        [DllImport("gdi32.dll")]
        private static extern bool BitBlt(
            IntPtr hdcDest, int xDest, int yDest, int width, int height,
            IntPtr hdcSrc, int xSrc, int ySrc, int rop);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateDIBSection(
            IntPtr hdc, ref BitmapInfoHeader pbmi, uint usage, out IntPtr ppvBits, IntPtr hSection, uint offset);
    }
}
