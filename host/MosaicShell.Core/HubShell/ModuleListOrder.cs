using MosaicShell.Core.Modules;

namespace MosaicShell.Core.HubShell
{
    /// <summary>Where a module stands, as the Modules and Widgets pages show it.</summary>
    public enum ModuleListState
    {
        On,
        Off,
        NotAdded,
    }

    /// <summary>
    /// The Modules and Widgets pages: every one of the kind, installed or not, sorted by state (on, then installed and
    /// off, then not added) and in catalog order inside each, with a state label and one action per row.
    /// </summary>
    public static class ModuleListOrder
    {
        public static ModuleListState StateOf(bool installed, bool on)
        {
            return !installed ? ModuleListState.NotAdded : on ? ModuleListState.On : ModuleListState.Off;
        }

        public static IReadOnlyList<T> Sort<T>(IEnumerable<T> rows, Func<T, ModuleListState> state, Func<T, string> id)
        {
            List<string> catalog = [.. ModuleCatalog.All.Select(m => m.Id)];
            int Rank(T row)
            {
                int i = catalog.FindIndex(c => c.Equals(id(row), StringComparison.OrdinalIgnoreCase));
                return i < 0 ? int.MaxValue : i;
            }

            return [.. rows.OrderBy(state).ThenBy(Rank)];
        }

        public static string Label(ModuleListState state)
        {
            return state switch
            {
                ModuleListState.On => "On",
                ModuleListState.Off => "Off",
                _ => "Not added",
            };
        }

        public static string ActionLabel(ModuleListState state)
        {
            return state switch
            {
                ModuleListState.On => "Turn off",
                ModuleListState.Off => "Turn on",
                _ => "Add",
            };
        }
    }
}
