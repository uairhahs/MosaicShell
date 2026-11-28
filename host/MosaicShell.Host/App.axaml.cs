using Avalonia;

using Avalonia.Controls;

using Avalonia.Controls.ApplicationLifetimes;

using Avalonia.Markup.Xaml;

using Avalonia.Threading;

using MosaicShell.Core.Capabilities;

using MosaicShell.Core.Capabilities.BuiltIn;

using MosaicShell.Core.Capabilities.Ipc;

using MosaicShell.Core.Capabilities.Platform;

using MosaicShell.Core.HostPlatform;
using MosaicShell.Core.HubShell;

using MosaicShell.Core.Runtime;

using MosaicShell.Core.Services;

using MosaicShell.Host.Capabilities;

using MosaicShell.Host.Tiles;

using MosaicShell.Host.ViewModels;

using MosaicShell.Host.Views;



namespace MosaicShell.Host
{


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
        private IDisposable? _hubPresence;

        /// <summary>
        /// The module switches as of the last change, written on the UI thread and read by GNOME's bus thread without
        /// blocking on the UI thread.
        /// </summary>
        private IReadOnlyList<TrayModule> _trayModules = [];

        private TrayPanel? _trayPanel;

        /// <summary>The tray's one menu, and its module items by id in menu order.</summary>
        private NativeMenu? _trayMenu;

        private readonly OrderedDictionary<string, NativeMenuItem> _trayItems = [with(StringComparer.OrdinalIgnoreCase)];

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

                _services = HostServices.CreatePlatformDefaults(browserSources: true);



                AvaloniaFlyoutPresenter flyouts = new(_services);

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



                AvaloniaCapabilityUiBridge uiBridge = new(flyouts, _hostUi);

