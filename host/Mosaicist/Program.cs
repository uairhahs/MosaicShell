using MosaicShell.Core;
using MosaicShell.Core.Install;
using MosaicShell.Core.Modules;
using MosaicShell.Core.Runtime;
using MosaicShell.Core.Scale;
using MosaicShell.Core.Shp;

namespace Mosaicist;

/// <summary>
/// MosaicShell installer CLI — copies native Tiles/ stubs into the modules directory.
/// </summary>
public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help" or "help")
        {
            PrintHelp();
            return 0;
        }

        try
        {
            return args[0] switch
            {
                "list" => CmdList(),
                "scale" => CmdScale(args.Skip(1).ToArray()),
                "hash" => await CmdHashAsync(args.Skip(1).ToArray()),
                "install-module" => await CmdInstallModuleAsync(args.Skip(1).ToArray()),
                "uninstall-module" => CmdUninstallModule(args.Skip(1).ToArray()),
                "import-shp" => CmdImportShp(args.Skip(1).ToArray()),
                _ => Fail($"Unknown command '{args[0]}'.")
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    private static void PrintHelp()
    {
        Console.WriteLine("""
            Mosaicist - MosaicShell native module installer

            Commands:
              list
              scale [--reset-user]
              hash <file>
              install-module <id>
              uninstall-module <id>
              import-shp <file.shp>

            Examples:
              Mosaicist list
              Mosaicist hash .\release.zip
              Mosaicist install-module Canvas
              Mosaicist uninstall-module Canvas
              Mosaicist import-shp .\Nordic{0}.shp
            """);
    }

    private static int Fail(string message)
    {
        Console.Error.WriteLine(message);
        return 1;
    }

    private static int CmdList()
    {
        AppPaths.EnsureLayout();
        foreach (var m in ModuleCatalog.All)
        {
            var state = ModuleCatalog.IsInstalled(m.Id) ? "installed" : "missing";
            Console.WriteLine($"{m.Id,-12} {m.Kind,-8} {state}");
        }
        Console.WriteLine($"Modules root: {AppPaths.ModulesDirectory}");
        return 0;
    }

    private static int CmdScale(string[] args)
    {
        AppPaths.EnsureLayout();
        var settings = ScaleSettingsStore.Load();
        settings.DpiScale = DpiProbe.GetDpiScale();
        if (args.Contains("--reset-user")) settings.UserScale = 1.0;
        ScaleSettingsStore.Save(settings);
        var ui = Math.Round(settings.DpiScale * settings.UserScale, 4);
        Console.WriteLine($"DpiScale={settings.DpiScale} UserScale={settings.UserScale} UiScale={ui}");
        Console.WriteLine($"Wrote {ScaleSettingsStore.DefaultPath}");
        return 0;
    }

    private static async Task<int> CmdHashAsync(string[] args)
    {
        if (args.Length < 1) return Fail("Usage: hash <file>");
        var path = args[0];
        if (!File.Exists(path)) return Fail($"Not found: {path}");
        Console.WriteLine(await ReleaseDownloader.ComputeSha256Async(path));
        return 0;
    }

    private static async Task<int> CmdInstallModuleAsync(string[] args)
    {
        if (args.Length < 1) return Fail("Usage: install-module <id>");
        var id = args[0];
        AppPaths.EnsureLayout();

        if (!ModuleCatalog.TryGet(id, out _))
            return Fail($"Unknown module id '{id}'.");

        Console.WriteLine($"Installing {id} from Tiles/{id}/ native stub…");
        await new ModuleInstaller().InstallAsync(id);

        Console.WriteLine($"Installed {id} → {Path.Combine(AppPaths.ModulesDirectory, id)}");
        return 0;
    }

    private static int CmdUninstallModule(string[] args)
    {
        if (args.Length < 1) return Fail("Usage: uninstall-module <id>");
        AppPaths.EnsureLayout();
        if (!ModuleUninstaller.Uninstall(args[0]))
            return Fail($"Could not uninstall '{args[0]}'.");
        Console.WriteLine($"Uninstalled {args[0]}");
        return 0;
    }

    private static int CmdImportShp(string[] args)
    {
        if (args.Length < 1) return Fail("Usage: import-shp <file.shp>");
        var result = ShpImporter.Import(args[0]);
        Console.WriteLine(result.Message);
        if (result.ImportedModules.Count > 0)
            Console.WriteLine("Modules: " + string.Join(", ", result.ImportedModules));
        return result.Success ? 0 : 1;
    }
}
