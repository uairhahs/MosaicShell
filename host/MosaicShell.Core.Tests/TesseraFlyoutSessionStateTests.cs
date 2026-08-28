using FluentAssertions;
using MosaicShell.Core.Capabilities;
using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Core.Tests
{
    public class TesseraFlyoutSessionStateTests
    {
        [Fact]
        public void Ingress_and_session_contracts_are_armed()
        {
            _ = TesseraFlyoutIngressPolicy.HostMustUseSingleIngressQueue.Should().BeTrue();
            _ = TesseraFlyoutIngressPolicy.SoftRefreshMustHonorSessionGeneration.Should().BeTrue();
            _ = TesseraFlyoutIngressPolicy.HostMustExecutePresenterCommandWithoutReResolve.Should().BeTrue();
            _ = TesseraFlyoutPresentHandoffPolicy.SoftRefreshMustHonorSessionGeneration.Should().BeTrue();
        }

        [Fact]
        public void Queue_keeps_present_when_soft_refresh_arrives()
        {
            TesseraFlyoutIngressQueue queue = new();
            FlyoutRequest present = new("Tessera", "vol", Payload: new Dictionary<string, string> { ["n"] = "1" });
            FlyoutRequest refresh = new("Tessera", "vol", Payload: new Dictionary<string, string> { ["n"] = "2" });
            queue.Enqueue(new TesseraFlyoutIngressWork(TesseraFlyoutIngressKind.Present, 1, present, true));
            queue.Enqueue(new TesseraFlyoutIngressWork(TesseraFlyoutIngressKind.SoftRefresh, 1, refresh, false));
            TesseraFlyoutIngressWork? taken = queue.Take(1);
            _ = taken.Should().NotBeNull();
            _ = taken!.Value.Kind.Should().Be(TesseraFlyoutIngressKind.Present);
            _ = taken.Value.Request.Payload!["n"].Should().Be("1");
        }

        [Fact]
        public void Queue_folds_patch_payload_into_pending_present()
        {
            TesseraFlyoutIngressQueue queue = new();
            FlyoutRequest present = new("Tessera", "vol", Payload: new Dictionary<string, string> { ["n"] = "1" });
            FlyoutRequest patch = new("Tessera", "vol", Payload: new Dictionary<string, string> { ["n"] = "2" });
            queue.Enqueue(new TesseraFlyoutIngressWork(TesseraFlyoutIngressKind.Present, 1, present, true));
            queue.Enqueue(new TesseraFlyoutIngressWork(TesseraFlyoutIngressKind.Patch, 1, patch, true));
            TesseraFlyoutIngressWork? taken = queue.Take(1);
            _ = taken.Should().NotBeNull();
            _ = taken!.Value.Kind.Should().Be(TesseraFlyoutIngressKind.Present);
            _ = taken.Value.Request.Payload!["n"].Should().Be("2");
        }

        [Fact]
        public void Begin_bumps_generation_and_records_mode()
        {
            TesseraFlyoutSessionState session = new();
            _ = session.HasOpenSession.Should().BeFalse();
            int gen = session.Begin(TesseraFlyoutSessionMode.Stacked, "vol", "Fluent");
            _ = gen.Should().Be(1);
            _ = session.Mode.Should().Be(TesseraFlyoutSessionMode.Stacked);
            _ = session.Kind.Should().Be("vol");
            _ = session.StyleId.Should().Be("Fluent");
            _ = session.Begin(TesseraFlyoutSessionMode.Single, "locks", "Meter").Should().Be(2);
            _ = session.Mode.Should().Be(TesseraFlyoutSessionMode.Single);
        }

        [Fact]
        public void Clear_bumps_generation_so_stale_soft_refresh_drops()
        {
            TesseraFlyoutSessionState session = new();
            _ = session.Begin(TesseraFlyoutSessionMode.Single, "vol", "Meter");
            int stamped = session.Generation;
            _ = session.Clear();
            _ = TesseraFlyoutIngressPolicy.IsStale(
                    stamped, session.Generation, TesseraFlyoutIngressKind.SoftRefresh)
                .Should().BeTrue();
            _ = TesseraFlyoutIngressPolicy.IsStale(
                    stamped, session.Generation, TesseraFlyoutIngressKind.Present)
                .Should().BeFalse();
        }

        [Fact]
        public void Queue_drops_stale_soft_refresh_after_cancel()
        {
            TesseraFlyoutSessionState session = new();
            _ = session.Begin(TesseraFlyoutSessionMode.Single, "vol", "Meter");
            TesseraFlyoutIngressQueue queue = new();
            FlyoutRequest request = new("Tessera", "vol", StyleId: "Meter");
            queue.Enqueue(new TesseraFlyoutIngressWork(
                TesseraFlyoutIngressKind.SoftRefresh, session.Generation, request, ResetDismiss: false));
            _ = session.Clear();
            _ = queue.Take(session.Generation).Should().BeNull();
        }

        [Fact]
        public void Queue_last_value_wins_then_take_clears()
        {
            TesseraFlyoutIngressQueue queue = new();
            FlyoutRequest first = new("Tessera", "vol");
            FlyoutRequest last = new("Tessera", "vol", Payload: new Dictionary<string, string> { ["n"] = "2" });
            queue.Enqueue(new TesseraFlyoutIngressWork(TesseraFlyoutIngressKind.Patch, 1, first, true));
            queue.Enqueue(new TesseraFlyoutIngressWork(TesseraFlyoutIngressKind.Patch, 1, last, true));
            TesseraFlyoutIngressWork? taken = queue.Take(1);
            _ = taken.Should().NotBeNull();
            _ = taken!.Value.Request.Payload!["n"].Should().Be("2");
            _ = queue.Take(1).Should().BeNull();
        }

        [Fact]
        public void Snapshot_carries_effective_showing_for_ipc()
        {
            TesseraFlyoutSessionState session = new();
            _ = session.Begin(TesseraFlyoutSessionMode.Stacked, "vol", "Fluent");
            TesseraFlyoutSessionSnapshot snap = session.Snapshot(effectivelyShowing: false);
            _ = snap.EffectivelyShowing.Should().BeFalse();
            _ = snap.Generation.Should().Be(1);
            _ = snap.Mode.Should().Be(TesseraFlyoutSessionMode.Stacked);
            _ = snap.Kind.Should().Be("vol");
            _ = TesseraFlyoutLiveSyncPolicy.IsEffectivelyShowing(windowVisible: true, opacity: 0)
                .Should().BeFalse();
        }
    }
}
