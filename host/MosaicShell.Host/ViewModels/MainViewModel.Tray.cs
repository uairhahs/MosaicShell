using MosaicShell.Core.HubShell;
using MosaicShell.Core.Modules;

namespace MosaicShell.Host.ViewModels
{
    /// <summary>
    /// The tray menu's side of the Hub (U24): the installed modules with their state, and turning one on or off through
    /// the same steps as the Hub's switch. The tray and GNOME's top bar menu read and drive it.
    /// </summary>
    public partial class MainViewModel
    {
        /// <summary>A module turned on or off, was added or removed: the tray rebuilds its menu.</summary>
        public event EventHandler? ModuleStatesChanged;

        /// <summary>The tray's switches, from the Hub's rows so both always agree.</summary>
        public IReadOnlyList<TrayModule> TrayModules()
        {
            Dictionary<string, LibraryItemViewModel> rows = Modules.Concat(Widgets)
                .ToDictionary(m => m.Id, StringComparer.OrdinalIgnoreCase);
            return TrayMenu.Modules(
                ModuleCatalog.All,
                id => rows.TryGetValue(id, out LibraryItemViewModel? row) && row.IsInstalled,
                id => rows.TryGetValue(id, out LibraryItemViewModel? row) && row.IsOn);
        }

        /// <summary>Turns a module on or off from the tray; nothing happens when it is already in that state.</summary>
        public async Task SetModuleOnAsync(string moduleId, bool on)
        {
            LibraryItemViewModel? item = Modules.Concat(Widgets)
                .FirstOrDefault(m => m.Id.Equals(moduleId, StringComparison.OrdinalIgnoreCase));
            if (item is null || !item.IsInstalled || item.IsOn == on)
            {
                return;
            }

            await ToggleModuleOnAsync(item);
        }

        private void RaiseModuleStatesChanged()
        {
            ModuleStatesChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
