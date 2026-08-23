using System.Collections.ObjectModel;
using System.Diagnostics;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MosaicShell.Core.Install;
using MosaicShell.Core.Modules;

namespace MosaicShell.Installer.ViewModels;

public sealed partial class ModulePickItem : ObservableObject
{
    public required string Id { get; init; }
    public required string DisplayName { get; init; }
    public required string Description { get; init; }

    [ObservableProperty]
    private bool _isSelected;
}

public enum InstallerPage
{
    Welcome = 0,
    InstallType = 1,
    RuntimeCheck = 2,
    Location = 3,
    Modules = 4,
    Options = 5,
    Progress = 6,
    Finish = 7,
}

public sealed partial class InstallerViewModel : ObservableObject
{
    private readonly HostInstallService _service = new();
    private string? _installedHostExe;

    public InstallerViewModel()
    {
        BundleRoot = ReleaseBundleLayout.TryFindRoot() ?? "";
        VersionLabel = ReadVersion(BundleRoot);
        TargetDirectory = HostInstallPolicy.DefaultInstallDirectory;
        UseSelfContained = false;

        foreach (var m in ModuleCatalog.BuiltIns)
        {
            Modules.Add(new ModulePickItem
            {
                Id = m.Id,
                DisplayName = m.DisplayName,
                Description = m.Description,
                IsSelected = HostInstallPolicy.DefaultModuleIds.Any(id =>
                    id.Equals(m.Id, StringComparison.OrdinalIgnoreCase)),
            });
        }

        RefreshRuntimeProbe();
        UpdatePageFlags();
    }

    public ObservableCollection<ModulePickItem> Modules { get; } = [];

    [ObservableProperty] private InstallerPage _page = InstallerPage.Welcome;
    [ObservableProperty] private string _bundleRoot = "";
    [ObservableProperty] private string _versionLabel = "unknown";
    [ObservableProperty] private string _targetDirectory = "";
    [ObservableProperty] private bool _useSelfContained;
    [ObservableProperty] private bool _runtimeOk;
    [ObservableProperty] private string _runtimeStatus = "";
    [ObservableProperty] private string _runtimeDownloadUrl = HostInstallPolicy.DotNetDesktopDownloadUrl;
    [ObservableProperty] private bool _createStartMenuShortcut = true;
    [ObservableProperty] private bool _enableLogonAutostart;
    [ObservableProperty] private string _statusMessage = "";
    [ObservableProperty] private string _progressDetail = "";
    [ObservableProperty] private double _progressFraction;
    [ObservableProperty] private bool _useDownloadSource;
    [ObservableProperty] private string _gitHubRepository = ReleaseInstallSource.DefaultRepository;
    [ObservableProperty] private bool _isDownloading;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _installSucceeded;
    [ObservableProperty] private string _errorMessage = "";

    [ObservableProperty] private bool _isWelcome;
    [ObservableProperty] private bool _isInstallType;
    [ObservableProperty] private bool _isRuntimeCheck;
    [ObservableProperty] private bool _isLocation;
    [ObservableProperty] private bool _isModules;
    [ObservableProperty] private bool _isOptions;
    [ObservableProperty] private bool _isProgress;
    [ObservableProperty] private bool _isFinish;

    public bool CanGoNext => Page switch
    {
        InstallerPage.Welcome => !string.IsNullOrWhiteSpace(BundleRoot)
                                 && ReleaseBundleLayout.IsBundleRoot(BundleRoot),
        InstallerPage.InstallType => true,
        InstallerPage.RuntimeCheck => UseSelfContained || RuntimeOk,
        InstallerPage.Location => !string.IsNullOrWhiteSpace(TargetDirectory),
        InstallerPage.Modules => true,
        InstallerPage.Options => true,
        _ => false,
    };

    public bool CanGoBack => Page is > InstallerPage.Welcome and < InstallerPage.Progress && !IsBusy;

    partial void OnPageChanged(InstallerPage value) => UpdatePageFlags();
    partial void OnUseSelfContainedChanged(bool value)
    {
        RefreshRuntimeProbe();
        OnPropertyChanged(nameof(CanGoNext));
    }
    partial void OnRuntimeOkChanged(bool value) => OnPropertyChanged(nameof(CanGoNext));
    partial void OnTargetDirectoryChanged(string value) => OnPropertyChanged(nameof(CanGoNext));
    partial void OnBundleRootChanged(string value) => OnPropertyChanged(nameof(CanGoNext));
    partial void OnIsBusyChanged(bool value)
    {
        OnPropertyChanged(nameof(CanGoBack));
        OnPropertyChanged(nameof(CanGoNext));
    }

    [RelayCommand]
    private void Next()
    {
        if (!CanGoNext) return;

        if (Page == InstallerPage.InstallType && !UseSelfContained)
        {
            Page = InstallerPage.RuntimeCheck;
            RefreshRuntimeProbe();
            return;
        }

        if (Page == InstallerPage.InstallType && UseSelfContained)
        {
            Page = InstallerPage.Location;
            return;
        }

        if (Page == InstallerPage.Options)
        {
            _ = RunInstallAsync();
            return;
        }

        if (Page < InstallerPage.Finish)
            Page++;
    }

