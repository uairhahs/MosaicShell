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
            _ = MainWindowXaml.Should().NotContain("Check catalog");
            _ = MainWindowXaml.Should().NotContain("Check GitHub releases");
            _ = Regex.Matches(MainWindowXaml, "Command=\"\\{Binding CheckUpdatesCommand\\}\"").Count.Should().Be(1);
            _ = MainWindowXaml.Should().Contain("Content=\"Check for updates\"");
        }

        [Fact]
        public void Clickable_surfaces_are_buttons_not_bordered_pointer_handlers()
        {
            _ = MainWindowXaml.Should().NotContain("PointerPressed=");
        }

        [Fact]
        public void A_library_row_opens_settings_through_one_focusable_named_button()
        {
            // The 16 px unfocusable cog duplicated the row body; the row body is now the one target.
            _ = MainWindowXaml.Should().NotContain("tileCog");
            _ = MainWindowXaml.Should().NotMatchRegex("\"Focusable\" Value=\"False\"");
            _ = Regex.Matches(MainWindowXaml, "<Button[^>]*Classes=\"libraryRow\"[^>]*OpenModuleConfigCommand[^>]*SettingsAutomationName", RegexOptions.Singleline)
                .Count.Should().Be(2);
        }

        [Fact]
        public void A_press_on_a_library_row_lights_the_whole_card()
        {
            // The press lit only the text column, the one control that was a button (user, 2026-11-26). The row
            // button now spans the card behind its content; the checkbox and the row's own buttons sit on top.
            MatchCollection rows = Regex.Matches(
                MainWindowXaml,
                "<Button[^>]*Classes=\"libraryRow\"[^>]*Grid.ColumnSpan=\"7\"[^>]*Grid.RowSpan=\"3\"[^>]*OpenModuleConfigCommand",
                RegexOptions.Singleline);
            _ = rows.Count.Should().Be(2);
            _ = MainWindowXaml.Should().NotContain("libraryHit");
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
        public void There_is_no_separate_welcome_page()
        {
            // Welcome repeated what Setup had done and what Home could say itself (user, 2026-11-26; audit U03, U25).
            _ = MainWindowXaml.Should().NotContain("IsWelcome");
            _ = MainWindowXaml.Should().NotContain("CompleteWelcomeCommand");
            _ = MainWindowXaml.Should().Contain("Your desktop, composed.");
        }

        [Fact]
        public void The_library_has_one_search_box_docked_under_the_lists()
        {
            // User, 2026-11-26: add a search box, docked to the bottom. Rows hide through the Core rule, never a view filter.
            Match box = Regex.Match(MainWindowXaml, "<TextBox[^>]*x:Name=\"LibrarySearchBox\"[^>]*>", RegexOptions.Singleline);
            _ = box.Success.Should().BeTrue();
            _ = box.Value.Should().Contain("DockPanel.Dock=\"Bottom\"");
            _ = box.Value.Should().Contain("{Binding LibrarySearchPlaceholder}");
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
            // User, 2026-11-26: with Reduce motion on (Settings) the motion settings change nothing, so they are disabled
            // and dimmed, with a notice and a way to Settings. Play on screen stays outside, to show the fade that plays.
            Match panel = Regex.Match(MainWindowXaml, "<StackPanel[^>]*x:Name=\"TesseraMotionSettings\"[^>]*>", RegexOptions.Singleline);
            _ = panel.Success.Should().BeTrue();
            _ = panel.Value.Should().Contain("IsEnabled=\"{Binding !IsReducedMotionInEffect}\"");
            _ = panel.Value.Should().Contain("Opacity=\"{Binding TesseraMotionSettingsOpacity}\"");
            _ = MainWindowXaml.Should().Contain("Text=\"{Binding ReducedMotionNotice}\"");

            int notice = MainWindowXaml.IndexOf("ReducedMotionNotice", StringComparison.Ordinal);
            int looks = MainWindowXaml.IndexOf("TesseraMotionPresetItems", StringComparison.Ordinal);
            int play = MainWindowXaml.IndexOf("PreviewTesseraFlyoutCommand", StringComparison.Ordinal);
            _ = panel.Index.Should().BeLessThan(looks);
            _ = play.Should().BeLessThan(panel.Index, "Play on screen is not greyed out");
            _ = notice.Should().BeLessThan(panel.Index);
        }

        [Fact]
        public void Remove_is_hidden_on_modules_that_are_not_installed()
        {
            Match remove = Regex.Match(MainWindowXaml, "<Button[^>]*RemoveModuleCommand[^>]*>", RegexOptions.Singleline);
            _ = remove.Success.Should().BeTrue();
            _ = remove.Value.Should().Contain("IsVisible=\"{Binding IsInstalled}\"");
            _ = MainWindowXaml.Should().NotContain("UninstallModuleCommand");
        }
    }
}
