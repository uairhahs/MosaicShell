using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform;
using MosaicShell.Core.Capabilities;
using MosaicShell.Core.Modules;
using MosaicShell.Core.Runtime;
using MosaicShell.Core.Services;
using MosaicShell.Host.Capabilities;
using MosaicShell.Host.Tiles.Surfaces;

namespace MosaicShell.Host.Tiles
{
    public sealed class AvaloniaTileSurfaceHost(
        HostServices services,
        IHostUiBridge hostUi,
        Action<string>? onClosedByUser = null) : ITileSurfaceHost
    {
        private readonly Dictionary<string, TileOverlayWindow> _windows = new(StringComparer.OrdinalIgnoreCase);
        private readonly HostServices _services = services;
        private readonly IHostUiBridge _hostUi = hostUi;
        private readonly Action<string>? _onClosedByUser = onClosedByUser;

        public bool Show(string moduleId, out string? error)
        {
            return Show(moduleId, null, out error);
        }

        public bool Show(string moduleId, TileSessionState? restore, out string? error)
        {
            try
            {
                if (_windows.ContainsKey(moduleId))
                {
                    Focus(moduleId);
                    error = null;
                    return true;
                }

                if (!ModuleCatalog.TryGet(moduleId, out ModuleInfo? info) || info is null)
                {
                    // Still allow overlays for installed folders even if discovery failed earlier.
                    if (!ModuleCatalog.IsInstalled(moduleId))
                    {
                        error = $"Unknown module '{moduleId}'.";
                        return false;
                    }

                    info = new ModuleInfo(
                        moduleId,
                        moduleId,
                        "Installed module.",
                        ModuleKind.Capability);
                }

                Control surface = TileSurfaceFactory.Create(info, _services);
                TileOverlayWindow window = new(info, surface, _hostUi);
                window.Closed += (_, _) =>
                {
                    PersistAll();
                    _ = _windows.Remove(moduleId);
                    _onClosedByUser?.Invoke(moduleId);
                };
                window.PropertyChanged += (_, e) =>
                {
                    if (e.Property == Window.WindowStateProperty
                        || e.Property == Layoutable.WidthProperty
                        || e.Property == Layoutable.HeightProperty)
                    {
                        PersistAll();
                    }
                };

                if (restore is not null)
                {
                    window.Width = Math.Max(window.MinWidth, restore.Width);
                    window.Height = Math.Max(window.MinHeight, restore.Height);
                    window.Position = new PixelPoint(restore.X, restore.Y);
                }
                else
                {
                    int offset = _windows.Count * 28;
                    window.Position = new PixelPoint(80 + offset, 80 + offset);
                }

                window.Show();
                _windows[moduleId] = window;
                PersistAll();
                error = null;
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public void Focus(string moduleId)
        {
            if (!_windows.TryGetValue(moduleId, out TileOverlayWindow? window))
            {
                return;
            }

            if (!window.IsVisible)
            {
                window.Show();
            }

            if (window.IsDesktopWidget)
            {
                window.SendToDesktop();
            }
            else
            {
                window.BringToFront();
                window.Activate();
            }
        }

        public void Close(string moduleId)
        {
            if (!_windows.TryGetValue(moduleId, out TileOverlayWindow? window))
            {
                return;
            }

            _ = _windows.Remove(moduleId);
            window.Close();
            PersistAll();
        }

        public void CloseAll()
        {
            foreach (string? id in _windows.Keys.ToList())
            {
                Close(id);
            }
        }

        public void Refresh(string moduleId)
        {
            if (!_windows.TryGetValue(moduleId, out TileOverlayWindow? window))
            {
                return;
            }

            TileSessionState state = new(
                moduleId, window.Position.X, window.Position.Y, window.Width, window.Height);
            Close(moduleId);
            _ = Show(moduleId, state, out _);
        }

        public IReadOnlyList<string> OpenModuleIds => [.. _windows.Keys];

        public void PersistAll()
        {
            List<TileSessionState> states = [.. _windows.Values.Select(w => new TileSessionState(
                w.ModuleId,
                w.Position.X,
                w.Position.Y,
                w.Width,
                w.Height))];
            SessionStore.Save(states);
        }
    }

    /// <summary>
    /// Borderless desktop/capability frame. Content fills the shell (no nested title chrome).
    /// Desktop-widget chrome: drag whole surface; manage via right-click Ctx (align / Z / configure / close).
    /// </summary>
    public sealed class TileOverlayWindow : Window
    {
        public string ModuleId { get; }
        public bool IsDesktopWidget { get; }
        private readonly IHostUiBridge _hostUi;
        private bool _stuckToDesktop;

        public TileOverlayWindow(ModuleInfo info, Control surface, IHostUiBridge hostUi)
        {
            ModuleId = info.Id;
            _hostUi = hostUi;
            IsDesktopWidget = info.Kind == ModuleKind.Widget;
            _stuckToDesktop = IsDesktopWidget
                || info.Id.Equals("Pulse", StringComparison.OrdinalIgnoreCase);

            Title = $"MosaicShell: {info.DisplayName}";
            ApplyDefaultSize(info);
            MinWidth = 160;
            MinHeight = 100;
            CanResize = true;
            WindowDecorations = WindowDecorations.None;
            Topmost = !IsDesktopWidget && !_stuckToDesktop;
            ShowInTaskbar = false;
            TransparencyLevelHint = [WindowTransparencyLevel.Transparent];
            Background = Brushes.Transparent;
            // docs: OS may suppress transparency (battery saver / RDP)
            TransparencyBackgroundFallback = new SolidColorBrush(Color.Parse("#1e1e2e"));

            Border shell = new()
            {
                Background = new SolidColorBrush(Color.Parse("#E61e1e2e")),
                CornerRadius = new CornerRadius(12),
                BorderBrush = new SolidColorBrush(Color.Parse("#45475a")),
                BorderThickness = new Thickness(1),
                ClipToBounds = true,
                // Content is the frame - no title strip.
                Child = new Border
                {
                    Padding = new Thickness(IsDesktopWidget ? 12 : 14),
                    Child = surface
                }
            };
            shell.PointerPressed += OnSurfacePointerPressed;
            shell.ContextMenu = BuildContextMenu();
            Content = shell;

            KeyDown += (_, e) =>
            {
                if (e.Key == Key.Escape && !IsDesktopWidget
                    && ModuleOverlaySettings.CloseOnEscape(ModuleId))
                {
                    e.Handled = true;
                    Close();
                }
            };
        }

        private void ApplyDefaultSize(ModuleInfo info)
        {
            if (info.Id.Equals("Canvas", StringComparison.OrdinalIgnoreCase))
            {
                Width = 340;
                Height = 420;
                return;
            }

            if (info.Kind == ModuleKind.Widget)
            {
                Width = 340;
                Height = 300;
                return;
            }

            Width = 420;
            Height = 360;
        }

        public void SendToDesktop()
        {
            _stuckToDesktop = true;
            Topmost = false;
            // Avalonia Topmost cannot place HWND below other windows; SetWindowPos required.
            Win32WindowChrome.SetZOrder(this, Win32WindowChrome.HwndBottom);
        }

        public void BringToFront()
        {
            _stuckToDesktop = false;
            Topmost = true;
            Win32WindowChrome.SetZOrder(this, Win32WindowChrome.HwndTopmost);
            Activate();
        }

        public void SetNormalZ()
        {
            _stuckToDesktop = false;
            Topmost = false;
            Win32WindowChrome.SetZOrder(this, Win32WindowChrome.HwndNoTopmost);
        }

        public void AlignTo(AlignPreset preset)
        {
            Screen? screen = Screens?.ScreenFromWindow(this) ?? Screens?.Primary;
            if (screen is null)
            {
                return;
            }

            PixelRect wa = screen.WorkingArea;
            double scale = screen.Scaling > 0.1 ? screen.Scaling : 1.0;
            int w = (int)Math.Round(Bounds.Width * scale);
            int h = (int)Math.Round(Bounds.Height * scale);
            int x = preset switch
            {
                AlignPreset.TopLeft or AlignPreset.BottomLeft => wa.X + 16,
                AlignPreset.TopRight or AlignPreset.BottomRight => wa.X + wa.Width - w - 16,
                AlignPreset.Center or AlignPreset.HorizontalCenter or AlignPreset.VerticalCenter
                    or AlignPreset.TopCenter or AlignPreset.BottomCenter =>
                    wa.X + ((wa.Width - w) / 2),
                _ => Position.X
            };
            int y = preset switch
            {
                AlignPreset.TopLeft or AlignPreset.TopRight or AlignPreset.TopCenter => wa.Y + 16,
                AlignPreset.BottomLeft or AlignPreset.BottomRight or AlignPreset.BottomCenter =>
                    wa.Y + wa.Height - h - 16,
                AlignPreset.Center or AlignPreset.HorizontalCenter or AlignPreset.VerticalCenter =>
                    wa.Y + ((wa.Height - h) / 2),
                _ => Position.Y
            };
            if (preset == AlignPreset.HorizontalCenter)
            {
                y = Position.Y;
            }

            if (preset == AlignPreset.VerticalCenter)
            {
                x = Position.X;
            }

            Position = new PixelPoint(x, y);
        }

        protected override void OnOpened(EventArgs e)
        {
            base.OnOpened(e);
            if (_stuckToDesktop)
            {
                SendToDesktop();
            }
        }

        private ContextMenu BuildContextMenu()
        {
            ContextMenu menu = new();

            MenuItem configure = new() { Header = "Configure in Host" };
            configure.Click += (_, _) => _hostUi.OpenModuleConfig(ModuleId);
            _ = menu.Items.Add(configure);

            MenuItem align = new() { Header = "Align" };
            _ = align.Items.Add(AlignItem("Center", AlignPreset.Center));
            _ = align.Items.Add(AlignItem("Horizontally centered", AlignPreset.HorizontalCenter));
            _ = align.Items.Add(AlignItem("Vertically centered", AlignPreset.VerticalCenter));
            _ = align.Items.Add(new Separator());
            _ = align.Items.Add(AlignItem("Top left", AlignPreset.TopLeft));
            _ = align.Items.Add(AlignItem("Top center", AlignPreset.TopCenter));
            _ = align.Items.Add(AlignItem("Top right", AlignPreset.TopRight));
            _ = align.Items.Add(AlignItem("Bottom left", AlignPreset.BottomLeft));
            _ = align.Items.Add(AlignItem("Bottom center", AlignPreset.BottomCenter));
            _ = align.Items.Add(AlignItem("Bottom right", AlignPreset.BottomRight));
            _ = menu.Items.Add(align);

            MenuItem z = new() { Header = "Change Z layer" };
            MenuItem desk = new() { Header = "Desktop (behind windows)" };
            desk.Click += (_, _) => SendToDesktop();
            MenuItem normal = new() { Header = "Normal" };
            normal.Click += (_, _) => SetNormalZ();
            MenuItem top = new() { Header = "Always on top" };
            top.Click += (_, _) => BringToFront();
            _ = z.Items.Add(desk);
            _ = z.Items.Add(normal);
            _ = z.Items.Add(top);
            _ = menu.Items.Add(z);

            _ = menu.Items.Add(new Separator());

            MenuItem refresh = new() { Header = "Refresh" };
            refresh.Click += (_, _) => _hostUi.RefreshOverlay(ModuleId);
            _ = menu.Items.Add(refresh);

            MenuItem close = new() { Header = "Unload" };
            close.Click += (_, _) => Close();
            _ = menu.Items.Add(close);

            return menu;
        }

        private MenuItem AlignItem(string header, AlignPreset preset)
        {
            MenuItem item = new() { Header = header };
            item.Click += (_, _) => AlignTo(preset);
            return item;
        }

        private void OnSurfacePointerPressed(object? sender, PointerPressedEventArgs e)
        {
            // Drag from empty chrome / non-interactive padding; don't steal button/slider drags.
            if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                return;
            }

            if (e.Source is Button or Slider or TextBox or ComboBox)
            {
                return;
            }

            if (e.Source is Control c && (c is TextBox || AncestorsContainInteractive(c)))
            {
                return;
            }

            BeginMoveDrag(e);
        }

        private static bool AncestorsContainInteractive(Control control)
        {
            for (StyledElement? p = control.Parent; p is not null; p = p.Parent)
            {
                if (p is Button or Slider or TextBox or ComboBox or ScrollViewer)
                {
                    return true;
                }
            }
            return false;
        }
    }

    public enum AlignPreset
    {
        Center,
        HorizontalCenter,
        VerticalCenter,
        TopLeft,
        TopCenter,
        TopRight,
        BottomLeft,
        BottomCenter,
        BottomRight
    }
}
