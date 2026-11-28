using System.Diagnostics;
using MosaicShell.Core.HostPlatform;

namespace MosaicShell.Core.Services.Linux
{
    /// <summary>Reads and writes KDE config files; a seam so tests never touch the user's config.</summary>
    public interface IKdeConfig
    {
        /// <summary>The entry's value, empty when it is missing, null when the config cannot be read.</summary>
        string? Read(string file, string group, string key);

        bool Write(string file, string group, string key, string value);
    }

    /// <summary>
    /// KDE config through <c>kreadconfig6</c> and <c>kwriteconfig6 --notify</c>, so running Plasma parts see a write at
    /// once.
    /// </summary>
    public sealed class KConfigCommand : IKdeConfig
    {
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(3);

        public string? Read(string file, string group, string key)
        {
            return Run("kreadconfig6", out string output, "--file", file, "--group", group, "--key", key) ? output.Trim() : null;
        }

        public bool Write(string file, string group, string key, string value)
        {
            return Run("kwriteconfig6", out _, "--notify", "--file", file, "--group", group, "--key", key, value);
        }

        private static bool Run(string tool, out string output, params string[] args)
        {
            output = "";
            try
            {
                ProcessStartInfo info = new(tool)
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                };
                foreach (string arg in args)
                {
                    info.ArgumentList.Add(arg);
                }

                using Process process = Process.Start(info) ?? throw new InvalidOperationException($"{tool} did not start");
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
                Debug.WriteLine($"[KConfig] {ex.Message}");
                return false;
            }
        }
    }

    /// <summary>GNOME's volume key step, its <c>media-keys volume-step</c> setting.</summary>
    /// <param name="settings">GNOME settings; null uses gsettings. Tests pass a stand-in.</param>
    public sealed class GnomeVolumeKeyStep(IGnomeSettings? settings = null) : IVolumeKeyStep
    {
        private readonly IGnomeSettings _settings = settings ?? new GsettingsCommand();

        public VolumeKeyStepSource Source => VolumeKeyStepSource.Gnome;

        public int? Read()
        {
            return VolumeKeyStepPolicy.ParseGnome(_settings.Get(VolumeKeyStepPolicy.GnomeSchema, VolumeKeyStepPolicy.GnomeKey));
        }

        public bool Write(int percent)
        {
            int step = VolumeKeyStepPolicy.Clamp(Source, percent);
            return _settings.Set(
                VolumeKeyStepPolicy.GnomeSchema,
                VolumeKeyStepPolicy.GnomeKey,
                step.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
    }

    /// <summary>Plasma's volume key step, <c>VolumeStep</c> in <c>plasmaparc</c>.</summary>
    /// <param name="config">KDE config; null uses kreadconfig6 and kwriteconfig6. Tests pass a stand-in.</param>
    public sealed class PlasmaVolumeKeyStep(IKdeConfig? config = null) : IVolumeKeyStep
    {
        private readonly IKdeConfig _config = config ?? new KConfigCommand();

        public VolumeKeyStepSource Source => VolumeKeyStepSource.Plasma;

        public int? Read()
        {
            string? value = _config.Read(VolumeKeyStepPolicy.PlasmaFile, VolumeKeyStepPolicy.PlasmaGroup, VolumeKeyStepPolicy.PlasmaKey);
            return value is null ? null : VolumeKeyStepPolicy.ParsePlasma(value);
        }

        public bool Write(int percent)
        {
            int step = VolumeKeyStepPolicy.Clamp(Source, percent);
            return _config.Write(
                VolumeKeyStepPolicy.PlasmaFile,
                VolumeKeyStepPolicy.PlasmaGroup,
                VolumeKeyStepPolicy.PlasmaKey,
                step.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
    }
}
