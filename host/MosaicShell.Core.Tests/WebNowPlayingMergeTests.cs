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
        var cover = Enumerable.Range(0, 64).Select(i => (byte)i).ToArray();
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
        var smtcCover = new byte[48];
        Array.Fill(smtcCover, (byte)9);
        var smtc = new MediaSessionInfo("T", "A", "Spotify.exe", true, smtcCover, 1, 2);
        var wnp = new WnpPlayerSnapshot { CoverPng = Enumerable.Repeat((byte)1, 64).ToArray() };

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
    public void SplitFields_handles_escape_and_empty_marker()
    {
        var blob = $"1|YouTube Music|A\\|B|{((char)1)}|album|http://x|";
        var fields = WebNowPlayingReduxHost.SplitFields(blob);
        fields[0].Should().Be("1");
        fields[1].Should().Be("YouTube Music");
        fields[2].Should().Be("A|B");
        fields[3].Should().Be("");
        fields[4].Should().Be("album");
        fields[5].Should().Be("http://x");
    }
}
