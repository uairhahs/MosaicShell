using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using MosaicShell.Core.Services;

namespace MosaicShell.Host.Tiles.Tessera;

/// <summary>
/// YourFlyouts-style track. Panel + Borders (Canvas+Shape left children un-arranged = purple “dot”).
/// </summary>
public sealed class TesseraTrack : Panel
{
    private const double M3HandleThin = 4.0;
    private const double M3HandleWide = 44.0;
    private const double M3ThumbGap = 6.0;
    private const double M3InsideCorner = 2.0;
    private const double M3StopSize = 4.0;
    private const double M3StopInset = 6.0;

    public static readonly StyledProperty<double> ValueProperty =
        AvaloniaProperty.Register<TesseraTrack, double>(nameof(Value), 0.5);

    public static readonly StyledProperty<bool> IsVerticalProperty =
        AvaloniaProperty.Register<TesseraTrack, bool>(nameof(IsVertical), true);

    public static readonly StyledProperty<bool> FatThumbProperty =
        AvaloniaProperty.Register<TesseraTrack, bool>(nameof(FatThumb));

    public static readonly StyledProperty<bool> ShowThumbProperty =
        AvaloniaProperty.Register<TesseraTrack, bool>(nameof(ShowThumb), true);

    public static readonly StyledProperty<double> TrackThicknessProperty =
        AvaloniaProperty.Register<TesseraTrack, double>(nameof(TrackThickness), 4);

    /// <summary>Inset of the track within the control. 0 = fill edge-to-edge (Amber pill).</summary>
    public static readonly StyledProperty<double> TrackPadProperty =
        AvaloniaProperty.Register<TesseraTrack, double>(nameof(TrackPad), 8);

    /// <summary>When set, vertical full-bleed fill follows a rounded-rect shell (Center card) instead of a circle.</summary>
    public static readonly StyledProperty<double> ShellRadiusProperty =
        AvaloniaProperty.Register<TesseraTrack, double>(nameof(ShellRadius), 0);

    /// <summary>Bottom corner radius when clipped to a shell (0 = square bottom, e.g. track above a label row).</summary>
    public static readonly StyledProperty<double> ShellEndRadiusProperty =
        AvaloniaProperty.Register<TesseraTrack, double>(nameof(ShellEndRadius), 0);

    public static readonly StyledProperty<IBrush?> AccentBrushOverrideProperty =
        AvaloniaProperty.Register<TesseraTrack, IBrush?>(nameof(AccentBrushOverride));

    public static readonly StyledProperty<IBrush?> TrackBackBrushOverrideProperty =
        AvaloniaProperty.Register<TesseraTrack, IBrush?>(nameof(TrackBackBrushOverride));

    /// <summary>Pixel.inc vertical slider: thin spine + wide rounded fill, no thumb.</summary>
    public static readonly StyledProperty<bool> PixelVerticalFillProperty =
        AvaloniaProperty.Register<TesseraTrack, bool>(nameof(PixelVerticalFill));

    /// <summary>M3 vertical slider: wide inactive/active track, handle bar at junction, top stop, inset icon.</summary>
    public static readonly StyledProperty<bool> ExpressiveVerticalProperty =
        AvaloniaProperty.Register<TesseraTrack, bool>(nameof(ExpressiveVertical));

    /// <summary>Translucent accent fill (Amber pill).</summary>
    public static readonly StyledProperty<bool> GlassFillProperty =
        AvaloniaProperty.Register<TesseraTrack, bool>(nameof(GlassFill));

    private readonly Border _back = new();
    private readonly Border _fill = new();
    private readonly Border _thumb = new();
    private readonly Border _stop = new();
    private bool _dragging;
    private bool _suppress;
    private DateTime _userUntil = DateTime.MinValue;

