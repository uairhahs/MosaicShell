using MosaicShell.Core.Capabilities.Platform;

namespace MosaicShell.Core.Modules.Tessera
{
    /// <summary>
    /// Default-off OS AcrylicBlur spike for single-shell Tessera flyouts (H1).
    /// Alpha ships Skia frost; enable with hub <c>UseNativeTranslucency</c> (when signed off) or
    /// <see cref="HostLaunchOptions.TesseraNativeTranslucencyTrialFlag"/>.
    /// Stacked volume+media uses N FlyoutWindows under H3; see TesseraNativeTranslucencyStackedPolicy.
    /// Windows 11 is the supported evaluation and ship target. Windows 10 may pass the
    /// technical gate below but is best-effort only; MosaicShell makes no Win10 acrylic promises.
    /// </summary>
    public static class TesseraNativeTranslucencyPolicy
    {
        /// <summary>Compile kill-switch. Hotfix can set false without removing the flag parser.</summary>
        public const bool Available = true;

        /// <summary>Fluent / Windows11 kit corner radius; process-wide when trial is on.</summary>
        public const float SpikeCornerRadius = 12f;

        /// <summary>
        /// Technical floor for WinUIComposition acrylic (Windows 10 build 17134+).
        /// Passing this gate does not imply Win10 support; see class summary.
        /// </summary>
        public static bool OsSupportsWinUiAcrylic =>
            OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17134);

        public static bool IsStackedMultiPanel(IReadOnlyDictionary<string, string>? payload)
        {
            return payload is not null && payload.TryGetValue("showMediaStrip", out string? raw) && raw is "1" or "true" or "True" or "on" or "On";
        }

        public static bool IsEligible(
            IReadOnlyDictionary<string, string>? payload,
            bool trialRequested,
            bool platformSupported,
            bool compileAvailable = Available,
            bool nativeTranslucencyRenderingAvailable = true,
            string? styleId = null)
        {
            // A style whose spec calls for matte-only chrome (MaterialYou, PlainText) must never
            // be eligible for any window-level transparency, Native Translucency included - the unpainted
            // window area around its shape has no local background to fall back on if the OS
            // composite silently fails, unlike every other style.
            return !TesseraFlyoutTweenTargetCatalog.ResolveProfile(styleId).RequiresMatteChrome
                && compileAvailable && trialRequested && platformSupported && nativeTranslucencyRenderingAvailable && (!IsStackedMultiPanel(payload)
                || styleId is null
                || !TesseraStackedPlacementPolicy.SupportsStackedNativeTranslucency(styleId)) && TesseraFlyoutMaterialFactory.UseAcrylicFromPayload(payload);
        }

        /// <summary>
        /// After Win11 eval sign-off, persisted hub <c>UseNativeTranslucency</c> is the sole opt-in.
        /// Before sign-off, only the launch flag applies (H2 eval).
        /// </summary>
        public static bool ResolveTrialRequested(bool launchFlag, bool persistedUseNativeTranslucency)
        {
            return Available && (TesseraNativeTranslucencySignOffPolicy.Win11EvalComplete
                    ? persistedUseNativeTranslucency
                    : launchFlag);
        }

        /// <summary>Effective trial opt-in for this process (CLI flag or saved Tessera setting).</summary>
        public static bool IsTrialRequested()
        {
            return ResolveTrialRequested(
                HostLaunchOptions.TesseraNativeTranslucencyTrial,
                TesseraFlyoutRequestBuilder.LoadSettings().UseNativeTranslucency);
        }

        public static bool IsEligibleFromPayload(
            IReadOnlyDictionary<string, string>? payload,
            string? styleId = null)
        {
            return IsEligible(
                payload,
                IsTrialRequested(),
                NativeTranslucencyPlatform.IsAvailable,
                nativeTranslucencyRenderingAvailable: !HostLaunchOptions.TesseraForceSoftwareRender,
                styleId: styleId);
        }
    }
}
