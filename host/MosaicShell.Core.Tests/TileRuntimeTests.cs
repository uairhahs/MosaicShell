using FluentAssertions;
using MosaicShell.Core;
using MosaicShell.Core.Runtime;

namespace MosaicShell.Core.Tests;

public class TileRuntimeTests : IDisposable
{
    private readonly string _home;
    private readonly FakeSurfaceHost _host = new();

    public TileRuntimeTests()
    {
        _home = Path.Combine(Path.GetTempPath(), "ms-runtime-" + Guid.NewGuid().ToString("N"));
        AppPaths.SetRootOverride(_home);
        AppPaths.EnsureLayout();
        Directory.CreateDirectory(Path.Combine(AppPaths.ModulesDirectory, "Canvas"));
        Directory.CreateDirectory(Path.Combine(AppPaths.ModulesDirectory, "Chrono"));
    }

    public void Dispose()
    {
        AppPaths.ClearRootOverride();
        try { Directory.Delete(_home, recursive: true); } catch { /* ignore */ }
    }

    [Fact]
    public void Start_not_installed_fails()
    {
        var runtime = new TileRuntime(_host);
        var result = runtime.Start("Tessera");
        result.Started.Should().BeFalse();
        result.Blocker.Should().Be(ModuleLaunchBlocker.NotInstalled);
        _host.Shown.Should().BeEmpty();
    }

    [Fact]
    public void Start_shows_surface_and_tracks_session()
    {
        var runtime = new TileRuntime(_host);
        var result = runtime.Start("Canvas");
        result.Started.Should().BeTrue();
        result.Blocker.Should().Be(ModuleLaunchBlocker.None);
        runtime.IsRunning("Canvas").Should().BeTrue();
        runtime.Running.Select(s => s.ModuleId).Should().Equal("Canvas");
        _host.Shown.Should().Equal("Canvas");
    }

    [Fact]
    public void Start_already_running_is_idempotent_focus()
    {
        var runtime = new TileRuntime(_host);
        runtime.Start("Canvas").Started.Should().BeTrue();
        var again = runtime.Start("Canvas");
        again.Started.Should().BeTrue();
        again.Message.Should().Contain("already");
        _host.Shown.Should().Equal("Canvas");
        _host.Focused.Should().Contain("Canvas");
        runtime.Running.Should().HaveCount(1);
    }

    [Fact]
    public void Stop_hides_surface_and_clears_session()
    {
        var runtime = new TileRuntime(_host);
        runtime.Start("Chrono");
        runtime.Stop("Chrono").Should().BeTrue();
        runtime.IsRunning("Chrono").Should().BeFalse();
        _host.Closed.Should().Contain("Chrono");
    }

    [Fact]
    public void StopAll_closes_every_session()
    {
        var runtime = new TileRuntime(_host);
        runtime.Start("Canvas");
        runtime.Start("Chrono");
        runtime.StopAll();
        runtime.Running.Should().BeEmpty();
        _host.Closed.Should().BeEquivalentTo("Canvas", "Chrono");
    }

    [Fact]
    public void Host_failure_reports_runtime_blocker()
    {
        _host.FailNext = true;
        var runtime = new TileRuntime(_host);
        var result = runtime.Start("Canvas");
        result.Started.Should().BeFalse();
        result.Blocker.Should().Be(ModuleLaunchBlocker.NativeRuntimeMissing);
        runtime.IsRunning("Canvas").Should().BeFalse();
    }

    [Fact]
    public void ModuleLauncher_delegates_to_runtime()
    {
        var runtime = new TileRuntime(_host);
        var launcher = new ModuleLauncher(runtime);
        launcher.TryLaunch("Canvas").Started.Should().BeTrue();
        runtime.IsRunning("Canvas").Should().BeTrue();
    }

    [Fact]
    public void NotifySurfaceClosed_drops_session_without_host_close()
    {
        var runtime = new TileRuntime(_host);
        runtime.Start("Canvas");
        runtime.NotifySurfaceClosed("Canvas");
        runtime.IsRunning("Canvas").Should().BeFalse();
        _host.Closed.Should().BeEmpty();
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

        public void Focus(string moduleId) => Focused.Add(moduleId);

        public void Close(string moduleId) => Closed.Add(moduleId);
    }
}
