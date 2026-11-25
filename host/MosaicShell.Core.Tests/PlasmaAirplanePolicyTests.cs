using FluentAssertions;
using MosaicShell.Core.Services.Kde;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// Plasma's own airplane mode: the <c>AirplaneModeEnabled</c> key plasma-nm keeps in <c>~/.config/plasma-nm</c>, and
    /// the KConfig change signal it sends when the key is written, in the shapes captured live on Plasma 6.
    /// </summary>
    public sealed class PlasmaAirplanePolicyTests
    {
        [Fact]
        public void The_file_is_plasma_nm_under_the_config_home()
        {
            _ = PlasmaAirplanePolicy.ConfigPath(null, "/home/u").Should().Be("/home/u/.config/plasma-nm");
            _ = PlasmaAirplanePolicy.ConfigPath("/x/cfg/", "/home/u").Should().Be("/x/cfg/plasma-nm");
            _ = PlasmaAirplanePolicy.ConfigPath("relative", "/home/u/").Should().Be("/home/u/.config/plasma-nm", "XDG ignores a relative config home");
        }

        [Fact]
        public void The_key_in_the_General_group_is_the_state()
        {
            const string on = "[General]\nAirplaneModeEnabled=true\nHotspotName=x\n";
            const string off = "[General]\nAirplaneModeEnabled=false\n";

            _ = PlasmaAirplanePolicy.ReadEnabled(on).Should().BeTrue();
            _ = PlasmaAirplanePolicy.ReadEnabled(off).Should().BeFalse();
        }

        [Theory]
        [InlineData("true", true)]
        [InlineData("True", true)]
        [InlineData("on", true)]
        [InlineData("yes", true)]
        [InlineData("1", true)]
        [InlineData(" true ", true)]
        [InlineData("false", false)]
        [InlineData("0", false)]
        [InlineData("", false)]
        public void Values_read_as_KConfig_reads_a_bool(string value, bool expected)
        {
            _ = PlasmaAirplanePolicy.ReadEnabled("[General]\nAirplaneModeEnabled=" + value + "\n").Should().Be(expected);
        }

        [Fact]
        public void The_key_in_another_group_does_not_count()
        {
            _ = PlasmaAirplanePolicy.ReadEnabled("[Other]\nAirplaneModeEnabled=true\n[General]\nHotspotName=x\n").Should().BeFalse();
        }

        [Fact]
        public void An_immutable_marker_on_the_key_still_reads_its_value()
        {
            _ = PlasmaAirplanePolicy.ReadEnabled("[General]\nAirplaneModeEnabled[$i]=true\n").Should().BeTrue();
        }

        [Fact]
        public void No_file_or_no_key_means_off_as_plasma_nm_defaults_it()
        {
            _ = PlasmaAirplanePolicy.ReadEnabled(null).Should().BeFalse();
            _ = PlasmaAirplanePolicy.ReadEnabled("").Should().BeFalse();
            _ = PlasmaAirplanePolicy.ReadEnabled("[General]\nHotspotName=x\n").Should().BeFalse();
        }

        [Fact]
        public void Windows_line_endings_and_comments_are_tolerated()
        {
            _ = PlasmaAirplanePolicy.ReadEnabled("# note\r\n[General]\r\nAirplaneModeEnabled=true\r\n").Should().BeTrue();
        }

        [Fact]
        public void Only_a_change_naming_the_key_in_General_is_a_change_of_airplane_mode()
        {
            _ = PlasmaAirplanePolicy.Touches("General", ["AirplaneModeEnabled"]).Should().BeTrue();
            _ = PlasmaAirplanePolicy.Touches("General", ["HotspotName", "AirplaneModeEnabled"]).Should().BeTrue();
            _ = PlasmaAirplanePolicy.Touches("General", ["HotspotName"]).Should().BeFalse();
            _ = PlasmaAirplanePolicy.Touches("Other", ["AirplaneModeEnabled"]).Should().BeFalse();
        }

        [Fact]
        public void The_signal_is_KConfigs_notify_on_the_files_path()
        {
            _ = PlasmaAirplanePolicy.NotifyPath.Should().Be("/plasma_nm");
            _ = PlasmaAirplanePolicy.NotifyInterface.Should().Be("org.kde.kconfig.notify");
            _ = PlasmaAirplanePolicy.NotifyMember.Should().Be("ConfigChanged");
        }

        [Theory]
        [InlineData("KDE", true)]
        [InlineData("ubuntu:KDE", true)]
        [InlineData("GNOME", false)]
        [InlineData("ubuntu:GNOME", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void Plasma_is_the_session_when_XDG_CURRENT_DESKTOP_names_KDE(string? desktop, bool expected)
        {
            _ = PlasmaAirplanePolicy.IsPlasmaSession(desktop).Should().Be(expected);
        }
    }
}
