using System.IO;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using MosaicShell.Core.Runtime;
using MosaicShell.Core.Services;
using MosaicShell.Core.Settings;

namespace MosaicShell.Host.Tiles.Surfaces;

internal static class WidgetChrome
{
    /// <summary>Content-only frame fill. No module title - the overlay shell is the only chrome.</summary>
    public static Control Wrap(Control body, double corner = 0, double padding = 0, double minWidth = 0)
    {
        if (corner <= 0 && padding <= 0 && minWidth <= 0)
            return body;

        return new Border
        {
            CornerRadius = new Avalonia.CornerRadius(corner),
            Padding = new Avalonia.Thickness(padding),
            MinWidth = minWidth > 0 ? minWidth : 0,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Background = Brushes.Transparent,
            Child = body
        };
    }

    public static IBrush Brush(string hex) => new SolidColorBrush(Color.Parse(hex));
}

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
        var compact = _settings.Style.Equals("Compact", StringComparison.OrdinalIgnoreCase);
        var stack = new StackPanel { Spacing = compact ? 4 : 8 };
        if (_settings.ShowHost) { stack.Children.Add(Label("HOST", compact)); stack.Children.Add(_host); }
        if (_settings.ShowCpu) { stack.Children.Add(Label("CPU", compact)); stack.Children.Add(_cpu); }
        if (_settings.ShowRam) { stack.Children.Add(Label("MEMORY", compact)); stack.Children.Add(_ram); }
        if (_settings.ShowDisk) { stack.Children.Add(Label("DISK", compact)); stack.Children.Add(_disk); }

        Content = WidgetChrome.Wrap(
            stack,
            corner: compact ? 6 : 0,
            padding: 0,
            minWidth: compact ? 220 : 280);

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
        _disk.Text = string.Join("\n", s.Disks.Select(d => $"{d.Name} {d.FreeGb:0.0}G free"));
    }

    private static TextBlock Label(string t, bool compact) => new()
    {
        Text = t,
        FontSize = compact ? 9 : 10,
        Foreground = WidgetChrome.Brush("#6c7086"),
        LetterSpacing = 1.2
    };

    private static TextBlock Val() => new()
    {
        FontSize = 18,
        FontWeight = FontWeight.SemiBold,
        Foreground = WidgetChrome.Brush("#cdd6f4"),
        TextWrapping = TextWrapping.Wrap
    };
}

public sealed class ChronoTileView : UserControl
{
    private readonly ChronoSettings _settings;
    private readonly TextBlock _time = new()
    {
        FontWeight = FontWeight.Light,
        Foreground = WidgetChrome.Brush("#cdd6f4"),
        HorizontalAlignment = HorizontalAlignment.Center
    };
    private readonly TextBlock _date = new()
    {
        Foreground = WidgetChrome.Brush("#a6adc8"),
        HorizontalAlignment = HorizontalAlignment.Center,
        Margin = new Avalonia.Thickness(0, 8, 0, 0)
    };
    private readonly DispatcherTimer _timer;

    public ChronoTileView()
    {
        _settings = ModuleSettingsStore.Load("Chrono", () => new ChronoSettings());
        ApplyStyleChrome();
        Content = WidgetChrome.Wrap(
            new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                Children = { _time, _date }
            },
            minWidth: 280);
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        _timer.Tick += (_, _) => Tick();
        _timer.Start();
        Tick();
        DetachedFromVisualTree += (_, _) => _timer.Stop();
    }

    private void ApplyStyleChrome()
    {
        var style = _settings.Style;
        if (style.Equals("Text", StringComparison.OrdinalIgnoreCase)
            || style.Equals("Minimal", StringComparison.OrdinalIgnoreCase))
        {
            _time.FontSize = 36;
            _time.FontWeight = FontWeight.SemiBold;
            _date.FontSize = 12;
        }
        else if (style.Equals("Tech", StringComparison.OrdinalIgnoreCase)
                 || style.Equals("CircTech", StringComparison.OrdinalIgnoreCase))
        {
            _time.FontSize = 44;
            _time.FontFamily = new FontFamily("Consolas, Cascadia Mono, monospace");
            _date.FontSize = 13;
            _date.FontFamily = _time.FontFamily;
        }
        else if (style.Equals("Light", StringComparison.OrdinalIgnoreCase))
        {
            _time.FontSize = 52;
            _time.FontWeight = FontWeight.Thin;
            _date.FontSize = 14;
        }
        else
        {
            _time.FontSize = 48;
            _date.FontSize = 14;
        }
    }

    private void Tick()
    {
        var now = DateTime.Now;
        var fmt = _settings.TwentyFourHour
            ? (_settings.ShowSeconds ? "HH:mm:ss" : "HH:mm")
            : (_settings.ShowSeconds ? "h:mm:ss tt" : "h:mm tt");
        _time.Text = now.ToString(fmt);
        _date.Text = now.ToString("dddd, MMM d");
    }
}