                CapabilityRegistry registry = new();

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
                    {
                        _capabilityHost = _remoteCapabilityHost;
                    }
                }



                _ipcFlyoutServer = new CapabilityIpcFlyoutServer(flyouts, uiBridge.RunOnHostThread);

                _ipcFlyoutServer.Start();



                _tileHost = new AvaloniaTileSurfaceHost(_services, _hostUi, id =>

                    _tileRuntime?.NotifySurfaceClosed(id));

                _tileRuntime = new TileRuntime(new RestoringSurfaceHost(_tileHost));



                _vm = new MainViewModel(_tileRuntime, _services, _tileHost, _capabilityHost, _hostUi);

                // Posted, so the menu is never changed from inside the click that changed a module.
                _vm.ModuleStatesChanged += (_, _) => Dispatcher.UIThread.Post(RebuildTrayMenu);
                RebuildTrayMenu();
                _mainWindow = new MainWindow { DataContext = _vm, ShowActivated = false };

                _mainWindow.Closing += OnMainWindowClosing;

                desktop.MainWindow = _mainWindow;



                Dispatcher.UIThread.Post(async () =>

                {

                    if (_localDaemon is not null)
                    {
                        await _localDaemon.RestoreAsync();
                    }

                    _vm.RestoreSessions();

                    _vm.RefreshArmedState();
                    await _vm.RunFirstRunIfDueAsync(Program.SettingsSavedAtStart);

                    // The first run screen shows even when started for the tray: it is how a new install says it is ready.
                    if (HostLaunchOptions.IsTrayOnly && !_vm.IsFirstRunPage)
                    {
                        _mainWindow.Hide();
                    }
                });



                // GNOME has no tray: MosaicShell's icon in its top bar opens the Hub or quits instead.
                _hubPresence = HostServices.StartHubPresence(
                    () => Dispatcher.UIThread.Post(ShowHubOnRequest),
                    () => Dispatcher.UIThread.Post(() => _ = ExitApplicationAsync()));

                desktop.Exit += async (_, _) =>

                {

                    _hubPresence?.Dispose();
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
            {
                // Stop the modules but keep the saved armed list: it is what arms them again on the next start.
                await _localDaemon.DisarmAllAsync(persist: false);
            }

            _ipcFlyoutServer?.Dispose();

            _localDaemon?.Dispose();

            _remoteCapabilityHost?.Dispose();

            _daemonCoordinator?.Dispose();

            _services?.Dispose();

        }



        private void OnMainWindowClosing(object? sender, WindowClosingEventArgs e)

        {

            if (Equals(_mainWindow?.Tag, "force-close"))
            {
                return;
            }

            bool minimizeToTray = _vm?.Hub.CloseMinimizesToTray ?? true;

            MainWindowCloseTrigger trigger = e.CloseReason switch
            {
                WindowCloseReason.OSShutdown => MainWindowCloseTrigger.SessionEnding,
                WindowCloseReason.ApplicationShutdown => MainWindowCloseTrigger.ApplicationShutdown,
                _ => MainWindowCloseTrigger.User,
            };

            MainWindowCloseAction action = MainWindowClosePolicy.Decide(trigger, minimizeToTray);

            if (action == MainWindowCloseAction.AllowClose)
            {
                // Logout or app shutdown: cancelling would veto it. Save now; the lifetime's Exit runs teardown.
                _tileHost?.PersistAll();
                _capabilityHost?.Persist();
                return;
            }

            if (action == MainWindowCloseAction.HideToTray)

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
            {
                desktop.Shutdown();
            }
        }



        /// <summary>Left-click: the quick panel, which stays open while modules are switched. Right-click: the menu.</summary>
        private void TrayIcon_OnClicked(object? sender, EventArgs e)
        {
            if (_vm is null)
            {
                ShowMainWindow();
                return;
            }

            if (_trayPanel is null)
            {
                _trayPanel = new TrayPanel { DataContext = _vm };
                _trayPanel.OpenRequested += (_, _) => ShowMainWindow();
                _trayPanel.QuitRequested += async (_, _) => await ExitApplicationAsync();
            }

            _trayPanel.ShowByPointer();
        }

        /// <summary>
        /// The tray menu (U24): a switch per installed module, so it turns on and off without the Hub, then Open and
        /// Quit; GNOME's top bar menu reads the same list. One menu for the Host's life: a change ticks its items in
        /// place, and only a change in which modules are installed lists them again. Replacing the whole menu on every
        /// toggle, from inside the click that caused it, made it disappear.
        /// </summary>
        private void RebuildTrayMenu()
        {
            if (_vm is null)
            {
                return;
            }

            IReadOnlyList<TrayModule> modules = _vm.TrayModules();
            Volatile.Write(ref _trayModules, modules);

            if (_trayMenu is null)
            {
                _trayMenu = [];
                if (TrayIcon.GetIcons(this) is { Count: > 0 } icons)
                {
                    icons[0].Menu = _trayMenu;
                }
            }

            if (modules.Select(m => m.Id).SequenceEqual(_trayItems.Keys))
            {
                foreach (TrayModule module in modules)
                {
                    _trayItems[module.Id].IsChecked = module.IsOn;
                }

                return;
            }

            _trayMenu.Items.Clear();
            _trayItems.Clear();
            foreach (TrayModule module in modules)
            {
                string id = module.Id;
                NativeMenuItem item = new(module.Label) { ToggleType = MenuItemToggleType.CheckBox, IsChecked = module.IsOn };
                // The module's state when clicked, not when the menu was built.
                item.Click += (_, _) => Dispatcher.UIThread.Post(async () =>
                {
                    bool on = Volatile.Read(ref _trayModules).FirstOrDefault(m => m.Id == id)?.IsOn ?? false;
                    await _vm.SetModuleOnAsync(id, !on);
                });
                _trayItems.Add(id, item);
                _trayMenu.Add(item);
            }

            if (modules.Count > 0)
            {
                _trayMenu.Add(new NativeMenuItemSeparator());
            }

            NativeMenuItem open = new(TrayMenu.OpenLabel);
            open.Click += (_, _) => ShowMainWindow();
            _trayMenu.Add(open);
            NativeMenuItem quit = new(TrayMenu.QuitLabel);
            quit.Click += async (_, _) => await ExitApplicationAsync();
            _trayMenu.Add(quit);
        }

        /// <summary>A second launch asked this Host to show itself (<see cref="HostSingleInstance"/>).</summary>
        internal void ShowHubOnRequest()
        {
            ShowMainWindow();
        }

        private void ShowMainWindow()

        {

            if (_mainWindow is null)
            {
                return;
            }

            if (!_mainWindow.IsVisible)
            {
                _mainWindow.Show();
            }

            _mainWindow.WindowState = WindowState.Normal;

            _mainWindow.Activate();

        }



        private sealed class RestoringSurfaceHost(AvaloniaTileSurfaceHost inner) : ITileSurfaceHost

        {

            public bool Show(string moduleId, out string? error)

            {

                TileSessionState? prior = SessionStore.Load()

                    .FirstOrDefault(s => s.ModuleId.Equals(moduleId, StringComparison.OrdinalIgnoreCase));

                return inner.Show(moduleId, prior, out error);

            }



            public void Focus(string moduleId)
            {
                inner.Focus(moduleId);
            }

            public void Close(string moduleId)
            {
                inner.Close(moduleId);
            }
        }

    }


}
