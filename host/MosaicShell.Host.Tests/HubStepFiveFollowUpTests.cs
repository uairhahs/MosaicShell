using Avalonia.Headless.XUnit;
using FluentAssertions;
using MosaicShell.Core;
using MosaicShell.Core.Modules;
using MosaicShell.Core.Runtime;
using MosaicShell.Core.Services;
using MosaicShell.Host.ViewModels;

namespace MosaicShell.Host.Tests
{
    /// <summary>Found in the Windows check of step 5 (2026-11-28): the page after Remove, and the row button's name.</summary>
    public sealed class HubStepFiveFollowUpTests : HeadlessHostTest
    {
        [AvaloniaFact]
        public async Task Removing_a_module_turns_its_open_page_switch_off_at_once()
        {
            _ = Directory.CreateDirectory(Path.Combine(AppPaths.ModulesDirectory, ModuleIds.Chrono));
            TileRuntime runtime = new(new ShowsEverything());
            _ = runtime.Start(ModuleIds.Chrono);
            MainViewModel vm = new(runtime, HostServicesFakes.Create(), null);
            vm.NavigateCommand.Execute("Widgets");
            LibraryItemViewModel chrono = vm.Widgets.Single(w => w.Id == ModuleIds.Chrono);
            vm.OpenModuleCommand.Execute(chrono);
            _ = vm.ModulePage!.IsOn.Should().BeTrue("Chrono is on the desktop");

            vm.RemoveModuleCommand.Execute(chrono);
            await Task.Yield();

            _ = vm.ModulePage.Should().NotBeNull("Remove leaves the page open");
            _ = vm.ModulePage!.IsOn.Should().BeFalse("the switch said On until the page was left");
        }

        [AvaloniaFact]
        public void Each_row_button_is_named_with_its_module()
        {
            _ = Directory.CreateDirectory(Path.Combine(AppPaths.ModulesDirectory, ModuleIds.Chrono));
            MainViewModel vm = new();
            vm.NavigateCommand.Execute("Widgets");

            LibraryItemViewModel chrono = vm.Widgets.Single(w => w.Id == ModuleIds.Chrono);
            LibraryItemViewModel pulse = vm.Widgets.Single(w => w.Id == ModuleIds.Pulse);

            _ = chrono.RowActionAutomationName.Should().Be($"{chrono.RowActionLabel} Chrono");
            _ = pulse.RowActionAutomationName.Should().Be("Add Pulse", "a screen reader heard \"Add\" on every row");
        }

        /// <summary>A surface host where every widget opens, so the runtime holds a real session.</summary>
        private sealed class ShowsEverything : ITileSurfaceHost
        {
            public bool Show(string moduleId, out string? error)
            {
                error = null;
                return true;
            }

            public void Focus(string moduleId)
            {
            }

            public void Close(string moduleId)
            {
            }
        }
    }
}
