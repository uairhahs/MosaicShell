namespace MosaicShell.Core.Install
{
    /// <summary>What a package install says, from the Hub's Add from file and from <c>--install-package</c>.</summary>
    public static class PackageInstallText
    {
        public const string MissingPath = "--install-package needs the path of a folder or .zip after it.";

        /// <summary>Where the new module shows: widgets on the Widgets page, everything else on Modules.</summary>
        public static string Installed(string moduleId, string? kind)
        {
            string page = string.Equals(kind, "Widget", StringComparison.OrdinalIgnoreCase) ? "Widgets" : "Modules";
            return $"Installed {moduleId}. It is on the {page} page.";
        }

        public static string Failed(string reason)
        {
            return $"Could not install the package: {reason}";
        }
    }
}
