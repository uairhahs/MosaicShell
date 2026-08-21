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

    private readonly Border _back = new();
    private readonly Border _fill = new();
    private readonly Border _thumb = new();
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

        Children.Add(_back);
        Children.Add(_fill);
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

    public event EventHandler<double>? ValueChanged;

    /// <summary>True while dragging / shortly after a wheel nudge — live pump should not fight the user.</summary>
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
            || change.Property == TrackThicknessProperty || change.Property == TrackPadProperty)
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
            var w = double.IsInfinity(availableSize.Width) ? 28 : Math.Min(availableSize.Width, 28);
            var h = double.IsInfinity(availableSize.Height)
                ? (!double.IsNaN(Height) && Height > 0 ? Height : 140)
                : availableSize.Height;
            if (!double.IsNaN(Height) && Height > 0) h = Height;
            if (!double.IsNaN(Width) && Width > 0) w = Width;
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

        _back.Background = TesseraPalette.TrackBackBrush;
        _fill.Background = TesseraPalette.AccentBrush;
        _thumb.Background = FatThumb ? new SolidColorBrush(Color.FromRgb(235, 235, 240)) : TesseraPalette.FontBrush;
        _thumb.IsVisible = ShowThumb;
        _thumb.Width = FatThumb && IsVertical ? Math.Max(18, w - 6) : thumb;
        _thumb.Height = FatThumb && IsVertical ? Math.Max(36, h * 0.22) : thumb;
        _thumb.CornerRadius = new CornerRadius(FatThumb ? 10 : thumb / 2);

        if (IsVertical)
        {
            var x = Math.Max(0, (w - thickness) / 2);
            var trackLen = Math.Max(1, h - pad * 2);
            var fillLen = Math.Max(0, trackLen * v);
            var fillTop = pad + trackLen - fillLen;

            if (FatThumb)
            {
                // Pill fill from bottom (Pixel style)
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
                // Edge-to-edge (Amber): radius matches full pill when thickness ≈ width
                var radius = pad <= 0.5 ? Math.Min(w, thickness) / 2 : thickness / 2;
                _back.CornerRadius = new CornerRadius(radius);
                _fill.CornerRadius = new CornerRadius(radius);
                _back.Arrange(new Rect(x, pad, thickness, trackLen));
                _fill.Arrange(new Rect(x, fillTop, thickness, Math.Max(fillLen > 0 ? fillLen : 0, v > 0.001 ? 2 : 0)));
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
        // 2% per notch — same as typical Windows volume keys
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
}
