using FluentAssertions;
using MosaicShell.Core.Capabilities;
using MosaicShell.Core.Capabilities.Ipc;
using MosaicShell.Core.Capabilities.Platform;
using MosaicShell.Core.Modules.Tessera;
using MosaicShell.Core.Runtime;
using MosaicShell.Core.Services;

namespace MosaicShell.Core.Tests
{
    public class CapabilityStoreMosaicTests
    {
        [Fact]
        public void SaveArmed_persists_multiple_modules()
        {
            string root = Path.Combine(Path.GetTempPath(), "MosaicCapStore_" + Guid.NewGuid().ToString("N"));
            AppPaths.SetRootOverride(root);
            AppPaths.EnsureLayout();
            try
            {
                CapabilityStore.SaveArmed(["Tessera", "Mixdeck", "Slate"]);
                List<string> loaded = CapabilityStore.Load().Armed;
                _ = loaded.Should().BeEquivalentTo(["Tessera", "Mixdeck", "Slate"]);
            }
            finally
            {
                AppPaths.ClearRootOverride();
                try { Directory.Delete(root, true); } catch { /* ignore */ }
            }
        }
    }

    public class HostLaunchOptionsTests
    {
        [Fact]
        public void Default_is_full_hub()
        {
            HostLaunchOptions.ResetForTests();
            HostLaunchOptions.Apply([]);
            _ = HostLaunchOptions.IsTrayOnly.Should().BeFalse();
        }

        [Fact]
        public void Tray_only_flag_sets_mode()
        {
            HostLaunchOptions.ResetForTests();
            HostLaunchOptions.Apply(["--tray-only"]);
            _ = HostLaunchOptions.IsTrayOnly.Should().BeTrue();
            _ = HostLaunchOptions.TesseraOsAcrylicTrial.Should().BeFalse();
        }

        [Fact]
        public void Tessera_os_acrylic_flag_is_opt_in()
        {
            HostLaunchOptions.ResetForTests();
            HostLaunchOptions.Apply([HostLaunchOptions.TesseraOsAcrylicTrialFlag]);
            _ = HostLaunchOptions.TesseraOsAcrylicTrial.Should().BeTrue();
            _ = HostLaunchOptions.IsTrayOnly.Should().BeFalse();
        }

        [Fact]
        public void Tessera_force_software_render_flag_is_opt_in()
        {
            HostLaunchOptions.ResetForTests();
            HostLaunchOptions.Apply([HostLaunchOptions.TesseraForceSoftwareRenderFlag]);
            _ = HostLaunchOptions.TesseraForceSoftwareRender.Should().BeTrue();
            _ = HostLaunchOptions.TesseraOsAcrylicTrial.Should().BeFalse();
        }

        [Fact]
        public void Apply_resets_acrylic_trial_so_alpha_rollback_is_omit_the_flag()
        {
            HostLaunchOptions.ResetForTests();
            HostLaunchOptions.Apply([HostLaunchOptions.TesseraOsAcrylicTrialFlag]);
            HostLaunchOptions.Apply(["--tray-only"]);
            _ = HostLaunchOptions.TesseraOsAcrylicTrial.Should().BeFalse();
            _ = HostLaunchOptions.IsTrayOnly.Should().BeTrue();
        }
    }

    public class CapabilityEventBusTests
    {
        [Fact]
        public void Subscribe_receives_published_kind()
        {
            CapabilityEventBus bus = new();
            CapabilityEvent? received = null;
            using IDisposable sub = bus.Subscribe(CapabilityEventKind.VolumeChanged, e => received = e);
            bus.Publish(new CapabilityEvent(CapabilityEventKind.VolumeChanged));
            _ = received.Should().NotBeNull();
            _ = received.Kind.Should().Be(CapabilityEventKind.VolumeChanged);
        }

        [Fact]
        public void SubscribeAll_receives_any_kind()
        {
            CapabilityEventBus bus = new();
            int count = 0;
            using IDisposable sub = bus.SubscribeAll(_ => count++);
            bus.Publish(new CapabilityEvent(CapabilityEventKind.MediaProgress));
            bus.Publish(new CapabilityEvent(CapabilityEventKind.CapabilityArmed, "Tessera"));
            _ = count.Should().Be(2);
        }
    }

