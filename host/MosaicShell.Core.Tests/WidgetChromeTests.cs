using FluentAssertions;
using MosaicShell.Core.Modules;

namespace MosaicShell.Core.Tests
{
    public class WidgetChromeTests
    {
        [Fact]
        public void Tile_overlay_context_menu_contract_lists_required_actions()
        {
            _ = TileOverlayChromeSpec.RequiredContextMenuHeaders.Should().ContainInOrder(
                "Configure in Host",
                "Align",
                "Change Z layer",
                "Refresh",
                "Unload");
        }

        [Fact]
        public void Tile_overlay_context_menu_contract_is_non_empty_parity_gate()
        {
            _ = TileOverlayChromeSpec.RequiredContextMenuHeaders.Should().HaveCountGreaterThan(3);
            _ = TileOverlayChromeSpec.RequiredContextMenuHeaders.Should().OnlyContain(h => !string.IsNullOrWhiteSpace(h));
        }
    }
}
