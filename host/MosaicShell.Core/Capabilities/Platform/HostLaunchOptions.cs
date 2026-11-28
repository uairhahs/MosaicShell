namespace MosaicShell.Core.Capabilities.Platform
{
    /// <summary>How MosaicShell Host starts (see <see cref="HostLaunchOptions"/>).</summary>
    public enum HostLaunchMode
    {
        /// <summary>Hub window visible on startup (default).</summary>
        FullHub,

        /// <summary>Tray icon only; armed capabilities restore without showing Hub.</summary>
        TrayOnly,
    }

    /// <summary>Parsed CLI flags for MosaicShell.Host.</summary>
    public static class HostLaunchOptions
    {
        public const string TrayOnlyFlag = "--tray-only";

        /// <summary>Opt-in OS AcrylicBlur spike for single-shell Tessera flyouts (default off).</summary>
        public const string TesseraNativeTranslucencyTrialFlag = "--tessera-native-translucency";

        /// <summary>The flag's name before the Native Translucency rename; still accepted.</summary>
        public const string LegacyTesseraOsAcrylicTrialFlag = "--tessera-os-acrylic";

        /// <summary>H2 eval: force Software-only rendering (NativeTranslucency ineligible; frost fallback).</summary>
        public const string TesseraForceSoftwareRenderFlag = "--tessera-software-render";

        /// <summary>
        /// Installs a third-party module from a folder or .zip and exits, without starting the Hub (developers, scripts,
        /// CI). Takes the path after it.
        /// </summary>
        public const string InstallPackageFlag = "--install-package";

        public static HostLaunchMode Mode { get; private set; } = HostLaunchMode.FullHub;

        /// <summary>The package <see cref="InstallPackageFlag"/> names, or null.</summary>
        public static string? InstallPackagePath { get; private set; }

        /// <summary>True when <see cref="InstallPackageFlag"/> came with no path after it.</summary>
        public static bool InstallPackageMissingPath { get; private set; }

        public static bool IsTrayOnly => Mode == HostLaunchMode.TrayOnly;

        /// <summary>When true, eligible Tessera flyouts may request AcrylicBlur (H1 trial).</summary>
        public static bool TesseraNativeTranslucencyTrial { get; private set; }

        /// <summary>When true, Host pins Software rendering only (H2 acrylic-unavailable fallback).</summary>
        public static bool TesseraForceSoftwareRender { get; private set; }

        public static void Apply(IReadOnlyList<string> args)
        {
            Mode = HostLaunchMode.FullHub;
            TesseraNativeTranslucencyTrial = false;
            TesseraForceSoftwareRender = false;
            InstallPackagePath = null;
            InstallPackageMissingPath = false;
            for (int i = 0; i < args.Count; i++)
            {
                string arg = args[i];
                if (arg.Equals(InstallPackageFlag, StringComparison.OrdinalIgnoreCase))
                {
                    if (i + 1 < args.Count && !args[i + 1].StartsWith("--", StringComparison.Ordinal))
                    {
                        InstallPackagePath = args[++i];
                    }
                    else
                    {
                        InstallPackageMissingPath = true;
                    }
                }
                else if (arg.Equals(TrayOnlyFlag, StringComparison.OrdinalIgnoreCase))
                {
                    Mode = HostLaunchMode.TrayOnly;
                }
                else if (arg.Equals(TesseraNativeTranslucencyTrialFlag, StringComparison.OrdinalIgnoreCase)
                    || arg.Equals(LegacyTesseraOsAcrylicTrialFlag, StringComparison.OrdinalIgnoreCase))
                {
                    TesseraNativeTranslucencyTrial = true;
                }
                else if (arg.Equals(TesseraForceSoftwareRenderFlag, StringComparison.OrdinalIgnoreCase))
                {
                    TesseraForceSoftwareRender = true;
                }
            }
        }

        internal static void ResetForTests()
        {
            Mode = HostLaunchMode.FullHub;
            InstallPackagePath = null;
            InstallPackageMissingPath = false;
            TesseraNativeTranslucencyTrial = false;
            TesseraForceSoftwareRender = false;
        }
    }
}