    [RelayCommand]
    private void Back()
    {
        if (!CanGoBack) return;

        if (Page == InstallerPage.Location && UseSelfContained)
        {
            Page = InstallerPage.InstallType;
            return;
        }

        if (Page == InstallerPage.Location && !UseSelfContained)
        {
            Page = InstallerPage.RuntimeCheck;
            return;
        }

        if (Page > InstallerPage.Welcome)
            Page--;
    }

    [RelayCommand]
    private void RefreshRuntime() => RefreshRuntimeProbe();

    [RelayCommand]
    private void OpenRuntimeDownload()
    {
        try
        {
            Process.Start(new ProcessStartInfo(RuntimeDownloadUrl) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task DownloadLatestAsync()
    {
        IsDownloading = true;
        StatusMessage = "Downloading latest release…";
        ErrorMessage = "";
        try
        {
            var cache = Path.Combine(Path.GetTempPath(), "MosaicShell-Installer-Cache");
            var source = new ReleaseInstallSource();
            var progress = new Progress<HostInstallProgress>(p =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    StatusMessage = string.IsNullOrWhiteSpace(p.Detail)
                        ? p.Stage
                        : $"{p.Stage}: {p.Detail}";
                });
            });
            var (bundle, release) = await source.DownloadAndExtractLatestAsync(
                cache,
                GitHubRepository,
                progress);
            BundleRoot = bundle;
            VersionLabel = release.TagName;
            StatusMessage = $"Downloaded {release.TagName}.";
            OnPropertyChanged(nameof(CanGoNext));
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            StatusMessage = "Download failed.";
        }
        finally
        {
            IsDownloading = false;
        }
    }

    [RelayCommand]
    private void BrowseBundle()
    {
        StatusMessage = "Paste or type the extracted release folder path (contains Host*/Mosaicist/Tiles).";
    }

    [RelayCommand]
    private void LaunchHost()
    {
        if (string.IsNullOrWhiteSpace(_installedHostExe) || !File.Exists(_installedHostExe))
        {
            StatusMessage = "Host executable not found.";
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(_installedHostExe) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
    }

    [RelayCommand]
    private void Exit()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            desktop.Shutdown();
    }

    private async Task RunInstallAsync()
    {
        Page = InstallerPage.Progress;
        IsBusy = true;
        InstallSucceeded = false;
        ErrorMessage = "";
        ProgressFraction = 0;
        ProgressDetail = "Starting…";

        try
        {
            var moduleIds = Modules.Where(m => m.IsSelected).Select(m => m.Id).ToList();
            var progress = new Progress<HostInstallProgress>(p =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    ProgressDetail = string.IsNullOrWhiteSpace(p.Detail)
                        ? p.Stage
                        : $"{p.Stage}: {p.Detail}";
                    if (p.Fraction is { } f)
                        ProgressFraction = f;
                });
            });

            var result = await _service.InstallAsync(new HostInstallRequest
            {
                BundleRoot = BundleRoot,
                TargetDirectory = TargetDirectory,
                Variant = UseSelfContained
                    ? HostInstallVariant.SelfContained
                    : HostInstallVariant.FrameworkDependent,
                ModuleIds = moduleIds,
                CreateStartMenuShortcut = CreateStartMenuShortcut,
                EnableLogonAutostart = EnableLogonAutostart,
                ResetWelcomeCompleted = true,
            }, progress);

            _installedHostExe = result.HostExePath;
            InstallSucceeded = true;
            StatusMessage = $"Installed to {result.TargetDirectory}";
            Page = InstallerPage.Finish;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            StatusMessage = "Install failed.";
            InstallSucceeded = false;
            Page = InstallerPage.Finish;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void RefreshRuntimeProbe()
    {
        if (UseSelfContained)
        {
            RuntimeOk = true;
            RuntimeStatus = "Self-contained install includes the runtime.";
            return;
        }

        var probe = DotNetRuntimeProbe.Probe();
        RuntimeOk = probe.IsDesktopRuntimePresent;
        RuntimeDownloadUrl = probe.DownloadUrl;
        RuntimeStatus = probe.IsDesktopRuntimePresent
            ? $"Found .NET Desktop Runtime {probe.DetectedVersion}."
            : $".NET {DotNetRuntimeProbe.RequiredMajor} Desktop Runtime was not found. Install it, then click Refresh.";
    }

    private void UpdatePageFlags()
    {
        IsWelcome = Page == InstallerPage.Welcome;
        IsInstallType = Page == InstallerPage.InstallType;
        IsRuntimeCheck = Page == InstallerPage.RuntimeCheck;
        IsLocation = Page == InstallerPage.Location;
        IsModules = Page == InstallerPage.Modules;
        IsOptions = Page == InstallerPage.Options;
        IsProgress = Page == InstallerPage.Progress;
        IsFinish = Page == InstallerPage.Finish;
        OnPropertyChanged(nameof(CanGoNext));
        OnPropertyChanged(nameof(CanGoBack));
    }

    private static string ReadVersion(string bundleRoot)
    {
        if (string.IsNullOrWhiteSpace(bundleRoot))
            return "bundle not found";
        var path = Path.Combine(bundleRoot, HostInstallLayoutSpec.VersionFileName);
        if (!File.Exists(path))
            return "unversioned";
        try { return File.ReadAllText(path).Trim(); }
        catch { return "unversioned"; }
    }
}
