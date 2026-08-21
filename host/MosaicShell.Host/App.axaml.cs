using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using MosaicShell.Core.Runtime;
using MosaicShell.Core.Scale;
using MosaicShell.Core.Services;
using MosaicShell.Host.Tiles;
using MosaicShell.Host.ViewModels;
using MosaicShell.Host.Views;

namespace MosaicShell.Host;

public partial class App : Application
{
    private MainWindow? _mainWindow;
    private TileRuntime? _tileRuntime;
    private AvaloniaTileSurfaceHost? _tileHost;
    private HostServices? _services;
    private MainViewModel? _vm;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            _services = HostServices.CreateWindowsDefaults();

            double UserScale() => _vm?.UserScale ?? 1.0;

            _tileHost = new AvaloniaTileSurfaceHost(_services, UserScale, id =>
                _tileRuntime?.NotifySurfaceClosed(id));
            _tileRuntime = new TileRuntime(new RestoringSurfaceHost(_tileHost));

            _vm = new MainViewModel(_tileRuntime, _services, _tileHost);
            _mainWindow = new MainWindow { DataContext = _vm };
            _mainWindow.Closing += OnMainWindowClosing;
            desktop.MainWindow = _mainWindow;

            Dispatcher.UIThread.Post(() => _vm.RestoreSessions());

            desktop.Exit += (_, _) =>
            {
                _tileHost.PersistAll();
                _tileRuntime.StopAll();
                _services.Dispose();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void OnMainWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        if (Equals(_mainWindow?.Tag, "force-close")) return;
        e.Cancel = true;
        _mainWindow?.Hide();
        _tileHost?.PersistAll();
    }

    private void TrayOpen_OnClick(object? sender, EventArgs e) => ShowMainWindow();
    private void TrayIcon_OnClicked(object? sender, EventArgs e) => ShowMainWindow();

    private void TrayExit_OnClick(object? sender, EventArgs e)
    {
        _tileHost?.PersistAll();
        _tileRuntime?.StopAll();
        _services?.Dispose();
        if (_mainWindow is not null)
        {
            _mainWindow.Tag = "force-close";
            _mainWindow.Closing -= OnMainWindowClosing;
            _mainWindow.Close();
        }
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.Shutdown();
    }

    private void ShowMainWindow()
    {
        if (_mainWindow is null) return;
        if (!_mainWindow.IsVisible) _mainWindow.Show();
        _mainWindow.WindowState = WindowState.Normal;
        _mainWindow.Activate();
    }

    /// <summary>Wraps host so TileRuntime.Show restores last geometry when present.</summary>
    private sealed class RestoringSurfaceHost(AvaloniaTileSurfaceHost inner) : ITileSurfaceHost
    {
        public bool Show(string moduleId, out string? error)
        {
            var prior = SessionStore.Load()
                .FirstOrDefault(s => s.ModuleId.Equals(moduleId, StringComparison.OrdinalIgnoreCase));
            return inner.Show(moduleId, prior, out error);
        }

        public void Focus(string moduleId) => inner.Focus(moduleId);
        public void Close(string moduleId) => inner.Close(moduleId);
    }
}
