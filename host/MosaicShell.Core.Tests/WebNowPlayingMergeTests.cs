using FluentAssertions;
using MosaicShell.Core.Services;
using MosaicShell.Core.Services.WebNowPlaying;

namespace MosaicShell.Core.Tests;

public class WebNowPlayingMergeTests
{
    [Fact]
    public void Merge_overlays_wnp_cover_when_smtc_thumbnail_missing()
    {
        var smtc = new MediaSessionInfo(
            "Song | YouTube Music", null, "music.youtube.com-x!App", true,
            ThumbnailPng: null, PositionSeconds: 10, DurationSeconds: 100);
        var cover = WebNowPlayingHostTests.TinyPng;
        var wnp = new WnpPlayerSnapshot
        {
            Title = "Song",
            Artist = "Artist",
            Name = "YouTube Music",
            State = WnpState.Playing,
            CoverPng = cover,
        };

        var merged = CompositeMediaSessionService.Merge(smtc, wnp)!;
        merged.ThumbnailPng.Should().BeSameAs(cover);
        merged.Title.Should().Be("Song");
        merged.Artist.Should().Be("Artist");
    }

    [Fact]
    public void Merge_keeps_smtc_cover_when_present()
    {
        var smtcCover = WebNowPlayingHostTests.TinyPng.ToArray();
        smtcCover[^1] ^= 0x01;
        var smtc = new MediaSessionInfo("T", "A", "Spotify.exe", true, smtcCover, 1, 2);
        var wnpCover = WebNowPlayingHostTests.TinyPng.ToArray();
        wnpCover[^2] ^= 0x01;
        var wnp = new WnpPlayerSnapshot { CoverPng = wnpCover };

        var merged = CompositeMediaSessionService.Merge(smtc, wnp)!;
        merged.ThumbnailPng.Should().BeSameAs(smtcCover);
    }

    [Fact]
    public void Merge_wnp_only_when_no_smtc()
    {
        var cover = new byte[40];
        var wnp = new WnpPlayerSnapshot
        {
            Title = "Web",
            Artist = "A",
            Name = "YouTube Music",
            State = WnpState.Playing,
            CoverPng = cover,
            PositionSeconds = 3,
            DurationSeconds = 30,
        };

        var merged = CompositeMediaSessionService.Merge(null, wnp)!;
        merged.Title.Should().Be("Web");
        merged.ThumbnailPng.Should().BeSameAs(cover);
        merged.IsPlaying.Should().BeTrue();
    }

    [Fact]
    public void Merge_prefers_smtc_title_when_wnp_is_stale_on_browser_session()
    {
        var smtc = new MediaSessionInfo(
            "Brand New Track", "New Artist", "music.youtube.com-x!App", true,
            ThumbnailPng: null, PositionSeconds: 1, DurationSeconds: 200);
        var wnp = new WnpPlayerSnapshot
        {
            Title = "Old Track",
            Artist = "Old Artist",
            Name = "YouTube Music",
            State = WnpState.Playing,
            CoverPng = WebNowPlayingHostTests.TinyPng,
        };

        var merged = CompositeMediaSessionService.Merge(smtc, wnp)!;
        merged.Title.Should().Be("Brand New Track");
        merged.Artist.Should().Be("New Artist");
        merged.ThumbnailPng.Should().BeSameAs(wnp.CoverPng);
    }

    [Fact]
    public void Merge_prefers_smtc_position_when_wnp_lags_after_skip()
    {
        var smtc = new MediaSessionInfo(
            "Song", "Artist", "music.youtube.com-x!App", true,
            ThumbnailPng: null, PositionSeconds: 0.5, DurationSeconds: 180);
        var wnp = new WnpPlayerSnapshot
        {
            Title = "Song",
            Artist = "Artist",
            Name = "YouTube Music",
            State = WnpState.Playing,
            PositionSeconds = 42,
            DurationSeconds = 180,
        };

        var merged = CompositeMediaSessionService.Merge(smtc, wnp)!;
        merged.PositionSeconds.Should().BeApproximately(0.5, 0.01);
    }

    [Fact]
    public void Merge_still_overlays_wnp_title_when_smtc_and_wnp_agree()
    {
        var smtc = new MediaSessionInfo(
            "Song | YouTube Music", null, "music.youtube.com-x!App", true,
            ThumbnailPng: null, PositionSeconds: 10, DurationSeconds: 100);
        var cover = WebNowPlayingHostTests.TinyPng;
        var wnp = new WnpPlayerSnapshot
        {
            Title = "Song",
            Artist = "Artist",
            Name = "YouTube Music",
            State = WnpState.Playing,
            CoverPng = cover,
        };

        var merged = CompositeMediaSessionService.Merge(smtc, wnp)!;
        merged.Title.Should().Be("Song");
        merged.Artist.Should().Be("Artist");
    }
}
