namespace MosaicShell.Core.Modules.Tessera;

/// <summary>
/// Ephemeral status flyouts (caps/airplane). Warm toggles Patch in place (dismiss timer
/// reset via TryApplyLive); cold/hidden sessions Present/revive via TesseraFlyoutRefreshPolicy.
/// Status must never share volume/media stacked HWNDs (H3 acrylic): one chip, one window.
/// </summary>
public static class TesseraStatusFlyoutPolicy
{
    /// <summary>Content-sized chip height (DIP). Not volume/media shell height.</summary>
    public const double ChipHeightDip = 50;

    /// <summary>Status chip must never inherit media-shell width.</summary>
    public const double ChipMaxWidthDip = 280;

    /// <summary>Fallback width when media stale measure is cleared before chip layout.</summary>
    public const double ChipTypicalWidthDip = 168;

    /// <summary>Status chip must never inherit media-shell height.</summary>
    public const double ChipMaxHeightDip = 72;

    /// <summary>Default rounded clip for OS acrylic status HWNDs.</summary>
    public const float ChipCornerRadiusDip = 12f;

    public static bool IsStatusKind(string? kind) =>
        kind is not null
        && (kind.Equals("locks", StringComparison.OrdinalIgnoreCase)
            || kind.Equals("flight", StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Caps/airplane replace any open volume+media session with a dedicated status HWND.
    /// Never route through <see cref="TesseraOsAcrylicStackedPolicy.UseMultiWindow"/>.
    /// </summary>
    public static bool MustUseDedicatedSingleWindow(string? kind) => IsStatusKind(kind);

    /// <summary>
    /// Status→volume handoff: Host must cancel the status dismiss timer before Close so a
    /// late Tick cannot dismiss the replacement volume/media session.
    /// </summary>
    public static bool SupersededStatusMustCancelDismissBeforeClose =>
        TesseraOsAcrylicStackedPolicy.SupersededSingleHwndMustCancelDismissBeforeClose;

    /// <summary>
    /// Status chips must measure to content. Forcing volume Width×Height leaves a square
    /// AcrylicBlur / Transparent HWND with black corners around the pill.
    /// </summary>
    public static bool ForbidFixedVolumeShellSize => true;

    /// <summary>
    /// Under OS acrylic, skip nested Skia Glass (edge-only / transparent chrome) and clip
    /// the HWND to <see cref="ResolveChipCornerRadiusDip"/>.
    /// </summary>
    public static bool PreferOsAcrylicEdgeOnlyChrome => true;

    /// <summary>
    /// SoftFrost Transparent and AcrylicBlur status HWNDs are rectangular. Composition clears
    /// corners to black until (or unless) Win32 round-clips before Opacity reveal.
    /// </summary>
    public const bool MustRoundClipHwndBeforeReveal = true;

    /// <summary>
    /// Round-clip SoftFrost status too (not only AcrylicBlur). Nested Skia Glass is rounded;
    /// unclipped HWND corners paint solid black on first frames.
    /// </summary>
    public const bool SoftFrostMustRoundClipHwnd = true;

    /// <summary>
    /// <c>SetWindowRgn</c> must run synchronously when the HWND exists. Posting it at the same
    /// Loaded priority as Opacity reveal races a black rectangular frame.
    /// </summary>
    public const bool RoundClipMustApplySynchronouslyWhenHandleReady = true;

    /// <summary>
    /// Media→CapsLock: never size/clip with <c>Max(Bounds, Desired)</c>. Stale media Bounds
    /// keep a black acrylic rectangle around the chip after kind handoff.
    /// </summary>
    public const bool StatusLayoutMustPreferDesiredSizeOverStaleBounds = true;

    /// <summary>
    /// Stacked volume/media→status: Opacity-hide slots before status Present. Close()-only
    /// leaves dying AcrylicBlur HWNDs that flash black under the CapsLock chip.
    /// </summary>
    public const bool StackedToStatusMustHideSlotsBeforeStatusReveal = true;

    /// <summary>
    /// Media/volume single-shell → status: SoftFrost/media HWND must not ApplyRequest in place.
    /// Stale Bounds + cleared Win32 region leaves a full-screen black acrylic plate.
    /// </summary>
    public const bool MustRecreateHwndAfterMediaShell = true;

    public static bool MustRecreateHwndAfterMediaShellKind(string? openKind, string? nextKind) =>
        MustRecreateHwndAfterMediaShell
        && IsStatusKind(nextKind)
        && openKind is not null
        && !IsStatusKind(openKind);

    /// <summary>Measure looks like a volume/media shell, not a status chip.</summary>
    public static bool IsStaleMediaShellMeasure(double widthDip, double heightDip) =>
        widthDip > ChipMaxWidthDip || heightDip > ChipMaxHeightDip;

    /// <summary>
    /// Status client DIP for placement + round clip. Ignores stale media Bounds/Desired.
    /// </summary>
    public static (double Width, double Height) ResolveStatusClientSizeDip(
        double boundsWidth,
        double boundsHeight,
        double desiredWidth,
        double desiredHeight)
    {
        if (IsStaleMediaShellMeasure(desiredWidth, desiredHeight))
        {
            desiredWidth = 0;
            desiredHeight = 0;
        }

        if (IsStaleMediaShellMeasure(boundsWidth, boundsHeight))
        {
            boundsWidth = 0;
            boundsHeight = 0;
        }

        var w = desiredWidth > 1 ? desiredWidth : boundsWidth;
        var h = desiredHeight > 1 ? desiredHeight : boundsHeight;
        if (w < 1)
            w = ChipTypicalWidthDip;
        if (h < 1)
            h = ChipHeightDip;
        return (Math.Clamp(w, TesseraFlyoutWindowPolicy.RelayoutMinWidthDip, ChipMaxWidthDip),
                Math.Clamp(h, TesseraFlyoutWindowPolicy.RelayoutMinHeightDip, ChipMaxHeightDip));
    }

    public static float ResolveChipCornerRadiusDip(string? styleId)
    {
        var id = Styles.StyleIds.Normalize(styleId ?? Styles.StyleIds.Fluent);
        return id switch
        {
            Styles.StyleIds.Square => 24f,
            Styles.StyleIds.Gnome => 24f,
            Styles.StyleIds.MaterialYou => 24f,
            Styles.StyleIds.Meter => 16f,
            Styles.StyleIds.Radial => 10f,
            Styles.StyleIds.CoreUI => 8f,
            Styles.StyleIds.PlainText => 4f,
            Styles.StyleIds.Fluent => ChipCornerRadiusDip,
            _ => ChipCornerRadiusDip,
        };
    }
}
