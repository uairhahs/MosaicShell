using FluentAssertions;
using MosaicShell.Core.Capabilities;
using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Core.Tests;

public class TesseraFlyoutSessionStateTests
{
    [Fact]
    public void Ingress_and_session_contracts_are_armed()
    {
        TesseraFlyoutIngressPolicy.HostMustUseSingleIngressQueue.Should().BeTrue();
        TesseraFlyoutIngressPolicy.SoftRefreshMustHonorSessionGeneration.Should().BeTrue();
        TesseraFlyoutIngressPolicy.HostMustExecutePresenterCommandWithoutReResolve.Should().BeTrue();
        TesseraFlyoutPresentHandoffPolicy.SoftRefreshMustHonorSessionGeneration.Should().BeTrue();
    }

    [Fact]
    public void Queue_keeps_present_when_soft_refresh_arrives()
    {
        var queue = new TesseraFlyoutIngressQueue();
        var present = new FlyoutRequest("Tessera", "vol", Payload: new Dictionary<string, string> { ["n"] = "1" });
        var refresh = new FlyoutRequest("Tessera", "vol", Payload: new Dictionary<string, string> { ["n"] = "2" });
        queue.Enqueue(new TesseraFlyoutIngressWork(TesseraFlyoutIngressKind.Present, 1, present, true));
        queue.Enqueue(new TesseraFlyoutIngressWork(TesseraFlyoutIngressKind.SoftRefresh, 1, refresh, false));
        var taken = queue.Take(1);
        taken.Should().NotBeNull();
        taken!.Value.Kind.Should().Be(TesseraFlyoutIngressKind.Present);
        taken.Value.Request.Payload!["n"].Should().Be("1");
    }

    [Fact]
    public void Queue_folds_patch_payload_into_pending_present()
    {
        var queue = new TesseraFlyoutIngressQueue();
        var present = new FlyoutRequest("Tessera", "vol", Payload: new Dictionary<string, string> { ["n"] = "1" });
        var patch = new FlyoutRequest("Tessera", "vol", Payload: new Dictionary<string, string> { ["n"] = "2" });
        queue.Enqueue(new TesseraFlyoutIngressWork(TesseraFlyoutIngressKind.Present, 1, present, true));
        queue.Enqueue(new TesseraFlyoutIngressWork(TesseraFlyoutIngressKind.Patch, 1, patch, true));
        var taken = queue.Take(1);
        taken.Should().NotBeNull();
        taken!.Value.Kind.Should().Be(TesseraFlyoutIngressKind.Present);
        taken.Value.Request.Payload!["n"].Should().Be("2");
    }

    [Fact]
    public void Begin_bumps_generation_and_records_mode()
    {
        var session = new TesseraFlyoutSessionState();
        session.HasOpenSession.Should().BeFalse();
        var gen = session.Begin(TesseraFlyoutSessionMode.Stacked, "vol", "Fluent");
        gen.Should().Be(1);
        session.Mode.Should().Be(TesseraFlyoutSessionMode.Stacked);
        session.Kind.Should().Be("vol");
        session.StyleId.Should().Be("Fluent");
        session.Begin(TesseraFlyoutSessionMode.Single, "locks", "Meter").Should().Be(2);
        session.Mode.Should().Be(TesseraFlyoutSessionMode.Single);
    }

    [Fact]
    public void Clear_bumps_generation_so_stale_soft_refresh_drops()
    {
        var session = new TesseraFlyoutSessionState();
        session.Begin(TesseraFlyoutSessionMode.Single, "vol", "Meter");
        var stamped = session.Generation;
        session.Clear();
        TesseraFlyoutIngressPolicy.IsStale(
                stamped, session.Generation, TesseraFlyoutIngressKind.SoftRefresh)
            .Should().BeTrue();
        TesseraFlyoutIngressPolicy.IsStale(
                stamped, session.Generation, TesseraFlyoutIngressKind.Present)
            .Should().BeFalse();
    }

    [Fact]
    public void Queue_drops_stale_soft_refresh_after_cancel()
    {
        var session = new TesseraFlyoutSessionState();
        session.Begin(TesseraFlyoutSessionMode.Single, "vol", "Meter");
        var queue = new TesseraFlyoutIngressQueue();
        var request = new FlyoutRequest("Tessera", "vol", StyleId: "Meter");
        queue.Enqueue(new TesseraFlyoutIngressWork(
            TesseraFlyoutIngressKind.SoftRefresh, session.Generation, request, ResetDismiss: false));
        session.Clear();
        queue.Take(session.Generation).Should().BeNull();
    }

    [Fact]
    public void Queue_last_value_wins_then_take_clears()
    {
        var queue = new TesseraFlyoutIngressQueue();
        var first = new FlyoutRequest("Tessera", "vol");
        var last = new FlyoutRequest("Tessera", "vol", Payload: new Dictionary<string, string> { ["n"] = "2" });
        queue.Enqueue(new TesseraFlyoutIngressWork(TesseraFlyoutIngressKind.Patch, 1, first, true));
        queue.Enqueue(new TesseraFlyoutIngressWork(TesseraFlyoutIngressKind.Patch, 1, last, true));
        var taken = queue.Take(1);
        taken.Should().NotBeNull();
        taken!.Value.Request.Payload!["n"].Should().Be("2");
        queue.Take(1).Should().BeNull();
    }

    [Fact]
    public void Snapshot_carries_effective_showing_for_ipc()
    {
        var session = new TesseraFlyoutSessionState();
        session.Begin(TesseraFlyoutSessionMode.Stacked, "vol", "Fluent");
        var snap = session.Snapshot(effectivelyShowing: false);
        snap.EffectivelyShowing.Should().BeFalse();
        snap.Generation.Should().Be(1);
        snap.Mode.Should().Be(TesseraFlyoutSessionMode.Stacked);
        snap.Kind.Should().Be("vol");
        TesseraFlyoutLiveSyncPolicy.IsEffectivelyShowing(windowVisible: true, opacity: 0)
            .Should().BeFalse();
    }
}
