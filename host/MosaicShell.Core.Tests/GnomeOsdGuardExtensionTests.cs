using System.Text.Json;
using FluentAssertions;
using MosaicShell.Core.HubShell;
using MosaicShell.Core.Services.Gnome;
using MosaicShell.Core.Services.Kde;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// The GNOME Shell extension MosaicShell installs to hide Shell's volume and brightness pop-ups while Tessera shows
    /// its own. GNOME has no API for it (Shell exposes ShowOSD only), and every pop-up in GNOME 51 passes through
    /// OsdWindowManager._showOsdWindow(monitorIndex, icon, label, level, maxLevel), so the extension wraps that one
    /// method while MosaicShell owns the same guard name the KDE route uses.
    /// </summary>
    public sealed class GnomeOsdGuardExtensionTests
    {
        [Fact]
        public void The_extension_has_a_GNOME_style_uuid_and_installs_under_the_users_extensions()
        {
            _ = GnomeOsdGuardExtension.Uuid.Should().Contain("@");
            _ = GnomeOsdGuardExtension.InstallDirectory("/home/u/.local/share")
                .Should().Be("/home/u/.local/share/gnome-shell/extensions/" + GnomeOsdGuardExtension.Uuid);
        }

        [Fact]
        public void The_metadata_names_the_uuid_and_the_shell_versions_it_runs_on()
        {
            using JsonDocument doc = JsonDocument.Parse(GnomeOsdGuardExtension.MetadataJson);

            _ = doc.RootElement.GetProperty("uuid").GetString().Should().Be(GnomeOsdGuardExtension.Uuid);
            string?[] versions = [.. doc.RootElement.GetProperty("shell-version").EnumerateArray().Select(static v => v.GetString())];
            _ = versions.Should().Contain("51", "taizo runs GNOME 51");
            _ = versions.Should().Contain("45", "ES module extensions start at 45");
        }

        [Fact]
        public void The_script_hooks_the_one_pop_up_method_while_the_guard_name_is_owned()
        {
            string js = GnomeOsdGuardExtension.ExtensionJs;

            _ = js.Should().Contain("export default class");
            _ = js.Should().Contain("_showOsdWindow");
            _ = js.Should().Contain(KWinOsdScriptPolicy.GuardBusName, "the same guard name as the KDE route");
            foreach (string prefix in GnomeOsdGuardExtension.HiddenIconPrefixes)
            {
                _ = js.Should().Contain(prefix);
            }

            _ = js.Should().Contain("disable()", "it must put Shell's method back");
        }

        [Theory]
        [InlineData("audio-volume-high-symbolic", true)]
        [InlineData("audio-volume-muted-symbolic", true)]
        [InlineData("display-brightness-symbolic", true)]
        [InlineData("microphone-sensitivity-muted-symbolic", false)]
        [InlineData("keyboard-brightness-symbolic", false)]
        [InlineData("audio-headphones-symbolic", false)]
        public void Only_the_pop_ups_Tessera_replaces_are_hidden(string iconName, bool hidden)
        {
            // As on KDE: volume and brightness; the microphone and keyboard backlight pop-ups stay.
            _ = GnomeOsdGuardExtension.HidesIcon(iconName).Should().Be(hidden);
        }

        [Theory]
        [InlineData("@as []", "['" + GnomeOsdGuardExtension.Uuid + "']")]
        [InlineData("['a@b']", "['a@b', '" + GnomeOsdGuardExtension.Uuid + "']")]
        [InlineData("['a@b', 'c@d']", "['a@b', 'c@d', '" + GnomeOsdGuardExtension.Uuid + "']")]
        public void Enabling_adds_the_uuid_to_GNOMEs_list(string current, string expected)
        {
            _ = GnomeOsdGuardExtension.EnabledListWithUuid(current).Should().Be(expected);
        }

        [Fact]
        public void An_already_enabled_extension_leaves_the_list_alone()
        {
            _ = GnomeOsdGuardExtension.EnabledListWithUuid("['a@b', '" + GnomeOsdGuardExtension.Uuid + "']").Should().BeNull();
        }

        [Fact]
        public void An_unreadable_list_is_not_overwritten()
        {
            _ = GnomeOsdGuardExtension.EnabledListWithUuid("not a list").Should().BeNull();
            _ = GnomeOsdGuardExtension.EnabledListWithUuid(null).Should().BeNull();
        }

        [Fact]
        public void The_script_shows_MosaicShell_in_the_top_bar_while_the_Host_runs()
        {
            // GNOME has no tray without an extension, so closing the Hub left no way back. The same extension puts an
            // icon in the top bar while the Host owns its presence name; a closed or crashed Host takes the icon away.
            string js = GnomeOsdGuardExtension.ExtensionJs;

            _ = js.Should().Contain(JsonSerializer.Serialize(GnomeHubPresencePolicy.BusName));
            _ = js.Should().Contain(JsonSerializer.Serialize(GnomeHubPresencePolicy.ObjectPath));
            _ = js.Should().Contain(JsonSerializer.Serialize(GnomeHubPresencePolicy.Interface));
            _ = js.Should().Contain(JsonSerializer.Serialize(GnomeHubPresencePolicy.ShowHubMethod));
            _ = js.Should().Contain(JsonSerializer.Serialize(GnomeHubPresencePolicy.QuitMethod));
            _ = js.Should().Contain("PanelMenu.Button");
            _ = js.Should().Contain("Main.panel.addToStatusArea");
            _ = js.Should().Contain("Open MosaicShell").And.Contain("Quit MosaicShell");
            _ = js.Should().Contain("this._indicator?.destroy()", "disable takes the icon away");
        }

        [Fact]
        public void The_top_bar_icon_ships_with_the_extension_and_recolours_like_Shells_own()
        {
            // A themed icon name needs MosaicShell's icons installed, so without them Shell drew a generic grid. The
            // logo travels in the extension's folder instead, as a symbolic SVG so Shell tints it like every panel icon.
            _ = GnomeOsdGuardExtension.PanelIconFile.Should().EndWith("-symbolic.svg");
            _ = GnomeOsdGuardExtension.ExtensionJs.Should().Contain("Gio.FileIcon")
                .And.Contain(JsonSerializer.Serialize(GnomeOsdGuardExtension.PanelIconFile))
                .And.NotContain("view-grid-symbolic");

            System.Xml.Linq.XElement svg = System.Xml.Linq.XElement.Parse(GnomeOsdGuardExtension.PanelIconSvg);
            _ = svg.Name.LocalName.Should().Be("svg");
            _ = svg.Attribute("viewBox")!.Value.Should().Be("0 0 16 16");
        }

        [Fact]
        public void The_metadata_says_it_also_shows_MosaicShell_in_the_top_bar()
        {
            using JsonDocument doc = JsonDocument.Parse(GnomeOsdGuardExtension.MetadataJson);

            _ = doc.RootElement.GetProperty("description").GetString().Should().Contain("top bar");
            _ = doc.RootElement.GetProperty("uuid").GetString().Should().Be("osd-guard@mosaicshell", "an update replaces the installed copy");
        }

        [Fact]
        public void The_top_bar_menu_has_a_switch_per_module_read_from_the_Host_when_it_opens()
        {
            // U24: modules turn on and off from the top bar, GNOME's stand-in for a tray.
            string js = GnomeOsdGuardExtension.ExtensionJs;

            _ = js.Should().Contain("import GLib from 'gi://GLib';");
            _ = js.Should().Contain($"const MODULES = \"{GnomeHubPresencePolicy.ModulesMethod}\";");
            _ = js.Should().Contain($"const SET_MODULE_ON = \"{GnomeHubPresencePolicy.SetModuleOnMethod}\";");
            _ = js.Should().Contain("'open-state-changed'").And.Contain("this._fillModules()");
            _ = js.Should().Contain("new GLib.VariantType('(a(ssb))')");
            _ = js.Should().Contain("new PopupMenu.PopupSwitchMenuItem(label, on)");
            _ = js.Should().Contain("callHub(SET_MODULE_ON, new GLib.Variant('(sb)', [id, state]))");
            _ = js.Should().Contain($"const OPEN_LABEL = \"{TrayMenu.OpenLabel}\";").And.Contain($"const QUIT_LABEL = \"{TrayMenu.QuitLabel}\";");
        }

        [Fact]
        public void The_Hosts_presence_is_its_own_name_not_the_pop_up_guard()
        {
            // The guard name is owned only while Tessera is on; the icon must stay while the Host runs at all.
            _ = GnomeHubPresencePolicy.BusName.Should().Be("org.mosaicshell.Hub");
            _ = GnomeHubPresencePolicy.BusName.Should().NotBe(KWinOsdScriptPolicy.GuardBusName);
            _ = GnomeHubPresencePolicy.ObjectPath.Should().Be("/org/mosaicshell/Hub");
            _ = GnomeHubPresencePolicy.ShowHubMethod.Should().Be("ShowHub");
            _ = GnomeHubPresencePolicy.QuitMethod.Should().Be("Quit");
        }
    }
}
