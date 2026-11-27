using Avalonia.Headless.XUnit;
using FluentAssertions;
using MosaicShell.Core;
using MosaicShell.Core.Modules;
using MosaicShell.Host.ViewModels;

namespace MosaicShell.Host.Tests
{
    /// <summary>
    /// Home's "Running now" has a quick switch per row. Turning one off there keeps its row (shown off) while the Hub is
    /// open, so it can be turned straight back on instead of vanishing.
    /// </summary>
    public sealed class HomeRunningTests : HeadlessHostTest
    {
        [AvaloniaFact]
        public async Task Turning_a_module_off_from_Home_keeps_its_row_there()
        {
            _ = Directory.CreateDirectory(Path.Combine(AppPaths.ModulesDirectory, ModuleIds.Chrono));
            MainViewModel vm = new();
            vm.NavigateCommand.Execute("Home");
            LibraryItemViewModel chrono = vm.Widgets.Single(w => w.Id == ModuleIds.Chrono);
            chrono.ApplyRunning(true);
            vm.NavigateCommand.Execute("Home");
            _ = vm.Widgets.Single(w => w.Id == ModuleIds.Chrono).IsOnHome.Should().BeFalse("nothing is running in this test host");

            await vm.ToggleOnHomeCommand.ExecuteAsync(chrono);

            LibraryItemViewModel after = vm.Widgets.Single(w => w.Id == ModuleIds.Chrono);
            _ = after.IsOn.Should().BeFalse();
            _ = after.IsOnHome.Should().BeTrue("a row turned off from Home stays until the Hub closes");
            _ = vm.HasRunningDesktop.Should().BeTrue();
        }

        [AvaloniaFact]
        public async Task Toggling_never_moves_a_row()
        {
            foreach (string id in new[] { ModuleIds.Chrono, ModuleIds.Canvas, ModuleIds.Pulse })
            {
                _ = Directory.CreateDirectory(Path.Combine(AppPaths.ModulesDirectory, id));
            }

            MainViewModel vm = new();
            vm.NavigateCommand.Execute("Widgets");
            List<string> before = [.. vm.Widgets.Select(w => w.Id)];
            LibraryItemViewModel first = vm.Widgets.First(w => w.IsInstalled);

            await vm.RowActionCommand.ExecuteAsync(first);

            _ = vm.Widgets.Select(w => w.Id).Should().Equal(before, "a toggle updates its row in place");
            _ = vm.Widgets.Should().Contain(first, "the same row, not a rebuilt one");
        }

        [AvaloniaFact]
        public void A_module_that_is_off_and_never_touched_is_not_on_Home()
        {
            _ = Directory.CreateDirectory(Path.Combine(AppPaths.ModulesDirectory, ModuleIds.Chrono));
            MainViewModel vm = new();
            vm.NavigateCommand.Execute("Home");

            _ = vm.Widgets.Single(w => w.Id == ModuleIds.Chrono).IsOnHome.Should().BeFalse();
            _ = vm.HasNothingRunning.Should().BeTrue();
        }
    }
}
