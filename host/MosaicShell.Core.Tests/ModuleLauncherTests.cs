using FluentAssertions;
using MosaicShell.Core;
using MosaicShell.Core.Runtime;

namespace MosaicShell.Core.Tests;

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
        var host = new RecordingHost();
        var launcher = new ModuleLauncher(new TileRuntime(host));
        var result = launcher.TryLaunch("Canvas");
        result.Started.Should().BeFalse();
        result.Blocker.Should().Be(ModuleLaunchBlocker.NotInstalled);
    }

    [Fact]
    public void TryLaunch_installed_starts_via_runtime()
    {
        Directory.CreateDirectory(Path.Combine(AppPaths.ModulesDirectory, "Canvas"));
        var host = new RecordingHost();
        var runtime = new TileRuntime(host);
        var launcher = new ModuleLauncher(runtime);

        var result = launcher.TryLaunch("Canvas");
        result.Started.Should().BeTrue();
        result.Blocker.Should().Be(ModuleLaunchBlocker.None);
        result.Message.Should().NotContain("Rainmeter");
        runtime.IsRunning("Canvas").Should().BeTrue();
        host.Shown.Should().Contain("Canvas");
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
