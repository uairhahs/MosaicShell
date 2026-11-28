using FluentAssertions;
using MosaicShell.Core.Capabilities.Platform;
using MosaicShell.Core.HostPlatform;
using MosaicShell.Core.Modules.Tessera;
using MosaicShell.Core.Runtime;
using MosaicShell.Core.Settings;
using MosaicShell.Core.Styles;

namespace MosaicShell.Core.Tests
{
    public class TesseraNativeTranslucencyPolicyTests : IDisposable
    {
        private readonly string _root;

        public TesseraNativeTranslucencyPolicyTests()
        {
            _root = Path.Combine(Path.GetTempPath(), "MosaicNativeTranslucencyTrial_" + Guid.NewGuid().ToString("N"));
            AppPaths.SetRootOverride(_root);
            AppPaths.EnsureLayout();
            ModuleSettingsStore.Save("Tessera", new TesseraSettings());
            HostLaunchOptions.ResetForTests();
            HostLaunchOptions.Apply([]);
        }

        public void Dispose()
        {
            HostLaunchOptions.ResetForTests();
            HostLaunchOptions.Apply([]);
            AppPaths.ClearRootOverride();
            try { Directory.Delete(_root, true); } catch { /* ignore */ }
        }

        [Fact]
        public void Solid_never_asks_for_the_OS_blur()
        {
            ModuleSettingsStore.Save("Tessera", new TesseraSettings { UseAcrylicBackdrop = false });

            _ = TesseraNativeTranslucencyPolicy.Available.Should().BeTrue(
                "compile kill-switch stays on so --tessera-native-translucency works without a rebuild");
            _ = HostLaunchOptions.TesseraNativeTranslucencyTrial.Should().BeFalse();
            _ = TesseraNativeTranslucencyPolicy.IsTrialRequested().Should().BeFalse();
            _ = Win32HostCompositionPolicy.NativeTranslucencyTrialRequested.Should().BeFalse();
            _ = Win32HostCompositionPolicy.WinUiCompositionBackdropCornerRadius.Should().BeNull(
                "process-wide radius is unset without the blur, so a Solid card keeps square HWND corners");
        }

        // Frosted is the blur with the frosted glass over it wherever there is a blur; Windows has one.
        [WindowsFact]
        public void Frosted_asks_for_the_blur_on_Windows()
        {
            ModuleSettingsStore.Save("Tessera", new TesseraSettings { UseAcrylicBackdrop = true, UseNativeTranslucency = false });

            _ = TesseraNativeTranslucencyPolicy.IsTrialRequested().Should().BeTrue();
            _ = Win32HostCompositionPolicy.WinUiCompositionBackdropCornerRadius
                .Should().Be(TesseraNativeTranslucencyPolicy.SpikeCornerRadius);
        }

        // Windows' sign-off governs only on Windows; KDE's is pending (see the sign-off tests).
        [WindowsFact]
        public void Persisted_hub_setting_enables_trial_when_win11_eval_is_signed_off()
        {
            ModuleSettingsStore.Save("Tessera", new TesseraSettings { UseNativeTranslucency = true });
            _ = TesseraNativeTranslucencyPolicy.IsTrialRequested().Should().BeTrue();
            _ = Win32HostCompositionPolicy.NativeTranslucencyTrialRequested.Should().BeTrue();
        }

        [Theory]
        [InlineData(true, false, true, true)]
        [InlineData(true, true, false, false)]
        [InlineData(false, false, true, false)]
        [InlineData(false, true, false, true)]
        public void After_sign_off_the_hub_setting_decides_and_before_it_only_the_flag(
            bool evalComplete, bool launchFlag, bool persisted, bool expected)
        {
            _ = TesseraNativeTranslucencyPolicy
                .ResolveTrialRequested(launchFlag, persisted, evalComplete)
                .Should().Be(expected);
        }

