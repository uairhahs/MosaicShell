using System.Net;
using FluentAssertions;
using MosaicShell.Core.Services.BrowserBridge;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// The artwork URL comes from a web page, and the extension runs on every https site, so any page can name any
    /// address. The Host must never be talked into requesting something on the user's own network.
    /// </summary>
    public class BrowserArtworkPolicyTests
    {
        private static BrowserArtwork Art(string src, string sizes = "")
        {
            return new BrowserArtwork(src, sizes);
        }

        [Fact]
        public void The_largest_advertised_size_is_chosen()
        {
            BrowserArtwork chosen = BrowserArtworkPolicy.Choose(
            [
                Art("https://a.example/small.jpg", "96x96"),
                Art("https://a.example/big.jpg", "512x512"),
                Art("https://a.example/mid.jpg", "256x256"),
            ])!;

            _ = chosen.Src.Should().Be("https://a.example/big.jpg");
        }

        [Fact]
        public void An_entry_may_list_several_sizes_and_counts_its_largest()
        {
            BrowserArtwork chosen = BrowserArtworkPolicy.Choose(
            [
                Art("https://a.example/multi.png", "48x48 512x512"),
                Art("https://a.example/single.png", "256x256"),
            ])!;

            _ = chosen.Src.Should().Be("https://a.example/multi.png");
        }

        [Fact]
        public void Area_decides_not_width_alone()
        {
            BrowserArtwork chosen = BrowserArtworkPolicy.Choose(
            [
                Art("https://a.example/wide.jpg", "640x100"),
                Art("https://a.example/square.jpg", "300x300"),
            ])!;

            _ = chosen.Src.Should().Be("https://a.example/square.jpg");
        }

        [Fact]
        public void Entries_without_a_usable_size_are_still_chosen_when_nothing_better_exists_and_the_first_wins_a_tie()
        {
            _ = BrowserArtworkPolicy.Choose([Art("https://a.example/1.jpg"), Art("https://a.example/2.jpg", "any")])!.Src
                .Should().Be("https://a.example/1.jpg");
        }

        [Fact]
        public void A_sized_entry_beats_an_unsized_one()
        {
            _ = BrowserArtworkPolicy.Choose([Art("https://a.example/1.jpg"), Art("https://a.example/2.jpg", "64x64")])!.Src
                .Should().Be("https://a.example/2.jpg");
        }

        [Fact]
        public void An_absurd_size_claim_does_not_beat_a_real_one()
        {
            BrowserArtwork chosen = BrowserArtworkPolicy.Choose(
            [
                Art("https://a.example/liar.jpg", "99999x99999"),
                Art("https://a.example/honest.jpg", "1024x1024"),
            ])!;

            _ = chosen.Src.Should().Be("https://a.example/honest.jpg");
        }

        [Fact]
        public void Blob_and_unfetchable_entries_are_never_chosen()
        {
            BrowserArtwork? chosen = BrowserArtworkPolicy.Choose(
            [
                Art("blob:https://a.example/uuid", "1024x1024"),
                Art("https://127.0.0.1/x.jpg", "1024x1024"),
                Art("https://a.example/ok.jpg", "16x16"),
            ]);

            _ = chosen!.Src.Should().Be("https://a.example/ok.jpg");
        }

        [Fact]
        public void No_usable_entry_chooses_nothing()
        {
            _ = BrowserArtworkPolicy.Choose([]).Should().BeNull();
            _ = BrowserArtworkPolicy.Choose([Art("blob:https://a.example/uuid")]).Should().BeNull();
        }

        [Theory]
        [InlineData("https://i.ytimg.com/vi/x/sddefault.jpg")]
        [InlineData("https://lh3.googleusercontent.com/abc=w544-h544")]
        [InlineData("https://8.8.8.8/x.png")]
        [InlineData("https://[2606:4700:4700::1111]/x.png")]
        [InlineData("https://cdn.example.co.uk:8443/x.png")]
        public void Public_https_addresses_are_fetchable(string url)
        {
            _ = BrowserArtworkPolicy.IsFetchableUrl(url).Should().BeTrue();
        }

        [Theory]
        [InlineData("http://i.ytimg.com/x.jpg")]
        [InlineData("blob:https://a.example/uuid")]
        [InlineData("data:image/png;base64,AAAA")]
        [InlineData("file:///c:/x.png")]
        [InlineData("ftp://a.example/x.png")]
        [InlineData("https://localhost/x.png")]
        [InlineData("https://LOCALHOST/x.png")]
        [InlineData("https://app.localhost/x.png")]
        [InlineData("https://printer.local/x.png")]
        [InlineData("https://intranet.internal/x.png")]
        [InlineData("https://router/x.png")]
        [InlineData("https://127.0.0.1/x.png")]
        [InlineData("https://10.0.0.5/x.png")]
        [InlineData("https://192.168.1.1/x.png")]
        [InlineData("https://172.16.0.1/x.png")]
        [InlineData("https://169.254.169.254/latest/meta-data")]
        [InlineData("https://[::1]/x.png")]
        [InlineData("https://[fe80::1]/x.png")]
        [InlineData("https://[::ffff:10.0.0.1]/x.png")]
        [InlineData("https://user:pass@a.example/x.png")]
        [InlineData("not a url")]
        [InlineData("")]
        public void Anything_else_is_not_fetchable(string url)
        {
            _ = BrowserArtworkPolicy.IsFetchableUrl(url).Should().BeFalse();
        }

        [Theory]
        [InlineData("8.8.8.8", true)]
        [InlineData("1.1.1.1", true)]
        [InlineData("172.15.255.255", true)]
        [InlineData("172.32.0.0", true)]
        [InlineData("100.63.255.255", true)]
        [InlineData("100.128.0.0", true)]
        [InlineData("2606:4700:4700::1111", true)]
        [InlineData("0.0.0.0", false)]
        [InlineData("0.1.2.3", false)]
        [InlineData("10.255.255.255", false)]
        [InlineData("100.64.0.1", false)]
        [InlineData("100.127.255.255", false)]
        [InlineData("127.0.0.1", false)]
        [InlineData("127.255.255.254", false)]
        [InlineData("169.254.0.1", false)]
        [InlineData("172.16.0.0", false)]
        [InlineData("172.31.255.255", false)]
        [InlineData("192.168.0.0", false)]
        [InlineData("192.0.0.1", false)]
        [InlineData("224.0.0.1", false)]
        [InlineData("240.0.0.1", false)]
        [InlineData("255.255.255.255", false)]
        [InlineData("::", false)]
        [InlineData("::1", false)]
        [InlineData("fe80::1", false)]
        [InlineData("fc00::1", false)]
        [InlineData("fd12:3456::1", false)]
        [InlineData("ff02::1", false)]
        [InlineData("::ffff:127.0.0.1", false)]
        [InlineData("::ffff:192.168.1.1", false)]
        [InlineData("::ffff:8.8.8.8", true)]
        public void Only_public_addresses_are_allowed(string address, bool expected)
        {
            _ = BrowserArtworkPolicy.IsPublicAddress(IPAddress.Parse(address)).Should().Be(expected);
        }

        [Theory]
        [InlineData(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0 }, true)]
        [InlineData(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0, 0, 0, 0, 0, 0, 0, 0 }, true)]
        [InlineData(new byte[] { 0x52, 0x49, 0x46, 0x46, 1, 2, 3, 4, 0x57, 0x45, 0x42, 0x50 }, true)]
        [InlineData(new byte[] { 0x3C, 0x68, 0x74, 0x6D, 0x6C, 0x3E, 0, 0, 0, 0, 0, 0 }, false)]
        [InlineData(new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61, 0, 0, 0, 0, 0, 0 }, false)]
        [InlineData(new byte[] { 0x52, 0x49, 0x46, 0x46, 1, 2, 3, 4, 0x57, 0x41, 0x56, 0x45 }, false)]
        [InlineData(new byte[] { 0x89, 0x50 }, false)]
        [InlineData(new byte[0], false)]
        public void Only_png_jpeg_and_webp_bytes_count_as_an_image(byte[] bytes, bool expected)
        {
            _ = BrowserArtworkPolicy.LooksLikeSupportedImage(bytes).Should().Be(expected);
        }
    }
}
