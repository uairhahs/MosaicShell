using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using MosaicShell.Core.Services;

namespace MosaicShell.Host.Tiles.Tessera;

/// <summary>Shared high-fidelity chrome pieces for Tessera style layouts.</summary>
internal static class TesseraChrome
{
    // Catppuccin Mocha crust #11111b - translucent (ClipToBounds off on stroke shell so outline reaches corners)
    public static IBrush DarkSolid => new SolidColorBrush(TesseraPalette.PrimarySolid);
    public static IBrush SoftStroke => new SolidColorBrush(Color.FromArgb(
        (byte)(TesseraPalette.UseEdgeBlend ? 55 : 80), 255, 255, 255));
    public static IBrush ArtDim => new SolidColorBrush(Color.FromArgb(
        (byte)Math.Clamp(TesseraPalette.ShellAlpha - 20, 80, 200), 0x11, 0x11, 0x1b));
    public static IBrush TileFace => new SolidColorBrush(Color.FromArgb(
        (byte)Math.Clamp(TesseraPalette.ShellAlpha + 10, 100, 230), 0x11, 0x11, 0x1b));
    public static IBrush TileFaceHi => new SolidColorBrush(Color.FromArgb(
        (byte)Math.Clamp(TesseraPalette.ShellAlpha + 25, 120, 240), 0x18, 0x18, 0x25));

    /// <summary>Skia glass shell (backdrop blur + tint + edge).</summary>
    public static Control Glass(Control child, double radius, Thickness? pad = null, double? w = null, double? h = null) =>
        TesseraGlassPanel.Wrap(child, radius, pad, w, h, tint: TesseraPalette.Primary);

    /// <summary>Opaque style pill (Pixel columns) — no frost wash.</summary>
    public static Border SolidPill(
        Control child,
        IBrush fill,
        double radius,
        double w,
        double? h = null,
        Thickness? pad = null)
    {
        return new Border
        {
            Width = w,
            Height = h ?? double.NaN,
            MinWidth = w,
            MaxWidth = w,
            MinHeight = h ?? double.NaN,
            MaxHeight = h ?? double.NaN,
            Background = fill,
            CornerRadius = new CornerRadius(radius),
            ClipToBounds = true,
            Padding = pad ?? new Thickness(0),
            Child = child
        };
    }

    public static Control GlassTinted(
        Control child,
        double radius,
        IBrush background,
        Thickness? pad = null,
        double? w = null,
        double? h = null)
    {
        var tint = background is SolidColorBrush scb ? scb.Color : TesseraPalette.Primary;
        return TesseraGlassPanel.Wrap(child, radius, pad, w, h, tint: tint);
    }

    /// <summary>Frosted wash: translucent shell + soft art under solid tint (no OS acrylic).</summary>
    public static Control WithArtWash(Control foreground, byte[]? png, double radius, Thickness pad, double? maxWidth = null, double? maxHeight = null)
    {
        var root = new Grid();
        var wash = new Border
        {
            Name = "TesseraMediaWash",
            Background = Brushes.Transparent,
            IsHitTestVisible = false,
            Opacity = 0.55
        };
        TesseraMediaPanel.ApplyArtToBorder(wash, png, fillHost: true);
        root.Children.Add(wash);
        root.Children.Add(new Border { Background = ArtDim, IsHitTestVisible = false });
        root.Children.Add(new Border { Padding = pad, Child = foreground });
        return TesseraGlassPanel.Wrap(root, radius, maxWidth: maxWidth, maxHeight: maxHeight, tint: TesseraPalette.Primary);
    }

    /// <summary>Tile whose face is album art (CoreUI bottom-left) - art reads clearly with a readable dim.</summary>
    public static Control ArtTile(Control foreground, byte[]? png, double radius, Thickness pad, double height = 72) =>
        ArtTile(foreground, png, radius, pad, height, out _);

