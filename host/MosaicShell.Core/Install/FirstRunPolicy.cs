namespace MosaicShell.Core.Install
{
    /// <summary>
    /// First run (U25): the Host shows one screen with the modules that were set up, already on, then Home. It must never
    /// show after an update. <see cref="Settings.HubSettings.DefaultModulesOffered"/> is new and reads false on every
    /// existing install, so an install that has saved any settings is not a first run either.
    /// </summary>
    public static class FirstRunPolicy
    {
        public static bool IsFirstRun(bool defaultsOffered, bool anySettingsSaved)
        {
            return !defaultsOffered && !anySettingsSaved;
        }

        /// <summary>Whether <paramref name="configDirectory"/> holds any saved settings file, in any subfolder.</summary>
        public static bool AnySettingsSaved(string configDirectory)
        {
            return Directory.Exists(configDirectory)
                && Directory.EnumerateFiles(configDirectory, "*.json", SearchOption.AllDirectories).Any();
        }

        /// <summary>The defaults to turn on: those that are installed and still off.</summary>
        public static IReadOnlyList<string> ToTurnOn(
            IEnumerable<string> defaults,
            Func<string, bool> isInstalled,
            Func<string, bool> isOn)
        {
            return [.. defaults.Where(id => isInstalled(id) && !isOn(id))];
        }
    }
}
