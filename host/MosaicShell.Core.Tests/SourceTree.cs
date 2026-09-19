namespace MosaicShell.Core.Tests
{
    /// <summary>Locates production sources for source-scan (ratchet) tests.</summary>
    internal static class SourceTree
    {
        public static string HostRoot => Path.Combine(RepoRoot(), "host");

        /// <summary>Every production <c>.cs</c> file under <paramref name="relativeDirs"/> (relative to <c>host</c>), skipping build output.</summary>
        public static IEnumerable<string> EnumerateSources(params string[] relativeDirs)
        {
            foreach (string relative in relativeDirs)
            {
                string dir = Path.Combine(HostRoot, relative);
                if (!Directory.Exists(dir))
                {
                    continue;
                }

                foreach (string file in Directory.EnumerateFiles(dir, "*.cs", SearchOption.AllDirectories))
                {
                    if (!IsBuildOutput(file))
                    {
                        yield return file;
                    }
                }
            }
        }

        public static string RelativeToHost(string file)
        {
            return Path.GetRelativePath(HostRoot, file);
        }

        private static bool IsBuildOutput(string path)
        {
            string[] segments = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return segments.Contains("obj") || segments.Contains("bin");
        }

        internal static string RepoRoot()
        {
            foreach (string start in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
            {
                DirectoryInfo? dir = new(start);
                while (dir is not null)
                {
                    if (Directory.Exists(Path.Combine(dir.FullName, "host", "MosaicShell.Core")))
                    {
                        return dir.FullName;
                    }

                    dir = dir.Parent;
                }
            }

            throw new DirectoryNotFoundException("Could not locate the repo root (host/MosaicShell.Core).");
        }
    }
}