    public static Control ArtTile(
        Control foreground, byte[]? png, double radius, Thickness pad, double height, out Border artHost)
    {
        var root = new Grid();
        artHost = new Border
        {
            Name = "TesseraMediaArt",
            Background = TileFaceHi,
            ClipToBounds = true,
            IsHitTestVisible = false
        };
        TesseraMediaPanel.ApplyArtToBorder(artHost, png, fillHost: true);
        root.Children.Add(artHost);
        root.Children.Add(new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(140, 0x11, 0x11, 0x1b)),
            IsHitTestVisible = false
        });
        root.Children.Add(new Border
        {
            Padding = pad,
            VerticalAlignment = VerticalAlignment.Center,
            Child = foreground
        });
        var tileTint = Color.FromArgb(
            (byte)Math.Clamp(TesseraPalette.ShellAlpha + 10, 100, 230), 0x11, 0x11, 0x1b);
        return TesseraGlassPanel.Wrap(root, radius, height: height, tint: tileTint);
    }

    public static string SlashFill(double value, int segments = 20)
    {
        var n = Math.Clamp((int)Math.Round(Math.Clamp(value, 0, 1) * segments), 0, segments);
        return new string('/', n) + new string('·', segments - n);
    }

    public static Control SlashMeter(double value, int segments, double fontSize, out TextBlock live)
    {
        live = new TextBlock
        {
            Text = SlashFill(value, segments),
            FontFamily = new FontFamily("Cascadia Mono, Consolas, Courier New"),
            FontSize = fontSize,
            Foreground = TesseraPalette.AccentBrush,
            Name = "TesseraSlashMeter"
        };
        // Two-tone: rebuild as Run is awkward; color whole line accent, inactive via mid-dot
        return live;
    }

    public static TextBlock Mono(string text, double size, bool muted = false) =>
        new()
        {
            Text = text,
            FontFamily = new FontFamily("Cascadia Mono, Consolas, Courier New"),
            FontSize = size,
            Foreground = muted ? TesseraPalette.FontMutedBrush : TesseraPalette.FontBrush
        };

    public static TextBlock Label(string text, double size, FontWeight weight = FontWeight.Normal, bool muted = false) =>
        new()
        {
            Text = text,
            FontSize = size,
            FontWeight = weight,
            FontFamily = new FontFamily("Segoe UI Variable, Segoe UI"),
            Foreground = muted ? TesseraPalette.FontMutedBrush : TesseraPalette.FontBrush,
            TextTrimming = TextTrimming.CharacterEllipsis
        };

    public static void ApplyHoverHighlight(Border border, IBrush normal, IBrush hover)
    {
        border.Background = normal;
        border.PointerEntered += (_, _) => border.Background = hover;
        border.PointerExited += (_, _) => border.Background = normal;
    }

    /// <summary>Transport glyph with hover — circular for icons, rounded rect for play/pause.</summary>
    public static Border IconButton(Control child, Action act, double size, bool circularHighlight = true, IBrush? hover = null)
    {
        var corner = circularHighlight ? size / 2 : Math.Min(10, size * 0.36);
        var b = new Border
        {
            Width = size,
            Height = size,
            CornerRadius = new CornerRadius(corner),
            ClipToBounds = true,
            Background = Brushes.Transparent,
            Child = child
        };
        ApplyHoverHighlight(b, Brushes.Transparent, hover ?? TileFaceHi);
        b.PointerPressed += (_, e) => { act(); e.Handled = true; };
        return b;
    }
}

/// <summary>Circular volume ring (Smouti / ref12).</summary>
internal static class TesseraVolumeLabel
{
    public static string Volume(bool muted, int pct, bool plainextSpeakers) =>
        muted ? "Mute" : plainextSpeakers ? $"Speakers: {pct}%" : $"{pct}%";

    public static string Brightness(int pct, bool plainext) =>
        plainext ? $"Brightness: {pct}%" : $"{pct}%";
}

public sealed class TesseraRingVolume : Panel
{
    public static readonly StyledProperty<double> ValueProperty =
        AvaloniaProperty.Register<TesseraRingVolume, double>(nameof(Value), 0.5);

    public static readonly StyledProperty<IBrush?> AccentBrushOverrideProperty =
        AvaloniaProperty.Register<TesseraRingVolume, IBrush?>(nameof(AccentBrushOverride));

    public static readonly StyledProperty<IBrush?> PercentBrushOverrideProperty =
        AvaloniaProperty.Register<TesseraRingVolume, IBrush?>(nameof(PercentBrushOverride));

    /// <summary>Hero ring: glass disc, glow bed, thick arc — Smouti centerpiece.</summary>
    public static readonly StyledProperty<bool> ShowcaseProperty =
        AvaloniaProperty.Register<TesseraRingVolume, bool>(nameof(Showcase));

