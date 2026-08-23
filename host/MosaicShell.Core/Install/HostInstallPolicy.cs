namespace MosaicShell.Core.Install;

/// <summary>Default install destinations and shortcut names for Host setup.</summary>
public static class HostInstallPolicy
{
    public const string StartMenuShortcutFileName = "MosaicShell.lnk.url";
    public const string StartupShortcutFileName = "MosaicShell.Host.url";
    public const string TrayOnlyArg = "--tray-only";

    /// <summary>Default app install root: %LocalAppData%\Programs\MosaicShell</summary>
    public static string DefaultInstallDirectory =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Programs",
            "MosaicShell");

    public static string StartMenuProgramsDirectory =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
            "Programs");

    public static string StartupDirectory =>
        Environment.GetFolderPath(Environment.SpecialFolder.Startup);

    /// <summary>Module ids installed by default from Inno Setup / Mosaicist post-install.</summary>
    public static IReadOnlyList<string> DefaultModuleIds { get; } = ["Tessera", "Mixdeck"];
}
