namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// A fact that only means something off Windows (FIFOs, Unix file types). On Windows it is reported as skipped,
    /// not dropped, so the Windows test run shows what it did not cover.
    /// </summary>
    public sealed class UnixFactAttribute : FactAttribute
    {
        public UnixFactAttribute()
        {
            if (OperatingSystem.IsWindows())
            {
                Skip = "Unix only.";
            }
        }
    }
}
