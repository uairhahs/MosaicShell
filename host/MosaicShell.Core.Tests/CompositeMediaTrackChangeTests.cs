using FluentAssertions;
using MosaicShell.Core.Modules.Tessera;
using MosaicShell.Core.Services;
using MosaicShell.Core.Services.WebNowPlaying;

namespace MosaicShell.Core.Tests;

public class LockKeyPollPolicyTests
{
    [Fact]
    public void Lock_keys_prefer_poll_over_low_level_hook() =>
        LockKeyPollPolicy.PreferPollOverLowLevelHook.Should().BeTrue();

    [Fact]
    public void Poll_interval_is_positive() =>
        LockKeyPollPolicy.PollIntervalMs.Should().BePositive();

    [Fact]
    public void Changed_must_not_be_deferred_to_thread_pool() =>
        LockKeyPollPolicy.MustInvokeChangedSynchronously.Should().BeTrue();
}

public class TesseraMediaPresentPolicyTests
{
    [Fact]
    public void Present_settle_applies_to_media_present_only()
    {
        TesseraMediaPresentPolicy.ShouldSchedulePresentSettle(
            "media", TesseraFlyoutSyncAction.Present).Should().BeTrue();
        TesseraMediaPresentPolicy.ShouldSchedulePresentSettle(
            "media", TesseraFlyoutSyncAction.Patch).Should().BeFalse();
        TesseraMediaPresentPolicy.ShouldSchedulePresentSettle(
            "vol", TesseraFlyoutSyncAction.Present).Should().BeFalse();
    }

    [Fact]
    public void Pump_before_build_applies_to_cold_media_present()
    {
        TesseraMediaPresentPolicy.ShouldPumpBeforeBuild(
            "media", TesseraFlyoutSyncAction.Present).Should().BeTrue();
        TesseraMediaPresentPolicy.ShouldPumpBeforeBuild(
            "media", TesseraFlyoutSyncAction.Patch).Should().BeFalse();
    }
}

public class CompositeMediaTrackChangeTests
{
    [Fact]
    public void Rebuild_raises_changed_on_position_restart_when_title_still_stale()
    {
        var smtc = new SteppingMediaSessionService();
        var wnp = new StubWebNowPlayingService();
        using var composite = new CompositeMediaSessionService(smtc, wnp);

        var changed = 0;
        composite.Changed += (_, _) => changed++;

        smtc.Set(new MediaSessionInfo("Track A", "A", "app", true, null, 30, 180));
        changed.Should().Be(1);

        smtc.Set(new MediaSessionInfo("Track A", "A", "app", true, null, 25, 180));
        changed.Should().Be(1, "in-track progress without title change is not a flyout event");

        smtc.Set(new MediaSessionInfo("Track A", "A", "app", true, null, 0.5, 180));
        changed.Should().Be(2, "position restart must raise Changed even when title is still stale");
    }

    [Fact]
    public void Rebuild_raises_changed_once_when_smtc_title_and_position_both_signal_skip()
    {
        var smtc = new SteppingMediaSessionService();
        var wnp = new StubWebNowPlayingService();
        using var composite = new CompositeMediaSessionService(smtc, wnp);

        var changed = 0;
        composite.Changed += (_, _) => changed++;

        smtc.Set(new MediaSessionInfo("Track A", "A", "app", true, null, 40, 180));
        changed.Should().Be(1);

        smtc.Set(new MediaSessionInfo("Track B", "A", "app", true, null, 0.5, 180));
        changed.Should().Be(2, "one Changed per rebuild even when title and position both moved");
    }

    [Fact]
    public void Rebuild_raises_changed_when_smtc_skips_but_wnp_position_is_stale()
    {
        var smtc = new SteppingMediaSessionService();
        var wnp = new StubWebNowPlayingService
        {
            Active = new WnpPlayerSnapshot
            {
                Title = "Track A",
                Artist = "A",
                Name = "YouTube Music",
                State = WnpState.Playing,
                PositionSeconds = 45,
                DurationSeconds = 180,
            }
        };
        using var composite = new CompositeMediaSessionService(smtc, wnp);

        var changed = 0;
        composite.Changed += (_, _) => changed++;

        smtc.Set(new MediaSessionInfo(
            "Track A", "A", "music.youtube.com-x!App", true, null, 40, 180));
        changed.Should().Be(1);

        smtc.Set(new MediaSessionInfo(
            "Track A", "A", "music.youtube.com-x!App", true, null, 0.5, 180));
        changed.Should().Be(2, "SMTC restart must raise Changed even when WNP timeline is stale");
    }

    private sealed class SteppingMediaSessionService : IMediaSessionService
    {
        public MediaSessionInfo? Current { get; private set; }
        public event EventHandler? Changed;
        public event EventHandler? ProgressChanged;

        public void Set(MediaSessionInfo info)
        {
            Current = info;
            Changed?.Invoke(this, EventArgs.Empty);
        }

        public void PumpTimeline() { }
        public Task PlayPauseAsync() => Task.CompletedTask;
        public Task NextAsync() => Task.CompletedTask;
        public Task PreviousAsync() => Task.CompletedTask;
        public Task SeekAsync(double positionSeconds) => Task.CompletedTask;
        public Task ToggleShuffleAsync() => Task.CompletedTask;
        public Task ToggleRepeatAsync() => Task.CompletedTask;
        public Task ToggleLikeAsync(bool wantLiked) => Task.CompletedTask;
        public void Dispose() { }
    }

    private sealed class StubWebNowPlayingService : IWebNowPlayingService
    {
        public WnpPlayerSnapshot? Active { get; init; }
        public int ConnectedClients => 0;
        public int ListenPort => 0;
#pragma warning disable CS0067
        public event EventHandler? Changed;
#pragma warning restore CS0067
        public void Dispose() { }
    }
}
