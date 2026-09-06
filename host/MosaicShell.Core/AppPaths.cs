using System.Diagnostics.CodeAnalysis;

namespace MosaicShell.Core
{
    public static class AppPaths
    {

        /// <summary>Optional override for tests / portable installs. Cleared with <see cref="ClearRootOverride"/>.</summary>
        public static void SetRootOverride(string? root)
        {
            RootDirectory = string.IsNullOrWhiteSpace(root) ? null : Path.GetFullPath(root);
        }

        public static void ClearRootOverride()
        {
            RootDirectory = null;
        }

        [AllowNull]
        public static string RootDirectory
        {
            get =>
            field
            ?? Environment.GetEnvironmentVariable("MOSAICSHELL_HOME")
            ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MosaicShell"); private set;
        }

        public static string ConfigDirectory => Path.Combine(RootDirectory, "Config");
        public static string ModulesDirectory => Path.Combine(RootDirectory, "Modules");
        public static string CacheDirectory => Path.Combine(RootDirectory, "Cache");

        public static void EnsureLayout()
        {
            _ = Directory.CreateDirectory(ConfigDirectory);
            _ = Directory.CreateDirectory(ModulesDirectory);
            _ = Directory.CreateDirectory(CacheDirectory);
        }
    }
}
