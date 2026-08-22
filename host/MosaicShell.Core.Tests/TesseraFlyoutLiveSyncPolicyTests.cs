using FluentAssertions;
using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Core.Tests;

public class TesseraFlyoutLiveSyncPolicyTests
{
    [Fact]
    public void Patch_must_not_imply_present_restack_or_outside_click_rearm()
    {
        TesseraFlyoutLiveSyncPolicy.PatchImpliesPresent.Should().BeFalse();
        TesseraFlyoutLiveSyncPolicy.PatchImpliesWin32Restack.Should().BeFalse();
        TesseraFlyoutLiveSyncPolicy.PatchImpliesOutsideClickRearm.Should().BeFalse();
    }

    [Fact]
    public void Pump_may_advance_media_timeline_but_must_not_write_volume()
    {
        TesseraFlyoutLiveSyncPolicy.PumpMayAdvanceMediaTimeline.Should().BeTrue();
        TesseraFlyoutLiveSyncPolicy.PumpMayWriteVolumeBindings.Should().BeFalse();
    }

    [Fact]
    public void Max_refresh_hz_bounds_ui_flushes()
    {
        TesseraFlyoutLiveSyncPolicy.MaxRefreshHz.Should().Be(30);
        TesseraFlyoutLiveSyncPolicy.MinFlushInterval.Should().Be(TimeSpan.FromMilliseconds(1000.0 / 30));
    }

    [Fact]
    public void Tessera_content_requires_live_host_for_successful_session()
    {
        TesseraFlyoutLiveSyncPolicy.LiveHostRequiredForTesseraSuccess.Should().BeTrue();
        TesseraFlyoutLiveSyncPolicy.IsSuccessfulTesseraContent(hasLiveHost: true, isFallbackContent: false)
            .Should().BeTrue();
        TesseraFlyoutLiveSyncPolicy.IsSuccessfulTesseraContent(hasLiveHost: false, isFallbackContent: false)
            .Should().BeFalse();
        TesseraFlyoutLiveSyncPolicy.IsSuccessfulTesseraContent(hasLiveHost: true, isFallbackContent: true)
            .Should().BeFalse();
    }

    [Theory]
    [InlineData(false, "vol", "vol", "Amber", "Amber", TesseraFlyoutSyncAction.Present)]
    [InlineData(true, "vol", "vol", "Amber", "Amber", TesseraFlyoutSyncAction.Patch)]
    [InlineData(true, "vol", "bright", "Amber", "Amber", TesseraFlyoutSyncAction.Present)]
    [InlineData(true, "vol", "vol", "Amber", "Fluent", TesseraFlyoutSyncAction.Present)]
    [InlineData(true, "vol", "media", "Amber", "Amber", TesseraFlyoutSyncAction.Present)]
    public void Visible_same_kind_and_style_patches_otherwise_presents(
        bool visible,
        string openKind,
        string nextKind,
        string openStyle,
        string nextStyle,
        TesseraFlyoutSyncAction expected)
    {
        TesseraFlyoutLiveSyncPolicy
            .ResolveAction(visible, openKind, nextKind, openStyle, nextStyle)
            .Should().Be(expected);
    }

    [Fact]
    public void Coalesce_flushes_immediately_after_idle_then_at_most_max_hz()
    {
        var gate = new TesseraFlyoutLiveSyncCoalescer(TesseraFlyoutLiveSyncPolicy.MinFlushInterval);
        var t0 = TimeSpan.FromSeconds(10);

        var half = TimeSpan.FromTicks(TesseraFlyoutLiveSyncPolicy.MinFlushInterval.Ticks / 2);
        gate.TryBeginFlush(t0).Should().BeTrue("first event after idle flushes immediately");
        gate.TryBeginFlush(t0 + half).Should().BeFalse("within min interval");
        gate.TryBeginFlush(t0 + TesseraFlyoutLiveSyncPolicy.MinFlushInterval - TimeSpan.FromTicks(1))
            .Should().BeFalse();
        gate.TryBeginFlush(t0 + TesseraFlyoutLiveSyncPolicy.MinFlushInterval)
            .Should().BeTrue("next flush allowed at MaxRefreshHz");
    }

    [Fact]
    public void Coalesce_drops_intermediate_events_last_value_wins()
    {
        var gate = new TesseraFlyoutLiveSyncCoalescer(TesseraFlyoutLiveSyncPolicy.MinFlushInterval);
        var t0 = TimeSpan.FromSeconds(1);
        var flushes = 0;
        for (var i = 0; i < 50; i++)
        {
            if (gate.TryBeginFlush(t0 + TimeSpan.FromMilliseconds(i * 5)))
                flushes++;
        }

        // 50 events over 245ms at 30Hz → at most ~8 flushes; must be far below event count.
        flushes.Should().BeLessThan(10);
        flushes.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Coalesce_reports_retry_after_when_throttled()
    {
        var gate = new TesseraFlyoutLiveSyncCoalescer(TimeSpan.FromMilliseconds(100));
        var t0 = TimeSpan.FromSeconds(5);
        gate.TryBeginFlush(t0, out _).Should().BeTrue();
        gate.TryBeginFlush(t0 + TimeSpan.FromMilliseconds(40), out var retry).Should().BeFalse();
        retry.Should().Be(TimeSpan.FromMilliseconds(60));
    }
}
