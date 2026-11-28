using Avalonia.Headless.XUnit;
using FluentAssertions;
using MosaicShell.Core;
using MosaicShell.Core.HubShell;
using MosaicShell.Core.Modules;
using MosaicShell.Core.Runtime;
using MosaicShell.Core.Services;
using MosaicShell.Host.ViewModels;

namespace MosaicShell.Host.Tests
{
    /// <summary>The tray menu's switches (U24) turn modules on and off through the same steps as the Hub's.</summary>
    public sealed class TrayMenuHostTests : HeadlessHostTest
    {
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

        private static MainViewModel WithChrono(out TileRuntime runtime)
        {
            _ = Directory.CreateDirectory(Path.Combine(AppPaths.ModulesDirectory, ModuleIds.Chrono));
            runtime = new TileRuntime(new ShowsEverything());
            return new MainViewModel(runtime, HostServicesFakes.Create(), null);
        }

        [AvaloniaFact]
        public void The_tray_lists_installed_modules_with_their_state()
        {
            MainViewModel vm = WithChrono(out _);

            _ = vm.TrayModules().Should().ContainSingle().Which.Should().Be(new TrayModule(ModuleIds.Chrono, "Chrono", false));
        }

        [AvaloniaFact]
        public async Task A_tray_switch_turns_the_module_on_and_tells_the_tray()
        {
            MainViewModel vm = WithChrono(out TileRuntime runtime);
            int changes = 0;
            vm.ModuleStatesChanged += (_, _) => changes++;

            await vm.SetModuleOnAsync(ModuleIds.Chrono, on: true);

            _ = runtime.IsRunning(ModuleIds.Chrono).Should().BeTrue();
            _ = vm.TrayModules().Single().IsOn.Should().BeTrue();
            _ = changes.Should().BePositive("the tray rebuilds its menu from the new state");
        }

        [AvaloniaFact]
        public async Task A_switch_already_in_that_state_does_nothing()
        {
            MainViewModel vm = WithChrono(out TileRuntime runtime);

            await vm.SetModuleOnAsync(ModuleIds.Chrono, on: false);
            await vm.SetModuleOnAsync("NotAModule", on: true);

            _ = runtime.IsRunning(ModuleIds.Chrono).Should().BeFalse();
        }
    }
}
