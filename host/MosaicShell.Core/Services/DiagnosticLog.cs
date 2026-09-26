namespace MosaicShell.Core.Services
{
    /// <summary>
    /// The only sanctioned on-disk diagnostic log writer. Each file is capped at
    /// <see cref="MaxFileBytes"/> and, when full, rolls over to a single <c>name.1.log</c> backup,
    /// so any one log never occupies more than about twice the cap. Messages are truncated to
    /// <see cref="MaxMessageChars"/>. Write failures are swallowed: diagnostics must never break
    /// the caller.
    /// </summary>
    public static class DiagnosticLog
    {
        public const long MaxFileBytes = 4L * 1024 * 1024;
        public const int MaxMessageChars = 2048;

        private static readonly Lock Gate = new();

        /// <summary>Appends a timestamped line to <paramref name="fileName"/> under <see cref="AppPaths.CacheDirectory"/>.</summary>
        public static void Append(string fileName, string message)
        {
            try
            {
                AppPaths.EnsureLayout();
                AppendTo(Path.Combine(AppPaths.CacheDirectory, fileName), message, MaxFileBytes);
            }
            catch
            {
                // soft-fail
            }
        }

        public static void AppendTo(string path, string message, long maxFileBytes)
        {
            if (message.Length > MaxMessageChars)
            {
                message = string.Concat(message.AsSpan(0, MaxMessageChars), "…[truncated]");
            }

            string line = $"{DateTime.Now:HH:mm:ss.fff} {message}{Environment.NewLine}";
            try
            {
                lock (Gate)
                {
                    FileInfo info = new(path);
                    if (info.Exists && info.Length + line.Length > maxFileBytes)
                    {
                        File.Move(path, BackupPathFor(path), overwrite: true);
                    }

                    File.AppendAllText(path, line);
                }
            }
            catch
            {
                // soft-fail: another process may hold the file mid-rollover; the next call retries.
            }
        }

        public static string BackupPathFor(string path)
        {
            string dir = Path.GetDirectoryName(path) ?? string.Empty;
            return Path.Combine(dir, Path.GetFileNameWithoutExtension(path) + ".1" + Path.GetExtension(path));
        }
    }
}
