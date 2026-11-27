using FluentAssertions;
using MosaicShell.Core.HubShell;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// The Modules and Widgets pages list every one of their kind, installed or not, sorted by state: what is on first,
    /// then what is installed and off, then what can still be added; catalog order inside each.
    /// </summary>
    public sealed class ModuleListOrderTests
    {
        private sealed record Row(string Id, bool Installed, bool On);

        [Theory]
        [InlineData(true, true, ModuleListState.On)]
        [InlineData(true, false, ModuleListState.Off)]
        [InlineData(false, false, ModuleListState.NotAdded)]
        [InlineData(false, true, ModuleListState.NotAdded)]
        public void A_rows_state_comes_from_installed_and_on(bool installed, bool on, ModuleListState state)
        {
            _ = ModuleListOrder.StateOf(installed, on).Should().Be(state);
        }

        [Fact]
        public void Rows_sort_on_then_off_then_not_added_in_catalog_order()
        {
            Row[] rows =
            [
                new("Chord", false, false),
                new("Slate", true, false),
                new("Mixdeck", true, true),
                new("Inlay", false, false),
                new("Tessera", true, true),
                new("Substrate", true, false),
            ];

            IReadOnlyList<Row> sorted = ModuleListOrder.Sort(rows, r => ModuleListOrder.StateOf(r.Installed, r.On), r => r.Id);

            _ = sorted.Select(r => r.Id).Should().Equal("Tessera", "Mixdeck", "Slate", "Substrate", "Inlay", "Chord");
        }

        [Fact]
        public void A_module_the_catalog_does_not_know_sorts_last_in_its_state()
        {
            Row[] rows = [new("Acme", true, true), new("Tessera", true, true)];

            _ = ModuleListOrder.Sort(rows, r => ModuleListOrder.StateOf(r.Installed, r.On), r => r.Id)
                .Select(r => r.Id).Should().Equal("Tessera", "Acme");
        }

        [Theory]
        [InlineData(ModuleListState.On, "On", "Turn off")]
        [InlineData(ModuleListState.Off, "Off", "Turn on")]
        [InlineData(ModuleListState.NotAdded, "Not added", "Add")]
        public void Each_state_has_a_label_and_one_action(ModuleListState state, string label, string action)
        {
            _ = ModuleListOrder.Label(state).Should().Be(label);
            _ = ModuleListOrder.ActionLabel(state).Should().Be(action);
        }
    }
}
