using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using MosaicShell.Core.HostPlatform;
using MosaicShell.Core.Services;
using MosaicShell.Host.ViewModels;

namespace MosaicShell.Host.Views
{
    /// <summary>
    /// The Hub window keeps the size the user gives it when it moves to another monitor (<see cref="HostWindowFitPolicy"/>):
    /// sizes are in device-independent pixels, so the OS handles a scaling change, and the window is only shrunk when it no
    /// longer fits the work area, once a move has settled. It is never resized during a drag.
    /// </summary>
    public partial class MainWindow : Window
    {
        private DispatcherTimer? _fitTimer;
        private EventHandler? _screensChanged;
        private MainViewModel? _watched;

        /// <summary>A page opened since a control last had focus: Tab starts on that page, not back at the rail.</summary>
        private bool _pageChangedSinceFocus;

        /// <summary>The last input was a key, not the pointer: a page opened by Enter takes focus onto itself.</summary>
        private bool _lastInputWasKeyboard;

        /// <summary>Where focus last was on the page, so Right from the rail returns there.</summary>
        private InputElement? _lastPageFocus;

        public MainWindow()
        {
            InitializeComponent();
            // The rail folds to icons on a narrow window (HubRail.CollapseBelowDip).
            SizeChanged += (_, e) => (DataContext as MainViewModel)?.SetWindowWidth(e.NewSize.Width);
            AddHandler(KeyDownEvent, OnKeyDownTunnel, RoutingStrategies.Tunnel);
            AddHandler(PointerPressedEvent, (_, _) => _lastInputWasKeyboard = false, RoutingStrategies.Tunnel, handledEventsToo: true);
            AddHandler(GotFocusEvent, OnGotFocus, RoutingStrategies.Bubble, handledEventsToo: true);
            // After the page's own arrow handling (XY focus): a Left nothing on the page took goes to the rail.
            AddHandler(KeyDownEvent, OnKeyDownBubble, RoutingStrategies.Bubble);
            DataContextChanged += (_, _) => WatchPages();
        }

        private void OnGotFocus(object? sender, FocusChangedEventArgs e)
        {
            _pageChangedSinceFocus = false;
            if (e.Source is InputElement focused && Pages.IsVisualAncestorOf(focused))
            {
                _lastPageFocus = focused;
            }
        }

        private void OnKeyDownBubble(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Left && FocusManager?.GetFocusedElement() is Visual focused && Pages.IsVisualAncestorOf(focused)
                && OpenRailItem() is { } rail)
            {
                _ = rail.Focus(NavigationMethod.Directional);
                e.Handled = true;
            }
        }

        /// <summary>
        /// The other control in a card row: from the card (<c>Button.listRow</c>) going right, its action; from the action
        /// going left, the card. Rows are a Grid holding the card button and one action on top of it.
        /// </summary>
        private static Control? RowPartner(Control focused, bool right)
        {
            if (focused.GetVisualParent() is not Grid row)
            {
                return null;
            }

            Control? card = row.Children.OfType<Button>().FirstOrDefault(b => b.Classes.Contains("listRow"));
            Control? action = row.Children.FirstOrDefault(c => c != card && c is Button or ToggleSwitch && IsReachable(c));
            return card is null || action is null ? null
                : right && focused == card ? action
                : !right && focused == action ? card
                : null;
        }

        /// <summary>The rail item of the page that is open (a module's page belongs to Modules or Widgets).</summary>
        private Button? OpenRailItem()
        {
            return Rail.GetVisualDescendants().OfType<Button>().FirstOrDefault(b => b.Classes.Contains("rail") && b.Classes.Contains("selected") && IsReachable(b));
        }

        private void WatchPages()
        {
            _watched?.PropertyChanged -= OnViewModelChanged;
            _watched = DataContext as MainViewModel;
            _watched?.PropertyChanged += OnViewModelChanged;
        }

