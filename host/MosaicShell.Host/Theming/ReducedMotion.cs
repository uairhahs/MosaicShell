using System.Runtime.InteropServices;
using MosaicShell.Core.HostPlatform;
using MosaicShell.Core.Runtime;
using MosaicShell.Core.Settings;
using MosaicShell.Core.Services.Xdg;

namespace MosaicShell.Host.Theming
{
    /// <summary>
    /// The OS reduce-motion setting, read once on first use (audit U30). The reading is here; what it means is
    /// <see cref="ReducedMotionPolicy"/>'s. A setting that cannot be read counts as motion on.
    /// </summary>
    internal static class ReducedMotion
    {
        private static readonly Lazy<bool> Cached = new(Read);
        private static bool? _followSystem;

        /// <summary>Whether to reduce motion now: the Hub's "follow the system" choice and the OS setting together.</summary>
        public static bool IsOn => ReducedMotionPolicy.IsReduced(FollowSystem, SystemReduced);

        /// <summary>What the OS says, whether or not MosaicShell follows it.</summary>
        public static bool SystemReduced => Cached.Value;

        /// <summary>The Hub setting, read once and then kept in step by the Hub when it changes.</summary>
        public static bool FollowSystem
        {
            get => _followSystem ??= ModuleSettingsStore.Load("Hub", () => new HubSettings()).FollowSystemReducedMotion;
            set => _followSystem = value;
        }

        private static bool Read()
        {
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    int on = 1;
                    return SystemParametersInfo(SpiGetClientAreaAnimation, 0, ref on, 0)
                           && ReducedMotionPolicy.FromWindowsAnimationEffects(on != 0);
                }

                if (OperatingSystem.IsLinux())
                {
                    string? desktop = Environment.GetEnvironmentVariable(XdgCurrentDesktop.Variable);
                    if (XdgCurrentDesktop.IsPlasma(desktop))
                    {
                        string config = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME") is { Length: > 0 } home
                            ? home
                            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config");
                        string path = Path.Combine(config, "kdeglobals");
                        return File.Exists(path) && ReducedMotionPolicy.FromKdeGlobals(File.ReadAllText(path));
                    }

                    return ReducedMotionPolicy.FromGnomeEnableAnimations(Gsettings(ReducedMotionPolicy.GnomeSchema, ReducedMotionPolicy.GnomeKey));
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ExternalException or InvalidOperationException)
            {
                System.Diagnostics.Debug.WriteLine($"[ReducedMotion] {ex.Message}");
            }

            return false;
        }

        /// <summary>The value gsettings prints, or null; Core's gsettings reader is in the Linux-only build.</summary>
        private static string? Gsettings(string schema, string key)
        {
            System.Diagnostics.ProcessStartInfo info = new("gsettings")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            info.ArgumentList.Add("get");
            info.ArgumentList.Add(schema);
            info.ArgumentList.Add(key);
            using System.Diagnostics.Process process = System.Diagnostics.Process.Start(info)
                ?? throw new InvalidOperationException("gsettings did not start");
            string output = process.StandardOutput.ReadToEnd();
            if (!process.WaitForExit(TimeSpan.FromSeconds(2)))
            {
                process.Kill();
                return null;
            }

            return process.ExitCode == 0 ? output : null;
        }

        private const uint SpiGetClientAreaAnimation = 0x1042;

        [DllImport("user32.dll", EntryPoint = "SystemParametersInfoW")]
        private static extern bool SystemParametersInfo(uint action, uint param, ref int value, uint winIni);
    }
}
