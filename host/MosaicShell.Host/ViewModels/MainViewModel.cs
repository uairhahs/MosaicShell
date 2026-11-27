using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Material.Icons;
using MosaicShell.Core;
using MosaicShell.Core.Capabilities;
using MosaicShell.Core.Capabilities.Platform;
using MosaicShell.Core.HostPlatform;
using MosaicShell.Core.HubShell;
using MosaicShell.Core.Install;
using MosaicShell.Core.Modules;
using MosaicShell.Core.Modules.Tessera;
using MosaicShell.Core.Runtime;
using MosaicShell.Core.Services;
using MosaicShell.Core.Settings;
using MosaicShell.Core.Shp;
using MosaicShell.Core.Styles;
using MosaicShell.Core.Update;
using MosaicShell.Host.Tiles;
using MosaicShell.Host.Tiles.Tessera;

namespace MosaicShell.Host.ViewModels
{
    public partial class MainViewModel : ViewModelBase
    {
        private readonly ModuleInstaller _installer = new();
        private readonly ITileRuntime _runtime;
        private readonly ModuleLauncher _launcher;
        private readonly HostServices _services;
        private readonly AvaloniaTileSurfaceHost? _tileHost;
        private readonly ICapabilityHost? _capabilityHost;

        private readonly IHostUiBridge _hostUi;

        public MainViewModel(
            ITileRuntime runtime,
            HostServices services,
            AvaloniaTileSurfaceHost? tileHost,
            ICapabilityHost? capabilityHost = null,
            IHostUiBridge? hostUi = null)
        {
            _runtime = runtime;
            _launcher = new ModuleLauncher(runtime);
            _services = services;
            _tileHost = tileHost;
            _capabilityHost = capabilityHost;
            _hostUi = hostUi ?? NullHostUiBridge.Instance;

            AppPaths.EnsureLayout();
            Hub = ModuleSettingsStore.Load("Hub", () => new HubSettings());

            HomeCards =
            [
                HomeCard("Tiles", "Install widgets or set tiles (Tessera flyouts, launchers).", "Tiles"),
                HomeCard("About", "What MosaicShell is, where it came from, and how to follow the project.", "About"),
            ];

            TesseraAccentSwatches = [];
            foreach (TesseraAccentPreset preset in TesseraAccentColor.Presets)
            {
                TesseraAccentSwatches.Add(TesseraAccentSwatchVm.From(preset));
            }

            SyncTesseraAccentSwatches();
            PropertyChanged += OnTesseraSlotPropertyChanged;

            ModuleStyleOptions = [];

            RefreshLibrary();
            HostBuildLabel = HostBuildVersion.ReadCurrent();
            Navigate("Home");
        }

        public MainViewModel() : this(
            new TileRuntime(new NullTileHost()),
            HostServicesFakes.Create(),
            null)
        {
        }

        public ObservableCollection<DiscoverCard> HomeCards { get; }
        public ObservableCollection<TesseraAccentSwatchVm> TesseraAccentSwatches { get; }
        public ObservableCollection<LibraryItemViewModel> Modules { get; } = [];
        public ObservableCollection<LibraryItemViewModel> Widgets { get; } = [];
        public ObservableCollection<StyleDescriptor> ModuleStyleOptions { get; }
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
        public ObservableCollection<string> TesseraEaseFamilies { get; } =
            [with(TesseraFlyoutAnimationPolicy.EaseFamilies)];
        public ObservableCollection<string> TesseraEaseVariants { get; } =
            [with(TesseraFlyoutAnimationPolicy.EaseVariants)];
        public HubSettings Hub { get; }

