namespace MosaicShell.Core.Services
{
    /// <summary>Severity of a diagnostic log line, lowest first.</summary>
    public enum DiagnosticLogLevel
    {
        /// <summary>High-volume tracing (routing decisions, media signals). May name tracks.</summary>
        Debug = 0,
        Info = 1,
        Warning = 2,
        Error = 3
    }

    public static class DiagnosticLogLevels
    {
        /// <summary>Overrides the build default, for example <c>debug</c> on a release build.</summary>
        public const string EnvironmentVariable = "MOSAICSHELL_LOG_LEVEL";

        /// <summary>
        /// The configured level by name (case-insensitive), otherwise the build default: Debug for a
        /// debug build, Info for a release build. Release builds therefore do not record Debug lines,
        /// which include track titles and artists, unless someone opts in. Numbers and unknown names
        /// fall back to the default rather than guessing.
        /// </summary>
        public static DiagnosticLogLevel Resolve(string? configured, bool isDebugBuild)
        {
            DiagnosticLogLevel fallback = isDebugBuild ? DiagnosticLogLevel.Debug : DiagnosticLogLevel.Info;
            return configured?.Trim().ToLowerInvariant() switch
            {
                "debug" => DiagnosticLogLevel.Debug,
                "info" => DiagnosticLogLevel.Info,
                "warning" or "warn" => DiagnosticLogLevel.Warning,
                "error" => DiagnosticLogLevel.Error,
                _ => fallback
            };
        }

        /// <summary>Fixed-width-ish tag written into each line.</summary>
        public static string Tag(DiagnosticLogLevel level)
        {
            return level switch
            {
                DiagnosticLogLevel.Debug => "DEBUG",
                DiagnosticLogLevel.Info => "INFO",
                DiagnosticLogLevel.Warning => "WARN",
                DiagnosticLogLevel.Error => "ERROR",
                _ => "INFO"
            };
        }
    }
}
