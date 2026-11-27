using CommunityToolkit.Mvvm.ComponentModel;
using MosaicShell.Core.HostPlatform;
using MosaicShell.Core.Modules;
using MosaicShell.Core.Modules.Settings;
using MosaicShell.Core.Runtime;
using MosaicShell.Host.ViewModels.ModulePage;

namespace MosaicShell.Host.ViewModels
{
    /// <summary>
    /// The Hub as host of the schema module page: module state, the steps behind the on switch, and the follow-up
    /// after a saved change.
    /// </summary>
    public partial class MainViewModel : IModulePageHost
    {
        /// <summary>The open module's page, for every module with a Core schema; null for Tessera and modules without one.</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasModulePage))]
        private ModulePageViewModel? _modulePage;

        public bool HasModulePage => ModulePage is not null;

        /// <summary>The page for <paramref name="info"/>, or null when Core has no schema for it.</summary>
        private ModulePageViewModel? CreateModulePage(ModuleInfo info)
        {
            return ModuleIds.IsTessera(info.Id)
                ? new ModulePageViewModel(info, ModuleSettingsSchemas.Tessera, this) { HeaderPreview = new TesseraHeaderPreviewViewModel(this) }
                : ModuleSettingsSchemas.TryGet(info.Id, out ModuleSettingsSchema? schema)
                    ? new ModulePageViewModel(info, schema!, this)
                    : null;
        }

        /// <summary>Saves whatever the open page still holds before it goes.</summary>
        private void CloseModulePage()
        {
            StopTesseraSlots();
            if (ModulePage is ModulePageViewModel page)
            {
                ModulePage = null;
                _ = page.FlushAsync();
            }
        }

        DesktopFeatures IModulePageHost.Features => _services.Features;

        bool IModulePageHost.IsSignInStartOn => AutostartEnabled;

        bool IModulePageHost.IsInstalled(string moduleId)
        {
            return ModuleCatalog.IsInstalled(moduleId);
        }

        bool IModulePageHost.IsArmed(string moduleId)
        {
            return _capabilityHost?.IsArmed(moduleId) == true;
        }

        bool IModulePageHost.IsWidgetOpen(string moduleId)
        {
            return _runtime.IsRunning(moduleId);
        }

        async Task<bool> IModulePageHost.RunStepAsync(string moduleId, ModuleSwitchStep step)
        {
            bool ok;
            switch (step)
            {
                case ModuleSwitchStep.Install:
                    StatusMessage = $"Installing {moduleId}...";
                    await _installer.InstallAsync(moduleId);
                    ok = ModuleCatalog.IsInstalled(moduleId);
                    StatusMessage = ok ? $"Installed {moduleId}." : $"Could not install {moduleId}.";
                    break;
                case ModuleSwitchStep.Arm:
                    ok = _capabilityHost is not null && await _capabilityHost.ArmAsync(moduleId);
                    string? hotkeyError = ok ? _capabilityHost!.GetHotkeyError(moduleId) : null;
                    StatusMessage = !ok
                        ? $"Could not turn on {moduleId}."
                        : string.IsNullOrWhiteSpace(hotkeyError)
                            ? $"{moduleId} is on. {ModuleUsageGuide.HowToTrigger(moduleId, _services.Features)}".Trim()
                            : $"{moduleId} is on. {hotkeyError}";
                    break;
                case ModuleSwitchStep.Disarm:
                    ok = _capabilityHost is not null && await _capabilityHost.DisarmAsync(moduleId);
                    StatusMessage = ok ? $"{moduleId} is off." : $"Could not turn off {moduleId}.";
                    break;
                case ModuleSwitchStep.ShowWidget:
                    ModuleLaunchResult launch = _launcher.TryLaunch(moduleId);
                    ok = launch.Started;
                    StatusMessage = launch.Message;
                    break;
                case ModuleSwitchStep.CloseWidget:
                    ok = !_runtime.IsRunning(moduleId) || _runtime.Stop(moduleId);
                    StatusMessage = $"{moduleId} is off.";
                    break;
                default:
                    ok = false;
                    break;
            }

            RefreshLibrary();
            return ok;
        }

        async Task IModulePageHost.FollowUpAsync(string moduleId, SettingFollowUp followUp)
        {
            switch (followUp)
            {
                case SettingFollowUp.ReArm when _capabilityHost is not null:
                    bool ok = await _capabilityHost.ReArmAsync(moduleId);
                    string? hotkeyError = ok ? _capabilityHost.GetHotkeyError(moduleId) : null;
                    StatusMessage = !ok
                        ? $"Saved, but {moduleId} could not restart."
                        : string.IsNullOrWhiteSpace(hotkeyError) ? $"Saved. {moduleId} is using it now." : hotkeyError;
                    break;
                case SettingFollowUp.RelaunchWidget:
                    _tileHost?.Refresh(moduleId);
                    StatusMessage = $"Saved. {moduleId} is showing it now.";
                    break;
                case SettingFollowUp.OfferRestart:
                    StatusMessage = "Saved. Restart MosaicShell to see the change.";
                    break;
                default:
                    break;
            }
        }

        void IModulePageHost.TurnOnSignInStart()
        {
            if (!AutostartEnabled)
            {
                ToggleAutostart();
            }
        }

        Task IModulePageHost.PreviewAsync(string moduleId)
        {
            if (ModuleIds.IsTessera(moduleId))
            {
                // A pure preview: it plays a volume flyout without turning Tessera on.
                PreviewTesseraFlyout();
                return Task.CompletedTask;
            }

            return OpenOverlayPreviewAsync(moduleId);
        }

        void IModulePageHost.Remove(string moduleId)
        {
            LibraryItemViewModel? item = Modules.Concat(Widgets)
                .FirstOrDefault(m => m.Id.Equals(moduleId, StringComparison.OrdinalIgnoreCase));
            if (item is not null)
            {
                RemoveModule(item);
            }
        }

        IReadOnlyList<string> IModulePageHost.LaunchTargetLabels
        {
            get
            {
                EnsureLaunchTargetsLoaded();
                return LaunchTargetChoices;
            }
        }

        bool IModulePageHost.TryResolveLaunchTarget(string label, out string target, out string displayName)
        {
            return LaunchTargetCatalog.TryResolveLabel(label, out target, out displayName);
        }

        void IModulePageHost.Report(string message)
        {
            StatusMessage = message;
        }
    }
}
