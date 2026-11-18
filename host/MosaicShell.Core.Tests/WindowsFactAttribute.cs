namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// A fact that only means something on Windows (Win32 ACLs, WinUI acrylic). Elsewhere it is reported as skipped,
    /// not dropped, so the portable test run shows what it did not cover.
    /// </summary>
    public sealed class WindowsFactAttribute : FactAttribute
    {
        public WindowsFactAttribute()
        {
            if (!OperatingSystem.IsWindows())
            {
                Skip = "Windows only.";
            }
        }
    }
}
