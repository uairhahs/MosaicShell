using System.Reflection;
using MosaicShell.Core.Services.Xdg;

namespace MosaicShell.Core.Services.Linux
{
    /// <summary>
    /// Start at login on Linux: an XDG autostart entry, which Plasma and GNOME both run, in place of Windows' Startup
    /// folder shortcut. Enabled means the entry is there and the desktop has not switched it off.
    /// </summary>
    public sealed class XdgAutostartService(string entryPath, string exec) : IAutostartService
    {
        public string EntryPath { get; } = entryPath;

        public string Exec { get; } = exec;

        /// <summary>The user's autostart folder, starting this Host as it is running now.</summary>
        public static XdgAutostartService ForCurrentProcess()
        {
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string processPath = Environment.ProcessPath ?? Path.Combine(AppContext.BaseDirectory, "MosaicShell.Host");
            return new XdgAutostartService(
                XdgAutostartPolicy.EntryPath(Environment.GetEnvironmentVariable("XDG_CONFIG_HOME"), home),
                XdgAutostartPolicy.ExecLine(processPath, Assembly.GetEntryAssembly()?.Location));
        }

        public bool IsEnabled => XdgAutostartPolicy.IsEnabledEntry(ReadEntry());

        public void SetEnabled(bool enabled)
        {
            if (!enabled)
            {
                if (File.Exists(EntryPath))
                {
                    File.Delete(EntryPath);
                }

                return;
            }

            _ = Directory.CreateDirectory(Path.GetDirectoryName(EntryPath)!);
            File.WriteAllText(EntryPath, XdgAutostartPolicy.EntryText(Exec));
        }

        private string? ReadEntry()
        {
            try
            {
                return File.ReadAllText(EntryPath);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                return null;
            }
        }
    }
}
