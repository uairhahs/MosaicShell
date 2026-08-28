using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Avalonia.VisualTree;
using MosaicShell.Core.Modules.Tessera;
using SkiaSharp;

namespace MosaicShell.Host.Tiles.Tessera
{
    /// <summary>Global Skia glass policy (replaces baked frost PNG wash).</summary>
    public static class TesseraGlass
    {
        /// <summary>
        /// When true and <see cref="AllowGdiScreenCapture"/> is also true, sample/blur the live
        /// desktop via GDI BitBlt. Default is false, prefer Avalonia AcrylicBlur / Transparent.
        /// </summary>
        public static bool UseBackdropBlur { get; set; }

        /// <summary>
        /// Opt-in GDI screen capture for glass. Off by default (Avalonia docs prefer OS transparency).
        /// Set true only when OS acrylic is unavailable and BitBlt glass is explicitly desired.
        /// </summary>
        public static bool AllowGdiScreenCapture { get; set; }

        /// <summary>Embedded previews (module config), backdrop sampling is unstable; use fallback glass.</summary>
        public static bool PreviewMode { get; set; }

        /// <summary>While true, glass shells render as simple borders (config preview build).</summary>
        public static bool EmbeddedPreviewBuild { get; set; }

        /// <summary>OS AcrylicBlur trial: skip Skia frost slab; HWND blur + light edge only.</summary>
        public static bool UseOsAcrylicChrome { get; set; }

        /// <summary>Live flyout: skip inner TesseraChrome.Glass on Meter/Amber (see Core policy).</summary>
        public static bool SuppressInnerSkiaGlass { get; set; }

