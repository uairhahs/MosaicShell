namespace MosaicShell.Core.Modules.Tessera;

/// <summary>
/// Tessera module view of media flyout reactions (maps to <see cref="Capabilities.Platform.MediaFlyoutAction"/>).
/// </summary>
public enum TesseraMediaChangeAction
{
    Ignore = 0,
    SoftRefreshVisible = 1,
    PresentMediaFlyout = 2,
}
