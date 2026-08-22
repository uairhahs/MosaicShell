using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using SkiaSharp;

namespace MosaicShell.Host.Tiles.Tessera;

/// <summary>
/// One screen capture + blur per flyout; glass panels blit sub-rects instead of re-capturing.
/// </summary>
internal sealed class TesseraSharedBackdropHost : Control
{
    private SKImage? _blurred;
    private int _screenX = int.MinValue;
    private int _screenY = int.MinValue;
    private double _cachedBlur = -1;
    private int _generation;

    public TesseraSharedBackdropHost()
    {
        IsHitTestVisible = false;
    }

    public static TesseraSharedBackdropHost? FindAncestor(Visual? from)
    {
        for (var v = from; v is not null; v = v.GetVisualParent())
        {
            if (v is TesseraSharedBackdropHost host)
                return host;
        }
        return null;
    }

    protected override Size MeasureOverride(Size availableSize) => default;

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (finalSize.Width > 0 && finalSize.Height > 0)
            InvalidateCache();
        return finalSize;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        InvalidateCache();
        base.OnDetachedFromVisualTree(e);
    }

    internal bool TryBlitSubrect(
        SKCanvas dest,
        SKRoundRect round,
        Visual panel,
        Rect localBounds,
        double blurRadius)
    {
        if (TesseraGlass.PreviewMode || !TesseraGlass.UseBackdropBlur)
            return false;

        if (!EnsureBlurred(blurRadius))
            return false;

        if (_blurred is null)
            return false;

        try
        {
            var panelScreen = panel.PointToScreen(localBounds.TopLeft);
            var offsetX = panelScreen.X - _screenX;
            var offsetY = panelScreen.Y - _screenY;
            var w = (float)localBounds.Width;
            var h = (float)localBounds.Height;

            dest.Save();
            dest.ClipRoundRect(round, antialias: true);
            using var paint = new SKPaint
            {
                IsAntialias = true,
                FilterQuality = SKFilterQuality.Medium
            };
            var src = SKRect.Create(offsetX, offsetY, w, h);
            var dst = SKRect.Create(0, 0, w, h);
            dest.DrawImage(_blurred, src, dst, paint);
            dest.Restore();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private bool EnsureBlurred(double blurRadius)
    {
        if (Bounds.Width < 1 || Bounds.Height < 1)
            return false;

        var screenX = int.MinValue;
        var screenY = int.MinValue;
        try
        {
            var pt = ((Visual)this).PointToScreen(new Point(0, 0));
            screenX = pt.X;
            screenY = pt.Y;
        }
        catch
        {
            return false;
        }

        if (_blurred is not null
            && _screenX == screenX
            && _screenY == screenY
            && Math.Abs(_cachedBlur - blurRadius) < 0.01
            && Math.Abs(Bounds.Width - _blurred.Width) < 1
            && Math.Abs(Bounds.Height - _blurred.Height) < 1)
        {
            return true;
        }

        InvalidateCache();
        _screenX = screenX;
        _screenY = screenY;
        _cachedBlur = blurRadius;

        using var capture = TesseraScreenBackdrop.TryCapture(this, Bounds);
        if (capture is null)
            return false;

        var blur = (float)Math.Clamp(blurRadius, 4, 28);
        using var blurFilter = SKImageFilter.CreateBlur(blur, blur, SKShaderTileMode.Clamp);
        using var paint = new SKPaint
        {
            ImageFilter = blurFilter,
            IsAntialias = true,
            FilterQuality = SKFilterQuality.Medium
        };

        var w = (int)Math.Ceiling(Bounds.Width);
        var h = (int)Math.Ceiling(Bounds.Height);
        var info = new SKImageInfo(w, h, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info);
        if (surface is null)
            return false;

        surface.Canvas.DrawImage(capture, 0, 0, paint);
        _blurred = surface.Snapshot();
        return _blurred is not null;
    }

    private void InvalidateCache()
    {
        _blurred?.Dispose();
        _blurred = null;
        _generation++;
    }
}
