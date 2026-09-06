using FluentAssertions;
using MosaicShell.Core.Runtime;
using MosaicShell.Core.Settings;

namespace MosaicShell.Core.Tests
{
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
            IReadOnlyList<TileSessionState> loaded = SessionStore.Load();
            _ = loaded.Should().ContainSingle(s => s.ModuleId == "Chrono" && s.X == 10 && s.Y == 20);
        }

        [Fact]
        public void ModuleSettingsStore_roundtrips()
        {
            ChronoSettings settings = new() { Style = "Square", ShowSeconds = false };
            ModuleSettingsStore.Save("Chrono", settings);
            ChronoSettings loaded = ModuleSettingsStore.Load("Chrono", () => new ChronoSettings());
            _ = loaded.Style.Should().Be("Square");
            _ = loaded.ShowSeconds.Should().BeFalse();
        }

        [Theory]
        [InlineData("Tessera", /*lang=json,strict*/ """{"Style":"Pixel"}""", "MaterialYou")]
        [InlineData("Tessera", /*lang=json,strict*/ """{"Style":"Win11"}""", "Windows11")]
        [InlineData("Chrono", /*lang=json,strict*/ """{"Style":"Center","ShowSeconds":true}""", "Square")]
        [InlineData("Phono", /*lang=json,strict*/ """{"Style":"Simple","ShowArtist":true}""", "Compact")]
        [InlineData("Inlay", /*lang=json,strict*/ """{"Style":"Win11"}""", "Windows11")]
        public void ModuleSettingsStore_migrates_legacy_style_ids_on_load(
            string moduleId, string json, string expectedStyle)
        {
            string path = ModuleSettingsStore.PathFor(moduleId);
            _ = Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, json);

            string style = moduleId switch
            {
                "Tessera" => ModuleSettingsStore.Load(moduleId, () => new TesseraSettings()).Style,
                "Chrono" => ModuleSettingsStore.Load(moduleId, () => new ChronoSettings()).Style,
                "Phono" => ModuleSettingsStore.Load(moduleId, () => new PhonoSettings()).Style,
                "Inlay" => ModuleSettingsStore.Load(moduleId, () => new InlaySettings()).Style,
                _ => throw new ArgumentOutOfRangeException(nameof(moduleId))
            };

            _ = style.Should().Be(expectedStyle);
            _ = File.ReadAllText(path).Should().Contain($"\"Style\": \"{expectedStyle}\"");
        }

        [Fact]
        public void Uninstall_removes_module_dir_settings_and_session()
        {
            _ = Directory.CreateDirectory(Path.Combine(AppPaths.ModulesDirectory, "Canvas"));
            ModuleSettingsStore.Save("Canvas", new CanvasSettings());
            SessionStore.Save([new TileSessionState("Canvas", 0, 0, 100, 100)]);
            _ = ModuleUninstaller.Uninstall("Canvas").Should().BeTrue();
            _ = Directory.Exists(Path.Combine(AppPaths.ModulesDirectory, "Canvas")).Should().BeFalse();
            _ = File.Exists(ModuleSettingsStore.PathFor("Canvas")).Should().BeFalse();
            _ = SessionStore.Load().Should().BeEmpty();
        }

        [Fact]
        public void ModuleManifest_write_and_load()
        {
            _ = Directory.CreateDirectory(Path.Combine(AppPaths.ModulesDirectory, "Pulse"));
            ModuleManifest.WriteDefault("Pulse", "Pulse");
            ModuleManifest? m = ModuleManifest.TryLoad("Pulse");
            _ = m.Should().NotBeNull();
            _ = m.Id.Should().Be("Pulse");
        }
    }
}
