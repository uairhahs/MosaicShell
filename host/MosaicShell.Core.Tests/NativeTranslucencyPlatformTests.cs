using FluentAssertions;
using MosaicShell.Core.Capabilities.Platform;
using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// Which OS mechanism draws Native Translucency: Windows acrylic, KWin's blur behind an X11 window, or none. The
    /// policies only ask whether there is one; the window material asks which, because Windows takes it as a
    /// transparency level and KWin takes it as a window property the Host sets.
    /// </summary>
    public sealed class NativeTranslucencyPlatformTests : IDisposable
    {
        public void Dispose()
        {
            NativeTranslucencyPlatform.ResetForTests();
        }

        [Theory]
        [InlineData(true, true, false, false, false, NativeTranslucencyBackend.WindowsAcrylic)]
        [InlineData(true, false, false, false, false, NativeTranslucencyBackend.None)]
        [InlineData(false, false, true, true, true, NativeTranslucencyBackend.KWinBlur)]
        [InlineData(false, false, true, true, false, NativeTranslucencyBackend.None)]
        [InlineData(false, false, true, false, true, NativeTranslucencyBackend.None)]
        [InlineData(false, false, false, false, false, NativeTranslucencyBackend.None)]
        public void The_backend_follows_the_platform_facts(
            bool isWindows,
            bool windowsSupportsWinUiAcrylic,
            bool isLinux,
            bool hasX11Display,
            bool kwinBlurLoaded,
            NativeTranslucencyBackend expected)
        {
            _ = NativeTranslucencyPlatform.Resolve(isWindows, windowsSupportsWinUiAcrylic, isLinux, hasX11Display, kwinBlurLoaded)
                .Should().Be(expected);
        }

        [Fact]
        public void Before_the_host_probes_only_windows_has_a_backend()
        {
            NativeTranslucencyPlatform.ResetForTests();

            _ = NativeTranslucencyPlatform.IsAvailable.Should().Be(
                OperatingSystem.IsWindows() && TesseraNativeTranslucencyPolicy.OsSupportsWinUiAcrylic);
        }

        [Fact]
        public void The_host_can_set_the_probed_backend()
        {
            NativeTranslucencyPlatform.Set(NativeTranslucencyBackend.KWinBlur);

            _ = NativeTranslucencyPlatform.Backend.Should().Be(NativeTranslucencyBackend.KWinBlur);
            _ = NativeTranslucencyPlatform.IsAvailable.Should().BeTrue();
        }

        [Fact]
        public void Windows_asks_for_acrylic_with_a_transparent_fallback()
        {
            TesseraFlyoutMaterial m = TesseraFlyoutMaterialFactory.Create(
                useAcrylic: true, nativeTranslucencyEligible: true, NativeTranslucencyBackend.WindowsAcrylic);

            _ = m.UseNativeTranslucency.Should().BeTrue();
            _ = m.TransparencyHints.Should().Equal("AcrylicBlur", "Transparent");
        }

        [Fact]
        public void Kwin_takes_a_plain_transparent_window_and_blurs_behind_it()
        {
            // The blur is a window property the Host sets with the card's rounded shape; Avalonia's own Blur level
            // would blur the whole window rectangle, square corners included.
            TesseraFlyoutMaterial m = TesseraFlyoutMaterialFactory.Create(
                useAcrylic: true, nativeTranslucencyEligible: true, NativeTranslucencyBackend.KWinBlur);

            _ = m.UseNativeTranslucency.Should().BeTrue();
            _ = m.TransparencyHints.Should().Equal("Transparent");
        }

        [Theory]
        [InlineData(true, false, NativeTranslucencyBackend.WindowsAcrylic)]
        [InlineData(false, true, NativeTranslucencyBackend.KWinBlur)]
        [InlineData(true, true, NativeTranslucencyBackend.None)]
        public void Frost_off_ineligible_or_no_backend_means_no_native_translucency(
            bool useAcrylic, bool eligible, NativeTranslucencyBackend backend)
        {
            TesseraFlyoutMaterial m = TesseraFlyoutMaterialFactory.Create(useAcrylic, eligible, backend);

            _ = m.UseNativeTranslucency.Should().BeFalse();
            _ = m.TransparencyHints.Should().Equal("Transparent");
        }

        [Fact]
        public void Eligibility_from_the_payload_needs_a_backend()
        {
            Dictionary<string, string> single = new() { ["showMediaStrip"] = "0", ["acrylic"] = "1" };

            NativeTranslucencyPlatform.Set(NativeTranslucencyBackend.None);
            _ = TesseraNativeTranslucencyPolicy.IsEligible(single, trialRequested: true,
                    platformSupported: NativeTranslucencyPlatform.IsAvailable)
                .Should().BeFalse();

            NativeTranslucencyPlatform.Set(NativeTranslucencyBackend.KWinBlur);
            _ = TesseraNativeTranslucencyPolicy.IsEligible(single, trialRequested: true,
                    platformSupported: NativeTranslucencyPlatform.IsAvailable)
                .Should().BeTrue();
        }
    }
}
