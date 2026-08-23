using FluentAssertions;
using MosaicShell.Core;
using MosaicShell.Core.Install;
using MosaicShell.Core.Modules;
using MosaicShell.Core.Runtime;
using MosaicShell.Core.Settings;

namespace MosaicShell.Core.Tests;

public class HostInstallServiceTests : IDisposable
{
    private readonly string _home;
    private readonly string _bundle;
    private readonly string _target;
    private readonly string _startMenu;
    private readonly string _startup;

    public HostInstallServiceTests()
    {
        _home = Path.Combine(Path.GetTempPath(), "ms-hi-home-" + Guid.NewGuid().ToString("N"));
        _bundle = Path.Combine(Path.GetTempPath(), "ms-hi-bundle-" + Guid.NewGuid().ToString("N"));
        _target = Path.Combine(Path.GetTempPath(), "ms-hi-target-" + Guid.NewGuid().ToString("N"));
        _startMenu = Path.Combine(Path.GetTempPath(), "ms-hi-sm-" + Guid.NewGuid().ToString("N"));
        _startup = Path.Combine(Path.GetTempPath(), "ms-hi-su-" + Guid.NewGuid().ToString("N"));
        AppPaths.SetRootOverride(_home);
        AppPaths.EnsureLayout();
        SeedBundle(_bundle);
    }

    public void Dispose()
    {
        AppPaths.ClearRootOverride();
        foreach (var d in new[] { _home, _bundle, _target, _startMenu, _startup })
        {
            try { Directory.Delete(d, recursive: true); } catch { /* ignore */ }
        }
    }

    [Fact]
    public void DotNetRuntimeProbe_finds_major_10_under_shared_desktop()
    {
        var root = Path.Combine(Path.GetTempPath(), "ms-dotnet-" + Guid.NewGuid().ToString("N"));
        try
        {
            var verDir = Path.Combine(root, "shared", "Microsoft.WindowsDesktop.App", "10.0.1");
            Directory.CreateDirectory(verDir);
            var result = DotNetRuntimeProbe.Probe(root);
            result.IsDesktopRuntimePresent.Should().BeTrue();
            result.DetectedVersion.Should().Be("10.0.1");
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { /* ignore */ }
        }
    }

    [Fact]
    public void DotNetRuntimeProbe_missing_when_no_desktop_folder()
    {
        var root = Path.Combine(Path.GetTempPath(), "ms-dotnet-empty-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            DotNetRuntimeProbe.Probe(root).IsDesktopRuntimePresent.Should().BeFalse();
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { /* ignore */ }
        }
    }

    [Fact]
    public void StartupShortcutPolicy_formats_url_with_optional_args()
    {
        var text = StartupShortcutPolicy.FormatUrlShortcut(@"C:\Apps\MosaicShell.Host.exe", "--tray-only");
        text.Should().Contain("URL=file:///C:/Apps/MosaicShell.Host.exe --tray-only");
        text.Should().Contain("IconFile=C:/Apps/MosaicShell.Host.exe");
    }

    [Fact]
    public async Task Install_copies_host_mosaicist_tiles_and_modules()
    {
        var stages = new List<string>();
        var service = new HostInstallService();
        var result = await service.InstallAsync(new HostInstallRequest
        {
            BundleRoot = _bundle,
            TargetDirectory = _target,
            Variant = HostInstallVariant.SelfContained,
            ModuleIds = ["Canvas"],
            CreateStartMenuShortcut = true,
            EnableLogonAutostart = true,
            StartMenuDirectoryOverride = _startMenu,
            StartupDirectoryOverride = _startup,
        }, new Progress<HostInstallProgress>(p => stages.Add(p.Stage)));

        File.Exists(result.HostExePath).Should().BeTrue();
        File.Exists(Path.Combine(_target, "Mosaicist", "Mosaicist.exe")).Should().BeTrue();
        File.Exists(Path.Combine(_target, "Tiles", "Canvas", "module.native.json")).Should().BeTrue();
        ModuleCatalog.IsInstalled("Canvas").Should().BeTrue();
        result.InstalledModules.Should().Contain("Canvas");
        result.Version.Should().Be("2026.1.1-b1");

        File.Exists(Path.Combine(_startMenu, HostInstallPolicy.StartMenuShortcutFileName)).Should().BeTrue();
        var startup = await File.ReadAllTextAsync(Path.Combine(_startup, HostInstallPolicy.StartupShortcutFileName));
        startup.Should().Contain("--tray-only");

        stages.Should().Contain("copy");
        stages.Should().Contain("modules");
        stages.Should().Contain("shortcuts");
        stages.Should().Contain("done");

        var hub = ModuleSettingsStore.Load("Hub", () => new HubSettings());
        hub.WelcomeCompleted.Should().BeFalse();
        hub.AutostartHost.Should().BeTrue();
    }

    [Fact]
    public async Task Framework_install_fails_when_desktop_runtime_missing()
    {
        var emptyDotNet = Path.Combine(Path.GetTempPath(), "ms-nodotnet-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(emptyDotNet);
        try
        {
            var service = new HostInstallService();
            var act = () => service.InstallAsync(new HostInstallRequest
            {
                BundleRoot = _bundle,
                TargetDirectory = _target,
                Variant = HostInstallVariant.FrameworkDependent,
                ModuleIds = [],
                CreateStartMenuShortcut = false,
                DotNetRootOverride = emptyDotNet,
            });
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Desktop Runtime*");
        }
        finally
        {
            try { Directory.Delete(emptyDotNet, recursive: true); } catch { /* ignore */ }
        }
    }

    [Fact]
    public void ResolveHostFolder_prefers_Host_sc_for_self_contained()
    {
        var root = Path.Combine(Path.GetTempPath(), "ms-vars-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(Path.Combine(root, "Host-fw"));
            Directory.CreateDirectory(Path.Combine(root, "Host-sc"));
            Directory.CreateDirectory(Path.Combine(root, "Mosaicist"));
            Directory.CreateDirectory(Path.Combine(root, "Tiles"));
            File.WriteAllText(Path.Combine(root, "Host-fw", "MosaicShell.Host.exe"), "fw");
            File.WriteAllText(Path.Combine(root, "Host-sc", "MosaicShell.Host.exe"), "sc");
            File.WriteAllText(Path.Combine(root, "Mosaicist", "Mosaicist.exe"), "m");

            ReleaseBundleLayout.ResolveHostFolder(root, HostInstallVariant.SelfContained)
                .Should().EndWith("Host-sc");
            ReleaseBundleLayout.ResolveHostFolder(root, HostInstallVariant.FrameworkDependent)
                .Should().EndWith("Host-fw");
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { /* ignore */ }
        }
    }

    private static void SeedBundle(string root)
    {
        Directory.CreateDirectory(Path.Combine(root, "Host"));
        Directory.CreateDirectory(Path.Combine(root, "Mosaicist"));
        Directory.CreateDirectory(Path.Combine(root, "Tiles", "Canvas"));
        File.WriteAllText(Path.Combine(root, "Host", "MosaicShell.Host.exe"), "host-bin");
        File.WriteAllText(Path.Combine(root, "Mosaicist", "Mosaicist.exe"), "cli-bin");
        File.WriteAllText(
            Path.Combine(root, "Tiles", "Canvas", "module.native.json"),
            """{"id":"Canvas","runtime":"avalonia"}""");
        File.WriteAllText(Path.Combine(root, "VERSION.txt"), "2026.1.1-b1\n");
    }
}
