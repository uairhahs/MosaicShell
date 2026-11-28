using MosaicShell.Core.HostPlatform;
using MosaicShell.Core.Modules;
using MosaicShell.Core.Modules.Settings;

namespace MosaicShell.Host.ViewModels.ModulePage
{
    /// <summary>
    /// What a module page needs from the Hub: module state, the steps behind the on switch, and the follow-up after a
    /// saved change. <see cref="MainViewModel"/> is the real one; tests pass a recording fake.
    /// </summary>
    public interface IModulePageHost
    {
        DesktopFeatures Features { get; }

        bool IsSignInStartOn { get; }

        bool IsInstalled(string moduleId);

        bool IsArmed(string moduleId);

        bool IsWidgetOpen(string moduleId);

        /// <summary>Runs one step of the on switch; false when it failed (the host reports why).</summary>
        Task<bool> RunStepAsync(string moduleId, ModuleSwitchStep step);

        /// <summary>Runs what a saved change needs (<see cref="ModuleSettingsApplyPolicy"/>).</summary>
        Task FollowUpAsync(string moduleId, SettingFollowUp followUp);

        void TurnOnSignInStart();

        /// <summary>Opens the module's overlay on screen to show what it does, without changing anything.</summary>
        Task PreviewAsync(string moduleId);

        /// <summary>Starts Remove for the module, with the same confirm or undo as the library row.</summary>
        void Remove(string moduleId);

        /// <summary>Apps, shortcuts and settings pages a list row can pick from.</summary>
        IReadOnlyList<string> LaunchTargetLabels { get; }

        /// <summary>The target a picked label stands for, and its display name.</summary>
        bool TryResolveLaunchTarget(string label, out string target, out string displayName);

        /// <summary>A line for the Hub's status area.</summary>
        void Report(string message);

        /// <summary>The control for a custom slot (<see cref="SettingKind.Custom"/>), by its <c>CustomId</c>.</summary>
        CustomRowViewModel CreateCustomRow(ModulePageViewModel page, SettingRow row, SettingAvailability availability);

        /// <summary>A row was saved; the Hub refreshes anything it shows from that setting.</summary>
        void Saved(string moduleId, string key);
    }
}
