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
            _ = Regex.Matches(MainWindowXaml, "<Button[^>]*Classes=\"libraryHit\"[^>]*OpenModuleConfigCommand[^>]*SettingsAutomationName", RegexOptions.Singleline)
                .Count.Should().Be(2);
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
