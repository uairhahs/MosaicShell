namespace MosaicShell.Core.Capabilities.Platform
{
    /// <summary>How MosaicShell Host starts (see <see cref="HostLaunchOptions"/>).</summary>
    public enum HostLaunchMode
    {
        /// <summary>Hub window visible on startup (default).</summary>
        FullHub,

        /// <summary>Tray icon only; armed capabilities restore without showing Hub.</summary>
        TrayOnly,

        /// <summary>
        /// Writes the glint's reference images to <see cref="HostLaunchOptions.GlintCaptureDirectory"/> and exits, so
        /// each OS's set can be compared for visual parity. Nothing else starts.
        /// </summary>
        GlintCapture,
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

        /// <summary>Followed by a folder: write the glint's reference images there and exit (<see cref="HostLaunchMode.GlintCapture"/>).</summary>
        public const string GlintCaptureFlag = "--capture-glint";

        public static HostLaunchMode Mode { get; private set; } = HostLaunchMode.FullHub;

        /// <summary>Where <see cref="HostLaunchMode.GlintCapture"/> writes; null otherwise.</summary>
        public static string? GlintCaptureDirectory { get; private set; }

        public static bool IsTrayOnly => Mode == HostLaunchMode.TrayOnly;

        /// <summary>When true, eligible Tessera flyouts may request AcrylicBlur (H1 trial).</summary>
        public static bool TesseraNativeTranslucencyTrial { get; private set; }

        /// <summary>When true, Host pins Software rendering only (H2 acrylic-unavailable fallback).</summary>
        public static bool TesseraForceSoftwareRender { get; private set; }

        public static void Apply(IReadOnlyList<string> args)
        {
            Mode = HostLaunchMode.FullHub;
            GlintCaptureDirectory = null;
            TesseraNativeTranslucencyTrial = false;
            TesseraForceSoftwareRender = false;
            for (int i = 0; i < args.Count; i++)
            {
                string arg = args[i];
                if (arg.Equals(TrayOnlyFlag, StringComparison.OrdinalIgnoreCase))
                {
                    Mode = HostLaunchMode.TrayOnly;
                }
                else if (arg.Equals(GlintCaptureFlag, StringComparison.OrdinalIgnoreCase))
                {
                    if (i + 1 < args.Count && !args[i + 1].StartsWith("--", StringComparison.Ordinal))
                    {
                        Mode = HostLaunchMode.GlintCapture;
                        GlintCaptureDirectory = args[++i];
                    }
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
            GlintCaptureDirectory = null;
            TesseraNativeTranslucencyTrial = false;
            TesseraForceSoftwareRender = false;
        }
    }
}
