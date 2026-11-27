using FluentAssertions;
using MosaicShell.Core.HostPlatform;
using MosaicShell.Core.HubShell;
using MosaicShell.Core.Modules;
using MosaicShell.Core.Runtime;
using MosaicShell.Core.Settings;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// The Hub's left rail and the one line each module shows on Home: installed modules are the navigation, grouped by
    /// where they run, and each says in plain words what it is doing now.
    /// </summary>
    public sealed class HubShellTests : IDisposable
    {
        private static readonly DesktopFeatures Plasma = DesktopFeatures.Resolve(false, "KDE");
        private static readonly DesktopFeatures Gnome = DesktopFeatures.Resolve(false, "GNOME");
        private readonly string _root;

        public HubShellTests()
        {
            _root = Path.Combine(Path.GetTempPath(), "MosaicShell_" + Guid.NewGuid().ToString("N"));
            AppPaths.SetRootOverride(_root);
            AppPaths.EnsureLayout();
        }

        public void Dispose()
        {
            AppPaths.ClearRootOverride();
            try { Directory.Delete(_root, true); } catch { /* ignore */ }
        }

        private static ModuleInfo Info(string id)
        {
            _ = ModuleCatalog.TryGet(id, out ModuleInfo? info).Should().BeTrue();
            return info!;
        }

        [Fact]
        public void The_rail_lists_installed_modules_grouped_by_where_they_run_in_catalog_order()
        {
            IReadOnlyList<RailEntry> entries = HubRail.Entries([Info("Chrono"), Info("Mixdeck"), Info("Tessera"), Info("Canvas")], query: null);

            _ = entries.Select(e => (e.Id, e.Group)).Should().Equal(
                ("Tessera", RailGroup.Background),
                ("Mixdeck", RailGroup.Background),
                ("Chrono", RailGroup.Desktop),
                ("Canvas", RailGroup.Desktop));
        }

        [Fact]
        public void The_rail_search_filters_by_name_and_description()
        {
            IReadOnlyList<RailEntry> entries = HubRail.Entries([Info("Tessera"), Info("Mixdeck"), Info("Chrono")], query: "mixer");

            _ = entries.Select(e => e.Id).Should().Equal("Mixdeck");
        }

        [Fact]
        public void Group_titles_never_promise_a_tray()
        {
            _ = HubRail.GroupTitle(RailGroup.Background).Should().Be("Running in the background");
            _ = HubRail.GroupTitle(RailGroup.Desktop).Should().Be("On the desktop");
        }

        [Theory]
        [InlineData(1100, false)]
        [InlineData(900, false)]
        [InlineData(899, true)]
        public void The_rail_folds_to_icons_on_a_narrow_window(double width, bool collapsed)
        {
            _ = HubRail.IsCollapsed(width).Should().Be(collapsed);
        }

        [Fact]
        public void Add_modules_lists_only_what_is_not_installed()
        {
            HashSet<string> installed = ["Tessera", "Chrono"];

            IReadOnlyList<ModuleInfo> addable = HubRail.Addable(ModuleCatalog.BuiltIns, installed.Contains);

            _ = addable.Select(m => m.Id).Should().NotContain(installed).And.Contain(["Mixdeck", "Canvas"]);
        }

        [Fact]
        public void A_module_that_is_off_says_so()
        {
            ModuleStatus status = ModuleStatusLine.For(Info("Mixdeck"), isOn: false, hotkeyError: null, Plasma);

            _ = status.Should().Be(new ModuleStatus("Off", IsProblem: false));
        }

        [Fact]
        public void Tessera_says_what_it_replaces()
        {
            _ = ModuleStatusLine.For(Info("Tessera"), true, null, Plasma).Line
                .Should().Be("Replacing the volume, brightness and media pop-ups");
        }

        [Theory]
        [InlineData("Mixdeck", "Ctrl+Alt+X opens the mixer")]
        [InlineData("Inlay", "Ctrl+Alt+I opens the launcher")]
        [InlineData("Chord", "Ctrl+Alt+K opens your actions")]
        [InlineData("Substrate", "Ctrl+Alt+Q opens the quick settings")]
        public void A_shortcut_module_names_its_shortcut_and_what_it_opens(string id, string line)
        {
            _ = ModuleStatusLine.For(Info(id), true, null, Plasma).Should().Be(new ModuleStatus(line, false));
        }

        [Fact]
        public void On_GNOME_a_shortcut_module_says_shortcuts_are_missing_without_a_problem_mark()
        {
            _ = ModuleStatusLine.For(Info("Mixdeck"), true, "Shortcuts are not available on GNOME yet.", Gnome)
                .Should().Be(new ModuleStatus("Shortcuts are not available on GNOME yet.", IsProblem: false));
        }

        [Fact]
        public void A_shortcut_another_app_holds_is_a_problem()
        {
            const string taken = "Could not register Ctrl+Alt+X (in use by a desktop shortcut or another app). Try Ctrl+Alt+Letter.";

            _ = ModuleStatusLine.For(Info("Mixdeck"), true, taken, Plasma).Should().Be(new ModuleStatus(taken, IsProblem: true));
        }

        [Theory]
        [InlineData(300, "Shows after 5 min without input")]
        [InlineData(90, "Shows after 90 s without input")]
        [InlineData(3600, "Shows after 60 min without input")]
        public void Slate_says_when_it_shows(int seconds, string line)
        {
            ModuleSettingsStore.Save("Slate", new SlateSettings { IdleSeconds = seconds });

            _ = ModuleStatusLine.For(Info("Slate"), true, null, Plasma).Line.Should().Be(line);
        }

        [Fact]
        public void A_widget_that_is_on_is_on_the_desktop()
        {
            _ = ModuleStatusLine.For(Info("Chrono"), true, null, Plasma).Line.Should().Be("On the desktop");
        }

        [Fact]
        public void No_status_line_uses_the_old_words()
        {
            string[] old = ["Arm", "arm", "capability", "Capability", "widget", "Widget", "tile", "Tile"];
            foreach (ModuleInfo info in ModuleCatalog.BuiltIns)
            {
                foreach (bool on in new[] { true, false })
                {
                    string line = ModuleStatusLine.For(info, on, null, Plasma).Line;
                    _ = old.Should().NotContain(w => line.Contains(w, StringComparison.Ordinal), line);
                }
            }
        }
    }
}
