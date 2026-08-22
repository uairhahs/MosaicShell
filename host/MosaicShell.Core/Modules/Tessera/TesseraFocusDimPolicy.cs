namespace MosaicShell.Core.Modules.Tessera;

/// <summary>
/// Subtle desktop dim behind Tessera (Focus-plugin analogue). Extremely light - not a modal scrim.
/// Must never capture input (Win32 click-through); outside clicks dismiss Tessera instantly without swallowing the click.
/// </summary>
public static class TesseraFocusDimPolicy
{
 /// <summary>Mocha crust dim alpha - quiet but readable (~27% opacity).</summary>
 public const byte OverlayAlpha = 68;

 public const byte CrustR = 0x11;
 public const byte CrustG = 0x11;
 public const byte CrustB = 0x1b;

 /// <summary>Contract: overlay is visual-only and must not block or steal pointer input.</summary>
 public const bool MustPassThroughInput = true;

 /// <summary>Contract: any outside click clears dim + flyout immediately (no fade).</summary>
 public const bool InstantDismissOnOutsideClick = true;

 /// <summary>Payload <c>focusDim</c>: "0" off; missing / other → on (default).</summary>
 public static bool EnabledFromPayload(IReadOnlyDictionary<string, string>? payload)
 {
 if (payload is null) return true;
 if (!payload.TryGetValue("focusDim", out var raw) || string.IsNullOrWhiteSpace(raw))
 return true;
 return raw is not ("0" or "false" or "False" or "off" or "Off");
 }
}
