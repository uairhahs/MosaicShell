using FluentAssertions;
using MosaicShell.Core.Capabilities.Platform;
using MosaicShell.Core.Modules.Tessera;
using MosaicShell.Core.Services;
using MosaicShell.Core.Settings;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// Frosted and Native Translucency on a desktop whose compositor blurs (KWin): both ask for the OS blur, and differ
    /// in what the app paints over it. Frosted paints the frosted glass (light shade and frost, highlight, crisp edge,
    /// faint grain, shadowed content); Native Translucency is the plain OS material, the blur under a flat tint. Where
    /// the OS draws the whole material itself (Windows acrylic) or there is no blur, Frosted keeps the app's own frost.
    /// </summary>
    public sealed class TesseraFrostedGlassTests
    {
        [Theory]
        [InlineData(false, true, NativeTranslucencyBackend.KWinBlur, true)]
        [InlineData(true, true, NativeTranslucencyBackend.KWinBlur, true)]
        [InlineData(false, false, NativeTranslucencyBackend.KWinBlur, false)]
        [InlineData(false, true, NativeTranslucencyBackend.WindowsAcrylic, false)]
        [InlineData(true, true, NativeTranslucencyBackend.WindowsAcrylic, true)]
        [InlineData(false, true, NativeTranslucencyBackend.None, false)]
        public void Frosted_asks_for_the_OS_blur_only_where_the_compositor_draws_just_the_blur(
            bool useNative, bool useAcrylic, NativeTranslucencyBackend backend, bool wantsBlur)
        {
            _ = TesseraNativeTranslucencyPolicy.WantsOsBlur(useNative, useAcrylic, backend).Should().Be(wantsBlur);
        }

        [Fact]
        public void On_KWin_Frosted_is_the_blur_with_the_frosted_glass_over_it()
        {
            TesseraFlyoutMaterial frosted = TesseraFlyoutMaterialFactory.Create(
                useAcrylic: true, nativeTranslucencyEligible: true, NativeTranslucencyBackend.KWinBlur, frostedChoice: true);

            _ = frosted.UseNativeTranslucency.Should().BeTrue();
            _ = frosted.FrostedGlass.Should().BeTrue();
        }

        [Fact]
        public void On_KWin_Native_Translucency_is_the_plain_blur()
        {
            TesseraFlyoutMaterial native = TesseraFlyoutMaterialFactory.Create(
                useAcrylic: true, nativeTranslucencyEligible: true, NativeTranslucencyBackend.KWinBlur, frostedChoice: false);

            _ = native.UseNativeTranslucency.Should().BeTrue();
            _ = native.FrostedGlass.Should().BeFalse();
        }

        [Fact]
        public void Windows_acrylic_is_drawn_whole_by_the_OS_so_no_frosted_glass_over_it()
        {
            TesseraFlyoutMaterial material = TesseraFlyoutMaterialFactory.Create(
                useAcrylic: true, nativeTranslucencyEligible: true, NativeTranslucencyBackend.WindowsAcrylic, frostedChoice: true);

            _ = material.FrostedGlass.Should().BeFalse();
        }

        [Fact]
        public void Switching_between_Frosted_and_Native_rebuilds_the_window()
        {
            TesseraFlyoutMaterial frosted = TesseraFlyoutMaterialFactory.Create(true, true, NativeTranslucencyBackend.KWinBlur, frostedChoice: true);
            TesseraFlyoutMaterial native = TesseraFlyoutMaterialFactory.Create(true, true, NativeTranslucencyBackend.KWinBlur, frostedChoice: false);

            _ = frosted.SameWindowMaterial(native).Should().BeFalse();
        }

        [Theory]
        [InlineData(false, "frosted", true)]
        [InlineData(true, "native", false)]
        public void The_payload_carries_the_choice_to_the_flyout(bool useNative, string glass, bool frosted)
        {
            TesseraSettings settings = new() { UseAcrylicBackdrop = true, UseNativeTranslucency = useNative };

            Dictionary<string, string> payload = new TesseraFlyoutRequestBuilder().BuildPayload(HostServicesFakes.Create(), settings, "vol");

            _ = payload["glass"].Should().Be(glass);
            _ = TesseraFlyoutMaterialFactory.FrostedChoiceFromPayload(payload).Should().Be(frosted);
        }

        [Fact]
        public void The_plain_veil_is_a_light_neutral_so_the_pane_reads_translucent_not_darker()
        {
            // A dark tint over the blur only darkened it and kept its colour: transparent and saturated, not glass.
            double opacity = NativeTranslucencyAcrylicSpec.PlainTintAlpha / 255.0;
            int r = NativeTranslucencyAcrylicSpec.PlainTintR;
            int g = NativeTranslucencyAcrylicSpec.PlainTintG;
            int b = NativeTranslucencyAcrylicSpec.PlainTintB;
            double luminance = ((0.2126 * r) + (0.7152 * g) + (0.0722 * b)) / 255.0;

            _ = opacity.Should().BeInRange(0.10, 0.25);
            _ = luminance.Should().BeGreaterThan(0.7, "light, so dark backdrops lift");
            _ = (Math.Max(r, Math.Max(g, b)) - Math.Min(r, Math.Min(g, b))).Should().BeLessThan(16, "neutral, so strong colours wash a little");
        }

        [Fact]
        public void Both_glass_finishes_over_KWin_shadow_their_content()
        {
            _ = NativeTranslucencyAcrylicSpec.ShadowsContent(NativeTranslucencyBackend.KWinBlur).Should().BeTrue();
            _ = NativeTranslucencyAcrylicSpec.ShadowsContent(NativeTranslucencyBackend.WindowsAcrylic).Should().BeFalse();
        }
    }
}
