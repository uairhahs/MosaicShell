using FluentAssertions;
using MosaicShell.Core.Modules.Tessera;
using MosaicShell.Core.Services;

namespace MosaicShell.Core.Tests
{
    public class LockKeyPollPolicyTests
    {
        [Fact]
        public void Lock_keys_prefer_poll_over_low_level_hook()
        {
            _ = LockKeyPollPolicy.PreferPollOverLowLevelHook.Should().BeTrue();
        }

        [Fact]
        public void Poll_interval_is_positive()
        {
            _ = LockKeyPollPolicy.PollIntervalMs.Should().BePositive();
        }

        [Fact]
        public void Changed_must_not_be_deferred_to_thread_pool()
        {
            _ = LockKeyPollPolicy.MustInvokeChangedSynchronously.Should().BeTrue();
        }
    }

    public class TesseraMediaPresentPolicyTests
    {
        [Fact]
        public void Present_settle_applies_to_media_present_only()
        {
            _ = TesseraMediaPresentPolicy.ShouldSchedulePresentSettle(
                "media", TesseraFlyoutSyncAction.Present).Should().BeTrue();
            _ = TesseraMediaPresentPolicy.ShouldSchedulePresentSettle(
                "media", TesseraFlyoutSyncAction.Patch).Should().BeFalse();
            _ = TesseraMediaPresentPolicy.ShouldSchedulePresentSettle(
                "vol", TesseraFlyoutSyncAction.Present).Should().BeFalse();
        }

        [Fact]
        public void Pump_before_build_applies_to_cold_media_present()
        {
            _ = TesseraMediaPresentPolicy.ShouldPumpBeforeBuild(
                "media", TesseraFlyoutSyncAction.Present).Should().BeTrue();
            _ = TesseraMediaPresentPolicy.ShouldPumpBeforeBuild(
                "media", TesseraFlyoutSyncAction.Patch).Should().BeFalse();
        }
    }

    public class CompositeMediaTrackChangeTests
    {
        [Fact]
        public void Rebuild_raises_changed_on_position_restart_when_title_still_stale()
        {
            SteppingMediaSessionService smtc = new();
            StubBrowserMediaSource wnp = new();
            using CompositeMediaSessionService composite = new(smtc, wnp);

            int changed = 0;
            composite.Changed += (_, _) => changed++;

            smtc.Set(new MediaSessionInfo("Track A", "A", "app", true, null, 30, 180));
            _ = changed.Should().Be(1);

            smtc.Set(new MediaSessionInfo("Track A", "A", "app", true, null, 25, 180));
            _ = changed.Should().Be(1, "in-track progress without title change is not a flyout event");

            smtc.Set(new MediaSessionInfo("Track A", "A", "app", true, null, 0.5, 180));
            _ = changed.Should().Be(2, "position restart must raise Changed even when title is still stale");
        }

        [Fact]
        public void Rebuild_raises_changed_once_when_smtc_title_and_position_both_signal_skip()
        {
            SteppingMediaSessionService smtc = new();
            StubBrowserMediaSource wnp = new();
            using CompositeMediaSessionService composite = new(smtc, wnp);

            int changed = 0;
            composite.Changed += (_, _) => changed++;

            smtc.Set(new MediaSessionInfo("Track A", "A", "app", true, null, 40, 180));
            _ = changed.Should().Be(1);

            smtc.Set(new MediaSessionInfo("Track B", "A", "app", true, null, 0.5, 180));
            _ = changed.Should().Be(2, "one Changed per rebuild even when title and position both moved");
        }

