namespace MosaicShell.Core.Install
{
    /// <summary>
    /// The Host installs the default modules (<see cref="HostInstallPolicy.DefaultModuleIds"/>) itself on its first run. This
    /// used to be the installer's job, through Mosaicist, a separate console program that releases shipped only for that.
    /// </summary>
    public static class DefaultModulesPolicy
    {
        /// <summary>
        /// The modules to install now: the defaults on a first run, else none. A first run is one where the defaults have
        /// never been offered and no module is installed, so an update never brings back a module the user removed.
        /// </summary>
        public static IReadOnlyList<string> ToInstall(bool alreadyOffered, bool anyModuleInstalled)
        {
            return alreadyOffered || anyModuleInstalled ? [] : HostInstallPolicy.DefaultModuleIds;
        }

        /// <summary>Status line when some defaults could not be installed.</summary>
        public static string FailedStatus(IReadOnlyCollection<string> failed)
        {
            return $"Could not add {string.Join(" and ", failed)}. Add them from Modules.";
        }
    }
}
