using System.Net;
using System.Net.Sockets;

namespace MosaicShell.Core.Services.BrowserBridge
{
    /// <summary>
    /// Decides which artwork the Host may fetch, and from where. The URL comes from a web page and the extension runs
    /// on every https site, so any page can name any address; the Host fetches only from the public internet.
    /// </summary>
    public static class BrowserArtworkPolicy
    {
        /// <summary>Sizes a page claims above this are ignored so a false claim cannot outrank an honest entry.</summary>
        private const int MaxClaimedSide = 4096;

        private static readonly string[] LocalSuffixes = [".localhost", ".local", ".internal", ".lan", ".home.arpa"];

        /// <summary>The best entry the Host can fetch: https from a public address, largest advertised area first.</summary>
        public static BrowserArtwork? Choose(IReadOnlyList<BrowserArtwork> artwork)
        {
            BrowserArtwork? best = null;
            long bestArea = -1;
            foreach (BrowserArtwork candidate in artwork)
            {
                if (!IsFetchableUrl(candidate.Src))
                {
                    continue;
                }

                long area = LargestArea(candidate.Sizes);
                if (area > bestArea)
                {
                    best = candidate;
                    bestArea = area;
                }
            }

            return best;
        }

        /// <summary>https to a public host name or public IP literal, with no user info.</summary>
        public static bool IsFetchableUrl(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri)
                || uri.Scheme != Uri.UriSchemeHttps
                || uri.UserInfo.Length > 0)
            {
                return false;
            }

            string host = uri.IdnHost;
            return IPAddress.TryParse(host.Trim('[', ']'), out IPAddress? literal)
                ? IsPublicAddress(literal)
                : host.Contains('.', StringComparison.Ordinal)
                    && !host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
                    && !LocalSuffixes.Any(suffix => host.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// False for loopback, private, link-local, carrier-grade NAT, multicast, reserved and unspecified addresses,
        /// including an IPv4 address written as IPv6. Applied to the resolved address as well as to literals, so a
        /// name that resolves to the user's own network is refused too.
        /// </summary>
        public static bool IsPublicAddress(IPAddress address)
        {
            if (address.IsIPv4MappedToIPv6)
            {
                address = address.MapToIPv4();
            }

            if (IPAddress.IsLoopback(address) || address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any))
            {
                return false;
            }

            byte[] b = address.GetAddressBytes();
            return address.AddressFamily switch
            {
                AddressFamily.InterNetwork => IsPublicV4(b),
                AddressFamily.InterNetworkV6 => IsPublicV6(address, b),
                _ => false,
            };
        }

        /// <summary>Png, jpeg or webp, by signature. The declared content type is not trusted.</summary>
        public static bool LooksLikeSupportedImage(ReadOnlySpan<byte> bytes)
        {
            return bytes.Length >= 12
                && ((bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47)
                    || (bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
                    || (bytes[0] == 0x52 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x46
                        && bytes[8] == 0x57 && bytes[9] == 0x45 && bytes[10] == 0x42 && bytes[11] == 0x50));
        }

        private static bool IsPublicV4(byte[] b)
        {
            return !(b[0] == 0
                || b[0] == 10
                || (b[0] == 100 && b[1] is >= 64 and <= 127)
                || b[0] == 127
                || (b[0] == 169 && b[1] == 254)
                || (b[0] == 172 && b[1] is >= 16 and <= 31)
                || (b[0] == 192 && b[1] == 0 && b[2] == 0)
                || (b[0] == 192 && b[1] == 168)
                || b[0] >= 224);
        }

        private static bool IsPublicV6(IPAddress address, byte[] b)
        {
            return !(address.IsIPv6LinkLocal
                || address.IsIPv6SiteLocal
                || address.IsIPv6Multicast
                || (b[0] & 0xFE) == 0xFC);
        }

        /// <summary>The largest "WxH" in a space separated sizes list, 0 when none parses, capped per side.</summary>
        private static long LargestArea(string sizes)
        {
            long largest = 0;
            foreach (string size in sizes.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                int x = size.IndexOf('x', StringComparison.OrdinalIgnoreCase);
                if (x > 0
                    && int.TryParse(size[..x], out int w)
                    && int.TryParse(size[(x + 1)..], out int h)
                    && w is > 0 and <= MaxClaimedSide
                    && h is > 0 and <= MaxClaimedSide)
                {
                    largest = Math.Max(largest, (long)w * h);
                }
            }

            return largest;
        }
    }
}
