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
    }
}
