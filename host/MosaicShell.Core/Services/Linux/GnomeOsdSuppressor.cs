using System.Diagnostics;
using System.Text;
using MosaicShell.Core.Services.Gnome;
using MosaicShell.Core.Services.Kde;
using Tmds.DBus.Protocol;

namespace MosaicShell.Core.Services.Linux
{
    /// <summary>Reads and writes GNOME settings; a seam so tests never touch the user's dconf.</summary>
    public interface IGnomeSettings
    {
        /// <summary>The value as gsettings prints it, or null when it cannot be read.</summary>
        string? Get(string schema, string key);

        bool Set(string schema, string key, string value);
    }

    /// <summary>GNOME settings through the <c>gsettings</c> command.</summary>
    public sealed class GsettingsCommand : IGnomeSettings
    {
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(3);

        public string? Get(string schema, string key)
        {
            return Run(out string output, "get", schema, key) ? output.Trim() : null;
        }

        public bool Set(string schema, string key, string value)
        {
            return Run(out _, "set", schema, key, value);
        }

        private static bool Run(out string output, params string[] args)
        {
            output = "";
            try
            {
                ProcessStartInfo info = new("gsettings")
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                };
                foreach (string arg in args)
                {
                    info.ArgumentList.Add(arg);
                }

                using Process process = Process.Start(info) ?? throw new InvalidOperationException("gsettings did not start");
                output = process.StandardOutput.ReadToEnd();
                if (!process.WaitForExit(Timeout))
                {
                    process.Kill();
                    return false;
                }

                return process.ExitCode == 0;
            }
            catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException or IOException)
            {
                Debug.WriteLine($"[Gsettings] {ex.Message}");
                return false;
            }
        }
    }

    /// <summary>
    /// Hides GNOME's volume and brightness pop-ups while Tessera is armed, through MosaicShell's GNOME Shell extension
    /// (<see cref="GnomeOsdGuardExtension"/>). Start writes the extension into the user's extensions folder when it differs
    /// and adds it to GNOME's enabled list, then owns the guard name the extension watches; Stop gives the name up, so
    /// Shell's pop-ups come back at once, and a crash does the same.
    /// </summary>
    /// <remarks>
    /// GNOME on Wayland loads a newly installed extension only at the next login, so the first session after install
    /// still shows Shell's pop-ups. When the user has turned extensions off, the extension is written but not switched
    /// on. Without a bus nothing happens and nothing throws. The one-shot and burst hides do nothing: the extension
    /// holds the pop-up back for as long as the name is owned.
    /// </remarks>
    /// <param name="address">A bus address; null for the user's session bus, empty for none. Tests pass a private bus.</param>
    /// <param name="dataHome">XDG_DATA_HOME; null finds it. Tests pass a throwaway folder.</param>
    /// <param name="settings">GNOME settings; null uses gsettings. Tests pass a stand-in.</param>
    public sealed class GnomeOsdSuppressor(string? address = null, string? dataHome = null, IGnomeSettings? settings = null) : INativeOsdSuppressor
    {
        private readonly string? _address = address ?? DBusAddress.Session;
        private readonly string _dataHome = dataHome ?? Xdg.XdgBaseDirectories.CurrentDataHome();
        private readonly IGnomeSettings _settings = settings ?? new GsettingsCommand();
        private readonly Lock _gate = new();
        private DBusConnection? _connection;
#pragma warning disable IDE0032 // Read and written under the lock; an auto property would not be.
        private bool _owned;
#pragma warning restore IDE0032
        private int _generation;

        public bool IsActive
        {
            get
            {
                lock (_gate)
                {
                    return _owned;
                }
            }
        }

        public void Start()
        {
            int generation;
            lock (_gate)
            {
                if (_connection is not null)
                {
                    return;
                }

                generation = ++_generation;
                _connection = string.IsNullOrEmpty(_address) ? null : new DBusConnection(_address);
                if (_connection is null)
                {
                    return;
                }
            }

            _ = Task.Run(() => RunAsync(generation));
        }

        public void Stop()
        {
            DBusConnection? closing;
            lock (_gate)
            {
                _generation++;
                closing = _connection;
                _connection = null;
                _owned = false;
            }

            // Closing the connection gives the guard name up.
            closing?.Dispose();
        }

        public void SuppressOnce()
        {
        }

        public void SuppressBurst(int durationMs = 1500)
        {
        }

        public void Dispose()
        {
            Stop();
        }

        private async Task RunAsync(int generation)
        {
            EnsureExtension();
            DBusConnection? connection;
            lock (_gate)
            {
                connection = generation == _generation ? _connection : null;
            }

            if (connection is null)
            {
                return;
            }

            try
            {
                await connection.ConnectAsync().ConfigureAwait(false);
                await connection.RequestNameAsync(KWinOsdScriptPolicy.GuardBusName).ConfigureAwait(false);
                lock (_gate)
                {
                    _owned = generation == _generation;
                }
            }
            catch (Exception ex)
            {
                // No bus or a Stop mid-connect: Shell keeps its pop-ups.
                Debug.WriteLine($"[GnomeOsd] guard: {ex.Message}");
            }
        }

        /// <summary>Writes the extension where it differs and switches it on, unless the user turned extensions off.</summary>
        private void EnsureExtension()
        {
            try
            {
                string dir = GnomeOsdGuardExtension.InstallDirectory(_dataHome);
                _ = Directory.CreateDirectory(dir);
                WriteIfChanged(Path.Combine(dir, "metadata.json"), GnomeOsdGuardExtension.MetadataJson);
                WriteIfChanged(Path.Combine(dir, "extension.js"), GnomeOsdGuardExtension.ExtensionJs);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Debug.WriteLine($"[GnomeOsd] install: {ex.Message}");
                return;
            }

            if (_settings.Get(GnomeOsdGuardExtension.ShellSchema, GnomeOsdGuardExtension.DisableUserExtensionsKey) == "true")
            {
                Debug.WriteLine("[GnomeOsd] user extensions are turned off; leaving them off");
                return;
            }

            string? enabled = GnomeOsdGuardExtension.EnabledListWithUuid(
                _settings.Get(GnomeOsdGuardExtension.ShellSchema, GnomeOsdGuardExtension.EnabledKey));
            if (enabled is not null)
            {
                _ = _settings.Set(GnomeOsdGuardExtension.ShellSchema, GnomeOsdGuardExtension.EnabledKey, enabled);
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
