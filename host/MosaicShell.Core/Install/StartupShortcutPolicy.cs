namespace MosaicShell.Core.Install
{
    /// <summary>Writes Internet Shortcut (.url) files for Host launch / logon.</summary>
    public static class StartupShortcutPolicy
    {
        public static string FormatUrlShortcut(string exePath, string? arguments = null)
        {
            string normalized = Path.GetFullPath(exePath).Replace('\\', '/');
            string url = string.IsNullOrWhiteSpace(arguments)
                ? $"file:///{normalized}"
                : $"file:///{normalized} {arguments.Trim()}";
            return $"[InternetShortcut]\r\nURL={url}\r\nIconIndex=0\r\nIconFile={normalized}\r\n";
        }

        public static void WriteHostShortcut(string shortcutPath, string hostExePath, bool trayOnly = false)
        {
            string? dir = Path.GetDirectoryName(shortcutPath);
            if (!string.IsNullOrEmpty(dir))
            {
                _ = Directory.CreateDirectory(dir);
            }

            string? args = trayOnly ? HostInstallPolicy.TrayOnlyArg : null;
            File.WriteAllText(shortcutPath, FormatUrlShortcut(hostExePath, args));
        }

        public static void WriteStartMenuShortcut(string hostExePath)
        {
            WriteHostShortcut(
                Path.Combine(HostInstallPolicy.StartMenuProgramsDirectory, HostInstallPolicy.StartMenuShortcutFileName),
                hostExePath,
                trayOnly: false);
        }

        public static void WriteStartupShortcut(string hostExePath, bool trayOnly = true)
        {
            WriteHostShortcut(
                Path.Combine(HostInstallPolicy.StartupDirectory, HostInstallPolicy.StartupShortcutFileName),
                hostExePath,
                trayOnly);
        }

        public static void RemoveStartupShortcut()
        {
            string path = Path.Combine(HostInstallPolicy.StartupDirectory, HostInstallPolicy.StartupShortcutFileName);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