        private void OnViewModelChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(MainViewModel.SelectedPage) or nameof(MainViewModel.ModulePage))
            {
                _pageChangedSinceFocus = true;
                if (_lastInputWasKeyboard)
                {
                    // Enter on a card: the card goes away with its list, so focus would be left nowhere. Once the page
                    // is laid out, start on it, as Tab would.
                    Dispatcher.UIThread.Post(FocusOpenedPage, DispatcherPriority.Loaded);
                }
            }
        }

        private void FocusOpenedPage()
        {
            if (!HasUsableFocus() && FirstOnPage() is { } start)
            {
                _ = start.Focus(NavigationMethod.Tab);
            }
        }

        /// <summary>
        /// Tab with no usable focus starts somewhere: on the page that just opened, else at the rail. A control that
        /// went away with its page (the list row that opened a module) keeps focus in Avalonia, and Tab cannot move
        /// on from it, so Tab did nothing (keyboard pass, 2026-11-28). Up and Down move along the rail; Right enters the page.
        /// </summary>
        private void OnKeyDownTunnel(object? sender, KeyEventArgs e)
        {
            _lastInputWasKeyboard = true;
            if (e.Key == Key.Tab && !HasUsableFocus())
            {
                InputElement? start = (_pageChangedSinceFocus ? FirstOnPage() : null) ?? FirstFocusable(Rail);
                if (start is not null)
                {
                    _ = start.Focus(NavigationMethod.Tab);
                    e.Handled = true;
                }

                return;
            }

            // A card row: its Turn on, Turn off or Add button (or Home's switch) sits inside the card's bounds, where
            // XY focus never looks. Right goes to it, Left comes back to the card.
            if (e.Key is Key.Right or Key.Left && FocusManager?.GetFocusedElement() is Control inRow
                && RowPartner(inRow, e.Key == Key.Right) is { } partner)
            {
                _ = partner.Focus(NavigationMethod.Directional);
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Right && FocusManager?.GetFocusedElement() is Button { Classes: var railClasses } && railClasses.Contains("rail"))
            {
                InputElement? target = _lastPageFocus is { } last && IsReachable(last) ? last : FirstOnPage();
                if (target is not null)
                {
                    _ = target.Focus(NavigationMethod.Directional);
                }

                e.Handled = true;
                return;
            }

            if (e.Key is Key.Up or Key.Down && FocusManager?.GetFocusedElement() is Button { Classes: var classes } current
                && classes.Contains("rail"))
            {
                List<Button> rail = [.. Rail.GetVisualDescendants().OfType<Button>().Where(b => b.Classes.Contains("rail") && IsReachable(b))];
                int at = rail.IndexOf(current);
                int next = at + (e.Key == Key.Down ? 1 : -1);
                if (at >= 0 && next >= 0 && next < rail.Count)
                {
                    _ = rail[next].Focus(NavigationMethod.Directional);
                }

                e.Handled = true;
            }
        }

        private bool HasUsableFocus()
        {
            return FocusManager?.GetFocusedElement() is Visual focused && focused != this && IsReachable(focused);
        }

        private static bool IsReachable(Visual visual)
        {
            return visual.IsAttachedToVisualTree() && visual.IsEffectivelyVisible
                && (visual is not InputElement input || input.IsEffectivelyEnabled);
        }

        /// <summary>The page's first control in Tab's own order, else the first in the visual tree.</summary>
        private InputElement? FirstOnPage()
        {
            return Avalonia.Input.FocusManager.FindFirstFocusableElement(Pages) is InputElement element && IsReachable(element)
                ? element
                : FirstFocusable(Pages);
        }

        private static InputElement? FirstFocusable(Visual root)
        {
            return root.GetVisualDescendants().OfType<InputElement>().FirstOrDefault(e =>
                e.Focusable && KeyboardNavigation.GetIsTabStop(e) && IsReachable(e));
        }

        protected override void OnOpened(EventArgs e)
        {
            base.OnOpened(e);
            _fitTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(HostWindowFitPolicy.SettleMs) };
            _fitTimer.Tick += (_, _) =>
            {
                _fitTimer.Stop();
                FitToScreen();
            };

            ScalingChanged += OnMovedOrRescaled;
            PositionChanged += OnMovedOrRescaled;
            if (Screens is not null)
            {
                _screensChanged = OnMovedOrRescaled;
                Screens.Changed += _screensChanged;
            }

            FitToScreen();
        }

        protected override void OnClosed(EventArgs e)
        {
            _fitTimer?.Stop();
            ScalingChanged -= OnMovedOrRescaled;
            PositionChanged -= OnMovedOrRescaled;
            if (Screens is not null && _screensChanged is not null)
            {
                Screens.Changed -= _screensChanged;
            }

            base.OnClosed(e);
        }

        /// <summary>Restarts the settle timer, so the fit runs once, after the last move of a drag.</summary>
        private void OnMovedOrRescaled(object? sender, EventArgs e)
        {
            _fitTimer?.Stop();
            _fitTimer?.Start();
        }

        private void FitToScreen()
        {
            Screen? screen = Screens?.ScreenFromWindow(this) ?? Screens?.Primary;
            if (screen is null)
            {
                return;
            }

            double scaling = screen.Scaling > 0.1 ? screen.Scaling : 1.0;
            double workW = screen.WorkingArea.Width / scaling;
            double workH = screen.WorkingArea.Height / scaling;
            double w = double.IsNaN(Width) ? ClientSize.Width : Width;
            double h = double.IsNaN(Height) ? ClientSize.Height : Height;
            if (!HostWindowFitPolicy.NeedsFit(w, h, workW, workH, MinWidth, MinHeight))
            {
                return;
            }

            (double fitW, double fitH) = HostWindowFitPolicy.Fit(w, h, workW, workH, MinWidth, MinHeight);
            if (fitW < MinWidth || fitH < MinHeight)
            {
                // A work area smaller than the design minimum: let the window follow it down.
                MinWidth = Math.Min(MinWidth, fitW);
                MinHeight = Math.Min(MinHeight, fitH);
            }

            Width = fitW;
            Height = fitH;
            DiagnosticLog.Append(
                "host-size.log",
                $"fit {w:0}x{h:0} -> {fitW:0}x{fitH:0}dip work={workW:0}x{workH:0}dip scale={scaling:0.##}");
        }

        private async void OnAddPackageZipClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            IReadOnlyList<Avalonia.Platform.Storage.IStorageFile> files = await StorageProvider.OpenFilePickerAsync(new()
            {
                Title = "Add a module from a .zip package",
                AllowMultiple = false,
                FileTypeFilter = [new("Module package") { Patterns = ["*.zip"] }],
            });
            if (files.Count == 1 && files[0].TryGetLocalPath() is string path && DataContext is MainViewModel vm)
            {
                await vm.AddFromPackageAsync(path);
            }
        }

        private async void OnAddPackageFolderClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            IReadOnlyList<Avalonia.Platform.Storage.IStorageFolder> folders = await StorageProvider.OpenFolderPickerAsync(new()
            {
                Title = "Add a module from a folder with module.manifest.json",
                AllowMultiple = false,
            });
            if (folders.Count == 1 && folders[0].TryGetLocalPath() is string path && DataContext is MainViewModel vm)
            {
                await vm.AddFromPackageAsync(path);
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (DataContext is MainViewModel { ModulePage.CapturingHotkey: { } row } && row.TryCapture(e))
            {
                return;
            }

            base.OnKeyDown(e);
        }

        /// <summary>Opens the data folder in the OS file manager (Explorer, Dolphin, Files) through Avalonia's launcher.</summary>
        private async void OnOpenDataFolderClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                await OpenFolderAsync(vm, vm.DataFolderPath, "the data folder");
            }
        }

        private async void OnOpenLogFolderClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                await OpenFolderAsync(vm, vm.LogFolderPath, "the log folder");
            }
        }

        private async Task OpenFolderAsync(MainViewModel vm, string path, string what)
        {
            try
            {
                DirectoryInfo folder = new(path);
                folder.Create();
                bool opened = await Launcher.LaunchDirectoryInfoAsync(folder);
                if (!opened)
                {
                    vm.StatusMessage = $"Could not open {folder.FullName}.";
                }
            }
            catch (Exception ex)
            {
                vm.StatusMessage = $"Could not open {what}: {ex.Message}";
            }
        }
    }
}