    public class CapabilityIpcCodecTests
    {
        [Fact]
        public void Flyout_request_round_trips_through_dto()
        {
            FlyoutRequest request = new(
                "Tessera",
                "media",
                StyleId: "Fluent",
                Payload: new Dictionary<string, string> { ["title"] = "Track" });

            FlyoutRequest back = CapabilityIpcCodec.FromDto(CapabilityIpcCodec.ToDto(request));
            _ = back.ModuleId.Should().Be("Tessera");
            _ = back.Kind.Should().Be("media");
            _ = back.StyleId.Should().Be("Fluent");
            _ = back.Payload!["title"].Should().Be("Track");
        }

        [Fact]
        public void Message_frame_serializes_and_deserializes()
        {
            CapabilityIpcMessage message = new(
                CapabilityIpcMessageType.FlyoutShow,
                CapabilityIpcCodec.ToDto(new FlyoutRequest("Tessera", "vol")));

            byte[] frame = CapabilityIpcCodec.Serialize(message);
            Span<byte> payload = frame.AsSpan(4);
            CapabilityIpcMessage back = CapabilityIpcCodec.Deserialize(payload);
            _ = back.Type.Should().Be(CapabilityIpcMessageType.FlyoutShow);
            _ = back.Request!.Kind.Should().Be("vol");
        }

        [Fact]
        public void Session_snapshot_round_trips_through_message_frame()
        {
            _ = TesseraFlyoutDismissCoordinator.IpcMustEchoSessionSnapshot.Should().BeTrue();
            TesseraFlyoutSessionState session = new();
            _ = session.Begin(TesseraFlyoutSessionMode.Stacked, "vol", "Fluent");
            TesseraFlyoutSessionSnapshot snap = session.Snapshot(effectivelyShowing: false);
            CapabilityIpcMessage message = new(
                CapabilityIpcMessageType.FlyoutSessionSnapshot,
                ModuleId: "Tessera",
                SessionSnapshot: TesseraFlyoutIpcSnapshotDto.From("Tessera", snap));

            byte[] frame = CapabilityIpcCodec.Serialize(message);
            CapabilityIpcMessage back = CapabilityIpcCodec.Deserialize(frame.AsSpan(4));
            _ = back.Type.Should().Be(CapabilityIpcMessageType.FlyoutSessionSnapshot);
            _ = back.ModuleId.Should().Be("Tessera");
            _ = back.SessionSnapshot.Should().NotBeNull();
            _ = back.SessionSnapshot.EffectivelyShowing.Should().BeFalse();
            _ = back.SessionSnapshot.Generation.Should().Be(1);
            _ = back.SessionSnapshot.Mode.Should().Be(nameof(TesseraFlyoutSessionMode.Stacked));
            _ = back.SessionSnapshot.Kind.Should().Be("vol");
            _ = back.SessionSnapshot.StyleId.Should().Be("Fluent");
        }
    }

    public class CapabilityDaemonEventBusTests : IDisposable
    {
        private readonly string _root;

        public CapabilityDaemonEventBusTests()
        {
            _root = Path.Combine(Path.GetTempPath(), "MosaicBus_" + Guid.NewGuid().ToString("N"));
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
        public async Task Arm_publishes_capability_armed()
        {
            CapabilityEventBus bus = new();
            string? armedId = null;
            using IDisposable sub = bus.Subscribe(CapabilityEventKind.CapabilityArmed, e => armedId = e.SourceModuleId);

            CapabilityRegistry registry = new();
            registry.Register(new FakeDaemonCapabilityFactory("Tessera"));
            FakeDaemonUiBridge ui = new();
            using CapabilityDaemon daemon = new(registry, HostServicesFakes.Create(), ui, bus);
            _ = await daemon.ArmAsync("Tessera");

            _ = armedId.Should().Be("Tessera");
        }

        private sealed class FakeDaemonCapabilityFactory(string moduleId) : ICapabilityFactory
        {
            public string ModuleId => moduleId;
            public IModuleCapability Create(ModuleManifest manifest, ICapabilityContext context)
            {
                return new FakeDaemonCapability(moduleId);
            }
        }

        private sealed class FakeDaemonCapability(string moduleId) : IModuleCapability
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

        private sealed class FakeDaemonUiBridge : ICapabilityUiBridge
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
