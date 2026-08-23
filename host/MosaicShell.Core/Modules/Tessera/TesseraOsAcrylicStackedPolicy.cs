using MosaicShell.Core.Capabilities.Platform;
using MosaicShell.Core.Styles;

namespace MosaicShell.Core.Modules.Tessera;

/// <summary>
/// H3 (a): per-panel FlyoutWindows for stacked Tessera shells under the OS acrylic trial.
/// Single-shell acrylic stays on <see cref="TesseraOsAcrylicTrialPolicy"/>; this policy
/// covers volume+media strip and (later) CoreUI multi-tile forks.
/// </summary>
public enum TesseraStackedPanelRole
{
    Volume,
    Media,
    /// <summary>CoreUI device tile (phase 2).</summary>
    Device,
}

public static class TesseraOsAcrylicStackedPolicy
{
    /// <summary>Compile gate for Host N-window presenter work (H3 phase 1+).</summary>
    public const bool Available = true;

    /// <summary>CoreUI N-window fork ships after volume+media strip is signed off.</summary>
    public const bool CoreUiMultiWindowEnabled = false;

    public const string VolumeSlotSuffix = "vol";
    public const string MediaSlotSuffix = "media";
    public const string DeviceSlotSuffix = "device";

    /// <summary>Default corner radius when style/role has no dedicated pill metric.</summary>
    public const float PanelCornerRadius = TesseraOsAcrylicTrialPolicy.SpikeCornerRadius;

    /// <summary>
    /// Win32 region clip radius for stacked OS acrylic HWNDs. Caps design radius at half the
    /// shorter client edge so capsules (GNOME pill, Meter spine) do not leak square corners.
    /// </summary>
    public static float ResolvePanelCornerRadiusDip(
        string? styleId,
        TesseraStackedPanelRole role,
        double widthDip,
        double heightDip)
    {
        var id = StyleIds.Normalize(styleId ?? StyleIds.Fluent);
        var cap = (float)Math.Min(Math.Max(1, widthDip), Math.Max(1, heightDip)) / 2f;

        var design = (id, role) switch
        {
            (StyleIds.Meter, TesseraStackedPanelRole.Volume) =>
                TesseraStackedPlacementSpec.MeterVolumeCornerRadiusDip,
            (StyleIds.Meter, TesseraStackedPanelRole.Media) =>
                TesseraStackedPlacementSpec.MeterMediaCornerRadiusDip,
            (StyleIds.Gnome, TesseraStackedPanelRole.Volume) =>
                TesseraStackedPlacementSpec.GnomePillCornerRadiusDip,
            (StyleIds.Gnome, TesseraStackedPanelRole.Media) =>
                TesseraStackedPlacementSpec.GnomePillCornerRadiusDip,
            (StyleIds.Compact, _) => PanelCornerRadius,
            (StyleIds.ModernFlyouts, _) => PanelCornerRadius,
            _ => PanelCornerRadius,
        };

        return Math.Min(design, cap);
    }

    /// <summary>
    /// Stacked acrylic uses the same trial flag as single-shell. Frost remains alpha default.
    /// </summary>
    public static bool TrialRequested =>
        TesseraOsAcrylicTrialPolicy.Available && HostLaunchOptions.TesseraOsAcrylicTrial;

    public static bool IsVolumeMediaStripStacked(IReadOnlyDictionary<string, string>? payload) =>
        TesseraOsAcrylicTrialPolicy.IsStackedMultiPanel(payload);

