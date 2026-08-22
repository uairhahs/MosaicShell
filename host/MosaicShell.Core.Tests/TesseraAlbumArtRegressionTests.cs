using FluentAssertions;
using MosaicShell.Core.Services;
using MosaicShell.Core.Services.WebNowPlaying;

namespace MosaicShell.Core.Tests;

/// <summary>
/// Guards the album-art contract that first made Tessera covers work.
/// Protocol proof: <see cref="WebNowPlayingHostTests"/> (binary cover → Active.CoverPng).
/// Handoff proof: merge must put those same bytes on <see cref="MediaSessionInfo.ThumbnailPng"/>
/// with the ≥32-byte acceptance the UI ApplyArt path uses.
/// </summary>
public class TesseraAlbumArtRegressionTests
{
 [Fact]
 public void Wnp_tiny_png_fixture_meets_apply_art_length_contract()
 {
 // Same 1×1 PNG used by WebNowPlayingHostTests when album art first landed
 WebNowPlayingHostTests.TinyPng.Length.Should().BeGreaterThanOrEqualTo(32);
 CompositeMediaSessionService.IsUsableCover(WebNowPlayingHostTests.TinyPng).Should().BeTrue();
 }

 [Fact]
 public void Merge_puts_wnp_tiny_png_on_media_session_for_browser()
 {
 var smtc = new MediaSessionInfo(
 "Track | YouTube Music", "A", "Chrome.exe", true,
 ThumbnailPng: null, PositionSeconds: 1, DurationSeconds: 10);
 var wnp = new WnpPlayerSnapshot
 {
 Title = "Track",
 Artist = "A",
 Name = "YouTube Music",
 State = WnpState.Playing,
 CoverPng = WebNowPlayingHostTests.TinyPng,
 };

 var merged = CompositeMediaSessionService.Merge(smtc, wnp)!;
 merged.ThumbnailPng.Should().BeSameAs(WebNowPlayingHostTests.TinyPng);
 merged.ThumbnailPng!.Length.Should().BeGreaterThanOrEqualTo(32);
 }

 [Fact]
 public void Merge_does_not_drop_wnp_cover_when_smtc_has_tiny_stub()
 {
 var stub = new byte[40];
 Array.Fill(stub, (byte)0xAB);
 var smtc = new MediaSessionInfo(
 "Track", "A", "msedge", true, stub, 1, 10);
 var wnp = new WnpPlayerSnapshot
 {
 Title = "Track",
 CoverPng = WebNowPlayingHostTests.TinyPng,
 State = WnpState.Playing,
 };

 var merged = CompositeMediaSessionService.Merge(smtc, wnp)!;
 merged.ThumbnailPng.Should().BeSameAs(WebNowPlayingHostTests.TinyPng);
 }

 [Fact]
 public void Spotify_keeps_smtc_png_when_usable()
 {
 var smtcPng = WebNowPlayingHostTests.TinyPng.ToArray();
 smtcPng[^1] ^= 0x01; // distinct instance, still PNG-shaped
 var smtc = new MediaSessionInfo("T", "A", "Spotify.exe", true, smtcPng, 1, 2);
 var wnp = new WnpPlayerSnapshot { CoverPng = WebNowPlayingHostTests.TinyPng };

 var merged = CompositeMediaSessionService.Merge(smtc, wnp)!;
 merged.ThumbnailPng.Should().BeSameAs(smtcPng);
 }
}
