using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using SkiaSharp;

namespace MosaicShell.Host.Tiles.Tessera
{
    /// <summary>
    /// Shared-backdrop ancestor for Tessera flyout trees (consolidation scaffold).
    /// Live GDI/screen sampling is dormant while
    /// <see cref="Core.Modules.Tessera.TesseraFlyoutGlassPolicy.ForbidLiveBackdropPixelSampling"/>
    /// is true, SoftFrost paints Skia frost on Transparent HWND instead.
    /// Keep this wrap so future glass can opt in without rewiring chrome.
    /// </summary>
    internal sealed class TesseraSharedBackdropHost : Decorator
    {
        private SKImage? _blurred;
        private int _screenX = int.MinValue;
        private int _screenY = int.MinValue;
        private double _cachedBlur = -1;
        private Size _lastArrangeSize;

        public TesseraSharedBackdropHost()
        {
            IsHitTestVisible = true;
        }

        public static TesseraSharedBackdropHost? FindAncestor(Visual? from)
        {
            for (Visual? v = from; v is not null; v = v.GetVisualParent())
            {
                if (v is TesseraSharedBackdropHost host)
                {
                    return host;
                }
            }
            return null;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            Size arranged = base.ArrangeOverride(finalSize);
            if (Math.Abs(arranged.Width - _lastArrangeSize.Width) > 0.5
                || Math.Abs(arranged.Height - _lastArrangeSize.Height) > 0.5)
            {
                _lastArrangeSize = arranged;
                InvalidateCache();
            }
            return arranged;
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
            {
                return false;
            }

            if (!EnsureBlurred(blurRadius))
            {
                return false;
            }

            if (_blurred is null)
            {
                return false;
            }

            try
            {
                PixelPoint panelScreen = panel.PointToScreen(localBounds.TopLeft);
                int offsetX = panelScreen.X - _screenX;
                int offsetY = panelScreen.Y - _screenY;
                float w = (float)localBounds.Width;
                float h = (float)localBounds.Height;

                _ = dest.Save();
                dest.ClipRoundRect(round, antialias: true);
                using SKPaint paint = new()
                {
                    IsAntialias = true
                };
                SKRect src = SKRect.Create(offsetX, offsetY, w, h);
                SKRect dst = SKRect.Create(0, 0, w, h);
                dest.DrawImage(_blurred, src, dst, TesseraGlassPanel.MediumSampling, paint);
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
            {
                return false;
            }

            int screenX;
            int screenY;
            try
            {
                PixelPoint pt = this.PointToScreen(new Point(0, 0));
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

            using SKImage? capture = TesseraScreenBackdrop.TryCapture(this, Bounds);
            if (capture is null)
            {
                return false;
            }

            float blur = (float)Math.Clamp(blurRadius, 4, 28);
            using SKImageFilter blurFilter = SKImageFilter.CreateBlur(blur, blur, SKShaderTileMode.Clamp);
            using SKPaint paint = new()
            {
                ImageFilter = blurFilter,
                IsAntialias = true
            };

            int w = (int)Math.Ceiling(Bounds.Width);
            int h = (int)Math.Ceiling(Bounds.Height);
            SKImageInfo info = new(w, h, SKColorType.Rgba8888, SKAlphaType.Premul);
            using SKSurface? surface = SKSurface.Create(info);
            if (surface is null)
            {
                return false;
            }

            surface.Canvas.DrawImage(capture, 0, 0, TesseraGlassPanel.MediumSampling, paint);
            _blurred = surface.Snapshot();
            return _blurred is not null;
        }

        private void InvalidateCache()
        {
            _blurred?.Dispose();
            _blurred = null;
        }
    }
}
