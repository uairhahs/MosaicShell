using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FluentAssertions;
using MosaicShell.Core;
using MosaicShell.Core.Modules;
using MosaicShell.Host.ViewModels;
using MosaicShell.Host.Views;

namespace MosaicShell.Host.Tests
{
    /// <summary>
    /// The keyboard pass of step 5 (2026-11-28): Tab did nothing when the Hub opened, and nothing again after a list row
    /// opened a page, since no control had focus and Tab had no place to start from. The rail also tabbed Settings and
    /// About before Home.
    /// </summary>
    public sealed class HubKeyboardTests : HeadlessHostTest
    {
        private static (MainWindow Window, MainViewModel Vm) Open()
        {
            MainViewModel vm = new();
            MainWindow window = new() { DataContext = vm, Width = 1116, Height = 760 };
            window.Show();
            Dispatcher.UIThread.RunJobs();
            return (window, vm);
        }

        private static string FocusedName(Window window)
        {
            object? focused = window.FocusManager?.GetFocusedElement();
            return focused is Control c ? AutomationName(c) : "(nothing)";
        }

        private static string AutomationName(Control c)
        {
            return Avalonia.Automation.AutomationProperties.GetName(c) ?? c.GetType().Name;
        }

        private static void Press(Window window, Key key, RawInputModifiers modifiers = RawInputModifiers.None)
        {
            window.KeyPress(key, modifiers, PhysicalKey.None, null);
            window.KeyRelease(key, modifiers, PhysicalKey.None, null);
            Dispatcher.UIThread.RunJobs();
        }

        [AvaloniaFact]
        public void Tab_with_nothing_focused_lands_on_the_first_control()
        {
            (MainWindow window, _) = Open();

            Press(window, Key.Tab);

            _ = window.FocusManager?.GetFocusedElement().Should().BeAssignableTo<Control>("Tab did nothing");
            _ = FocusedName(window).Should().Be("Home", "the rail comes first, and Home is its first item");
        }

        [AvaloniaFact]
        public void The_rail_tabs_in_reading_order()
        {
            (MainWindow window, _) = Open();
            Press(window, Key.Tab);

            List<string> order = [FocusedName(window)];
            for (int i = 0; i < 4; i++)
            {
                Press(window, Key.Tab);
                order.Add(FocusedName(window));
            }

            _ = order.Should().Equal("Home", "Modules", "Widgets", "Settings", "About");
        }

        [AvaloniaFact]
        public void Arrow_keys_move_along_the_rail()
        {
            (MainWindow window, _) = Open();
            Press(window, Key.Tab);

            Press(window, Key.Down);
            _ = FocusedName(window).Should().Be("Modules");
            Press(window, Key.Down);
            Press(window, Key.Down);
            _ = FocusedName(window).Should().Be("Settings", "the arrows cross from the pages to Settings and About");
            Press(window, Key.Up);
            _ = FocusedName(window).Should().Be("Widgets");
        }

        [AvaloniaFact]
        public void After_a_row_opens_a_page_Tab_continues_on_that_page()
        {
            _ = Directory.CreateDirectory(Path.Combine(AppPaths.ModulesDirectory, ModuleIds.Chrono));
            (MainWindow window, MainViewModel vm) = Open();
            vm.NavigateCommand.Execute("Widgets");
            Dispatcher.UIThread.RunJobs();
            LibraryItemViewModel chrono = vm.Widgets.Single(w => w.Id == ModuleIds.Chrono);

            // As with the mouse: the row that opens the page has focus, and goes away with the list.
            Button row = window.GetVisualDescendants().OfType<Button>()
                .Single(b => Avalonia.Automation.AutomationProperties.GetName(b) == "Chrono settings" && b.IsEffectivelyVisible);
            _ = row.Focus();
            vm.OpenModuleCommand.Execute(chrono);
            Dispatcher.UIThread.RunJobs();
            Press(window, Key.Tab);

            object? focused = window.FocusManager?.GetFocusedElement();
            _ = focused.Should().BeAssignableTo<Control>("Tab did nothing after the page changed");
            Control control = (Control)focused!;
            _ = control.FindAncestorOfType<ModulePageView>().Should().NotBeNull("focus starts on the page that just opened");
        }

        private static Button RowButton(Window window, string name)
        {
            return window.GetVisualDescendants().OfType<Button>()
                .Single(b => Avalonia.Automation.AutomationProperties.GetName(b) == name + " settings" && b.IsEffectivelyVisible);
        }

