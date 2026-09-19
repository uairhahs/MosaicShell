using FluentAssertions;
using MosaicShell.Core.Services;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// Guards the album-art contract that first made Tessera covers work: a cover a browser source
    /// supplies must reach <see cref="MediaSessionInfo.ThumbnailPng"/> with the ≥32-byte acceptance
    /// the UI ApplyArt path uses.
    /// </summary>
    public class TesseraAlbumArtRegressionTests
    {
        [Fact]
        public void Tiny_png_fixture_meets_apply_art_length_contract()
        {
            // A 1×1 PNG: far smaller than a real cover, but over the acceptance floor
            _ = TestImages.TinyPng.Length.Should().BeGreaterThanOrEqualTo(32);
            _ = CompositeMediaSessionService.IsUsableCover(TestImages.TinyPng).Should().BeTrue();
        }

        [Fact]
        public void Merge_puts_browser_tiny_png_on_media_session_for_browser()
        {
            MediaSessionInfo smtc = new(
            "Track | YouTube Music", "A", "Chrome.exe", true,
            ThumbnailPng: null, PositionSeconds: 1, DurationSeconds: 10);
            BrowserPlayerSnapshot browser = new()
            {
                Title = "Track",
                Artist = "A",
                Name = "YouTube Music",
                State = BrowserPlaybackState.Playing,
                CoverPng = TestImages.TinyPng,
            };

            MediaSessionInfo merged = CompositeMediaSessionService.Merge(smtc, browser)!;
            _ = merged.ThumbnailPng.Should().BeSameAs(TestImages.TinyPng);
            _ = merged.ThumbnailPng!.Length.Should().BeGreaterThanOrEqualTo(32);
        }

        [Fact]
        public void Merge_does_not_drop_browser_cover_when_smtc_has_tiny_stub()
        {
            byte[] stub = new byte[40];
            Array.Fill(stub, (byte)0xAB);
            MediaSessionInfo smtc = new(
            "Track", "A", "msedge", true, stub, 1, 10);
            BrowserPlayerSnapshot browser = new()
            {
                Title = "Track",
                CoverPng = TestImages.TinyPng,
                State = BrowserPlaybackState.Playing,
            };

            MediaSessionInfo merged = CompositeMediaSessionService.Merge(smtc, browser)!;
            _ = merged.ThumbnailPng.Should().BeSameAs(TestImages.TinyPng);
        }

        [Fact]
        public void Spotify_keeps_smtc_png_when_usable()
        {
            byte[] smtcPng = [.. TestImages.TinyPng];
            smtcPng[^1] ^= 0x01; // distinct instance, still PNG-shaped
            MediaSessionInfo smtc = new("T", "A", "Spotify.exe", true, smtcPng, 1, 2);
            BrowserPlayerSnapshot browser = new() { CoverPng = TestImages.TinyPng };

            MediaSessionInfo merged = CompositeMediaSessionService.Merge(smtc, browser)!;
            _ = merged.ThumbnailPng.Should().BeSameAs(smtcPng);
        }
    }
}
