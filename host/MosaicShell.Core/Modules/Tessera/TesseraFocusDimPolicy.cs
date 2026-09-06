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

        /// <summary>When <see cref="UseConstantLayeredAlpha"/>, composition fallback brush alpha must stay 0.</summary>
        public const byte MaxCompositionFallbackAlpha = 0;

        /// <summary>Contract: overlay is visual-only and must not block or steal pointer input.</summary>
        public const bool MustPassThroughInput = true;

        /// <summary>Contract: any outside click clears dim + flyout immediately (no fade).</summary>
        public const bool InstantDismissOnOutsideClick = true;

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

        /// <summary>Payload <c>focusDim</c>: "0" off; missing / other → on (default).</summary>
        public static bool EnabledFromPayload(IReadOnlyDictionary<string, string>? payload)
        {
            return payload is null || !payload.TryGetValue("focusDim", out string? raw) || string.IsNullOrWhiteSpace(raw) || raw is not ("0" or "false" or "False" or "off" or "Off");
        }
    }
}
