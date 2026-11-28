using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MosaicShell.Core.HubShell;
using MosaicShell.Core.Install;
using MosaicShell.Core.Modules;
using MosaicShell.Core.Runtime;
using MosaicShell.Host.ViewModels.ModulePage;

namespace MosaicShell.Host.ViewModels
{
    /// <summary>
    /// The Hub's shell: the rail (Home, Modules, Widgets, Settings, About), Home's welcome with what is running now, one
    /// page each for modules and widgets listing every one sorted by state, and toasts in place of the status line.
    /// </summary>
    public partial class MainViewModel
    {
        private DispatcherTimer? _toastTimer;

        /// <summary>
        /// Modules turned off from Home while the Hub is open: their rows stay (shown off) so they can be turned straight
        /// back on, instead of vanishing from "Running now" the moment they stop.
        /// </summary>
        private readonly HashSet<string> _keptOnHome = [with(StringComparer.OrdinalIgnoreCase)];

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsModulesRailSelected))]
        private bool _isModulesList;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsWidgetsRailSelected))]
        private bool _isWidgetsList;

        /// <summary>The rail's Modules entry is lit on the list and on a module's own page.</summary>
        public bool IsModulesRailSelected => IsModulesList || (IsModuleConfig && ModuleCatalog.IsCapability(ConfigModuleId));

        public bool IsWidgetsRailSelected => IsWidgetsList || (IsModuleConfig && !string.IsNullOrEmpty(ConfigModuleId) && !ModuleCatalog.IsCapability(ConfigModuleId));

        /// <summary>The line under Home's welcome: what closing this window does, in words true for this desktop.</summary>
        public string CloseHint => HubSettingsLayout.CloseChoices(_services.Features)[CloseMinimizesToTray ? 0 : 1].Consequence;

        /// <summary>True on a narrow window: the rail shows icons only.</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(RailWidth))]
        private bool _isRailCollapsed;

        public double RailWidth => IsRailCollapsed ? 64 : 248;

        /// <summary>Home's banner for an update; empty when there is none.</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasUpdateBanner))]
        private string _updateBanner = "";

        [ObservableProperty] private string _toastMessage = "";
        [ObservableProperty] private bool _isToastVisible;
        [ObservableProperty] private bool _toastHasUndo;

        public bool HasUpdateBanner => UpdateBanner.Length > 0;

        public bool HasRunningBackground => Modules.Any(static m => m.IsOnHome);

        public bool HasRunningDesktop => Widgets.Any(static m => m.IsOnHome);

        /// <summary>Nothing on: Home says so and points at Modules and Widgets.</summary>
        public bool HasNothingRunning => !HasRunningBackground && !HasRunningDesktop;

        /// <summary>
        /// Brings one module's row up to date without rebuilding the lists, so it keeps its place; the lists sort by state
        /// again the next time a page is opened.
        /// </summary>
        private void RefreshModuleInPlace(string moduleId)
        {
            LibraryItemViewModel? item = Modules.Concat(Widgets).FirstOrDefault(m => m.Id.Equals(moduleId, StringComparison.OrdinalIgnoreCase));
            if (item is null)
            {
                RefreshLibrary();
                return;
            }

            if (!ModuleCatalog.IsInstalled(moduleId))
            {
                item.ApplyNotInstalled();
            }
            else
            {
                if (!item.IsInstalled)
                {
                    item.ApplyInstalled();
                }

                if (item.IsCapability)
                {
                    item.ApplyArmed(_capabilityHost?.IsArmed(moduleId) == true);
                }
                else
                {
                    item.ApplyRunning(_runtime.IsRunning(moduleId));
                }
            }

            RefreshShell();
        }

        /// <summary>Called by the window as it resizes.</summary>
        public void SetWindowWidth(double widthDip)
        {
            IsRailCollapsed = HubRail.IsCollapsed(widthDip);
        }

        /// <summary>Refreshes each installed module's line and problem mark, and what the rail lights.</summary>
        private void RefreshShell()
        {
            foreach (LibraryItemViewModel item in Modules.Concat(Widgets))
            {
                if (!item.IsInstalled || !ModuleCatalog.TryGet(item.Id, out ModuleInfo? info) || info is null)
                {
                    item.StatusLine = "";
                    item.IsProblem = false;
                }
                else
                {
                    string? hotkeyError = item.IsCapability && item.IsOn ? _capabilityHost?.GetHotkeyError(item.Id) : null;
                    ModuleStatus status = ModuleStatusLine.For(info, item.IsOn, hotkeyError, _services.Features);
                    item.StatusLine = status.Line;
                    item.IsProblem = status.IsProblem;
                }

                item.IsOnHome = item.IsRunningNow || (item.IsInstalled && _keptOnHome.Contains(item.Id));

            }

            OnPropertyChanged(nameof(HasRunningBackground));
            OnPropertyChanged(nameof(HasRunningDesktop));
            OnPropertyChanged(nameof(HasNothingRunning));
            OnPropertyChanged(nameof(IsModulesRailSelected));
            OnPropertyChanged(nameof(IsWidgetsRailSelected));
            OnPropertyChanged(nameof(CloseHint));
            NotifyInstalledModules();
            RaiseModuleStatesChanged();
        }

        [RelayCommand]
        private void OpenModule(LibraryItemViewModel? item)
        {
            if (item is not null)
            {
                OpenModuleConfigById(item.Id);
                RefreshShell();
            }
        }

        /// <summary>Home's switch: like the list's action, but the row stays on Home while it is off.</summary>
        [RelayCommand]
        private Task ToggleOnHomeAsync(LibraryItemViewModel? item)
        {
            if (item is not null && item.IsOn)
            {
                _ = _keptOnHome.Add(item.Id);
            }

            return ToggleModuleOnAsync(item);
        }

        /// <summary>The same steps as the switch on the module's page.</summary>
        [RelayCommand]
        private async Task ToggleModuleOnAsync(LibraryItemViewModel? item)
        {
            if (item is null || !ModuleCatalog.TryGet(item.Id, out ModuleInfo? info) || info is null)
            {
                return;
            }

            bool turnOn = !item.IsOn;
            foreach (ModuleSwitchStep step in ModuleOnSwitch.Plan(info.Kind, item.IsInstalled, turnOn))
            {
                if (!await ((IModulePageHost)this).RunStepAsync(item.Id, step))
                {
                    break;
                }
            }

            ModulePage?.RefreshSwitch();
        }

        /// <summary>A list row's one action: Add, Turn on or Turn off.</summary>
        [RelayCommand]
        private Task RowActionAsync(LibraryItemViewModel? item)
        {
            return item is null ? Task.CompletedTask : item.IsInstalled ? ToggleModuleOnAsync(item) : AddModuleAsync(item);
        }

        /// <summary>Add: install, then open the module's page with its switch ready.</summary>
        [RelayCommand]
        private async Task AddModuleAsync(LibraryItemViewModel? item)
        {
            if (item is null || IsBusy)
            {
                return;
            }

            IsBusy = true;
            item.IsInstalling = true;
            try
            {
                if (await ((IModulePageHost)this).RunStepAsync(item.Id, ModuleSwitchStep.Install))
                {
                    OpenModuleConfigById(item.Id);
                    RefreshShell();
                }
            }
            finally
            {
                item.IsInstalling = false;
                IsBusy = false;
            }
        }

        /// <summary>
        /// Add from file: installs a third-party package (a folder or .zip with module.manifest.json) and opens its page.
        /// The window picks the path; a failure is said in a toast.
        /// </summary>
        public async Task AddFromPackageAsync(string path)
        {
            if (IsBusy)
            {
                return;
            }

            IsBusy = true;
            try
            {
                string id = await _installer.InstallFromPackageAsync(path);
                StatusMessage = PackageInstallText.Installed(id, ModuleManifest.TryLoad(id)?.Kind);
                RefreshLibrary();
                OpenModuleConfigById(id);
            }
            catch (Exception ex) when (ex is InvalidOperationException or IOException or ArgumentException or UnauthorizedAccessException
                or System.Text.Json.JsonException or InvalidDataException)
            {
                StatusMessage = PackageInstallText.Failed(ex.Message);
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private void DismissToast()
        {
            IsToastVisible = false;
        }

        /// <summary>Shows <paramref name="value"/> as a toast; with Undo while a removal can still be undone.</summary>
        private void ShowToast(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value == "Ready")
            {
                return;
            }

            HubToast toast = CanUndoRemove ? HubToast.Undoable(value) : HubToast.Done(value);
            ToastMessage = toast.Message;
            ToastHasUndo = toast.UndoLabel is not null;
            IsToastVisible = true;
            _toastTimer?.Stop();
            _toastTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(toast.StaysMs) };
            _toastTimer.Tick += (_, _) =>
            {
                _toastTimer?.Stop();
                IsToastVisible = false;
            };
            _toastTimer.Start();
        }
    }

    public partial class LibraryItemViewModel
    {
        /// <summary>Home's and the rail's line on what the module is doing now.</summary>
        [ObservableProperty] private string _statusLine = "";

        /// <summary>Something to fix (a shortcut another app holds); the rail marks it.</summary>
        [ObservableProperty] private bool _isProblem;

        /// <summary>A capability is on while armed, a widget while it is on the desktop.</summary>
        public bool IsOn => IsCapability ? IsArmed : IsRunning;

        /// <summary>Not shipped with MosaicShell: Remove asks first, since MosaicShell cannot bring it back.</summary>
        public bool IsThirdParty => !ModuleCatalog.IsBundled(Id);

        /// <summary>Installed and on.</summary>
        public bool IsRunningNow => IsInstalled && IsOn;

        /// <summary>On Home's "Running now": on, or turned off from Home while the Hub is open.</summary>
        [ObservableProperty] private bool _isOnHome;

        public ModuleListState ListState => ModuleListOrder.StateOf(IsInstalled, IsOn);

        public string StateLabel => ModuleListOrder.Label(ListState);

        public string RowActionLabel => ModuleListOrder.ActionLabel(ListState);

        /// <summary>The row's action as a screen reader says it: "Add Mixdeck", not "Add" on every row.</summary>
        public string RowActionAutomationName => $"{RowActionLabel} {Name}";

        public bool IsStateOn => ListState == ModuleListState.On;

        /// <summary>The line under the name on a list: what it is doing when on, what it is otherwise.</summary>
        public string ListLine => IsRunningNow && StatusLine.Length > 0 ? StatusLine : Description;

        partial void OnIsArmedChanged(bool value)
        {
            NotifyState();
        }

        partial void OnIsRunningChanged(bool value)
        {
            NotifyState();
        }

        partial void OnIsInstalledChanged(bool value)
        {
            NotifyState();
        }

        partial void OnStatusLineChanged(string value)
        {
            OnPropertyChanged(nameof(ListLine));
        }

        private void NotifyState()
        {
            OnPropertyChanged(nameof(IsOn));
            OnPropertyChanged(nameof(IsRunningNow));
            OnPropertyChanged(nameof(ListState));
            OnPropertyChanged(nameof(StateLabel));
            OnPropertyChanged(nameof(RowActionLabel));
            OnPropertyChanged(nameof(RowActionAutomationName));
            OnPropertyChanged(nameof(IsStateOn));
            OnPropertyChanged(nameof(ListLine));
        }
    }
}
