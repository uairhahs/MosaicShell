using FluentAssertions;
using MosaicShell.Core.Capabilities.Platform;
using MosaicShell.Core.HostPlatform;
using MosaicShell.Core.Modules.Tessera;
using MosaicShell.Core.Styles;

namespace MosaicShell.Core.Tests;

public class TesseraOsAcrylicTrialPolicyTests : IDisposable
{
    public TesseraOsAcrylicTrialPolicyTests()
    {
        HostLaunchOptions.ResetForTests();
        HostLaunchOptions.Apply(Array.Empty<string>());
    }

    public void Dispose()
    {
        HostLaunchOptions.ResetForTests();
        HostLaunchOptions.Apply(Array.Empty<string>());
    }

    [Fact]
    public void Trial_is_available_but_default_off()
    {
        TesseraOsAcrylicTrialPolicy.Available.Should().BeTrue(
            "compile kill-switch stays on so --tessera-os-acrylic works without a rebuild");
        HostLaunchOptions.TesseraOsAcrylicTrial.Should().BeFalse();
        Win32HostCompositionPolicy.OsAcrylicTrialRequested.Should().BeFalse();
        Win32HostCompositionPolicy.WinUiCompositionBackdropCornerRadius.Should().BeNull(
            "process-wide radius is unset while the trial is off so alpha keeps Skia frost");
    }

    [Fact]
    public void Launch_flag_requests_trial_without_changing_ship_default()
    {
        HostLaunchOptions.Apply([HostLaunchOptions.TesseraOsAcrylicTrialFlag]);
        HostLaunchOptions.TesseraOsAcrylicTrial.Should().BeTrue();
        Win32HostCompositionPolicy.OsAcrylicTrialRequested.Should().BeTrue();
        Win32HostCompositionPolicy.WinUiCompositionBackdropCornerRadius
            .Should().Be(TesseraOsAcrylicTrialPolicy.SpikeCornerRadius);
    }

    [Fact]
    public void AngleEgl_is_pinned_ahead_of_software_regardless_of_trial()
    {
        Win32HostCompositionPolicy.PreferAngleEglRendering.Should().BeTrue();
        Win32HostCompositionPolicy.RenderingModeHints.Should().Equal("AngleEgl", "Software");
    }

    [Theory]
    [InlineData("1", true)]
    [InlineData("0", false)]
    public void Stacked_media_strip_is_out_of_spike(string showMediaStrip, bool stacked)
    {
        var payload = new Dictionary<string, string> { ["showMediaStrip"] = showMediaStrip };
        TesseraOsAcrylicTrialPolicy.IsStackedMultiPanel(payload).Should().Be(stacked);
    }

    [Fact]
    public void Eligible_only_when_flag_os_and_single_shell()
    {
        var single = new Dictionary<string, string> { ["showMediaStrip"] = "0", ["acrylic"] = "1" };
        var stacked = new Dictionary<string, string> { ["showMediaStrip"] = "1", ["acrylic"] = "1" };

        TesseraOsAcrylicTrialPolicy.IsEligible(single, trialRequested: false, osSupportsWinUiAcrylic: true)
            .Should().BeFalse();
        TesseraOsAcrylicTrialPolicy.IsEligible(single, trialRequested: true, osSupportsWinUiAcrylic: false)
            .Should().BeFalse("below WinUIComposition floor keeps frost; Win10 is best-effort only");
        TesseraOsAcrylicTrialPolicy.IsEligible(stacked, trialRequested: true, osSupportsWinUiAcrylic: true,
                styleId: StyleIds.Meter)
            .Should().BeFalse("Meter uses N-window split under H3");
        TesseraOsAcrylicTrialPolicy.IsEligible(stacked, trialRequested: true, osSupportsWinUiAcrylic: true,
                styleId: StyleIds.Fluent)
            .Should().BeTrue("Fluent keeps volume+media in one acrylic shell");
        TesseraOsAcrylicTrialPolicy.IsEligible(single, trialRequested: true, osSupportsWinUiAcrylic: true)
            .Should().BeTrue();
        TesseraOsAcrylicTrialPolicy.IsEligible(single, trialRequested: true, osSupportsWinUiAcrylic: true,
                compileAvailable: false)
            .Should().BeFalse("per-build kill-switch");
        TesseraOsAcrylicTrialPolicy.IsEligible(single, trialRequested: true, osSupportsWinUiAcrylic: true,
                osAcrylicRenderingAvailable: false)
            .Should().BeFalse("Software-only rendering keeps frost (H2 fallback row)");
    }