        [Fact]
        public void Rebuild_raises_changed_when_smtc_skips_but_wnp_position_is_stale()
        {
            SteppingMediaSessionService smtc = new();
            StubBrowserMediaSource wnp = new()
            {
                Active = new BrowserPlayerSnapshot
                {
                    Title = "Track A",
                    Artist = "A",
                    Name = "YouTube Music",
                    State = BrowserPlaybackState.Playing,
                    PositionSeconds = 45,
                    DurationSeconds = 180,
                }
            };
            using CompositeMediaSessionService composite = new(smtc, wnp);

            int changed = 0;
            composite.Changed += (_, _) => changed++;

            smtc.Set(new MediaSessionInfo(
                "Track A", "A", "music.youtube.com-x!App", true, null, 40, 180));
            _ = changed.Should().Be(1);

            smtc.Set(new MediaSessionInfo(
                "Track A", "A", "music.youtube.com-x!App", true, null, 0.5, 180));
            _ = changed.Should().Be(2, "SMTC restart must raise Changed even when WNP timeline is stale");
        }

        [Fact]
        public void Browser_tab_title_reaches_consumers_without_the_site_suffix_when_wnp_is_silent()
        {
            SteppingMediaSessionService smtc = new();
            StubBrowserMediaSource wnp = new();
            using CompositeMediaSessionService composite = new(smtc, wnp);

            smtc.Set(new MediaSessionInfo(
                "JUST DANCE | YouTube Music", null, "music.youtube.com-x!App", true, null, 5, 108));

            _ = composite.Current!.Title.Should().Be("JUST DANCE");
        }

        [Fact]
        public void Suffixed_smtc_title_does_not_raise_changed_on_every_progress_tick()
        {
            SteppingMediaSessionService smtc = new();
            StubBrowserMediaSource wnp = new();
            using CompositeMediaSessionService composite = new(smtc, wnp);

            int changed = 0;
            composite.Changed += (_, _) => changed++;

            smtc.Set(new MediaSessionInfo(
                "JUST DANCE | YouTube Music", null, "music.youtube.com-x!App", true, null, 5, 108));
            _ = changed.Should().Be(1);

            smtc.Set(new MediaSessionInfo(
                "JUST DANCE | YouTube Music", null, "music.youtube.com-x!App", true, null, 8, 108));
            _ = changed.Should().Be(1, "the title did not change, only the suffix differs from the merged value");
        }

        private sealed class SteppingMediaSessionService : IMediaSessionService
        {
            public MediaSessionInfo? Current { get; private set; }
            public event EventHandler? Changed;
            public event EventHandler? ProgressChanged { add { } remove { } }

            public void Set(MediaSessionInfo info)
            {
                Current = info;
                Changed?.Invoke(this, EventArgs.Empty);
            }

            public void PumpTimeline() { }
            public Task PlayPauseAsync()
            {
                return Task.CompletedTask;
            }

            public Task NextAsync()
            {
                return Task.CompletedTask;
            }

            public Task PreviousAsync()
            {
                return Task.CompletedTask;
            }

            public Task SeekAsync(double positionSeconds)
            {
                return Task.CompletedTask;
            }

            public Task ToggleShuffleAsync()
            {
                return Task.CompletedTask;
            }

            public Task ToggleRepeatAsync()
            {
                return Task.CompletedTask;
            }

            public Task ToggleLikeAsync(bool wantLiked)
            {
                return Task.CompletedTask;
            }

            public Task ToggleDislikeAsync(bool wantDisliked)
            {
                return Task.CompletedTask;
            }

            public void Dispose() { }
        }

        private sealed class StubBrowserMediaSource : IBrowserMediaSource
        {
            public BrowserPlayerSnapshot? Active { get; init; }
#pragma warning disable CS0067
            public event EventHandler? Changed;
#pragma warning restore CS0067
            public Task SetLikedAsync(bool liked)
            {
                return Task.CompletedTask;
            }

            public Task SetDislikedAsync(bool disliked)
            {
                return Task.CompletedTask;
            }

            public Task ToggleShuffleAsync()
            {
                return Task.CompletedTask;
            }

            public Task ToggleRepeatAsync()
            {
                return Task.CompletedTask;
            }

            public void Dispose() { }
        }
    }
}
