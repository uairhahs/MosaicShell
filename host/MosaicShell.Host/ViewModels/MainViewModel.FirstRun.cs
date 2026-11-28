using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MosaicShell.Core.Install;
using MosaicShell.Core.Modules;
using MosaicShell.Core.Runtime;

namespace MosaicShell.Host.ViewModels
{
    /// <summary>
    /// First run (U25): the Host adds the default modules (<see cref="DefaultModulesPolicy"/>), turns them on, and shows one
    /// screen with them, each with its switch, then Done lands on Home. Never after an update (<see cref="FirstRunPolicy"/>).
    /// </summary>
    public partial class MainViewModel
    {
        [ObservableProperty] private bool _isFirstRunPage;

        /// <summary>Every installed module, background ones first: the first run screen and the tray panel list these.</summary>
        public IReadOnlyList<LibraryItemViewModel> InstalledModules { get; private set; } = [];

        public bool HasInstalledModules => InstalledModules.Count > 0;

        /// <summary>
        /// Called once the capabilities are restored. On a first run: adds the defaults, turns them on and opens the
        /// first run screen. Every start records that the defaults were offered, so this decides once per install.
        /// </summary>
        /// <param name="settingsSavedAtStart">
        /// Whether the data folder held saved settings before this process wrote any (<c>Program.SettingsSavedAtStart</c>):
        /// the Host saves Tessera's settings within its first moments, so reading the folder here would always find some.
        /// </param>
        public async Task RunFirstRunIfDueAsync(bool settingsSavedAtStart)
        {
            if (Hub.DefaultModulesOffered)
            {
                return;
            }

            bool firstRun = FirstRunPolicy.IsFirstRun(defaultsOffered: false, anySettingsSaved: settingsSavedAtStart);
            Hub.DefaultModulesOffered = true;
            ModuleSettingsStore.Save("Hub", Hub);
            if (!firstRun)
            {
                return;
            }

            bool anyInstalled = ModuleCatalog.All.Any(m => ModuleCatalog.IsInstalled(m.Id));
            List<string> failed = [];
            foreach (string id in DefaultModulesPolicy.ToInstall(alreadyOffered: false, anyModuleInstalled: anyInstalled))
            {
                try
                {
                    await _installer.InstallAsync(id);
                }
                catch (Exception ex) when (ex is InvalidOperationException or IOException or UnauthorizedAccessException)
                {
                    failed.Add(id);
                }
            }

            // Turned on directly, not through the switch's steps, so the screen opens without a toast per module.
            if (_capabilityHost is not null)
            {
                foreach (string id in FirstRunPolicy.ToTurnOn(HostInstallPolicy.DefaultModuleIds, ModuleCatalog.IsInstalled, _capabilityHost.IsArmed))
                {
                    _ = await _capabilityHost.ArmAsync(id);
                }
            }

            Navigate("FirstRun");
            RefreshLibrary();
            if (failed.Count > 0)
            {
                StatusMessage = DefaultModulesPolicy.FailedStatus(failed);
            }
        }

        [RelayCommand]
        private void FinishFirstRun()
        {
            Navigate("Home");
        }

        /// <summary>Show it on screen, from the first run screen.</summary>
        [RelayCommand]
        private Task PreviewModule(LibraryItemViewModel? item)
        {
            return item is null ? Task.CompletedTask : OpenOverlayPreviewAsync(item.Id);
        }

        /// <summary>
        /// Announces the list only when its rows change. A module turning on or off updates its own row; announcing the
        /// list then rebuilt every switch in the tray panel, dropping focus and the next switch under the pointer.
        /// </summary>
        private void NotifyInstalledModules()
        {
            List<LibraryItemViewModel> now = [.. Modules.Concat(Widgets).Where(static m => m.IsInstalled)];
            if (now.SequenceEqual(InstalledModules))
            {
                return;
            }

            InstalledModules = now;
            OnPropertyChanged(nameof(InstalledModules));
            OnPropertyChanged(nameof(HasInstalledModules));
        }
    }
}