    public TesseraTrack()
    {
        Background = Brushes.Transparent;
        ClipToBounds = false;
        IsHitTestVisible = true;

        _back.CornerRadius = new CornerRadius(2);
        _back.Background = TesseraPalette.TrackBackBrush;
        _back.IsHitTestVisible = false;

        _fill.CornerRadius = new CornerRadius(2);
        _fill.Background = TesseraPalette.AccentBrush;
        _fill.IsHitTestVisible = false;

        _thumb.Width = 14;
        _thumb.Height = 14;
        _thumb.CornerRadius = new CornerRadius(7);
        _thumb.Background = TesseraPalette.FontBrush;
        _thumb.IsHitTestVisible = false;

        _stop.Width = 4;
        _stop.Height = 4;
        _stop.CornerRadius = new CornerRadius(2);
        _stop.IsHitTestVisible = false;
        _stop.IsVisible = false;

        Children.Add(_back);
        Children.Add(_fill);
        Children.Add(_stop);
        Children.Add(_thumb);

        PointerPressed += OnPressed;
        PointerMoved += OnMoved;
        PointerReleased += OnReleased;
        PointerCaptureLost += (_, _) => _dragging = false;
        PointerWheelChanged += OnWheel;
    }

    public double Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, Math.Clamp(value, 0, 1));
    }

    public bool IsVertical
    {
        get => GetValue(IsVerticalProperty);
        set => SetValue(IsVerticalProperty, value);
    }

    public bool FatThumb
    {
        get => GetValue(FatThumbProperty);
        set => SetValue(FatThumbProperty, value);
    }

    public bool ShowThumb
    {
        get => GetValue(ShowThumbProperty);
        set => SetValue(ShowThumbProperty, value);
    }

    public double TrackThickness
    {
        get => GetValue(TrackThicknessProperty);
        set => SetValue(TrackThicknessProperty, value);
    }

    public double TrackPad
    {
        get => GetValue(TrackPadProperty);
        set => SetValue(TrackPadProperty, value);
    }

    public double ShellRadius
    {
        get => GetValue(ShellRadiusProperty);
        set => SetValue(ShellRadiusProperty, value);
    }

    public double ShellEndRadius
    {
        get => GetValue(ShellEndRadiusProperty);
        set => SetValue(ShellEndRadiusProperty, value);
    }

    public IBrush? AccentBrushOverride
    {
        get => GetValue(AccentBrushOverrideProperty);
        set => SetValue(AccentBrushOverrideProperty, value);
    }

    public IBrush? TrackBackBrushOverride
    {
        get => GetValue(TrackBackBrushOverrideProperty);
        set => SetValue(TrackBackBrushOverrideProperty, value);
    }

    public bool PixelVerticalFill
    {
        get => GetValue(PixelVerticalFillProperty);
        set => SetValue(PixelVerticalFillProperty, value);
    }

    public bool ExpressiveVertical
    {
        get => GetValue(ExpressiveVerticalProperty);
        set => SetValue(ExpressiveVerticalProperty, value);
    }

    public bool GlassFill
    {
        get => GetValue(GlassFillProperty);
        set => SetValue(GlassFillProperty, value);
    }

    public event EventHandler<double>? ValueChanged;

    /// <summary>True while dragging / shortly after a wheel nudge - live pump should not fight the user.</summary>
    public bool IsUserAdjusting =>
        _dragging || DateTime.UtcNow < _userUntil;

    private void MarkUser() => _userUntil = DateTime.UtcNow.AddMilliseconds(350);

    public void SetValueSilent(double v)
    {
        if (IsUserAdjusting) return;
        var clamped = VolumePercent.Quantize(v);
        _suppress = true;
        try
        {
            if (Math.Abs(Value - clamped) >= 0.004)
                SetCurrentValue(ValueProperty, clamped);
        }
        finally
        {
            _suppress = false;
        }
        InvalidateArrange();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ValueProperty || change.Property == IsVerticalProperty
            || change.Property == FatThumbProperty || change.Property == ShowThumbProperty
            || change.Property == TrackThicknessProperty || change.Property == TrackPadProperty
            || change.Property == ShellRadiusProperty
            || change.Property == ShellEndRadiusProperty
            || change.Property == AccentBrushOverrideProperty
            || change.Property == TrackBackBrushOverrideProperty
            || change.Property == PixelVerticalFillProperty
            || change.Property == ExpressiveVerticalProperty
            || change.Property == GlassFillProperty)
        {
            InvalidateArrange();
            if (change.Property == ValueProperty && !_suppress && change.NewValue is double nv)
                ValueChanged?.Invoke(this, nv);
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (IsVertical)
        {
            var w = double.IsInfinity(availableSize.Width) ? 28 : availableSize.Width;
            if (!double.IsNaN(Width) && Width > 0) w = Width;
            else if (!ExpressiveVertical && !FatThumb && ShellRadius <= 0.5 && TrackThickness < 20)
                w = Math.Min(w, 28);

            var h = double.IsInfinity(availableSize.Height)
                ? (!double.IsNaN(Height) && Height > 0 ? Height : 140)
                : availableSize.Height;
            if (!double.IsNaN(Height) && Height > 0) h = Height;
            return new Size(w, h);
        }

        var hw = double.IsInfinity(availableSize.Width)
            ? (!double.IsNaN(Width) && Width > 0 ? Width : 200)
            : availableSize.Width;
        var hh = double.IsInfinity(availableSize.Height) ? 28 : Math.Min(availableSize.Height, 28);
        if (!double.IsNaN(Width) && Width > 0) hw = Width;
        if (!double.IsNaN(Height) && Height > 0) hh = Height;
        return new Size(hw, hh);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var pad = Math.Max(0, TrackPad);
        var thickness = Math.Max(2, TrackThickness);
        var v = Math.Clamp(Value, 0, 1);
        var w = finalSize.Width;
        var h = finalSize.Height;
        var thumb = FatThumb ? 22.0 : 14.0;
        if (FatThumb && IsVertical) thumb = Math.Max(28, w - 4);

        _back.Background = TrackBackBrushOverride ?? TesseraPalette.TrackBackBrush;
        ApplyFillBrush();
        if (!ExpressiveVertical && !(GlassFill && ShowThumb))
        {
            _thumb.Background = FatThumb
                ? (AccentBrushOverride is SolidColorBrush scb
                    ? new SolidColorBrush(scb.Color)
                    : new SolidColorBrush(Color.FromRgb(235, 235, 240)))
                : TesseraPalette.FontBrush;
            _thumb.IsVisible = ShowThumb;
            _thumb.Width = FatThumb && IsVertical ? Math.Max(18, w - 6) : thumb;
            _thumb.Height = FatThumb && IsVertical ? Math.Max(36, h * 0.22) : thumb;
            _thumb.CornerRadius = new CornerRadius(FatThumb ? 10 : thumb / 2);
        }

        if (IsVertical)
        {
            if (!ExpressiveVertical)
                Clip = null;

            if (!ExpressiveVertical)
                _stop.IsVisible = false;

            if (!FatThumb && ShellRadius > 0.5 && thickness >= w * 0.85)
            {
                ClipToBounds = true;
                var r = ShellRadius;
                var trackLen = Math.Max(1, h - pad * 2);
                var accent = AccentBrushOverride ?? TesseraPalette.AccentBrush;
                var useM3Handle = GlassFill && ShowThumb;
                var fillH = Math.Max(v > 0.001 ? (useM3Handle ? M3HandleThin : 2) : 0, trackLen * v);
                var fillTop = pad + trackLen - fillH;

                _back.IsVisible = false;
                ApplyFillBrush();

                var bottomR = Math.Min(r, Math.Max(0, fillH / 2));
                var topR = fillH >= trackLen - 1 ? r : (useM3Handle ? M3InsideCorner : 0);
                var fillCorner = new CornerRadius(topR, topR, bottomR, bottomR);
                _fill.CornerRadius = fillCorner;
                var fillRect = new Rect(0, fillTop, w, fillH);
                _fill.Arrange(fillRect);

                if (useM3Handle)
                {
                    ArrangeM3VerticalHandle(w, pad, fillTop, fillH, v, accent, w);
                    ArrangeM3Stop(w, pad, v, accent);
                }
                else
                {
                    var thumbY = Math.Clamp(fillTop - thumb / 2, 0, Math.Max(0, h - thumb));
                    _thumb.Arrange(new Rect(Math.Max(0, w / 2 - thumb / 2), thumbY, thumb, thumb));
                }
            }
            else if (ExpressiveVertical)
            {
                _back.IsVisible = true;
                _fill.IsVisible = true;

                var shellTopR = Math.Max(0, ShellRadius);
                var shellBottomR = Math.Max(0, ShellEndRadius);
                var shellClip = shellTopR > 0.5 || shellBottomR > 0.5;
                const double bedInsetX = 4.0;
                var insetY = shellClip ? 0 : Math.Max(6, TrackPad);
                var bedW = shellClip
                    ? Math.Max(12, w - bedInsetX * 2)
                    : TrackThickness > 20
                        ? Math.Min(w - Math.Max(4, TrackPad) * 2, TrackThickness)
                        : Math.Max(12, w - Math.Max(4, TrackPad) * 2);
                var bedX = (w - bedW) / 2;
                var trackLen = Math.Max(1, h - insetY * 2);
                var fillH = Math.Max(v > 0.001 ? M3HandleThin : 0, trackLen * v);
                var fillTop = insetY + trackLen - fillH;
                var accent = AccentBrushOverride ?? TesseraPalette.AccentBrush;

                var backTopR = shellClip ? Math.Max(0, shellTopR - bedInsetX) : bedW / 2.0;
                var backBottomR = shellClip ? shellBottomR : bedW / 2.0;
                _back.CornerRadius = new CornerRadius(backTopR, backTopR, backBottomR, backBottomR);
                _back.Arrange(new Rect(bedX, insetY, bedW, trackLen));

                var fillTopR = fillH >= trackLen - 1 ? backTopR : M3InsideCorner;
                var fillBottomR = backBottomR;
                _fill.CornerRadius = new CornerRadius(fillTopR, fillTopR, fillBottomR, fillBottomR);
                _fill.Arrange(new Rect(bedX, fillTop, bedW, fillH));

                ArrangeM3VerticalHandle(w, insetY, fillTop, fillH, v, accent, bedW);
                ArrangeM3Stop(w, insetY, v, accent);

                ClipToBounds = false;
                Clip = null;
            }
            else if (PixelVerticalFill)
            {
                ClipToBounds = true;
                _back.IsVisible = true;
                _thumb.IsVisible = false;
                var spine = Math.Max(2, TrackThickness);
                var fillW = Math.Max(10, w - pad * 2);
                var fillX = (w - fillW) / 2;
                var spineX = (w - spine) / 2;
                var trackLen = Math.Max(1, h - pad * 2);
                var fillH = Math.Max(v > 0.001 ? 6 : 0, trackLen * v);
                var fillTop = pad + trackLen - fillH;

                _back.CornerRadius = new CornerRadius(spine / 2);
                _fill.CornerRadius = new CornerRadius(fillW / 2);
                _back.Arrange(new Rect(spineX, pad, spine, trackLen));
                _fill.Arrange(new Rect(fillX, fillTop, fillW, fillH));
            }
            else if (FatThumb)
            {
                var trackLen = Math.Max(1, h - pad * 2);
                // Pill fill from bottom (legacy fat thumb)
                _back.CornerRadius = new CornerRadius(w / 2);
                _fill.CornerRadius = new CornerRadius(w / 2);
                _back.Arrange(new Rect(2, pad, Math.Max(1, w - 4), trackLen));
                var fillH = Math.Max(v > 0.001 ? 8 : 0, trackLen * v);
                _fill.Arrange(new Rect(2, pad + trackLen - fillH, Math.Max(1, w - 4), fillH));
                var th = _thumb.Height;
                var ty = Math.Clamp(pad + trackLen - fillH - th * 0.35, pad, h - th - pad);
                _thumb.Arrange(new Rect((w - _thumb.Width) / 2, ty, _thumb.Width, th));
            }
            else
            {
                ClipToBounds = false;
                _back.IsVisible = true;
                var x = Math.Max(0, (w - thickness) / 2);
                var trackLen = Math.Max(1, h - pad * 2);
                var fillLen = Math.Max(0, trackLen * v);
                var fillTop = pad + trackLen - fillLen;

                // Edge-to-edge (Amber): radius matches full pill when thickness ≈ width
                var radius = pad <= 0.5 ? Math.Min(w, thickness) / 2 : thickness / 2;
                _back.CornerRadius = new CornerRadius(radius);
                _fill.CornerRadius = new CornerRadius(radius);
                _back.Arrange(new Rect(x, pad, thickness, trackLen));
                var fillRect = new Rect(x, fillTop, thickness, Math.Max(fillLen > 0 ? fillLen : 0, v > 0.001 ? 2 : 0));
                _fill.Arrange(fillRect);
                var thumbY = Math.Clamp(fillTop - thumb / 2, 0, Math.Max(0, h - thumb));
                _thumb.Arrange(new Rect(Math.Max(0, w / 2 - thumb / 2), thumbY, thumb, thumb));
            }
        }
        else
        {
            var y = Math.Max(0, (h - thickness) / 2);
            var trackLen = Math.Max(1, w - pad * 2);
            var fillLen = Math.Max(0, trackLen * v);
            _back.CornerRadius = new CornerRadius(thickness / 2);
            _fill.CornerRadius = new CornerRadius(thickness / 2);
            _back.Arrange(new Rect(pad, y, trackLen, thickness));
            _fill.Arrange(new Rect(pad, y, Math.Max(fillLen > 0 ? fillLen : 0, v > 0.001 ? 2 : 0), thickness));
            var thumbX = Math.Clamp(pad + fillLen - thumb / 2, 0, Math.Max(0, w - thumb));
            _thumb.Arrange(new Rect(thumbX, Math.Max(0, h / 2 - thumb / 2), thumb, thumb));
        }

        return finalSize;
    }

    private void OnPressed(object? sender, PointerPressedEventArgs e)
    {
        _dragging = true;
        MarkUser();
        e.Pointer.Capture(this);
        ApplyPointer(e.GetPosition(this));
        e.Handled = true;
    }

    private void OnMoved(object? sender, PointerEventArgs e)
    {
        if (!_dragging) return;
        MarkUser();
        ApplyPointer(e.GetPosition(this));
        e.Handled = true;
    }

    private void OnReleased(object? sender, PointerReleasedEventArgs e)
    {
        _dragging = false;
        MarkUser();
        e.Pointer.Capture(null);
    }

    private void OnWheel(object? sender, PointerWheelEventArgs e)
    {
        MarkUser();
        // 2% per notch - same as typical Windows volume keys
        Value = VolumePercent.Step(Value, e.Delta.Y > 0 ? 2 : -2);
        e.Handled = true;
    }

    private void ApplyPointer(Point p)
    {
        var pad = Math.Max(0, TrackPad);
        double raw;
        if (IsVertical)
        {
            var len = Math.Max(1, Bounds.Height - pad * 2);
            raw = Math.Clamp((Bounds.Height - pad - p.Y) / len, 0, 1);
        }
        else
        {
            var len = Math.Max(1, Bounds.Width - pad * 2);
            raw = Math.Clamp((p.X - pad) / len, 0, 1);
        }
        Value = VolumePercent.Quantize(raw);
    }

    private void ApplyFillBrush()
    {
        var accent = AccentBrushOverride ?? TesseraPalette.AccentBrush;
        _fill.Background = GlassFill ? MakeGlassFillBrush(accent) : accent;
    }

    private static IBrush MakeGlassFillBrush(IBrush accent)
    {
        if (accent is SolidColorBrush scb)
        {
            var c = scb.Color;
            return new SolidColorBrush(Color.FromArgb(112, c.R, c.G, c.B));
        }
        return accent;
    }

    private void ArrangeM3VerticalHandle(
        double w, double insetY, double fillTop, double fillH, double v, IBrush accent, double bedW)
    {
        if (ShowThumb && fillH >= M3HandleThin && v < 0.999)
        {
            var handleW = Math.Min(w, Math.Max(M3HandleWide, bedW + 12));
            var handleY = fillTop - M3ThumbGap - M3HandleThin;
            if (handleY >= insetY - 0.5)
            {
                _thumb.IsVisible = true;
                _thumb.Width = handleW;
                _thumb.Height = M3HandleThin;
                _thumb.CornerRadius = new CornerRadius(M3HandleThin / 2);
                _thumb.Background = accent;
                _thumb.Arrange(new Rect((w - handleW) / 2, handleY, handleW, M3HandleThin));
                return;
            }
        }

        _thumb.IsVisible = false;
    }

    private void ArrangeM3Stop(double w, double insetY, double v, IBrush accent)
    {
        if (v < 0.999)
        {
            _stop.IsVisible = true;
            _stop.Width = M3StopSize;
            _stop.Height = M3StopSize;
            _stop.CornerRadius = new CornerRadius(M3StopSize / 2);
            _stop.Background = accent;
            _stop.Arrange(new Rect(
                w / 2 - M3StopSize / 2,
                insetY + M3StopInset,
                M3StopSize,
                M3StopSize));
        }
        else
        {
            _stop.IsVisible = false;
        }
    }
}
