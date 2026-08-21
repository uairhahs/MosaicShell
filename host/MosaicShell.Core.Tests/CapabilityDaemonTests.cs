using FluentAssertions;
using MosaicShell.Core;
using MosaicShell.Core.Capabilities;
using MosaicShell.Core.Modules;
using MosaicShell.Core.Runtime;
using MosaicShell.Core.Services;

namespace MosaicShell.Core.Tests;

public class CapabilityDaemonTests : IDisposable
{
    private readonly string _root;

    public CapabilityDaemonTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "MosaicCapTests_" + Guid.NewGuid().ToString("N"));
        AppPaths.SetRootOverride(_root);
        AppPaths.EnsureLayout();
        Directory.CreateDirectory(Path.Combine(AppPaths.ModulesDirectory, "Tessera"));
        ModuleManifest.WriteDefault("Tessera");
    }

    public void Dispose()
    {
        AppPaths.ClearRootOverride();
        try { Directory.Delete(_root, true); } catch { /* ignore */ }
    }

    [Fact]
    public async Task Arm_unknown_module_returns_false()
    {
        var daemon = CreateDaemon();
        (await daemon.ArmAsync("NoSuchModule")).Should().BeFalse();
    }

    [Fact]
    public async Task Arm_without_factory_returns_false()
    {
        Directory.CreateDirectory(Path.Combine(AppPaths.ModulesDirectory, "Chrono"));
        var daemon = CreateDaemon(registerTessera: false);
        (await daemon.ArmAsync("Chrono")).Should().BeFalse();
    }

    [Fact]
    public async Task Arm_and_disarm_persists()
    {
        var daemon = CreateDaemon();
        (await daemon.ArmAsync("Tessera")).Should().BeTrue();
        daemon.IsArmed("Tessera").Should().BeTrue();
        CapabilityStore.Load().Armed.Should().Contain("Tessera");

        (await daemon.DisarmAsync("Tessera")).Should().BeTrue();
        daemon.IsArmed("Tessera").Should().BeFalse();
        CapabilityStore.Load().Armed.Should().NotContain("Tessera");
    }

    [Fact]
    public async Task Restore_rearms_persisted_modules()
    {
        var daemon1 = CreateDaemon();
        await daemon1.ArmAsync("Tessera");
        daemon1.Dispose();

        var daemon2 = CreateDaemon();
        await daemon2.RestoreAsync();
        daemon2.IsArmed("Tessera").Should().BeTrue();
    }

    [Fact]
    public async Task Uninstall_disarms()
    {
        var daemon = CreateDaemon();
        await daemon.ArmAsync("Tessera");
        await daemon.DisarmAsync("Tessera");
        ModuleUninstaller.Uninstall("Tessera");
        ModuleCatalog.IsInstalled("Tessera").Should().BeFalse();
        (await daemon.ArmAsync("Tessera")).Should().BeFalse();
    }

    private static CapabilityDaemon CreateDaemon(bool registerTessera = true)
    {
        var registry = new CapabilityRegistry();
        if (registerTessera)
            registry.Register(new FakeCapabilityFactory("Tessera"));
        var ui = new FakeUiBridge();
        return new CapabilityDaemon(registry, HostServicesFakes.Create(), ui);
    }

    private sealed class FakeCapabilityFactory(string moduleId) : ICapabilityFactory
    {
        public string ModuleId => moduleId;
        public IModuleCapability Create(ModuleManifest manifest, HostServices services, ICapabilityUiBridge ui) =>
            new FakeCapability(moduleId);
    }

    private sealed class FakeCapability(string moduleId) : IModuleCapability
    {
        public string ModuleId => moduleId;
        public bool IsArmed { get; private set; }
        public Task ArmAsync(CancellationToken cancellationToken = default)
        {
            IsArmed = true;
            return Task.CompletedTask;
        }

        public Task DisarmAsync(CancellationToken cancellationToken = default)
        {
            IsArmed = false;
            return Task.CompletedTask;
        }

        public void Dispose() { }
    }

    private sealed class FakeUiBridge : ICapabilityUiBridge
    {
        public IFlyoutPresenter Flyouts { get; } = new FakeFlyouts();
    }

    private sealed class FakeFlyouts : IFlyoutPresenter
    {
        public void Show(FlyoutRequest request) { }
        public void Hide(string moduleId) { }
        public void HideAll() { }
    }
}