    public static bool IsCoreUiMultiTile(string? styleId) =>
        CoreUiMultiWindowEnabled
        && styleId is not null
        && StyleIds.Normalize(styleId).Equals(StyleIds.CoreUI, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// True when Host should present N FlyoutWindows instead of one frost shell.
    /// Mutually exclusive with <see cref="TesseraOsAcrylicTrialPolicy.IsEligible"/>.
    /// </summary>
    public static bool UseMultiWindowFromPayload(
        IReadOnlyDictionary<string, string>? payload,
        string? styleId = null) =>
        UseMultiWindow(
            payload,
            styleId,
            trialRequested: HostLaunchOptions.TesseraOsAcrylicTrial,
            osSupportsWinUiAcrylic: TesseraOsAcrylicTrialPolicy.OsSupportsWinUiAcrylic,
            osAcrylicRenderingAvailable: !HostLaunchOptions.TesseraForceSoftwareRender);

    public static bool UseMultiWindow(
        IReadOnlyDictionary<string, string>? payload,
        string? styleId,
        bool trialRequested,
        bool osSupportsWinUiAcrylic,
        bool osAcrylicRenderingAvailable = true,
        bool compileAvailable = Available)
    {
        if (!compileAvailable || !trialRequested || !osSupportsWinUiAcrylic || !osAcrylicRenderingAvailable)
            return false;

        if (!TesseraFlyoutMaterialFactory.UseAcrylicFromPayload(payload))
            return false;

        if (!IsVolumeMediaStripStacked(payload))
            return false;

        return TesseraStackedPlacementPolicy.SupportsStackedOsAcrylic(styleId);
    }

    /// <summary>Logical panels for volume+media strip (phase 1).</summary>
    public static IReadOnlyList<TesseraStackedPanelRole> ResolveVolumeMediaPanels(string? styleId)
    {
        _ = styleId;
        return [TesseraStackedPanelRole.Volume, TesseraStackedPanelRole.Media];
    }

    /// <summary>Logical panels for CoreUI (phase 2).</summary>
    public static IReadOnlyList<TesseraStackedPanelRole> ResolveCoreUiPanels() =>
        [TesseraStackedPanelRole.Device, TesseraStackedPanelRole.Volume, TesseraStackedPanelRole.Media];

    public static IReadOnlyList<TesseraStackedPanelRole> ResolvePanels(
        IReadOnlyDictionary<string, string>? payload,
        string? styleId)
    {
        if (IsCoreUiMultiTile(styleId))
            return ResolveCoreUiPanels();

        if (IsVolumeMediaStripStacked(payload))
            return ResolveVolumeMediaPanels(styleId);

        return Array.Empty<TesseraStackedPanelRole>();
    }

    public static string SlotSuffix(TesseraStackedPanelRole role) => role switch
    {
        TesseraStackedPanelRole.Volume => VolumeSlotSuffix,
        TesseraStackedPanelRole.Media => MediaSlotSuffix,
        TesseraStackedPanelRole.Device => DeviceSlotSuffix,
        _ => throw new ArgumentOutOfRangeException(nameof(role)),
    };

    /// <summary>Host flyout dictionary key: <c>Tessera:vol</c>, <c>Tessera:media</c>, …</summary>
    public static string WindowSlotKey(string moduleId, TesseraStackedPanelRole role)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleId);
        return $"{moduleId}:{SlotSuffix(role)}";
    }

    /// <summary>
    /// Win32 Z-order rank (higher = closer to user). FocusDim is below all flyout slots.
    /// Volume is topmost so the slider thumb wins overlapping edge cases.
    /// </summary>
    public static int ZOrderRank(TesseraStackedPanelRole role) => role switch
    {
        TesseraStackedPanelRole.Media => 1,
        TesseraStackedPanelRole.Device => 1,
        TesseraStackedPanelRole.Volume => 2,
        _ => 0,
    };

    /// <summary>Volume panel owns TesseraLiveHost and the live pump.</summary>
    public static TesseraStackedPanelRole LiveHostOwner => TesseraStackedPanelRole.Volume;

    public static bool RoleOwnsLiveHost(TesseraStackedPanelRole role) =>
        role == LiveHostOwner;

    /// <summary>Outside-click dismiss must union bounds from every visible slot.</summary>
    public const bool OutsideClickUsesUnionBounds = true;

    /// <summary>Transient dismiss / Hide must affect every slot in the session.</summary>
    public const bool TransientDismissMustHideAllSlots = true;

    /// <summary>Each slot reuses its own registered HWND (SoftFrost overlap rule).</summary>
    public const bool MustReuseRegisteredFlyoutHwndPerSlot = true;

    /// <summary>Present restacks every slot above FocusDim after layout.</summary>
    public const bool PresentMustRestackAllSlots = true;

    /// <summary>
    /// Meter volume spine is 28 DIP. Stacked cluster relayout must not use the 40 DIP
    /// single-flyout width gate or the volume HWND never receives cluster placement.
    /// </summary>
    public const double ClusterRelayoutMinWidthDip = 1;
}
