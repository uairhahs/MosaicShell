using FluentAssertions;
using MosaicShell.Core.Services;
using MosaicShell.Core.Services.WebNowPlaying;

namespace MosaicShell.Core.Tests
{
    public class WebNowPlayingMergeTests
    {
        [Fact]
        public void Merge_overlays_wnp_cover_when_smtc_thumbnail_missing()
        {
            MediaSessionInfo smtc = new(
                "Song | YouTube Music", null, "music.youtube.com-x!App", true,
                ThumbnailPng: null, PositionSeconds: 10, DurationSeconds: 100);
            byte[] cover = WebNowPlayingHostTests.TinyPng;
            WnpPlayerSnapshot wnp = new()
            {
                Title = "Song",
                Artist = "Artist",
                Name = "YouTube Music",
                State = WnpState.Playing,
                CoverPng = cover,
            };

            MediaSessionInfo merged = CompositeMediaSessionService.Merge(smtc, wnp)!;
            _ = merged.ThumbnailPng.Should().BeSameAs(cover);
            _ = merged.Title.Should().Be("Song");
            _ = merged.Artist.Should().Be("Artist");
        }

        [Fact]
        public void Merge_keeps_smtc_cover_when_present()
        {
            byte[] smtcCover = [.. WebNowPlayingHostTests.TinyPng];
            smtcCover[^1] ^= 0x01;
            MediaSessionInfo smtc = new("T", "A", "Spotify.exe", true, smtcCover, 1, 2);
            byte[] wnpCover = [.. WebNowPlayingHostTests.TinyPng];
            wnpCover[^2] ^= 0x01;
            WnpPlayerSnapshot wnp = new() { CoverPng = wnpCover };

            MediaSessionInfo merged = CompositeMediaSessionService.Merge(smtc, wnp)!;
            _ = merged.ThumbnailPng.Should().BeSameAs(smtcCover);
        }

        [Fact]
        public void Merge_wnp_only_when_no_smtc()
        {
            byte[] cover = new byte[40];
            WnpPlayerSnapshot wnp = new()
            {
                Title = "Web",
                Artist = "A",
                Name = "YouTube Music",
                State = WnpState.Playing,
                CoverPng = cover,
                PositionSeconds = 3,
                DurationSeconds = 30,
            };

            MediaSessionInfo merged = CompositeMediaSessionService.Merge(null, wnp)!;
            _ = merged.Title.Should().Be("Web");
            _ = merged.ThumbnailPng.Should().BeSameAs(cover);
            _ = merged.IsPlaying.Should().BeTrue();
        }

        [Fact]
        public void Merge_prefers_smtc_title_when_wnp_is_stale_on_browser_session()
        {
            MediaSessionInfo smtc = new(
                "Brand New Track", "New Artist", "music.youtube.com-x!App", true,
                ThumbnailPng: null, PositionSeconds: 1, DurationSeconds: 200);
            WnpPlayerSnapshot wnp = new()
            {
                Title = "Old Track",
                Artist = "Old Artist",
                Name = "YouTube Music",
                State = WnpState.Playing,
                CoverPng = WebNowPlayingHostTests.TinyPng,
            };

            MediaSessionInfo merged = CompositeMediaSessionService.Merge(smtc, wnp)!;
            _ = merged.Title.Should().Be("Brand New Track");
            _ = merged.Artist.Should().Be("New Artist");
            _ = merged.ThumbnailPng.Should().BeSameAs(wnp.CoverPng);
        }

        [Fact]
        public void Merge_prefers_smtc_position_when_wnp_lags_after_skip()
        {
            MediaSessionInfo smtc = new(
                "Song", "Artist", "music.youtube.com-x!App", true,
                ThumbnailPng: null, PositionSeconds: 0.5, DurationSeconds: 180);
            WnpPlayerSnapshot wnp = new()
            {
                Title = "Song",
                Artist = "Artist",
                Name = "YouTube Music",
                State = WnpState.Playing,
                PositionSeconds = 42,
                DurationSeconds = 180,
            };

            MediaSessionInfo merged = CompositeMediaSessionService.Merge(smtc, wnp)!;
            _ = merged.PositionSeconds.Should().BeApproximately(0.5, 0.01);
        }

        [Fact]
        public void Merge_does_not_rewind_playing_smtc_to_lagging_wnp()
        {
            MediaSessionInfo smtc = new(
                "Song", "Artist", "music.youtube.com-x!App", true,
                ThumbnailPng: null, PositionSeconds: 40, DurationSeconds: 180);
            WnpPlayerSnapshot wnp = new()
            {
                Title = "Song",
                Artist = "Artist",
                Name = "YouTube Music",
                State = WnpState.Playing,
                PositionSeconds = 38,
                DurationSeconds = 180,
            };

            MediaSessionInfo merged = CompositeMediaSessionService.Merge(smtc, wnp)!;
            _ = merged.PositionSeconds.Should().BeApproximately(40, 0.01);
        }

        [Fact]
        public void Merge_still_overlays_wnp_title_when_smtc_and_wnp_agree()
        {
            MediaSessionInfo smtc = new(
                "Song | YouTube Music", null, "music.youtube.com-x!App", true,
                ThumbnailPng: null, PositionSeconds: 10, DurationSeconds: 100);
            byte[] cover = WebNowPlayingHostTests.TinyPng;
            WnpPlayerSnapshot wnp = new()
            {
                Title = "Song",
                Artist = "Artist",
                Name = "YouTube Music",
                State = WnpState.Playing,
                CoverPng = cover,
            };

            MediaSessionInfo merged = CompositeMediaSessionService.Merge(smtc, wnp)!;
            _ = merged.Title.Should().Be("Song");
            _ = merged.Artist.Should().Be("Artist");
        }
    }
}
