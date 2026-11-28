using FluentAssertions;
using MosaicShell.Core.Capabilities.Platform;
using MosaicShell.Core.Modules.Tessera;
using MosaicShell.Core.Services;
using MosaicShell.Core.Settings;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// Frosted and Native Translucency where the OS blurs behind the card (KWin's blur effect; on Windows, the blur
    /// Avalonia's AcrylicBlur level asks the compositor for, which carries no tint, luminosity or noise): both ask for
    /// the blur and differ in what the app paints over it. Frosted paints the frosted glass (light shade and frost,
    /// highlight, crisp edge, faint grain, shadowed content); Native Translucency is the plain material, the blur under a
    /// flat veil. Without a blur, Frosted keeps the app's own frost.
    /// </summary>
    public sealed class TesseraFrostedGlassTests
    {
        [Theory]
        [InlineData(false, true, NativeTranslucencyBackend.KWinBlur, true)]
        [InlineData(true, true, NativeTranslucencyBackend.KWinBlur, true)]
        [InlineData(false, false, NativeTranslucencyBackend.KWinBlur, false)]
        [InlineData(false, true, NativeTranslucencyBackend.WindowsAcrylic, true)]
        [InlineData(true, true, NativeTranslucencyBackend.WindowsAcrylic, true)]
        [InlineData(false, false, NativeTranslucencyBackend.WindowsAcrylic, false)]
        [InlineData(false, true, NativeTranslucencyBackend.None, false)]
        public void Frosted_asks_for_the_OS_blur_wherever_there_is_one(
            bool useNative, bool useAcrylic, NativeTranslucencyBackend backend, bool wantsBlur)
        {
            _ = TesseraNativeTranslucencyPolicy.WantsOsBlur(useNative, useAcrylic, backend).Should().Be(wantsBlur);
        }

        [Theory]
        [InlineData(NativeTranslucencyBackend.KWinBlur)]
        [InlineData(NativeTranslucencyBackend.WindowsAcrylic)]
        public void Frosted_is_the_blur_with_the_frosted_glass_over_it(NativeTranslucencyBackend backend)
        {
            TesseraFlyoutMaterial frosted = TesseraFlyoutMaterialFactory.Create(
                useAcrylic: true, nativeTranslucencyEligible: true, backend, frostedChoice: true);

            _ = frosted.UseNativeTranslucency.Should().BeTrue();
            _ = frosted.FrostedGlass.Should().BeTrue();
        }

        [Theory]
        [InlineData(NativeTranslucencyBackend.KWinBlur)]
        [InlineData(NativeTranslucencyBackend.WindowsAcrylic)]
        public void Native_Translucency_is_the_plain_blur(NativeTranslucencyBackend backend)
        {
            TesseraFlyoutMaterial native = TesseraFlyoutMaterialFactory.Create(
                useAcrylic: true, nativeTranslucencyEligible: true, backend, frostedChoice: false);

            _ = native.UseNativeTranslucency.Should().BeTrue();
            _ = native.FrostedGlass.Should().BeFalse();
        }

        [Fact]
        public void Windows_still_asks_for_its_blur_through_AcrylicBlur()
        {
            TesseraFlyoutMaterial frosted = TesseraFlyoutMaterialFactory.Create(
                useAcrylic: true, nativeTranslucencyEligible: true, NativeTranslucencyBackend.WindowsAcrylic, frostedChoice: true);

            _ = frosted.TransparencyHints.Should().Equal("AcrylicBlur", "Transparent");
        }

        [Fact]
        public void Without_a_blur_Frosted_keeps_the_apps_own_frost()
        {
            TesseraFlyoutMaterial frosted = TesseraFlyoutMaterialFactory.Create(
                useAcrylic: true, nativeTranslucencyEligible: true, NativeTranslucencyBackend.None, frostedChoice: true);

            _ = frosted.UseNativeTranslucency.Should().BeFalse();
            _ = frosted.FrostedGlass.Should().BeFalse();
            _ = frosted.UseSoftFrost.Should().BeTrue();
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
        public void The_plain_material_has_a_soft_edge_and_Frosted_a_crisp_one()
        {
            // The two finishes must read apart at a glance: Frosted catches the light on its edge, the plain pane barely.
            _ = NativeTranslucencyAcrylicSpec.PlainEdgeAlpha.Should().BeGreaterThan(0);
            _ = ((int)NativeTranslucencyAcrylicSpec.PlainEdgeAlpha).Should().BeLessThan(NativeTranslucencyAcrylicSpec.EdgeAlpha / 2);
        }

        [Fact]
        public void Both_glass_finishes_over_a_blur_shadow_their_content()
        {
            _ = NativeTranslucencyAcrylicSpec.ShadowsContent(NativeTranslucencyBackend.KWinBlur).Should().BeTrue();
            _ = NativeTranslucencyAcrylicSpec.ShadowsContent(NativeTranslucencyBackend.WindowsAcrylic).Should().BeTrue();
            _ = NativeTranslucencyAcrylicSpec.ShadowsContent(NativeTranslucencyBackend.None).Should().BeFalse();
        }

        [Theory]
        [InlineData(false, false, true, false, true)]
        [InlineData(false, true, true, false, false)]
        [InlineData(true, true, true, false, false)]
        [InlineData(true, true, false, false, true)]
        [InlineData(false, true, false, false, true)]
        public void Windows_needs_a_restart_only_when_the_OS_blur_comes_or_goes(
            bool wasNative, bool wasGlass, bool isGlass, bool isNative, bool restart)
        {
            // The rounded blur's corner radius is a Win32 option read once at start. Frosted and Native both use the blur
            // on Windows now, so moving between them needs none; Solid to either, or back, does.
            _ = TesseraNativeTranslucencyPolicy.MaterialChangeNeedsRestart(
                    isWindows: true, wasNative, wasGlass, isNative, isGlass, NativeTranslucencyBackend.WindowsAcrylic)
                .Should().Be(restart);
        }

        [Fact]
        public void KWin_never_needs_a_restart_for_the_material()
        {
            _ = TesseraNativeTranslucencyPolicy.MaterialChangeNeedsRestart(
                    isWindows: false, wasNative: false, wasGlass: false, isNative: true, isGlass: true, NativeTranslucencyBackend.KWinBlur)
                .Should().BeFalse();
        }
    }
}
