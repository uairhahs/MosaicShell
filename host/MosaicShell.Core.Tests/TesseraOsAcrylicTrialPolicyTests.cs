using FluentAssertions;
using MosaicShell.Core;
using MosaicShell.Core.Capabilities.Platform;
using MosaicShell.Core.HostPlatform;
using MosaicShell.Core.Modules.Tessera;
using MosaicShell.Core.Runtime;
using MosaicShell.Core.Settings;
using MosaicShell.Core.Styles;

namespace MosaicShell.Core.Tests;

public class TesseraOsAcrylicTrialPolicyTests : IDisposable
{
    private readonly string _root;

    public TesseraOsAcrylicTrialPolicyTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "MosaicOsAcrylicTrial_" + Guid.NewGuid().ToString("N"));
        AppPaths.SetRootOverride(_root);
        AppPaths.EnsureLayout();
        ModuleSettingsStore.Save("Tessera", new TesseraSettings());
        HostLaunchOptions.ResetForTests();
        HostLaunchOptions.Apply(Array.Empty<string>());
    }

    public void Dispose()
    {
        HostLaunchOptions.ResetForTests();
        HostLaunchOptions.Apply(Array.Empty<string>());
        AppPaths.ClearRootOverride();
        try { Directory.Delete(_root, true); } catch { /* ignore */ }
    }

    [Fact]
    public void Trial_is_available_but_default_off()
    {
        TesseraOsAcrylicTrialPolicy.Available.Should().BeTrue(
            "compile kill-switch stays on so --tessera-os-acrylic works without a rebuild");
        HostLaunchOptions.TesseraOsAcrylicTrial.Should().BeFalse();
        TesseraOsAcrylicTrialPolicy.IsTrialRequested().Should().BeFalse();
        Win32HostCompositionPolicy.OsAcrylicTrialRequested.Should().BeFalse();
        Win32HostCompositionPolicy.WinUiCompositionBackdropCornerRadius.Should().BeNull(
            "process-wide radius is unset while the trial is off so alpha keeps Skia frost");
    }

    [Fact]
    public void Persisted_hub_setting_enables_trial_when_win11_eval_is_signed_off()
    {
        ModuleSettingsStore.Save("Tessera", new TesseraSettings { UseOsAcrylic = true });
        TesseraOsAcrylicTrialPolicy.IsTrialRequested().Should().BeTrue();
        Win32HostCompositionPolicy.OsAcrylicTrialRequested.Should().BeTrue();
    }

    [Fact]
    public void Persisted_hub_setting_is_ignored_without_win11_eval_sign_off()
    {
        TesseraOsAcrylicTrialPolicy
            .ResolveTrialRequested(launchFlag: false, persistedUseOsAcrylic: true)
            .Should().Be(TesseraOsAcrylicSignOffPolicy.Win11EvalComplete);
        TesseraOsAcrylicTrialPolicy
            .ResolveTrialRequested(launchFlag: true, persistedUseOsAcrylic: false)
            .Should().Be(!TesseraOsAcrylicSignOffPolicy.Win11EvalComplete);
    }

    [Fact]
    public void Launch_flag_is_ignored_after_win11_sign_off_until_hub_setting_is_on()
    {
        HostLaunchOptions.Apply([HostLaunchOptions.TesseraOsAcrylicTrialFlag]);
        HostLaunchOptions.TesseraOsAcrylicTrial.Should().BeTrue();
        TesseraOsAcrylicTrialPolicy.IsTrialRequested().Should().BeFalse(
            "post sign-off the hub setting is authoritative; CLI flag alone must not enable acrylic");
        Win32HostCompositionPolicy.OsAcrylicTrialRequested.Should().BeFalse();

        ModuleSettingsStore.Save("Tessera", new TesseraSettings { UseOsAcrylic = true });
        TesseraOsAcrylicTrialPolicy.IsTrialRequested().Should().BeTrue();
        Win32HostCompositionPolicy.OsAcrylicTrialRequested.Should().BeTrue();
        Win32HostCompositionPolicy.WinUiCompositionBackdropCornerRadius
            .Should().Be(TesseraOsAcrylicTrialPolicy.SpikeCornerRadius);
    }

    [Fact]
    public void Hub_setting_off_disables_acrylic_even_when_launch_flag_is_on()
    {
        ModuleSettingsStore.Save("Tessera", new TesseraSettings { UseOsAcrylic = false });
        HostLaunchOptions.Apply([HostLaunchOptions.TesseraOsAcrylicTrialFlag]);
        TesseraOsAcrylicTrialPolicy.IsTrialRequested().Should().BeFalse();
        Win32HostCompositionPolicy.OsAcrylicTrialRequested.Should().BeFalse();
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
        TesseraOsAcrylicTestHarness.EnableHubOsAcrylic();
        HostLaunchOptions.Apply([HostLaunchOptions.TesseraForceSoftwareRenderFlag]);
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
