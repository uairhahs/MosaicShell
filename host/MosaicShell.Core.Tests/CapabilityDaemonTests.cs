using FluentAssertions;
using MosaicShell.Core.Capabilities;
using MosaicShell.Core.Capabilities.Platform;
using MosaicShell.Core.Modules;
using MosaicShell.Core.Runtime;
using MosaicShell.Core.Services;

namespace MosaicShell.Core.Tests
{
    public class CapabilityDaemonTests : IDisposable
    {
        private readonly string _root;

        public CapabilityDaemonTests()
        {
            _root = Path.Combine(Path.GetTempPath(), "MosaicCapTests_" + Guid.NewGuid().ToString("N"));
            AppPaths.SetRootOverride(_root);
            AppPaths.EnsureLayout();
            _ = Directory.CreateDirectory(Path.Combine(AppPaths.ModulesDirectory, "Tessera"));
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
            CapabilityDaemon daemon = CreateDaemon();
            _ = (await daemon.ArmAsync("NoSuchModule")).Should().BeFalse();
        }

        [Fact]
        public async Task Arm_without_factory_returns_false()
        {
            _ = Directory.CreateDirectory(Path.Combine(AppPaths.ModulesDirectory, "Chrono"));
            CapabilityDaemon daemon = CreateDaemon(registerTessera: false);
            _ = (await daemon.ArmAsync("Chrono")).Should().BeFalse();
        }

        [Fact]
        public async Task Arm_and_disarm_persists()
        {
            CapabilityDaemon daemon = CreateDaemon();
            _ = (await daemon.ArmAsync("Tessera")).Should().BeTrue();
            _ = daemon.IsArmed("Tessera").Should().BeTrue();
            _ = CapabilityStore.Load().Armed.Should().Contain("Tessera");

            _ = (await daemon.DisarmAsync("Tessera")).Should().BeTrue();
            _ = daemon.IsArmed("Tessera").Should().BeFalse();
            _ = CapabilityStore.Load().Armed.Should().NotContain("Tessera");
        }

        [Fact]
        public async Task Restore_rearms_persisted_modules()
        {
            CapabilityDaemon daemon1 = CreateDaemon();
            _ = await daemon1.ArmAsync("Tessera");
            daemon1.Dispose();

            CapabilityDaemon daemon2 = CreateDaemon();
            await daemon2.RestoreAsync();
            _ = daemon2.IsArmed("Tessera").Should().BeTrue();
        }

        [Fact]
        public async Task Uninstall_disarms()
        {
            CapabilityDaemon daemon = CreateDaemon();
            _ = await daemon.ArmAsync("Tessera");
            _ = await daemon.DisarmAsync("Tessera");
            _ = ModuleUninstaller.Uninstall("Tessera");
            _ = ModuleCatalog.IsInstalled("Tessera").Should().BeFalse();
            _ = (await daemon.ArmAsync("Tessera")).Should().BeFalse();
        }

        private static CapabilityDaemon CreateDaemon(bool registerTessera = true)
        {
            CapabilityRegistry registry = new();
            if (registerTessera)
            {
                registry.Register(new FakeCapabilityFactory("Tessera"));
            }

            FakeUiBridge ui = new();
            return new CapabilityDaemon(registry, HostServicesFakes.Create(), ui);
        }

        private sealed class FakeCapabilityFactory(string moduleId) : ICapabilityFactory
        {
            public string ModuleId => moduleId;
            public IModuleCapability Create(ModuleManifest manifest, ICapabilityContext context)
            {
                return new FakeCapability(moduleId);
            }
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
            public IFlyoutPresenter Flyouts { get; } = new NullFlyoutPresenter();
            public IHostUiBridge HostUi { get; } = NullHostUiBridge.Instance;
            public void RunOnHostThread(Action action)
            {
                action();
            }
        }
    }
}
