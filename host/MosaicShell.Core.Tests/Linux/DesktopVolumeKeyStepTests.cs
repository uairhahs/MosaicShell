using FluentAssertions;
using MosaicShell.Core.HostPlatform;
using MosaicShell.Core.Services.Linux;

namespace MosaicShell.Core.Tests.Linux
{
    /// <summary>The desktop's own volume key step, read and written through stand-ins for gsettings and KConfig.</summary>
    public sealed class DesktopVolumeKeyStepTests
    {
        [Fact]
        public void GNOME_reads_and_writes_media_keys_volume_step()
        {
            FakeGnomeSettings settings = new() { Value = "6" };
            GnomeVolumeKeyStep step = new(settings);

            _ = step.Read().Should().Be(6);
            _ = step.Write(3).Should().BeTrue();

            _ = settings.Writes.Should().Equal($"{VolumeKeyStepPolicy.GnomeSchema} {VolumeKeyStepPolicy.GnomeKey} 3");
        }

        [Fact]
        public void GNOME_never_gets_a_step_outside_its_range()
        {
            FakeGnomeSettings settings = new();

            _ = new GnomeVolumeKeyStep(settings).Write(50).Should().BeTrue();

            _ = settings.Writes.Single().Should().EndWith(" 20");
        }

        [Fact]
        public void GNOME_without_gsettings_has_no_step_to_read()
        {
            _ = new GnomeVolumeKeyStep(new FakeGnomeSettings { Value = null }).Read().Should().BeNull();
        }

        [Fact]
        public void Plasma_reads_and_writes_plasmaparc_and_tells_Plasma()
        {
            FakeKdeConfig config = new() { Value = "2" };
            PlasmaVolumeKeyStep step = new(config);

            _ = step.Read().Should().Be(2);
            _ = step.Write(7).Should().BeTrue();

            _ = config.Writes.Should().Equal(
                $"{VolumeKeyStepPolicy.PlasmaFile} {VolumeKeyStepPolicy.PlasmaGroup} {VolumeKeyStepPolicy.PlasmaKey} 7");
        }

        [Fact]
        public void Plasma_with_no_entry_reads_its_default()
        {
            _ = new PlasmaVolumeKeyStep(new FakeKdeConfig { Value = "" }).Read().Should().Be(5);
        }

        [Fact]
        public void Plasma_without_its_config_tools_has_no_step_to_read()
        {
            _ = new PlasmaVolumeKeyStep(new FakeKdeConfig { Value = null }).Read().Should().BeNull();
        }

        private sealed class FakeGnomeSettings : IGnomeSettings
        {
            public string? Value { get; set; } = "6";
            public List<string> Writes { get; } = [];

            public string? Get(string schema, string key)
            {
                return schema == VolumeKeyStepPolicy.GnomeSchema && key == VolumeKeyStepPolicy.GnomeKey ? Value : null;
            }

            public bool Set(string schema, string key, string value)
            {
                Writes.Add($"{schema} {key} {value}");
                return true;
            }
        }

        private sealed class FakeKdeConfig : IKdeConfig
        {
            public string? Value { get; set; } = "";
            public List<string> Writes { get; } = [];

            public string? Read(string file, string group, string key)
            {
                return file == VolumeKeyStepPolicy.PlasmaFile && group == VolumeKeyStepPolicy.PlasmaGroup && key == VolumeKeyStepPolicy.PlasmaKey
                    ? Value
                    : null;
            }

            public bool Write(string file, string group, string key, string value)
            {
                Writes.Add($"{file} {group} {key} {value}");
                return true;
            }
        }
    }
}
