using FluentAssertions;
using MosaicShell.Core.Modules;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// One on switch for every module. A capability is on while it is armed. A widget is on while it is on the desktop,
    /// which also brings it back each time MosaicShell starts (its saved session).
    /// </summary>
    public class ModuleOnSwitchTests
    {
        [Theory]
        [InlineData(ModuleKind.Capability, true, true, false, true)]
        [InlineData(ModuleKind.Capability, true, false, true, false)]
        [InlineData(ModuleKind.Hybrid, true, true, false, true)]
        [InlineData(ModuleKind.Widget, true, false, true, true)]
        [InlineData(ModuleKind.Widget, true, true, false, false)]
        [InlineData(ModuleKind.Capability, false, true, true, false)]
        [InlineData(ModuleKind.Widget, false, true, true, false)]
        public void A_module_is_on_while_it_runs_the_way_its_kind_does(
            ModuleKind kind, bool installed, bool armed, bool widgetOpen, bool on)
        {
            _ = ModuleOnSwitch.IsOn(kind, installed, armed, widgetOpen).Should().Be(on);
        }

        [Theory]
        [InlineData(ModuleKind.Capability, true, new[] { ModuleSwitchStep.Arm })]
        [InlineData(ModuleKind.Capability, false, new[] { ModuleSwitchStep.Install, ModuleSwitchStep.Arm })]
        [InlineData(ModuleKind.Hybrid, true, new[] { ModuleSwitchStep.Arm })]
        [InlineData(ModuleKind.Widget, true, new[] { ModuleSwitchStep.ShowWidget })]
        [InlineData(ModuleKind.Widget, false, new[] { ModuleSwitchStep.Install, ModuleSwitchStep.ShowWidget })]
        public void Turning_on_installs_first_when_needed(ModuleKind kind, bool installed, ModuleSwitchStep[] steps)
        {
            _ = ModuleOnSwitch.Plan(kind, installed, turnOn: true).Should().Equal(steps);
        }

        [Theory]
        [InlineData(ModuleKind.Capability, ModuleSwitchStep.Disarm)]
        [InlineData(ModuleKind.Hybrid, ModuleSwitchStep.Disarm)]
        [InlineData(ModuleKind.Widget, ModuleSwitchStep.CloseWidget)]
        public void Turning_off_stops_it_and_never_removes_it(ModuleKind kind, ModuleSwitchStep step)
        {
            _ = ModuleOnSwitch.Plan(kind, installed: true, turnOn: false).Should().Equal(step);
        }

        [Fact]
        public void Turning_off_a_module_that_is_not_installed_does_nothing()
        {
            _ = ModuleOnSwitch.Plan(ModuleKind.Widget, installed: false, turnOn: false).Should().BeEmpty();
        }

        [Fact]
        public void The_switch_is_labelled_with_the_modules_name()
        {
            _ = ModuleOnSwitch.Label("Chrono").Should().Be("Turn on Chrono");
        }

        [Fact]
        public void A_capability_says_it_runs_in_the_background()
        {
            _ = ModuleOnSwitch.Description(ModuleKind.Capability, signInStartOn: false)
                .Should().Be("Runs in the background while MosaicShell is running.");
        }

        [Fact]
        public void A_widget_says_it_comes_back_each_start()
        {
            _ = ModuleOnSwitch.Description(ModuleKind.Widget, signInStartOn: true)
                .Should().Be("Shows on the desktop now and every time MosaicShell starts.");
            _ = ModuleOnSwitch.OffersSignInStart(ModuleKind.Widget, signInStartOn: true).Should().BeFalse();
        }

        [Fact]
        public void Without_sign_in_start_a_widget_still_turns_on_and_says_when_it_comes_back()
        {
            _ = ModuleOnSwitch.Description(ModuleKind.Widget, signInStartOn: false).Should().Be(
                "Shows on the desktop now and every time MosaicShell starts. MosaicShell does not start when you sign in, "
                + "so after you sign in it shows once you open MosaicShell.");
            _ = ModuleOnSwitch.OffersSignInStart(ModuleKind.Widget, signInStartOn: false).Should().BeTrue();
            _ = ModuleOnSwitch.Plan(ModuleKind.Widget, installed: true, turnOn: true).Should().Equal(ModuleSwitchStep.ShowWidget);
        }

        [Fact]
        public void A_capability_never_offers_sign_in_start()
        {
            _ = ModuleOnSwitch.OffersSignInStart(ModuleKind.Capability, signInStartOn: false).Should().BeFalse();
        }
    }
}
