using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using MosaicShell.Core.Modules;
using MosaicShell.Core.Runtime;
using MosaicShell.Core.Services;
using MosaicShell.Core.Settings;
using MosaicShell.Core.Styles;

namespace MosaicShell.Host.Tiles.Surfaces
{
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
                Minimum = 0,
                Maximum = 1,
                Value = brightness.IsSupported ? brightness.Brightness : 0.5,
                Width = 260,
                IsEnabled = brightness.IsSupported
            };
            _media = new TextBlock
            {
                Text = media.Current?.Title ?? "No media",
                FontSize = 12,
                Foreground = Brush("#a6adc8")
            };
            _vol.PropertyChanged += (_, e) =>
            {
                if (_suppress || e.Property != Avalonia.Controls.Primitives.RangeBase.ValueProperty)
                {
                    return;
                }

                _audio.MasterVolume = _vol.Value;
            };
            _bright.PropertyChanged += (_, e) =>
            {
                if (_suppress || e.Property != Avalonia.Controls.Primitives.RangeBase.ValueProperty)
                {
                    return;
                }

                if (_brightness.IsSupported)
                {
                    _brightness.Brightness = _bright.Value;
                }
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

        private void OnAudio(object? s, EventArgs e)
        {
            Dispatcher.UIThread.Post(() =>
        {
            _suppress = true;
            _vol.Value = _audio.MasterVolume;
            _suppress = false;
        });
        }

        private Button MuteButton()
        {
            Button b = new()
            {
                Content = _audio.IsMuted ? "Unmute" : "Mute",
                Background = Brush("#313244"),
                Foreground = Brush("#cdd6f4"),
                HorizontalAlignment = HorizontalAlignment.Left,
                Padding = new Thickness(12, 6)
            };
            b.Click += (_, _) =>
            {
                _audio.IsMuted = !_audio.IsMuted;
                b.Content = _audio.IsMuted ? "Unmute" : "Mute";
            };
            return b;
        }

        private static StackPanel Labeled(string label, Control c)
        {
            return new()
            {
                Spacing = 4,
                Children =
            {
                new TextBlock { Text = label, FontSize = 12, Foreground = Brush("#a6adc8") },
                c
            }
            };
        }

        private static IBrush Brush(string hex)
        {
            return new SolidColorBrush(Color.Parse(hex));
        }
    }

    public sealed class MixdeckTileView : UserControl
    {
        private readonly IAppAudioService _apps;
        private readonly IAudioService _master;
        private readonly StackPanel _list = new() { Spacing = 10 };
        private readonly DispatcherTimer _timer;
        private readonly string _style;
        private readonly Slider _masterSlider;
        private bool _suppressMaster;

        public MixdeckTileView(IAppAudioService apps, IAudioService master)
        {
            _apps = apps;
            _master = master;
            MixdeckSettings mixSettings = ModuleSettingsStore.Load("Mixdeck", () => new MixdeckSettings());
            _style = mixSettings.Style;
            (string? bg, string? border, int radius) = MixdeckChrome(_style, mixSettings.ColorScheme);
            TextBlock title = new()
            {
                Text = $"Mixdeck · {_style}",
                FontSize = 16,
                FontWeight = FontWeight.SemiBold,
                Foreground = Brush("#cdd6f4"),
                Margin = new Thickness(0, 0, 0, 4)
            };
            TextBlock sessionCount = new()
            {
                FontSize = 11,
                Foreground = Brush("#6c7086"),
                Margin = new Thickness(0, 0, 0, 8)
            };
            _masterSlider = new Slider
            {
                Minimum = 0,
                Maximum = 1,
                Value = _master.MasterVolume,
                Margin = new Thickness(0, 0, 0, 4)
            };
            _masterSlider.PropertyChanged += (_, e) =>
            {
                if (_suppressMaster || e.Property != Avalonia.Controls.Primitives.RangeBase.ValueProperty)
                {
                    return;
                }

                _master.MasterVolume = _masterSlider.Value;
            };
            StackPanel masterRow = new()
            {
                Spacing = 4,
                Children =
                {
                    new TextBlock
                    {
                        Text = _master.IsMuted ? "Master · muted" : "Master volume",
                        FontSize = 12,
                        Foreground = Brush("#a6adc8")
                    },
                    _masterSlider
                }
            };
            StackPanel body = new() { Children = { title, sessionCount, masterRow, _list } };
            Content = new Border
            {
                Background = Brush(bg),
                CornerRadius = new CornerRadius(radius),
                Padding = new Thickness(16),
                BorderBrush = Brush(border),
                BorderThickness = new Thickness(1),
                MinWidth = 340,
                Child = new ScrollViewer { Content = body, Height = 320 }
            };
            Refresh(sessionCount);
            _apps.SessionsChanged += (_, _) => Dispatcher.UIThread.Post(() => Refresh(sessionCount));
            _master.Changed += OnMasterChanged;
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            _timer.Tick += (_, _) => Refresh(sessionCount);
            _timer.Start();
            DetachedFromVisualTree += (_, _) =>
            {
                _timer.Stop();
                _master.Changed -= OnMasterChanged;
            };
        }

        private void OnMasterChanged(object? sender, EventArgs e)
        {
            Dispatcher.UIThread.Post(() =>
        {
            _suppressMaster = true;
            _masterSlider.Value = _master.MasterVolume;
            _suppressMaster = false;
        });
        }

        private void Refresh(TextBlock sessionCount)
        {
            IReadOnlyList<AppAudioSession> sessions = _apps.GetSessions();
            sessionCount.Text = $"{sessions.Count} session{(sessions.Count == 1 ? "" : "s")}";
            _list.Children.Clear();
            foreach (AppAudioSession? s in sessions.Take(14))
            {
                string id = s.Id;
                TextBlock label = new()
                {
                    Text = s.DisplayName,
                    Foreground = Brush("#cdd6f4"),
                    VerticalAlignment = VerticalAlignment.Center,
                    Width = 110,
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
                Slider slider = new()
                {
                    Minimum = 0,
                    Maximum = 1,
                    Value = s.Volume,
                    VerticalAlignment = VerticalAlignment.Center
                };
                slider.PropertyChanged += (_, e) =>
                {
                    if (e.Property == Avalonia.Controls.Primitives.RangeBase.ValueProperty)
                    {
                        _apps.SetVolume(id, slider.Value);
                    }
                };
                Button mute = new()
                {
                    Content = s.IsMuted ? "Unmute" : "Mute",
                    Width = 64,
                    Padding = new Thickness(4, 2),
                    Background = Brush("#313244"),
                    Foreground = Brush("#cdd6f4")
                };
                mute.Click += (_, _) =>
                {
                    _apps.SetMuted(id, !s.IsMuted);
                    Refresh(sessionCount);
                };
                Grid row = new() { ColumnDefinitions = new ColumnDefinitions("110,*,68") };
                Grid.SetColumn(slider, 1);
                Grid.SetColumn(mute, 2);
                row.Children.Add(label);
                row.Children.Add(slider);
                row.Children.Add(mute);
                _list.Children.Add(row);
            }

            if (_list.Children.Count == 0)
            {
                _list.Children.Add(new TextBlock { Text = "No active audio sessions", Foreground = Brush("#6c7086") });
            }
        }

        private static (string bg, string border, int radius) MixdeckChrome(string style, string colorScheme)
        {
            (string? bg, string? border, int radius) = style.ToLowerInvariant() switch
            {
                "rounded" => ("#E6282438", "#585b70", 16),
                "solid" => ("#F0181818", "#313244", 4),
                "center" => ("#E6222130", "#45475a", 12),
                _ when style.StartsWith("Fluent", StringComparison.OrdinalIgnoreCase) => ("#E6181825", "#45475a", 8),
                _ => ("#E6181825", "#45475a", 12)
            };

            return colorScheme.ToLowerInvariant() switch
            {
                "dark" => ("#F0101018", "#313244", radius),
                "light" => ("#F0eceff4", "#bac2de", radius),
                "accent" => ("#E6282438", "#89b4fa", radius),
                "frost" => ("#D8e6e9ef", "#94a3b8", radius),
                "midnight" => ("#F0080a12", "#1e2030", radius),
                "sunset" => ("#E63d2e4a", "#f38ba8", radius),
                _ => (bg, border, radius)
            };
        }

        private static IBrush Brush(string hex)
        {
            return new SolidColorBrush(Color.Parse(hex));
        }
    }

    public sealed class InlayTileView : UserControl
    {
        private readonly InlaySettings _settings;
        private readonly TextBox _search = new() { PlaceholderText = "Search apps…" };
        private readonly bool _win11;
        private readonly StackPanel? _pinsColumn;
        private readonly StackPanel? _appsColumn;
        private readonly WrapPanel? _singleGrid;

        public InlayTileView()
        {
            _settings = ModuleSettingsStore.Load("Inlay", () => new InlaySettings());
            _win11 = StyleIds.Normalize(_settings.Style)
                .Equals(StyleIds.Windows11, StringComparison.OrdinalIgnoreCase);

            _search.KeyUp += (_, e) =>
            {
                if (e.Key != Avalonia.Input.Key.Enter)
                {
                    return;
                }

                if (InlayLaunchLogic.TryLaunchFromQuery(_search.Text))
                {
                    return;
                }

                RebuildApps(_search.Text ?? "");
            };
            _search.TextChanged += (_, _) => RebuildApps(_search.Text ?? "");

            Control appArea;
            if (_win11)
            {
                _pinsColumn = new StackPanel { Spacing = 6 };
                _appsColumn = new StackPanel { Spacing = 6 };
                appArea = new Grid
                {
                    ColumnDefinitions = new ColumnDefinitions("*,*"),
                    Margin = new Thickness(0, 4, 0, 0),
                    Children =
                    {
                        ColumnBlock("Pinned", _pinsColumn, 0),
                        ColumnBlock("Apps", _appsColumn, 1)
                    }
                };
            }
            else
            {
                _singleGrid = new WrapPanel { Margin = new Thickness(0, 4, 0, 0) };
                appArea = _singleGrid;
            }

            StackPanel inner = new()
            {
                Spacing = 10,
                Children =
                {
                    _search,
                    new TextBlock
                    {
                        Text = _win11 ? "Pinned · All apps" : "Pinned & apps",
                        FontSize = 12,
                        Foreground = Brush("#a6adc8")
                    },
                    appArea
                }
            };
            int pad = _win11 ? 12
                : _settings.Style.Equals("ClassicWavy", StringComparison.OrdinalIgnoreCase) ? 8 : 4;
            int radius = _win11 ? 12
                : _settings.Style.Equals("Flat", StringComparison.OrdinalIgnoreCase) ? 4 : 8;
            Content = new Border
            {
                Background = Brush("#E6181825"),
                BorderBrush = Brush("#45475a"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(radius),
                Padding = new Thickness(pad),
                MinWidth = 360,
                Child = inner
            };
            RebuildApps("");
        }

        private static StackPanel ColumnBlock(string header, StackPanel body, int column)
        {
            StackPanel col = new() { Spacing = 6, Margin = new Thickness(column == 0 ? 0 : 6, 0, column == 0 ? 6 : 0, 0) };
            col.Children.Add(new TextBlock
            {
                Text = header,
                FontSize = 11,
                Foreground = Brush("#6c7086")
            });
            col.Children.Add(body);
            Grid.SetColumn(col, column);
            return col;
        }

        private void RebuildApps(string filter)
        {
            IReadOnlyList<LaunchTarget> targets = InlayLaunchLogic.BuildTargets(filter, _settings.Pins);
            if (_win11 && _pinsColumn is not null && _appsColumn is not null)
            {
                _pinsColumn.Children.Clear();
                _appsColumn.Children.Clear();
                List<LaunchTarget> pinned = [.. targets.Where(t => t.Group == "Pinned")];
                List<LaunchTarget> rest = [.. targets.Where(t => t.Group != "Pinned")];
                PopulateColumn(_pinsColumn, pinned, "No pinned apps");
                PopulateColumn(_appsColumn, rest, string.IsNullOrWhiteSpace(filter) ? "No apps" : $"No match for \"{filter.Trim()}\"");
                return;
            }

            if (_singleGrid is null)
            {
                return;
            }

            _singleGrid.Children.Clear();
            foreach (LaunchTarget t in targets)
            {
                _singleGrid.Children.Add(AppButton(t));
            }

            if (_singleGrid.Children.Count == 0)
            {
                _singleGrid.Children.Add(new TextBlock
                {
                    Text = string.IsNullOrWhiteSpace(filter) ? "No apps" : $"No match for \"{filter.Trim()}\"",
                    Foreground = Brush("#6c7086"),
                    FontSize = 12
                });
            }
        }

        private static void PopulateColumn(StackPanel column, IReadOnlyList<LaunchTarget> items, string emptyText)
        {
            foreach (LaunchTarget t in items)
            {
                column.Children.Add(AppButton(t));
            }

            if (column.Children.Count == 0)
            {
                column.Children.Add(new TextBlock
                {
                    Text = emptyText,
                    Foreground = Brush("#6c7086"),
                    FontSize = 11
                });
            }
        }

        private static Button AppButton(LaunchTarget target)
        {
            Button b = new()
            {
                Content = target.DisplayName,
                Margin = new Thickness(2),
                Width = 104,
                Height = 44,
                Background = Brush("#313244"),
                Foreground = Brush("#cdd6f4"),
                CornerRadius = new CornerRadius(8),
                Tag = target.Target
            };
            string launchTarget = target.Target;
            b.Click += (_, _) => Launch(launchTarget);
            return b;
        }

        private static void Launch(string target)
        {
            try
            {
                _ = Process.Start(new ProcessStartInfo { FileName = target, UseShellExecute = true });
            }
            catch { /* ignore */ }
        }

        private static IBrush Brush(string hex)
        {
            return new SolidColorBrush(Color.Parse(hex));
        }
    }

    public sealed class ChordTileView : UserControl
    {
        private readonly TextBox _box = new() { PlaceholderText = "Type to launch… (Enter)" };
        private readonly ChordSettings _settings;
        private readonly StackPanel _actions = new() { Spacing = 4 };
        private readonly Border _shell;

        public ChordTileView()
        {
            _settings = ModuleSettingsStore.Load("Chord", () => new ChordSettings());
            _box.KeyUp += (_, e) =>
            {
                if (e.Key != Avalonia.Input.Key.Enter)
                {
                    return;
                }

                string q = (_box.Text ?? "").Trim();
                ChordAction? match = _settings.Actions.FirstOrDefault(a =>
                    a.Name.Contains(q, StringComparison.OrdinalIgnoreCase));
                string target = match?.Target ?? q;
                Launch(target);
            };
            _box.TextChanged += (_, _) => HighlightMatches(_box.Text ?? "");

            foreach (ChordAction a in _settings.Actions)
            {
                _actions.Children.Add(ActionButton(a));
            }

            if (_actions.Children.Count == 0)
            {
                _actions.Children.Add(new TextBlock
                {
                    Text = "No actions configured - add ChordSettings.Actions or type a path.",
                    Foreground = Brush("#6c7086"),
                    FontSize = 12,
                    TextWrapping = TextWrapping.Wrap
                });
            }

            StackPanel inner = new()
            {
                Spacing = 10,
                Children = { _box, _actions }
            };
            _shell = new Border
            {
                Background = Brush("#E6181825"),
                BorderBrush = Brush("#45475a"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(_settings.Style.Equals("Bottom", StringComparison.OrdinalIgnoreCase) ? 16 : 10),
                Padding = new Thickness(14),
                MinWidth = 320,
                Child = inner,
                RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative)
            };
            Content = _shell;
            AttachedToVisualTree += (_, _) => AnimateOpenIfNeeded();
        }

        private Button ActionButton(ChordAction a)
        {
            Button btn = new()
            {
                Content = string.IsNullOrWhiteSpace(a.Name) ? a.Target : $"{a.Name} → {a.Target}",
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Background = Brush("#313244"),
                Foreground = Brush("#cdd6f4"),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10, 8),
                Tag = a.Name
            };
            string target = a.Target;
            btn.Click += (_, _) => Launch(target);
            return btn;
        }

        private void HighlightMatches(string query)
        {
            string q = query.Trim();
            foreach (Control child in _actions.Children)
            {
                if (child is not Button btn)
                {
                    continue;
                }

                string name = btn.Tag as string ?? "";
                bool match = string.IsNullOrEmpty(q)
                    || name.Contains(q, StringComparison.OrdinalIgnoreCase)
                    || (btn.Content?.ToString()?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false);
                btn.Opacity = match ? 1 : 0.45;
            }
        }

        private void AnimateOpenIfNeeded()
        {
            if (!_settings.Style.Equals("Expand", StringComparison.OrdinalIgnoreCase)
                && !_settings.Style.Equals("VectorSlide", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _shell.RenderTransform = new ScaleTransform(0.94, 0.94);
            _shell.Opacity = 0.6;
            int steps = 0;
            DispatcherTimer timer = new() { Interval = TimeSpan.FromMilliseconds(16) };
            timer.Tick += (_, _) =>
            {
                steps++;
                double t = Math.Min(1, steps / 8.0);
                _shell.Opacity = 0.6 + (0.4 * t);
                if (_shell.RenderTransform is ScaleTransform st)
                {
                    double s = 0.94 + (0.06 * t);
                    st.ScaleX = s;
                    st.ScaleY = s;
                }
                if (t >= 1)
                {
                    timer.Stop();
                }
            };
            timer.Start();
        }

        private static void Launch(string target)
        {
            try { _ = Process.Start(new ProcessStartInfo { FileName = target, UseShellExecute = true }); }
            catch { /* ignore */ }
        }

        private static IBrush Brush(string hex)
        {
            return new SolidColorBrush(Color.Parse(hex));
        }
    }

    public sealed class SubstrateTileView : UserControl
    {
        public SubstrateTileView(IAudioService audio, IBrightnessService brightness, IMediaSessionService media)
        {
            SubstrateSettings settings = ModuleSettingsStore.Load("Substrate", () => new SubstrateSettings());
            TextBlock smtcTitle = new()
            {
                FontSize = 13,
                Foreground = Brush("#cdd6f4"),
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = VerticalAlignment.Center
            };
            TextBlock smtcArtist = new()
            {
                FontSize = 11,
                Foreground = Brush("#6c7086"),
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            Button playBtn = new()
            {
                Content = "⏯",
                Width = 44,
                Height = 44,
                Background = Brush("#313244"),
                Foreground = Brush("#cdd6f4"),
                CornerRadius = new CornerRadius(10),
                VerticalAlignment = VerticalAlignment.Center
            };
            playBtn.Click += (_, _) => _ = media.PlayPauseAsync();

            void UpdateSmtc()
            {
                MediaSessionInfo? c = media.Current;
                smtcTitle.Text = c?.Title ?? "Nothing playing";
                smtcArtist.Text = c?.Artist ?? "System media";
                playBtn.Content = c?.IsPlaying == true ? "⏸" : "⏯";
            }
            UpdateSmtc();
            void onMediaChanged(object? _1, EventArgs _2)
            {
                Dispatcher.UIThread.Post(UpdateSmtc);
            }

            media.Changed += onMediaChanged;

            StackPanel smtcText = new()
            {
                Margin = new Thickness(10, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Children = { smtcTitle, smtcArtist }
            };
            Grid.SetColumn(playBtn, 0);
            Grid.SetColumn(smtcText, 1);
            Border smtc = new()
            {
                Background = Brush("#313244"),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(10, 8),
                Margin = new Thickness(0, 0, 0, 10),
                Child = new Grid
                {
                    ColumnDefinitions = new ColumnDefinitions("44,*"),
                    Children = { playBtn, smtcText }
                }
            };

            TextBlock volLabel = new()
            {
                FontSize = 12,
                Foreground = Brush("#a6adc8"),
                Margin = new Thickness(4, 0, 0, 8)
            };
            void UpdateVolLabel()
            {
                volLabel.Text = audio.IsMuted
                    ? "Volume · muted"
                    : $"Volume · {Math.Round(audio.MasterVolume * 100)}%";
            }
            UpdateVolLabel();
            void onAudioChanged(object? _1, EventArgs _2)
            {
                Dispatcher.UIThread.Post(UpdateVolLabel);
            }

            audio.Changed += onAudioChanged;

            WrapPanel tiles = new();
            if (settings.ShowMute)
            {
                tiles.Children.Add(Qs("Mute", () => audio.IsMuted = !audio.IsMuted));
            }

            tiles.Children.Add(Qs("Vol +", () => audio.MasterVolume = Math.Min(1, audio.MasterVolume + 0.05)));
            tiles.Children.Add(Qs("Vol −", () => audio.MasterVolume = Math.Max(0, audio.MasterVolume - 0.05)));
            if (brightness.IsSupported)
            {
                tiles.Children.Add(Qs("Bright +", () => brightness.Brightness = Math.Min(1, brightness.Brightness + 0.05)));
            }

            tiles.Children.Add(Qs("Media", () => Process.Start(new ProcessStartInfo
            {
                FileName = "ms-settings:appsfeatures-app",
                UseShellExecute = true
            })));
            tiles.Children.Add(Qs("Settings", () => Process.Start(new ProcessStartInfo
            {
                FileName = "ms-settings:",
                UseShellExecute = true
            })));

            Content = new Border
            {
                Background = Brush("#E6181825"),
                BorderBrush = Brush("#45475a"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(12),
                MinWidth = 340,
                Child = new StackPanel { Children = { smtc, volLabel, tiles } }
            };
            DetachedFromVisualTree += (_, _) =>
            {
                audio.Changed -= onAudioChanged;
                media.Changed -= onMediaChanged;
            };
        }

        private static Button Qs(string label, Action act)
        {
            Button b = new()
            {
                Content = label,
                Margin = new Thickness(4),
                Width = 140,
                Height = 52,
                Background = Brush("#313244"),
                Foreground = Brush("#cdd6f4"),
                CornerRadius = new CornerRadius(10)
            };
            b.Click += (_, _) => act();
            return b;
        }

        private static IBrush Brush(string hex)
        {
            return new SolidColorBrush(Color.Parse(hex));
        }
    }

    public sealed class SlateTileView : UserControl
    {
        private readonly TextBlock _clock = new()
        {
            FontWeight = FontWeight.Thin,
            Foreground = Brush("#cdd6f4"),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        private readonly TextBlock _date = new()
        {
            FontSize = 16,
            Foreground = Brush("#a6adc8"),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 8, 0, 0)
        };
        private readonly DispatcherTimer _timer;

        public SlateTileView()
        {
            SlateSettings settings = ModuleSettingsStore.Load("Slate", () => new SlateSettings());
            _clock.FontSize = settings.Style.Equals("Ninety", StringComparison.OrdinalIgnoreCase) ? 72
                : settings.Style.Equals("String", StringComparison.OrdinalIgnoreCase) ? 48 : 56;
            if (settings.Style.Equals("String", StringComparison.OrdinalIgnoreCase))
            {
                _clock.FontFamily = "Consolas,Courier New,monospace";
            }

            Content = new Border
            {
                Background = Brush("#CC0a0a12"),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(32, 24),
                MinWidth = 280,
                Child = new StackPanel
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Children =
                    {
                        _clock,
                        _date,
                        new TextBlock
                        {
                            Text = settings.HideOnFullscreen
                                ? "Idle · hide on fullscreen"
                                : "Idle",
                            FontSize = 13,
                            Foreground = Brush("#6c7086"),
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Margin = new Thickness(0, 12, 0, 0)
                        }
                    }
                }
            };
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += (_, _) => Tick();
            _timer.Start();
            Tick();
            DetachedFromVisualTree += (_, _) => _timer.Stop();
        }

        private void Tick()
        {
            DateTime now = DateTime.Now;
            _clock.Text = now.ToString("HH:mm");
            _date.Text = now.ToString("dddd, MMMM d");
        }

        private static IBrush Brush(string hex)
        {
            return new SolidColorBrush(Color.Parse(hex));
        }
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

        private static IBrush Brush(string hex)
        {
            return new SolidColorBrush(Color.Parse(hex));
        }
    }
}
