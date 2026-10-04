using MosaicShell.Core;
using MosaicShell.Core.Capabilities.Platform;

// Host code reads process-wide state (AppPaths, HostLaunchOptions, the Tessera palette), so tests
// in this assembly run one at a time.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace MosaicShell.Host.Tests
{
    /// <summary>
    /// Isolates a Host test from the user's machine: settings and logs resolve under a temp root,
    /// not %LocalAppData%\MosaicShell, and launch options start from defaults.
    /// </summary>
    public abstract class HeadlessHostTest : IDisposable
    {
        private readonly string _root = Path.Combine(Path.GetTempPath(), "MosaicHostTests_" + Guid.NewGuid().ToString("N"));

        protected HeadlessHostTest(params string[] launchArgs)
        {
            AppPaths.SetRootOverride(_root);
            AppPaths.EnsureLayout();
            HostLaunchOptions.Apply(launchArgs);
        }

        public void Dispose()
        {
            HostLaunchOptions.Apply([]);
            AppPaths.ClearRootOverride();
            try { Directory.Delete(_root, recursive: true); } catch { /* best effort */ }
            GC.SuppressFinalize(this);
        }
    }
}
