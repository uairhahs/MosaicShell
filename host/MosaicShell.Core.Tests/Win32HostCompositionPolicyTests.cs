using FluentAssertions;
using MosaicShell.Core.Capabilities.Platform;
using MosaicShell.Core.HostPlatform;
using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Core.Tests
{
    public class Win32HostCompositionPolicyTests : IDisposable
    {
        public Win32HostCompositionPolicyTests()
        {
            HostLaunchOptions.ResetForTests();
            HostLaunchOptions.Apply([]);
        }

        public void Dispose()
        {
            HostLaunchOptions.ResetForTests();
            HostLaunchOptions.Apply([]);
        }

        [Fact]
        public void Prefer_winui_composition_tracks_soft_frost_window_mapping()
        {
            _ = Win32HostCompositionPolicy.PreferWinUiComposition.Should().Be(
                TesseraFlyoutWindowPolicy.PreferWinUiCompositionForSoftFrost,
                "Program must read Win32HostCompositionPolicy only; SoftFrost owns the mapping");
        }

        [Fact]
        public void Rendering_mode_pins_angle_egl_with_software_fallback()
        {
            _ = Win32HostCompositionPolicy.PreferAngleEglRendering.Should().BeTrue();
            _ = Win32HostCompositionPolicy.RenderingModeHints.Should().Equal("AngleEgl", "Software");
            _ = Win32HostCompositionPolicy.OsAcrylicRenderingAvailable.Should().BeTrue();
        }

        [Fact]
        public void Force_software_render_pins_software_only_and_blocks_os_acrylic()
        {
            HostLaunchOptions.Apply([HostLaunchOptions.TesseraForceSoftwareRenderFlag]);
            _ = Win32HostCompositionPolicy.RenderingModeHints.Should().Equal("Software");
            _ = Win32HostCompositionPolicy.OsAcrylicRenderingAvailable.Should().BeFalse();
            HostLaunchOptions.Apply(
            [
                HostLaunchOptions.TesseraOsAcrylicTrialFlag,
                HostLaunchOptions.TesseraForceSoftwareRenderFlag
            ]);
            _ = Win32HostCompositionPolicy.OsAcrylicTrialRequested.Should().BeFalse(
                "Software-only path must fall back to frost even when acrylic flag is on");
            _ = Win32HostCompositionPolicy.WinUiCompositionBackdropCornerRadius.Should().BeNull();
        }
    }
}
