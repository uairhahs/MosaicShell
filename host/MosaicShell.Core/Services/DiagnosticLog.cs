using System.Text;

namespace MosaicShell.Core.Services
{
    /// <summary>
    /// The only sanctioned on-disk diagnostic log. Each file is capped at <see cref="MaxFileBytes"/>
    /// and, when full, rolls over to a single <c>name.1.log</c> backup, so any one log never
    /// occupies more than about twice the cap. Messages are capped at <see cref="MaxMessageChars"/>.
    /// <para>
    /// Inert until a process entry point calls <see cref="Start"/>: library code and tests can log
    /// freely without writing to the user's profile. After <see cref="Start"/>, every
    /// <see cref="Append(string, DiagnosticLogLevel, string)"/> only queues (see
    /// <see cref="DiagnosticLogWriter"/>), so callers on the UI thread never wait on disk.
    /// </para>
    /// Write failures are swallowed: diagnostics must never break the caller.
    /// </summary>
    public static class DiagnosticLog
    {
        public const long MaxFileBytes = 4L * 1024 * 1024;
        public const int MaxMessageChars = 2048;

        private static readonly Lock Gate = new();
        private static readonly UTF8Encoding Utf8 = new(encoderShouldEmitUTF8Identifier: false);
        private static DiagnosticLogWriter? s_writer;
        private static int s_exitHooked;

        public static bool IsStarted => Volatile.Read(ref s_writer) is not null;

        /// <summary>
        /// Starts the process-wide writer for <paramref name="directory"/>. Call once from a
        /// process entry point. Queued lines are flushed on <see cref="Stop"/> and at process exit.
        /// </summary>
        public static void Start(string directory, DiagnosticLogLevel minimumLevel)
        {
            _ = Directory.CreateDirectory(directory);
            DiagnosticLogWriter writer = new(directory, minimumLevel);
            Interlocked.Exchange(ref s_writer, writer)?.Dispose();
            if (Interlocked.Exchange(ref s_exitHooked, 1) == 0)
            {
                AppDomain.CurrentDomain.ProcessExit += static (_, _) => Stop();
            }
        }

        /// <summary>Flushes queued lines (bounded by <see cref="DiagnosticLogWriter.DrainTimeout"/>) and goes inert.</summary>
        public static void Stop()
        {
            Interlocked.Exchange(ref s_writer, null)?.Dispose();
        }

        public static bool IsEnabled(DiagnosticLogLevel level)
        {
            return Volatile.Read(ref s_writer)?.IsEnabled(level) ?? false;
        }

        /// <summary>Queues an Info line for <paramref name="fileName"/> (a bare <c>name.log</c>).</summary>
        public static void Append(string fileName, string message)
        {
            Append(fileName, DiagnosticLogLevel.Info, message);
        }

        public static void Append(string fileName, DiagnosticLogLevel level, string message)
        {
            _ = Volatile.Read(ref s_writer)?.TryWrite(fileName, level, message);
        }

        /// <summary>Synchronously writes one formatted line. For tests and tools, not the UI thread.</summary>
        public static void AppendTo(string path, string message, long maxFileBytes)
        {
            AppendLines(path, [DiagnosticLogFormat.Line(DateTimeOffset.Now, "-", DiagnosticLogLevel.Info, message)], maxFileBytes);
        }

        /// <summary>
        /// Appends already formatted lines, rolling over to the backup whenever the next line would
        /// pass <paramref name="maxFileBytes"/>. Refuses to write through a symbolic link or junction
        /// at the file or its directory, so a planted link cannot redirect log writes elsewhere.
        /// </summary>
        internal static void AppendLines(string path, IReadOnlyList<string> lines, long maxFileBytes)
        {
            if (lines.Count == 0)
            {
                return;
            }

            try
            {
                lock (Gate)
                {
                    if (IsLinked(path))
                    {
                        return;
                    }

                    FileStream? stream = null;
                    try
                    {
                        foreach (string line in lines)
                        {
                            byte[] bytes = Utf8.GetBytes(line + Environment.NewLine);
                            stream ??= OpenForAppend(path);
                            if (stream.Length > 0 && stream.Length + bytes.Length > maxFileBytes)
                            {
                                stream.Dispose();
                                stream = null;
                                string backup = BackupPathFor(path);
                                if (IsLinked(backup))
                                {
                                    return;
                                }

                                File.Move(path, backup, overwrite: true);
                                stream = OpenForAppend(path);
                            }

                            stream.Write(bytes);
                        }
                    }
                    finally
                    {
                        stream?.Dispose();
                    }
                }
            }
            catch
            {
                // soft-fail: another process may hold the file mid-rollover; the next batch retries.
            }
        }

        public static string BackupPathFor(string path)
        {
            string dir = Path.GetDirectoryName(path) ?? string.Empty;
            return Path.Combine(dir, Path.GetFileNameWithoutExtension(path) + ".1" + Path.GetExtension(path));
        }

        internal static bool IsReparsePoint(FileAttributes attributes)
        {
            return (attributes & FileAttributes.ReparsePoint) != 0;
        }

        private static bool IsLinked(string path)
        {
            FileInfo file = new(path);
            if (file.Exists && IsReparsePoint(file.Attributes))
            {
                return true;
            }

            DirectoryInfo? dir = file.Directory;
            return dir is { Exists: true } && IsReparsePoint(dir.Attributes);
        }

        private static FileStream OpenForAppend(string path)
        {
            // Read sharing lets someone tail the log; no write sharing, so lines never interleave.
            return new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read);
        }
    }
}
