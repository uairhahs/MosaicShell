using FluentAssertions;
using MosaicShell.Core.Services;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// The 2026-09-19 "artist and art are haphazard" report could not be answered from the log: the media
    /// signal line showed the merged result only, with art as a byte length and null and empty both
    /// printed as "-". These pin the attribution line that replaces that guesswork.
    /// </summary>
    public class MediaSourceAttributionTests
    {
        private const string PwaApp = "music.youtube.com-5929F88E_v";

        private static MediaSessionInfo Smtc(string? title, string? artist, byte[]? art = null)
        {
            return new MediaSessionInfo(title, artist, PwaApp, true, art, 5, 108);
        }

        [Fact]
        public void Smtc_only_browser_session_reports_no_artist_no_art_and_no_browser()
        {
            MediaSessionInfo smtc = Smtc("JUST DANCE | YouTube Music", null);
            MediaSessionInfo merged = CompositeMediaSessionService.Merge(smtc, null)!;

            string line = MediaSourceAttribution.Describe(smtc, null, merged);

            _ = line.Should().Contain("browser=none");
            _ = line.Should().Contain("smtc.title=[JUST DANCE | YouTube Music]");
            _ = line.Should().Contain("smtc.artist=null");
            _ = line.Should().Contain("smtc.art=none");
            _ = line.Should().Contain("use title=smtc artist=none art=none");
        }

        [Fact]
        public void Browser_supplying_title_artist_and_cover_is_reported_as_the_source()
        {
            byte[] cover = TestImages.TinyPng;
            MediaSessionInfo smtc = Smtc("Song | YouTube Music", null);
            BrowserPlayerSnapshot browser = new()
            {
                Name = "YouTube Music",
                Title = "Song",
                Artist = "Someone",
                State = BrowserPlaybackState.Playing,
                CoverPng = cover,
            };
            MediaSessionInfo merged = CompositeMediaSessionService.Merge(smtc, browser)!;

            string line = MediaSourceAttribution.Describe(smtc, browser, merged);

            _ = line.Should().Contain("browser=[name=YouTube Music state=Playing title=[Song] artist=[Someone]");
            _ = line.Should().Contain("use title=browser artist=browser art=browser");
        }

        [Fact]
        public void Smtc_supplying_everything_is_reported_as_smtc()
        {
            byte[] art = new byte[300]; // long enough for CompositeMediaSessionService.IsUsableCover
            MediaSessionInfo smtc = Smtc("Song", "Someone", art);
            MediaSessionInfo merged = CompositeMediaSessionService.Merge(smtc, null)!;

            string line = MediaSourceAttribution.Describe(smtc, null, merged);

            _ = line.Should().Contain("use title=smtc artist=smtc art=smtc");
        }

        [Fact]
        public void Browser_present_but_empty_is_distinguished_from_no_browser_source()
        {
            MediaSessionInfo smtc = Smtc("Song | YouTube Music", null);
            BrowserPlayerSnapshot browser = new() { State = BrowserPlaybackState.Stopped };
            MediaSessionInfo merged = CompositeMediaSessionService.Merge(smtc, browser)!;

            string line = MediaSourceAttribution.Describe(smtc, browser, merged);

            _ = line.Should().NotContain("browser=none");
            _ = line.Should().Contain("browser=[name= state=Stopped title=[] artist=[]");
            _ = line.Should().Contain("use title=smtc artist=none art=none");
        }

        [Fact]
        public void Art_is_identified_by_content_not_length()
        {
            byte[] a = new byte[64];
            byte[] b = new byte[64];
            b[10] = 1;

            string first = MediaSourceAttribution.Describe(Smtc("T", "A", a), null, Smtc("T", "A", a));
            string second = MediaSourceAttribution.Describe(Smtc("T", "A", b), null, Smtc("T", "A", b));
            string firstAgain = MediaSourceAttribution.Describe(Smtc("T", "A", [.. a]), null, Smtc("T", "A", [.. a]));

            _ = first.Should().Contain("smtc.art=64#");
            _ = second.Should().NotBe(first, "same length, different bytes");
            _ = firstAgain.Should().Be(first, "same bytes, different array instance");
        }

        [Fact]
        public void Null_and_empty_artist_are_written_differently()
        {
            string nullArtist = MediaSourceAttribution.Describe(Smtc("T", null), null, Smtc("T", null));
            string emptyArtist = MediaSourceAttribution.Describe(Smtc("T", ""), null, Smtc("T", ""));

            _ = nullArtist.Should().Contain("smtc.artist=null");
            _ = emptyArtist.Should().Contain("smtc.artist=[]");
        }

        [Fact]
        public void No_sources_at_all_is_reported_plainly()
        {
            string line = MediaSourceAttribution.Describe(null, null, null);

            _ = line.Should().Contain("smtc=none").And.Contain("browser=none");
        }
    }

    public class RunBannerTests
    {
        [Fact]
        public void Banner_carries_the_full_date_pid_and_build_commit()
        {
            DateTimeOffset started = new(2026, 9, 19, 16, 29, 26, 966, TimeSpan.FromHours(1));

            string banner = RunBanner.Format(started, 20040, "0.0.0-dev+9ad6a11015366b3c22f97ca1e9527a4f59111dd2");

            _ = banner.Should().StartWith("=== run start 2026-09-19T16:29:26.966+01:00");
            _ = banner.Should().Contain("pid=20040");
            _ = banner.Should().Contain("build=0.0.0-dev");
            _ = banner.Should().Contain("commit=9ad6a110");
            _ = banner.Should().EndWith("===");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("1.2.3")]
        public void Banner_survives_a_version_without_a_commit(string? version)
        {
            string banner = RunBanner.Format(DateTimeOffset.UnixEpoch, 1, version);

            _ = banner.Should().StartWith("=== run start ").And.Contain("commit=unknown");
        }
    }
}
