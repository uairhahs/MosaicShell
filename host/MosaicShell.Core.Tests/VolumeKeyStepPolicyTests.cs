using FluentAssertions;
using MosaicShell.Core.HostPlatform;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// Who decides how far one volume key press moves the volume: Tessera's own key hook on Windows, the desktop's own
    /// setting on GNOME and Plasma, where MosaicShell cannot intercept the keys.
    /// </summary>
    public class VolumeKeyStepPolicyTests
    {
        [Theory]
        [InlineData(true, null, VolumeKeyStepSource.OwnHook)]
        [InlineData(false, "KDE", VolumeKeyStepSource.Plasma)]
        [InlineData(false, "GNOME", VolumeKeyStepSource.Gnome)]
        [InlineData(false, "ubuntu:GNOME", VolumeKeyStepSource.Gnome)]
        [InlineData(false, "Hyprland", VolumeKeyStepSource.None)]
        [InlineData(false, null, VolumeKeyStepSource.None)]
        public void Each_desktop_has_one_owner_of_the_step(bool windows, string? desktop, VolumeKeyStepSource expected)
        {
            _ = VolumeKeyStepPolicy.Resolve(windows, desktop).Should().Be(expected);
        }

        [Theory]
        [InlineData(VolumeKeyStepSource.OwnHook, 1, 25)]
        [InlineData(VolumeKeyStepSource.Gnome, 1, 20)]
        [InlineData(VolumeKeyStepSource.Plasma, 1, 100)]
        public void Each_owner_takes_the_range_it_stores(VolumeKeyStepSource source, int min, int max)
        {
            _ = VolumeKeyStepPolicy.Range(source).Should().Be((min, max));
        }

        [Theory]
        [InlineData(VolumeKeyStepSource.Gnome, 0, 1)]
        [InlineData(VolumeKeyStepSource.Gnome, 30, 20)]
        [InlineData(VolumeKeyStepSource.Plasma, 30, 30)]
        [InlineData(VolumeKeyStepSource.OwnHook, 30, 25)]
        public void A_step_is_clamped_to_its_owners_range(VolumeKeyStepSource source, int asked, int stored)
        {
            _ = VolumeKeyStepPolicy.Clamp(source, asked).Should().Be(stored);
        }

        [Theory]
        [InlineData("6", 6)]
        [InlineData(" 12\n", 12)]
        [InlineData("int32 4", 4)]
        [InlineData("", null)]
        [InlineData(null, null)]
        [InlineData("six", null)]
        public void GNOME_step_is_read_from_what_gsettings_prints(string? printed, int? step)
        {
            _ = VolumeKeyStepPolicy.ParseGnome(printed).Should().Be(step);
        }

        [Theory]
        [InlineData("2", 2)]
        [InlineData("", 5)]
        [InlineData(null, 5)]
        [InlineData("x", 5)]
        public void Plasma_step_is_its_config_value_or_Plasmas_default_of_5(string? value, int step)
        {
            _ = VolumeKeyStepPolicy.ParsePlasma(value).Should().Be(step);
        }

        [Fact]
        public void Only_the_own_hook_has_an_on_switch()
        {
            _ = VolumeKeyStepPolicy.HasHookSwitch(VolumeKeyStepSource.OwnHook).Should().BeTrue();
            _ = VolumeKeyStepPolicy.HasHookSwitch(VolumeKeyStepSource.Gnome).Should().BeFalse();
            _ = VolumeKeyStepPolicy.HasHookSwitch(VolumeKeyStepSource.Plasma).Should().BeFalse();
        }

        [Theory]
        [InlineData(VolumeKeyStepSource.Gnome, "GNOME")]
        [InlineData(VolumeKeyStepSource.Plasma, "Plasma")]
        public void On_Linux_the_row_says_it_is_the_desktops_own_setting(VolumeKeyStepSource source, string desktop)
        {
            _ = VolumeKeyStepPolicy.Hint(source).Should().Contain(desktop);
        }

        [Fact]
        public void Where_nobody_owns_the_step_the_row_says_so()
        {
            _ = VolumeKeyStepPolicy.Hint(VolumeKeyStepSource.None).Should().Contain("not");
        }
    }
}
