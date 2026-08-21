using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using MosaicShell.Core.Modules;
using MosaicShell.Core.Runtime;
using MosaicShell.Core.Services;
using MosaicShell.Core.Settings;

namespace MosaicShell.Host.Tiles.Surfaces;

public sealed class TesseraTileView : UserControl
{
    private readonly IAudioService _audio;
    private readonly IBrightnessService _brightness;
    private readonly Slider _vol;
    private readonly Slider _bright;
    private readonly TextBlock _media;
    private bool _suppress;

    public TesseraTileView(IAudioService audio, IBrightnessService brightness, IMediaSessionService media)
    {
        _audio = audio;
        _brightness = brightness;
        _vol = new Slider { Minimum = 0, Maximum = 1, Value = audio.MasterVolume, Width = 260 };
        _bright = new Slider
        {
            Minimum = 0, Maximum = 1,
            Value = brightness.IsSupported ? brightness.Brightness : 0.5,
            Width = 260,
            IsEnabled = brightness.IsSupported
        };
        _media = new TextBlock
        {
            Text = media.Current?.Title ?? "No media",
            FontSize = 12, Foreground = Brush("#a6adc8")
        };
        _vol.PropertyChanged += (_, e) =>
        {
            if (_suppress || e.Property != Slider.ValueProperty) return;
            _audio.MasterVolume = _vol.Value;
        };
        _bright.PropertyChanged += (_, e) =>
        {
            if (_suppress || e.Property != Slider.ValueProperty) return;
            if (_brightness.IsSupported) _brightness.Brightness = _bright.Value;
        };
        _audio.Changed += OnAudio;
        media.Changed += (_, _) =>
            Dispatcher.UIThread.Post(() => _media.Text = media.Current?.Title ?? "No media");

        Content = new StackPanel
        {
            Spacing = 12,
            Children =
            {
                Labeled("Volume", _vol),
                Labeled(_brightness.IsSupported ? "Brightness" : "Brightness (unsupported)", _bright),
                _media,
                MuteButton()
            }
        };
        DetachedFromVisualTree += (_, _) => _audio.Changed -= OnAudio;
    }

    private void OnAudio(object? s, EventArgs e) => Dispatcher.UIThread.Post(() =>
    {
        _suppress = true;
        _vol.Value = _audio.MasterVolume;
        _suppress = false;
    });

    private Button MuteButton()
    {
        var b = new Button
        {
            Content = _audio.IsMuted ? "Unmute" : "Mute",
            Background = Brush("#313244"), Foreground = Brush("#cdd6f4"),
            HorizontalAlignment = HorizontalAlignment.Left, Padding = new Avalonia.Thickness(12, 6)
        };
        b.Click += (_, _) =>
        {
            _audio.IsMuted = !_audio.IsMuted;
            b.Content = _audio.IsMuted ? "Unmute" : "Mute";
        };
        return b;
    }

    private static StackPanel Labeled(string label, Control c) => new()
    {
        Spacing = 4,
        Children =
        {
            new TextBlock { Text = label, FontSize = 12, Foreground = Brush("#a6adc8") },
            c
        }
    };

    private static IBrush Brush(string hex) => new SolidColorBrush(Color.Parse(hex));
}

public sealed class MixdeckTileView : UserControl
{
    private readonly IAppAudioService _apps;
    private readonly StackPanel _list = new() { Spacing = 8 };
    private readonly DispatcherTimer _timer;

    public MixdeckTileView(IAppAudioService apps)
    {
        _apps = apps;
        Content = new ScrollViewer { Content = _list, Height = 220 };
        Refresh();
        _apps.SessionsChanged += (_, _) => Dispatcher.UIThread.Post(Refresh);
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _timer.Tick += (_, _) => Refresh();
        _timer.Start();
        DetachedFromVisualTree += (_, _) => _timer.Stop();
    }

    private void Refresh()
    {
        _list.Children.Clear();
        foreach (var s in _apps.GetSessions().Take(12))
        {
            var label = new TextBlock
            {
                Text = s.DisplayName, Foreground = Brush("#cdd6f4"),
                VerticalAlignment = VerticalAlignment.Center
            };
            var slider = new Slider { Minimum = 0, Maximum = 1, Value = s.Volume };
            var id = s.Id;
            slider.PropertyChanged += (_, e) =>
            {
                if (e.Property == Slider.ValueProperty)
                    _apps.SetVolume(id, slider.Value);
            };
            var row = new Grid { ColumnDefinitions = new ColumnDefinitions("120,*") };
            Grid.SetColumn(slider, 1);
            row.Children.Add(label);
            row.Children.Add(slider);
            _list.Children.Add(row);
        }

        if (_list.Children.Count == 0)
            _list.Children.Add(new TextBlock { Text = "No active audio sessions", Foreground = Brush("#6c7086") });
    }

    private static IBrush Brush(string hex) => new SolidColorBrush(Color.Parse(hex));
}

public sealed class InlayTileView : UserControl
{
    private readonly TextBox _search = new() { Watermark = "Search apps…" };
    private readonly WrapPanel _grid = new();