    [Fact]
    public void Is_eligible_from_payload_respects_force_software_render()
    {
        var single = new Dictionary<string, string> { ["showMediaStrip"] = "0", ["acrylic"] = "1" };
        HostLaunchOptions.Apply(
        [
            HostLaunchOptions.TesseraOsAcrylicTrialFlag,
            HostLaunchOptions.TesseraForceSoftwareRenderFlag
        ]);
        TesseraOsAcrylicTrialPolicy.IsEligibleFromPayload(single, StyleIds.Fluent).Should().BeFalse();
    }

    [Fact]
    public void Windows11_primary_target_is_build_22000()
    {
        TesseraOsAcrylicTrialPolicy.OsIsWindows11PrimaryTarget.Should().Be(
            OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000));
    }

    [Fact]
    public void Material_stays_transparent_frost_when_trial_off()
    {
        var m = TesseraFlyoutMaterialFactory.Create(useAcrylic: true);
        m.TransparencyHints.Should().Equal("Transparent");
        m.TransparencyHints.Should().NotContain("AcrylicBlur");
    }

    [Fact]
    public void Material_requests_acrylic_with_transparent_fallback_when_eligible()
    {
        var m = TesseraFlyoutMaterialFactory.Create(useAcrylic: true, osAcrylicEligible: true);
        m.UseSoftFrost.Should().BeTrue();
        m.TransparencyHints.Should().Equal("AcrylicBlur", "Transparent");
        TesseraFlyoutWindowPolicy.ResolveTransparencyHints(m).Should().Equal("AcrylicBlur", "Transparent");
    }

    [Fact]
    public void Material_does_not_request_acrylic_when_user_disables_frost()
    {
        var m = TesseraFlyoutMaterialFactory.Create(useAcrylic: false, osAcrylicEligible: true);
        m.TransparencyHints.Should().Equal("Transparent");
        m.TransparencyHints.Should().NotContain("AcrylicBlur");
    }

    [Fact]
    public void Hide_until_composition_ready_stays_on_during_trial()
    {
        TesseraFlyoutWindowPolicy.HideUntilCompositionReady.Should().BeTrue();
        TesseraFlyoutWindowPolicy.RevealMustBeGenerationGated.Should().BeTrue(
            "AcrylicBlur brush attach is a second async composition step; reuse SoftFrost settle");
    }

    [Fact]
    public void Live_mode_is_os_acrylic_only_when_eligible()
    {
        TesseraFlyoutGlassPolicy.ResolveLiveMode(softFrostHwndReady: true, useBackdropBlur: true)
            .Should().Be(TesseraFlyoutGlassMode.SkiaFallback);
        TesseraFlyoutGlassPolicy
            .ResolveLiveMode(softFrostHwndReady: true, useBackdropBlur: true, osAcrylicEligible: true)
            .Should().Be(TesseraFlyoutGlassMode.OsAcrylic);
        TesseraFlyoutGlassPolicy
            .ResolveLiveMode(softFrostHwndReady: false, useBackdropBlur: true, osAcrylicEligible: true)
            .Should().Be(TesseraFlyoutGlassMode.EmbeddedSimple);
    }

    [Fact]
    public void BitBlt_sampling_stays_forbidden_during_trial()
    {
        TesseraFlyoutGlassPolicy.ForbidLiveBackdropPixelSampling.Should().BeTrue();
        TesseraFlyoutGlassPolicy.ShouldAllowGdiScreenCapture(true, true).Should().BeFalse();
    }
}
