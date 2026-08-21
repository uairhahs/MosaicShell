using FluentAssertions;
using MosaicShell.Core;
using MosaicShell.Core.Runtime;
using MosaicShell.Core.Settings;

namespace MosaicShell.Core.Tests;

public class SessionAndSettingsTests : IDisposable
{
    private readonly string _home;

    public SessionAndSettingsTests()
    {
        _home = Path.Combine(Path.GetTempPath(), "ms-sess-" + Guid.NewGuid().ToString("N"));
        AppPaths.SetRootOverride(_home);
        AppPaths.EnsureLayout();
    }

    public void Dispose()
    {
        AppPaths.ClearRootOverride();
        try { Directory.Delete(_home, recursive: true); } catch { /* ignore */ }
    }

    [Fact]
    public void SessionStore_roundtrips()
    {
        SessionStore.Save([new TileSessionState("Chrono", 10, 20, 360, 280)]);
        var loaded = SessionStore.Load();
        loaded.Should().ContainSingle(s => s.ModuleId == "Chrono" && s.X == 10 && s.Y == 20);
    }

    [Fact]
    public void ModuleSettingsStore_roundtrips()
    {
        var settings = new ChronoSettings { Style = "Center", ShowSeconds = false };
        ModuleSettingsStore.Save("Chrono", settings);
        var loaded = ModuleSettingsStore.Load("Chrono", () => new ChronoSettings());
        loaded.Style.Should().Be("Center");
        loaded.ShowSeconds.Should().BeFalse();
    }

    [Fact]
    public void Uninstall_removes_module_dir_settings_and_session()
    {
        Directory.CreateDirectory(Path.Combine(AppPaths.ModulesDirectory, "Canvas"));
        ModuleSettingsStore.Save("Canvas", new CanvasSettings());
        SessionStore.Save([new TileSessionState("Canvas", 0, 0, 100, 100)]);
        ModuleUninstaller.Uninstall("Canvas").Should().BeTrue();
        Directory.Exists(Path.Combine(AppPaths.ModulesDirectory, "Canvas")).Should().BeFalse();
        File.Exists(ModuleSettingsStore.PathFor("Canvas")).Should().BeFalse();
        SessionStore.Load().Should().BeEmpty();
    }

    [Fact]
    public void ModuleManifest_write_and_load()
    {
        Directory.CreateDirectory(Path.Combine(AppPaths.ModulesDirectory, "Pulse"));
        ModuleManifest.WriteDefault("Pulse", "Pulse");
        var m = ModuleManifest.TryLoad("Pulse");
        m.Should().NotBeNull();
        m!.Id.Should().Be("Pulse");
    }
}
