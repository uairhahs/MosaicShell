using System.Diagnostics;
using System.Text;
using MosaicShell.Core.Services.Gnome;

namespace MosaicShell.Core.Services.Linux
{
    /// <summary>
    /// Writes MosaicShell's GNOME Shell extension where it differs and switches it on, unless the user turned extensions
    /// off. Both the Host's presence (the top bar icon) and Tessera's pop-up guard need it, so either one installs it.
    /// GNOME on Wayland loads a new or changed extension at the next login.
    /// </summary>
    internal static class GnomeShellExtensionInstall
    {
        public static void Ensure(string dataHome, IGnomeSettings settings)
        {
            try
            {
                string dir = GnomeOsdGuardExtension.InstallDirectory(dataHome);
                _ = Directory.CreateDirectory(dir);
                WriteIfChanged(Path.Combine(dir, "metadata.json"), GnomeOsdGuardExtension.MetadataJson);
                WriteIfChanged(Path.Combine(dir, "extension.js"), GnomeOsdGuardExtension.ExtensionJs);
                WriteIfChanged(Path.Combine(dir, GnomeOsdGuardExtension.PanelIconFile), GnomeOsdGuardExtension.PanelIconSvg);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Debug.WriteLine($"[GnomeExtension] install: {ex.Message}");
                return;
            }

            if (settings.Get(GnomeOsdGuardExtension.ShellSchema, GnomeOsdGuardExtension.DisableUserExtensionsKey) == "true")
            {
                Debug.WriteLine("[GnomeExtension] user extensions are turned off; leaving them off");
                return;
            }

            string? enabled = GnomeOsdGuardExtension.EnabledListWithUuid(
                settings.Get(GnomeOsdGuardExtension.ShellSchema, GnomeOsdGuardExtension.EnabledKey));
            if (enabled is not null)
            {
                _ = settings.Set(GnomeOsdGuardExtension.ShellSchema, GnomeOsdGuardExtension.EnabledKey, enabled);
            }
        }

        private static void WriteIfChanged(string path, string content)
        {
            if (!File.Exists(path) || File.ReadAllText(path) != content)
            {
                File.WriteAllText(path, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            }
        }
    }
}