    public InlayTileView()
    {
        var settings = ModuleSettingsStore.Load("Inlay", () => new InlaySettings());
        _search.KeyUp += (_, e) =>
        {
            if (e.Key == Avalonia.Input.Key.Enter) Launch(_search.Text ?? "");
        };
        foreach (var pin in settings.Pins)
            _grid.Children.Add(AppButton(pin));
        foreach (var app in new[] { "notepad", "calc", "cmd", "explorer", "ms-settings:" })
            if (!settings.Pins.Contains(app, StringComparer.OrdinalIgnoreCase))
                _grid.Children.Add(AppButton(app));

        Content = new StackPanel { Children = { _search, _grid } };
    }

    private Button AppButton(string app)
    {
        var b = new Button
        {
            Content = app, Margin = new Avalonia.Thickness(4), Width = 100, Height = 44,
            Background = Brush("#313244"), Foreground = Brush("#cdd6f4"),
            CornerRadius = new Avalonia.CornerRadius(8)
        };
        b.Click += (_, _) => Launch(app);
        return b;
    }

    private static void Launch(string target)
    {
        try
        {
            Process.Start(new ProcessStartInfo { FileName = target, UseShellExecute = true });
        }
        catch { /* ignore */ }
    }

    private static IBrush Brush(string hex) => new SolidColorBrush(Color.Parse(hex));
}

public sealed class ChordTileView : UserControl
{
    private readonly TextBox _box = new() { Watermark = "Type to launch… (Enter)" };
    private readonly ChordSettings _settings;

    public ChordTileView()
    {
        _settings = ModuleSettingsStore.Load("Chord", () => new ChordSettings());
        _box.KeyUp += (_, e) =>
        {
            if (e.Key != Avalonia.Input.Key.Enter) return;
            var q = (_box.Text ?? "").Trim();
            var match = _settings.Actions.FirstOrDefault(a =>
                a.Name.Contains(q, StringComparison.OrdinalIgnoreCase));
            var target = match?.Target ?? q;
            try { Process.Start(new ProcessStartInfo { FileName = target, UseShellExecute = true }); }
            catch { /* ignore */ }
        };
        Content = new StackPanel
        {
            Spacing = 10,
            Children =
            {
                _box,
                new TextBlock
                {
                    Text = $"Hotkey setting: {_settings.HotkeyGesture}",
                    FontSize = 12, Foreground = Brush("#6c7086")
                }
            }
        };
    }

    private static IBrush Brush(string hex) => new SolidColorBrush(Color.Parse(hex));
}

public sealed class SubstrateTileView : UserControl
{
    public SubstrateTileView(IAudioService audio, IBrightnessService brightness)
    {
        var tiles = new WrapPanel();
        tiles.Children.Add(Qs("Mute", () => audio.IsMuted = !audio.IsMuted));
        tiles.Children.Add(Qs("Vol +", () => audio.MasterVolume = Math.Min(1, audio.MasterVolume + 0.05)));
        tiles.Children.Add(Qs("Vol −", () => audio.MasterVolume = Math.Max(0, audio.MasterVolume - 0.05)));
        if (brightness.IsSupported)
            tiles.Children.Add(Qs("Bright", () => brightness.Brightness = Math.Min(1, brightness.Brightness + 0.05)));
        tiles.Children.Add(Qs("Settings", () => Process.Start(new ProcessStartInfo
        {
            FileName = "ms-settings:", UseShellExecute = true
        })));
        Content = tiles;
    }

    private static Button Qs(string label, Action act)
    {
        var b = new Button
        {
            Content = label, Margin = new Avalonia.Thickness(4), Width = 140, Height = 52,
            Background = Brush("#313244"), Foreground = Brush("#cdd6f4"),
            CornerRadius = new Avalonia.CornerRadius(10)
        };
        b.Click += (_, _) => act();
        return b;
    }

    private static IBrush Brush(string hex) => new SolidColorBrush(Color.Parse(hex));
}

public sealed class SlateTileView : UserControl
{
    private readonly TextBlock _clock = new()
    {
        FontSize = 56, FontWeight = FontWeight.Thin,
        Foreground = Brush("#cdd6f4"), HorizontalAlignment = HorizontalAlignment.Center
    };
    private readonly DispatcherTimer _timer;

    public SlateTileView()
    {
        var settings = ModuleSettingsStore.Load("Slate", () => new SlateSettings());
        Content = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            Children =
            {
                _clock,
                new TextBlock
                {
                    Text = settings.HideOnFullscreen ? "Idle · hide on fullscreen (policy on)" : "Idle surface",
                    FontSize = 13, Foreground = Brush("#6c7086"),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Avalonia.Thickness(0, 8, 0, 0)
                }
            }
        };
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) => _clock.Text = DateTime.Now.ToString("HH:mm");
        _timer.Start();
        _clock.Text = DateTime.Now.ToString("HH:mm");
        DetachedFromVisualTree += (_, _) => _timer.Stop();
    }

    private static IBrush Brush(string hex) => new SolidColorBrush(Color.Parse(hex));
}

public sealed class GenericTileView : UserControl
{
    public GenericTileView(ModuleInfo info)
    {
        Content = new TextBlock
        {
            Text = $"{info.DisplayName}\n{info.Description}",
            TextWrapping = TextWrapping.Wrap,
            Foreground = Brush("#cdd6f4")
        };
    }

    private static IBrush Brush(string hex) => new SolidColorBrush(Color.Parse(hex));
}
