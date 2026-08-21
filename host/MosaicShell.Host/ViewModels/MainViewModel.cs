using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Material.Icons;
using MosaicShell.Core;
using MosaicShell.Core.Capabilities;
using MosaicShell.Core.Install;
using MosaicShell.Core.Modules;
using MosaicShell.Core.Runtime;
using MosaicShell.Core.Scale;
using MosaicShell.Core.Services;
using MosaicShell.Core.Settings;
using MosaicShell.Core.Shp;
using MosaicShell.Core.Styles;
using MosaicShell.Core.Update;
using MosaicShell.Host.Tiles;
using System.Collections.ObjectModel;

namespace MosaicShell.Host.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly ScaleContract _scale;
    private readonly ModuleInstaller _installer = new();
    private readonly ITileRuntime _runtime;
    private readonly ModuleLauncher _launcher;
    private readonly HostServices _services;
    private readonly AvaloniaTileSurfaceHost? _tileHost;
    private readonly CapabilityDaemon? _daemon;

    public MainViewModel(
        ITileRuntime runtime,
        HostServices services,
        AvaloniaTileSurfaceHost? tileHost,
        CapabilityDaemon? daemon = null)
    {
        _runtime = runtime;
        _launcher = new ModuleLauncher(runtime);
        _services = services;
        _tileHost = tileHost;
        _daemon = daemon;

        AppPaths.EnsureLayout();
        var settings = ScaleSettingsStore.Load();
        settings.DpiScale = DpiProbe.GetDpiScale();
        _scale = ScaleContract.FromSettings(settings);
        ScaleSettingsStore.Save(_scale.ToSettings());
        Hub = ModuleSettingsStore.Load("Hub", () => new HubSettings());

        HomeCards =
        [
            new("Welcome", "First-run picks, batch install, startup.", "Welcome", "/Assets/Modules/Inlay.png"),
            new("Tiles", "Install widgets or set tiles (Tessera flyouts, launchers).", "Tiles", "/Assets/Modules/Tessera.png"),
            new("About", "MosaicShell is a native host re-write. The app allows for desktop customisation and tool suite to tailor your experience and relies solely on the background CapabilityDaemon for persistence. Th app is fully self-contained and extensible.", "About", "/Assets/MosaicShell.png"),
        ];

        ModuleStyleOptions = new ObservableCollection<string>();

        RefreshLibrary();
        SyncScaleProps();
        Navigate(Hub.WelcomeCompleted ? "Home" : "Welcome");
    }

    public MainViewModel() : this(
        new TileRuntime(new NullTileHost()),
        HostServicesFakes.Create(),
        null)
    {
    }

    public ObservableCollection<DiscoverCard> HomeCards { get; }
    public ObservableCollection<LibraryItemViewModel> Modules { get; } = [];
    public ObservableCollection<LibraryItemViewModel> Widgets { get; } = [];
    public ObservableCollection<string> ModuleStyleOptions { get; }
    public ObservableCollection<TesseraNamedChoice> TesseraPositionChoices { get; } =
    [
        new("TL", "Top left"),
        new("TC", "Top center"),
        new("TR", "Top right"),
        new("CL", "Center left"),
        new("CC", "Center"),
        new("CR", "Center right"),
        new("BL", "Bottom left"),
        new("BC", "Bottom center"),
        new("BR", "Bottom right"),
    ];
    public ObservableCollection<TesseraAniChoice> TesseraAniChoices { get; } =
    [
        new(0, "None (fade only)"),
        new(1, "Fast slide"),
        new(2, "Fancy slide"),
    ];
    public ObservableCollection<string> TesseraAniDirOptions { get; } =
        ["Left", "Right", "Top", "Bottom"];
    public HubSettings Hub { get; }

    [ObservableProperty] private string _selectedPage = "Home";
    [ObservableProperty] private bool _isHome = true;
    [ObservableProperty] private bool _isTiles;
    [ObservableProperty] private bool _isSettings;
    [ObservableProperty] private bool _isAbout;
    [ObservableProperty] private bool _isWelcome;
    [ObservableProperty] private bool _isModuleConfig;
    [ObservableProperty] private bool _showBackButton;
    [ObservableProperty] private double _layoutScale = 1.0;
    [ObservableProperty] private double _dpiScale = 1.0;
    [ObservableProperty] private double _userScale = 1.0;
    [ObservableProperty] private double _uiScale = 1.0;
    [ObservableProperty] private string _scaleSummary = "";
    [ObservableProperty] private string _userScalePercentLabel = "100%";
    [ObservableProperty] private string _statusMessage = "Ready";
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _serviceProbe = "";
    [ObservableProperty] private string _configModuleId = "";
    [ObservableProperty] private string _configModuleTitle = "";
    [ObservableProperty] private string _moduleStyle = "DEFAULT";
    [ObservableProperty] private bool _chronoSeconds = true;
    [ObservableProperty] private bool _showChronoExtras;
    [ObservableProperty] private bool _tesseraLegacyVol = true;
    [ObservableProperty] private bool _showTesseraExtras;
    [ObservableProperty] private TesseraNamedChoice? _selectedTesseraPosition;
    [ObservableProperty] private string _tesseraPosition = "TL";
    [ObservableProperty] private decimal _tesseraMonitorIndex = 1;
    [ObservableProperty] private decimal _tesseraXPad = 20;
    [ObservableProperty] private decimal _tesseraYPad = 20;
    [ObservableProperty] private decimal _tesseraAutoDismissSeconds = 2.00m;
    [ObservableProperty] private TesseraAniChoice? _selectedTesseraAni;
    [ObservableProperty] private int _tesseraAni = 2;
    [ObservableProperty] private string _tesseraAniDir = "Left";
    [ObservableProperty] private bool _tesseraAniDirEnabled = true;
    [ObservableProperty] private bool _tesseraMediaFlyouts = true;
    [ObservableProperty] private bool _tesseraLockFlyouts = true;
    [ObservableProperty] private bool _tesseraFlightFlyouts = true;
    [ObservableProperty] private bool _tesseraMediaStrip = true;
    [ObservableProperty] private bool _tesseraAcrylicBackdrop = true;
    [ObservableProperty] private bool _tesseraFocusDim = true;
    [ObservableProperty] private decimal _tesseraLegacyStepPercent = 2;
    [ObservableProperty] private bool _autostartEnabled;
    [ObservableProperty] private bool _closeMinimizesToTray = true;
    [ObservableProperty] private string _updateStatus = "";

    public void RestoreSessions()
    {
        foreach (var s in SessionStore.Load())
        {
            if (!ModuleCatalog.IsInstalled(s.ModuleId)) continue;
            if (ModuleCatalog.IsCapability(s.ModuleId)) continue; // capabilities use daemon, not tile sessions
            _runtime.Start(s.ModuleId);
        }
        RefreshLibrary();
    }

    public void RefreshArmedState() => RefreshLibrary();

    [RelayCommand]
    private void Navigate(string page)
    {
        // Accept legacy Discover/Library names from older links
        if (page == "Discover") page = "Home";
        if (page == "Library") page = "Tiles";

        SelectedPage = page;
        IsHome = page == "Home";
        IsTiles = page == "Tiles";
        IsSettings = page == "Settings";
        IsAbout = page == "About";
        IsWelcome = page == "Welcome";
        IsModuleConfig = page == "ModuleConfig";
        ShowBackButton = IsTiles || IsWelcome || IsAbout || IsModuleConfig;
        if (IsTiles) RefreshLibrary();
        if (IsSettings)
        {
            SyncScaleProps();
            SyncServiceProbe();
            AutostartEnabled = _services.Autostart.IsEnabled;
        }
    }

    [RelayCommand] private void OpenCard(DiscoverCard? card) { if (card is not null) Navigate(card.TargetPage); }

    [RelayCommand]
    private void GoBack()
    {
        if (IsModuleConfig) Navigate("Tiles");
        else Navigate("Home");
    }

    [RelayCommand]
    private void OpenModuleConfig(LibraryItemViewModel? item)
    {
        if (item is null) return;
        ConfigModuleId = item.Id;
        ConfigModuleTitle = item.Name;
        ModuleStyleOptions.Clear();
        foreach (var id in StyleCatalog.IdsFor(item.Id))
            ModuleStyleOptions.Add(id);

        ShowChronoExtras = item.Id.Equals("Chrono", StringComparison.OrdinalIgnoreCase);
        ShowTesseraExtras = item.Id.Equals("Tessera", StringComparison.OrdinalIgnoreCase);

        if (ShowChronoExtras)
        {
            var s = ModuleSettingsStore.Load("Chrono", () => new ChronoSettings());
            ModuleStyle = s.Style;
            ChronoSeconds = s.ShowSeconds;
        }
        else if (ShowTesseraExtras)
        {
            var s = ModuleSettingsStore.Load("Tessera", () => new TesseraSettings());
            ModuleStyle = s.Style;
            TesseraLegacyVol = s.UseLegacyVolumeHooks;
            TesseraPosition = string.IsNullOrWhiteSpace(s.Position) ? "TL" : s.Position.ToUpperInvariant();
            SelectedTesseraPosition = TesseraPositionChoices.FirstOrDefault(c => c.Code == TesseraPosition)
                                      ?? TesseraPositionChoices[0];
            TesseraMonitorIndex = Math.Clamp(s.MonitorIndex, 1, 8);
            TesseraXPad = Math.Clamp(s.XPad, 0, 200);
            TesseraYPad = Math.Clamp(s.YPad, 0, 200);
            TesseraAutoDismissSeconds = Math.Clamp((decimal)s.AutoDismissMs / 1000m, 0.5m, 20m);
            TesseraAni = s.Ani;
            SelectedTesseraAni = TesseraAniChoices.FirstOrDefault(c => c.Value == TesseraAni)
                                 ?? TesseraAniChoices[^1];
            TesseraAniDir = s.AniDir;
            TesseraAniDirEnabled = TesseraAni > 0;
            TesseraMediaFlyouts = s.EnableMediaFlyouts;
            TesseraLockFlyouts = s.EnableLockFlyouts;
            TesseraFlightFlyouts = s.EnableFlightFlyouts;
            TesseraMediaStrip = s.ShowMediaStripOnVolume;
            TesseraAcrylicBackdrop = s.UseAcrylicBackdrop;
            TesseraFocusDim = s.UseFocusDim;
            // Stored as 0-1 fraction; UI is percent points out of 100
            var stepPct = s.LegacyVolumeStep <= 1.0
                ? (decimal)Math.Round(s.LegacyVolumeStep * 100)
                : (decimal)Math.Round(s.LegacyVolumeStep);
            TesseraLegacyStepPercent = Math.Clamp(stepPct < 1 ? 2 : stepPct, 1, 25);
        }
        else
        {
            ModuleStyle = LoadStylePreference(item.Id, ModuleStyleOptions.FirstOrDefault() ?? StyleCatalog.DefaultFor(item.Id));
        }

        if (ModuleStyleOptions.Count > 0 && !ModuleStyleOptions.Contains(ModuleStyle))
            ModuleStyle = ModuleStyleOptions[0];

        Navigate("ModuleConfig");
    }

    private static string LoadStylePreference(string moduleId, string fallback)
    {
        try
        {
            return moduleId.ToLowerInvariant() switch
            {
                "phono" => ModuleSettingsStore.Load("Phono", () => new PhonoSettings()).Style,
                "pulse" => ModuleSettingsStore.Load("Pulse", () => new PulseSettings()).Style,
                "canvas" => ModuleSettingsStore.Load("Canvas", () => new CanvasSettings()).Style,
                "mixdeck" => ModuleSettingsStore.Load("Mixdeck", () => new MixdeckSettings()).Style,
                "inlay" => ModuleSettingsStore.Load("Inlay", () => new InlaySettings()).Style,
                "chord" => ModuleSettingsStore.Load("Chord", () => new ChordSettings()).Style,
                "slate" => ModuleSettingsStore.Load("Slate", () => new SlateSettings()).Style,
                "substrate" => ModuleSettingsStore.Load("Substrate", () => new SubstrateSettings()).Style,
                _ => fallback
            };
        }
        catch { return fallback; }
    }

    [RelayCommand]
    private void PreviewTesseraFlyout()
    {
        if (ShowTesseraExtras)
            PersistTesseraFromUi();
        MosaicShell.Host.Tiles.Tessera.TesseraHostBridge.PreviewVolumeFlyout?.Invoke();
        StatusMessage = "Tessera preview shown (uses current placement & style).";
    }

    private void PersistTesseraFromUi()
    {
        var s = ModuleSettingsStore.Load("Tessera", () => new TesseraSettings());
        s.Style = ModuleStyle;
        s.Position = SelectedTesseraPosition?.Code ?? TesseraPosition;
        s.MonitorIndex = Math.Clamp((int)TesseraMonitorIndex, 1, 8);
        s.XPad = Math.Clamp((int)TesseraXPad, 0, 200);
        s.YPad = Math.Clamp((int)TesseraYPad, 0, 200);
        TesseraMonitorIndex = s.MonitorIndex;
        TesseraXPad = s.XPad;
        TesseraYPad = s.YPad;
        s.AutoDismissMs = (int)Math.Round(Math.Clamp((double)TesseraAutoDismissSeconds, 0.5, 20) * 1000);
        s.Ani = SelectedTesseraAni?.Value ?? TesseraAni;
        s.AniDir = TesseraAniDir;
        s.EnableMediaFlyouts = TesseraMediaFlyouts;
        s.EnableLockFlyouts = TesseraLockFlyouts;
        s.EnableFlightFlyouts = TesseraFlightFlyouts;
        s.ShowMediaStripOnVolume = TesseraMediaStrip;
        s.UseAcrylicBackdrop = TesseraAcrylicBackdrop;
        s.UseFocusDim = TesseraFocusDim;
        s.UseLegacyVolumeHooks = TesseraLegacyVol;
        s.LegacyVolumeStep = Math.Clamp((double)TesseraLegacyStepPercent, 1, 25) / 100.0;
        ModuleSettingsStore.Save("Tessera", s);
        TesseraPosition = s.Position;
        TesseraAni = s.Ani;
    }

    partial void OnSelectedTesseraPositionChanged(TesseraNamedChoice? value)
    {
        if (value is not null) TesseraPosition = value.Code;
    }

    partial void OnSelectedTesseraAniChanged(TesseraAniChoice? value)
    {
        if (value is null) return;
        TesseraAni = value.Value;
        TesseraAniDirEnabled = value.Value > 0;
    }

    [RelayCommand]
    private void SaveModuleConfig()
    {
        if (string.IsNullOrWhiteSpace(ConfigModuleId)) return;
        var id = ConfigModuleId;
        switch (id.ToLowerInvariant())
        {
            case "chrono":
            {
                var s = ModuleSettingsStore.Load("Chrono", () => new ChronoSettings());
                s.Style = ModuleStyle;
                s.ShowSeconds = ChronoSeconds;
                ModuleSettingsStore.Save("Chrono", s);
                StatusMessage = "Chrono settings saved! Relaunch widget to apply.";
                break;
            }
            case "tessera":
            {
                PersistTesseraFromUi();
                StatusMessage = "Tessera settings saved - re-arm if you changed legacy hooks or flyout sources.";
                break;
            }
            case "phono":
            {
                var s = ModuleSettingsStore.Load("Phono", () => new PhonoSettings());
                s.Style = ModuleStyle;
                ModuleSettingsStore.Save("Phono", s);
                StatusMessage = "Phono style saved.";
                break;
            }
            case "pulse":
            {
                var s = ModuleSettingsStore.Load("Pulse", () => new PulseSettings());
                s.Style = ModuleStyle;
                ModuleSettingsStore.Save("Pulse", s);
                StatusMessage = "Pulse style saved.";
                break;
            }
            case "canvas":
            {
                var s = ModuleSettingsStore.Load("Canvas", () => new CanvasSettings());
                s.Style = ModuleStyle;
                ModuleSettingsStore.Save("Canvas", s);
                StatusMessage = "Canvas style saved.";
                break;
            }
            case "mixdeck":
            {
                var s = ModuleSettingsStore.Load("Mixdeck", () => new MixdeckSettings());
                s.Style = ModuleStyle;
                ModuleSettingsStore.Save("Mixdeck", s);
                StatusMessage = "Mixdeck style saved.";
                break;
            }
            case "inlay":
            {
                var s = ModuleSettingsStore.Load("Inlay", () => new InlaySettings());
                s.Style = ModuleStyle;
                ModuleSettingsStore.Save("Inlay", s);
                StatusMessage = "Inlay style saved.";
                break;
            }
            case "chord":
            {
                var s = ModuleSettingsStore.Load("Chord", () => new ChordSettings());
                s.Style = ModuleStyle;
                ModuleSettingsStore.Save("Chord", s);
                StatusMessage = "Chord style saved.";
                break;
            }
            case "slate":
            {
                var s = ModuleSettingsStore.Load("Slate", () => new SlateSettings());
                s.Style = ModuleStyle;
                ModuleSettingsStore.Save("Slate", s);
                StatusMessage = "Slate style saved.";
                break;
            }
            case "substrate":
            {
                var s = ModuleSettingsStore.Load("Substrate", () => new SubstrateSettings());
                s.Style = ModuleStyle;
                ModuleSettingsStore.Save("Substrate", s);
                StatusMessage = "Substrate style saved.";
                break;
            }
            default:
                StatusMessage = $"No settings store for {id}.";
                return;
        }
    }

    [RelayCommand]
    private void OpenGitHub()
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://github.com/uairhahs/MosaicShell",
                UseShellExecute = true
            });
        }
        catch (Exception ex) { StatusMessage = ex.Message; }
    }

    [RelayCommand]
    private void MatchWindows()
    {
        _scale.ResetUserScale();
        _scale.SetDpiScale(DpiProbe.GetDpiScale());
        PersistScale();
    }

    [RelayCommand]
    private void RedetectDpi()
    {
        _scale.SetDpiScale(DpiProbe.GetDpiScale());
        PersistScale();
    }

    [RelayCommand]
    private void ApplyUserScale()
    {
        try
        {
            _scale.SetUserScale(UserScale);
            PersistScale();
            _tileHost?.ApplyUserScale(UserScale);
        }
        catch (ArgumentOutOfRangeException) { UserScale = _scale.UserScale; }
    }

    partial void OnUserScaleChanged(double value)
    {
        // Live preview while dragging; Apply still commits LayoutScale / tiles.
        UserScalePercentLabel = $"{value * 100:0}%";
        var effective = value * DpiScale;
        ScaleSummary =
            $"{effective * 100:0}% effective  (OS DPI {DpiScale:0.##} × user {value:0.##})";
    }

    [RelayCommand]
    private void ToggleAutostart()
    {
        AutostartEnabled = !AutostartEnabled;
        _services.Autostart.SetEnabled(AutostartEnabled);
        Hub.AutostartHost = AutostartEnabled;
        ModuleSettingsStore.Save("Hub", Hub);
        StatusMessage = AutostartEnabled ? "Host will start at logon." : "Autostart disabled.";
    }

    [RelayCommand]
    private void SetCloseMinimizesToTray(string? value)
    {
        var minimize = !string.Equals(value, "exit", StringComparison.OrdinalIgnoreCase);
        CloseMinimizesToTray = minimize;
        Hub.CloseMinimizesToTray = minimize;
        ModuleSettingsStore.Save("Hub", Hub);
        StatusMessage = minimize
            ? "Close button minimizes to tray."
            : "Close button exits the app.";
    }

    partial void OnCloseMinimizesToTrayChanged(bool value)
    {
        // Keep Hub in sync when bound ToggleButton/CheckBox flips the property
        if (Hub.CloseMinimizesToTray == value) return;
        Hub.CloseMinimizesToTray = value;
        ModuleSettingsStore.Save("Hub", Hub);
    }

    [RelayCommand]
    private async Task CheckUpdatesAsync()
    {
        IsBusy = true;
        UpdateStatus = "Checking…";
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            var result = await UpdateChecker.CheckGitHubAsync(http);
            UpdateStatus = result.UpdateAvailable
                ? $"Update available: {result.LatestVersion} (you have {result.CurrentVersion})"
                : $"Up to date ({result.CurrentVersion}).";
            StatusMessage = UpdateStatus;
            if (result.ReleaseUrl is not null && result.UpdateAvailable)
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = result.ReleaseUrl,
                        UseShellExecute = true
                    });
                }
                catch { /* ignore */ }
            }
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task CompleteWelcomeAsync()
    {
        Hub.WelcomeCompleted = true;
        ModuleSettingsStore.Save("Hub", Hub);
        var selected = Modules.Concat(Widgets).Where(m => m.IsSelectedForBatch).Select(m => m.Id).ToList();
        foreach (var id in selected)
        {
            if (!ModuleCatalog.IsInstalled(id))
                await _installer.InstallAsync(id);
        }
        RefreshLibrary();
        Navigate("Home");
        StatusMessage = selected.Count == 0 ? "Welcome completed." : $"Installed {selected.Count} module(s).";
    }

    [RelayCommand]
    private async Task BatchInstallAsync()
    {
        var selected = Modules.Concat(Widgets).Where(m => m.IsSelectedForBatch && !m.IsInstalled).ToList();
        if (selected.Count == 0)
        {
            StatusMessage = "Select modules in Tiles (checkbox) first.";
            return;
        }
        IsBusy = true;
        try
        {
            foreach (var item in selected)
            {
                StatusMessage = $"Installing {item.Name}…";
                await _installer.InstallAsync(item.Id);
                item.ApplyInstalled(_daemon?.IsArmed(item.Id) == true);
            }
            StatusMessage = $"Batch installed {selected.Count} module(s).";
        }
        catch (Exception ex) { StatusMessage = ex.Message; }
        finally { IsBusy = false; RefreshLibrary(); }
    }

    [RelayCommand]
    private async Task InstallModuleAsync(LibraryItemViewModel? item)
    {
        if (item is null || IsBusy) return;
        if (item.IsInstalled)
        {
            if (item.IsCapability)
            {
                if (_daemon is null)
                {
                    StatusMessage = "Capability daemon not available.";
                    return;
                }
                if (_daemon.IsArmed(item.Id))
                {
                    await _daemon.DisarmAsync(item.Id);
                    item.ApplyArmed(false);
                    StatusMessage = $"Disarmed {item.Name}.";
                }
                else
                {
                    var ok = await _daemon.ArmAsync(item.Id);
                    item.ApplyArmed(ok);
                    StatusMessage = ok
                        ? $"Armed {item.Name}! Runs in background while Host is in tray."
                        : $"Could not arm {item.Name}.";
                }
                return;
            }

            if (_runtime.IsRunning(item.Id))
            {
                _runtime.Stop(item.Id);
                item.ApplyRunning(false);
                StatusMessage = $"Stopped {item.Name}.";
                return;
            }
            var launch = _launcher.TryLaunch(item.Id);
            item.ApplyRunning(launch.Started);
            StatusMessage = launch.Message;
            return;
        }

        IsBusy = true;
        item.IsInstalling = true;
        try
        {
            await _installer.InstallAsync(item.Id);
            var manifest = ModuleManifest.TryLoad(item.Id);
            item.ApplyInstalled(false);
            StatusMessage = item.IsCapability
                ? $"Installed {item.Name}. Use the flash button to arm the capability host."
                : $"Installed {item.Name}. Use play to launch the overlay.";

            if (manifest?.DefaultArmed == true && _daemon is not null)
            {
                var ok = await _daemon.ArmAsync(item.Id);
                item.ApplyArmed(ok);
                if (ok) StatusMessage = $"Installed and armed {item.Name}.";
            }
        }
        catch (Exception ex)
        {
            item.StatusText = "(Not Installed)";
            item.ActionIcon = MaterialIconKind.Plus;
            StatusMessage = $"Install failed: {ex.Message}";
        }
        finally
        {
            item.IsInstalling = false;
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task UninstallModule(LibraryItemViewModel? item)
    {
        if (item is null || !item.IsInstalled) return;
        if (ModuleUninstaller.Uninstall(item.Id, _runtime, _daemon))
        {
            StatusMessage = $"Uninstalled {item.Name}.";
            RefreshLibrary();
        }
        await Task.CompletedTask;
    }

    [RelayCommand]
    private void ImportShp()
    {
        var path = Environment.GetEnvironmentVariable("MOSAICSHELL_SHP");
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            StatusMessage = "Set MOSAICSHELL_SHP to an .shp path, or use: Mosaicist import-shp <file>";
            return;
        }
        var result = ShpImporter.Import(path);
        StatusMessage = result.Message;
    }

    private void PersistScale()
    {
        ScaleSettingsStore.Save(_scale.ToSettings());
        SyncScaleProps();
        StatusMessage = $"Scale saved! Layout ×{LayoutScale:0.##}";
    }

    private void SyncScaleProps()
    {
        DpiScale = _scale.DpiScale;
        UserScale = _scale.UserScale; // also refreshes percent label + ScaleSummary via OnUserScaleChanged
        UiScale = _scale.UiScale;
        LayoutScale = _scale.UserScale;
        UserScalePercentLabel = $"{UserScale * 100:0}%";
        ScaleSummary = $"{UiScale * 100:0}% effective  (OS DPI {_scale.DpiScale:0.##} × user {_scale.UserScale:0.##})";
    }

    private void SyncServiceProbe()
    {
        try
        {
            var m = _services.Metrics.Sample();
            var media = _services.Media.Current?.Title ?? "(none)";
            var armed = _daemon is null ? "0" : string.Join(",", _daemon.ArmedModuleIds);
            ServiceProbe =
                $"CPU {m.CpuPercent:0}% · RAM {m.RamUsedPercent:0}% · Vol {_services.Audio.MasterVolume:0%} · Media {media} · Armed [{armed}]";
        }
        catch (Exception ex)
        {
            ServiceProbe = ex.Message;
        }
    }

    private void RefreshLibrary()
    {
        Modules.Clear();
        Widgets.Clear();
        foreach (var m in ModuleCatalog.Modules)
        {
            var armed = _daemon?.IsArmed(m.Id) == true;
            Modules.Add(LibraryItemViewModel.From(m, running: false, armed: armed));
        }
        foreach (var w in ModuleCatalog.Widgets)
            Widgets.Add(LibraryItemViewModel.From(w, running: _runtime.IsRunning(w.Id), armed: false));
    }

    private sealed class NullTileHost : ITileSurfaceHost
    {
        public bool Show(string moduleId, out string? error) { error = "design-time"; return false; }
        public void Focus(string moduleId) { }
        public void Close(string moduleId) { }
    }
}

