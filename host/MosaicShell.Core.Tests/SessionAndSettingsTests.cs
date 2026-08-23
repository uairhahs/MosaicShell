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
        var settings = new ChronoSettings { Style = "Square", ShowSeconds = false };
        ModuleSettingsStore.Save("Chrono", settings);
        var loaded = ModuleSettingsStore.Load("Chrono", () => new ChronoSettings());
        loaded.Style.Should().Be("Square");
        loaded.ShowSeconds.Should().BeFalse();
    }

    [Theory]
    [InlineData("Tessera", """{"Style":"Pixel"}""", "MaterialYou")]
    [InlineData("Tessera", """{"Style":"Win11"}""", "Windows11")]
    [InlineData("Chrono", """{"Style":"Center","ShowSeconds":true}""", "Square")]
    [InlineData("Phono", """{"Style":"Simple","ShowArtist":true}""", "Compact")]
    [InlineData("Inlay", """{"Style":"Win11"}""", "Windows11")]
    public void ModuleSettingsStore_migrates_legacy_style_ids_on_load(
        string moduleId, string json, string expectedStyle)
    {
        var path = ModuleSettingsStore.PathFor(moduleId);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, json);

        var style = moduleId switch
        {
            "Tessera" => ModuleSettingsStore.Load(moduleId, () => new TesseraSettings()).Style,
            "Chrono" => ModuleSettingsStore.Load(moduleId, () => new ChronoSettings()).Style,
            "Phono" => ModuleSettingsStore.Load(moduleId, () => new PhonoSettings()).Style,
            "Inlay" => ModuleSettingsStore.Load(moduleId, () => new InlaySettings()).Style,
            _ => throw new ArgumentOutOfRangeException(nameof(moduleId))
        };

        style.Should().Be(expectedStyle);
        File.ReadAllText(path).Should().Contain($"\"Style\": \"{expectedStyle}\"");
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
