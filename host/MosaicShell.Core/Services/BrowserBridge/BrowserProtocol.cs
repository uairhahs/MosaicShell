using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace MosaicShell.Core.Services.BrowserBridge
{
    /// <summary>A message the browser extension sends to the Host.</summary>
    public abstract record BrowserInbound;

    /// <summary>First message on a connection: which protocol the extension speaks and where it runs.</summary>
    public sealed record BrowserHello(int Protocol, string ExtensionVersion, string Browser) : BrowserInbound;

    public sealed record BrowserArtwork(string Src, string Sizes);

    /// <summary>
    /// What one tab currently publishes. Sent when it changes. The Host stamps the time it received the report,
    /// so ordering never depends on the extension's clock.
    /// </summary>
    public sealed record BrowserSessionReport(
        int TabId,
        int WindowId,
        string Origin,
        string Title,
        string Artist,
        string Album,
        IReadOnlyList<BrowserArtwork> Artwork,
        BrowserPlaybackState PlaybackState,
        bool Audible,
        BrowserRating Rating,
        BrowserMediaCapabilities Capabilities) : BrowserInbound;

    /// <summary>The tab stopped publishing media: closed, navigated away, or the page cleared its metadata.</summary>
    public sealed record BrowserSessionRemoved(int TabId) : BrowserInbound;

    public sealed record BrowserPing : BrowserInbound
    {
        public static BrowserPing Instance { get; } = new();
    }

    /// <summary>The state the Host wants a tab's rating to end in. The extension decides which click, if any, gets there.</summary>
    public enum BrowserCommandAction
    {
        Like = 0,
        Dislike = 1,
        Clear = 2,
    }

    public enum BrowserParseError
    {
        None = 0,
        TooLarge,
        InvalidJson,
        NotAnObject,
        MissingType,

        /// <summary>A well-formed message of a kind this Host does not know. Callers ignore it.</summary>
        UnknownType,

        /// <summary>A hello for a protocol version this Host does not speak. Callers drop the connection.</summary>
        UnsupportedProtocol,
        InvalidField,
    }

    public readonly record struct BrowserParseResult(BrowserInbound? Message, BrowserParseError Error, string? Detail = null)
    {
        public static BrowserParseResult Ok(BrowserInbound message)
        {
            return new BrowserParseResult(message, BrowserParseError.None);
        }

        public static BrowserParseResult Fail(BrowserParseError error, string? detail = null)
        {
            return new BrowserParseResult(null, error, detail);
        }
    }

    /// <summary>
    /// Wire format between the extension and the Host (JSON, one message per native messaging frame). The
    /// extension is a separate process the Host does not control, so parsing is strict about structure and
    /// identifiers, ignores unknown fields, truncates over-long text and drops unusable artwork entries.
    /// </summary>
    public static class BrowserProtocol
    {
        public const int Version = 1;
        public const int MaxMessageBytes = 64 * 1024;
        public const int MaxTextLength = 512;
        public const int MaxOriginLength = 300;
        public const int MaxUrlLength = 2048;
        public const int MaxSizesLength = 32;
        public const int MaxArtwork = 8;

        private static readonly JsonDocumentOptions ParseOptions = new() { MaxDepth = 8 };

        public static BrowserParseResult Parse(ReadOnlySpan<byte> utf8Json)
        {
            if (utf8Json.Length > MaxMessageBytes)
            {
                return BrowserParseResult.Fail(BrowserParseError.TooLarge);
            }

            try
            {
                using JsonDocument document = JsonDocument.Parse(utf8Json.ToArray(), ParseOptions);
                return Dispatch(document.RootElement);
            }
            catch (JsonException)
            {
                return BrowserParseResult.Fail(BrowserParseError.InvalidJson);
            }
        }

        public static byte[] SerializeCommand(int tabId, BrowserCommandAction action)
        {
            using MemoryStream stream = new();
            using (Utf8JsonWriter writer = new(stream))
            {
                writer.WriteStartObject();
                writer.WriteString("type", "command");
                writer.WriteNumber("tabId", tabId);
                writer.WriteString("action", action switch
                {
                    BrowserCommandAction.Like => "like",
                    BrowserCommandAction.Dislike => "dislike",
                    _ => "clear",
                });
                writer.WriteEndObject();
            }

            return stream.ToArray();
        }

        private static BrowserParseResult Dispatch(JsonElement root)
        {
            return root.ValueKind != JsonValueKind.Object
                ? BrowserParseResult.Fail(BrowserParseError.NotAnObject)
                : !root.TryGetProperty("type", out JsonElement type) || type.ValueKind != JsonValueKind.String
                    ? BrowserParseResult.Fail(BrowserParseError.MissingType)
                    : type.GetString() switch
                    {
                        "hello" => ParseHello(root),
                        "session" => ParseSession(root),
                        "removed" => ParseRemoved(root),
                        "ping" => BrowserParseResult.Ok(BrowserPing.Instance),
                        _ => BrowserParseResult.Fail(BrowserParseError.UnknownType),
                    };
        }

        private static BrowserParseResult ParseHello(JsonElement root)
        {
            return !TryReadInt(root, "protocol", out int protocol)
                ? Invalid("protocol")
                : protocol != Version
                    ? BrowserParseResult.Fail(BrowserParseError.UnsupportedProtocol, "protocol")
                    : !TryReadText(root, "extensionVersion", MaxSizesLength, out string version) || version.Length == 0
                        ? Invalid("extensionVersion")
                        : !TryReadText(root, "browser", MaxSizesLength, out string browser) || browser.Length == 0
                            ? Invalid("browser")
                            : BrowserParseResult.Ok(new BrowserHello(protocol, version, browser));
        }

        private static BrowserParseResult ParseRemoved(JsonElement root)
        {
            return TryReadInt(root, "tabId", out int tabId)
                ? BrowserParseResult.Ok(new BrowserSessionRemoved(tabId))
                : Invalid("tabId");
        }

        private static BrowserParseResult ParseSession(JsonElement root)
        {
            return TryReadSession(root, out BrowserSessionReport? report, out string badField)
                ? BrowserParseResult.Ok(report)
                : Invalid(badField);
        }

        /// <summary>Reads the fields in wire order and names the first one that is missing or malformed.</summary>
        private static bool TryReadSession(JsonElement root, [NotNullWhen(true)] out BrowserSessionReport? report, out string badField)
        {
            report = null;
            badField = "";

            if (!TryReadInt(root, "tabId", out int tabId))
            {
                badField = "tabId";
                return false;
            }

            if (!TryReadInt(root, "windowId", out int windowId))
            {
                badField = "windowId";
                return false;
            }

            if (!TryReadOrigin(root, out string origin))
            {
                badField = "origin";
                return false;
            }

            if (!TryReadPlaybackState(root, out BrowserPlaybackState state))
            {
                badField = "playbackState";
                return false;
            }

            if (!TryReadText(root, "title", MaxTextLength, out string title))
            {
                badField = "title";
                return false;
            }

            if (!TryReadText(root, "artist", MaxTextLength, out string artist))
            {
                badField = "artist";
                return false;
            }

            if (!TryReadText(root, "album", MaxTextLength, out string album))
            {
                badField = "album";
                return false;
            }

            if (!TryReadArtwork(root, out IReadOnlyList<BrowserArtwork> artwork))
            {
                badField = "artwork";
                return false;
            }

            if (!TryReadAudible(root, out bool audible))
            {
                badField = "audible";
                return false;
            }

            if (!TryReadRating(root, out BrowserRating rating))
            {
                badField = "rating";
                return false;
            }

            if (!TryReadCapabilities(root, out BrowserMediaCapabilities capabilities))
            {
                badField = "capabilities";
                return false;
            }

            report = new BrowserSessionReport(
                tabId, windowId, origin, title, artist, album, artwork, state, audible, rating, capabilities);
            return true;
        }

        private static BrowserParseResult Invalid(string field)
        {
            return BrowserParseResult.Fail(BrowserParseError.InvalidField, field);
        }

        /// <summary>A required identifier: a JSON integer from 0 to <see cref="int.MaxValue"/>.</summary>
        private static bool TryReadInt(JsonElement root, string name, out int value)
        {
            value = 0;
            return root.TryGetProperty(name, out JsonElement element)
                && element.ValueKind == JsonValueKind.Number
                && element.TryGetInt32(out value)
                && value >= 0;
        }

        /// <summary>
        /// Optional text. Absent and null read as empty; any other type fails. Control characters become spaces
        /// (titles reach log lines) and the result is cut to <paramref name="cap"/> without splitting a surrogate pair.
        /// </summary>
        private static bool TryReadText(JsonElement root, string name, int cap, out string value)
        {
            value = "";
            if (!root.TryGetProperty(name, out JsonElement element) || element.ValueKind == JsonValueKind.Null)
            {
                return true;
            }

            if (element.ValueKind != JsonValueKind.String)
            {
                return false;
            }

            string? raw;
            try
            {
                raw = element.GetString();
            }
            catch (InvalidOperationException)
            {
                return false;
            }

            value = Clean(raw ?? "", cap);
            return true;
        }

        private static string Clean(string raw, int cap)
        {
            int length = Math.Min(raw.Length, cap);
            if (length < raw.Length && length > 0 && char.IsHighSurrogate(raw[length - 1]))
            {
                length--;
            }

            return string.Create(length, raw, static (span, source) =>
            {
                for (int i = 0; i < span.Length; i++)
                {
                    char c = source[i];
                    span[i] = char.IsControl(c) ? ' ' : c;
                }
            });
        }

        /// <summary>A plain https origin (no user info, path, query or fragment), returned as scheme, host and port.</summary>
        private static bool TryReadOrigin(JsonElement root, out string origin)
        {
            origin = "";
            if (!root.TryGetProperty("origin", out JsonElement element)
                || element.ValueKind != JsonValueKind.String
                || element.GetString() is not { Length: > 0 and <= MaxOriginLength } text
                || !Uri.TryCreate(text, UriKind.Absolute, out Uri? uri)
                || uri.Scheme != Uri.UriSchemeHttps
                || uri.UserInfo.Length > 0
                || uri.AbsolutePath != "/"
                || uri.Query.Length > 0
                || uri.Fragment.Length > 0)
            {
                return false;
            }

            origin = uri.GetLeftPart(UriPartial.Authority);
            return true;
        }

        private static bool TryReadPlaybackState(JsonElement root, out BrowserPlaybackState state)
        {
            state = BrowserPlaybackState.Stopped;
            if (!root.TryGetProperty("playbackState", out JsonElement element) || element.ValueKind != JsonValueKind.String)
            {
                return false;
            }

            switch (element.GetString())
            {
                case "playing":
                    state = BrowserPlaybackState.Playing;
                    return true;
                case "paused":
                    state = BrowserPlaybackState.Paused;
                    return true;
                case "none":
                    return true;
                default:
                    return false;
            }
        }

        private static bool TryReadRating(JsonElement root, out BrowserRating rating)
        {
            rating = BrowserRating.None;
            if (!root.TryGetProperty("rating", out JsonElement element) || element.ValueKind == JsonValueKind.Null)
            {
                return true;
            }

            if (element.ValueKind != JsonValueKind.String)
            {
                return false;
            }

            switch (element.GetString())
            {
                case "none":
                    return true;
                case "liked":
                    rating = BrowserRating.Liked;
                    return true;
                case "disliked":
                    rating = BrowserRating.Disliked;
                    return true;
                default:
                    return false;
            }
        }

        private static bool TryReadAudible(JsonElement root, out bool audible)
        {
            audible = false;
            if (!root.TryGetProperty("audible", out JsonElement element) || element.ValueKind == JsonValueKind.Null)
            {
                return true;
            }

            if (element.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
            {
                return false;
            }

            audible = element.GetBoolean();
            return true;
        }

        /// <summary>Names the Host does not know, such as ones a newer extension adds, are ignored.</summary>
        private static bool TryReadCapabilities(JsonElement root, out BrowserMediaCapabilities capabilities)
        {
            capabilities = BrowserMediaCapabilities.None;
            if (!root.TryGetProperty("capabilities", out JsonElement element) || element.ValueKind == JsonValueKind.Null)
            {
                return true;
            }

            if (element.ValueKind != JsonValueKind.Array)
            {
                return false;
            }

            foreach (JsonElement item in element.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.String)
                {
                    return false;
                }

                capabilities |= item.GetString() switch
                {
                    "rating" => BrowserMediaCapabilities.Rating,
                    "dislike" => BrowserMediaCapabilities.Dislike,
                    "shuffle" => BrowserMediaCapabilities.Shuffle,
                    "repeat" => BrowserMediaCapabilities.Repeat,
                    _ => BrowserMediaCapabilities.None,
                };
            }

            return true;
        }

        /// <summary>
        /// Keeps the first <see cref="MaxArtwork"/> usable entries. An entry the Host cannot use (wrong shape, a
        /// scheme other than https or blob, too long) is dropped rather than failing the whole report.
        /// </summary>
        private static bool TryReadArtwork(JsonElement root, out IReadOnlyList<BrowserArtwork> artwork)
        {
            artwork = [];
            if (!root.TryGetProperty("artwork", out JsonElement element) || element.ValueKind == JsonValueKind.Null)
            {
                return true;
            }

            if (element.ValueKind != JsonValueKind.Array)
            {
                return false;
            }

            List<BrowserArtwork> kept = [];
            foreach (JsonElement item in element.EnumerateArray())
            {
                if (kept.Count == MaxArtwork)
                {
                    break;
                }

                if (item.ValueKind == JsonValueKind.Object
                    && item.TryGetProperty("src", out JsonElement src)
                    && src.ValueKind == JsonValueKind.String
                    && src.GetString() is { Length: > 0 and <= MaxUrlLength } url
                    && Uri.TryCreate(url, UriKind.Absolute, out Uri? uri)
                    && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == "blob")
                    && TryReadText(item, "sizes", MaxSizesLength, out string sizes))
                {
                    kept.Add(new BrowserArtwork(url, sizes));
                }
            }

            artwork = kept;
            return true;
        }
    }
}
