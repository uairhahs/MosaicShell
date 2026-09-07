using FluentAssertions;
using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Core.Tests
{
    public class TesseraFlyoutMaterialTests
    {
        [Fact]
        public void Soft_frost_never_requests_os_acrylic_or_blur()
        {
            TesseraFlyoutMaterial m = TesseraFlyoutMaterialFactory.Create(useAcrylic: true);
            _ = m.UseSoftFrost.Should().BeTrue();
            _ = m.TransparencyHints.Should().Equal("Transparent");
            _ = m.TransparencyHints.Should().NotContain("AcrylicBlur");
            _ = m.TransparencyHints.Should().NotContain("Blur");
        }

        [Fact]
        public void Soft_frost_uses_edge_blend()
        {
            TesseraFlyoutMaterial m = TesseraFlyoutMaterialFactory.Create(useAcrylic: true);
            _ = m.UseEdgeBlend.Should().BeTrue();
            _ = m.ShouldLockClientSize.Should().BeFalse();
        }

        [Fact]
        public void Solid_mode_is_Transparent_only_without_edge_blend()
        {
            TesseraFlyoutMaterial m = TesseraFlyoutMaterialFactory.Create(useAcrylic: false);
            _ = m.UseSoftFrost.Should().BeFalse();
            _ = m.UseEdgeBlend.Should().BeFalse();
            _ = m.TransparencyHints.Should().Equal("Transparent");
        }

        [Fact]
        public void Soft_frost_shell_alpha_is_translucent_not_see_through()
        {
            TesseraFlyoutMaterial m = TesseraFlyoutMaterialFactory.Create(useAcrylic: true);
            _ = m.ShellAlpha.Should().BeInRange(170, 210);
        }

        [Fact]
        public void Solid_shell_alpha_is_more_opaque()
        {
            TesseraFlyoutMaterial m = TesseraFlyoutMaterialFactory.Create(useAcrylic: false);
            _ = m.ShellAlpha.Should().BeGreaterThanOrEqualTo(220);
        }

        [Fact]
        public void Payload_acrylic_flag_parses()
        {
            _ = TesseraFlyoutMaterialFactory.UseAcrylicFromPayload(
                new Dictionary<string, string> { ["acrylic"] = "1" }).Should().BeTrue();
            _ = TesseraFlyoutMaterialFactory.UseAcrylicFromPayload(
                new Dictionary<string, string> { ["acrylic"] = "0" }).Should().BeFalse();
            _ = TesseraFlyoutMaterialFactory.UseAcrylicFromPayload(null).Should().BeTrue();
        }

        /// <summary>
        /// MaterialYou's pills are the only chrome the style paints - there is no enclosing card
        /// to mask a failed glass composite, so unlike every other style it must never touch OS
        /// Acrylic or the Skia soft-frost glass. Confirmed as a real, reproducible defect
        /// (2026-09-07): AcrylicBlur reported success identically on two monitors while only one
        /// actually composited it, leaving raw desktop passthrough between the pills on the other.
        /// The window itself stays plain <c>Transparent</c> either way - true per-pixel alpha, not
        /// an OS material - so the desktop still shows through the gaps between pills exactly as
        /// intended; only the two glass paths are excluded, regardless of the acrylic/soft-frost
        /// payload settings that govern every other style.
        /// </summary>
        [Theory]
        [InlineData("1")]
        [InlineData("0")]
        [InlineData(null)]
        public void MaterialYou_never_uses_glass_regardless_of_acrylic_payload(string? acrylicPayloadValue)
        {
            Dictionary<string, string>? payload = acrylicPayloadValue is null
                ? null
                : new Dictionary<string, string> { ["acrylic"] = acrylicPayloadValue };

            TesseraFlyoutMaterial m = TesseraFlyoutMaterialFactory.FromPayload(
                payload, MosaicShell.Core.Styles.StyleIds.MaterialYou, "vol");
            _ = m.UseSoftFrost.Should().BeFalse();
            _ = m.TransparencyHints.Should().Equal("Transparent");
            _ = m.TransparencyHints.Should().NotContain("AcrylicBlur");
            _ = m.UseEdgeBlend.Should().BeFalse();
        }

        [Fact]
        public void Other_styles_still_use_soft_frost_by_default()
        {
            TesseraFlyoutMaterial m = TesseraFlyoutMaterialFactory.FromPayload(
                null, MosaicShell.Core.Styles.StyleIds.Fluent, "vol");
            _ = m.UseSoftFrost.Should().BeTrue();
        }

        [Fact]
        public void MaterialYou_never_reports_os_acrylic_eligible()
        {
            // Deterministic overload: bypasses HostLaunchOptions/settings-file reads so this
            // isolates the per-style exclusion from environment-dependent trial state.
            _ = TesseraOsAcrylicTrialPolicy.IsEligible(
                    payload: null,
                    trialRequested: true,
                    osSupportsWinUiAcrylic: true,
                    styleId: MosaicShell.Core.Styles.StyleIds.MaterialYou)
                .Should().BeFalse();
        }
    }
}
