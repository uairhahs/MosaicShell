namespace MosaicShell.Core.Services.Gnome
{
    /// <summary>
    /// GNOME has no tray without an extension, so a closed Hub had no way back. While the Host runs on GNOME it owns
    /// <see cref="BusName"/> and answers <see cref="ShowHubMethod"/> and <see cref="QuitMethod"/>; MosaicShell's Shell
    /// extension (<see cref="GnomeOsdGuardExtension"/>) shows an icon in the top bar while the name has an owner. This is
    /// not the pop-up guard name, which is owned only while Tessera is on. The icon's menu also lists the installed modules
    /// with a switch each (U24): it asks <see cref="ModulesMethod"/> when it opens and calls <see cref="SetModuleOnMethod"/>.
    /// </summary>
    public static class GnomeHubPresencePolicy
    {
        public const string BusName = "org.mosaicshell.Hub";
        public const string ObjectPath = "/org/mosaicshell/Hub";
        public const string Interface = "org.mosaicshell.Hub";
        public const string ShowHubMethod = "ShowHub";
        public const string QuitMethod = "Quit";

        /// <summary>Returns the installed modules as <c>a(ssb)</c>: id, label, on (<see cref="HubShell.TrayMenu"/>).</summary>
        public const string ModulesMethod = "Modules";

        /// <summary>Takes <c>(sb)</c>: a module's id and whether to turn it on.</summary>
        public const string SetModuleOnMethod = "SetModuleOn";
    }
}
