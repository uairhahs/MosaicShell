using System.Text;

namespace MosaicShell.Core.Services.Xdg
{
    /// <summary>
    /// The XDG autostart entry that starts the Host at login, as Windows' Startup folder shortcut does: where it lives,
    /// what it runs, and whether the desktop has switched it off. Kept pure so it is tested on every OS.
    /// </summary>
    public static class XdgAutostartPolicy
    {
        public const string EntryFileName = "mosaicshell.desktop";

        private const string MainGroup = "[Desktop Entry]";

        /// <summary><c>$XDG_CONFIG_HOME/autostart/mosaicshell.desktop</c>, with ~/.config when it is unset or relative.</summary>
        public static string EntryPath(string? xdgConfigHome, string home)
        {
            string config = !string.IsNullOrEmpty(xdgConfigHome) && xdgConfigHome.StartsWith('/')
                ? xdgConfigHome.TrimEnd('/')
                : home.TrimEnd('/') + "/.config";
            return config + "/autostart/" + EntryFileName;
        }

        /// <summary>
        /// The Exec value that starts this Host: its own executable, or under <c>dotnet MosaicShell.Host.dll</c>, dotnet
        /// with the assembly. Each path is quoted and escaped as the Desktop Entry spec requires.
        /// </summary>
        public static string ExecLine(string processPath, string? entryAssemblyPath)
        {
            bool runByDotnet = Path.GetFileNameWithoutExtension(processPath).Equals("dotnet", StringComparison.OrdinalIgnoreCase);
            return runByDotnet && !string.IsNullOrEmpty(entryAssemblyPath)
                ? Quote(processPath) + " " + Quote(entryAssemblyPath)
                : Quote(processPath);
        }

        public static string EntryText(string exec)
        {
            return MainGroup + "\n"
                + "Type=Application\n"
                + "Name=MosaicShell\n"
                + "Comment=Start MosaicShell at login\n"
                + "Exec=" + exec + "\n"
                + "Terminal=false\n";
        }

        /// <summary>
        /// Whether an entry still runs at login: present, and not switched off in place by the desktop's settings
        /// (<c>Hidden=true</c>, or GNOME's <c>X-GNOME-Autostart-enabled=false</c>).
        /// </summary>
        public static bool IsEnabledEntry(string? entry)
        {
            if (entry is null)
            {
                return false;
            }

            bool inMain = false;
            foreach (string raw in entry.Split('\n'))
            {
                string line = raw.Trim();
                if (line.StartsWith('['))
                {
                    inMain = line == MainGroup;
                    continue;
                }

                if (inMain && (line == "Hidden=true" || line == "X-GNOME-Autostart-enabled=false"))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Inside quotes ", `, $ and \ take a backslash; the value's string escaping then doubles every backslash, and a
        /// literal % is written %% so it is not read as a field code.
        /// </summary>
        private static string Quote(string arg)
        {
            StringBuilder quoted = new("\"");
            foreach (char c in arg)
            {
                _ = c switch
                {
                    '"' or '`' or '$' => quoted.Append("\\\\").Append(c),
                    '\\' => quoted.Append("\\\\\\\\"),
                    '%' => quoted.Append("%%"),
                    _ => quoted.Append(c),
                };
            }

            return quoted.Append('"').ToString();
        }
    }
}
