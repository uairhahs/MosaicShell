using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace MosaicShell.Core.Services
{
    /// <summary>
    /// Pure formatting rules for diagnostic log lines and file names.
    /// <para>
    /// Logged text includes values from outside the process (track titles from browsers and media
    /// apps, exception messages), so it is untrusted. Every message is written as exactly one line:
    /// line breaks, other control characters and bidirectional overrides are escaped, so a title
    /// cannot forge a second log line or visually reorder one in a viewer.
    /// </para>
    /// </summary>
    public static partial class DiagnosticLogFormat
    {
        public const string TruncationMarker = "…[truncated]";

        /// <summary>
        /// Upper bound on one formatted line in characters, newline included: the date, run id and
        /// level prefix, the capped message and the truncation marker.
        /// </summary>
        public const int MaxLineChars = DiagnosticLog.MaxMessageChars + 96;

        private const int MaxFileNameChars = 64;

        /// <summary><c>2026-10-04T13:05:01.123+01:00 run=abcd1234 WARN message</c></summary>
        public static string Line(DateTimeOffset at, string runId, DiagnosticLogLevel level, string message)
        {
            string when = at.ToString("yyyy-MM-dd'T'HH:mm:ss.fffzzz", CultureInfo.InvariantCulture);
            return $"{when} run={runId} {DiagnosticLogLevels.Tag(level)} {Sanitize(message)}";
        }

        /// <summary>Escapes characters that could break or spoof a line, then caps the length.</summary>
        public static string Sanitize(string? message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return string.Empty;
            }

            StringBuilder sb = new(Math.Min(message.Length, DiagnosticLog.MaxMessageChars) + 16);
            foreach (char c in message)
            {
                if (sb.Length >= DiagnosticLog.MaxMessageChars)
                {
                    sb.Length = DiagnosticLog.MaxMessageChars;
                    _ = sb.Append(TruncationMarker);
                    return sb.ToString();
                }

                _ = c switch
                {
                    '\r' => sb.Append("\\r"),
                    '\n' => sb.Append("\\n"),
                    '\t' => sb.Append(c),
                    _ when NeedsEscape(c) => sb.Append("\\u").Append(((int)c).ToString("X4", CultureInfo.InvariantCulture)),
                    _ => sb.Append(c)
                };
            }

            if (sb.Length > DiagnosticLog.MaxMessageChars)
            {
                sb.Length = DiagnosticLog.MaxMessageChars;
                _ = sb.Append(TruncationMarker);
            }

            return sb.ToString();
        }

        /// <summary>
        /// A bare <c>name.log</c>: letters, digits, <c>-</c> and <c>_</c>, starting with a letter or
        /// digit, and not a Windows device name. Anything else (separators, <c>..</c>, drive
        /// prefixes, alternate data streams, other extensions) is refused, so a caller cannot steer
        /// a write outside the log directory.
        /// </summary>
        public static bool IsSafeFileName(string? fileName)
        {
            if (fileName is null || fileName.Length > MaxFileNameChars || !SafeFileName().IsMatch(fileName))
            {
                return false;
            }

            string stem = fileName[..^".log".Length];
            return !ReservedDeviceName().IsMatch(stem);
        }

        /// <summary>Eight lowercase hex characters from a cryptographic source, unique per run.</summary>
        public static string NewRunId()
        {
            return Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(4));
        }

        private static bool NeedsEscape(char c)
        {
            int code = c;
            return char.IsControl(c) // C0, DEL and C1, including NEL (U+0085)
                || code is 0x2028 or 0x2029 // line and paragraph separators
                || code is >= 0x202A and <= 0x202E // bidi embeddings and overrides
                || code is >= 0x2066 and <= 0x2069; // bidi isolates
        }

        [GeneratedRegex("^[A-Za-z0-9][A-Za-z0-9_-]*\\.log$", RegexOptions.CultureInvariant)]
        private static partial Regex SafeFileName();

        [GeneratedRegex("^(con|prn|aux|nul|com[0-9]|lpt[0-9])$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex ReservedDeviceName();
    }
}
