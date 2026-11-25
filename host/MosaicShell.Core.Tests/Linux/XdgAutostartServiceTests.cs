using FluentAssertions;
using MosaicShell.Core.Services.Linux;
using MosaicShell.Core.Services.Xdg;

namespace MosaicShell.Core.Tests.Linux
{
    /// <summary><see cref="XdgAutostartService"/> writing into a throwaway config folder.</summary>
    public sealed class XdgAutostartServiceTests : IDisposable
    {
        private readonly string _config = Directory.CreateTempSubdirectory("mosaicshell-autostart-").FullName;

        private string EntryPath => Path.Combine(_config, "autostart", XdgAutostartPolicy.EntryFileName);

        private XdgAutostartService Service()
        {
            return new XdgAutostartService(EntryPath, "\"/opt/MosaicShell/MosaicShell.Host\"");
        }

        public void Dispose()
        {
            Directory.Delete(_config, recursive: true);
        }

        [Fact]
        public void Off_until_enabled()
        {
            _ = Service().IsEnabled.Should().BeFalse();
        }

        [Fact]
        public void Enabling_writes_the_entry_creating_the_autostart_folder()
        {
            XdgAutostartService autostart = Service();

            autostart.SetEnabled(true);

            _ = autostart.IsEnabled.Should().BeTrue();
            _ = File.ReadAllText(EntryPath).Should().Contain("\nExec=\"/opt/MosaicShell/MosaicShell.Host\"\n");
        }

        [Fact]
        public void Disabling_removes_the_entry()
        {
            XdgAutostartService autostart = Service();
            autostart.SetEnabled(true);

            autostart.SetEnabled(false);

            _ = autostart.IsEnabled.Should().BeFalse();
            _ = File.Exists(EntryPath).Should().BeFalse();
        }

        [Fact]
        public void Disabling_when_off_is_harmless()
        {
            XdgAutostartService autostart = Service();
            autostart.SetEnabled(false);
            _ = autostart.IsEnabled.Should().BeFalse();
        }

        [Fact]
        public void An_entry_hidden_by_the_desktop_reads_as_off_and_enabling_restores_it()
        {
            _ = Directory.CreateDirectory(Path.GetDirectoryName(EntryPath)!);
            File.WriteAllText(EntryPath, "[Desktop Entry]\nType=Application\nExec=old\nHidden=true\n");
            XdgAutostartService autostart = Service();
            _ = autostart.IsEnabled.Should().BeFalse();

            autostart.SetEnabled(true);

            _ = autostart.IsEnabled.Should().BeTrue();
        }

        [Fact]
        public void For_the_current_process_names_this_host()
        {
            XdgAutostartService autostart = XdgAutostartService.ForCurrentProcess();
            _ = autostart.EntryPath.Should().EndWith("/autostart/" + XdgAutostartPolicy.EntryFileName);
            _ = autostart.Exec.Should().StartWith("\"");
        }
    }
}
