using System.Text.Json;
using FluentAssertions;
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
    }
}
