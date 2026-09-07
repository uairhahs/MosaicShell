using FluentAssertions;
using MosaicShell.Core.Capabilities.Platform;
using MosaicShell.Core.Services;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// A track skip reaches Core as a settling sequence, not one event. These pin the
    /// classification that decides whether Tessera presents a new flyout, replayed from a real
    /// capture (2026-08-29, YouTube Music).
    /// </summary>
    public class MediaSessionPlatformSettleTests
    {
        /// <summary>
        /// The exact sequence one skip produced: app placeholder, browser tab title, then settled
        /// metadata. Only the settled stage may be announced as a track boundary - the first two
        /// carry placeholder titles that would otherwise be bound into the visible card.
        /// </summary>
        [Fact]
        public void Skip_announces_one_boundary_at_the_settled_stage_only()
        {
            FakeMediaSessionService media = new()
            {
                Current = new MediaSessionInfo("no hesi!", "Chow Lee & Synthetic", "ytm", true)
            };
            using MediaSessionPlatform platform = new(media);
            List<MediaSessionSignal> signals = [];
            platform.Signal += signals.Add;
            platform.Acquire();

            media.Current = new MediaSessionInfo("YouTube Music", "", "ytm", true);
            media.Current = new MediaSessionInfo("You A Stepper | YouTube Music", null, "ytm", true);
            media.Current = new MediaSessionInfo(
                "You A Stepper", "Baby Osamaa & Vontee The Sin", "ytm", true);

            _ = signals.Should().HaveCount(3);
            _ = signals[0].IsTrackBoundary.Should().BeFalse("the app placeholder is not a track");
            _ = signals[1].IsTrackBoundary.Should().BeFalse("the browser tab title is not a track");
            _ = signals[2].IsTrackBoundary.Should().BeTrue("settled metadata is the real boundary");
            _ = signals[2].Current!.Title.Should().Be("You A Stepper");
        }

        /// <summary>
        /// Settling must delay a boundary, never cancel one. A track that genuinely never reports
        /// an artist still has to present once the settle window elapses, or such tracks would
        /// silently stop opening the flyout.
        /// </summary>
        [Fact]
        public async Task Artist_less_track_still_announces_a_boundary_after_the_settle_window()
        {
            FakeMediaSessionService media = new()
            {
                Current = new MediaSessionInfo("Previous", "Someone", "app", true)
            };
            using MediaSessionPlatform platform = new(media);
            List<MediaSessionSignal> signals = [];
            platform.Signal += signals.Add;
            platform.Acquire();

            media.Current = new MediaSessionInfo("Untitled Recording", "", "app", true);
            _ = signals.Should().ContainSingle().Which.IsTrackBoundary.Should().BeFalse();

            await Task.Delay(MediaSessionChangePolicy.MetadataSettleMs + 400);

            _ = signals.Should().HaveCount(2);
            _ = signals[1].IsTrackBoundary.Should().BeTrue();
            _ = signals[1].Current!.Title.Should().Be("Untitled Recording");
        }

        /// <summary>
        /// Metadata that arrives complete must not pay the settle delay - the window exists only
        /// to absorb placeholder stages.
        /// </summary>
        [Fact]
        public void Settled_metadata_announces_its_boundary_immediately()
        {
            FakeMediaSessionService media = new()
            {
                Current = new MediaSessionInfo("Previous", "Someone", "app", true)
            };
            using MediaSessionPlatform platform = new(media);
            List<MediaSessionSignal> signals = [];
            platform.Signal += signals.Add;
            platform.Acquire();

            media.Current = new MediaSessionInfo("Next Track", "Real Artist", "app", true);

            _ = signals.Should().ContainSingle().Which.IsTrackBoundary.Should().BeTrue();
        }

        /// <summary>
        /// Regression: <c>Dispose</c> used to drain consumers by calling <c>Release</c> in a loop,
        /// but <c>Release</c> early-returns once disposed, so <c>_consumers</c> never decremented
        /// and disposing an acquired platform span forever. Surfaced as a hung test run, not as a
        /// failure, which is why it survived - nothing else disposed while still acquired.
        /// </summary>
        [Fact]
        public void Dispose_while_acquired_completes_and_unsubscribes()
        {
            FakeMediaSessionService media = new()
            {
                Current = new MediaSessionInfo("Track", "Artist", "app", true)
            };
            List<MediaSessionSignal> signals = [];
            MediaSessionPlatform platform = new(media);
            platform.Signal += signals.Add;
            platform.Acquire();
            platform.Acquire();

            platform.Dispose();

            // Would never return before the fix; the assertion below only matters if it does.
            media.Current = new MediaSessionInfo("After", "Someone", "app", true);
            _ = signals.Should().BeEmpty("dispose must unsubscribe regardless of refcount");
        }

        /// <summary>
        /// A withheld boundary must still surface as a plain session change, so an already-visible
        /// flyout keeps soft-refreshing. Only the "present a new track" classification is held.
        /// </summary>
        [Fact]
        public void Settling_stage_is_still_raised_as_a_non_boundary_change()
        {
            FakeMediaSessionService media = new()
            {
                Current = new MediaSessionInfo("Previous", "Someone", "app", true)
            };
            using MediaSessionPlatform platform = new(media);
            List<MediaSessionSignal> signals = [];
            platform.Signal += signals.Add;
            platform.Acquire();

            media.Current = new MediaSessionInfo("YouTube Music", "", "app", true);

            _ = signals.Should().ContainSingle();
            _ = signals[0].Kind.Should().Be(MediaSessionSignalKind.SessionChanged);
            _ = signals[0].IsTrackBoundary.Should().BeFalse();
        }
    }
}