        [AvaloniaFact]
        public void Down_moves_from_one_card_to_the_next_on_a_page()
        {
            foreach (string id in new[] { ModuleIds.Chrono, ModuleIds.Phono })
            {
                _ = Directory.CreateDirectory(Path.Combine(AppPaths.ModulesDirectory, id));
            }

            (MainWindow window, MainViewModel vm) = Open();
            vm.NavigateCommand.Execute("Widgets");
            Dispatcher.UIThread.RunJobs();
            List<string> rows = [.. vm.Widgets.Where(w => w.IsShownBySearch).Select(w => w.Name)];
            _ = RowButton(window, rows[0]).Focus(NavigationMethod.Tab);

            Press(window, Key.Down);
            _ = FocusedName(window).Should().Be(rows[1] + " settings", "Down should reach the next card, not scroll the page");
            Press(window, Key.Up);
            _ = FocusedName(window).Should().Be(rows[0] + " settings");
        }

        [AvaloniaFact]
        public void Enter_on_a_card_puts_focus_on_the_page_it_opens()
        {
            _ = Directory.CreateDirectory(Path.Combine(AppPaths.ModulesDirectory, ModuleIds.Chrono));
            (MainWindow window, MainViewModel vm) = Open();
            vm.NavigateCommand.Execute("Widgets");
            Dispatcher.UIThread.RunJobs();
            _ = RowButton(window, "Chrono").Focus(NavigationMethod.Tab);

            Press(window, Key.Enter);

            _ = vm.ModulePage.Should().NotBeNull("Enter opens the card");
            object? focused = window.FocusManager?.GetFocusedElement();
            _ = focused.Should().BeAssignableTo<Control>("focus was left on the card that went away");
            _ = ((Control)focused!).FindAncestorOfType<ModulePageView>().Should().NotBeNull("focus moves onto the page that opened");
        }

        [AvaloniaFact]
        public void Right_from_the_rail_enters_the_page_and_Left_from_its_edge_comes_back()
        {
            foreach (string id in new[] { ModuleIds.Chrono, ModuleIds.Phono })
            {
                _ = Directory.CreateDirectory(Path.Combine(AppPaths.ModulesDirectory, id));
            }

            (MainWindow window, MainViewModel vm) = Open();
            vm.NavigateCommand.Execute("Widgets");
            Dispatcher.UIThread.RunJobs();
            string second = vm.Widgets.Where(w => w.IsShownBySearch).Select(w => w.Name).ElementAt(1);
            _ = RowButton(window, second).Focus(NavigationMethod.Tab);

            Press(window, Key.Left);
            _ = FocusedName(window).Should().Be("Widgets", "Left at the page's edge goes to the open page's rail item");

            Press(window, Key.Right);
            _ = FocusedName(window).Should().Be(second + " settings", "Right goes back to where focus was on the page");
        }

        [AvaloniaFact]
        public void Right_from_the_rail_with_no_earlier_place_starts_at_the_page_top()
        {
            (MainWindow window, MainViewModel vm) = Open();
            vm.NavigateCommand.Execute("Modules");
            Dispatcher.UIThread.RunJobs();
            Button modules = window.GetVisualDescendants().OfType<Button>()
                .Single(b => b.Classes.Contains("rail") && Avalonia.Automation.AutomationProperties.GetName(b) == "Modules");
            _ = modules.Focus(NavigationMethod.Tab);

            Press(window, Key.Right);

            object? focused = window.FocusManager?.GetFocusedElement();
            _ = focused.Should().BeAssignableTo<Control>();
            _ = FocusedName(window).Should().Be("Add a module from a file", "the first control Tab reaches on the Modules page");
        }

        [AvaloniaFact]
        public void Right_on_a_card_reaches_its_button_and_Left_comes_back()
        {
            _ = Directory.CreateDirectory(Path.Combine(AppPaths.ModulesDirectory, ModuleIds.Chrono));
            (MainWindow window, MainViewModel vm) = Open();
            vm.NavigateCommand.Execute("Widgets");
            Dispatcher.UIThread.RunJobs();
            LibraryItemViewModel chrono = vm.Widgets.Single(w => w.Id == ModuleIds.Chrono);
            _ = RowButton(window, "Chrono").Focus(NavigationMethod.Tab);

            Press(window, Key.Right);
            _ = FocusedName(window).Should().Be(chrono.RowActionAutomationName, "the card's Turn on button sits inside the card");

            Press(window, Key.Left);
            _ = FocusedName(window).Should().Be("Chrono settings");

            Press(window, Key.Left);
            _ = FocusedName(window).Should().Be("Widgets", "from the card itself, Left still goes to the rail");
        }
    }
}
