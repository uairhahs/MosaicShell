using MosaicShell.Core.Runtime;
using MosaicShell.Core.Settings;

namespace MosaicShell.Core.Install;

/// <summary>
/// Copies a release bundle into an app directory, installs selected modules, and writes shortcuts.
/// </summary>
public sealed class HostInstallService
{
    private readonly ModuleInstaller _modules = new();

    public async Task<HostInstallResult> InstallAsync(
        HostInstallRequest request,
        IProgress<HostInstallProgress>? progress = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ct.ThrowIfCancellationRequested();

        var bundle = Path.GetFullPath(request.BundleRoot);
        if (!ReleaseBundleLayout.IsBundleRoot(bundle)
            && !Directory.Exists(Path.Combine(bundle, HostInstallLayoutSpec.TilesFolder)))
        {
            throw new InvalidOperationException(
                $"'{bundle}' is not a MosaicShell release bundle (need Host*/Mosaicist/Tiles).");
        }

        if (DotNetRuntimeProbe.RequiresDesktopRuntime(request.Variant))
        {
            var probe = DotNetRuntimeProbe.Probe(request.DotNetRootOverride);
            if (!probe.IsDesktopRuntimePresent)
            {
                throw new InvalidOperationException(
                    $".NET {DotNetRuntimeProbe.RequiredMajor} Desktop Runtime is required for framework-dependent installs. " +
                    $"Download: {probe.DownloadUrl}");
            }
        }

        var target = Path.GetFullPath(request.TargetDirectory);
        Directory.CreateDirectory(target);

        progress?.Report(new HostInstallProgress { Stage = "copy", Detail = "Host", Fraction = 0.1 });
        var hostSource = ReleaseBundleLayout.ResolveHostFolder(bundle, request.Variant);
        var hostDest = Path.Combine(target, HostInstallLayoutSpec.HostFolder);
        CopyDirectory(hostSource, hostDest, ct);

        progress?.Report(new HostInstallProgress { Stage = "copy", Detail = "Mosaicist", Fraction = 0.35 });
        var mosaicistSource = Path.Combine(bundle, HostInstallLayoutSpec.MosaicistFolder);
        if (!Directory.Exists(mosaicistSource))
            throw new DirectoryNotFoundException($"Missing Mosaicist folder under '{bundle}'.");
        CopyDirectory(mosaicistSource, Path.Combine(target, HostInstallLayoutSpec.MosaicistFolder), ct);

        progress?.Report(new HostInstallProgress { Stage = "copy", Detail = "Tiles", Fraction = 0.5 });
        var tilesSource = Path.Combine(bundle, HostInstallLayoutSpec.TilesFolder);
        CopyDirectory(tilesSource, Path.Combine(target, HostInstallLayoutSpec.TilesFolder), ct);

        var versionPath = Path.Combine(bundle, HostInstallLayoutSpec.VersionFileName);
        string? version = null;
        if (File.Exists(versionPath))
        {
            File.Copy(versionPath, Path.Combine(target, HostInstallLayoutSpec.VersionFileName), overwrite: true);
            version = (await File.ReadAllTextAsync(versionPath, ct)).Trim();
        }

        AppPaths.EnsureLayout();
        var installed = new List<string>();
        var moduleIds = request.ModuleIds ?? [];
        for (var i = 0; i < moduleIds.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            var id = moduleIds[i];
            progress?.Report(new HostInstallProgress
            {
                Stage = "modules",
                Detail = id,
                Fraction = 0.55 + 0.3 * ((i + 1) / (double)Math.Max(1, moduleIds.Count)),
            });
            await _modules.InstallAsync(id, progress: null, ct, sourceTreeRoot: target);
            installed.Add(id);
        }

        var hostExe = Path.Combine(hostDest, HostInstallLayoutSpec.HostExeName);
        if (!File.Exists(hostExe))
            throw new FileNotFoundException("Host executable missing after copy.", hostExe);

        progress?.Report(new HostInstallProgress { Stage = "shortcuts", Fraction = 0.9 });
        if (request.CreateStartMenuShortcut)
        {
            var startMenuDir = request.StartMenuDirectoryOverride
                               ?? HostInstallPolicy.StartMenuProgramsDirectory;
            StartupShortcutPolicy.WriteHostShortcut(
                Path.Combine(startMenuDir, HostInstallPolicy.StartMenuShortcutFileName),
                hostExe,
                trayOnly: false);
        }

        var startupDir = request.StartupDirectoryOverride ?? HostInstallPolicy.StartupDirectory;
        var startupPath = Path.Combine(startupDir, HostInstallPolicy.StartupShortcutFileName);
        if (request.EnableLogonAutostart)
            StartupShortcutPolicy.WriteHostShortcut(startupPath, hostExe, trayOnly: true);
        else if (File.Exists(startupPath))
            File.Delete(startupPath);

        if (request.ResetWelcomeCompleted || request.EnableLogonAutostart)
        {
            var hub = ModuleSettingsStore.Load("Hub", () => new HubSettings());
            if (request.ResetWelcomeCompleted)
                hub.WelcomeCompleted = false;
            if (request.EnableLogonAutostart)
                hub.AutostartHost = true;
            ModuleSettingsStore.Save("Hub", hub);
        }

        progress?.Report(new HostInstallProgress { Stage = "done", Detail = target, Fraction = 1 });
        return new HostInstallResult
        {
            TargetDirectory = target,
            HostExePath = hostExe,
            InstalledModules = installed,
            Version = version,
        };
    }

    private static void CopyDirectory(string source, string dest, CancellationToken ct)
    {
        Directory.CreateDirectory(dest);
        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            ct.ThrowIfCancellationRequested();
            var rel = Path.GetRelativePath(source, file);
            var target = Path.Combine(dest, rel);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, overwrite: true);
        }
    }
}
