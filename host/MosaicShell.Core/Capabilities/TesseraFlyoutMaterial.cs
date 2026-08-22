namespace MosaicShell.Core.Capabilities;

/// <summary>
/// Flyout material policy. Soft frost = translucent crust + chrome edge blend.
/// Never requests OS AcrylicBlur - that paints a hard rectangular slab behind rounded panels.
/// </summary>
public sealed record TesseraFlyoutMaterial(
 bool UseSoftFrost,
 IReadOnlyList<string> TransparencyHints,
 byte ShellAlpha,
 bool ShouldLockClientSize,
 bool UseEdgeBlend);

public static class TesseraFlyoutMaterialFactory
{
 public const byte SoftFrostShellAlpha = 188;
 public const byte SolidShellAlpha = 232;

 /// <param name="useAcrylic">Legacy setting name - means soft frost tint, not OS acrylic.</param>
 public static TesseraFlyoutMaterial Create(bool useAcrylic) =>
 useAcrylic
 ? new TesseraFlyoutMaterial(
 UseSoftFrost: true,
 TransparencyHints: ["Transparent"],
 ShellAlpha: SoftFrostShellAlpha,
 ShouldLockClientSize: false,
 UseEdgeBlend: true)
 : new TesseraFlyoutMaterial(
 UseSoftFrost: false,
 TransparencyHints: ["Transparent"],
 ShellAlpha: SolidShellAlpha,
 ShouldLockClientSize: false,
 UseEdgeBlend: false);

 /// <summary>Payload key <c>acrylic</c>: "0" off, anything else / missing → soft frost on.</summary>
 public static bool UseAcrylicFromPayload(IReadOnlyDictionary<string, string>? payload)
 {
 if (payload is null) return true;
 if (!payload.TryGetValue("acrylic", out var raw) || string.IsNullOrWhiteSpace(raw))
 return true;
 return raw is not ("0" or "false" or "False" or "off" or "Off");
 }

 public static TesseraFlyoutMaterial FromPayload(IReadOnlyDictionary<string, string>? payload) =>
 Create(UseAcrylicFromPayload(payload));
}
