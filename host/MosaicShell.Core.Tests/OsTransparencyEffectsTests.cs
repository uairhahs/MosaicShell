using FluentAssertions;
using MosaicShell.Core.Modules.Tessera;
using MosaicShell.Core.Services;
using MosaicShell.Core.Settings;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// Windows' Transparency effects switch (Settings, Personalization, Colors) is the user's say on see-through
    /// surfaces. With it off, Windows' own flyouts turn opaque, and so do Tessera's: Frosted and Native Translucency
    /// draw as Solid, with no setting of MosaicShell's own.
    /// </summary>
    public sealed class OsTransparencyEffectsTests
    {
        [Theory]
        [InlineData(true, "1")]
        [InlineData(false, "0")]
        public void The_flyout_is_see_through_only_while_the_OS_allows_it(bool osAllows, string acrylic)
        {
            HostServices services = HostServicesFakes.Create(transparencyEffects: osAllows);
            TesseraSettings settings = new() { UseAcrylicBackdrop = true, UseNativeTranslucency = true };

            Dictionary<string, string> payload = new TesseraFlyoutRequestBuilder().BuildPayload(services, settings, "vol");

            _ = payload["acrylic"].Should().Be(acrylic);
        }

        [Fact]
        public void With_transparency_off_the_material_is_Solid_whatever_the_choice()
        {
            HostServices services = HostServicesFakes.Create(transparencyEffects: false);
            TesseraSettings settings = new() { UseAcrylicBackdrop = true, UseNativeTranslucency = true };

            Dictionary<string, string> payload = new TesseraFlyoutRequestBuilder().BuildPayload(services, settings, "vol");
            TesseraFlyoutMaterial material = TesseraFlyoutMaterialFactory.FromPayload(payload);

            _ = material.UseSoftFrost.Should().BeFalse();
            _ = material.UseNativeTranslucency.Should().BeFalse();
        }

        [Fact]
        public void A_Solid_choice_stays_Solid_with_transparency_on()
        {
            HostServices services = HostServicesFakes.Create();
            TesseraSettings settings = new() { UseAcrylicBackdrop = false };

            _ = new TesseraFlyoutRequestBuilder().BuildPayload(services, settings, "vol")["acrylic"].Should().Be("0");
        }

        [Theory]
        [InlineData(null, true)]
        [InlineData(1, true)]
        [InlineData(0, false)]
        public void Windows_reads_the_switch_as_on_unless_it_says_zero(int? registryValue, bool enabled)
        {
            _ = WindowsTransparencyEffects.FromRegistryValue(registryValue).Should().Be(enabled);
        }

        [Fact]
        public void Desktops_without_such_a_switch_allow_transparency()
        {
            _ = new AlwaysTransparencyEffects().Enabled.Should().BeTrue();
            _ = HostServicesFakes.Create().TransparencyEffects.Enabled.Should().BeTrue();
        }
    }
}
