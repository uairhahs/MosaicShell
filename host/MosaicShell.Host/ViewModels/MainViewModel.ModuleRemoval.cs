using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MosaicShell.Core.Modules;
using MosaicShell.Core.Runtime;

namespace MosaicShell.Host.ViewModels
{
    /// <summary>
    /// Remove, Undo and the confirmation for modules MosaicShell cannot bring back. The rules and the
    /// words come from <see cref="ModuleRemovalPolicy"/>; this file only wires them to the Hub.
    /// </summary>
    public partial class MainViewModel
    {
        private ModuleRemoval? _lastRemoval;
        private string _lastRemovalName = "";
        private bool _settingRemovalStatus;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasPendingRemoval), nameof(PendingRemovalTitle), nameof(PendingRemovalBody))]
        private LibraryItemViewModel? _pendingRemoval;

        [ObservableProperty] private bool _canUndoRemove;

        public bool HasPendingRemoval => PendingRemoval is not null;
        public string PendingRemovalTitle => PendingRemoval is null ? "" : ModuleRemovalPolicy.ConfirmTitle(PendingRemoval.Name);
        public string PendingRemovalBody => PendingRemoval is null ? "" : ModuleRemovalPolicy.ConfirmBody(PendingRemoval.Name);
        public string UndoLabel => ModuleRemovalPolicy.UndoLabel;

        [RelayCommand]
        private void RemoveModule(LibraryItemViewModel? item)
        {
            if (item is null || IsBusy || !ModuleRemovalPolicy.CanRemove(item.Id))
            {
                return;
            }

            if (ModuleRemovalPolicy.ModeFor(item.Id) == ModuleRemovalMode.ConfirmFirst)
            {
                PendingRemoval = item;
                return;
            }

            RemoveNow(item);
        }

        [RelayCommand]
        private void ConfirmRemove()
        {
            LibraryItemViewModel? item = PendingRemoval;
            PendingRemoval = null;
            if (item is not null)
            {
                RemoveNow(item);
            }
        }

        [RelayCommand]
        private void CancelRemove()
        {
            PendingRemoval = null;
        }

        [RelayCommand]
        private async Task UndoRemove()
        {
            ModuleRemoval? removal = _lastRemoval;
            if (removal is null || !removal.IsBundled || IsBusy)
            {
                return;
            }

            ClearUndo();
            IsBusy = true;
            try
            {
                await _installer.InstallAsync(removal.ModuleId);
                if (removal.WasArmed && _capabilityHost is not null)
                {
                    _ = await _capabilityHost.ArmAsync(removal.ModuleId);
                }

                StatusMessage = ModuleRemovalPolicy.RestoredStatus(_lastRemovalName);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Could not add {_lastRemovalName} back: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
                RefreshLibrary();
                ModulePage?.RefreshSwitch();
            }
        }

        private void RemoveNow(LibraryItemViewModel item)
        {
            ModuleRemoval? removal = ModuleUninstaller.Remove(item.Id, _runtime, _capabilityHost);
            if (removal is null)
            {
                return;
            }

            _settingRemovalStatus = true;
            StatusMessage = ModuleRemovalPolicy.RemovedStatus(item.Name);
            _settingRemovalStatus = false;

            _lastRemoval = removal.IsBundled ? removal : null;
            _lastRemovalName = item.Name;
            CanUndoRemove = _lastRemoval is not null;
            ShowToast(StatusMessage);
            RefreshLibrary();

            // The module's page stays open after Remove: its switch must say Off now, not when the page is next opened.
            ModulePage?.RefreshSwitch();
        }

        private void ClearUndo()
        {
            _lastRemoval = null;
            CanUndoRemove = false;
        }

        /// <summary>
        /// Every status line is a toast. Undo belongs to the removal message: any later message takes it away. The
        /// removal shows its own toast once its Undo is ready.
        /// </summary>
        partial void OnStatusMessageChanged(string value)
        {
            if (_settingRemovalStatus)
            {
                return;
            }

            ClearUndo();
            ShowToast(value);
        }
    }
}
