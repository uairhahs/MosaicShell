using System.Text.RegularExpressions;
using FluentAssertions;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// Source ratchets for the Hub window (UX revamp step 1). Each one pins a fix so the old,
    /// misleading or inaccessible markup cannot come back.
    /// </summary>
    public partial class HubWindowRatchetTests
    {
        private static readonly string MainWindowXaml = File.ReadAllText(
            Path.Combine(SourceTree.HostRoot, "MosaicShell.Host", "Views", "MainWindow.axaml"));

        private static readonly string ModulePageXaml = File.ReadAllText(
            Path.Combine(SourceTree.HostRoot, "MosaicShell.Host", "Views", "ModulePageView.axaml"));

        [Fact]
        public void The_tray_menu_says_Quit_as_Settings_does()
        {
            // It said "Exit (stop tiles)", the old Hub's words; Settings' close choice is "Quit MosaicShell". The menu is
            // built in code from TrayMenu since step 6.
            string app = File.ReadAllText(Path.Combine(SourceTree.HostRoot, "MosaicShell.Host", "App.axaml"));
            string code = File.ReadAllText(Path.Combine(SourceTree.HostRoot, "MosaicShell.Host", "App.axaml.cs"));
            _ = app.Should().NotContainAny("tiles", "Exit (");
            _ = code.Should().Contain("new(TrayMenu.QuitLabel)").And.Contain("new(TrayMenu.OpenLabel)");
        }

        [Fact]
        public void No_list_on_a_scrolled_page_scrolls_its_selection_into_view()
        {
            // Tessera's position picker did, and the request bubbled up to the page: the page opened 42% down (2026-11-28).
            foreach (string xaml in new[] { MainWindowXaml, ModulePageXaml })
            {
                foreach (Match list in Regex.Matches(xaml, "<ListBox\\s[^>]*>"))
                {
                    _ = list.Value.Should().Contain("AutoScrollToSelectedItem=\"False\"");
                }
            }
        }

        [Fact]
        public void Data_location_is_read_from_AppPaths_not_written_as_a_Windows_literal()
        {
            _ = MainWindowXaml.Should().NotContain("%LocalAppData%");
            _ = MainWindowXaml.Should().Contain("{Binding DataFolderPath}");
        }

        [Fact]
        public void Autostart_is_a_labelled_toggle_not_a_raw_bool()
        {
            _ = MainWindowXaml.Should().NotContain("StringFormat=Autostart");
            _ = MainWindowXaml.Should().Contain("Start MosaicShell when I sign in");
        }

        [Fact]
        public void The_update_check_has_one_name_and_one_button()
        {
            // Settings has the one "Check for updates"; Home's banner offers Install, which runs the same command.
            _ = MainWindowXaml.Should().NotContain("Check catalog");
            _ = MainWindowXaml.Should().NotContain("Check GitHub releases");
            _ = Regex.Matches(MainWindowXaml, "Content=\"Check for updates\"").Count.Should().Be(1);
            _ = Regex.Matches(MainWindowXaml, "Command=\"\\{Binding CheckUpdatesCommand\\}\"").Count.Should().Be(2);
            _ = MainWindowXaml.Should().MatchRegex("<Button[^>]*Content=\"Install\"[^>]*CheckUpdatesCommand");
        }

        [Fact]
        public void Clickable_surfaces_are_buttons_not_bordered_pointer_handlers()
        {
            _ = MainWindowXaml.Should().NotContain("PointerPressed=");
        }

        [Fact]
        public void The_rail_has_one_entry_per_page_and_no_module_list()
        {
            // The rail is Home, Modules, Widgets, Settings and About; modules live on their own pages, not scattered
            // down the rail as well.
            int rail = MainWindowXaml.IndexOf("<!-- The rail:", StringComparison.Ordinal);
            int pages = MainWindowXaml.IndexOf("<!-- Pages -->", StringComparison.Ordinal);
            string railXaml = MainWindowXaml[rail..pages];
            foreach (string page in new[] { "Home", "Modules", "Widgets", "Settings", "About" })
            {
                _ = railXaml.Should().Contain($"CommandParameter=\"{page}\"");
            }

            _ = railXaml.Should().NotContain("ItemsControl");
            _ = railXaml.Should().NotContain("TextBox");
            _ = MainWindowXaml.Should().NotContain("tileCog");
            _ = MainWindowXaml.Should().NotMatchRegex("\"Focusable\" Value=\"False\"");
        }

        [Fact]
        public void Each_row_opens_its_page_through_one_named_button()
        {
            // Home's running rows and the two lists' rows: one Button across the row, named for the module's page.
            MatchCollection rows = Regex.Matches(
                MainWindowXaml,
                "<Button Classes=\"listRow\"[^>]*OpenModuleCommand[^>]*SettingsAutomationName",
                RegexOptions.Singleline);
            _ = rows.Count.Should().Be(4, "two running groups on Home, one row template per list");
            _ = MainWindowXaml.Should().NotContain("libraryHit");
            _ = MainWindowXaml.Should().NotContain("Classes=\"libraryRow\"");
        }

        [Fact]
        public void Settings_is_one_box_in_the_page_scroll_not_a_card_per_option()
        {
            // A card per option made a short page look long and busy (user, 2026-11-26). One box, grouped by headings
            // and dividers, scrolled by the page's own scroll viewer. The diagnostic probe sits last (audit U21).
            int start = MainWindowXaml.IndexOf("IsVisible=\"{Binding IsSettings}\"", StringComparison.Ordinal);
            int end = MainWindowXaml.IndexOf("<!-- Remove confirmation", StringComparison.Ordinal);
            _ = start.Should().BePositive();
            _ = end.Should().BeGreaterThan(start);
            string settings = MainWindowXaml[start..end];

            _ = Regex.Matches(settings, "Classes=\"section\"").Count.Should().Be(1);
            _ = Regex.Matches(settings, "<ScrollViewer").Count.Should().Be(1);
            _ = settings.IndexOf("ServiceProbe", StringComparison.Ordinal)
                .Should().BeGreaterThan(settings.IndexOf("DataFolderPath", StringComparison.Ordinal));
        }

        [Fact]
        public void Settings_runs_General_Updates_Browser_media_then_Diagnostics_folded()
        {
            int start = MainWindowXaml.IndexOf("IsVisible=\"{Binding IsSettings}\"", StringComparison.Ordinal);
            int end = MainWindowXaml.IndexOf("<!-- Remove confirmation", StringComparison.Ordinal);
            string settings = MainWindowXaml[start..end];

            int[] order = [.. new[] { "Text=\"General\"", "Text=\"Updates\"", "Text=\"Browser media\"", "Header=\"Diagnostics\"" }
                .Select(t => settings.IndexOf(t, StringComparison.Ordinal))];
            _ = order.Should().OnlyContain(i => i > 0).And.BeInAscendingOrder();
            _ = Regex.Match(settings, "<Expander Header=\"Diagnostics\"[^>]*>").Value.Should().Contain("IsExpanded=\"False\"");
            _ = settings.IndexOf("ServiceProbe", StringComparison.Ordinal).Should().BeGreaterThan(order[3], "the probe is diagnostics");

            // Closing is a real choice of two, each with what happens under it, in words from Core.
            _ = Regex.Matches(settings, "<RadioButton GroupName=\"HubClose\"").Count.Should().Be(2);
            _ = settings.Should().Contain("{Binding KeepRunningChoice.Consequence}").And.Contain("{Binding QuitChoice.Consequence}");
            _ = settings.Should().NotContain("Minimize to tray").And.NotContain("Exit application");
        }

        [Fact]
        public void There_is_no_separate_welcome_page()
        {
            // Welcome repeated what Setup had done and what Home could say itself (user, 2026-11-26; audit U03, U25).
            _ = MainWindowXaml.Should().NotContain("IsWelcome");
            _ = MainWindowXaml.Should().NotContain("CompleteWelcomeCommand");
            _ = MainWindowXaml.Should().Contain("Your desktop, composed.");
        }

        [Fact]
        public void Each_list_has_a_full_width_search_box_that_filters_through_the_Core_rule()
        {
            // The search box sits at the top of the Modules and Widgets pages, wide enough for its placeholder. Rows hide
            // through the Core rule (IsShownBySearch), never a view filter.
            int pages = MainWindowXaml.IndexOf("<!-- Pages -->", StringComparison.Ordinal);
            MatchCollection boxes = Regex.Matches(MainWindowXaml, "<TextBox Text=\"\\{Binding LibrarySearch\\}\"[^>]*>", RegexOptions.Singleline);
            _ = boxes.Count.Should().Be(2);
            _ = boxes.Should().OnlyContain(b => b.Index > pages && b.Value.Contains("{Binding LibrarySearchPlaceholder}"));
            _ = Regex.Matches(MainWindowXaml, "IsVisible=\"\\{Binding IsShownBySearch\\}\"").Count.Should().Be(2);
        }

        [Fact]
        public void The_hub_window_cannot_be_maximized()
        {
            // User, 2026-11-26: the full-screen toggle in the title bar goes; the Hub is a fixed-shape window. The native
            // caption reads CanMaximize; Avalonia's drawn title bar (Linux, 12.2) reads TitleBarDecorations, which also has
            // a separate full-screen button.
            _ = MainWindowXaml.Should().Contain("CanMaximize=\"False\"");
            _ = MainWindowXaml.Should().Contain("WindowDrawnDecorations.TitleBarDecorations=\"Title, MinimizeButton, CloseButton\"");
        }

        [Fact]
        public void Tessera_motion_settings_grey_out_and_say_why_while_reduced_motion_is_in_effect()
        {
            // With Reduce motion on (Settings) the motion settings change nothing, so they are disabled and dimmed, with
            // a notice and a way to Settings. Show it on screen stays outside, to show the fade that plays. The motion
            // settings are Tessera's Motion slot on the module page; the button is in the page's pinned header.
            Match slot = Regex.Match(ModulePageXaml, "<DataTemplate DataType=\"mp:TesseraMotionSlotViewModel\">.*?</DataTemplate>\\s*\\n\\s*<DataTemplate DataType=\"mp:TesseraPositionSlotViewModel\">", RegexOptions.Singleline);
            _ = slot.Success.Should().BeTrue();
            Match panel = Regex.Match(slot.Value, "<StackPanel[^>]*x:Name=\"TesseraMotionSettings\"[^>]*>", RegexOptions.Singleline);
            _ = panel.Success.Should().BeTrue();
            _ = panel.Value.Should().Contain("IsEnabled=\"{Binding !IsReducedMotionInEffect}\"");
            _ = panel.Value.Should().Contain("Opacity=\"{Binding TesseraMotionSettingsOpacity}\"");

            int notice = slot.Value.IndexOf("ReducedMotionNotice", StringComparison.Ordinal);
            int looks = slot.Value.IndexOf("TesseraMotionPresetItems", StringComparison.Ordinal);
            _ = notice.Should().BePositive().And.BeLessThan(panel.Index);
            _ = panel.Index.Should().BeLessThan(looks);
            _ = slot.Value.Should().NotContain("PreviewCommand", "Show it on screen is not greyed out with the motion settings");
            _ = ModulePageXaml.Should().Contain("Command=\"{Binding PreviewCommand}\"");
        }

        [Fact]
        public void Remove_is_on_the_module_page_only_never_on_a_list_of_modules_not_installed()
        {
            _ = MainWindowXaml.Should().NotContain("RemoveModuleCommand");
            _ = MainWindowXaml.Should().NotContain("UninstallModuleCommand");
            _ = ModulePageXaml.Should().Contain("Command=\"{Binding RemoveCommand}\"");
        }
    }
}
