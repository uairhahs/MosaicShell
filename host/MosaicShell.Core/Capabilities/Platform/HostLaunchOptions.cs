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
        public const string TesseraOsAcrylicTrialFlag = "--tessera-os-acrylic";

        /// <summary>H2 eval: force Software-only rendering (OsAcrylic ineligible; frost fallback).</summary>
        public const string TesseraForceSoftwareRenderFlag = "--tessera-software-render";

        public static HostLaunchMode Mode { get; private set; } = HostLaunchMode.FullHub;

        public static bool IsTrayOnly => Mode == HostLaunchMode.TrayOnly;

        /// <summary>When true, eligible Tessera flyouts may request AcrylicBlur (H1 trial).</summary>
        public static bool TesseraOsAcrylicTrial { get; private set; }

        /// <summary>When true, Host pins Software rendering only (H2 acrylic-unavailable fallback).</summary>
        public static bool TesseraForceSoftwareRender { get; private set; }

        public static void Apply(IReadOnlyList<string> args)
        {
            Mode = HostLaunchMode.FullHub;
            TesseraOsAcrylicTrial = false;
            TesseraForceSoftwareRender = false;
            foreach (string arg in args)
            {
                if (arg.Equals(TrayOnlyFlag, StringComparison.OrdinalIgnoreCase))
                {
                    Mode = HostLaunchMode.TrayOnly;
                }
                else if (arg.Equals(TesseraOsAcrylicTrialFlag, StringComparison.OrdinalIgnoreCase))
                {
                    TesseraOsAcrylicTrial = true;
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
            TesseraOsAcrylicTrial = false;
            TesseraForceSoftwareRender = false;
        }
    }
}
