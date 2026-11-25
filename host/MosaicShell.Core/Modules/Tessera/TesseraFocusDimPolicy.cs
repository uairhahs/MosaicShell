using MosaicShell.Core.Capabilities.Platform;

namespace MosaicShell.Core.Modules.Tessera
{
    /// <summary>The OS mechanism that draws the focus dim layer (translucent, and the outside-click hitbox).</summary>
    public enum FocusDimRoute
    {
        /// <summary>No mechanism on this system; the dim stays off.</summary>
        None,

        /// <summary>Windows: opaque background and constant LWA_ALPHA; the layer takes the outside click.</summary>
        Win32LayeredAlpha,

        /// <summary>X11 or XWayland with a compositor: ARGB background alpha; the layer takes the outside click.</summary>
        X11ArgbInputShape,
    }

    /// <summary>How a flyout is put back above the focus dim layer when the layer was shown after it.</summary>
    public enum FlyoutRestack
    {
        None,

        /// <summary>Windows: SetWindowPos with the flyout inserted above the dim.</summary>
        Win32InsertAfter,

        /// <summary>X11: an EWMH _NET_RESTACK_WINDOW request, the flyout directly above the layer.</summary>
        X11RestackWindow,
    }

    /// <summary>
    /// Subtle desktop dim behind Tessera (Focus-plugin analogue). Extremely light, not a modal scrim.
    /// The layer behind the flyout is the outside-click hitbox on every platform: a click outside the flyout dismisses Tessera
    /// instantly and is swallowed, popup-menu style.
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
        /// Whether an ingress must open or update the dim. Transient dismiss closes it, so every ingress that presents
        /// the session must open it again, a revived hidden window included (see
        /// <see cref="FlyoutIngressPolicy.PresentsSession"/>).
        /// </summary>
        public static bool MustSyncOnIngress(FlyoutIngressDecision decision)
        {
            return FlyoutIngressPolicy.PresentsSession(decision);
        }

        /// <summary>
        /// Invariant: the dim never outlives every Tessera flyout. Checked whenever a Tessera window reaches Hidden, so a
        /// dim re-opened by a request that raced an exit (the slots then hide anyway) is closed instead of staying over
        /// the desktop with no flyout above it.
        /// </summary>
        public static bool ShouldCloseOrphanedDim(bool dimOpen, bool anyTesseraFlyoutShowing)
        {
            return dimOpen && !anyTesseraFlyoutShowing;
        }

        /// <summary>
        /// LWA_ALPHA of a Windows layer that is only the outside-click hitbox (the dim is off, or another screen's bare
        /// layer): one step above invisible, since the layer must still take the click, as the X11 layer does.
        /// </summary>
        public const byte HitboxOnlyLayeredAlpha = 1;

        /// <summary>
        /// The Windows layer's SetLayeredWindowAttributes alpha; Host must use this, never a parallel magic number. The
        /// flyout's screen is dimmed when the dim is on; otherwise the layer is a hitbox only.
        /// </summary>
        public static byte ResolveLayeredAlpha(bool dimEnabled, bool onFlyoutScreen)
        {
            return dimEnabled && onFlyoutScreen ? OverlayAlpha : HitboxOnlyLayeredAlpha;
        }

        /// <summary>
        /// How this system draws the layer: a translucent wash that is also the outside-click hitbox. Windows uses an
        /// opaque window with constant LWA_ALPHA. Linux on X11 or XWayland uses a Transparent (ARGB) window whose
        /// background carries the alpha; that needs
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

        /// <summary>
        /// How each platform restacks a flyout above the layer. A layer mapped after the flyout lands on top of it, and on
        /// X11 the layer is the outside-click hitbox, so the flyout's own controls would dismiss it.
        /// </summary>
        public static FlyoutRestack ResolveRestack(FocusDimRoute route)
        {
            return route switch
            {
                FocusDimRoute.Win32LayeredAlpha => FlyoutRestack.Win32InsertAfter,
                FocusDimRoute.X11ArgbInputShape => FlyoutRestack.X11RestackWindow,
                _ => FlyoutRestack.None,
            };
        }

        /// <summary>
        /// The layer catches outside clicks and swallows them, popup-menu style, so it shows with the dim off too. On X11
        /// it must (Wayland gives the Host no clicks on other windows); Windows does the same, so both platforms behave
        /// alike.
        /// </summary>
        public static bool LayerIsOutsideClickHitbox(FocusDimRoute route)
        {
            return route != FocusDimRoute.None;
        }

        public static bool ShouldShowLayer(IReadOnlyDictionary<string, string>? payload, FocusDimRoute route)
        {
            return LayerIsOutsideClickHitbox(route) || ShouldShow(payload, CanDim(route));
        }

        /// <summary>
        /// Alpha of the layer's background brush: on X11 the dim's alpha, or fully transparent when the layer is only the
        /// hitbox. On Windows the brush stays opaque and <see cref="ResolveLayeredAlpha"/> sets the window's alpha.
        /// </summary>
        public static byte ResolveLayerAlpha(FocusDimRoute route, bool dimEnabled)
        {
            return dimEnabled || !PaintsAlphaInWindow(route) ? ResolveBackgroundAlpha(route) : (byte)0;
        }

        /// <summary>
        /// Whether the layer is shown on every screen, not only the flyout's. Where it is the outside-click hitbox, a
        /// screen without it lets a click there reach the app under it and leave the flyout up.
        /// </summary>
        public static bool LayerCoversEveryScreen(FocusDimRoute route)
        {
            return LayerIsOutsideClickHitbox(route);
        }

        /// <summary>
        /// The layer's alpha on one screen: only the flyout's screen is dimmed, as on Windows; the layers on the other
        /// screens (see <see cref="LayerCoversEveryScreen"/>) are fully transparent hitboxes.
        /// </summary>
        public static byte ResolveLayerAlpha(FocusDimRoute route, bool dimEnabled, bool onFlyoutScreen)
        {
            return ResolveLayerAlpha(route, dimEnabled && onFlyoutScreen);
        }

        /// <summary>Payload <c>focusDim</c>: "0" off; missing / other → on (default).</summary>
        public static bool EnabledFromPayload(IReadOnlyDictionary<string, string>? payload)
        {
            return payload is null || !payload.TryGetValue("focusDim", out string? raw) || string.IsNullOrWhiteSpace(raw) || raw is not ("0" or "false" or "False" or "off" or "Off");
        }
    }
}
