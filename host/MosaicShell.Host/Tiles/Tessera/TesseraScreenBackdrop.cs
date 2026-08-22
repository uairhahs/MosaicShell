using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using SkiaSharp;

namespace MosaicShell.Host.Tiles.Tessera;

/// <summary>Captures pixels behind a flyout so Skia glass can blur real wallpaper/desktop color.</summary>
internal static class TesseraScreenBackdrop
{
    public static SKImage? TryCapture(Visual visual, Rect localBounds)
    {
        if (localBounds.Width < 1 || localBounds.Height < 1)
            return null;

        try
        {
            var origin = visual.PointToScreen(localBounds.TopLeft);
            var w = Math.Max(1, (int)Math.Ceiling(localBounds.Width));
            var h = Math.Max(1, (int)Math.Ceiling(localBounds.Height));
            return CaptureScreenRegion(origin.X, origin.Y, w, h);
        }
        catch
        {
            return null;
        }
    }

    private static SKImage? CaptureScreenRegion(int screenX, int screenY, int width, int height)
    {
        var hdcScreen = GetDC(IntPtr.Zero);
        if (hdcScreen == IntPtr.Zero)
            return null;

        IntPtr hdcMem = IntPtr.Zero;
        IntPtr hBitmap = IntPtr.Zero;
        IntPtr oldObj = IntPtr.Zero;
        var bitsPtr = IntPtr.Zero;

        try
        {
            hdcMem = CreateCompatibleDC(hdcScreen);
            if (hdcMem == IntPtr.Zero)
                return null;

            var bi = new BitmapInfoHeader
            {
                Size = 40,
                Width = width,
                Height = -height, // top-down DIB
                Planes = 1,
                BitCount = 32,
                Compression = 0
            };

            hBitmap = CreateDIBSection(hdcScreen, ref bi, 0, out bitsPtr, IntPtr.Zero, 0);
            if (hBitmap == IntPtr.Zero || bitsPtr == IntPtr.Zero)
                return null;

            oldObj = SelectObject(hdcMem, hBitmap);
            if (!BitBlt(hdcMem, 0, 0, width, height, hdcScreen, screenX, screenY, Srccopy))
                return null;

            var info = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Opaque);
            using var skBmp = new SKBitmap(info);
            var byteCount = width * height * 4;
            var buffer = new byte[byteCount];
            Marshal.Copy(bitsPtr, buffer, 0, byteCount);
            Marshal.Copy(buffer, 0, skBmp.GetPixels(), byteCount);

            return SKImage.FromBitmap(skBmp);
        }
        finally
        {
            if (oldObj != IntPtr.Zero && hdcMem != IntPtr.Zero)
                SelectObject(hdcMem, oldObj);
            if (hBitmap != IntPtr.Zero)
                DeleteObject(hBitmap);
            if (hdcMem != IntPtr.Zero)
                DeleteDC(hdcMem);
            ReleaseDC(IntPtr.Zero, hdcScreen);
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
