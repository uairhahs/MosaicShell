using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using MosaicShell.Core;
using MosaicShell.Core.Shp;

namespace MosaicShell.Core.Tests;

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
        Directory.CreateDirectory(_work);
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
        var shp = CreateSampleShp();
        var result = ShpImporter.Import(shp);
        result.Success.Should().BeTrue();
        result.ImportedModules.Should().Contain("Chrono");
        File.Exists(Path.Combine(AppPaths.ConfigDirectory, "modules", "Chrono.json")).Should().BeTrue();
        Directory.EnumerateFiles(Path.Combine(AppPaths.ConfigDirectory, "Wallpaper")).Should().NotBeEmpty();
    }

    private string CreateSampleShp()
    {
        var root = Path.Combine(_work, "pkg");
        Directory.CreateDirectory(Path.Combine(root, "Wallpaper"));
        Directory.CreateDirectory(Path.Combine(root, "Rainmeter", "MosaicShell"));
        File.WriteAllText(Path.Combine(root, "Wallpaper", "Wallpaper.png"), "fake");
        File.WriteAllText(Path.Combine(root, "Rainmeter", "MosaicShell", "Chrono.json"),
            """{"Style":"Minimal","TwentyFourHour":true,"ShowSeconds":false}""");
        File.WriteAllText(Path.Combine(root, "SHP-data.json"),
            """{"Data":{"SetupName":"Test","CoreModules":"Chrono|Tessera"}}""");

        var shp = Path.Combine(_work, "Test{0}.shp");
        if (File.Exists(shp)) File.Delete(shp);
        ZipFile.CreateFromDirectory(root, shp);
        return shp;
    }
}