        public static bool IsEmbeddedPreviewContext(Visual? visual)
        {
            for (Visual? v = visual; v is not null; v = v.GetVisualParent())
            {
                if (v is TesseraLiveHost { IsEmbeddedPreview: true })
                {
                    return true;
                }

                if (v is TesseraStylePreview)
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>Skia glass shell shared by Tessera chrome.</summary>
    public static class TesseraGlassPanel
    {
        // Pre-consolidation (4fcc41a) values, consolidation crushed these and Soft frost vanished.
        public const double DefaultBlurRadius = 11;
        internal const byte BlurredTintAlphaMax = 48;
        internal const byte FallbackTintAlphaMax = 80;

        /// <summary>Former SKFilterQuality.Medium, SkiaSharp 3 sampling for DrawImage.</summary>
        internal static readonly SKSamplingOptions MediumSampling =
            new(SKFilterMode.Linear, SKMipmapMode.Linear);

        /// <summary>Cap shell tint so backdrop blur stays visible (true glass, not matte slab).</summary>
        public static Color NormalizeTint(Color color)
        {
            byte alpha = color.A switch
            {
                0 => 40,
                255 => BlurredTintAlphaMax,
                _ => (byte)Math.Clamp(color.A / 3, 24, BlurredTintAlphaMax)
            };
            return Color.FromArgb(alpha, color.R, color.G, color.B);
        }

        public static Control Wrap(
            Control child,
            double cornerRadius,
            Thickness? padding = null,
            double? width = null,
            double? height = null,
            double? minWidth = null,
            double? maxWidth = null,
            double? maxHeight = null,
            Color? tint = null,
            double blurRadius = DefaultBlurRadius,
            bool useSharedBackdrop = false,
            bool lightTintOnly = false)
        {
            if (TesseraGlass.EmbeddedPreviewBuild)
            {
                return WrapEmbeddedSimple(child, cornerRadius, padding, width, height, minWidth, maxWidth, maxHeight);
            }

            Control content = child;
            if (padding is { } pad && pad != default)
            {
                content = new Border
                {
                    Padding = pad,
                    Background = Brushes.Transparent,
                    Child = child
                };
            }

            TesseraGlassBackground glass = new()
            {
                CornerRadius = cornerRadius,
                BlurRadius = blurRadius,
                Tint = NormalizeTint(tint ?? TesseraPalette.Primary),
                UseSharedBackdrop = useSharedBackdrop,
                LightTintOnly = lightTintOnly,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            Grid grid = new()
            {
                ClipToBounds = true,
                HorizontalAlignment = width is null ? HorizontalAlignment.Left : HorizontalAlignment.Stretch,
                VerticalAlignment = height is null ? VerticalAlignment.Top : VerticalAlignment.Stretch,
                Children = { glass, content }
            };

            if (width is { } w)
            {
                grid.Width = w;
                grid.MinWidth = w;
                grid.MaxWidth = w;
            }
            else if (minWidth is { } mnw)
            {
                grid.MinWidth = mnw;
            }

            if (height is { } hh)
            {
                grid.Height = hh;
                grid.MinHeight = hh;
                grid.MaxHeight = hh;
            }
            else if (maxHeight is { } mxh)
            {
                grid.MaxHeight = mxh;
            }

            if (maxWidth is { } mw && width is null)
            {
                grid.MaxWidth = mw;
            }

            return grid;
        }

        private static Control WrapEmbeddedSimple(
            Control child,
            double cornerRadius,
            Thickness? padding,
            double? width,
            double? height,
            double? minWidth,
            double? maxWidth,
            double? maxHeight)
        {
            Border shell = new()
            {
                CornerRadius = new CornerRadius(cornerRadius),
                Background = new SolidColorBrush(Color.FromArgb(210, 0x11, 0x11, 0x1b)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(55, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                Padding = padding ?? default,
                ClipToBounds = true,
                Child = child
            };

            if (width is { } w)
            {
                shell.Width = w;
                shell.MinWidth = w;
                shell.MaxWidth = w;
            }
            else if (minWidth is { } mnw)
            {
                shell.MinWidth = mnw;
            }

            if (height is { } hh)
            {
                shell.Height = hh;
                shell.MinHeight = hh;
                shell.MaxHeight = hh;
            }
            else if (maxHeight is { } mxh)
            {
                shell.MaxHeight = mxh;
            }

            if (maxWidth is { } mw && width is null)
            {
                shell.MaxWidth = mw;
            }

            return shell;
        }

        internal static SKColor ToSkColor(Color color)
        {
            return new(color.R, color.G, color.B, color.A);
        }
    }

    internal sealed class TesseraGlassBackground : Control
    {
        public static readonly StyledProperty<double> CornerRadiusProperty =
            AvaloniaProperty.Register<TesseraGlassBackground, double>(nameof(CornerRadius), 14);

        public static readonly StyledProperty<double> BlurRadiusProperty =
            AvaloniaProperty.Register<TesseraGlassBackground, double>(nameof(BlurRadius), TesseraGlassPanel.DefaultBlurRadius);

        public static readonly StyledProperty<Color> TintProperty =
            AvaloniaProperty.Register<TesseraGlassBackground, Color>(nameof(Tint), TesseraPalette.Primary);

        public static readonly StyledProperty<bool> UseSharedBackdropProperty =
            AvaloniaProperty.Register<TesseraGlassBackground, bool>(nameof(UseSharedBackdrop));

        public static readonly StyledProperty<bool> LightTintOnlyProperty =
            AvaloniaProperty.Register<TesseraGlassBackground, bool>(nameof(LightTintOnly));

        private sealed class LayerCache : IDisposable
        {
            public SKImage? Image;
            public Rect Bounds;
            public int ScreenX;
            public int ScreenY;
            public int Generation;
            public Color Tint;
            public double BlurRadius;
            public double CornerRadius;

            public bool Matches(Rect bounds, int screenX, int screenY, int generation, Color tint, double blurRadius, double cornerRadius)
            {
                return Generation == generation
                && Tint == tint
                && Math.Abs(BlurRadius - blurRadius) < 0.01
                && Math.Abs(CornerRadius - cornerRadius) < 0.01
                && Math.Abs(Bounds.Width - bounds.Width) < 1
                && Math.Abs(Bounds.Height - bounds.Height) < 1
                && ScreenX == screenX
                && ScreenY == screenY
                && Image is not null;
            }

            public void Dispose()
            {
                Image?.Dispose();
                Image = null;
            }
        }

        private LayerCache? _layerCache;
        private int _glassGeneration;

        public TesseraGlassBackground()
        {
            IsHitTestVisible = false;
        }

        public double CornerRadius
        {
            get => GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        public double BlurRadius
        {
            get => GetValue(BlurRadiusProperty);
            set => SetValue(BlurRadiusProperty, value);
        }

        public Color Tint
        {
            get => GetValue(TintProperty);
            set => SetValue(TintProperty, value);
        }

        public bool UseSharedBackdrop
        {
            get => GetValue(UseSharedBackdropProperty);
            set => SetValue(UseSharedBackdropProperty, value);
        }

        public bool LightTintOnly
        {
            get => GetValue(LightTintOnlyProperty);
            set => SetValue(LightTintOnlyProperty, value);
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            // Contract: GlassBackgroundClaimsAvailableSize must stay false (4fcc41a paint-only).
            // Claiming available height Y-stretches Stretch tracks in Amber/CoreUI/Fluent/Win11.
            _ = TesseraFlyoutGlassPolicy.GlassBackgroundClaimsAvailableSize;
            return default;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            return finalSize;
        }

        public override void Render(DrawingContext context)
        {
            if (Bounds.Width <= 0 || Bounds.Height <= 0)
            {
                return;
            }

            // 4fcc41a: Custom Skia only, no Avalonia mocha underlay (that made Soft frost look matte/black).
            context.Custom(new TesseraGlassDrawOperation(
                this,
                Bounds,
                CornerRadius,
                BlurRadius,
                Tint,
                _glassGeneration));
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            if (change.Property == CornerRadiusProperty
                || change.Property == BlurRadiusProperty
                || change.Property == TintProperty
                || change.Property == UseSharedBackdropProperty
                || change.Property == LightTintOnlyProperty)
            {
                _glassGeneration++;
                InvalidateVisual();
            }
            else if (change.Property == BoundsProperty)
            {
                if (!TesseraGlass.IsEmbeddedPreviewContext(this))
                {
                    InvalidateVisual();
                }
            }
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            _layerCache?.Dispose();
            _layerCache = null;
            base.OnDetachedFromVisualTree(e);
        }

        internal SKImage? EnsureLayer(
            SKCanvas targetCanvas,
            SKSurface sourceSurface,
            Rect bounds,
            double cornerRadius,
            double blurRadius,
            Color tint,
            int generation)
        {
            int screenX = int.MinValue;
            int screenY = int.MinValue;
            if (VisualRoot is not null)
            {
                try
                {
                    PixelPoint pt = this.PointToScreen(bounds.TopLeft);
                    screenX = pt.X;
                    screenY = pt.Y;
                }
                catch { /* ignore */ }
            }

            if (_layerCache?.Matches(bounds, screenX, screenY, generation, tint, blurRadius, cornerRadius) == true)
            {
                return _layerCache.Image;
            }

            _layerCache?.Dispose();
            _layerCache = new LayerCache
            {
                Bounds = bounds,
                ScreenX = screenX,
                ScreenY = screenY,
                Generation = generation,
                Tint = tint,
                BlurRadius = blurRadius,
                CornerRadius = cornerRadius
            };

            int w = (int)Math.Ceiling(bounds.Width);
            int h = (int)Math.Ceiling(bounds.Height);
            if (w <= 0 || h <= 0)
            {
                return null;
            }

            SKImageInfo info = new(w, h, SKColorType.Rgba8888, SKAlphaType.Premul);
            using SKSurface? layerSurface = SKSurface.Create(info);
            if (layerSurface is null)
            {
                return null;
            }

            SKCanvas lc = layerSurface.Canvas;
            lc.Clear(SKColors.Transparent);

            SKRect rect = SKRect.Create(0, 0, w, h);
            SKRoundRect round = new(rect, (float)cornerRadius, (float)cornerRadius);

            bool osAcrylic = TesseraGlass.UseOsAcrylicChrome
                && !TesseraGlass.PreviewMode
                && !TesseraGlass.IsEmbeddedPreviewContext(this);

            if (!osAcrylic)
            {
                // Always paint translucent frost chrome. Do NOT GDI/BitBlt or shared-capture into
                // this layer, that self-captures the flyout as opaque black (Soft frost → black boxes).
                // Real wallpaper shows through Transparent HWND + alpha frost (4fcc41a fake-glass recipe).
                bool simulatedBlur = TesseraGlass.UseBackdropBlur
                    && !TesseraGlass.PreviewMode
                    && !TesseraGlass.IsEmbeddedPreviewContext(this);
                TesseraGlassDrawOperation.DrawFallbackGlass(lc, round, w, h, lighterFrost: simulatedBlur);
            }

            bool drewBackdrop = false;
            bool maySample = !osAcrylic
                && TesseraGlass.UseBackdropBlur
                && !TesseraGlass.PreviewMode
                && !TesseraGlass.IsEmbeddedPreviewContext(this)
                && !TesseraFlyoutGlassPolicy.ForbidLiveBackdropPixelSampling;

            if (maySample && UseSharedBackdrop)
            {
                TesseraSharedBackdropHost? shared = TesseraSharedBackdropHost.FindAncestor(this);
                if (shared?.TryBlitSubrect(lc, round, this, bounds, blurRadius) == true)
                {
                    drewBackdrop = true;
                }
            }

            if (maySample && !drewBackdrop)
            {
                // 4fcc41a: blur Avalonia's Skia surface (not GDI). Still gated, can sample empty buffer.
                drewBackdrop = TesseraGlassDrawOperation.TryDrawBackdropBlur(
                    lc, targetCanvas, sourceSurface, rect, round, blurRadius);
            }

            if (osAcrylic)
            {
                TesseraGlassDrawOperation.DrawGlassChrome(lc, round);
            }
            else
            {
                bool simulatedBlur = TesseraGlass.UseBackdropBlur
                    && !maySample
                    && !TesseraGlass.PreviewMode
                    && !TesseraGlass.IsEmbeddedPreviewContext(this);
                TesseraGlassDrawOperation.DrawShellTint(
                    lc,
                    round,
                    tint,
                    drewBackdrop || simulatedBlur,
                    lightTintOnly: false);
                TesseraGlassDrawOperation.DrawGlassChrome(lc, round);
            }

            _layerCache.Image = layerSurface.Snapshot();
            return _layerCache.Image;
        }
    }

    internal sealed class TesseraGlassDrawOperation(
        TesseraGlassBackground owner,
        Rect bounds,
        double cornerRadius,
        double blurRadius,
        Color tint,
        int generation) : ICustomDrawOperation
    {
        private readonly TesseraGlassBackground _owner = owner;
        private readonly double _cornerRadius = cornerRadius;
        private readonly double _blurRadius = blurRadius;
        private readonly Color _tint = tint;
        private readonly int _generation = generation;

        public Rect Bounds { get; } = bounds;

        public void Dispose() { }

        public bool HitTest(Point p)
        {
            return false;
        }

        public bool Equals(ICustomDrawOperation? other)
        {
            return other is TesseraGlassDrawOperation op
            && op._owner == _owner
            && op.Bounds == Bounds
            && op._cornerRadius == _cornerRadius
            && op._blurRadius == _blurRadius
            && op._tint == _tint
            && op._generation == _generation;
        }

        public void Render(ImmediateDrawingContext context)
        {
            ISkiaSharpApiLeaseFeature? leaseFeature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
            if (leaseFeature is null)
            {
                return;
            }

            using ISkiaSharpApiLease lease = leaseFeature.Lease();
            SKCanvas canvas = lease.SkCanvas;
            SKSurface? surface = lease.SkSurface;
            if (surface is null)
            {
                return;
            }

            float w = (float)Bounds.Width;
            float h = (float)Bounds.Height;
            if (w <= 0.5f || h <= 0.5f)
            {
                return;
            }

            SKImage? layer = _owner.EnsureLayer(canvas, surface, Bounds, _cornerRadius, _blurRadius, _tint, _generation);
            if (layer is null)
            {
                return;
            }

            float radius = (float)Math.Max(0, _cornerRadius);
            SKRoundRect round = new(SKRect.Create(0, 0, w, h), radius, radius);

            using SKPaint blit = new() { IsAntialias = true };
            _ = canvas.Save();
            canvas.ClipRoundRect(round, antialias: true);
            canvas.DrawImage(layer, 0, 0, TesseraGlassPanel.MediumSampling, blit);
            canvas.Restore();
        }

        internal static bool TryDrawImageBackdropBlur(
            SKCanvas dest,
            SKImage source,
            SKRoundRect round,
            double blurRadius)
        {
            float blur = (float)Math.Clamp(blurRadius, 4, 28);
            using SKImageFilter blurFilter = SKImageFilter.CreateBlur(blur, blur, SKShaderTileMode.Clamp);
            using SKPaint paint = new()
            {
                ImageFilter = blurFilter,
                IsAntialias = true
            };

            _ = dest.Save();
            dest.ClipRoundRect(round, antialias: true);
            dest.DrawImage(source, 0, 0, TesseraGlassPanel.MediumSampling, paint);
            dest.Restore();
            return true;
        }

        internal static bool TryDrawBackdropBlur(
            SKCanvas dest,
            SKCanvas sourceCanvas,
            SKSurface sourceSurface,
            SKRect rect,
            SKRoundRect round,
            double blurRadius)
        {
            if (!sourceCanvas.TotalMatrix.TryInvert(out SKMatrix inverse))
            {
                return false;
            }

            using SKImage? snapshot = sourceSurface.Snapshot();
            if (snapshot is null)
            {
                return false;
            }

            float blur = (float)Math.Clamp(blurRadius, 4, 24);
            using SKShader shader = snapshot.ToShader(SKShaderTileMode.Clamp, SKShaderTileMode.Clamp, inverse);
            using SKImageFilter blurFilter = SKImageFilter.CreateBlur(blur, blur, SKShaderTileMode.Clamp);
            using SKPaint blurPaint = new()
            {
                Shader = shader,
                ImageFilter = blurFilter,
                IsAntialias = true
            };

            _ = dest.Save();
            dest.ClipRoundRect(round, antialias: true);
            dest.DrawRect(rect, blurPaint);
            dest.Restore();
            return true;
        }

        internal static void DrawFallbackGlass(
            SKCanvas canvas,
            SKRoundRect round,
            int w,
            int h,
            bool lighterFrost = false)
        {
            // Recipe from 4fcc41a (pre-consolidation), dark slab + highlight so Soft frost reads.
            byte baseAlpha = lighterFrost ? (byte)68 : (byte)96;
            using SKPaint basePaint = new()
            {
                Color = new SKColor(17, 17, 27, baseAlpha),
                IsAntialias = true
            };
            canvas.DrawRoundRect(round, basePaint);

            byte hiTop = lighterFrost ? (byte)36 : (byte)28;
            byte hiBottom = lighterFrost ? (byte)10 : (byte)6;
            using SKPaint highlight = new()
            {
                Shader = SKShader.CreateLinearGradient(
                    new SKPoint(0, 0),
                    new SKPoint(w * 0.55f, h * 0.55f),
                    [new SKColor(255, 255, 255, hiTop), new SKColor(255, 255, 255, hiBottom)],
                    [0f, 1f],
                    SKShaderTileMode.Clamp),
                IsAntialias = true,
                BlendMode = SKBlendMode.Plus
            };
            canvas.DrawRoundRect(round, highlight);

            byte grainAlpha = lighterFrost ? (byte)14 : (byte)10;
            using SKPaint noisePaint = new()
            {
                Shader = SKShader.CreatePerlinNoiseFractalNoise(0.85f, 0.6f, 2, 0),
                Color = new SKColor(255, 255, 255, grainAlpha),
                IsAntialias = true,
                BlendMode = SKBlendMode.Overlay
            };
            _ = canvas.Save();
            canvas.ClipRoundRect(round, antialias: true);
            canvas.DrawRect(0, 0, w, h, noisePaint);
            canvas.Restore();
        }

        internal static void DrawShellTint(SKCanvas canvas, SKRoundRect round, Color tint, bool blurred, bool lightTintOnly = false)
        {
            SKColor sk = TesseraGlassPanel.ToSkColor(tint);
            byte alpha = blurred
                ? Math.Min(sk.Alpha, TesseraGlassPanel.BlurredTintAlphaMax)
                : (byte)Math.Min(Math.Max((int)sk.Alpha, 28), TesseraGlassPanel.FallbackTintAlphaMax);
            if (lightTintOnly)
            {
                alpha = (byte)Math.Max(12, alpha / 2);
            }

            sk = sk.WithAlpha(alpha);
            if (sk.Alpha == 0)
            {
                return;
            }

            // SoftLight (4fcc41a), SrcOver over dark frost reads as a matte tint slab.
            using SKPaint tintPaint = new()
            {
                Color = sk,
                IsAntialias = true,
                BlendMode = SKBlendMode.SoftLight
            };
            canvas.DrawRoundRect(round, tintPaint);
        }

        internal static void DrawGlassChrome(SKCanvas canvas, SKRoundRect round)
        {
            // Uniform edge only, no radial specular (reads as a spotlight on small panels).
            byte edgeAlpha = (byte)(TesseraPalette.UseEdgeBlend ? 48 : 64);
            using SKPaint edgePaint = new()
            {
                Color = new SKColor(255, 255, 255, edgeAlpha),
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1,
                BlendMode = SKBlendMode.Overlay
            };
            canvas.DrawRoundRect(round, edgePaint);
        }
    }
}
