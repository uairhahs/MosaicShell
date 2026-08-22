using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Avalonia.VisualTree;
using SkiaSharp;

namespace MosaicShell.Host.Tiles.Tessera;

/// <summary>Global Skia glass policy (replaces baked frost PNG wash).</summary>
public static class TesseraGlass
{
    /// <summary>When true, sample and blur the live backdrop; otherwise use gradient + grain fallback.</summary>
    public static bool UseBackdropBlur { get; set; } = true;

    /// <summary>Embedded previews (module config) — backdrop sampling is unstable; use fallback glass.</summary>
    public static bool PreviewMode { get; set; }
}

/// <summary>Skia glass shell shared by Tessera chrome.</summary>
public static class TesseraGlassPanel
{
    public const double DefaultBlurRadius = 14;
    internal const byte BlurredTintAlphaMax = 34;
    internal const byte FallbackTintAlphaMax = 72;

    /// <summary>Cap shell tint so backdrop blur stays visible (true glass, not matte slab).</summary>
    public static Color NormalizeTint(Color color)
    {
        var alpha = color.A switch
        {
            0 => (byte)40,
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
        var content = child;
        if (padding is { } pad && pad != default)
        {
            content = new Border
            {
                Padding = pad,
                Background = Brushes.Transparent,
                Child = child
            };
        }

        var glass = new TesseraGlassBackground
        {
            CornerRadius = cornerRadius,
            BlurRadius = blurRadius,
            Tint = NormalizeTint(tint ?? TesseraPalette.Primary),
            UseSharedBackdrop = useSharedBackdrop,
            LightTintOnly = lightTintOnly,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };

        var grid = new Grid
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
            grid.MaxWidth = mw;

        return grid;
    }

    internal static SKColor ToSkColor(Color color) =>
        new(color.R, color.G, color.B, color.A);
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

        public bool Matches(Rect bounds, int screenX, int screenY, int generation, Color tint, double blurRadius, double cornerRadius) =>
            Generation == generation
            && Tint == tint
            && Math.Abs(BlurRadius - blurRadius) < 0.01
            && Math.Abs(CornerRadius - cornerRadius) < 0.01
            && Math.Abs(Bounds.Width - bounds.Width) < 1
            && Math.Abs(Bounds.Height - bounds.Height) < 1
            && ScreenX == screenX
            && ScreenY == screenY
            && Image is not null;

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

    protected override Size MeasureOverride(Size availableSize) => default;

    protected override Size ArrangeOverride(Size finalSize) => finalSize;

    public override void Render(DrawingContext context)
    {
        if (Bounds.Width > 0 && Bounds.Height > 0)
        {
            context.Custom(new TesseraGlassDrawOperation(
                this,
                Bounds,
                CornerRadius,
                BlurRadius,
                Tint,
                _glassGeneration));
        }
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
            InvalidateVisual();
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
        var screenX = int.MinValue;
        var screenY = int.MinValue;
        if (this.GetVisualRoot() is not null)
        {
            try
            {
                var pt = this.PointToScreen(bounds.TopLeft);
                screenX = pt.X;
                screenY = pt.Y;
            }
            catch { /* ignore */ }
        }

        if (_layerCache?.Matches(bounds, screenX, screenY, generation, tint, blurRadius, cornerRadius) == true)
            return _layerCache.Image;

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

        var w = (int)Math.Ceiling(bounds.Width);
        var h = (int)Math.Ceiling(bounds.Height);
        if (w <= 0 || h <= 0)
            return null;

        var info = new SKImageInfo(w, h, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var layerSurface = SKSurface.Create(info);
        if (layerSurface is null)
            return null;

        var lc = layerSurface.Canvas;
        lc.Clear(SKColors.Transparent);

        var rect = SKRect.Create(0, 0, w, h);
        var round = new SKRoundRect(rect, (float)cornerRadius, (float)cornerRadius);

        var drewBackdrop = false;
        if (UseSharedBackdrop)
        {
            var shared = TesseraSharedBackdropHost.FindAncestor(this);
            if (shared?.TryBlitSubrect(lc, round, this, bounds, blurRadius) == true)
                drewBackdrop = true;
        }

        if (!drewBackdrop && TesseraGlass.UseBackdropBlur && !TesseraGlass.PreviewMode)
        {
            using var screen = TesseraScreenBackdrop.TryCapture(this, bounds);
            if (screen is not null)
                drewBackdrop = TesseraGlassDrawOperation.TryDrawImageBackdropBlur(lc, screen, round, blurRadius);
        }

        if (!drewBackdrop && TesseraGlass.UseBackdropBlur && !TesseraGlass.PreviewMode)
            drewBackdrop = TesseraGlassDrawOperation.TryDrawBackdropBlur(lc, targetCanvas, sourceSurface, rect, round, blurRadius);

        if (!drewBackdrop)
            TesseraGlassDrawOperation.DrawFallbackGlass(lc, round, w, h, tint);

        TesseraGlassDrawOperation.DrawShellTint(lc, round, tint, drewBackdrop, LightTintOnly);
        TesseraGlassDrawOperation.DrawGlassChrome(lc, round, w, h);

        _layerCache.Image = layerSurface.Snapshot();
        return _layerCache.Image;
    }
}

internal sealed class TesseraGlassDrawOperation : ICustomDrawOperation
{
    private readonly TesseraGlassBackground _owner;
    private readonly Rect _bounds;
    private readonly double _cornerRadius;
    private readonly double _blurRadius;
    private readonly Color _tint;
    private readonly int _generation;

    public TesseraGlassDrawOperation(
        TesseraGlassBackground owner,
        Rect bounds,
        double cornerRadius,
        double blurRadius,
        Color tint,
        int generation)
    {
        _owner = owner;
        _bounds = bounds;
        _cornerRadius = cornerRadius;
        _blurRadius = blurRadius;
        _tint = tint;
        _generation = generation;
    }

    public Rect Bounds => _bounds;

    public void Dispose() { }

    public bool HitTest(Point p) => false;

    public bool Equals(ICustomDrawOperation? other) =>
        other is TesseraGlassDrawOperation op
        && op._owner == _owner
        && op._bounds == _bounds
        && op._cornerRadius == _cornerRadius
        && op._blurRadius == _blurRadius
        && op._tint == _tint
        && op._generation == _generation;

    public void Render(ImmediateDrawingContext context)
    {
        var leaseFeature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
        if (leaseFeature is null)
            return;

        using var lease = leaseFeature.Lease();
        var canvas = lease.SkCanvas;
        var surface = lease.SkSurface;
        if (surface is null)
            return;

        var w = (float)_bounds.Width;
        var h = (float)_bounds.Height;
        if (w <= 0.5f || h <= 0.5f)
            return;

        var layer = _owner.EnsureLayer(canvas, surface, _bounds, _cornerRadius, _blurRadius, _tint, _generation);
        if (layer is null)
            return;

        var radius = (float)Math.Max(0, _cornerRadius);
        var round = new SKRoundRect(SKRect.Create(0, 0, w, h), radius, radius);

        using var blit = new SKPaint { IsAntialias = true, FilterQuality = SKFilterQuality.Medium };
        canvas.Save();
        canvas.ClipRoundRect(round, antialias: true);
        canvas.DrawImage(layer, 0, 0, blit);
        canvas.Restore();
    }

    internal static bool TryDrawImageBackdropBlur(
        SKCanvas dest,
        SKImage source,
        SKRoundRect round,
        double blurRadius)
    {
        var blur = (float)Math.Clamp(blurRadius, 4, 28);
        using var blurFilter = SKImageFilter.CreateBlur(blur, blur, SKShaderTileMode.Clamp);
        using var paint = new SKPaint
        {
            ImageFilter = blurFilter,
            IsAntialias = true,
            FilterQuality = SKFilterQuality.Medium
        };

        dest.Save();
        dest.ClipRoundRect(round, antialias: true);
        dest.DrawImage(source, 0, 0, paint);
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
        if (!sourceCanvas.TotalMatrix.TryInvert(out var inverse))
            return false;

        using var snapshot = sourceSurface.Snapshot();
        if (snapshot is null)
            return false;

        var blur = (float)Math.Clamp(blurRadius, 4, 24);
        using var shader = snapshot.ToShader(SKShaderTileMode.Clamp, SKShaderTileMode.Clamp, inverse);
        using var blurFilter = SKImageFilter.CreateBlur(blur, blur, SKShaderTileMode.Clamp);
        using var blurPaint = new SKPaint
        {
            Shader = shader,
            ImageFilter = blurFilter,
            IsAntialias = true,
            FilterQuality = SKFilterQuality.Medium
        };

        dest.Save();
        dest.ClipRoundRect(round, antialias: true);
        dest.DrawRect(rect, blurPaint);
        dest.Restore();
        return true;
    }

    internal static void DrawFallbackGlass(SKCanvas canvas, SKRoundRect round, int w, int h, Color tint)
    {
        var crust = TesseraPalette.Crust;
        using var basePaint = new SKPaint
        {
            Color = new SKColor(crust.R, crust.G, crust.B, 88),
            IsAntialias = true
        };
        canvas.DrawRoundRect(round, basePaint);

        using var tintWash = new SKPaint
        {
            Shader = SKShader.CreateLinearGradient(
                new SKPoint(0, h),
                new SKPoint(w, 0),
                [new SKColor(tint.R, tint.G, tint.B, 48), new SKColor(tint.R, tint.G, tint.B, 16)],
                [0f, 1f],
                SKShaderTileMode.Clamp),
            IsAntialias = true,
            BlendMode = SKBlendMode.Plus
        };
        canvas.DrawRoundRect(round, tintWash);

        using var noisePaint = new SKPaint
        {
            Shader = SKShader.CreatePerlinNoiseFractalNoise(0.85f, 0.6f, 2, 0),
            Color = new SKColor(255, 255, 255, 6),
            IsAntialias = true,
            BlendMode = SKBlendMode.Overlay
        };
        canvas.Save();
        canvas.ClipRoundRect(round, antialias: true);
        canvas.DrawRect(0, 0, w, h, noisePaint);
        canvas.Restore();
    }

    internal static void DrawShellTint(SKCanvas canvas, SKRoundRect round, Color tint, bool blurred, bool lightTintOnly = false)
    {
        var sk = TesseraGlassPanel.ToSkColor(tint);
        var alpha = blurred
            ? (byte)Math.Min(sk.Alpha, TesseraGlassPanel.BlurredTintAlphaMax)
            : (byte)Math.Min(Math.Max((int)sk.Alpha, 28), TesseraGlassPanel.FallbackTintAlphaMax);
        if (lightTintOnly)
            alpha = (byte)Math.Max(12, alpha / 2);
        sk = sk.WithAlpha(alpha);
        if (sk.Alpha == 0)
            return;

        // Light SrcOver tint — keeps blurred wallpaper hue visible.
        using var tintPaint = new SKPaint
        {
            Color = sk,
            IsAntialias = true,
            BlendMode = SKBlendMode.SrcOver
        };
        canvas.DrawRoundRect(round, tintPaint);
    }

    internal static void DrawGlassChrome(SKCanvas canvas, SKRoundRect round, int w, int h)
    {
        // Uniform edge only — no radial specular (reads as a spotlight on small panels).
        var edgeAlpha = (byte)(TesseraPalette.UseEdgeBlend ? 22 : 32);
        using var edgePaint = new SKPaint
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
