using FluentAssertions;
using MosaicShell.Core.Install;
using MosaicShell.Core.Settings;

namespace MosaicShell.Core.Tests
{
    public class DefaultModulesPolicyTests
    {
        [Fact]
        public void A_first_run_installs_the_defaults()
        {
            _ = DefaultModulesPolicy.ToInstall(alreadyOffered: false, anyModuleInstalled: false)
                .Should().Equal(HostInstallPolicy.DefaultModuleIds);
            _ = HostInstallPolicy.DefaultModuleIds.Should().Equal("Tessera", "Mixdeck");
        }

        [Fact]
        public void An_update_never_brings_back_a_module_the_user_removed()
        {
            // Existing installs had their defaults from the installer: any installed module means this is not a first run.
            _ = DefaultModulesPolicy.ToInstall(alreadyOffered: false, anyModuleInstalled: true).Should().BeEmpty();
            _ = DefaultModulesPolicy.ToInstall(alreadyOffered: true, anyModuleInstalled: false).Should().BeEmpty();
        }

        [Fact]
        public void The_defaults_are_offered_once_per_settings_folder()
        {
            _ = new HubSettings().DefaultModulesOffered.Should().BeFalse();
        }

        [Fact]
        public void A_failure_names_the_modules_and_where_to_add_them()
        {
            _ = DefaultModulesPolicy.FailedStatus(["Tessera", "Mixdeck"]).Should().Be("Could not add Tessera and Mixdeck. Add them from Modules.");
        }
    }
}
