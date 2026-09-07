using MosaicShell.Core.Capabilities.Platform;
using MosaicShell.Core.Styles;

namespace MosaicShell.Core.Modules.Tessera
{
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
            string id = StyleIds.Normalize(styleId ?? StyleIds.Fluent);
            float cap = (float)Math.Min(Math.Max(1, widthDip), Math.Max(1, heightDip)) / 2f;

            float design = (id, role) switch
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
            TesseraOsAcrylicTrialPolicy.Available && TesseraOsAcrylicTrialPolicy.IsTrialRequested();

        public static bool IsVolumeMediaStripStacked(IReadOnlyDictionary<string, string>? payload)
        {
            return TesseraOsAcrylicTrialPolicy.IsStackedMultiPanel(payload);
        }

        // styleId is unused only while CoreUiMultiWindowEnabled is false; it's needed again as
        // soon as that flag flips on.
#pragma warning disable IDE0060
        public static bool IsCoreUiMultiTile(string? styleId)
#pragma warning restore IDE0060
        {
            return CoreUiMultiWindowEnabled
            && styleId is not null
            && StyleIds.Normalize(styleId).Equals(StyleIds.CoreUI, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// True when Host should present N FlyoutWindows instead of one frost shell.
        /// Mutually exclusive with <see cref="TesseraOsAcrylicTrialPolicy.IsEligible"/>.
        /// </summary>
        public static bool UseMultiWindowFromPayload(
            IReadOnlyDictionary<string, string>? payload,
            string? styleId = null,
            string? kind = null)
        {
            return UseMultiWindow(
                payload,
                styleId,
                trialRequested: TesseraOsAcrylicTrialPolicy.IsTrialRequested(),
                osSupportsWinUiAcrylic: TesseraOsAcrylicTrialPolicy.OsSupportsWinUiAcrylic,
                osAcrylicRenderingAvailable: !HostLaunchOptions.TesseraForceSoftwareRender,
                kind: kind);
        }

        public static bool UseMultiWindow(
            IReadOnlyDictionary<string, string>? payload,
            string? styleId,
            bool trialRequested,
            bool osSupportsWinUiAcrylic,
            bool osAcrylicRenderingAvailable = true,
            bool compileAvailable = Available,
            string? kind = null)
        {
            return !TesseraStatusFlyoutPolicy.MustUseDedicatedSingleWindow(kind)
                && compileAvailable && trialRequested && osSupportsWinUiAcrylic && osAcrylicRenderingAvailable
                && TesseraFlyoutMaterialFactory.UseAcrylicFromPayload(payload)
                && IsVolumeMediaStripStacked(payload)
                && TesseraStackedPlacementPolicy.SupportsStackedOsAcrylic(styleId);
        }

        /// <summary>Logical panels for volume+media strip (phase 1).</summary>
        public static IReadOnlyList<TesseraStackedPanelRole> ResolveVolumeMediaPanels(string? styleId)
        {
            _ = styleId;
            return [TesseraStackedPanelRole.Volume, TesseraStackedPanelRole.Media];
        }

        /// <summary>Logical panels for CoreUI (phase 2).</summary>
        public static IReadOnlyList<TesseraStackedPanelRole> ResolveCoreUiPanels()
        {
            return [TesseraStackedPanelRole.Device, TesseraStackedPanelRole.Volume, TesseraStackedPanelRole.Media];
        }

        public static IReadOnlyList<TesseraStackedPanelRole> ResolvePanels(
            IReadOnlyDictionary<string, string>? payload,
            string? styleId)
        {
            return IsCoreUiMultiTile(styleId)
                ? ResolveCoreUiPanels()
                : IsVolumeMediaStripStacked(payload) ? ResolveVolumeMediaPanels(styleId) : [];
        }

        public static string SlotSuffix(TesseraStackedPanelRole role)
        {
            return role switch
            {
                TesseraStackedPanelRole.Volume => VolumeSlotSuffix,
                TesseraStackedPanelRole.Media => MediaSlotSuffix,
                TesseraStackedPanelRole.Device => DeviceSlotSuffix,
                _ => throw new ArgumentOutOfRangeException(nameof(role)),
            };
        }

        /// <summary>Host flyout dictionary key: <c>Tessera:vol</c>, <c>Tessera:media</c>, …</summary>
        public static string WindowSlotKey(string moduleId, TesseraStackedPanelRole role)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(moduleId);
            return $"{moduleId}:{SlotSuffix(role)}";
        }

        /// <summary>
        /// Per-HWND <c>TransientDismissed</c> key. Stacked slots must notify with the dictionary
        /// slot key so cascade matches live keys; CapsLock still notifies as <c>Tessera</c>.
        /// </summary>
        public static string ResolveTransientDismissNotifyKey(
            string moduleId,
            TesseraStackedPanelRole? role)
        {
            return role is { } r ? WindowSlotKey(moduleId, r) : moduleId;
        }

        /// <summary>
        /// Presenter consumers (capability sessions) subscribe by canonical module id.
        /// Strip a stacked slot suffix so <c>Tessera:vol</c> still notifies Tessera.
        /// </summary>
        public static string ResolveTransientDismissConsumerKey(string notifyKey)
        {
            if (string.IsNullOrWhiteSpace(notifyKey))
            {
                return notifyKey;
            }

            ReadOnlySpan<string> suffixes = [VolumeSlotSuffix, MediaSlotSuffix, DeviceSlotSuffix];
            for (int i = 0; i < suffixes.Length; i++)
            {
                string tail = ":" + suffixes[i];
                if (notifyKey.EndsWith(tail, StringComparison.OrdinalIgnoreCase))
                {
                    return notifyKey[..^tail.Length];
                }
            }

            return notifyKey;
        }

        /// <summary>
        /// Win32 Z-order rank (higher = closer to user). FocusDim is below all flyout slots.
        /// Volume is topmost so the slider thumb wins overlapping edge cases.
        /// </summary>
        public static int ZOrderRank(TesseraStackedPanelRole role)
        {
            return role switch
            {
                TesseraStackedPanelRole.Media => 1,
                TesseraStackedPanelRole.Device => 1,
                TesseraStackedPanelRole.Volume => 2,
                _ => 0,
            };
        }

        /// <summary>Volume panel owns TesseraLiveHost and the live pump.</summary>
        public static TesseraStackedPanelRole LiveHostOwner => TesseraStackedPanelRole.Volume;

        public static bool RoleOwnsLiveHost(TesseraStackedPanelRole role)
        {
            return role == LiveHostOwner;
        }

        /// <summary>Outside-click dismiss must union bounds from every visible slot.</summary>
        public const bool OutsideClickUsesUnionBounds = true;

        /// <summary>Transient dismiss / Hide must affect every slot in the session.</summary>
        public const bool TransientDismissMustHideAllSlots = true;

        /// <summary>
        /// Caps/airplane use module key <c>Tessera</c>. When Host swaps that HWND for stacked
        /// <c>Tessera:vol</c>/<c>Tessera:media</c>, it must cancel the status auto-dismiss and
        /// detach <c>TransientDismissed</c> before Close. A late Tick otherwise cascades into
        /// the live volume session via <see cref="ShouldCascadeTransientDismissToStackedSession"/>.
        /// </summary>
        public const bool SupersededSingleHwndMustCancelDismissBeforeClose = true;

        /// <summary>
        /// Cascade only when the notifying key is a live stacked slot. A superseded single-shell
        /// status dismiss (<c>Tessera</c>) must not hide volume/media.
        /// </summary>
        public static bool ShouldCascadeTransientDismissToStackedSession(
            string? dismissedKey,
            IReadOnlyList<string> liveSlotKeys)
        {
            if (!TransientDismissMustHideAllSlots
                || string.IsNullOrWhiteSpace(dismissedKey)
                || liveSlotKeys.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < liveSlotKeys.Count; i++)
            {
                if (string.Equals(liveSlotKeys[i], dismissedKey, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

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
}
