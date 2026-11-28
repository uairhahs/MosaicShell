using MosaicShell.Core.Modules;

namespace MosaicShell.Core.HubShell
{
    /// <summary>One module's switch in the tray menu: its id, the label shown, and whether it is on.</summary>
    public sealed record TrayModule(string Id, string Label, bool IsOn);

    /// <summary>
    /// The tray menu (U24): a switch per installed module, so it turns on and off without the Hub, then Open and Quit.
    /// The same list fills GNOME's top bar menu, which the Host answers over D-Bus.
    /// </summary>
    public static class TrayMenu
    {
        public const string OpenLabel = "Open MosaicShell";

        /// <summary>The same words as Settings' close choice.</summary>
        public const string QuitLabel = "Quit MosaicShell";

        /// <summary>
        /// The installed modules, background ones (capabilities and hybrids) first and then widgets, each in catalog
        /// order.
        /// </summary>
        public static IReadOnlyList<TrayModule> Modules(
            IEnumerable<ModuleInfo> catalog,
            Func<string, bool> isInstalled,
            Func<string, bool> isOn)
        {
            List<ModuleInfo> installed = [.. catalog.Where(m => isInstalled(m.Id))];
            return
            [
                .. installed.Where(m => m.Kind != ModuleKind.Widget)
                    .Concat(installed.Where(m => m.Kind == ModuleKind.Widget))
                    .Select(m => new TrayModule(m.Id, m.DisplayName, isOn(m.Id))),
            ];
        }
    }
}
