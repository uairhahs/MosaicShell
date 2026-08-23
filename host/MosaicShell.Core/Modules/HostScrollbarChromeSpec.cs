namespace MosaicShell.Core.Modules;

/// <summary>
/// Hub scrollbars use Avalonia <c>FluentTheme</c> chrome as-is (overlay auto-hide,
/// size, line buttons, thumb). Host must not restyle template parts or replace
/// <c>ScrollBarSize</c> — custom overlay chrome crashed Settings and tile config.
/// </summary>
public static class HostScrollbarChromeSpec
{
    public const bool OverrideFluentChrome = false;
    public const bool OverrideScrollBarSize = false;
    public const bool OverrideThumbBrushes = false;
    public const bool HideLineButtons = false;
}
