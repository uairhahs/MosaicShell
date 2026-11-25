namespace MosaicShell.Core.Install
{
    /// <summary>Relative paths that define a MosaicShell release / install bundle.</summary>
    public static class HostInstallLayoutSpec
    {
        public const string HostFolder = "Host";
        public const string MosaicistFolder = "Mosaicist";
        public const string TilesFolder = "Tiles";
        public const string HostExeName = "MosaicShell.Host.exe";
        public const string MosaicistExeName = "Mosaicist.exe";

        /// <summary>The Host's executable in a Linux bundle, where apphosts carry no suffix.</summary>
        public const string HostLinuxExeName = "MosaicShell.Host";

        /// <summary>Mosaicist's executable in a Linux bundle.</summary>
        public const string MosaicistLinuxExeName = "Mosaicist";
        public const string SetupExeName = "MosaicShell-Setup.exe";
        public const string VersionFileName = "VERSION.txt";
    }
}
