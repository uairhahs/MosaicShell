using MosaicShell.Core.Modules;

namespace MosaicShell.Core.HubShell
{
    /// <summary>Where a module runs, which is how the rail groups it.</summary>
    public enum RailGroup
    {
        /// <summary>A capability: it runs in the background and answers keys, events or idle time.</summary>
        Background,

        /// <summary>A widget: it sits on the desktop.</summary>
        Desktop,
    }

    /// <summary>One module on the rail.</summary>
    public sealed record RailEntry(string Id, string Name, RailGroup Group);

    /// <summary>
    /// The Hub's left rail: Home, the installed modules as the navigation (grouped by where they run, in catalog order,
    /// filtered by the search box), Add modules, then Settings and About. Folds to icons on a narrow window.
    /// </summary>
    public static class HubRail
    {
        /// <summary>Below this width the rail shows icons only.</summary>
        public const double CollapseBelowDip = 900;

        public static IReadOnlyList<RailEntry> Entries(IEnumerable<ModuleInfo> installed, string? query)
        {
            List<ModuleInfo> order = [.. ModuleCatalog.All];
            int Rank(ModuleInfo m)
            {
                int i = order.FindIndex(o => o.Id.Equals(m.Id, StringComparison.OrdinalIgnoreCase));
                return i < 0 ? int.MaxValue : i;
            }

            return [.. installed
                .Where(m => HubLibrarySearch.Matches(query, m.Id, m.DisplayName, m.Description))
                .OrderBy(m => GroupOf(m.Kind))
                .ThenBy(Rank)
                .Select(m => new RailEntry(m.Id, m.DisplayName, GroupOf(m.Kind)))];
        }

        public static RailGroup GroupOf(ModuleKind kind)
        {
            return kind == ModuleKind.Widget ? RailGroup.Desktop : RailGroup.Background;
        }

        /// <summary>
        /// The heading over a group. "In the background", not "in the tray": GNOME shows no tray without an extension.
        /// </summary>
        public static string GroupTitle(RailGroup group)
        {
            return group == RailGroup.Desktop ? "On the desktop" : "Running in the background";
        }

        public static bool IsCollapsed(double widthDip)
        {
            return widthDip < CollapseBelowDip;
        }

        /// <summary>The modules Add modules offers: every one known that is not installed.</summary>
        public static IReadOnlyList<ModuleInfo> Addable(IEnumerable<ModuleInfo> all, Func<string, bool> isInstalled)
        {
            return [.. all.Where(m => !isInstalled(m.Id))];
        }
    }
}
