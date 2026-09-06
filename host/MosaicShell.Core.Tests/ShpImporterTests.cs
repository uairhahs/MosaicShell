using System.IO.Compression;
using FluentAssertions;
using MosaicShell.Core.Shp;

namespace MosaicShell.Core.Tests
{
    public class ShpImporterTests : IDisposable
    {
        private readonly string _home;
        private readonly string _work;

        public ShpImporterTests()
        {
            _home = Path.Combine(Path.GetTempPath(), "ms-shp-" + Guid.NewGuid().ToString("N"));
            _work = Path.Combine(Path.GetTempPath(), "ms-shp-work-" + Guid.NewGuid().ToString("N"));
            AppPaths.SetRootOverride(_home);
            AppPaths.EnsureLayout();
            _ = Directory.CreateDirectory(_work);
        }

        public void Dispose()
        {
            AppPaths.ClearRootOverride();
            try { Directory.Delete(_home, recursive: true); } catch { /* ignore */ }
            try { Directory.Delete(_work, recursive: true); } catch { /* ignore */ }
        }

        [Fact]
        public void Import_applies_module_settings_and_wallpaper()
        {
            string shp = CreateSampleShp();
            ShpImportResult result = ShpImporter.Import(shp);
            _ = result.Success.Should().BeTrue();
            _ = result.ImportedModules.Should().Contain("Chrono");
            _ = File.Exists(Path.Combine(AppPaths.ConfigDirectory, "modules", "Chrono.json")).Should().BeTrue();
            _ = Directory.EnumerateFiles(Path.Combine(AppPaths.ConfigDirectory, "Wallpaper")).Should().NotBeEmpty();
        }

        private string CreateSampleShp()
        {
            string root = Path.Combine(_work, "pkg");
            _ = Directory.CreateDirectory(Path.Combine(root, "Wallpaper"));
            _ = Directory.CreateDirectory(Path.Combine(root, "Rainmeter", "MosaicShell"));
            File.WriteAllText(Path.Combine(root, "Wallpaper", "Wallpaper.png"), "fake");
            File.WriteAllText(Path.Combine(root, "Rainmeter", "MosaicShell", "Chrono.json"),
                                     /*lang=json,strict*/
                                     """{"Style":"Minimal","TwentyFourHour":true,"ShowSeconds":false}""");
            File.WriteAllText(Path.Combine(root, "SHP-data.json"),
                                     /*lang=json,strict*/
                                     """{"Data":{"SetupName":"Test","CoreModules":"Chrono|Tessera"}}""");

            string shp = Path.Combine(_work, "Test{0}.shp");
            if (File.Exists(shp))
            {
                File.Delete(shp);
            }

            ZipFile.CreateFromDirectory(root, shp);
            return shp;
        }
    }
}
