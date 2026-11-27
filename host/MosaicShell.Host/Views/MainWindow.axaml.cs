using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
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

        public MainWindow()
        {
            InitializeComponent();
            // The rail folds to icons on a narrow window (HubRail.CollapseBelowDip).
            SizeChanged += (_, e) => (DataContext as MainViewModel)?.SetWindowWidth(e.NewSize.Width);
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
            if (DataContext is not MainViewModel vm)
            {
                return;
            }

            try
            {
                DirectoryInfo folder = new(vm.DataFolderPath);
                folder.Create();
                bool opened = await Launcher.LaunchDirectoryInfoAsync(folder);
                if (!opened)
                {
                    vm.StatusMessage = $"Could not open {folder.FullName}.";
                }
            }
            catch (Exception ex)
            {
                vm.StatusMessage = $"Could not open the data folder: {ex.Message}";
            }
        }
    }
}
