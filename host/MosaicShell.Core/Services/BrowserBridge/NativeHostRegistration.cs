using System.Security;
using System.Text;
using System.Text.Json;
using Microsoft.Win32;

namespace MosaicShell.Core.Services.BrowserBridge
{
    /// <summary>The per-user registry, behind a seam so registration is tested without touching the real one.</summary>
    public interface IUserRegistry
    {
        /// <summary>The unnamed value of a key under HKEY_CURRENT_USER, or null when the key or value is absent.</summary>
        string? GetDefault(string subKey);

        void SetDefault(string subKey, string value);

        /// <summary>Deletes the key and everything under it; absent is not an error.</summary>
        void DeleteTree(string subKey);
    }

    public sealed class CurrentUserRegistry : IUserRegistry
    {
        public string? GetDefault(string subKey)
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(subKey);
            return key?.GetValue("") as string;
        }

        public void SetDefault(string subKey, string value)
        {
            using RegistryKey key = Registry.CurrentUser.CreateSubKey(subKey);
            key.SetValue("", value, RegistryValueKind.String);
        }

        public void DeleteTree(string subKey)
        {
            Registry.CurrentUser.DeleteSubKeyTree(subKey, throwOnMissingSubKey: false);
        }
    }

    /// <summary>
    /// Registers the relay as a native messaging host, which is how a browser extension reaches a program on the
    /// computer: the browser looks the host up by name, starts it, and lets only the extensions the registration names
    /// talk to it. The Host does this itself at every start, for the current user only, so nothing needs an installer,
    /// administrator rights, or a manual step, and a portable copy works the same as an installed one.
    /// </summary>
    public static class NativeHostRegistration
    {
        public const string HostName = "com.mosaicshell.grout";

        /// <summary>
        /// The extensions allowed to start the relay, by ID. The first is the ID an unpacked copy gets from the key in
        /// its manifest; the store IDs are added here when the extension is listed. Never a wildcard.
        /// </summary>
        public static IReadOnlyList<string> AllowedExtensionIds { get; } = ["aaffcapodpfecchmelidkkhgiaamijpe"];

        /// <summary>Where each supported browser looks up native messaging hosts, under HKEY_CURRENT_USER.</summary>
        private static readonly string[] BrowserKeys =
        [
            @"Software\Microsoft\Edge\NativeMessagingHosts\" + HostName,
            @"Software\Google\Chrome\NativeMessagingHosts\" + HostName,
        ];

        public static string DefaultDataDirectory()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MosaicShell", "browser");
        }

        /// <summary>
        /// Makes the browsers start <paramref name="relayPath"/> for the extension. Returns false, having registered
        /// nothing new, when the relay is not where it says or the registry refuses; the Host runs regardless.
        /// Writes only what differs, so calling it at every start costs nothing.
        /// </summary>
        public static bool Register(string relayPath, string dataDirectory, IUserRegistry registry)
        {
            if (!Path.IsPathFullyQualified(relayPath) || !File.Exists(relayPath))
            {
                return false;
            }

            try
            {
                string manifestPath = Path.Combine(dataDirectory, HostName + ".json");
                string manifest = ManifestFor(relayPath);
                _ = Directory.CreateDirectory(dataDirectory);
                if (!File.Exists(manifestPath) || File.ReadAllText(manifestPath) != manifest)
                {
                    File.WriteAllText(manifestPath, manifest, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                }

                foreach (string key in BrowserKeys)
                {
                    if (registry.GetDefault(key) != manifestPath)
                    {
                        registry.SetDefault(key, manifestPath);
                    }
                }

                return true;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SecurityException)
            {
                return false;
            }
        }

        /// <summary>Removes the registration and its manifest, for an uninstall. Nothing registered is harmless.</summary>
        public static void Unregister(string dataDirectory, IUserRegistry registry)
        {
            try
            {
                foreach (string key in BrowserKeys)
                {
                    registry.DeleteTree(key);
                }

                string manifestPath = Path.Combine(dataDirectory, HostName + ".json");
                if (File.Exists(manifestPath))
                {
                    File.Delete(manifestPath);
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SecurityException)
            {
                // An uninstall that cannot clean up must still finish.
            }
        }

        private static string ManifestFor(string relayPath)
        {
            using MemoryStream stream = new();
            using (Utf8JsonWriter writer = new(stream, new JsonWriterOptions { Indented = true }))
            {
                writer.WriteStartObject();
                writer.WriteString("name", HostName);
                writer.WriteString("description", "Lets the Grout browser extension tell MosaicShell what is playing.");
                writer.WriteString("path", relayPath);
                writer.WriteString("type", "stdio");
                writer.WriteStartArray("allowed_origins");
                foreach (string id in AllowedExtensionIds)
                {
                    writer.WriteStringValue("chrome-extension://" + id + "/");
                }

                writer.WriteEndArray();
                writer.WriteEndObject();
            }

            return Encoding.UTF8.GetString(stream.ToArray());
        }
    }
}
