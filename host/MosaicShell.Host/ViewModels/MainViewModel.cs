using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Material.Icons;
using MosaicShell.Core;
using MosaicShell.Core.Capabilities;
using MosaicShell.Core.Capabilities.BuiltIn;
using MosaicShell.Core.Install;
using MosaicShell.Core.Modules;
using MosaicShell.Core.Runtime;
using MosaicShell.Core.Scale;
using MosaicShell.Core.Services;
using MosaicShell.Core.Settings;
using MosaicShell.Core.Shp;
using MosaicShell.Core.Styles;
using MosaicShell.Core.Update;
using MosaicShell.Host.Input;
using MosaicShell.Host.Tiles;
using System.Collections.ObjectModel;
using Avalonia.Input;

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
    [ObservableProperty] private bool _showHotkeyCapExtras;
    [ObservableProperty] private bool _showSlateExtras;
    [ObservableProperty] private bool _showInlayPins;
    [ObservableProperty] private bool _showChordActions;
    [ObservableProperty] private bool _showSubstrateMute;
    [ObservableProperty] private string _configUsageSummary = "";
    [ObservableProperty] private string _configHowToTrigger = "";
    [ObservableProperty] private string _configHotkeyGesture = "";
    [ObservableProperty] private bool _isCapturingHotkey;
    [ObservableProperty] private string _hotkeyCaptureHint = "Click Capture, then press the shortcut";
    [ObservableProperty] private string _launchTargetQuery = "";
    [ObservableProperty] private string _chordActionName = "";
    [ObservableProperty] private string _configPinsText = "";
    [ObservableProperty] private string _configActionsText = "";
    [ObservableProperty] private bool _configShowMute = true;
    [ObservableProperty] private decimal _configIdleSeconds = 300;
    [ObservableProperty] private bool _configHideOnFullscreen = true;
    [ObservableProperty] private bool _configCanTryOverlay;

    public ObservableCollection<string> ConfigPins { get; } = [];
    public ObservableCollection<ChordActionRow> ConfigChordActions { get; } = [];
    public ObservableCollection<string> LaunchTargetChoices { get; } = [];

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
    [ObservableProperty] private bool _tesseraBakedFrost;
    [ObservableProperty] private decimal _tesseraFlyoutScalePercent = 100;
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
        OpenModuleConfigById(item.Id);
    }

    /// <summary>Opened from overlay context menu (Configure in Host).</summary>
    public void OpenModuleConfigById(string moduleId)
    {
        if (!ModuleCatalog.TryGet(moduleId, out var info) || info is null) return;
        ConfigModuleId = info.Id;
        ConfigModuleTitle = info.DisplayName;
        ModuleStyleOptions.Clear();
        foreach (var id in StyleCatalog.IdsFor(info.Id))
            ModuleStyleOptions.Add(id);

        ShowChronoExtras = info.Id.Equals("Chrono", StringComparison.OrdinalIgnoreCase);
        ShowTesseraExtras = info.Id.Equals("Tessera", StringComparison.OrdinalIgnoreCase);
        ShowHotkeyCapExtras = info.Id is "Inlay" or "Chord" or "Substrate" or "Mixdeck";
        ShowSlateExtras = info.Id.Equals("Slate", StringComparison.OrdinalIgnoreCase);
        ShowInlayPins = info.Id.Equals("Inlay", StringComparison.OrdinalIgnoreCase);
        ShowChordActions = info.Id.Equals("Chord", StringComparison.OrdinalIgnoreCase);
        ShowSubstrateMute = info.Id.Equals("Substrate", StringComparison.OrdinalIgnoreCase);
        ConfigCanTryOverlay = ShowHotkeyCapExtras || ShowSlateExtras || ShowTesseraExtras;
        ConfigUsageSummary = ModuleUsageGuide.Summary(info.Id);
        ConfigHowToTrigger = ModuleUsageGuide.HowToTrigger(info.Id);
        ConfigHotkeyGesture = ModuleUsageGuide.CurrentHotkey(info.Id);
        IsCapturingHotkey = false;
        HotkeyCaptureHint = "Click Capture, then press the shortcut";
        LaunchTargetQuery = "";
        ChordActionName = "";
        ConfigPins.Clear();
        ConfigChordActions.Clear();
        EnsureLaunchTargetsLoaded();
        ConfigPinsText = "";
        ConfigActionsText = "";
        ConfigShowMute = true;
        ConfigIdleSeconds = 300;
        ConfigHideOnFullscreen = true;

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
            TesseraBakedFrost = s.UseBakedFrost;
            TesseraFlyoutScalePercent = Math.Clamp(s.FlyoutScalePercent, 50, 150);
            // Stored as 0-1 fraction; UI is percent points out of 100
            var stepPct = s.LegacyVolumeStep <= 1.0
                ? (decimal)Math.Round(s.LegacyVolumeStep * 100)
                : (decimal)Math.Round(s.LegacyVolumeStep);
            TesseraLegacyStepPercent = Math.Clamp(stepPct < 1 ? 2 : stepPct, 1, 25);
        }
        else if (info.Id.Equals("Inlay", StringComparison.OrdinalIgnoreCase))
        {
            var s = ModuleSettingsStore.Load("Inlay", () => new InlaySettings());
            ModuleStyle = s.Style;
            ConfigHotkeyGesture = HotkeyGestureParser.EnsureRegisterable("Inlay", s.HotkeyGesture);
            foreach (var pin in s.Pins)
                ConfigPins.Add(pin);
        }
        else if (info.Id.Equals("Chord", StringComparison.OrdinalIgnoreCase))
        {
            var s = ModuleSettingsStore.Load("Chord", () => new ChordSettings());
            ModuleStyle = s.Style;
            ConfigHotkeyGesture = HotkeyGestureParser.EnsureRegisterable("Chord", s.HotkeyGesture);
            foreach (var a in s.Actions)
                ConfigChordActions.Add(new ChordActionRow(a.Name, a.Target));
        }
        else if (info.Id.Equals("Substrate", StringComparison.OrdinalIgnoreCase))
        {
            var s = ModuleSettingsStore.Load("Substrate", () => new SubstrateSettings());
            ModuleStyle = s.Style;
            ConfigHotkeyGesture = HotkeyGestureParser.EnsureRegisterable("Substrate", s.HotkeyGesture);
            ConfigShowMute = s.ShowMute;
        }
        else if (info.Id.Equals("Mixdeck", StringComparison.OrdinalIgnoreCase))
        {
            var s = ModuleSettingsStore.Load("Mixdeck", () => new MixdeckSettings());
            ModuleStyle = s.Style;
            ConfigHotkeyGesture = HotkeyGestureParser.EnsureRegisterable("Mixdeck", s.HotkeyGesture);
        }
        else if (ShowSlateExtras)
        {
            var s = ModuleSettingsStore.Load("Slate", () => new SlateSettings());
            ModuleStyle = s.Style;
            ConfigIdleSeconds = Math.Clamp(s.IdleSeconds, 30, 3600);
            ConfigHideOnFullscreen = s.HideOnFullscreen;
        }
        else
        {
            ModuleStyle = LoadStylePreference(info.Id, ModuleStyleOptions.FirstOrDefault() ?? StyleCatalog.DefaultFor(info.Id));
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
        s.UseBakedFrost = TesseraBakedFrost;
        s.FlyoutScalePercent = (int)Math.Clamp(TesseraFlyoutScalePercent, 50, 150);
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
    private async Task SaveModuleConfigAsync()
    {
        if (string.IsNullOrWhiteSpace(ConfigModuleId)) return;
        var id = ConfigModuleId;
        IsCapturingHotkey = false;
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
                s.HotkeyGesture = HotkeyGestureParser.EnsureRegisterable("Mixdeck", ConfigHotkeyGesture);
                ConfigHotkeyGesture = s.HotkeyGesture;
                ModuleSettingsStore.Save("Mixdeck", s);
                StatusMessage = await PersistHotkeyArmAsync(id);
                break;
            }
            case "inlay":
            {
                var s = ModuleSettingsStore.Load("Inlay", () => new InlaySettings());
                s.Style = ModuleStyle;
                s.HotkeyGesture = HotkeyGestureParser.EnsureRegisterable("Inlay", ConfigHotkeyGesture);
                ConfigHotkeyGesture = s.HotkeyGesture;
                s.Pins = ConfigPins.Where(p => !string.IsNullOrWhiteSpace(p)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                if (s.Pins.Count == 0) s.Pins = ["notepad", "calc"];
                ModuleSettingsStore.Save("Inlay", s);
                StatusMessage = await PersistHotkeyArmAsync(id);
                break;
            }
            case "chord":
            {
                var s = ModuleSettingsStore.Load("Chord", () => new ChordSettings());
                s.Style = ModuleStyle;
                s.HotkeyGesture = HotkeyGestureParser.EnsureRegisterable("Chord", ConfigHotkeyGesture);
                ConfigHotkeyGesture = s.HotkeyGesture;
                s.Actions = ConfigChordActions
                    .Where(a => !string.IsNullOrWhiteSpace(a.Target))
                    .Select(a => new ChordAction
                    {
                        Name = string.IsNullOrWhiteSpace(a.Name) ? a.Target : a.Name,
                        Target = a.Target
                    })
                    .ToList();
                ModuleSettingsStore.Save("Chord", s);
                StatusMessage = await PersistHotkeyArmAsync(id);
                break;
            }
            case "slate":
            {
                var s = ModuleSettingsStore.Load("Slate", () => new SlateSettings());
                s.Style = ModuleStyle;
                s.IdleSeconds = Math.Clamp((int)ConfigIdleSeconds, 30, 3600);
                s.HideOnFullscreen = ConfigHideOnFullscreen;
                ModuleSettingsStore.Save("Slate", s);
                if (_daemon?.IsArmed(id) == true)
                    await _daemon.ReArmAsync(id);
                StatusMessage = "Slate saved" + (_daemon?.IsArmed(id) == true ? " and re-armed." : ".");
                break;
            }
            case "substrate":
            {
                var s = ModuleSettingsStore.Load("Substrate", () => new SubstrateSettings());
                s.Style = ModuleStyle;
                s.HotkeyGesture = HotkeyGestureParser.EnsureRegisterable("Substrate", ConfigHotkeyGesture);
                ConfigHotkeyGesture = s.HotkeyGesture;
                s.ShowMute = ConfigShowMute;
                ModuleSettingsStore.Save("Substrate", s);
                StatusMessage = await PersistHotkeyArmAsync(id);
                break;
            }
            default:
                StatusMessage = $"No settings store for {id}.";
                return;
        }

        ConfigHowToTrigger = ModuleUsageGuide.HowToTrigger(id);
        RefreshLibrary();
    }

    private async Task<string> PersistHotkeyArmAsync(string id)
    {
        if (_daemon is null) return $"{id} saved.";
        if (!_daemon.IsArmed(id))
            return $"{id} saved. Arm from Tiles, then press {ConfigHotkeyGesture}.";

        var ok = await _daemon.ReArmAsync(id);
        if (!ok) return $"{id} saved but could not re-arm.";
        var err = _daemon.GetHotkeyError(id);
        return string.IsNullOrWhiteSpace(err)
            ? $"{id} saved. Hotkey active: {ConfigHotkeyGesture}."
            : $"{id} saved but hotkey failed: {err}";
    }

    private void EnsureLaunchTargetsLoaded()
    {
        if (LaunchTargetChoices.Count > 0) return;
        foreach (var label in LaunchTargetCatalog.DisplayLabels())
            LaunchTargetChoices.Add(label);
    }

    [RelayCommand]
    private void BeginHotkeyCapture()
    {
        IsCapturingHotkey = true;
        HotkeyCaptureHint = "Press the shortcut now (Esc cancels)…";
        StatusMessage = "Listening for hotkey…";
    }

    [RelayCommand]
    private void CancelHotkeyCapture()
    {
        IsCapturingHotkey = false;
        HotkeyCaptureHint = "Click Capture, then press the shortcut";
    }

    /// <summary>Called from MainWindow while capturing.</summary>
    public bool TryCaptureHotkey(KeyEventArgs e)
    {
        if (!IsCapturingHotkey) return false;
        if (e.Key == Key.Escape)
        {
            CancelHotkeyCapture();
            e.Handled = true;
            return true;
        }

        if (!HotkeyCapture.TryFormat(e, out var gesture))
            return false;

        if (HotkeyGestureParser.IsLikelyOsReserved(gesture))
        {
            StatusMessage = $"{gesture} is reserved by Windows. Pick Ctrl+Alt+Letter instead.";
            e.Handled = true;
            return true;
        }

        ConfigHotkeyGesture = gesture;
        IsCapturingHotkey = false;
        HotkeyCaptureHint = "Click Capture, then press the shortcut";
        StatusMessage = $"Hotkey set to {gesture}. Save to apply.";
        e.Handled = true;
        return true;
    }

    [RelayCommand]
    private void AddLaunchTargetPin()
    {
        if (!LaunchTargetCatalog.TryResolveLabel(LaunchTargetQuery, out var target, out _))
            return;
        if (ConfigPins.Any(p => p.Equals(target, StringComparison.OrdinalIgnoreCase)))
        {
            StatusMessage = "Already pinned.";
            return;
        }
        ConfigPins.Add(target);
        LaunchTargetQuery = "";
    }

    [RelayCommand]
    private void RemoveConfigPin(string? pin)
    {
        if (pin is null) return;
        ConfigPins.Remove(pin);
    }

    [RelayCommand]
    private void AddChordActionFromPicker()
    {
        if (!LaunchTargetCatalog.TryResolveLabel(LaunchTargetQuery, out var target, out var display))
            return;
        var name = string.IsNullOrWhiteSpace(ChordActionName) ? display : ChordActionName.Trim();
        ConfigChordActions.Add(new ChordActionRow(name, target));
        LaunchTargetQuery = "";
        ChordActionName = "";
    }

    [RelayCommand]
    private void RemoveChordAction(ChordActionRow? row)
    {
        if (row is null) return;
        ConfigChordActions.Remove(row);
    }

    [RelayCommand]
    private async Task TryCapabilityOverlayAsync()
    {
        if (string.IsNullOrWhiteSpace(ConfigModuleId) || _daemon is null) return;
        var id = ConfigModuleId;
        await SaveModuleConfigAsync();
        var ok = _daemon.IsArmed(id) || await _daemon.ArmAsync(id);
        if (!ok)
        {
            StatusMessage = $"Could not arm {id}. Install it from Tiles first.";
            return;
        }

        var err = _daemon.GetHotkeyError(id);
        if (!string.IsNullOrWhiteSpace(err) && ShowHotkeyCapExtras)
            StatusMessage = err;

        if (id.Equals("Tessera", StringComparison.OrdinalIgnoreCase))
        {
            PreviewTesseraFlyout();
            return;
        }

        if (_runtime.IsRunning(id))
            _tileHost?.Focus(id);
        else
            _runtime.Start(id);

        StatusMessage = id.Equals("Slate", StringComparison.OrdinalIgnoreCase)
            ? "Slate overlay opened (preview). Idle timer still applies when armed."
            : string.IsNullOrWhiteSpace(err)
                ? $"{id} overlay opened. Hotkey: {ModuleUsageGuide.CurrentHotkey(id)}"
                : err;
        RefreshLibrary();
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
                    if (!ok)
                        StatusMessage = $"Could not arm {item.Name}.";
                    else
                    {
                        var err = _daemon.GetHotkeyError(item.Id);
                        StatusMessage = string.IsNullOrWhiteSpace(err)
                            ? $"{ModuleUsageGuide.ArmedStatus(item.Id)}. {ModuleUsageGuide.HowToTrigger(item.Id)}"
                            : err;
                    }
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
            StatusText = armed ? ModuleUsageGuide.ArmedStatus(Id) : "Ready to arm";
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
        StatusText = armed ? ModuleUsageGuide.ArmedStatus(Id) : "Ready to arm";
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
            status = armed ? ModuleUsageGuide.ArmedStatus(info.Id) : "Ready to arm";
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

public sealed record ChordActionRow(string Name, string Target)
{
    public string Label => string.IsNullOrWhiteSpace(Name) ? Target : $"{Name} → {Target}";
    public override string ToString() => Label;
}

public sealed record TesseraNamedChoice(string Code, string Label)
{
    public override string ToString() => Label;
}

public sealed record TesseraAniChoice(int Value, string Label)
{
    public override string ToString() => Label;
}
