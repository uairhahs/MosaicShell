using System.Security.Cryptography;

namespace MosaicShell.Core.Services
{
    /// <summary>
    /// One log line saying which source supplied each field the flyout shows. The merged session alone
    /// cannot answer it: an empty artist could be SMTC publishing nothing, a browser source reporting
    /// nothing, or a merge that dropped what one of them sent.
    /// </summary>
    public static class MediaSourceAttribution
    {
        private const int MaxTitleChars = 80;

        public static string Describe(MediaSessionInfo? smtc, BrowserPlayerSnapshot? browser, MediaSessionInfo? merged)
        {
            return $"{DescribeSmtc(smtc)} {DescribeBrowser(browser)} "
                + $"use title={UseTitle(smtc, browser, merged)} "
                + $"artist={UseArtist(smtc, browser, merged)} "
                + $"art={UseArt(smtc, browser, merged)}";
        }

        private static string DescribeSmtc(MediaSessionInfo? smtc)
        {
            return smtc is null
                ? "smtc=none"
                : $"smtc.app={smtc.AppId ?? "null"} smtc.title={Text(smtc.Title)} "
                    + $"smtc.artist={Text(smtc.Artist)} smtc.art={Art(smtc.ThumbnailPng)}";
        }

        private static string DescribeBrowser(BrowserPlayerSnapshot? browser)
        {
            return browser is null
                ? "browser=none"
                : $"browser=[name={browser.Name} state={browser.State} title=[{Cap(browser.Title)}] artist=[{Cap(browser.Artist)}] "
                    + $"art={Art(browser.CoverPng)}]";
        }

        private static string UseTitle(MediaSessionInfo? smtc, BrowserPlayerSnapshot? browser, MediaSessionInfo? merged)
        {
            string? value = merged?.Title;
            if (string.IsNullOrWhiteSpace(value))
            {
                return "none";
            }

            bool browserHas = browser is not null && string.Equals(browser.Title, value, StringComparison.Ordinal);
            bool smtcRaw = smtc is not null && string.Equals(smtc.Title, value, StringComparison.Ordinal);
            bool smtcStripped = smtc is not null
                && string.Equals(MediaTitleNormalizer.StripSiteSuffix(smtc.Title), value, StringComparison.Ordinal);

            // A browser source wins a tie with a suffix-stripped SMTC title: Merge takes its text when the two agree.
            return browserHas && !smtcRaw ? "browser" : smtcRaw || smtcStripped ? "smtc" : browserHas ? "browser" : "other";
        }

        private static string UseArtist(MediaSessionInfo? smtc, BrowserPlayerSnapshot? browser, MediaSessionInfo? merged)
        {
            string? value = merged?.Artist;
            if (string.IsNullOrWhiteSpace(value))
            {
                return "none";
            }

            bool browserHas = browser is not null && string.Equals(browser.Artist, value, StringComparison.Ordinal);
            bool smtcHas = smtc is not null && string.Equals(smtc.Artist, value, StringComparison.Ordinal);
            return browserHas && !smtcHas ? "browser" : smtcHas ? "smtc" : browserHas ? "browser" : "other";
        }

        private static string UseArt(MediaSessionInfo? smtc, BrowserPlayerSnapshot? browser, MediaSessionInfo? merged)
        {
            byte[]? value = merged?.ThumbnailPng;
            if (value is null || value.Length == 0)
            {
                return "none";
            }

            bool fromSmtc = SameBytes(smtc?.ThumbnailPng, value);
            bool fromBrowser = SameBytes(browser?.CoverPng, value);
            return fromBrowser && !fromSmtc ? "browser" : fromSmtc ? "smtc" : fromBrowser ? "browser" : "cache";
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
