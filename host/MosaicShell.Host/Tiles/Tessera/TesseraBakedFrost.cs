using Avalonia.Media;
using Avalonia.Media.Imaging;
using MosaicShell.Core;
using SkiaSharp;

namespace MosaicShell.Host.Tiles.Tessera;

/// <summary>
/// Lightweight prerendered soft-frost wash (Skia bake - MagickMeter-style idea without CLI).
/// Opt-in via TesseraSettings.UseBakedFrost; falls back to tint-only soft frost.
/// </summary>
public static class TesseraBakedFrost
{
    private const int TextureSize = 128;
    private const int CacheVersion = 1;
    private static IBrush? _brush;
    private static bool _enabled;
    private static readonly object Gate = new();

    public static void SetEnabled(bool enabled)
    {
        lock (Gate)
        {
            _enabled = enabled;
            if (!enabled)
                _brush = null;
        }
    }

    public static bool TryGetBrush(out IBrush brush)
    {
        brush = Brushes.Transparent;
        lock (Gate)
        {
            if (!_enabled) return false;
            _brush ??= LoadOrBake();
            if (_brush is null) return false;
            brush = _brush;
            return true;
        }
    }

    private static IBrush? LoadOrBake()
    {
        try
        {
            AppPaths.EnsureLayout();
            var path = Path.Combine(AppPaths.CacheDirectory, $"tessera-frost-v{CacheVersion}.png");
            if (!File.Exists(path))
                BakePng(path);

            if (!File.Exists(path)) return null;
            using var fs = File.OpenRead(path);
            var bmp = new Bitmap(fs);
            return new ImageBrush(bmp)
            {
                Stretch = Stretch.UniformToFill,
                Opacity = 1
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Soft noise + blur + mocha tint - small PNG for shell wash.</summary>
    public static void BakePng(string path)
    {
        var info = new SKImageInfo(TextureSize, TextureSize, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info);
        var canvas = surface.Canvas;
        canvas.Clear(new SKColor(0x11, 0x11, 0x1b, 40));

        var rnd = new Random(42);
        using (var paint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill })
        {
            for (var i = 0; i < 900; i++)
            {
                var a = (byte)rnd.Next(8, 36);
                paint.Color = new SKColor(0xCD, 0xD6, 0xF4, a);
                var x = (float)rnd.NextDouble() * TextureSize;
                var y = (float)rnd.NextDouble() * TextureSize;
                canvas.DrawCircle(x, y, (float)(1.2 + rnd.NextDouble() * 2.5), paint);
            }
        }

        using var noisy = surface.Snapshot();
        using var blurPaint = new SKPaint
        {
            IsAntialias = true,
            ImageFilter = SKImageFilter.CreateBlur(6.5f, 6.5f)
        };

        using var outSurface = SKSurface.Create(info);
        var outCanvas = outSurface.Canvas;
        outCanvas.Clear(new SKColor(0x11, 0x11, 0x1b, 50));
        outCanvas.DrawImage(noisy, 0, 0, blurPaint);

        using (var vignette = new SKPaint
        {
            IsAntialias = true,
            Shader = SKShader.CreateRadialGradient(
                new SKPoint(TextureSize / 2f, TextureSize / 2f),
                TextureSize * 0.72f,
                [new SKColor(0x11, 0x11, 0x1b, 0), new SKColor(0x11, 0x11, 0x1b, 90)],
                [0.45f, 1f],
                SKShaderTileMode.Clamp)
        })
        {
            outCanvas.DrawRect(0, 0, TextureSize, TextureSize, vignette);
        }

        using var final = outSurface.Snapshot();
        using var data = final.Encode(SKEncodedImageFormat.Png, 85);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var fs = File.Create(path);
        data.SaveTo(fs);
    }
}
