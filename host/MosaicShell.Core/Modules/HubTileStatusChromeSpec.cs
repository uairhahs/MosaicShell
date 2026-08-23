namespace MosaicShell.Core.Modules;

/// <summary>Hub tile row status next to the title (Ready, Armed, …).</summary>
public enum HubTileStatusKind
{
    NotInstalled,
    Ready,
    ReadyToArm,
    Armed,
    Running,
}

/// <summary>
/// Mocha foreground hex for <see cref="HubTileStatusKind"/>. Host materializes brushes;
/// do not hardcode parallel colours in axaml.
/// </summary>
public static class HubTileStatusChromeSpec
{
    public const string NotInstalledHex = "#6C7086";
    public const string ReadyHex = "#A6E3A1";
    public const string ReadyToArmHex = "#F9E2AF";
    public const string ArmedHex = "#89B4FA";
    public const string RunningHex = "#94E2D5";

    public static HubTileStatusKind Resolve(bool installed, bool isCapability, bool armed, bool running)
    {
        if (!installed)
            return HubTileStatusKind.NotInstalled;
        if (isCapability)
            return armed ? HubTileStatusKind.Armed : HubTileStatusKind.ReadyToArm;
        return running ? HubTileStatusKind.Running : HubTileStatusKind.Ready;
    }

    public static string HexFor(HubTileStatusKind kind) => kind switch
    {
        HubTileStatusKind.NotInstalled => NotInstalledHex,
        HubTileStatusKind.Ready => ReadyHex,
        HubTileStatusKind.ReadyToArm => ReadyToArmHex,
        HubTileStatusKind.Armed => ArmedHex,
        HubTileStatusKind.Running => RunningHex,
        _ => NotInstalledHex,
    };
}
