namespace MosaicShell.Core.Services.Xdg
{
    /// <summary>
    /// The user's XDG base directories as the base directory spec reads them: an absolute value of the variable, else
    /// the home folder's default (a relative value is ignored, as the spec says). One rule for every Linux route that
    /// keeps a file per user. Kept pure so it is tested on every OS.
    /// </summary>
    public static class XdgBaseDirectories
    {
        public const string ConfigHomeVariable = "XDG_CONFIG_HOME";
        public const string DataHomeVariable = "XDG_DATA_HOME";
        public const string ConfigHomeDefault = ".config";
        public const string DataHomeDefault = ".local/share";

        public static string ConfigHome(string? xdgConfigHome, string home)
        {
            return Resolve(xdgConfigHome, home, ConfigHomeDefault);
        }

        public static string DataHome(string? xdgDataHome, string home)
        {
            return Resolve(xdgDataHome, home, DataHomeDefault);
        }

        /// <summary><see cref="ConfigHome"/> for this process's environment and user.</summary>
        public static string CurrentConfigHome()
        {
            return ConfigHome(Environment.GetEnvironmentVariable(ConfigHomeVariable), CurrentHome());
        }

        /// <summary><see cref="DataHome"/> for this process's environment and user.</summary>
        public static string CurrentDataHome()
        {
            return DataHome(Environment.GetEnvironmentVariable(DataHomeVariable), CurrentHome());
        }

        public static string CurrentHome()
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }

        private static string Resolve(string? value, string home, string fallback)
        {
            return !string.IsNullOrEmpty(value) && value.StartsWith('/')
                ? value.TrimEnd('/')
                : home.TrimEnd('/') + "/" + fallback;
        }
    }
}
