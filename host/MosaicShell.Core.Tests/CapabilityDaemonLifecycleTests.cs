using FluentAssertions;
using MosaicShell.Core.Capabilities;
using MosaicShell.Core.Capabilities.Platform;
using MosaicShell.Core.Modules;
using MosaicShell.Core.Runtime;
using MosaicShell.Core.Services;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// What the daemon does around arming beyond the basic arm/disarm/restore cases in <see cref="CapabilityDaemonTests"/>:
    /// re-arming, disarm-all, the events it publishes, the flyout it hides, and disposal.
    /// </summary>
    public class CapabilityDaemonLifecycleTests : IDisposable
    {
        private readonly string _root = Path.Combine(Path.GetTempPath(), "MosaicDaemonLife_" + Guid.NewGuid().ToString("N"));
        private readonly RecordingFlyouts _flyouts = new();
        private readonly HostServices _services = HostServicesFakes.Create();
        private readonly List<FakeCapability> _created = [];

        public CapabilityDaemonLifecycleTests()
        {
            AppPaths.SetRootOverride(_root);
            AppPaths.EnsureLayout();
            foreach (string id in new[] { "Tessera", "Inlay" })
            {
                _ = Directory.CreateDirectory(Path.Combine(AppPaths.ModulesDirectory, id));
                ModuleManifest.WriteDefault(id);
            }
        }

        public void Dispose()
        {
            AppPaths.ClearRootOverride();
            try { Directory.Delete(_root, true); } catch { /* ignore */ }
        }

        private CapabilityDaemon CreateDaemon()
        {
            CapabilityRegistry registry = new();
            registry.Register(new FakeFactory("Tessera", _created));
            registry.Register(new FakeFactory("Inlay", _created));
            return new CapabilityDaemon(registry, _services, new FakeUiBridge(_flyouts));
        }

        [Fact]
        public async Task Arming_an_armed_module_is_true_and_does_not_create_a_second_instance()
        {
            using CapabilityDaemon daemon = CreateDaemon();
            _ = await daemon.ArmAsync("Tessera");

            (await daemon.ArmAsync("Tessera")).Should().BeTrue();

            _created.Should().ContainSingle();
        }

        [Fact]
        public async Task Arming_publishes_an_armed_event_and_disarming_a_disarmed_event_naming_the_module()
        {
            using CapabilityDaemon daemon = CreateDaemon();
            List<CapabilityEvent> events = [];
            using IDisposable subscription = daemon.Events.SubscribeAll(events.Add);

            _ = await daemon.ArmAsync("Tessera");
            _ = await daemon.DisarmAsync("Tessera");

            events.Select(e => (e.Kind, e.SourceModuleId)).Should().Equal(
                (CapabilityEventKind.CapabilityArmed, "Tessera"),
                (CapabilityEventKind.CapabilityDisarmed, "Tessera"));
        }

        [Fact]
        public async Task Disarming_something_that_was_never_armed_is_false_and_publishes_nothing()
        {
            using CapabilityDaemon daemon = CreateDaemon();
            List<CapabilityEvent> events = [];
            using IDisposable subscription = daemon.Events.SubscribeAll(events.Add);

            (await daemon.DisarmAsync("Tessera")).Should().BeFalse();

            events.Should().BeEmpty();
            _flyouts.Hidden.Should().BeEmpty();
        }

        [Fact]
        public async Task Disarming_disposes_the_instance_and_hides_that_modules_flyout()
        {
            using CapabilityDaemon daemon = CreateDaemon();
            _ = await daemon.ArmAsync("Tessera");

            _ = await daemon.DisarmAsync("Tessera");

            _created.Single().Disposed.Should().BeTrue();
            _flyouts.Hidden.Should().Equal("Tessera");
        }

        [Fact]
        public async Task Armed_module_ids_lists_only_modules_that_are_armed()
        {
            using CapabilityDaemon daemon = CreateDaemon();
            _ = await daemon.ArmAsync("Tessera");
            _ = await daemon.ArmAsync("Inlay");
            _ = await daemon.DisarmAsync("Inlay");

            daemon.ArmedModuleIds.Should().Equal("Tessera");
            daemon.IsArmed("Inlay").Should().BeFalse();
        }

        [Fact]
        public async Task Rearming_builds_a_fresh_instance_and_keeps_the_module_armed_and_saved()
        {
            using CapabilityDaemon daemon = CreateDaemon();
            _ = await daemon.ArmAsync("Tessera");

            (await daemon.ReArmAsync("Tessera")).Should().BeTrue();

            _created.Should().HaveCount(2);
            _created[0].Disposed.Should().BeTrue();
            daemon.IsArmed("Tessera").Should().BeTrue();
            CapabilityStore.Load().Armed.Should().Contain("Tessera");
        }

        [Fact]
        public async Task Disarm_all_disarms_everything_saves_nothing_armed_and_hides_every_flyout()
        {
            using CapabilityDaemon daemon = CreateDaemon();
            _ = await daemon.ArmAsync("Tessera");
            _ = await daemon.ArmAsync("Inlay");

            await daemon.DisarmAllAsync();

            daemon.ArmedModuleIds.Should().BeEmpty();
            CapabilityStore.Load().Armed.Should().BeEmpty();
            _flyouts.HideAllCalls.Should().Be(1);
        }

        [Fact]
        public async Task Disarming_everything_without_persisting_keeps_the_saved_armed_list_for_the_next_start()
        {
            using (CapabilityDaemon first = CreateDaemon())
            {
                _ = await first.ArmAsync("Tessera");
                _ = await first.ArmAsync("Inlay");

                // What a clean shutdown does: stop everything, but leave the saved list as it was.
                await first.DisarmAllAsync(persist: false);

                first.ArmedModuleIds.Should().BeEmpty();
                _flyouts.HideAllCalls.Should().Be(1);
            }

            CapabilityStore.Load().Armed.Should().BeEquivalentTo("Tessera", "Inlay");
            using CapabilityDaemon second = CreateDaemon();
            await second.RestoreAsync();
            second.ArmedModuleIds.Should().BeEquivalentTo("Tessera", "Inlay");
        }

        [Fact]
        public async Task Persist_saves_exactly_the_armed_modules()
        {
            using CapabilityDaemon daemon = CreateDaemon();
            _ = await daemon.ArmAsync("Tessera", persist: false);
            CapabilityStore.Load().Armed.Should().BeEmpty("persist: false must not save");

            daemon.Persist();

            CapabilityStore.Load().Armed.Should().Equal("Tessera");
        }

        [Fact]
        public async Task A_module_without_a_hotkey_reports_no_hotkey_error()
        {
            using CapabilityDaemon daemon = CreateDaemon();
            _ = await daemon.ArmAsync("Tessera");

            daemon.GetHotkeyError("Tessera").Should().BeNull();
            daemon.GetHotkeyError("NotArmed").Should().BeNull();
        }

        [Fact]
        public void Volume_changes_and_flyout_dismissals_reach_the_event_bus()
        {
            using CapabilityDaemon daemon = CreateDaemon();
            List<CapabilityEvent> events = [];
            using IDisposable subscription = daemon.Events.SubscribeAll(events.Add);

            ((FakeAudioService)_services.Audio).MasterVolume = 0.9;
            _flyouts.RaiseTransientDismissed("Tessera");

            events.Select(e => e.Kind).Should().Contain(CapabilityEventKind.VolumeChanged);
            events.Should().Contain(e => e.Kind == CapabilityEventKind.FlyoutTransientDismissed && e.SourceModuleId == "Tessera");
        }

        [Fact]
        public async Task Disposing_disposes_instances_stops_listening_and_is_safe_to_repeat()
        {
            CapabilityDaemon daemon = CreateDaemon();
            _ = await daemon.ArmAsync("Tessera");
            List<CapabilityEvent> events = [];
            using IDisposable subscription = daemon.Events.SubscribeAll(events.Add);

            daemon.Dispose();
            daemon.Dispose();
            ((FakeAudioService)_services.Audio).MasterVolume = 0.1;
            _flyouts.RaiseTransientDismissed("Tessera");

            _created.Single().Disposed.Should().BeTrue();
            events.Should().BeEmpty("a disposed daemon no longer forwards volume or dismissal events");
            await daemon.Invoking(d => d.ArmAsync("Inlay")).Should().ThrowAsync<ObjectDisposedException>();
        }

        [Fact]
        public async Task A_capability_that_throws_while_being_disposed_does_not_stop_the_daemon_disposing()
        {
            CapabilityDaemon daemon = CreateDaemon();
            _ = await daemon.ArmAsync("Tessera");
            _ = await daemon.ArmAsync("Inlay");
            _created[0].ThrowOnDispose = true;

            daemon.Invoking(d => d.Dispose()).Should().NotThrow();

            _created[1].Disposed.Should().BeTrue();
        }

        private sealed class FakeFactory(string moduleId, List<FakeCapability> created) : ICapabilityFactory
        {
            public string ModuleId => moduleId;

            public IModuleCapability Create(ModuleManifest manifest, ICapabilityContext context)
            {
                FakeCapability capability = new(moduleId);
                created.Add(capability);
                return capability;
            }
        }

        private sealed class FakeCapability(string moduleId) : IModuleCapability
        {
            public string ModuleId => moduleId;
            public bool IsArmed { get; private set; }
            public bool Disposed { get; private set; }
            public bool ThrowOnDispose { get; set; }

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

            public void Dispose()
            {
                Disposed = true;
                if (ThrowOnDispose)
                {
                    throw new InvalidOperationException("dispose failed");
                }
            }
        }

        private sealed class FakeUiBridge(IFlyoutPresenter flyouts) : ICapabilityUiBridge
        {
            public IFlyoutPresenter Flyouts => flyouts;
            public IHostUiBridge HostUi { get; } = NullHostUiBridge.Instance;

            public void RunOnHostThread(Action action)
            {
                action();
            }
        }

        private sealed class RecordingFlyouts : IFlyoutPresenter
        {
            public event Action<string>? TransientDismissed;

            public List<string> Hidden { get; } = [];
            public int HideAllCalls { get; private set; }

            public void RaiseTransientDismissed(string moduleId)
            {
                TransientDismissed?.Invoke(moduleId);
            }

            public void Show(FlyoutRequest request) { }
            public void Update(FlyoutRequest request) { }
            public void SoftRefresh(FlyoutRequest request) { }

            public void Hide(string moduleId)
            {
                Hidden.Add(moduleId);
            }

            public void HideAll()
            {
                HideAllCalls++;
            }

            public bool IsVisible(string moduleId)
            {
                return false;
            }
        }
    }
}
