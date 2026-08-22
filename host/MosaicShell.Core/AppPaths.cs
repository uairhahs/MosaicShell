namespace MosaicShell.Core;

public static class AppPaths
{
    private static string? _rootOverride;

    /// <summary>Optional override for tests / portable installs. Cleared with <see cref="ClearRootOverride"/>.</summary>
    public static void SetRootOverride(string? root)
    {
        _rootOverride = string.IsNullOrWhiteSpace(root) ? null : Path.GetFullPath(root);
    }

    public static void ClearRootOverride() => _rootOverride = null;

    public static string RootDirectory =>
        _rootOverride
        ?? Environment.GetEnvironmentVariable("MOSAICSHELL_HOME")
        ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MosaicShell");

    public static string ConfigDirectory => Path.Combine(RootDirectory, "Config");
    public static string ModulesDirectory => Path.Combine(RootDirectory, "Modules");
    public static string CacheDirectory => Path.Combine(RootDirectory, "Cache");

    public static void EnsureLayout()
    {
        Directory.CreateDirectory(ConfigDirectory);
        Directory.CreateDirectory(ModulesDirectory);
        Directory.CreateDirectory(CacheDirectory);
    }
}
