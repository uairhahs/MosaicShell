using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;

namespace MosaicShell.Host.Tiles.Tessera;

/// <summary>YourFlyouts-style Shape track: 4px line + ellipse thumb + fat hit area.</summary>
public sealed class TesseraTrack : Canvas
{
    public static readonly StyledProperty<double> ValueProperty =
        AvaloniaProperty.Register<TesseraTrack, double>(nameof(Value), 0.5);

    public static readonly StyledProperty<bool> IsVerticalProperty =
        AvaloniaProperty.Register<TesseraTrack, bool>(nameof(IsVertical), true);

    private readonly Line _back = new();
    private readonly Line _fill = new();
    private readonly Ellipse _thumb = new();
    private readonly Line _hit = new();
    private bool _dragging;
    private bool _suppress;

    public TesseraTrack()
    {
        Background = Brushes.Transparent;
        Children.Add(_back);
        Children.Add(_fill);
        Children.Add(_thumb);
        Children.Add(_hit);

        _back.StrokeThickness = 4;
        _back.Stroke = TesseraPalette.TrackBackBrush;
        _back.StrokeLineCap = PenLineCap.Round;

        _fill.StrokeThickness = 4;
        _fill.Stroke = TesseraPalette.AccentBrush;
        _fill.StrokeLineCap = PenLineCap.Round;

        _thumb.Width = 16;
        _thumb.Height = 16;
        _thumb.Fill = TesseraPalette.FontBrush;

        _hit.StrokeThickness = 20;
        _hit.Stroke = Brushes.Transparent;

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

    public event EventHandler<double>? ValueChanged;

    /// <summary>Set value without raising ValueChanged (OS-driven refresh).</summary>
    public void SetValueSilent(double v)
    {
        _suppress = true;
        Value = v;
        _suppress = false;
        InvalidateVisual();
        LayoutTrack();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ValueProperty || change.Property == IsVerticalProperty ||
            change.Property == BoundsProperty || change.Property == WidthProperty || change.Property == HeightProperty)
        {
            LayoutTrack();
            if (change.Property == ValueProperty && !_suppress && change.NewValue is double nv)
                ValueChanged?.Invoke(this, nv);
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (IsVertical)
            return new Size(
                double.IsInfinity(availableSize.Width) ? 28 : Math.Min(availableSize.Width, 28),
                double.IsInfinity(availableSize.Height) ? 140 : availableSize.Height);
        return new Size(
            double.IsInfinity(availableSize.Width) ? 200 : availableSize.Width,
            double.IsInfinity(availableSize.Height) ? 28 : Math.Min(availableSize.Height, 28));
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        LayoutTrack(finalSize);
        return finalSize;
    }

    private void LayoutTrack(Size? size = null)
    {
        // Prefer arranged/explicit size — Bounds can lag right after SetValueSilent
        var w = size?.Width ?? (Bounds.Width > 1 ? Bounds.Width : (!double.IsNaN(Width) ? Width : 0));
        var h = size?.Height ?? (Bounds.Height > 1 ? Bounds.Height : (!double.IsNaN(Height) ? Height : 0));
        if (w < 1 || h < 1) return;

        const double pad = 10;
        var v = Math.Clamp(Value, 0, 1);

        if (IsVertical)
        {
            var x = w / 2;
            var y0 = pad;
            var y1 = h - pad;
            var len = Math.Max(1, y1 - y0);
            // YourFlyouts: fill from bottom — low volume near bottom
            var thumbY = y1 - len * v;

            SetLine(_back, x, y0, x, y1);
            SetLine(_fill, x, thumbY, x, y1);
            SetLine(_hit, x, y0, x, y1);
            SetLeft(_thumb, x - 8);
            SetTop(_thumb, thumbY - 8);
        }
        else
        {
            var y = h / 2;
            var x0 = pad;
            var x1 = w - pad;
            var len = Math.Max(1, x1 - x0);
            var thumbX = x0 + len * v;

            SetLine(_back, x0, y, x1, y);
            SetLine(_fill, x0, y, thumbX, y);
            SetLine(_hit, x0, y, x1, y);
            SetLeft(_thumb, thumbX - 8);
            SetTop(_thumb, y - 8);
        }

        _fill.Stroke = TesseraPalette.AccentBrush;
        _thumb.Fill = TesseraPalette.FontBrush;
    }

    private static void SetLine(Line line, double x1, double y1, double x2, double y2)
    {
        line.StartPoint = new Point(x1, y1);
        line.EndPoint = new Point(x2, y2);
    }

    private void OnPressed(object? sender, PointerPressedEventArgs e)
    {
        _dragging = true;
        e.Pointer.Capture(this);
        ApplyPointer(e.GetPosition(this));
        e.Handled = true;
    }

    private void OnMoved(object? sender, PointerEventArgs e)
    {
        if (!_dragging) return;
        ApplyPointer(e.GetPosition(this));
        e.Handled = true;
    }

    private void OnReleased(object? sender, PointerReleasedEventArgs e)
    {
        _dragging = false;
        e.Pointer.Capture(null);
    }

    private void OnWheel(object? sender, PointerWheelEventArgs e)
    {
        Value = Math.Clamp(Value + (e.Delta.Y > 0 ? 0.02 : -0.02), 0, 1);
        e.Handled = true;
    }

    private void ApplyPointer(Point p)
    {
        const double pad = 10;
        if (IsVertical)
        {
            var len = Math.Max(1, Bounds.Height - pad * 2);
            var fromBottom = Bounds.Height - pad - p.Y;
            Value = Math.Clamp(fromBottom / len, 0, 1);
        }
        else
        {
            var len = Math.Max(1, Bounds.Width - pad * 2);
            Value = Math.Clamp((p.X - pad) / len, 0, 1);
        }
    }
}
