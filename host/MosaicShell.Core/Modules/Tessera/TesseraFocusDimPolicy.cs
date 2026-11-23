using MosaicShell.Core.Capabilities.Platform;

namespace MosaicShell.Core.Modules.Tessera
{
    /// <summary>The OS mechanism that draws the focus dim translucent and click-through.</summary>
    public enum FocusDimRoute
    {
        /// <summary>No mechanism on this system; the dim stays off.</summary>
        None,

        /// <summary>Windows: opaque background, constant LWA_ALPHA and WS_EX_TRANSPARENT.</summary>
        Win32LayeredAlpha,

        /// <summary>X11 or XWayland with a compositor: ARGB background alpha and an empty input shape.</summary>
        X11ArgbInputShape,
    }

    /// <summary>
    /// Subtle desktop dim behind Tessera (Focus-plugin analogue). Extremely light, not a modal scrim.
    /// Must never capture input (Win32 click-through); outside clicks dismiss Tessera instantly without swallowing the click.
    /// </summary>
    public static class TesseraFocusDimPolicy
    {
        /// <summary>Mocha crust dim alpha, quiet but readable (~27% opacity).</summary>
        public const byte OverlayAlpha = 68;

        public const byte CrustR = 0x11;
        public const byte CrustG = 0x11;
        public const byte CrustB = 0x1b;

        /// <summary>
        /// Opacity is applied via constant WS_EX_LAYERED LWA_ALPHA only.
        /// High-alpha Transparent composition FallbackBrush paints a near-black fill under RedirectionSurface.
        /// </summary>
        public const bool UseConstantLayeredAlpha = true;

        /// <summary>
        /// Auto-dismiss / TransientDismiss must close FocusDim in the same turn as the flyout Hide.
        /// Outside-click already does; timer dismiss must not leave the dim orphaned.
        /// </summary>
        public const bool InstantDismissMustCloseFocusDim = true;

        /// <summary>Host gate, method so callers are not const-folded into unreachable code.</summary>
        public static bool ShouldCloseFocusDimOnTransientDismiss()
        {
            return InstantDismissMustCloseFocusDim;
        }

        /// <summary>
        /// Whether an ingress must open or update the dim. Transient dismiss closes it, so every decision that shows a
        /// flyout from hidden or rebuilds it must open it again, a revived hidden window included; only a patch of a
        /// session that is already showing keeps the dim it has.
        /// </summary>
        public static bool MustSyncOnIngress(FlyoutIngressDecision decision)
        {
            return decision is not (FlyoutIngressDecision.PatchActiveSession or FlyoutIngressDecision.PatchVisibleCoalesced);
        }

        /// <summary>Host must use this for SetLayeredWindowAttributes, never a parallel magic number.</summary>
        public static byte ResolveLayeredAlpha()
        {
            return OverlayAlpha;
        }

        /// <summary>
        /// How this system draws the dim as the policy requires: a translucent wash that never captures input. Windows
        /// uses constant LWA_ALPHA plus WS_EX_TRANSPARENT. Linux on X11 or XWayland uses a Transparent (ARGB) window
        /// whose background carries the alpha, plus an empty X11 input shape so every click passes through; that needs
        /// a running compositor, because without one the alpha is ignored and the dim is an opaque full-screen sheet
        /// that hides the flyouts and swallows clicks. Anywhere else there is no route and the dim stays off.
        /// </summary>
        public static FocusDimRoute ResolveRoute(bool isWindows, bool isLinux, bool hasX11Display, bool x11Composited)
        {
            return isWindows
                ? FocusDimRoute.Win32LayeredAlpha
                : isLinux && hasX11Display && x11Composited ? FocusDimRoute.X11ArgbInputShape : FocusDimRoute.None;
        }

        /// <summary>Whether <paramref name="route"/> can draw the dim at all; pass the result to <see cref="ShouldShow"/>.</summary>
        public static bool CanDim(FocusDimRoute route)
        {
            return route != FocusDimRoute.None;
        }

        /// <summary>
        /// Whether the window's own background carries the dim alpha (a Transparent window). On Windows it does not:
        /// the background is opaque and LWA_ALPHA (<see cref="ResolveLayeredAlpha"/>) makes the window translucent.
        /// </summary>
        public static bool PaintsAlphaInWindow(FocusDimRoute route)
        {
            return route == FocusDimRoute.X11ArgbInputShape;
        }

        /// <summary>Alpha of the crust background brush on <paramref name="route"/>.</summary>
        public static byte ResolveBackgroundAlpha(FocusDimRoute route)
        {
            return PaintsAlphaInWindow(route) ? OverlayAlpha : (byte)255;
        }

        /// <summary>
        /// Host gate for showing the dim: the user's <c>focusDim</c> setting, where the platform can draw it. Pass
        /// <see cref="CanDim"/> of the resolved route; the parameter keeps every platform's answer testable on any OS.
        /// </summary>
        public static bool ShouldShow(IReadOnlyDictionary<string, string>? payload, bool platformCanDim)
        {
            return platformCanDim && EnabledFromPayload(payload);
        }

        /// <summary>Payload <c>focusDim</c>: "0" off; missing / other → on (default).</summary>
        public static bool EnabledFromPayload(IReadOnlyDictionary<string, string>? payload)
        {
            return payload is null || !payload.TryGetValue("focusDim", out string? raw) || string.IsNullOrWhiteSpace(raw) || raw is not ("0" or "false" or "False" or "off" or "Off");
        }
    }
}
