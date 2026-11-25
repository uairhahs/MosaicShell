namespace MosaicShell.Host.Capabilities
{
    /// <summary>The native library the Host's X11 helpers (layer, focus dim, blur hint) call into.</summary>
    internal static class X11Native
    {
        /// <summary>Xlib by its versioned soname, which every distribution ships; the bare libX11.so is a dev package's.</summary>
        public const string LibX11 = "libX11.so.6";
    }
}
