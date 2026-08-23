namespace MosaicShell.Core.Install;

public sealed record DotNetRuntimeProbeResult(
    bool IsDesktopRuntimePresent,
    string? DetectedVersion,
    string DownloadUrl);

/// <summary>
/// Detects whether the .NET 10 Windows Desktop runtime is installed (x64).
/// Framework-dependent Host publish requires this; self-contained does not.
/// </summary>
public static class DotNetRuntimeProbe
{
    public const int RequiredMajor = 10;

    /// <param name="dotnetRoot">
    /// Optional override (tests). Defaults to <c>DOTNET_ROOT</c> or Program Files\dotnet.
    /// </param>
    public static DotNetRuntimeProbeResult Probe(string? dotnetRoot = null)
    {
        var root = ResolveDotNetRoot(dotnetRoot);
        var desktopShared = Path.Combine(root, "shared", "Microsoft.WindowsDesktop.App");
        if (!Directory.Exists(desktopShared))
        {
            return new DotNetRuntimeProbeResult(
                false,
                null,
                HostInstallPolicy.DotNetDesktopDownloadUrl);
        }

        string? best = null;
        foreach (var dir in Directory.EnumerateDirectories(desktopShared))
        {
            var name = Path.GetFileName(dir);
            if (!TryParseMajor(name, out var major) || major != RequiredMajor)
                continue;
            if (best is null || string.CompareOrdinal(name, best) > 0)
                best = name;
        }

        return new DotNetRuntimeProbeResult(
            best is not null,
            best,
            HostInstallPolicy.DotNetDesktopDownloadUrl);
    }

    public static bool RequiresDesktopRuntime(HostInstallVariant variant) =>
        variant == HostInstallVariant.FrameworkDependent;

    private static string ResolveDotNetRoot(string? overrideRoot)
    {
        if (!string.IsNullOrWhiteSpace(overrideRoot))
            return Path.GetFullPath(overrideRoot);

        var env = Environment.GetEnvironmentVariable("DOTNET_ROOT");
        if (!string.IsNullOrWhiteSpace(env) && Directory.Exists(env))
            return env;

        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        return Path.Combine(programFiles, "dotnet");
    }

    private static bool TryParseMajor(string versionFolder, out int major)
    {
        major = 0;
        var span = versionFolder.AsSpan();
        var dot = span.IndexOf('.');
        if (dot <= 0)
            return false;
        return int.TryParse(span[..dot], out major);
    }
}
