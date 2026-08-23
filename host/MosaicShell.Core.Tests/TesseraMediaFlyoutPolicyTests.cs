using FluentAssertions;
using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Core.Tests;

public class TesseraMediaFlyoutPolicyTests
{
    [Theory]
    [InlineData(true, false, "", TesseraMediaChangeAction.PresentMediaFlyout)]
    [InlineData(true, false, "vol", TesseraMediaChangeAction.PresentMediaFlyout)]
    [InlineData(true, false, "media", TesseraMediaChangeAction.PresentMediaFlyout)]
    [InlineData(false, false, "", TesseraMediaChangeAction.Ignore)]
    [InlineData(true, true, "vol", TesseraMediaChangeAction.SoftRefreshVisible)]
    [InlineData(true, true, "bright", TesseraMediaChangeAction.SoftRefreshVisible)]
    [InlineData(true, true, "media", TesseraMediaChangeAction.SoftRefreshVisible)]
    [InlineData(true, true, "locks", TesseraMediaChangeAction.PresentMediaFlyout)]
    [InlineData(false, true, "vol", TesseraMediaChangeAction.SoftRefreshVisible)]
    public void Resolve_track_change_actions(
        bool enableMedia,
        bool visible,
        string lastKind,
        TesseraMediaChangeAction expected) =>
        TesseraMediaFlyoutPolicy.Resolve(enableMedia, visible, lastKind).Should().Be(expected);
}
