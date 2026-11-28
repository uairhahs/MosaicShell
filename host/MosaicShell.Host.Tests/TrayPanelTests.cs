using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FluentAssertions;
using MosaicShell.Core;
using MosaicShell.Core.Modules;
using MosaicShell.Core.Runtime;
using MosaicShell.Core.Services;
using MosaicShell.Host.ViewModels;
using MosaicShell.Host.Views;

namespace MosaicShell.Host.Tests
{
    /// <summary>
    /// The tray's quick panel (U24): a native tray menu closes on every click, so flipping several modules meant reopening
    /// it each time. The panel stays open while its switches are flipped.
    /// </summary>
    public sealed class TrayPanelTests : HeadlessHostTest
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

        private static (TrayPanel Panel, MainViewModel Vm, TileRuntime Runtime) Open()
        {
            foreach (string id in new[] { ModuleIds.Chrono, ModuleIds.Phono })
            {
                _ = Directory.CreateDirectory(Path.Combine(AppPaths.ModulesDirectory, id));
            }

            TileRuntime runtime = new(new ShowsEverything());
            MainViewModel vm = new(runtime, HostServicesFakes.Create(), null);
            TrayPanel panel = new() { DataContext = vm };
            panel.ShowByPointer();
            Dispatcher.UIThread.RunJobs();
            return (panel, vm, runtime);
        }

        private static List<ToggleSwitch> Switches(TrayPanel panel)
        {
            return [.. panel.GetVisualDescendants().OfType<ToggleSwitch>()];
        }

        [AvaloniaFact]
        public void It_lists_a_switch_per_installed_module()
        {
            (TrayPanel panel, _, _) = Open();

            _ = Switches(panel).Select(s => Avalonia.Automation.AutomationProperties.GetName(s)).Should().Equal("Chrono", "Phono");
        }

        [AvaloniaFact]
        public void Flipping_switches_leaves_the_panel_open()
        {
            (TrayPanel panel, _, TileRuntime runtime) = Open();

            foreach (ToggleSwitch toggle in Switches(panel))
            {
                toggle.Command!.Execute(toggle.CommandParameter);
                Dispatcher.UIThread.RunJobs();
            }

            _ = runtime.IsRunning(ModuleIds.Chrono).Should().BeTrue();
            _ = runtime.IsRunning(ModuleIds.Phono).Should().BeTrue();
            _ = panel.IsVisible.Should().BeTrue("the native menu closed on every click; the panel does not");
            _ = Switches(panel).Should().OnlyContain(s => s.IsChecked == true);
        }

        [AvaloniaFact]
        public void Esc_closes_it()
        {
            (TrayPanel panel, _, _) = Open();

            panel.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
            Dispatcher.UIThread.RunJobs();

            _ = panel.IsVisible.Should().BeFalse();
        }

        [AvaloniaFact]
        public void Open_asks_for_the_Hub_and_closes_the_panel()
        {
            (TrayPanel panel, _, _) = Open();
            int opened = 0;
            panel.OpenRequested += (_, _) => opened++;

            Button open = panel.GetVisualDescendants().OfType<Button>().Single(b => b.Content as string == "Open MosaicShell");
            open.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            Dispatcher.UIThread.RunJobs();

            _ = opened.Should().Be(1);
            _ = panel.IsVisible.Should().BeFalse();
        }
    }
}
