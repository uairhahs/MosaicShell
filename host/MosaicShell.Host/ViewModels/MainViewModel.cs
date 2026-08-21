using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MosaicShell.Core;
using MosaicShell.Core.Install;
using MosaicShell.Core.Modules;
using MosaicShell.Core.Runtime;
using MosaicShell.Core.Scale;
using MosaicShell.Core.Services;
using MosaicShell.Core.Settings;
using MosaicShell.Core.Shp;
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

    public MainViewModel(ITileRuntime runtime, HostServices services, AvaloniaTileSurfaceHost? tileHost)
    {
        _runtime = runtime;
        _launcher = new ModuleLauncher(runtime);
        _services = services;
        _tileHost = tileHost;

        AppPaths.EnsureLayout();
        var settings = ScaleSettingsStore.Load();
        settings.DpiScale = DpiProbe.GetDpiScale();
        _scale = ScaleContract.FromSettings(settings);
        ScaleSettingsStore.Save(_scale.ToSettings());
        Hub = ModuleSettingsStore.Load("Hub", () => new HubSettings());

        DiscoverCards =
        [
            new("Library", "Install, launch, and uninstall native tiles.", "Library", "/Assets/Modules/Tessera.png"),
            new("Settings", "Scale, appearance, autostart, services probe.", "Settings", "/Assets/Modules/Slate.png"),
            new("Welcome", "First-run picks, batch install, startup.", "Welcome", "/Assets/Modules/Inlay.png"),
            new("About", "MosaicShell native host — no Rainmeter runtime.", "About", "/Assets/logo-256.png"),
        ];

        RefreshLibrary();
        SyncScaleProps();
        SyncServiceProbe();
        Navigate(Hub.WelcomeCompleted ? "Discover" : "Welcome");
    }

    public MainViewModel() : this(
        new TileRuntime(new NullTileHost()),
        HostServicesFakes.Create(),
        null!)
    {
    }

    public ObservableCollection<DiscoverCard> DiscoverCards { get; }
    public ObservableCollection<LibraryItemViewModel> Modules { get; } = [];
    public ObservableCollection<LibraryItemViewModel> Widgets { get; } = [];
    public HubSettings Hub { get; }

    [ObservableProperty] private string _selectedPage = "Discover";
    [ObservableProperty] private bool _isDiscover = true;
    [ObservableProperty] private bool _isLibrary;
    [ObservableProperty] private bool _isSettings;
    [ObservableProperty] private bool _isAbout;
    [ObservableProperty] private bool _isWelcome;
    [ObservableProperty] private double _layoutScale = 1.0;
    [ObservableProperty] private double _dpiScale = 1.0;
    [ObservableProperty] private double _userScale = 1.0;
    [ObservableProperty] private double _uiScale = 1.0;
    [ObservableProperty] private string _scaleSummary = "";
    [ObservableProperty] private string _statusMessage = "Ready";
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _serviceProbe = "";
    [ObservableProperty] private string _chronoStyle = "Center";
    [ObservableProperty] private bool _chronoSeconds = true;
    [ObservableProperty] private bool _autostartEnabled;
    [ObservableProperty] private string _updateStatus = "";

    public void RestoreSessions()
    {
        foreach (var s in SessionStore.Load())
        {
            if (ModuleCatalog.IsInstalled(s.ModuleId))
                _runtime.Start(s.ModuleId);
        }
        RefreshLibrary();
    }

    [RelayCommand]
    private void Navigate(string page)
    {
        SelectedPage = page;
        IsDiscover = page == "Discover";
        IsLibrary = page == "Library";
        IsSettings = page == "Settings";
        IsAbout = page == "About";
        IsWelcome = page == "Welcome";
        if (IsLibrary) RefreshLibrary();
        if (IsSettings)
        {
            SyncScaleProps();
            SyncServiceProbe();
            var chrono = ModuleSettingsStore.Load("Chrono", () => new ChronoSettings());
            ChronoStyle = chrono.Style;
            ChronoSeconds = chrono.ShowSeconds;
            AutostartEnabled = _services.Autostart.IsEnabled;
        }
    }

    [RelayCommand] private void OpenCard(DiscoverCard? card) { if (card is not null) Navigate(card.TargetPage); }

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

    [RelayCommand]
    private void SaveChronoSettings()
    {
        var s = ModuleSettingsStore.Load("Chrono", () => new ChronoSettings());
        s.Style = ChronoStyle;
        s.ShowSeconds = ChronoSeconds;
        ModuleSettingsStore.Save("Chrono", s);
        StatusMessage = "Chrono settings saved — relaunch tile to apply.";
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
        Navigate("Discover");
        StatusMessage = selected.Count == 0 ? "Welcome completed." : $"Installed {selected.Count} module(s).";
    }

    [RelayCommand]
    private async Task BatchInstallAsync()
    {
        var selected = Modules.Concat(Widgets).Where(m => m.IsSelectedForBatch && !m.IsInstalled).ToList();
        if (selected.Count == 0)
        {
            StatusMessage = "Select modules in Library (checkbox) first.";
            return;
        }
        IsBusy = true;
        try
        {
            foreach (var item in selected)
            {
                StatusMessage = $"Installing {item.Name}…";
                await _installer.InstallAsync(item.Id);
                item.ApplyInstalled();
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
            item.ApplyInstalled();
            StatusMessage = $"Installed {item.Name}. ▶ launches native overlay.";
        }
        catch (Exception ex)
        {
            item.StatusText = "(Not Installed)";
            item.ActionGlyph = "+";
            StatusMessage = $"Install failed: {ex.Message}";
        }
        finally
        {
            item.IsInstalling = false;
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void UninstallModule(LibraryItemViewModel? item)
    {
        if (item is null || !item.IsInstalled) return;
        if (ModuleUninstaller.Uninstall(item.Id, _runtime))
        {
            StatusMessage = $"Uninstalled {item.Name}.";
            RefreshLibrary();
        }
    }

    [RelayCommand]
    private void ImportShp()
    {
        // Path via env for headless; Host can set StatusMessage with instruction
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
        StatusMessage = $"Scale saved — layout ×{LayoutScale:0.##}";
    }

    private void SyncScaleProps()
    {
        DpiScale = _scale.DpiScale;
        UserScale = _scale.UserScale;
        UiScale = _scale.UiScale;
        LayoutScale = _scale.UserScale;
        ScaleSummary = $"{UiScale * 100:0}% effective  (OS DPI {_scale.DpiScale:0.##} × user {_scale.UserScale:0.##})";
    }

    private void SyncServiceProbe()
    {
        try
        {
            var m = _services.Metrics.Sample();
            var media = _services.Media.Current?.Title ?? "(none)";
            ServiceProbe =
                $"CPU {m.CpuPercent:0}% · RAM {m.RamUsedPercent:0}% · Vol {_services.Audio.MasterVolume:0%} · Media {media}";
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
            Modules.Add(LibraryItemViewModel.From(m, _runtime.IsRunning(m.Id)));
        foreach (var w in ModuleCatalog.Widgets)
            Widgets.Add(LibraryItemViewModel.From(w, _runtime.IsRunning(w.Id)));
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

    [ObservableProperty] private bool _isInstalled;
    [ObservableProperty] private bool _isInstalling;
    [ObservableProperty] private bool _isRunning;
    [ObservableProperty] private bool _isSelectedForBatch;
    [ObservableProperty] private string _statusText = "(Not Installed)";
    [ObservableProperty] private string _actionGlyph = "+";

    public void ApplyInstalled()
    {
        IsInstalled = true;
        IsRunning = false;
        StatusText = "Ready";
        ActionGlyph = "▶";
    }

    public void ApplyRunning(bool running)
    {
        IsRunning = running;
        if (!IsInstalled) return;
        StatusText = running ? "Running" : "Ready";
        ActionGlyph = running ? "■" : "▶";
    }

    public static LibraryItemViewModel From(ModuleInfo info, bool running = false)
    {
        var installed = ModuleCatalog.IsInstalled(info.Id);
        return new LibraryItemViewModel
        {
            Id = info.Id,
            Name = info.DisplayName,
            Description = info.Description,
            IconPath = $"/Assets/Modules/{info.Id}.png",
            IsInstalled = installed,
            IsRunning = running && installed,
            StatusText = !installed ? "(Not Installed)" : running ? "Running" : "Ready",
            ActionGlyph = !installed ? "+" : running ? "■" : "▶"
        };
    }
}
