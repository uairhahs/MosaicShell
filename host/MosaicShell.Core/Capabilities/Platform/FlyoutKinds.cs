namespace MosaicShell.Core.Capabilities.Platform
{
    /// <summary>
    /// The flyout kinds a request names. The values travel over the capability IPC and into the logs, so they stay as
    /// they are; kinds compare without regard to case. Which kinds carry a media strip and which are status chips is
    /// <see cref="MediaFlyoutRouter"/>'s to say.
    /// </summary>
    public static class FlyoutKinds
    {
        public const string Volume = "vol";
        public const string Brightness = "bright";
        public const string Media = "media";
        public const string Locks = "locks";
        public const string Flight = "flight";

        public static bool Is(string? kind, string expected)
        {
            return string.Equals(kind, expected, StringComparison.OrdinalIgnoreCase);
        }
    }
}
