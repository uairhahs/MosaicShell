using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using MosaicShell.Core.Runtime;
using MosaicShell.Core.Services;
using MosaicShell.Core.Settings;

namespace MosaicShell.Host.Tiles.Surfaces;

public sealed class CanvasTileView : UserControl
{
    private readonly ISystemMetricsService _metrics;
    private readonly CanvasSettings _settings;
    private readonly TextBlock _cpu = Val();
    private readonly TextBlock _ram = Val();
    private readonly TextBlock _disk = Val();
    private readonly TextBlock _host = Val();
    private readonly DispatcherTimer _timer;

    public CanvasTileView(ISystemMetricsService metrics)
    {
        _metrics = metrics;
        _settings = ModuleSettingsStore.Load("Canvas", () => new CanvasSettings());
        var stack = new StackPanel { Spacing = 8 };
        if (_settings.ShowHost) { stack.Children.Add(Label("HOST")); stack.Children.Add(_host); }
        if (_settings.ShowCpu) { stack.Children.Add(Label("CPU")); stack.Children.Add(_cpu); }
        if (_settings.ShowRam) { stack.Children.Add(Label("MEMORY")); stack.Children.Add(_ram); }
        if (_settings.ShowDisk) { stack.Children.Add(Label("DISK")); stack.Children.Add(_disk); }
        Content = stack;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) => Tick();
        _timer.Start();
        Tick();
        DetachedFromVisualTree += (_, _) => _timer.Stop();
    }

    private void Tick()
    {
        var s = _metrics.Sample();
        _host.Text = s.MachineName;
        _cpu.Text = $"{s.CpuPercent:0.0}%";
        _ram.Text = $"{s.RamUsedGb:0.0} / {s.RamTotalGb:0.0} GB ({s.RamUsedPercent:0.0}%)";
        _disk.Text = string.Join("  ", s.Disks.Select(d => $"{d.Name} {d.FreeGb:0.0}G free"));
    }

    private static TextBlock Label(string t) => new()
    {
        Text = t, FontSize = 10, Foreground = Brush("#6c7086"), LetterSpacing = 1.2
    };
    private static TextBlock Val() => new()
    {
        FontSize = 18, FontWeight = FontWeight.SemiBold, Foreground = Brush("#cdd6f4")
    };
    private static IBrush Brush(string hex) => new SolidColorBrush(Color.Parse(hex));
}

public sealed class ChronoTileView : UserControl
{
    private readonly ChronoSettings _settings;
    private readonly TextBlock _time = new()
    {
        FontSize = 48, FontWeight = FontWeight.Light,
        Foreground = new SolidColorBrush(Color.Parse("#cdd6f4")),
        HorizontalAlignment = HorizontalAlignment.Center
    };
    private readonly TextBlock _date = new()
    {
        FontSize = 14, Foreground = new SolidColorBrush(Color.Parse("#a6adc8")),
        HorizontalAlignment = HorizontalAlignment.Center, Margin = new Avalonia.Thickness(0, 8, 0, 0)
    };
    private readonly DispatcherTimer _timer;

    public ChronoTileView()
    {
        _settings = ModuleSettingsStore.Load("Chrono", () => new ChronoSettings());
        Content = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            Children = { _time, _date, StyleBadge() }
        };
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        _timer.Tick += (_, _) => Tick();
        _timer.Start();
        Tick();
        DetachedFromVisualTree += (_, _) => _timer.Stop();
    }

    private Control StyleBadge() => new TextBlock
    {
        Text = $"Style: {_settings.Style}",
        FontSize = 11,
        Foreground = new SolidColorBrush(Color.Parse("#6c7086")),
        HorizontalAlignment = HorizontalAlignment.Center,
        Margin = new Avalonia.Thickness(0, 12, 0, 0)
    };

    private void Tick()
    {
        var now = DateTime.Now;
        var fmt = _settings.TwentyFourHour
            ? (_settings.ShowSeconds ? "HH:mm:ss" : "HH:mm")
            : (_settings.ShowSeconds ? "h:mm:ss tt" : "h:mm tt");
        _time.Text = now.ToString(fmt);
        _date.Text = now.ToString("dddd, MMM d");
        if (_settings.Style.Equals("Minimal", StringComparison.OrdinalIgnoreCase))
            _time.FontSize = 36;
    }
}

public sealed class PhonoTileView : UserControl
{
    private readonly IMediaSessionService _media;
    private readonly TextBlock _title = new() { FontSize = 16, FontWeight = FontWeight.SemiBold, Foreground = Brush("#cdd6f4") };
    private readonly TextBlock _artist = new() { FontSize = 12, Foreground = Brush("#6c7086"), Margin = new Avalonia.Thickness(0, 4, 0, 16) };

    public PhonoTileView(IMediaSessionService media)
    {
        _media = media;
        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal, Spacing = 10,
            HorizontalAlignment = HorizontalAlignment.Center,
            Children =
            {
                Btn("⏮", () => _ = _media.PreviousAsync()),
                Btn("⏯", () => _ = _media.PlayPauseAsync()),
                Btn("⏭", () => _ = _media.NextAsync()),
            }
        };
        Content = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            Children = { _title, _artist, row }
        };
        _media.Changed += (_, _) => Update();
        Update();
        DetachedFromVisualTree += (_, _) => _media.Changed -= OnChanged;
    }

    private void OnChanged(object? s, EventArgs e) => Update();

    private void Update()
    {
        var c = _media.Current;
        _title.Text = c?.Title ?? "Nothing playing";
        _artist.Text = c?.Artist ?? "Start media on this PC";
    }

    private static Button Btn(string g, Action act)
    {
        var b = new Button
        {
            Content = g, Width = 44, Height = 36,
            Background = Brush("#313244"), Foreground = Brush("#cdd6f4"),
            CornerRadius = new Avalonia.CornerRadius(8)
        };
        b.Click += (_, _) => act();
        return b;
    }

    private static IBrush Brush(string hex) => new SolidColorBrush(Color.Parse(hex));
}

public sealed class PulseTileView : UserControl
{
    private readonly IAudioLevelService _levels;
    private readonly List<Avalonia.Controls.Shapes.Rectangle> _bars = [];
    private readonly DispatcherTimer _timer;

    public PulseTileView(IAudioLevelService levels)
    {
        _levels = levels;
        _levels.Start();
        var panel = new StackPanel
        {
            Orientation = Orientation.Horizontal, Spacing = 4, Height = 120,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Bottom
        };
        for (var i = 0; i < 16; i++)
        {
            var bar = new Avalonia.Controls.Shapes.Rectangle
            {
                Width = 10, Height = 8,
                Fill = new SolidColorBrush(Color.Parse("#89dceb")),
                RadiusX = 2, RadiusY = 2,
                VerticalAlignment = VerticalAlignment.Bottom
            };
            _bars.Add(bar);
            panel.Children.Add(bar);
        }
        Content = panel;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
        _timer.Tick += (_, _) =>
        {
            var bands = _levels.Bands;
            for (var i = 0; i < _bars.Count; i++)
                _bars[i].Height = 8 + (i < bands.Count ? bands[i] : _levels.Peak) * 110;
        };
        _timer.Start();
        DetachedFromVisualTree += (_, _) =>
        {
            _timer.Stop();
            _levels.Stop();
        };
    }
}