public sealed record DiscoverCard(string Title, string Body, string TargetPage, string IconPath);

public partial class LibraryItemViewModel : ObservableObject
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string IconPath { get; init; }
    public bool IsCapability { get; init; }

    [ObservableProperty] private bool _isInstalled;
    [ObservableProperty] private bool _isInstalling;
    [ObservableProperty] private bool _isRunning;
    [ObservableProperty] private bool _isArmed;
    [ObservableProperty] private bool _isSelectedForBatch;
    [ObservableProperty] private string _statusText = "(Not Installed)";
    [ObservableProperty] private MaterialIconKind _actionIcon = MaterialIconKind.Plus;

    public void ApplyInstalled(bool armed = false)
    {
        IsInstalled = true;
        IsRunning = false;
        IsArmed = armed;
        if (IsCapability)
        {
            StatusText = armed ? "Armed" : "Ready to arm";
            ActionIcon = armed ? MaterialIconKind.StopCircle : MaterialIconKind.Flash;
        }
        else
        {
            StatusText = "Ready";
            ActionIcon = MaterialIconKind.Play;
        }
    }

    public void ApplyRunning(bool running)
    {
        IsRunning = running;
        if (!IsInstalled || IsCapability) return;
        StatusText = running ? "Running" : "Ready";
        ActionIcon = running ? MaterialIconKind.Stop : MaterialIconKind.Play;
    }

    public void ApplyArmed(bool armed)
    {
        IsArmed = armed;
        if (!IsInstalled || !IsCapability) return;
        StatusText = armed ? "Armed" : "Ready to arm";
        ActionIcon = armed ? MaterialIconKind.StopCircle : MaterialIconKind.Flash;
    }

    public static LibraryItemViewModel From(ModuleInfo info, bool running = false, bool armed = false)
    {
        var installed = ModuleCatalog.IsInstalled(info.Id);
        var isCap = info.Kind is ModuleKind.Capability or ModuleKind.Hybrid;
        string status;
        MaterialIconKind icon;
        if (!installed)
        {
            status = "(Not Installed)";
            icon = MaterialIconKind.Plus;
        }
        else if (isCap)
        {
            status = armed ? "Armed" : "Ready to arm";
            icon = armed ? MaterialIconKind.StopCircle : MaterialIconKind.Flash;
        }
        else
        {
            status = running ? "Running" : "Ready";
            icon = running ? MaterialIconKind.Stop : MaterialIconKind.Play;
        }

        return new LibraryItemViewModel
        {
            Id = info.Id,
            Name = info.DisplayName,
            Description = info.Description,
            IconPath = $"/Assets/Modules/{info.Id}.png",
            IsCapability = isCap,
            IsInstalled = installed,
            IsRunning = running && installed && !isCap,
            IsArmed = armed && installed && isCap,
            StatusText = status,
            ActionIcon = icon
        };
    }
}

public sealed record TesseraNamedChoice(string Code, string Label)
{
    public override string ToString() => Label;
}

public sealed record TesseraAniChoice(int Value, string Label)
{
    public override string ToString() => Label;
}