public sealed class PhonoTileView : UserControl
{
    private readonly IMediaSessionService _media;
    private readonly PhonoSettings _settings;
    private readonly TextBlock _title = new()
    {
        FontSize = 16, FontWeight = FontWeight.SemiBold, Foreground = WidgetChrome.Brush("#cdd6f4"),
        TextTrimming = TextTrimming.CharacterEllipsis, MaxWidth = 220
    };
    private readonly TextBlock _artist = new()
    {
        FontSize = 12, Foreground = WidgetChrome.Brush("#6c7086"),
        Margin = new Avalonia.Thickness(0, 4, 0, 12),
        TextTrimming = TextTrimming.CharacterEllipsis, MaxWidth = 220
    };
    private readonly Image _art = new()
    {
        Width = 72, Height = 72, Stretch = Stretch.UniformToFill, IsVisible = false
    };
    private readonly EventHandler _onChanged;

    public PhonoTileView(IMediaSessionService media)
    {
        _media = media;
        _settings = ModuleSettingsStore.Load("Phono", () => new PhonoSettings());

        var transport = new StackPanel
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

        var textCol = new StackPanel { Children = { _title } };
        if (_settings.ShowArtist)
            textCol.Children.Add(_artist);
        textCol.Children.Add(transport);

        Content = WidgetChrome.Wrap(
            new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 14,
                VerticalAlignment = VerticalAlignment.Center,
                Children = { _art, textCol }
            },
            minWidth: 320);

        _onChanged = (_, _) => Dispatcher.UIThread.Post(Update);
        _media.Changed += _onChanged;
        Update();
        DetachedFromVisualTree += (_, _) => _media.Changed -= _onChanged;
    }

    private void Update()
    {
        var c = _media.Current;
        _title.Text = c?.Title ?? "Nothing playing";
        _artist.Text = c?.Artist ?? "Start media on this PC";
        if (c?.ThumbnailPng is { Length: > 0 } png)
        {
            try
            {
                using var ms = new MemoryStream(png);
                _art.Source = new Bitmap(ms);
                _art.IsVisible = true;
            }
            catch
            {
                _art.Source = null;
                _art.IsVisible = false;
            }
        }
        else
        {
            _art.Source = null;
            _art.IsVisible = false;
        }
    }

    private static Button Btn(string g, Action act)
    {
        var b = new Button
        {
            Content = g, Width = 44, Height = 36,
            Background = WidgetChrome.Brush("#313244"), Foreground = WidgetChrome.Brush("#cdd6f4"),
            CornerRadius = new Avalonia.CornerRadius(8)
        };
        b.Click += (_, _) => act();
        return b;
    }
}

public sealed class PulseTileView : UserControl
{
    private readonly IAudioLevelService _levels;
    private readonly PulseSettings _settings;
    private readonly List<Control> _viz = [];
    private readonly Panel _host;
    private readonly DispatcherTimer _timer;
    private readonly bool _round;

    public PulseTileView(IAudioLevelService levels)
    {
        _levels = levels;
        _settings = ModuleSettingsStore.Load("Pulse", () => new PulseSettings());
        _round = _settings.VisualizerType.Equals("Round", StringComparison.OrdinalIgnoreCase)
                 || _settings.Style.Equals("Circ", StringComparison.OrdinalIgnoreCase);

        _levels.Start();
        _host = _round
            ? new Canvas { Width = 200, Height = 200 }
            : new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 4,
                Height = 120,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom
            };

        for (var i = 0; i < 16; i++)
        {
            if (_round)
            {
                var dot = new Ellipse
                {
                    Width = 10, Height = 10,
                    Fill = WidgetChrome.Brush("#89dceb")
                };
                _viz.Add(dot);
                ((Canvas)_host).Children.Add(dot);
            }
            else
            {
                var bar = new Rectangle
                {
                    Width = 10, Height = 8,
                    Fill = WidgetChrome.Brush("#89dceb"),
                    RadiusX = 2, RadiusY = 2,
                    VerticalAlignment = VerticalAlignment.Bottom
                };
                _viz.Add(bar);
                ((StackPanel)_host).Children.Add(bar);
            }
        }

        Content = WidgetChrome.Wrap(_host, minWidth: 280);

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
        _timer.Tick += (_, _) => Tick();
        _timer.Start();
        DetachedFromVisualTree += (_, _) =>
        {
            _timer.Stop();
            _levels.Stop();
        };
    }

    private void Tick()
    {
        var bands = _levels.Bands;
        if (_round)
        {
            var canvas = (Canvas)_host;
            var cx = canvas.Width / 2;
            var cy = canvas.Height / 2;
            for (var i = 0; i < _viz.Count; i++)
            {
                var level = i < bands.Count ? bands[i] : _levels.Peak;
                var radius = 40 + level * 50;
                var angle = i * (Math.PI * 2 / _viz.Count) - Math.PI / 2;
                var ell = (Ellipse)_viz[i];
                var size = 8 + level * 14;
                ell.Width = size;
                ell.Height = size;
                Canvas.SetLeft(ell, cx + Math.Cos(angle) * radius - size / 2);
                Canvas.SetTop(ell, cy + Math.Sin(angle) * radius - size / 2);
            }
        }
        else
        {
            for (var i = 0; i < _viz.Count; i++)
            {
                var level = i < bands.Count ? bands[i] : _levels.Peak;
                ((Rectangle)_viz[i]).Height = 8 + level * 110;
            }
        }
    }
}