    private readonly Border _glass = new() { IsHitTestVisible = false, IsVisible = false };
    private readonly Arc _glow = new() { IsHitTestVisible = false, IsVisible = false };
    private readonly Arc _back = new() { StrokeThickness = 8, Stroke = TesseraPalette.TrackBackBrush, IsHitTestVisible = false };
    private readonly Arc _fill = new() { StrokeThickness = 8, Stroke = TesseraPalette.AccentBrush, IsHitTestVisible = false };
    private readonly TextBlock _pct = new()
    {
        FontSize = 16,
        FontWeight = FontWeight.Bold,
        Foreground = TesseraPalette.FontBrush,
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center,
        Name = "TesseraPercent"
    };
    private bool _dragging;
    private bool _suppress;
    private bool _hovered;
    private DateTime _userUntil = DateTime.MinValue;

    public TesseraRingVolume()
    {
        Width = 64;
        Height = 64;
        MinWidth = 64;
        MinHeight = 64;
        MaxWidth = 64;
        MaxHeight = 64;
        HorizontalAlignment = HorizontalAlignment.Left;
        VerticalAlignment = VerticalAlignment.Top;
        Children.Add(_glass);
        Children.Add(_glow);
        Children.Add(_back);
        Children.Add(_fill);
        Children.Add(_pct);
        PointerEntered += (_, _) => { _hovered = true; SyncChrome(); };
        PointerExited += (_, _) => { _hovered = false; SyncChrome(); };
        PointerPressed += (_, e) => { _dragging = true; Mark(); e.Pointer.Capture(this); Apply(e.GetPosition(this)); SyncChrome(); e.Handled = true; };
        PointerMoved += (_, e) => { if (!_dragging) return; Mark(); Apply(e.GetPosition(this)); SyncChrome(); e.Handled = true; };
        PointerReleased += (_, e) => { _dragging = false; Mark(); SyncChrome(); e.Pointer.Capture(null); };
        PointerWheelChanged += (_, e) =>
        {
            Mark();
            Value = VolumePercent.Step(Value, e.Delta.Y > 0 ? 2 : -2);
            SyncChrome();
            InvalidateArrange();
            e.Handled = true;
        };
        SyncLabel();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        // Critical: default Panel measures to infinity and blows Smouti to full display height
        var w = !double.IsNaN(Width) && Width > 0 ? Width : 64;
        var h = !double.IsNaN(Height) && Height > 0 ? Height : 64;
        return new Size(w, h);
    }

