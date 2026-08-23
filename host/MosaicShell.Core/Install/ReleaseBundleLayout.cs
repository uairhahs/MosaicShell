namespace MosaicShell.Core.Install;

/// <summary>
/// Detects a release zip / install bundle root (Host + Mosaicist + Tiles, no repo .sln required).
/// </summary>
public static class ReleaseBundleLayout
{
    /// <summary>True when <paramref name="root"/> contains Host exe, Mosaicist exe, and a Tiles folder.</summary>
    public static bool IsBundleRoot(string root)
    {
        if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
            return false;

        var hostExe = Path.Combine(root, HostInstallLayoutSpec.HostFolder, HostInstallLayoutSpec.HostExeName);
        var mosaicistExe = Path.Combine(root, HostInstallLayoutSpec.MosaicistFolder, HostInstallLayoutSpec.MosaicistExeName);
        var tiles = Path.Combine(root, HostInstallLayoutSpec.TilesFolder);

        // Variant layouts: Host-fw / Host-sc instead of Host/
        var hostFwExe = Path.Combine(root, HostInstallLayoutSpec.HostFwFolder, HostInstallLayoutSpec.HostExeName);
        var hostScExe = Path.Combine(root, HostInstallLayoutSpec.HostScFolder, HostInstallLayoutSpec.HostExeName);
        var hasHost = File.Exists(hostExe) || File.Exists(hostFwExe) || File.Exists(hostScExe);

        return hasHost
               && File.Exists(mosaicistExe)
               && Directory.Exists(tiles);
    }

    /// <summary>
    /// Walks up from <paramref name="startDirectory"/> looking for a bundle root.
    /// Returns null when none is found.
    /// </summary>
    public static string? TryFindRoot(string? startDirectory = null)
    {
        var dir = new DirectoryInfo(
            string.IsNullOrWhiteSpace(startDirectory)
                ? AppContext.BaseDirectory
                : Path.GetFullPath(startDirectory));

        while (dir is not null)
        {
            if (IsBundleRoot(dir.FullName))
                return dir.FullName;
            dir = dir.Parent;
        }

        return null;
    }

    /// <summary>
    /// Resolves the Host payload folder for a variant inside a bundle root.
    /// Prefers exact folder names; falls back to <c>Host/</c>.
    /// </summary>
    public static string ResolveHostFolder(string bundleRoot, HostInstallVariant variant)
    {
        var preferred = variant switch
        {
            HostInstallVariant.SelfContained => HostInstallLayoutSpec.HostScFolder,
            _ => HostInstallLayoutSpec.HostFwFolder,
        };

        var preferredPath = Path.Combine(bundleRoot, preferred);
        if (Directory.Exists(preferredPath)
            && File.Exists(Path.Combine(preferredPath, HostInstallLayoutSpec.HostExeName)))
            return preferredPath;

        var classic = Path.Combine(bundleRoot, HostInstallLayoutSpec.HostFolder);
        if (Directory.Exists(classic)
            && File.Exists(Path.Combine(classic, HostInstallLayoutSpec.HostExeName)))
            return classic;

        throw new DirectoryNotFoundException(
            $"No Host payload for {variant} under '{bundleRoot}'. Expected {preferred}/ or Host/.");
    }
}
