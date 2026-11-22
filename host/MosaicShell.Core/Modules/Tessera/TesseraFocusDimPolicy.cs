namespace MosaicShell.Core.Modules.Tessera
{
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

        /// <summary>Host must use this for SetLayeredWindowAttributes, never a parallel magic number.</summary>
        public static byte ResolveLayeredAlpha()
        {
            return OverlayAlpha;
        }

        /// <summary>
        /// Whether this OS can draw the dim as the policy requires: a translucent wash that never captures input. Only
        /// Windows can today (constant LWA_ALPHA plus WS_EX_TRANSPARENT). Elsewhere those calls do nothing, so the dim is
        /// an opaque full-screen sheet that hides the flyouts and swallows clicks; it stays off until that platform has
        /// its own translucency and click-through.
        /// </summary>
        public static bool PlatformCanDim => OperatingSystem.IsWindows();

        /// <summary>
        /// Host gate for showing the dim: the user's <c>focusDim</c> setting, where the platform can draw it. Pass
        /// <see cref="PlatformCanDim"/>; the parameter keeps both platforms' answers testable on any OS.
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
