using FluentAssertions;
using MosaicShell.Core.Install;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// First run (U25): one screen with the modules that were set up, already on. It must never show after an update:
    /// <c>DefaultModulesOffered</c> is new and reads false on every existing install, so an install that has saved any
    /// settings is not a first run.
    /// </summary>
    public sealed class FirstRunPolicyTests : IDisposable
    {
        private readonly string _config = Directory.CreateTempSubdirectory("mosaic-first-run-").FullName;

        public void Dispose()
        {
            Directory.Delete(_config, recursive: true);
        }

        [Theory]
        [InlineData(false, false, true)]
        [InlineData(false, true, false)]
        [InlineData(true, false, false)]
        [InlineData(true, true, false)]
        public void A_first_run_has_never_offered_the_defaults_and_saved_nothing(bool offered, bool saved, bool expected)
        {
            _ = FirstRunPolicy.IsFirstRun(defaultsOffered: offered, anySettingsSaved: saved).Should().Be(expected);
        }

        [Fact]
        public void An_empty_or_missing_config_folder_has_saved_nothing()
        {
            _ = FirstRunPolicy.AnySettingsSaved(_config).Should().BeFalse();
            _ = FirstRunPolicy.AnySettingsSaved(Path.Combine(_config, "missing")).Should().BeFalse();
        }

        [Fact]
        public void Any_saved_settings_file_counts_even_in_a_subfolder()
        {
            _ = Directory.CreateDirectory(Path.Combine(_config, "modules"));
            File.WriteAllText(Path.Combine(_config, "modules", "Tessera.json"), "{}");

            _ = FirstRunPolicy.AnySettingsSaved(_config).Should().BeTrue("an update keeps its settings and is no first run");
        }

        [Fact]
        public void The_defaults_that_are_installed_and_off_are_turned_on()
        {
            HashSet<string> installed = ["Tessera", "Mixdeck"];
            HashSet<string> on = ["Mixdeck"];

            _ = FirstRunPolicy.ToTurnOn(["Tessera", "Mixdeck", "Slate"], installed.Contains, on.Contains)
                .Should().Equal(["Tessera"], "Slate did not install, and Mixdeck is already on");
        }
    }
}
