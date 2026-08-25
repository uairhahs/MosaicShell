using FluentAssertions;
using MosaicShell.Core;
using MosaicShell.Core.Capabilities;
using MosaicShell.Core.Capabilities.Ipc;
using MosaicShell.Core.Capabilities.Platform;
using MosaicShell.Core.Modules;
using MosaicShell.Core.Modules.Tessera;
using MosaicShell.Core.Runtime;
using MosaicShell.Core.Services;

namespace MosaicShell.Core.Tests;

public class CapabilityStoreMosaicTests
{
    [Fact]
    public void SaveArmed_persists_multiple_modules()
    {
        var root = Path.Combine(Path.GetTempPath(), "MosaicCapStore_" + Guid.NewGuid().ToString("N"));
        AppPaths.SetRootOverride(root);
        AppPaths.EnsureLayout();
        try
        {
            CapabilityStore.SaveArmed(["Tessera", "Mixdeck", "Slate"]);
            var loaded = CapabilityStore.Load().Armed;
            loaded.Should().BeEquivalentTo(["Tessera", "Mixdeck", "Slate"]);
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
        HostLaunchOptions.Apply(Array.Empty<string>());
        HostLaunchOptions.IsTrayOnly.Should().BeFalse();
    }

    [Fact]
    public void Tray_only_flag_sets_mode()
    {
        HostLaunchOptions.ResetForTests();
        HostLaunchOptions.Apply(["--tray-only"]);
        HostLaunchOptions.IsTrayOnly.Should().BeTrue();
        HostLaunchOptions.TesseraOsAcrylicTrial.Should().BeFalse();
    }

    [Fact]
    public void Tessera_os_acrylic_flag_is_opt_in()
    {
        HostLaunchOptions.ResetForTests();
        HostLaunchOptions.Apply([HostLaunchOptions.TesseraOsAcrylicTrialFlag]);
        HostLaunchOptions.TesseraOsAcrylicTrial.Should().BeTrue();
        HostLaunchOptions.IsTrayOnly.Should().BeFalse();
    }

    [Fact]
    public void Tessera_force_software_render_flag_is_opt_in()
    {
        HostLaunchOptions.ResetForTests();
        HostLaunchOptions.Apply([HostLaunchOptions.TesseraForceSoftwareRenderFlag]);
        HostLaunchOptions.TesseraForceSoftwareRender.Should().BeTrue();
        HostLaunchOptions.TesseraOsAcrylicTrial.Should().BeFalse();
    }

    [Fact]
    public void Apply_resets_acrylic_trial_so_alpha_rollback_is_omit_the_flag()
    {
        HostLaunchOptions.ResetForTests();
        HostLaunchOptions.Apply([HostLaunchOptions.TesseraOsAcrylicTrialFlag]);
        HostLaunchOptions.Apply(["--tray-only"]);
        HostLaunchOptions.TesseraOsAcrylicTrial.Should().BeFalse();
        HostLaunchOptions.IsTrayOnly.Should().BeTrue();
    }
}

public class CapabilityEventBusTests
{
    [Fact]
    public void Subscribe_receives_published_kind()
    {
        var bus = new CapabilityEventBus();
        CapabilityEvent? received = null;
        using var sub = bus.Subscribe(CapabilityEventKind.VolumeChanged, e => received = e);
        bus.Publish(new CapabilityEvent(CapabilityEventKind.VolumeChanged));
        received.Should().NotBeNull();
        received!.Kind.Should().Be(CapabilityEventKind.VolumeChanged);
    }

    [Fact]
    public void SubscribeAll_receives_any_kind()
    {
        var bus = new CapabilityEventBus();
        var count = 0;
        using var sub = bus.SubscribeAll(_ => count++);
        bus.Publish(new CapabilityEvent(CapabilityEventKind.MediaProgress));
        bus.Publish(new CapabilityEvent(CapabilityEventKind.CapabilityArmed, "Tessera"));
        count.Should().Be(2);
    }
}

public class CapabilityIpcCodecTests
{
    [Fact]
    public void Flyout_request_round_trips_through_dto()
    {
        var request = new FlyoutRequest(
            "Tessera",
            "media",
            StyleId: "Fluent",
            Payload: new Dictionary<string, string> { ["title"] = "Track" });

        var back = CapabilityIpcCodec.FromDto(CapabilityIpcCodec.ToDto(request));
        back.ModuleId.Should().Be("Tessera");
        back.Kind.Should().Be("media");
        back.StyleId.Should().Be("Fluent");
        back.Payload!["title"].Should().Be("Track");
    }

    [Fact]
    public void Message_frame_serializes_and_deserializes()
    {
        var message = new CapabilityIpcMessage(
            CapabilityIpcMessageType.FlyoutShow,
            CapabilityIpcCodec.ToDto(new FlyoutRequest("Tessera", "vol")));

        var frame = CapabilityIpcCodec.Serialize(message);
        var payload = frame.AsSpan(4);
        var back = CapabilityIpcCodec.Deserialize(payload);
        back.Type.Should().Be(CapabilityIpcMessageType.FlyoutShow);
        back.Request!.Kind.Should().Be("vol");
    }

    [Fact]
    public void Session_snapshot_round_trips_through_message_frame()
    {
        TesseraFlyoutDismissCoordinator.IpcMustEchoSessionSnapshot.Should().BeTrue();
        var session = new TesseraFlyoutSessionState();
        session.Begin(TesseraFlyoutSessionMode.Stacked, "vol", "Fluent");
        var snap = session.Snapshot(effectivelyShowing: false);
        var message = new CapabilityIpcMessage(
            CapabilityIpcMessageType.FlyoutSessionSnapshot,
            ModuleId: "Tessera",
            SessionSnapshot: TesseraFlyoutIpcSnapshotDto.From("Tessera", snap));

        var frame = CapabilityIpcCodec.Serialize(message);
        var back = CapabilityIpcCodec.Deserialize(frame.AsSpan(4));
        back.Type.Should().Be(CapabilityIpcMessageType.FlyoutSessionSnapshot);
        back.ModuleId.Should().Be("Tessera");
        back.SessionSnapshot.Should().NotBeNull();
        back.SessionSnapshot!.EffectivelyShowing.Should().BeFalse();
        back.SessionSnapshot.Generation.Should().Be(1);
        back.SessionSnapshot.Mode.Should().Be(nameof(TesseraFlyoutSessionMode.Stacked));
        back.SessionSnapshot.Kind.Should().Be("vol");
        back.SessionSnapshot.StyleId.Should().Be("Fluent");
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
        Directory.CreateDirectory(Path.Combine(AppPaths.ModulesDirectory, "Tessera"));
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
        var bus = new CapabilityEventBus();
        string? armedId = null;
        using var sub = bus.Subscribe(CapabilityEventKind.CapabilityArmed, e => armedId = e.SourceModuleId);

        var registry = new CapabilityRegistry();
        registry.Register(new FakeDaemonCapabilityFactory("Tessera"));
        var ui = new FakeDaemonUiBridge();
        using var daemon = new CapabilityDaemon(registry, HostServicesFakes.Create(), ui, bus);
        await daemon.ArmAsync("Tessera");

        armedId.Should().Be("Tessera");
    }

    private sealed class FakeDaemonCapabilityFactory(string moduleId) : ICapabilityFactory
    {
        public string ModuleId => moduleId;
        public IModuleCapability Create(ModuleManifest manifest, ICapabilityContext context) =>
            new FakeDaemonCapability(moduleId);
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
        public void RunOnHostThread(Action action) => action();
    }
}
