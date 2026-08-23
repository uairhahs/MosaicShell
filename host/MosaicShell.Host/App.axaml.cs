using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using MosaicShell.Core.Capabilities;
using MosaicShell.Core.Capabilities.BuiltIn;
using MosaicShell.Core.Capabilities.Ipc;
using MosaicShell.Core.Capabilities.Platform;
using MosaicShell.Core.Runtime;
using MosaicShell.Core.Services;
using MosaicShell.Host.Capabilities;
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
    private ICapabilityHost? _capabilityHost;
    private CapabilityDaemon? _localDaemon;
    private RemoteCapabilityHost? _remoteCapabilityHost;
    private CapabilityDaemonCoordinator? _daemonCoordinator;
    private CapabilityIpcFlyoutServer? _ipcFlyoutServer;
    private MainViewModel? _vm;
    private IHostUiBridge? _hostUi;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            _services = HostServices.CreateWindowsDefaults();

            var flyouts = new AvaloniaFlyoutPresenter(_services);
            _hostUi = new AvaloniaHostUiBridge(
                () => _tileRuntime!,
                () => _tileHost!,
                flyouts,
                _services,
                id =>
                {
                    _mainWindow?.Show();
                    _mainWindow?.Activate();
                    _vm?.OpenModuleConfigById(id);
                });
            flyouts.AttachHostUi(_hostUi);

            var uiBridge = new AvaloniaCapabilityUiBridge(flyouts, _hostUi);
            var registry = new CapabilityRegistry();
            BuiltInCapabilityFactories.RegisterAll(registry);

            _daemonCoordinator = new CapabilityDaemonCoordinator();
            if (_daemonCoordinator.TryAcquireOwner())
            {
                _localDaemon = new CapabilityDaemon(registry, _services, uiBridge);
                _capabilityHost = _localDaemon;
            }
            else
            {
                _remoteCapabilityHost = new RemoteCapabilityHost();
                if (_remoteCapabilityHost.TryConnect())
                    _capabilityHost = _remoteCapabilityHost;
            }

            _ipcFlyoutServer = new CapabilityIpcFlyoutServer(flyouts, uiBridge.RunOnHostThread);
            _ipcFlyoutServer.Start();

            _tileHost = new AvaloniaTileSurfaceHost(_services, _hostUi, id =>
                _tileRuntime?.NotifySurfaceClosed(id));
            _tileRuntime = new TileRuntime(new RestoringSurfaceHost(_tileHost));

            _vm = new MainViewModel(_tileRuntime, _services, _tileHost, _capabilityHost, _hostUi);
            _mainWindow = new MainWindow { DataContext = _vm, ShowActivated = false };
            _mainWindow.Closing += OnMainWindowClosing;
            desktop.MainWindow = _mainWindow;

            Dispatcher.UIThread.Post(async () =>
            {
                if (_localDaemon is not null)
                    await _localDaemon.RestoreAsync();
                _vm.RestoreSessions();
                _vm.RefreshArmedState();
                if (HostLaunchOptions.IsTrayOnly)
                    _mainWindow.Hide();
            });

            desktop.Exit += async (_, _) =>
            {
                await TeardownCapabilitiesAsync();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private async Task TeardownCapabilitiesAsync()
    {
        _tileHost?.PersistAll();
        _tileRuntime?.StopAll();
        if (_localDaemon is not null)
            await _localDaemon.DisarmAllAsync();
        _ipcFlyoutServer?.Dispose();
        _localDaemon?.Dispose();
        _remoteCapabilityHost?.Dispose();
        _daemonCoordinator?.Dispose();
        _services?.Dispose();
    }

    private void OnMainWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        if (Equals(_mainWindow?.Tag, "force-close")) return;

        var minimizeToTray = _vm?.Hub.CloseMinimizesToTray ?? true;
        if (minimizeToTray)
        {
            e.Cancel = true;
            _mainWindow?.Hide();
            _tileHost?.PersistAll();
            _capabilityHost?.Persist();
            return;
        }

        e.Cancel = true;
        _ = ExitApplicationAsync();
    }

    private async Task ExitApplicationAsync()
    {
        await TeardownCapabilitiesAsync();
        if (_mainWindow is not null)
        {
            _mainWindow.Tag = "force-close";
            _mainWindow.Closing -= OnMainWindowClosing;
            _mainWindow.Close();
        }
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.Shutdown();
    }

    private void TrayOpen_OnClick(object? sender, EventArgs e) => ShowMainWindow();
    private void TrayIcon_OnClicked(object? sender, EventArgs e) => ShowMainWindow();

    private async void TrayExit_OnClick(object? sender, EventArgs e) =>
        await ExitApplicationAsync();

    private void ShowMainWindow()
    {
        if (_mainWindow is null) return;
        if (!_mainWindow.IsVisible) _mainWindow.Show();
        _mainWindow.WindowState = WindowState.Normal;
        _mainWindow.Activate();
    }

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
