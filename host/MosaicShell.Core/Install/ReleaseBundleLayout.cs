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
        return File.Exists(hostExe) && File.Exists(mosaicistExe) && Directory.Exists(tiles);
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
}