        // Windows' sign-off governs only on Windows; KDE's is pending (see the sign-off tests).
        [WindowsFact]
        public void Launch_flag_is_ignored_after_win11_sign_off_until_hub_setting_is_on()
        {
            ModuleSettingsStore.Save("Tessera", new TesseraSettings { UseAcrylicBackdrop = false });
            HostLaunchOptions.Apply([HostLaunchOptions.TesseraNativeTranslucencyTrialFlag]);
            _ = HostLaunchOptions.TesseraNativeTranslucencyTrial.Should().BeTrue();
            _ = TesseraNativeTranslucencyPolicy.IsTrialRequested().Should().BeFalse(
                "post sign-off the hub setting is authoritative; CLI flag alone must not enable acrylic");
            _ = Win32HostCompositionPolicy.NativeTranslucencyTrialRequested.Should().BeFalse();

            ModuleSettingsStore.Save("Tessera", new TesseraSettings { UseAcrylicBackdrop = true, UseNativeTranslucency = true });
            _ = TesseraNativeTranslucencyPolicy.IsTrialRequested().Should().BeTrue();
            _ = Win32HostCompositionPolicy.NativeTranslucencyTrialRequested.Should().BeTrue();
            _ = Win32HostCompositionPolicy.WinUiCompositionBackdropCornerRadius
                .Should().Be(TesseraNativeTranslucencyPolicy.SpikeCornerRadius);
        }

        // Windows' sign-off governs only on Windows; KDE's is pending (see the sign-off tests).
        [WindowsFact]
        public void Solid_disables_the_blur_even_when_launch_flag_is_on()
        {
            ModuleSettingsStore.Save("Tessera", new TesseraSettings { UseAcrylicBackdrop = false, UseNativeTranslucency = false });
            HostLaunchOptions.Apply([HostLaunchOptions.TesseraNativeTranslucencyTrialFlag]);
            _ = TesseraNativeTranslucencyPolicy.IsTrialRequested().Should().BeFalse();
            _ = Win32HostCompositionPolicy.NativeTranslucencyTrialRequested.Should().BeFalse();
        }

        [Fact]
        public void AngleEgl_is_pinned_ahead_of_software_regardless_of_trial()
        {
            _ = Win32HostCompositionPolicy.PreferAngleEglRendering.Should().BeTrue();
            _ = Win32HostCompositionPolicy.RenderingModeHints.Should().Equal("AngleEgl", "Software");
        }

        [Theory]
        [InlineData("1", true)]
        [InlineData("0", false)]
        public void Stacked_media_strip_is_out_of_spike(string showMediaStrip, bool stacked)
        {
            Dictionary<string, string> payload = new() { ["showMediaStrip"] = showMediaStrip };
            _ = TesseraNativeTranslucencyPolicy.IsStackedMultiPanel(payload).Should().Be(stacked);
        }

        [Fact]
        public void Eligible_only_when_flag_os_and_single_shell()
        {
            Dictionary<string, string> single = new() { ["showMediaStrip"] = "0", ["acrylic"] = "1" };
            Dictionary<string, string> stacked = new() { ["showMediaStrip"] = "1", ["acrylic"] = "1" };

            _ = TesseraNativeTranslucencyPolicy.IsEligible(single, trialRequested: false, platformSupported: true)
                .Should().BeFalse();
            _ = TesseraNativeTranslucencyPolicy.IsEligible(single, trialRequested: true, platformSupported: false)
                .Should().BeFalse("below WinUIComposition floor keeps frost; Win10 is best-effort only");
            _ = TesseraNativeTranslucencyPolicy.IsEligible(stacked, trialRequested: true, platformSupported: true,
                    styleId: StyleIds.Meter)
                .Should().BeFalse("Meter uses N-window split under H3");
            _ = TesseraNativeTranslucencyPolicy.IsEligible(stacked, trialRequested: true, platformSupported: true,
                    styleId: StyleIds.Fluent)
                .Should().BeTrue("Fluent keeps volume+media in one acrylic shell");
            _ = TesseraNativeTranslucencyPolicy.IsEligible(single, trialRequested: true, platformSupported: true)
                .Should().BeTrue();
            _ = TesseraNativeTranslucencyPolicy.IsEligible(single, trialRequested: true, platformSupported: true,
                    compileAvailable: false)
                .Should().BeFalse("per-build kill-switch");
            _ = TesseraNativeTranslucencyPolicy.IsEligible(single, trialRequested: true, platformSupported: true,
                    nativeTranslucencyRenderingAvailable: false)
                .Should().BeFalse("Software-only rendering keeps frost (H2 fallback row)");
        }

