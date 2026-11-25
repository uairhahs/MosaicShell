namespace MosaicShell.Core.Install
{
    /// <summary>
    /// Detects a release zip / install bundle root (Host + Mosaicist + Tiles, no repo .sln required).
    /// </summary>
    public static class ReleaseBundleLayout
    {
        /// <summary>
        /// True when <paramref name="root"/> contains the Host and Mosaicist executables, under their Windows or Linux
        /// names, and a Tiles folder.
        /// </summary>
        public static bool IsBundleRoot(string root)
        {
            if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
            {
                return false;
            }

            string host = Path.Combine(root, HostInstallLayoutSpec.HostFolder);
            string mosaicist = Path.Combine(root, HostInstallLayoutSpec.MosaicistFolder);
            string tiles = Path.Combine(root, HostInstallLayoutSpec.TilesFolder);
            return AnyExists(host, HostInstallLayoutSpec.HostExeName, HostInstallLayoutSpec.HostLinuxExeName)
                && AnyExists(mosaicist, HostInstallLayoutSpec.MosaicistExeName, HostInstallLayoutSpec.MosaicistLinuxExeName)
                && Directory.Exists(tiles);
        }

        private static bool AnyExists(string folder, string windowsName, string linuxName)
        {
            return File.Exists(Path.Combine(folder, windowsName)) || File.Exists(Path.Combine(folder, linuxName));
        }

        /// <summary>
        /// Walks up from <paramref name="startDirectory"/> looking for a bundle root.
        /// Returns null when none is found.
        /// </summary>
        public static string? TryFindRoot(string? startDirectory = null)
        {
            DirectoryInfo? dir = new(
                string.IsNullOrWhiteSpace(startDirectory)
                    ? AppContext.BaseDirectory
                    : Path.GetFullPath(startDirectory));

            while (dir is not null)
            {
                if (IsBundleRoot(dir.FullName))
                {
                    return dir.FullName;
                }

                dir = dir.Parent;
            }

            return null;
        }
    }
}
