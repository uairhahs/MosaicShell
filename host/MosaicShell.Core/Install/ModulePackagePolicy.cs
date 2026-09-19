namespace MosaicShell.Core.Install
{
    /// <summary>
    /// Trust boundary for module packages. A module id can arrive from an untrusted
    /// <c>module.manifest.json</c> inside a zip, and the install path both deletes and writes the
    /// directory it names, so an id must be a single, plain, non-reserved path segment.
    /// </summary>
    public static class ModulePackagePolicy
    {
        /// <summary>Longest accepted module id. Keeps installed paths well inside MAX_PATH.</summary>
        public const int MaxModuleIdLength = 64;

        private static readonly string[] ReservedDeviceNames =
        [
            "CON", "PRN", "AUX", "NUL",
            "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
            "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
        ];

        public static bool IsValidModuleId(string? moduleId)
        {
            if (string.IsNullOrWhiteSpace(moduleId) || moduleId.Length > MaxModuleIdLength)
            {
                return false;
            }

            if (moduleId is "." or "..")
            {
                return false;
            }

            // Windows silently trims these, so the id on disk would not match the id in the manifest.
            if (moduleId != moduleId.Trim() || moduleId.EndsWith('.'))
            {
                return false;
            }

            if (moduleId.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                return false;
            }

            if (moduleId.Contains('/') || moduleId.Contains('\\') || moduleId.Contains(':'))
            {
                return false;
            }

            if (Path.IsPathRooted(moduleId) || Path.IsPathFullyQualified(moduleId))
            {
                return false;
            }

            // A rooted or multi-segment id would not survive this round trip unchanged.
            if (!string.Equals(Path.GetFileName(moduleId), moduleId, StringComparison.Ordinal))
            {
                return false;
            }

            string stem = Path.GetFileNameWithoutExtension(moduleId);
            return !ReservedDeviceNames.Contains(stem, StringComparer.OrdinalIgnoreCase);
        }

        public static string EnsureValidModuleId(string? moduleId)
        {
            return IsValidModuleId(moduleId)
                ? moduleId!
                : throw new InvalidOperationException(
                    $"Invalid module id '{moduleId}'. A module id must be a single path segment: " +
                    "no separators, no '..', no drive prefix, no reserved device name, " +
                    $"at most {MaxModuleIdLength} characters.");
        }

        /// <summary>
        /// Resolves the install directory for <paramref name="moduleId"/>, throwing if the result
        /// would land outside <paramref name="modulesRoot"/>.
        /// </summary>
        public static string ResolveModuleDirectory(string modulesRoot, string? moduleId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modulesRoot);
            string id = EnsureValidModuleId(moduleId);

            string root = Path.GetFullPath(modulesRoot);
            string resolved = Path.GetFullPath(Path.Combine(root, id));

            string rootWithSeparator = root.EndsWith(Path.DirectorySeparatorChar)
                ? root
                : root + Path.DirectorySeparatorChar;

            return resolved.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase)
                ? resolved
                : throw new InvalidOperationException(
                    $"Module id '{moduleId}' resolves outside the modules directory.");
        }
    }
}