        [ObservableProperty] private string _selectedPage = "Home";
        [ObservableProperty] private bool _isHome = true;
        [ObservableProperty] private bool _isSettings;
        [ObservableProperty] private bool _isAbout;
        [ObservableProperty] private bool _isModuleConfig;
        [ObservableProperty] private string _statusMessage = "Ready";
        [ObservableProperty] private bool _isBusy;
        [ObservableProperty] private string _serviceProbe = "";
        [ObservableProperty] private string _configModuleId = "";
        [ObservableProperty] private string _configModuleTitle = "";
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(TesseraMaterialSelectable))]
        [NotifyPropertyChangedFor(nameof(TesseraMaterialFixedNote))]
        private string _moduleStyle = "DEFAULT";
        [ObservableProperty] private StyleDescriptor? _selectedModuleStyle;
        [ObservableProperty] private bool _tesseraLegacyVol = true;
        [ObservableProperty] private bool _showTesseraExtras;
        [ObservableProperty] private string _configUsageSummary = "";
        [ObservableProperty] private string _configHowToTrigger = "";
        [ObservableProperty] private bool _tesseraConfigPreviewReady;

        public ObservableCollection<string> LaunchTargetChoices { get; } = [];

        [ObservableProperty] private TesseraNamedChoice? _selectedTesseraPosition;
        [ObservableProperty] private string _tesseraPosition = "TL";
        [ObservableProperty] private decimal _tesseraMonitorIndex = 1;
        [ObservableProperty] private int _tesseraAni = 2;
        [ObservableProperty] private string _tesseraAniDir = "Left";
        [ObservableProperty] private string _tesseraAniEase = TesseraFlyoutAnimationPolicy.DefaultEase;
        [ObservableProperty] private string _tesseraEaseFamily = TesseraFlyoutAnimationPolicy.DefaultEaseFamily;
        [ObservableProperty] private string _tesseraEaseVariant = TesseraFlyoutAnimationPolicy.DefaultEaseVariant;
        [ObservableProperty] private bool _tesseraEaseVariantEnabled = true;
        [ObservableProperty] private int _tesseraAniSteps = TesseraFlyoutAnimationPolicy.DefaultAniSteps;
        [ObservableProperty] private int _tesseraAnimationDisplacement = TesseraFlyoutAnimationPolicy.DefaultDisplacementPx;
        private bool _syncingEaseParts;
        [ObservableProperty] private bool _tesseraMediaStrip = true;
        [ObservableProperty] private string _tesseraAccentHex = "";
        [ObservableProperty] private decimal _tesseraLegacyStepPercent = 2;

        /// <summary>Who owns the volume key step on this desktop (<see cref="VolumeKeyStepPolicy"/>).</summary>
        public VolumeKeyStepSource VolumeStepSource => _services.VolumeKeyStep.Source;

        /// <summary>The key hook switch exists only where Tessera steps the volume itself (Windows).</summary>
        public bool ShowLegacyHookSwitch => VolumeKeyStepPolicy.HasHookSwitch(VolumeStepSource);

        public bool VolumeStepEditable =>
            VolumeStepSource != VolumeKeyStepSource.None && (!ShowLegacyHookSwitch || TesseraLegacyVol);

        public int VolumeStepMax => VolumeKeyStepPolicy.Range(VolumeStepSource).Max;

        public string VolumeStepHint => VolumeKeyStepPolicy.Hint(VolumeStepSource);

        partial void OnTesseraLegacyVolChanged(bool value) => OnPropertyChanged(nameof(VolumeStepEditable));
        [ObservableProperty] private bool _autostartEnabled;
        [ObservableProperty] private bool _closeMinimizesToTray = true;
        [ObservableProperty] private string _updateStatus = "";
        [ObservableProperty] private string _hostBuildLabel = HostBuildVersionPolicy.LocalDevLabel;

        /// <summary>The real data root (honours <c>MOSAICSHELL_HOME</c> and the platform's local app data).</summary>
        public string DataFolderPath => AppPaths.RootDirectory;

        public void RestoreSessions()
        {
            foreach (TileSessionState s in SessionStore.Load())
            {
                if (!ModuleCatalog.IsInstalled(s.ModuleId))
                {
                    continue;
                }

                if (ModuleCatalog.IsCapability(s.ModuleId))
                {
                    continue; // capabilities use daemon, not tile sessions
                }

                _ = _runtime.Start(s.ModuleId);
            }
            RefreshLibrary();
        }

        public void RefreshArmedState()
        {
            RefreshLibrary();
        }

        public bool IsModuleConfigOpening { get; private set; }

        [RelayCommand]
        private void Navigate(string page)
        {
            // Accept legacy Discover/Library names from older links
            if (page == "Discover")
            {
                page = "Home";
            }

            // The Tiles library became two lists: Modules (capabilities) and Widgets.
            if (page is "Library" or "Tiles" or "AddModules")
            {
                page = "Modules";
            }

            SelectedPage = page;
            IsHome = page == "Home";
            IsModulesList = page == "Modules";
            IsWidgetsList = page == "Widgets";
            IsSettings = page == "Settings";
            IsAbout = page == "About";
            IsModuleConfig = page == "ModuleConfig";
            if (!IsModuleConfig)
            {
                TesseraConfigPreviewReady = false;
                CloseModulePage();
            }

            if (IsHome || IsModulesList || IsWidgetsList)
            {
                RefreshLibrary();
            }
            else
            {
                RefreshShell();
            }

            if (IsSettings)
            {
                SyncServiceProbe();
                AutostartEnabled = _services.Autostart.IsEnabled;
            }
        }

        [RelayCommand]
        private void OpenCard(DiscoverCard? card)
        {
            if (card is not null)
            {
                Navigate(card.TargetPage);
            }
        }

        [RelayCommand]
        private void SelectTesseraAccent(string? hex)
        {
            TesseraAccentHex = TesseraAccentColor.NormalizeOrEmpty(hex);
        }

        partial void OnTesseraAccentHexChanged(string value) => SyncTesseraAccentSwatches();

        private void SyncTesseraAccentSwatches()
        {
            foreach (TesseraAccentSwatchVm swatch in TesseraAccentSwatches)
            {
                swatch.IsSelected = TesseraAccentColor.MatchesPreset(TesseraAccentHex, new TesseraAccentPreset(swatch.Name, swatch.Hex));
            }
        }

        private static DiscoverCard HomeCard(string title, string body, string page)
        {
            HubGlyph glyph = HubGlyphCatalog.ForHomeCard(page);
            return new(title, body, page, HubGlyphUi.Kind(glyph), HubGlyphUi.Brush(glyph));
        }

        [RelayCommand]
        private void GoBack()
        {
            if (IsModuleConfig)
            {
                Navigate("Tiles");
            }
            else
            {
                Navigate("Home");
            }
        }

        [RelayCommand]
        private void OpenModuleConfig(LibraryItemViewModel? item)
        {
            if (item is null)
            {
                return;
            }

            OpenModuleConfigById(item.Id);
        }

        /// <summary>Opened from overlay context menu (Configure in Host).</summary>
        public void OpenModuleConfigById(string moduleId)
        {
            if (!ModuleCatalog.TryGet(moduleId, out ModuleInfo? info) || info is null)
            {
                return;
            }

            if (IsModuleConfigOpening)
            {
                return;
            }

            if (IsModuleConfig && ConfigModuleId.Equals(moduleId, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            IsModuleConfigOpening = true;
            TesseraConfigPreviewReady = false;
            try
            {
                CloseModulePage();
                ConfigModuleId = info.Id;
                ConfigModuleTitle = info.DisplayName;
                ModulePage = CreateModulePage(info);
                ModuleStyleOptions.Clear();
                foreach (StyleDescriptor style in StyleCatalog.For(info.Id))
                {
                    ModuleStyleOptions.Add(style);
                }

                ShowTesseraExtras = ModuleIds.IsTessera(info.Id);
                ConfigUsageSummary = ModuleUsageGuide.Summary(info.Id);
                ConfigHowToTrigger = ModuleUsageGuide.HowToTrigger(info.Id, _services.Features);

                using (TesseraStylePreview.EnterSuspendRebuild())
                {
                    Navigate("ModuleConfig");
                    LoadModuleConfigFields(info);
                    if (ShowTesseraExtras && HasModulePage)
                    {
                        StartTesseraSlots();
                    }
                }
            }
            finally
            {
                IsModuleConfigOpening = false;
                if (ShowTesseraExtras)
                {
                    Dispatcher.UIThread.Post(
                        () => TesseraConfigPreviewReady = true,
                        DispatcherPriority.Background);
                }
            }
        }

        private void LoadModuleConfigFields(ModuleInfo info)
        {
            if (ShowTesseraExtras)
            {
                TesseraSettings s = ModuleSettingsStore.Load(ModuleIds.Tessera, () => new TesseraSettings());
                ModuleStyle = StyleIds.Normalize(s.Style);
                TesseraLegacyVol = s.UseLegacyVolumeHooks;
                TesseraPosition = string.IsNullOrWhiteSpace(s.Position) ? "TL" : s.Position.ToUpperInvariant();
                SelectedTesseraPosition = TesseraPositionChoices.FirstOrDefault(c => c.Code == TesseraPosition)
                                          ?? TesseraPositionChoices[0];
                TesseraMonitorIndex = Math.Clamp(s.MonitorIndex, 1, 8);
                TesseraAni = s.Ani;
                TesseraAniDir = s.AniDir;
                TesseraAniEase = TesseraFlyoutAnimationPolicy.NormalizeEase(s.AniEase);
                ApplyEasePartsFromStored(TesseraAniEase);
                TesseraAniSteps = TesseraFlyoutAnimationPolicy.NormalizeAniSteps(s.AniSteps);
                TesseraAnimationDisplacement =
                    TesseraFlyoutAnimationPolicy.NormalizeDisplacementPx(s.AnimationDisplacement);
                RefreshTesseraMotion();
                TesseraMediaStrip = s.ShowMediaStripOnVolume;
                LoadTesseraMaterial(s);
                TesseraAccentHex = s.AccentColor ?? "";
                decimal stepPct = s.LegacyVolumeStep <= 1.0
                    ? (decimal)Math.Round(s.LegacyVolumeStep * 100)
                    : (decimal)Math.Round(s.LegacyVolumeStep);
                TesseraLegacyStepPercent = Math.Clamp(stepPct < 1 ? 2 : stepPct, 1, 25);
                if (!VolumeKeyStepPolicy.HasHookSwitch(VolumeStepSource) && _services.VolumeKeyStep.Read() is int desktopStep)
                {
                    TesseraLegacyStepPercent = VolumeKeyStepPolicy.Clamp(VolumeStepSource, desktopStep);
                }
            }
            else
            {
                ModuleStyle = StyleIds.Normalize(StyleCatalog.DefaultFor(info.Id));
            }

            if (ModuleStyleOptions.Count > 0
                && !ModuleStyleOptions.Any(d => d.StyleId.Equals(ModuleStyle, StringComparison.OrdinalIgnoreCase)))
            {
                ModuleStyle = ModuleStyleOptions[0].StyleId;
            }

            SyncSelectedModuleStyle();
        }

        private void SyncSelectedModuleStyle()
        {
            SelectedModuleStyle = ModuleStyleOptions.FirstOrDefault(d =>
                d.StyleId.Equals(ModuleStyle, StringComparison.OrdinalIgnoreCase))
                ?? ModuleStyleOptions.FirstOrDefault();
            if (SelectedModuleStyle is not null)
            {
                ModuleStyle = SelectedModuleStyle.StyleId;
            }
        }

        partial void OnSelectedModuleStyleChanged(StyleDescriptor? value)
        {
            if (IsModuleConfigOpening || value is null) return;
            if (!ModuleStyle.Equals(value.StyleId, StringComparison.OrdinalIgnoreCase))
                ModuleStyle = value.StyleId;
        }

        partial void OnModuleStyleChanged(string value)
        {
            if (IsModuleConfigOpening) return;
            var normalized = StyleIds.Normalize(value);
            if (!string.Equals(normalized, value, StringComparison.Ordinal))
            {
                ModuleStyle = normalized;
                return;
            }
            var match = ModuleStyleOptions.FirstOrDefault(d =>
                d.StyleId.Equals(normalized, StringComparison.OrdinalIgnoreCase));
            if (match is not null && !ReferenceEquals(SelectedModuleStyle, match))
                SelectedModuleStyle = match;
        }


        [RelayCommand]
        private void PreviewTesseraFlyout()
        {
            // Every change is already saved; write any that is still waiting out its pause.
            if (ModulePage is not null)
            {
                _ = ModulePage.FlushAsync();
            }

            TesseraSettings settings = TesseraFlyoutRequestBuilder.LoadSettings();
            // Preview: no FocusDim (Z-order fights Transparent/Topmost), land on Host's monitor.
            settings.UseFocusDim = false;
            FlyoutRequest request = new TesseraFlyoutRequestBuilder().Build(_services, settings, FlyoutKinds.Volume);
            request = request with { MonitorIndex = ResolveHostMonitorIndex(settings.MonitorIndex) };
            _hostUi.PreviewFlyout(request);
            StatusMessage =
                $"Tessera preview → monitor {request.MonitorIndex}. If blank, check {AppPaths.FlyoutLogPath}";
        }

        private static int ResolveHostMonitorIndex(int fallback)
        {
            try
            {
                if (Application.Current?.ApplicationLifetime
                    is not Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                    || desktop.MainWindow is not { } main)
                {
                    return Math.Clamp(fallback, 1, 8);
                }

                List<Screen> screens = main.Screens?.All?.ToList() ?? [];
                if (screens.Count == 0)
                {
                    return Math.Clamp(fallback, 1, 8);
                }

                Screen screen = main.Screens?.ScreenFromWindow(main) ?? screens.FirstOrDefault(s => s.IsPrimary) ?? screens[0];
                int idx = screens.FindIndex(s => s.WorkingArea == screen.WorkingArea && Math.Abs(s.Scaling - screen.Scaling) < 0.001);
                return idx >= 0 ? idx + 1 : Math.Clamp(fallback, 1, 8);
            }
            catch
            {
                return Math.Clamp(fallback, 1, 8);
            }
        }

        partial void OnSelectedTesseraPositionChanged(TesseraNamedChoice? value)
        {
            if (value is not null) TesseraPosition = value.Code;
        }


        partial void OnTesseraEaseFamilyChanged(string value)
        {
            if (_syncingEaseParts) return;
            TesseraEaseVariantEnabled = !value.Equals("Linear", StringComparison.OrdinalIgnoreCase);
            SyncAniEaseFromParts();
        }

        partial void OnTesseraEaseVariantChanged(string value)
        {
            if (_syncingEaseParts) return;
            SyncAniEaseFromParts();
        }

        private void ApplyEasePartsFromStored(string ease)
        {
            (string? family, string? variant) = TesseraFlyoutAnimationPolicy.SplitEase(ease);
            _syncingEaseParts = true;
            try
            {
                TesseraEaseFamily = family;
                TesseraEaseVariant = variant;
                TesseraEaseVariantEnabled = !family.Equals("Linear", StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                _syncingEaseParts = false;
            }
        }

        private void SyncAniEaseFromParts()
        {
            TesseraAniEase = TesseraFlyoutAnimationPolicy.ComposeEase(TesseraEaseFamily, TesseraEaseVariant);
        }

        private void EnsureLaunchTargetsLoaded()
        {
            if (LaunchTargetChoices.Count > 0)
            {
                return;
            }

            foreach (string label in LaunchTargetCatalog.DisplayLabels())
            {
                LaunchTargetChoices.Add(label);
            }
        }

        /// <summary>Arms the capability if needed and opens its overlay so its effect can be seen.</summary>
        private async Task OpenOverlayPreviewAsync(string id)
        {
            if (_capabilityHost is null)
            {
                return;
            }

            bool ok = _capabilityHost.IsArmed(id) || await _capabilityHost.ArmAsync(id);
            if (!ok)
            {
                StatusMessage = $"Could not arm {id}. Install it from Tiles first.";
                return;
            }

            string? err = _capabilityHost.GetHotkeyError(id);
            if (!string.IsNullOrWhiteSpace(err))
            {
                StatusMessage = err;
            }

            if (ModuleIds.IsTessera(id))
            {
                PreviewTesseraFlyout();
                return;
            }

            if (_runtime.IsRunning(id))
            {
                _tileHost?.Focus(id);
            }
            else
            {
                _ = _runtime.Start(id);
            }

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
                _ = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "https://github.com/uairhahs/MosaicShell",
                    UseShellExecute = true
                });
            }
            catch (Exception ex) { StatusMessage = ex.Message; }
        }

        [RelayCommand]
        private void ToggleAutostart()
        {
            AutostartEnabled = !AutostartEnabled;
            _services.Autostart.SetEnabled(AutostartEnabled);
            Hub.AutostartHost = AutostartEnabled;
            ModuleSettingsStore.Save("Hub", Hub);
            StatusMessage = AutostartEnabled
                ? "MosaicShell will start when you sign in."
                : "MosaicShell will not start when you sign in.";
        }

        [RelayCommand]
        private void SetCloseMinimizesToTray(string? value)
        {
            bool minimize = !string.Equals(value, "exit", StringComparison.OrdinalIgnoreCase);
            CloseMinimizesToTray = minimize;
            Hub.CloseMinimizesToTray = minimize;
            ModuleSettingsStore.Save("Hub", Hub);
            StatusMessage = minimize
                ? "Close button minimizes to tray."
                : "Close button exits the app.";
        }

        /// <summary>The Settings toggle: whether the OS reduce-motion setting turns flyout slides into fades.</summary>
        public bool FollowSystemReducedMotion
        {
            get => Hub.FollowSystemReducedMotion;
            set
            {
                if (Hub.FollowSystemReducedMotion == value)
                {
                    return;
                }

                Hub.FollowSystemReducedMotion = value;
                Theming.ReducedMotion.FollowSystem = value;
                ModuleSettingsStore.Save("Hub", Hub);
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsReducedMotionInEffect));
                OnPropertyChanged(nameof(TesseraMotionSettingsOpacity));
            }
        }

        /// <summary>Reduced motion is on and the system asks for it, so Tessera's motion settings change nothing.</summary>
        public bool IsReducedMotionInEffect => Theming.ReducedMotion.IsOn;

        public string ReducedMotionNotice => Core.HostPlatform.ReducedMotionPolicy.MotionSettingsNotice;

        public double TesseraMotionSettingsOpacity =>
            IsReducedMotionInEffect ? Core.HostPlatform.ReducedMotionPolicy.MotionSettingsDimmedOpacity : 1;

        /// <summary>What the OS says now, so the Settings row can tell whether turning it on changes anything.</summary>
        public string ReducedMotionStatus => Theming.ReducedMotion.SystemReduced
            ? "Your system's animation effects are off, so this is in effect while it is on."
            : "Your system's animation effects are on, so this changes nothing right now.";

        partial void OnCloseMinimizesToTrayChanged(bool value)
        {
            OnPropertyChanged(nameof(CloseHint));

            // Keep Hub in sync when bound ToggleButton/CheckBox flips the property
            if (Hub.CloseMinimizesToTray == value) return;
            Hub.CloseMinimizesToTray = value;
            ModuleSettingsStore.Save("Hub", Hub);
        }

        [RelayCommand]
        private async Task CheckUpdatesAsync()
        {
            IsBusy = true;
            UpdateStatus = "Checking...";
            try
            {
                using HttpClient http = new() { Timeout = TimeSpan.FromMinutes(5) };
                UpdateCheckResult result = await UpdateChecker.CheckGitHubAsync(http, currentVersion: HostBuildLabel);
                if (!result.UpdateAvailable)
                {
                    UpdateStatus = $"Up to date ({result.CurrentVersion}).";
                    StatusMessage = UpdateStatus;
                    return;
                }

                UpdateStatus = $"Update available: {result.LatestVersion} (you have {result.CurrentVersion})";
                StatusMessage = UpdateStatus;
                UpdateBanner = HubBanner.UpdateReady(result.LatestVersion ?? "");

                if (string.IsNullOrWhiteSpace(result.SetupDownloadUrl))
                {
                    UpdateStatus += "; no Setup.exe on release (opening release page).";
                    StatusMessage = UpdateStatus;
                    if (!string.IsNullOrWhiteSpace(result.ReleaseUrl))
                    {
                        TryOpenUrl(result.ReleaseUrl!);
                    }

                    return;
                }

                if (!ConfirmHostUpdate(result.LatestVersion!))
                {
                    UpdateStatus = "Update cancelled.";
                    StatusMessage = UpdateStatus;
                    return;
                }

                UpdateStatus = "Downloading Setup...";
                StatusMessage = UpdateStatus;
                string setupPath = await UpdateChecker.DownloadSetupAsync(http, result);

                UpdateStatus = "Launching Setup...";
                StatusMessage = UpdateStatus;
                _ = HostUpdateApplier.LaunchSetup(setupPath);
                UpdateStatus = "Setup started. Host will close for upgrade.";
                StatusMessage = UpdateStatus;

                // Give Setup a moment to start before we exit (Inno /CLOSEAPPLICATIONS also closes us).
                await Task.Delay(750);
                if (Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
                {
                    desktop.Shutdown();
                }
            }
            catch (Exception ex)
            {
                UpdateStatus = $"Update failed: {ex.Message}";
                StatusMessage = UpdateStatus;
            }
            finally { IsBusy = false; }
        }

        private static bool ConfirmHostUpdate(string latestVersion)
        {
            // Native confirm; Avalonia has no built-in MessageBox in this Host.
            const uint mbYesNo = 0x00000004;
            const uint mbIconQuestion = 0x00000020;
            const uint idYes = 6;
            string text =
                $"Download and install MosaicShell {latestVersion} now?\n\n" +
                "The silent Setup will close MosaicShell, upgrade files, then you can relaunch.";
            int result = NativeMessageBox(IntPtr.Zero, text, "MosaicShell update", mbYesNo | mbIconQuestion);
            return result == idYes;
        }

        private static void TryOpenUrl(string url)
        {
            try
            {
                _ = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch { /* ignore */ }
        }

        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode, EntryPoint = "MessageBoxW")]
        private static extern int NativeMessageBox(IntPtr hWnd, string text, string caption, uint type);

        [RelayCommand]
        private async Task BatchInstallAsync()
        {
            List<LibraryItemViewModel> selected = [.. Modules.Concat(Widgets).Where(m => m.IsSelectedForBatch && !m.IsInstalled)];
            if (selected.Count == 0)
            {
                StatusMessage = "Select modules in Tiles (checkbox) first.";
                return;
            }
            IsBusy = true;
            try
            {
                foreach (LibraryItemViewModel item in selected)
                {
                    StatusMessage = $"Installing {item.Name}…";
                    await _installer.InstallAsync(item.Id);
                    item.ApplyInstalled(_capabilityHost?.IsArmed(item.Id) == true);
                }
                StatusMessage = $"Batch installed {selected.Count} module(s).";
            }
            catch (Exception ex) { StatusMessage = ex.Message; }
            finally { IsBusy = false; RefreshLibrary(); }
        }

        [RelayCommand]
        private async Task InstallModuleAsync(LibraryItemViewModel? item)
        {
            if (item is null || IsBusy)
            {
                return;
            }

            if (item.IsInstalled)
            {
                if (item.IsCapability)
                {
                    if (_capabilityHost is null)
                    {
                        StatusMessage = "Capability daemon not available.";
                        return;
                    }
                    if (_capabilityHost.IsArmed(item.Id))
                    {
                        _ = await _capabilityHost.DisarmAsync(item.Id);
                        item.ApplyArmed(false);
                        StatusMessage = $"Disarmed {item.Name}.";
                    }
                    else
                    {
                        bool ok = await _capabilityHost.ArmAsync(item.Id);
                        item.ApplyArmed(ok);
                        if (!ok)
                        {
                            StatusMessage = $"Could not arm {item.Name}.";
                        }
                        else
                        {
                            string? err = _capabilityHost.GetHotkeyError(item.Id);
                            StatusMessage = string.IsNullOrWhiteSpace(err)
                                ? $"{ModuleUsageGuide.ArmedStatus(item.Id)}. {ModuleUsageGuide.HowToTrigger(item.Id, _services.Features)}"
                                : err;
                        }
                    }
                    return;
                }

                if (_runtime.IsRunning(item.Id))
                {
                    _ = _runtime.Stop(item.Id);
                    item.ApplyRunning(false);
                    StatusMessage = $"Stopped {item.Name}.";
                    return;
                }
                ModuleLaunchResult launch = _launcher.TryLaunch(item.Id);
                item.ApplyRunning(launch.Started);
                StatusMessage = launch.Message;
                return;
            }

            IsBusy = true;
            item.IsInstalling = true;
            try
            {
                await _installer.InstallAsync(item.Id);
                ModuleManifest? manifest = ModuleManifest.TryLoad(item.Id);
                item.ApplyInstalled(false);
                StatusMessage = item.IsCapability
                    ? $"Installed {item.Name}. Use the play button to arm the capability host."
                    : $"Installed {item.Name}. Use play to launch the overlay.";

                if (manifest?.DefaultArmed == true && _capabilityHost is not null)
                {
                    bool ok = await _capabilityHost.ArmAsync(item.Id);
                    item.ApplyArmed(ok);
                    if (ok)
                    {
                        StatusMessage = $"Installed and armed {item.Name}.";
                    }
                }
            }
            catch (Exception ex)
            {
                item.ApplyNotInstalled();
                StatusMessage = $"Install failed: {ex.Message}";
            }
            finally
            {
                item.IsInstalling = false;
                IsBusy = false;
            }
        }

        [RelayCommand]
        private void ImportShp()
        {
            string? path = Environment.GetEnvironmentVariable("MOSAICSHELL_SHP");
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                StatusMessage = "Set MOSAICSHELL_SHP to an .shp path, or use: Mosaicist import-shp <file>";
                return;
            }
            ShpImportResult result = ShpImporter.Import(path);
            StatusMessage = result.Message;
        }

        private void SyncServiceProbe()
        {
            try
            {
                SystemMetricsSnapshot m = _services.Metrics.Sample();
                string media = _services.Media.Current?.Title ?? "(none)";
                string armed = _capabilityHost is null ? "0" : string.Join(",", _capabilityHost.ArmedModuleIds);
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
            List<LibraryItemViewModel> modules = [.. ModuleCatalog.Modules.Select(m =>
                LibraryItemViewModel.From(m, running: false, armed: _capabilityHost?.IsArmed(m.Id) == true))];
            List<LibraryItemViewModel> widgets = [.. ModuleCatalog.Widgets.Select(w =>
                LibraryItemViewModel.From(w, running: _runtime.IsRunning(w.Id), armed: false))];

            // Every one of the kind, sorted by state: on, then installed and off, then not added.
            foreach (LibraryItemViewModel item in ModuleListOrder.Sort(modules, static i => i.ListState, static i => i.Id))
            {
                Modules.Add(item);
            }

            foreach (LibraryItemViewModel item in ModuleListOrder.Sort(widgets, static i => i.ListState, static i => i.Id))
            {
                Widgets.Add(item);
            }

            ApplyLibrarySearch();
            RefreshShell();
        }

        private sealed class NullTileHost : ITileSurfaceHost
        {
            public bool Show(string moduleId, out string? error) { error = "design-time"; return false; }
            public void Focus(string moduleId) { }
            public void Close(string moduleId) { }
        }
    }

    public sealed record DiscoverCard(string Title, string Body, string TargetPage, MaterialIconKind GlyphKind, IBrush GlyphBrush);

    public partial class LibraryItemViewModel : ObservableObject
    {
        public required string Id { get; init; }
        public required string Name { get; init; }
        public required string Description { get; init; }
        public required string IconPath { get; init; }
        public required MaterialIconKind GlyphKind { get; init; }
        public required IBrush GlyphBrush { get; init; }
        public bool IsCapability { get; init; }

        public string SettingsAutomationName => $"{Name} settings";
        public string RemoveAutomationName => $"Remove {Name}";

        [ObservableProperty] private bool _isInstalled;
        [ObservableProperty] private bool _isInstalling;
        [ObservableProperty] private bool _isRunning;
        [ObservableProperty] private bool _isArmed;
        [ObservableProperty] private bool _isSelectedForBatch;
        [ObservableProperty] private bool _isShownBySearch = true;
        [ObservableProperty] private string _statusText = "(Not Installed)";
        [ObservableProperty] private IBrush _statusBrush = HubGlyphUi.StatusBrush(HubTileStatusKind.NotInstalled);
        [ObservableProperty] private MaterialIconKind _actionIcon = MaterialIconKind.Download;
        [ObservableProperty] private string _actionToolTip = "Install";

        public void ApplyNotInstalled()
        {
            IsInstalled = false;
            IsRunning = false;
            IsArmed = false;
            StatusText = "(Not Installed)";
            SyncStatusChrome();
            RefreshActionState();
        }

        public void ApplyInstalled(bool armed = false)
        {
            IsInstalled = true;
            IsRunning = false;
            IsArmed = armed;
            StatusText = IsCapability ? armed ? ModuleUsageGuide.ArmedStatus(Id) : "Ready to arm" : "Ready";

            SyncStatusChrome();
            RefreshActionState();
        }

        public void ApplyRunning(bool running)
        {
            IsRunning = running;
            if (!IsInstalled || IsCapability)
            {
                return;
            }

            StatusText = running ? "Running" : "Ready";
            SyncStatusChrome();
            RefreshActionState();
        }

        public void ApplyArmed(bool armed)
        {
            IsArmed = armed;
            if (!IsInstalled || !IsCapability)
            {
                return;
            }

            StatusText = armed ? ModuleUsageGuide.ArmedStatus(Id) : "Ready to arm";
            SyncStatusChrome();
            RefreshActionState();
        }

        private void SyncStatusChrome()
        {
            HubTileStatusKind kind = HubTileStatusChromeSpec.Resolve(IsInstalled, IsCapability, IsArmed, IsRunning);
            StatusBrush = HubGlyphUi.StatusBrush(kind);
        }

        private void RefreshActionState()
        {
            if (!IsInstalled)
            {
                ActionIcon = MaterialIconKind.Download;
                ActionToolTip = "Install";
                return;
            }

            if (IsCapability)
            {
                ActionIcon = IsArmed ? MaterialIconKind.StopCircle : MaterialIconKind.Play;
                ActionToolTip = IsArmed ? "Disarm" : "Arm";
                return;
            }

            ActionIcon = IsRunning ? MaterialIconKind.Stop : MaterialIconKind.Play;
            ActionToolTip = IsRunning ? "Stop" : "Launch";
        }

        public static LibraryItemViewModel From(ModuleInfo info, bool running = false, bool armed = false)
        {
            bool installed = ModuleCatalog.IsInstalled(info.Id);
            bool isCap = info.Kind is ModuleKind.Capability or ModuleKind.Hybrid;
            string status = !installed
                ? "(Not Installed)"
                : isCap ? armed ? ModuleUsageGuide.ArmedStatus(info.Id) : "Ready to arm" : running ? "Running" : "Ready";
            HubGlyph glyph = HubGlyphCatalog.ForModule(info.Id);
            LibraryItemViewModel item = new()
            {
                Id = info.Id,
                Name = info.DisplayName,
                Description = info.Description,
                IconPath = $"/Assets/Modules/{info.Id}.png",
                GlyphKind = HubGlyphUi.Kind(glyph),
                GlyphBrush = HubGlyphUi.Brush(glyph),
                IsCapability = isCap,
                IsInstalled = installed,
                IsRunning = running && installed && !isCap,
                IsArmed = armed && installed && isCap,
                StatusText = status,
            };
            item.SyncStatusChrome();
            item.RefreshActionState();
            return item;
        }
    }


    public sealed record TesseraNamedChoice(string Code, string Label)
    {
        public override string ToString()
        {
            return Label;
        }
    }

    public sealed partial class TesseraAccentSwatchVm : ObservableObject
    {
        public required string Name { get; init; }
        public required string Hex { get; init; }
        public required IBrush Fill { get; init; }
        public bool IsSystem { get; init; }

        [ObservableProperty] private bool _isSelected;

        public static TesseraAccentSwatchVm From(TesseraAccentPreset preset)
        {
            return new()
            {
                Name = preset.Name,
                Hex = preset.Hex,
                IsSystem = preset.IsSystem,
                Fill = preset.IsSystem ? SystemFill() : new SolidColorBrush(Color.Parse(TesseraAccentColor.NormalizeOrEmpty(preset.Hex))),
            };
        }

        private static IBrush SystemFill()
        {
            return new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
                EndPoint = new RelativePoint(1, 0, RelativeUnit.Relative),
                GradientStops =
            {
                new GradientStop(Color.Parse("#CBA6F7"), 0),
                new GradientStop(Color.Parse("#89B4FA"), 0.5),
                new GradientStop(Color.Parse("#94E2D5"), 1),
            }
            };
        }
    }
}
