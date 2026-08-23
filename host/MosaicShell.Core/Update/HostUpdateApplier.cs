using System.Diagnostics;
using MosaicShell.Core.Install;

namespace MosaicShell.Core.Update;

/// <summary>Inno Setup silent upgrade flags (UniGetUI-style).</summary>
public static class HostUpdatePolicy
{
    public const string SilentSetupArgs = "/SILENT /CLOSEAPPLICATIONS /NORESTART /NoAutoStart";

    public static string CacheDirectory =>
        Path.Combine(AppPaths.CacheDirectory, "updates");
}

/// <summary>Launches a downloaded MosaicShell-Setup.exe for upgrade.</summary>
public static class HostUpdateApplier
{
    public static ProcessStartInfo CreateSetupStartInfo(string setupExePath, string? extraArgs = null)
    {
        var full = Path.GetFullPath(setupExePath);
        if (!File.Exists(full))
            throw new FileNotFoundException("Setup executable not found.", full);

        var args = string.IsNullOrWhiteSpace(extraArgs)
            ? HostUpdatePolicy.SilentSetupArgs
            : $"{HostUpdatePolicy.SilentSetupArgs} {extraArgs.Trim()}";

        return new ProcessStartInfo
        {
            FileName = full,
            Arguments = args,
            UseShellExecute = true,
        };
    }

    public static Process LaunchSetup(string setupExePath, string? extraArgs = null)
    {
        var psi = CreateSetupStartInfo(setupExePath, extraArgs);
        return Process.Start(psi)
               ?? throw new InvalidOperationException($"Failed to start setup: {setupExePath}");
    }
}