    public double Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, Math.Clamp(value, 0, 1));
    }

    public IBrush? AccentBrushOverride
    {
        get => GetValue(AccentBrushOverrideProperty);
        set => SetValue(AccentBrushOverrideProperty, value);
    }

    public IBrush? PercentBrushOverride
    {
        get => GetValue(PercentBrushOverrideProperty);
        set => SetValue(PercentBrushOverrideProperty, value);
    }

    public bool Showcase
    {
        get => GetValue(ShowcaseProperty);
        set => SetValue(ShowcaseProperty, value);
    }

    public event EventHandler<double>? ValueChanged;
    public bool IsUserAdjusting => _dragging || DateTime.UtcNow < _userUntil;
    public TextBlock PercentLabel => _pct;

    private void Mark() => _userUntil = DateTime.UtcNow.AddMilliseconds(350);

    public void SetValueSilent(double v)
    {
        if (IsUserAdjusting) return;
        _suppress = true;
        try { Value = VolumePercent.Quantize(v); }
        finally { _suppress = false; }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ValueProperty)
        {
            SyncLabel();
            InvalidateArrange();
            if (!_suppress && change.NewValue is double nv)
                ValueChanged?.Invoke(this, nv);
            return;
        }

        if (change.Property == AccentBrushOverrideProperty || change.Property == PercentBrushOverrideProperty
            || change.Property == ShowcaseProperty)
        {
            ClipToBounds = Showcase;
            ApplyStyleBrushes();
            SyncChrome();
            InvalidateArrange();
        }
    }

    private void ApplyStyleBrushes()
    {
        var accent = AccentBrushOverride ?? TesseraPalette.AccentBrush;
        _fill.Stroke = accent;
        _pct.Foreground = PercentBrushOverride ?? TesseraPalette.FontBrush;
    }

    private void SyncChrome()
    {
        if (!Showcase)
        {
            _glass.IsVisible = false;
            _glow.IsVisible = false;
            _pct.FontSize = 16;
            return;
        }

        _glass.IsVisible = true;
        _glow.IsVisible = true;
        _pct.FontSize = 22;

        var accent = AccentBrushOverride ?? TesseraPalette.AccentBrush;
        var accentColor = accent is SolidColorBrush scb ? scb.Color : Colors.Cyan;
        var lit = _hovered || _dragging || IsUserAdjusting;
        _glass.Background = new SolidColorBrush(Color.FromArgb((byte)(lit ? 78 : 52), (byte)255, (byte)255, (byte)255));
        _glass.BorderBrush = new SolidColorBrush(Color.FromArgb(
            (byte)(lit ? 110 : 70), accentColor.R, accentColor.G, accentColor.B));
        _glass.BorderThickness = new Thickness(lit ? 2 : 1.5);
        _glow.Stroke = new SolidColorBrush(Color.FromArgb(
            (byte)(lit ? 55 : 32), accentColor.R, accentColor.G, accentColor.B));
    }

    private void SyncLabel()
    {
        ApplyStyleBrushes();
        SyncChrome();
        _pct.Text = $"{VolumePercent.ToPercent(Value)}%";
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var s = Math.Min(finalSize.Width, finalSize.Height);
        var showcase = Showcase;
        var stroke = showcase ? Math.Max(11, s * 0.105) : 8;
        var pad = showcase ? s * 0.075 : 6.0;
        var rect = new Rect(pad, pad, s - pad * 2, s - pad * 2);

        if (showcase)
        {
            _glass.CornerRadius = new CornerRadius(s / 2);
            _glass.Arrange(new Rect(0, 0, s, s));
            _glow.StrokeThickness = stroke + 4;
            _glow.StartAngle = -90;
            _glow.SweepAngle = 360;
            _glow.Arrange(rect);
            _back.Stroke = new SolidColorBrush(Color.FromArgb(72, 255, 255, 255));
        }
        else
        {
            _back.Stroke = TesseraPalette.TrackBackBrush;
        }

        _back.StrokeThickness = stroke;
        _fill.StrokeThickness = stroke + (showcase && (_hovered || _dragging) ? 1.5 : 0);
        _back.StartAngle = -90;
        _back.SweepAngle = 360;
        _back.Arrange(rect);
        _fill.StartAngle = -90;
        _fill.SweepAngle = 360 * Math.Clamp(Value, 0, 1);
        _fill.Arrange(rect);
        _pct.Arrange(new Rect(0, 0, finalSize.Width, finalSize.Height));
        return finalSize;
    }

    private void Apply(Point p)
    {
        var cx = Bounds.Width / 2;
        var cy = Bounds.Height / 2;
        var ang = Math.Atan2(p.Y - cy, p.X - cx); // -pi..pi, 0 = east
        // Convert so -90° (north) = 0
        var deg = ang * 180 / Math.PI + 90;
        if (deg < 0) deg += 360;
        Value = VolumePercent.Quantize(deg / 360.0);
    }
}

/// <summary>Arc shape used by <see cref="TesseraRingVolume"/>.</summary>
internal sealed class Arc : Control
{
    public double StartAngle { get; set; }
    public double SweepAngle { get; set; }
    public double StrokeThickness { get; set; } = 8;
    public IBrush? Stroke { get; set; }

    public override void Render(DrawingContext context)
    {
        if (Stroke is null || Bounds.Width < 2 || Math.Abs(SweepAngle) < 0.1) return;
        var pen = new Pen(Stroke, StrokeThickness) { LineCap = PenLineCap.Round };
        var r = new Rect(
            StrokeThickness / 2,
            StrokeThickness / 2,
            Math.Max(1, Bounds.Width - StrokeThickness),
            Math.Max(1, Bounds.Height - StrokeThickness));
        var geo = new StreamGeometry();
        using (var ctx = geo.Open())
        {
            var start = PointOn(r, StartAngle);
            ctx.BeginFigure(start, false);
            // Approximate arc with line segments
            var steps = Math.Max(8, (int)(Math.Abs(SweepAngle) / 4));
            for (var i = 1; i <= steps; i++)
            {
                var a = StartAngle + SweepAngle * i / steps;
                ctx.LineTo(PointOn(r, a));
            }
        }
        context.DrawGeometry(null, pen, geo);
    }

    private static Point PointOn(Rect r, double deg)
    {
        var rad = deg * Math.PI / 180;
        var cx = r.X + r.Width / 2;
        var cy = r.Y + r.Height / 2;
        return new Point(cx + Math.Cos(rad) * r.Width / 2, cy + Math.Sin(rad) * r.Height / 2);
    }
}
