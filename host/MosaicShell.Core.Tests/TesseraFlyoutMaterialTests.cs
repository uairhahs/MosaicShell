using FluentAssertions;
using MosaicShell.Core.Capabilities;
using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Core.Tests;

public class TesseraFlyoutMaterialTests
{
    [Fact]
    public void Soft_frost_never_requests_os_acrylic_or_blur()
    {
        var m = TesseraFlyoutMaterialFactory.Create(useAcrylic: true);
        m.UseSoftFrost.Should().BeTrue();
        m.TransparencyHints.Should().Equal("Transparent");
        m.TransparencyHints.Should().NotContain("AcrylicBlur");
        m.TransparencyHints.Should().NotContain("Blur");
    }

    [Fact]
    public void Soft_frost_uses_edge_blend()
    {
        var m = TesseraFlyoutMaterialFactory.Create(useAcrylic: true);
        m.UseEdgeBlend.Should().BeTrue();
        m.ShouldLockClientSize.Should().BeFalse();
    }

    [Fact]
    public void Solid_mode_is_Transparent_only_without_edge_blend()
    {
        var m = TesseraFlyoutMaterialFactory.Create(useAcrylic: false);
        m.UseSoftFrost.Should().BeFalse();
        m.UseEdgeBlend.Should().BeFalse();
        m.TransparencyHints.Should().Equal("Transparent");
    }

    [Fact]
    public void Soft_frost_shell_alpha_is_translucent_not_see_through()
    {
        var m = TesseraFlyoutMaterialFactory.Create(useAcrylic: true);
        m.ShellAlpha.Should().BeInRange((byte)170, (byte)210);
    }

    [Fact]
    public void Solid_shell_alpha_is_more_opaque()
    {
        var m = TesseraFlyoutMaterialFactory.Create(useAcrylic: false);
        m.ShellAlpha.Should().BeGreaterThanOrEqualTo((byte)220);
    }

    [Fact]
    public void Payload_acrylic_flag_parses()
    {
        TesseraFlyoutMaterialFactory.UseAcrylicFromPayload(
            new Dictionary<string, string> { ["acrylic"] = "1" }).Should().BeTrue();
        TesseraFlyoutMaterialFactory.UseAcrylicFromPayload(
            new Dictionary<string, string> { ["acrylic"] = "0" }).Should().BeFalse();
        TesseraFlyoutMaterialFactory.UseAcrylicFromPayload(null).Should().BeTrue();
    }
}
