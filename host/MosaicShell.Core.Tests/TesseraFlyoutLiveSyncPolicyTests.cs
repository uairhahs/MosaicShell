using FluentAssertions;
using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Core.Tests
{
    public class TesseraFlyoutLiveSyncPolicyTests
    {
        [Fact]
        public void Closed_must_only_unregister_the_same_window_instance()
        {
            // Superseded HWND Closed must not clear the live session entry; that orphans
            // SoftFrost surfaces and stacks black/frost layers on rapid Try now.
            _ = TesseraFlyoutLiveSyncPolicy.ClosedMustOnlyUnregisterSameInstance.Should().BeTrue();
        }

        [Fact]
        public void Registered_flyout_hwnd_must_be_reused_never_close_and_recreate()
        {
            // Close()+immediate Show() overlaps SoftFrost composition surfaces (random stack/black).
            // Host must Hide/revive the registered HWND instead of Close+new.
            _ = TesseraFlyoutLiveSyncPolicy.MustReuseRegisteredFlyoutHwnd.Should().BeTrue();
            _ = TesseraFlyoutLiveSyncPolicy.TransientDismissMustHideNotClose.Should().BeTrue();
        }

        [Fact]
        public void Soft_frost_opacity_zero_is_not_effectively_showing()
        {
            _ = TesseraFlyoutLiveSyncPolicy.IsEffectivelyShowing(windowVisible: true, opacity: 0).Should().BeFalse();
            _ = TesseraFlyoutLiveSyncPolicy.IsEffectivelyShowing(windowVisible: true, opacity: 1).Should().BeTrue();
            _ = TesseraFlyoutLiveSyncPolicy.IsEffectivelyShowing(windowVisible: false, opacity: 1).Should().BeFalse();
        }

        [Fact]
        public void Soft_frost_reveal_must_be_generation_gated()
        {
            // HideUntilCompositionReady posts Opacity=1; rapid rebuild must invalidate stale reveals
            // or SoftFrost paints stacked/black frames mid-swap.
            _ = TesseraFlyoutWindowPolicy.HideUntilCompositionReady.Should().BeTrue();
            _ = TesseraFlyoutWindowPolicy.RevealMustBeGenerationGated.Should().BeTrue();
        }

        [Fact]
        public void Patch_must_not_imply_present_restack_or_outside_click_rearm()
        {
            _ = TesseraFlyoutLiveSyncPolicy.PatchImpliesPresent.Should().BeFalse();
            _ = TesseraFlyoutLiveSyncPolicy.PatchImpliesWin32Restack.Should().BeFalse();
            _ = TesseraFlyoutLiveSyncPolicy.PatchImpliesOutsideClickRearm.Should().BeFalse();
        }

        [Fact]
        public void Pump_may_advance_media_timeline_but_must_not_write_volume()
        {
            _ = TesseraFlyoutLiveSyncPolicy.PumpMayAdvanceMediaTimeline.Should().BeTrue();
            _ = TesseraFlyoutLiveSyncPolicy.PumpMayWriteVolumeBindings.Should().BeFalse();
        }

        [Fact]
        public void Max_refresh_hz_bounds_ui_flushes()
        {
            _ = TesseraFlyoutLiveSyncPolicy.MaxRefreshHz.Should().Be(30);
            _ = TesseraFlyoutLiveSyncPolicy.MinFlushInterval.Should().Be(TimeSpan.FromMilliseconds(1000.0 / 30));
        }

        [Fact]
        public void Tessera_content_requires_live_host_for_successful_session()
        {
            _ = TesseraFlyoutLiveSyncPolicy.LiveHostRequiredForTesseraSuccess.Should().BeTrue();
            _ = TesseraFlyoutLiveSyncPolicy.IsSuccessfulTesseraContent(hasLiveHost: true, isFallbackContent: false)
                .Should().BeTrue();
            _ = TesseraFlyoutLiveSyncPolicy.IsSuccessfulTesseraContent(hasLiveHost: false, isFallbackContent: false)
                .Should().BeFalse();
            _ = TesseraFlyoutLiveSyncPolicy.IsSuccessfulTesseraContent(hasLiveHost: true, isFallbackContent: true)
                .Should().BeFalse();
        }

        [Theory]
        [InlineData(false, "vol", "vol", "Meter", "Meter", TesseraFlyoutSyncAction.Present)]
        [InlineData(true, "vol", "vol", "Meter", "Meter", TesseraFlyoutSyncAction.Patch)]
        [InlineData(true, "vol", "bright", "Meter", "Meter", TesseraFlyoutSyncAction.Present)]
        [InlineData(true, "vol", "vol", "Meter", "Fluent", TesseraFlyoutSyncAction.Present)]
        [InlineData(true, "vol", "media", "Meter", "Meter", TesseraFlyoutSyncAction.Present)]
        public void Visible_same_kind_and_style_patches_otherwise_presents(
            bool visible,
            string openKind,
            string nextKind,
            string openStyle,
            string nextStyle,
            TesseraFlyoutSyncAction expected)
        {
            _ = TesseraFlyoutLiveSyncPolicy
                .ResolveAction(visible, openKind, nextKind, openStyle, nextStyle)
                .Should().Be(expected);
        }

        [Fact]
        public void Coalesce_flushes_immediately_after_idle_then_at_most_max_hz()
        {
            TesseraFlyoutLiveSyncCoalescer gate = new(TesseraFlyoutLiveSyncPolicy.MinFlushInterval);
            TimeSpan t0 = TimeSpan.FromSeconds(10);

            TimeSpan half = TimeSpan.FromTicks(TesseraFlyoutLiveSyncPolicy.MinFlushInterval.Ticks / 2);
            _ = gate.TryBeginFlush(t0).Should().BeTrue("first event after idle flushes immediately");
            _ = gate.TryBeginFlush(t0 + half).Should().BeFalse("within min interval");
            _ = gate.TryBeginFlush(t0 + TesseraFlyoutLiveSyncPolicy.MinFlushInterval - TimeSpan.FromTicks(1))
                .Should().BeFalse();
            _ = gate.TryBeginFlush(t0 + TesseraFlyoutLiveSyncPolicy.MinFlushInterval)
                .Should().BeTrue("next flush allowed at MaxRefreshHz");
        }

        [Fact]
        public void Coalesce_drops_intermediate_events_last_value_wins()
        {
            TesseraFlyoutLiveSyncCoalescer gate = new(TesseraFlyoutLiveSyncPolicy.MinFlushInterval);
            TimeSpan t0 = TimeSpan.FromSeconds(1);
            int flushes = 0;
            for (int i = 0; i < 50; i++)
            {
                if (gate.TryBeginFlush(t0 + TimeSpan.FromMilliseconds(i * 5)))
                {
                    flushes++;
                }
            }

            // 50 events over 245ms at 30Hz → at most ~8 flushes; must be far below event count.
            _ = flushes.Should().BeLessThan(10);
            _ = flushes.Should().BeGreaterThan(0);
        }

        [Fact]
        public void Coalesce_reports_retry_after_when_throttled()
        {
            TesseraFlyoutLiveSyncCoalescer gate = new(TimeSpan.FromMilliseconds(100));
            TimeSpan t0 = TimeSpan.FromSeconds(5);
            _ = gate.TryBeginFlush(t0, out _).Should().BeTrue();
            _ = gate.TryBeginFlush(t0 + TimeSpan.FromMilliseconds(40), out TimeSpan retry).Should().BeFalse();
            _ = retry.Should().Be(TimeSpan.FromMilliseconds(60));
        }

        [Fact]
        public void Update_dispatch_gate_must_coalesce_before_ui_post()
        {
            _ = TesseraFlyoutLiveSyncPolicy.MustCoalesceBeforeUiPost.Should().BeTrue();
            _ = TesseraFlyoutLiveSyncPolicy.NonStatusShowMustCoalesceThroughUpdateGate.Should().BeTrue();
        }

        [Fact]
        public void Update_dispatch_gate_merges_same_frame_burst_into_one_post()
        {
            TesseraFlyoutUpdateDispatchGate gate = new();
            int posts = 0;
            for (int i = 0; i < 50; i++)
            {
                if (gate.TryEnqueue() == TesseraFlyoutUpdateDispatchKind.PostNow)
                {
                    posts++;
                }
            }

            _ = posts.Should().Be(1);
            _ = gate.HasScheduledDispatch.Should().BeTrue();
            gate.CompleteDispatch();
            _ = gate.TryEnqueue().Should().Be(TesseraFlyoutUpdateDispatchKind.PostNow);
        }

        [Theory]
        [InlineData(false, true, true)]
        [InlineData(true, true, false)]
        [InlineData(true, false, false)]
        [InlineData(false, false, true)]
        public void ApplyRequest_replays_show_only_when_session_was_not_showing(
            bool reuseWasVisible,
            bool hideUntilReady,
            bool playShow)
        {
            _ = TesseraFlyoutLiveSyncPolicy
                .ShouldPlayShowAnimationAfterApplyRequest(reuseWasVisible, hideUntilReady)
                .Should().Be(playShow);
        }

        [Theory]
        [InlineData(false, true, true)]
        [InlineData(true, true, false)]
        [InlineData(true, false, false)]
        [InlineData(false, false, false)]
        public void ApplyRequest_zeros_motion_surface_only_on_hidden_revive(
            bool reuseWasVisible,
            bool hideUntilReady,
            bool zero)
        {
            _ = TesseraFlyoutLiveSyncPolicy
                .ShouldZeroMotionSurfaceOnApplyRequest(reuseWasVisible, hideUntilReady)
                .Should().Be(zero);
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(false, false)]
        public void Visible_rebuild_snaps_reveal_to_rest(bool reuseWasVisible, bool snapRest)
        {
            _ = TesseraFlyoutLiveSyncPolicy
                .ShouldSnapRevealToRestAfterApplyRequest(reuseWasVisible)
                .Should().Be(snapRest);
        }
    }
}
