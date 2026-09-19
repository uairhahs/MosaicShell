using System.Security.Cryptography;
using MosaicShell.Core.Services.WebNowPlaying;

namespace MosaicShell.Core.Services
{
    /// <summary>
    /// One log line saying which source supplied each field the flyout shows. The merged session alone
    /// cannot answer it: an empty artist could be SMTC publishing nothing, WebNowPlaying reporting
    /// nothing, or a merge that dropped what one of them sent.
    /// </summary>
    public static class MediaSourceAttribution
    {
        private const int MaxTitleChars = 80;

        public static string Describe(MediaSessionInfo? smtc, WnpPlayerSnapshot? wnp, MediaSessionInfo? merged)
        {
            return $"{DescribeSmtc(smtc)} {DescribeWnp(wnp)} "
                + $"use title={UseTitle(smtc, wnp, merged)} "
                + $"artist={UseArtist(smtc, wnp, merged)} "
                + $"art={UseArt(smtc, wnp, merged)}";
        }

        private static string DescribeSmtc(MediaSessionInfo? smtc)
        {
            return smtc is null
                ? "smtc=none"
                : $"smtc.app={smtc.AppId ?? "null"} smtc.title={Text(smtc.Title)} "
                    + $"smtc.artist={Text(smtc.Artist)} smtc.art={Art(smtc.ThumbnailPng)}";
        }

        private static string DescribeWnp(WnpPlayerSnapshot? wnp)
        {
            return wnp is null
                ? "wnp=none"
                : $"wnp=[name={wnp.Name} state={wnp.State} title=[{Cap(wnp.Title)}] artist=[{Cap(wnp.Artist)}] "
                    + $"art={Art(wnp.CoverPng)}]";
        }

        private static string UseTitle(MediaSessionInfo? smtc, WnpPlayerSnapshot? wnp, MediaSessionInfo? merged)
        {
            string? value = merged?.Title;
            if (string.IsNullOrWhiteSpace(value))
            {
                return "none";
            }

            bool wnpHas = wnp is not null && string.Equals(wnp.Title, value, StringComparison.Ordinal);
            bool smtcRaw = smtc is not null && string.Equals(smtc.Title, value, StringComparison.Ordinal);
            bool smtcStripped = smtc is not null
                && string.Equals(MediaTitleNormalizer.StripSiteSuffix(smtc.Title), value, StringComparison.Ordinal);

            // WebNowPlaying wins a tie with a suffix-stripped SMTC title: Merge takes its text when the two agree.
            return wnpHas && !smtcRaw ? "wnp" : smtcRaw || smtcStripped ? "smtc" : wnpHas ? "wnp" : "other";
        }

        private static string UseArtist(MediaSessionInfo? smtc, WnpPlayerSnapshot? wnp, MediaSessionInfo? merged)
        {
            string? value = merged?.Artist;
            if (string.IsNullOrWhiteSpace(value))
            {
                return "none";
            }

            bool wnpHas = wnp is not null && string.Equals(wnp.Artist, value, StringComparison.Ordinal);
            bool smtcHas = smtc is not null && string.Equals(smtc.Artist, value, StringComparison.Ordinal);
            return wnpHas && !smtcHas ? "wnp" : smtcHas ? "smtc" : wnpHas ? "wnp" : "other";
        }

        private static string UseArt(MediaSessionInfo? smtc, WnpPlayerSnapshot? wnp, MediaSessionInfo? merged)
        {
            byte[]? value = merged?.ThumbnailPng;
            if (value is null || value.Length == 0)
            {
                return "none";
            }

            bool fromSmtc = SameBytes(smtc?.ThumbnailPng, value);
            bool fromWnp = SameBytes(wnp?.CoverPng, value);
            return fromWnp && !fromSmtc ? "wnp" : fromSmtc ? "smtc" : fromWnp ? "wnp" : "cache";
        }

        private static bool SameBytes(byte[]? a, byte[]? b)
        {
            return a is not null && b is not null && a.AsSpan().SequenceEqual(b);
        }

        /// <summary>Null and empty are different facts: null means the source did not publish the field.</summary>
        private static string Text(string? value)
        {
            return value is null ? "null" : $"[{Cap(value)}]";
        }

        private static string Cap(string value)
        {
            return value.Length <= MaxTitleChars ? value : value[..MaxTitleChars];
        }

        /// <summary>Identity, not length: two same-sized covers must not look the same.</summary>
        private static string Art(byte[]? bytes)
        {
            return bytes is null
                ? "none"
                : $"{bytes.Length}#{Convert.ToHexString(SHA256.HashData(bytes), 0, 4).ToLowerInvariant()}";
        }
    }
}