        [Fact]
        public void Is_eligible_from_payload_respects_force_software_render()
        {
            Dictionary<string, string> single = new() { ["showMediaStrip"] = "0", ["acrylic"] = "1" };
            TesseraNativeTranslucencyTestHarness.EnableHubNativeTranslucency();
            HostLaunchOptions.Apply([HostLaunchOptions.TesseraForceSoftwareRenderFlag]);
            _ = TesseraNativeTranslucencyPolicy.IsEligibleFromPayload(single, StyleIds.Fluent).Should().BeFalse();
        }

        [Fact]
        public void Material_stays_transparent_frost_when_trial_off()
        {
            TesseraFlyoutMaterial m = TesseraFlyoutMaterialFactory.Create(useAcrylic: true);
            _ = m.TransparencyHints.Should().Equal("Transparent");
            _ = m.TransparencyHints.Should().NotContain("AcrylicBlur");
        }

        [Fact]
        public void Material_requests_acrylic_with_transparent_fallback_when_eligible()
        {
            TesseraFlyoutMaterial m = TesseraFlyoutMaterialFactory.Create(
                useAcrylic: true, nativeTranslucencyEligible: true, NativeTranslucencyBackend.WindowsAcrylic);
            _ = m.UseSoftFrost.Should().BeTrue();
            _ = m.TransparencyHints.Should().Equal("AcrylicBlur", "Transparent");
            _ = TesseraFlyoutWindowPolicy.ResolveTransparencyHints(m).Should().Equal("AcrylicBlur", "Transparent");
        }

        [Fact]
        public void Material_does_not_request_acrylic_when_user_disables_frost()
        {
            TesseraFlyoutMaterial m = TesseraFlyoutMaterialFactory.Create(useAcrylic: false, nativeTranslucencyEligible: true);
            _ = m.TransparencyHints.Should().Equal("Transparent");
            _ = m.TransparencyHints.Should().NotContain("AcrylicBlur");
        }

        [Fact]
        public void Hide_until_composition_ready_stays_on_during_trial()
        {
            _ = TesseraFlyoutWindowPolicy.HideUntilCompositionReady.Should().BeTrue();
            _ = TesseraFlyoutWindowPolicy.RevealMustBeGenerationGated.Should().BeTrue(
                "AcrylicBlur brush attach is a second async composition step; reuse SoftFrost settle");
        }

        [Fact]
        public void Live_mode_is_native_translucency_only_when_eligible()
        {
            _ = TesseraFlyoutGlassPolicy.ResolveLiveMode(softFrostHwndReady: true, useBackdropBlur: true)
                .Should().Be(TesseraFlyoutGlassMode.SkiaFallback);
            _ = TesseraFlyoutGlassPolicy
                .ResolveLiveMode(softFrostHwndReady: true, useBackdropBlur: true, nativeTranslucencyEligible: true)
                .Should().Be(TesseraFlyoutGlassMode.NativeTranslucency);
            _ = TesseraFlyoutGlassPolicy
                .ResolveLiveMode(softFrostHwndReady: false, useBackdropBlur: true, nativeTranslucencyEligible: true)
                .Should().Be(TesseraFlyoutGlassMode.EmbeddedSimple);
        }

        [Fact]
        public void BitBlt_sampling_stays_forbidden_during_trial()
        {
            _ = TesseraFlyoutGlassPolicy.ForbidLiveBackdropPixelSampling.Should().BeTrue();
            _ = TesseraFlyoutGlassPolicy.ShouldAllowGdiScreenCapture(true, true).Should().BeFalse();
        }
    }
}
