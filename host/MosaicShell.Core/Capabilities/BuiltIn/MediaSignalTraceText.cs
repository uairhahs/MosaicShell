using System.Security.Cryptography;

namespace MosaicShell.Core.Capabilities.BuiltIn
{
    /// <summary>
    /// Text for the `media signal` trace line. Values are kept whole: the diagnostic log already
    /// escapes line breaks and caps each line, and it records track names only at Debug level.
    /// </summary>
    internal static class MediaSignalTraceText
    {
        /// <summary>The value, or a dash when it is missing.</summary>
        public static string Text(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? "-" : value;
        }

        /// <summary>Length and a short content hash, so a different picture of the same size shows as a change.</summary>
        public static string Art(byte[]? png)
        {
            if (png is null)
            {
                return "-";
            }

            return $"{png.Length}:{Convert.ToHexString(SHA256.HashData(png))[..8].ToLowerInvariant()}";
        }
    }
}
