namespace MosaicShell.Core.Services.Linux
{
    /// <summary>
    /// The installed .desktop files (the XDG data dirs and the user's own), and what a process says about its install:
    /// its executable and, inside a Flatpak, its app id. Feeds <see cref="Mpris.MprisExportPolicy.DesktopEntryFor"/>.
    /// </summary>
    public sealed class LinuxDesktopEntries(string dataHome, IReadOnlyList<string> dataDirs)
    {
        private static readonly string[] DefaultDataDirs = ["/usr/local/share", "/usr/share"];

        public static LinuxDesktopEntries ForCurrentUser()
        {
            return new LinuxDesktopEntries(
                Xdg.XdgBaseDirectories.CurrentDataHome(),
                DataDirs(Environment.GetEnvironmentVariable("XDG_DATA_DIRS")));
        }

        /// <summary>Whether <c>&lt;name&gt;.desktop</c> is installed. A name is a file name, never a path.</summary>
        public bool Exists(string name)
        {
            return !string.IsNullOrWhiteSpace(name)
                && !name.Contains('/')
                && !name.Contains("..", StringComparison.Ordinal)
                && new[] { dataHome }.Concat(dataDirs).Any(dir => File.Exists(Path.Combine(dir, "applications", name + ".desktop")));
        }

        /// <summary>The absolute entries of XDG_DATA_DIRS, or the spec's default when it is unset or empty.</summary>
        public static IReadOnlyList<string> DataDirs(string? xdgDataDirs)
        {
            string[] dirs = string.IsNullOrEmpty(xdgDataDirs)
                ? []
                : [.. xdgDataDirs.Split(':', StringSplitOptions.RemoveEmptyEntries).Where(d => d.StartsWith('/'))];
            return dirs.Length > 0 ? dirs : DefaultDataDirs;
        }

        /// <summary>The Flatpak app id a process runs as (from its <c>/.flatpak-info</c>), or null outside a Flatpak.</summary>
        public static string? FlatpakAppId(int pid, string procRoot = "/proc")
        {
            try
            {
                string info = Path.Combine(procRoot, pid.ToString(System.Globalization.CultureInfo.InvariantCulture), "root", ".flatpak-info");
                if (!File.Exists(info))
                {
                    return null;
                }

                bool inApplication = false;
                foreach (string raw in File.ReadLines(info))
                {
                    string line = raw.Trim();
                    if (line.StartsWith('['))
                    {
                        inApplication = line == "[Application]";
                    }
                    else if (inApplication && line.StartsWith("name=", StringComparison.Ordinal))
                    {
                        string name = line["name=".Length..].Trim();
                        return name.Length > 0 ? name : null;
                    }
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Another user's process, or gone.
            }

            return null;
        }

        /// <summary>The executable a process runs, or null when it cannot be read.</summary>
        public static string? ExecutablePath(int pid, string procRoot = "/proc")
        {
            try
            {
                return new FileInfo(Path.Combine(procRoot, pid.ToString(System.Globalization.CultureInfo.InvariantCulture), "exe")).LinkTarget;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                return null;
            }
        }
    }
}
