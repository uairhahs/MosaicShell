namespace MosaicShell.Core.Capabilities.Platform;

/// <summary>How MosaicShell Host starts (see <see cref="HostLaunchOptions"/>).</summary>
public enum HostLaunchMode
{
    /// <summary>Hub window visible on startup (default).</summary>
    FullHub,

    /// <summary>Tray icon only; armed capabilities restore without showing Hub.</summary>
    TrayOnly,
}

/// <summary>Parsed CLI flags for MosaicShell.Host.</summary>
public static class HostLaunchOptions
{
    public const string TrayOnlyFlag = "--tray-only";

    public static HostLaunchMode Mode { get; private set; } = HostLaunchMode.FullHub;

    public static bool IsTrayOnly => Mode == HostLaunchMode.TrayOnly;

    public static void Apply(IReadOnlyList<string> args)
    {
        Mode = HostLaunchMode.FullHub;
        foreach (var arg in args)
        {
            if (arg.Equals(TrayOnlyFlag, StringComparison.OrdinalIgnoreCase))
                Mode = HostLaunchMode.TrayOnly;
        }
    }

    internal static void ResetForTests() => Mode = HostLaunchMode.FullHub;
}
