using MosaicShell.Core.Capabilities.Platform;

namespace MosaicShell.Core.Modules.Tessera;

/// <summary>
/// Default-off OS AcrylicBlur spike for single-shell Tessera flyouts (H1).
/// Alpha ships Skia frost; enable with <see cref="HostLaunchOptions.TesseraOsAcrylicTrialFlag"/>.
/// Stacked volume+media uses N FlyoutWindows under H3; see TesseraOsAcrylicStackedPolicy.
/// Windows 11 is the supported evaluation and ship target. Windows 10 may pass the
/// technical gate below but is best-effort only; MosaicShell makes no Win10 acrylic promises.
/// </summary>
public static class TesseraOsAcrylicTrialPolicy
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

    /// <summary>True when the OS reports Windows 11+ (22000). H2 sign-off target.</summary>
    public static bool OsIsWindows11PrimaryTarget =>
        OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000);

    public static bool IsStackedMultiPanel(IReadOnlyDictionary<string, string>? payload)
    {
        if (payload is null)
            return false;
        if (!payload.TryGetValue("showMediaStrip", out var raw))
            return false;
        return raw is "1" or "true" or "True" or "on" or "On";
    }

    public static bool IsEligible(
        IReadOnlyDictionary<string, string>? payload,
        bool trialRequested,
        bool osSupportsWinUiAcrylic,
        bool compileAvailable = Available,
        bool osAcrylicRenderingAvailable = true,
        string? styleId = null)
    {
        if (!compileAvailable || !trialRequested || !osSupportsWinUiAcrylic || !osAcrylicRenderingAvailable)
            return false;

        if (IsStackedMultiPanel(payload)
            && styleId is not null
            && TesseraStackedPlacementPolicy.SupportsStackedOsAcrylic(styleId))
            return false;

        return TesseraFlyoutMaterialFactory.UseAcrylicFromPayload(payload);
    }

    public static bool IsEligibleFromPayload(
        IReadOnlyDictionary<string, string>? payload,
        string? styleId = null) =>
        IsEligible(
            payload,
            HostLaunchOptions.TesseraOsAcrylicTrial,
            OsSupportsWinUiAcrylic,
            osAcrylicRenderingAvailable: !HostLaunchOptions.TesseraForceSoftwareRender,
            styleId: styleId);
}
