using FluentAssertions;
using MosaicShell.Core.Runtime;

namespace MosaicShell.Core.Tests
{
    public class ModuleLauncherTests : IDisposable
    {
        private readonly string _home;

        public ModuleLauncherTests()
        {
            _home = Path.Combine(Path.GetTempPath(), "ms-launch-" + Guid.NewGuid().ToString("N"));
            AppPaths.SetRootOverride(_home);
            AppPaths.EnsureLayout();
        }

        public void Dispose()
        {
            AppPaths.ClearRootOverride();
            try { Directory.Delete(_home, recursive: true); } catch { /* ignore */ }
        }

        [Fact]
        public void TryLaunch_not_installed_reports_blocker()
        {
            RecordingHost host = new();
            ModuleLauncher launcher = new(new TileRuntime(host));
            ModuleLaunchResult result = launcher.TryLaunch("Canvas");
            _ = result.Started.Should().BeFalse();
            _ = result.Blocker.Should().Be(ModuleLaunchBlocker.NotInstalled);
        }

        [Fact]
        public void TryLaunch_installed_starts_via_runtime()
        {
            _ = Directory.CreateDirectory(Path.Combine(AppPaths.ModulesDirectory, "Canvas"));
            RecordingHost host = new();
            TileRuntime runtime = new(host);
            ModuleLauncher launcher = new(runtime);

            ModuleLaunchResult result = launcher.TryLaunch("Canvas");
            _ = result.Started.Should().BeTrue();
            _ = result.Blocker.Should().Be(ModuleLaunchBlocker.None);
            _ = result.Message.Should().NotContain("Rainmeter");
            _ = runtime.IsRunning("Canvas").Should().BeTrue();
            _ = host.Shown.Should().Contain("Canvas");
        }

        private sealed class RecordingHost : ITileSurfaceHost
        {
            public List<string> Shown { get; } = [];
            public bool Show(string moduleId, out string? error)
            {
                Shown.Add(moduleId);
                error = null;
                return true;
            }
            public void Focus(string moduleId) { }
            public void Close(string moduleId) { }
        }
    }
}
