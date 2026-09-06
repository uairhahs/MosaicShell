using FluentAssertions;
using MosaicShell.Core.Runtime;

namespace MosaicShell.Core.Tests
{
    public class TileRuntimeTests : IDisposable
    {
        private readonly string _home;
        private readonly FakeSurfaceHost _host = new();

        public TileRuntimeTests()
        {
            _home = Path.Combine(Path.GetTempPath(), "ms-runtime-" + Guid.NewGuid().ToString("N"));
            AppPaths.SetRootOverride(_home);
            AppPaths.EnsureLayout();
            _ = Directory.CreateDirectory(Path.Combine(AppPaths.ModulesDirectory, "Canvas"));
            _ = Directory.CreateDirectory(Path.Combine(AppPaths.ModulesDirectory, "Chrono"));
        }

        public void Dispose()
        {
            AppPaths.ClearRootOverride();
            try { Directory.Delete(_home, recursive: true); } catch { /* ignore */ }
        }

        [Fact]
        public void Start_not_installed_fails()
        {
            TileRuntime runtime = new(_host);
            ModuleLaunchResult result = runtime.Start("Tessera");
            _ = result.Started.Should().BeFalse();
            _ = result.Blocker.Should().Be(ModuleLaunchBlocker.NotInstalled);
            _ = _host.Shown.Should().BeEmpty();
        }

        [Fact]
        public void Start_shows_surface_and_tracks_session()
        {
            TileRuntime runtime = new(_host);
            ModuleLaunchResult result = runtime.Start("Canvas");
            _ = result.Started.Should().BeTrue();
            _ = result.Blocker.Should().Be(ModuleLaunchBlocker.None);
            _ = runtime.IsRunning("Canvas").Should().BeTrue();
            _ = runtime.Running.Select(s => s.ModuleId).Should().Equal("Canvas");
            _ = _host.Shown.Should().Equal("Canvas");
        }

        [Fact]
        public void Start_already_running_is_idempotent_focus()
        {
            TileRuntime runtime = new(_host);
            _ = runtime.Start("Canvas").Started.Should().BeTrue();
            ModuleLaunchResult again = runtime.Start("Canvas");
            _ = again.Started.Should().BeTrue();
            _ = again.Message.Should().Contain("already");
            _ = _host.Shown.Should().Equal("Canvas");
            _ = _host.Focused.Should().Contain("Canvas");
            _ = runtime.Running.Should().HaveCount(1);
        }

        [Fact]
        public void Stop_hides_surface_and_clears_session()
        {
            TileRuntime runtime = new(_host);
            _ = runtime.Start("Chrono");
            _ = runtime.Stop("Chrono").Should().BeTrue();
            _ = runtime.IsRunning("Chrono").Should().BeFalse();
            _ = _host.Closed.Should().Contain("Chrono");
        }

        [Fact]
        public void StopAll_closes_every_session()
        {
            TileRuntime runtime = new(_host);
            _ = runtime.Start("Canvas");
            _ = runtime.Start("Chrono");
            runtime.StopAll();
            _ = runtime.Running.Should().BeEmpty();
            _ = _host.Closed.Should().BeEquivalentTo("Canvas", "Chrono");
        }

        [Fact]
        public void Host_failure_reports_runtime_blocker()
        {
            _host.FailNext = true;
            TileRuntime runtime = new(_host);
            ModuleLaunchResult result = runtime.Start("Canvas");
            _ = result.Started.Should().BeFalse();
            _ = result.Blocker.Should().Be(ModuleLaunchBlocker.NativeRuntimeMissing);
            _ = runtime.IsRunning("Canvas").Should().BeFalse();
        }

        [Fact]
        public void ModuleLauncher_delegates_to_runtime()
        {
            TileRuntime runtime = new(_host);
            ModuleLauncher launcher = new(runtime);
            _ = launcher.TryLaunch("Canvas").Started.Should().BeTrue();
            _ = runtime.IsRunning("Canvas").Should().BeTrue();
        }

        [Fact]
        public void NotifySurfaceClosed_drops_session_without_host_close()
        {
            TileRuntime runtime = new(_host);
            _ = runtime.Start("Canvas");
            runtime.NotifySurfaceClosed("Canvas");
            _ = runtime.IsRunning("Canvas").Should().BeFalse();
            _ = _host.Closed.Should().BeEmpty();
        }

        private sealed class FakeSurfaceHost : ITileSurfaceHost
        {
            public List<string> Shown { get; } = [];
            public List<string> Focused { get; } = [];
            public List<string> Closed { get; } = [];
            public bool FailNext { get; set; }

            public bool Show(string moduleId, out string? error)
            {
                if (FailNext)
                {
                    FailNext = false;
                    error = "simulated host failure";
                    return false;
                }

                Shown.Add(moduleId);
                error = null;
                return true;
            }

            public void Focus(string moduleId)
            {
                Focused.Add(moduleId);
            }

            public void Close(string moduleId)
            {
                Closed.Add(moduleId);
            }
        }
    }
}
