using System.Text;
using FluentAssertions;
using MosaicShell.Core.Services;
using MosaicShell.Core.Services.BrowserBridge;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// The messages the browser extension sends to the Host come from a process the Host does not control, so the
    /// parser is strict about structure and identifiers, tolerant about unknown fields, and caps every string.
    /// </summary>
    public class BrowserProtocolParserTests
    {
        private const string MinimalSession =
            """{"type":"session","tabId":7,"windowId":3,"origin":"https://music.youtube.com","playbackState":"playing"}""";

        private static BrowserParseResult Parse(string json)
        {
            return BrowserProtocol.Parse(Encoding.UTF8.GetBytes(json));
        }

        private static BrowserSessionReport Session(string json)
        {
            BrowserParseResult result = Parse(json);
            _ = result.Error.Should().Be(BrowserParseError.None, result.Detail);
            return result.Message.Should().BeOfType<BrowserSessionReport>().Subject;
        }

        private static string SessionWith(string field)
        {
            return MinimalSession[..^1] + "," + field + "}";
        }

        [Fact]
        public void A_hello_carries_the_protocol_extension_version_and_browser()
        {
            BrowserParseResult result = Parse("""{"type":"hello","protocol":1,"extensionVersion":"0.1.0","browser":"edge"}""");

            _ = result.Message.Should().Be(new BrowserHello(1, "0.1.0", "edge"));
        }

        [Fact]
        public void A_hello_for_another_protocol_version_is_refused()
        {
            BrowserParseResult result = Parse("""{"type":"hello","protocol":2,"extensionVersion":"9.0.0","browser":"edge"}""");

            _ = result.Message.Should().BeNull();
            _ = result.Error.Should().Be(BrowserParseError.UnsupportedProtocol);
        }

        [Fact]
        public void A_hello_without_a_protocol_is_invalid()
        {
            BrowserParseResult result = Parse("""{"type":"hello","extensionVersion":"0.1.0","browser":"edge"}""");

            _ = result.Error.Should().Be(BrowserParseError.InvalidField);
            _ = result.Detail.Should().Be("protocol");
        }

        [Fact]
        public void A_full_session_report_is_read_field_by_field()
        {
            BrowserSessionReport s = Session("""
                {"type":"session","tabId":7,"windowId":3,"origin":"https://music.youtube.com",
                 "title":"Humid","artist":"Moody Good","album":"Album",
                 "artwork":[{"src":"https://i.ytimg.com/vi/x/sddefault.jpg","sizes":"320x180"}],
                 "playbackState":"playing","audible":true,"rating":"liked",
                 "capabilities":["rating","dislike"]}
                """);

            _ = s.TabId.Should().Be(7);
            _ = s.WindowId.Should().Be(3);
            _ = s.Origin.Should().Be("https://music.youtube.com");
            _ = s.Title.Should().Be("Humid");
            _ = s.Artist.Should().Be("Moody Good");
            _ = s.Album.Should().Be("Album");
            _ = s.Artwork.Should().ContainSingle().Which.Should().Be(new BrowserArtwork("https://i.ytimg.com/vi/x/sddefault.jpg", "320x180"));
            _ = s.PlaybackState.Should().Be(BrowserPlaybackState.Playing);
            _ = s.Audible.Should().BeTrue();
            _ = s.Rating.Should().Be(BrowserRating.Liked);
            _ = s.Capabilities.Should().Be(BrowserMediaCapabilities.Rating | BrowserMediaCapabilities.Dislike);
        }

        [Fact]
        public void A_session_needs_only_the_identifying_fields_and_defaults_the_rest()
        {
            BrowserSessionReport s = Session(MinimalSession);

            _ = s.Title.Should().BeEmpty();
            _ = s.Artist.Should().BeEmpty();
            _ = s.Album.Should().BeEmpty();
            _ = s.Artwork.Should().BeEmpty();
            _ = s.Audible.Should().BeFalse();
            _ = s.Rating.Should().Be(BrowserRating.None);
            _ = s.Capabilities.Should().Be(BrowserMediaCapabilities.None);
        }

        [Theory]
        [InlineData("tabId")]
        [InlineData("windowId")]
        [InlineData("origin")]
        [InlineData("playbackState")]
        public void A_session_missing_an_identifying_field_is_invalid(string field)
        {
            string json = System.Text.RegularExpressions.Regex.Replace(MinimalSession, $"\"{field}\":(\"[^\"]*\"|[0-9]+),?", "")
                .Replace(",}", "}", StringComparison.Ordinal);

            BrowserParseResult result = Parse(json);

            _ = result.Error.Should().Be(BrowserParseError.InvalidField);
            _ = result.Detail.Should().Be(field);
        }

        [Theory]
        [InlineData("playing", BrowserPlaybackState.Playing)]
        [InlineData("paused", BrowserPlaybackState.Paused)]
        [InlineData("none", BrowserPlaybackState.Stopped)]
        public void The_Media_Session_playback_states_are_mapped(string wire, BrowserPlaybackState expected)
        {
            _ = Session(MinimalSession.Replace("playing", wire, StringComparison.Ordinal)).PlaybackState.Should().Be(expected);
        }

        [Fact]
        public void An_unknown_playback_state_is_invalid()
        {
            BrowserParseResult result = Parse(MinimalSession.Replace("playing", "buffering", StringComparison.Ordinal));

            _ = result.Error.Should().Be(BrowserParseError.InvalidField);
            _ = result.Detail.Should().Be("playbackState");
        }

        [Theory]
        [InlineData("none", BrowserRating.None)]
        [InlineData("liked", BrowserRating.Liked)]
        [InlineData("disliked", BrowserRating.Disliked)]
        public void The_rating_is_mapped(string wire, BrowserRating expected)
        {
            _ = Session(SessionWith($"\"rating\":\"{wire}\"")).Rating.Should().Be(expected);
        }

        [Fact]
        public void An_unknown_rating_is_invalid()
        {
            BrowserParseResult result = Parse(SessionWith("\"rating\":\"love\""));

            _ = result.Error.Should().Be(BrowserParseError.InvalidField);
            _ = result.Detail.Should().Be("rating");
        }

        [Fact]
        public void Every_capability_name_maps_to_its_flag()
        {
            BrowserSessionReport s = Session(SessionWith("\"capabilities\":[\"rating\",\"dislike\",\"shuffle\",\"repeat\"]"));

            _ = s.Capabilities.Should().Be(
                BrowserMediaCapabilities.Rating | BrowserMediaCapabilities.Dislike
                | BrowserMediaCapabilities.Shuffle | BrowserMediaCapabilities.Repeat);
        }

        [Fact]
        public void An_unknown_capability_name_is_ignored_so_a_newer_extension_still_works()
        {
            BrowserSessionReport s = Session(SessionWith("\"capabilities\":[\"rating\",\"teleport\"]"));

            _ = s.Capabilities.Should().Be(BrowserMediaCapabilities.Rating);
        }

        [Theory]
        [InlineData("\"capabilities\":\"rating\"")]
        [InlineData("\"capabilities\":[1]")]
        public void Capabilities_of_the_wrong_shape_are_invalid(string field)
        {
            BrowserParseResult result = Parse(SessionWith(field));

            _ = result.Error.Should().Be(BrowserParseError.InvalidField);
            _ = result.Detail.Should().Be("capabilities");
        }

        [Theory]
        [InlineData("https://music.youtube.com", "https://music.youtube.com")]
        [InlineData("https://music.youtube.com/", "https://music.youtube.com")]
        [InlineData("https://MUSIC.YouTube.com", "https://music.youtube.com")]
        [InlineData("https://open.spotify.com:8443", "https://open.spotify.com:8443")]
        public void An_https_origin_is_normalized(string wire, string expected)
        {
            _ = Session(MinimalSession.Replace("https://music.youtube.com", wire, StringComparison.Ordinal)).Origin.Should().Be(expected);
        }

        [Theory]
        [InlineData("http://music.youtube.com")]
        [InlineData("https://music.youtube.com/watch?v=1")]
        [InlineData("https://user@music.youtube.com")]
        [InlineData("javascript:alert(1)")]
        [InlineData("file:///c:/x")]
        [InlineData("music.youtube.com")]
        [InlineData("")]
        public void An_origin_that_is_not_a_plain_https_origin_is_invalid(string wire)
        {
            BrowserParseResult result = Parse(MinimalSession.Replace("https://music.youtube.com", wire, StringComparison.Ordinal));

            _ = result.Error.Should().Be(BrowserParseError.InvalidField);
            _ = result.Detail.Should().Be("origin");
        }

        [Fact]
        public void An_origin_over_the_cap_is_invalid()
        {
            string host = new('a', BrowserProtocol.MaxOriginLength);

            BrowserParseResult result = Parse(MinimalSession.Replace("https://music.youtube.com", $"https://{host}.com", StringComparison.Ordinal));

            _ = result.Error.Should().Be(BrowserParseError.InvalidField);
        }

        [Theory]
        [InlineData("\"tabId\":7", "\"tabId\":-1")]
        [InlineData("\"tabId\":7", "\"tabId\":1.5")]
        [InlineData("\"tabId\":7", "\"tabId\":\"7\"")]
        [InlineData("\"tabId\":7", "\"tabId\":2147483648")]
        [InlineData("\"windowId\":3", "\"windowId\":-2")]
        public void A_tab_or_window_id_must_be_a_non_negative_integer(string original, string replacement)
        {
            BrowserParseResult result = Parse(MinimalSession.Replace(original, replacement, StringComparison.Ordinal));

            _ = result.Error.Should().Be(BrowserParseError.InvalidField);
        }

        [Fact]
        public void Tab_and_window_id_zero_and_the_largest_int_are_valid()
        {
            BrowserSessionReport s = Session(MinimalSession
                .Replace("\"tabId\":7", "\"tabId\":0", StringComparison.Ordinal)
                .Replace("\"windowId\":3", "\"windowId\":2147483647", StringComparison.Ordinal));

            _ = s.TabId.Should().Be(0);
            _ = s.WindowId.Should().Be(int.MaxValue);
        }

        [Fact]
        public void A_string_at_the_cap_is_kept_whole_and_one_over_is_truncated()
        {
            string atCap = new('t', BrowserProtocol.MaxTextLength);

            _ = Session(SessionWith($"\"title\":\"{atCap}\"")).Title.Should().Be(atCap);
            _ = Session(SessionWith($"\"title\":\"{atCap}x\"")).Title.Should().Be(atCap);
        }

        [Fact]
        public void Truncation_never_leaves_half_a_surrogate_pair()
        {
            string title = new string('a', BrowserProtocol.MaxTextLength - 1) + "\U0001F3B5";

            string result = Session(SessionWith($"\"title\":\"{title}\"")).Title;

            _ = result.Should().Be(new string('a', BrowserProtocol.MaxTextLength - 1));
        }

        [Fact]
        public void Control_characters_in_text_become_spaces_so_a_title_cannot_forge_a_log_line()
        {
            string result = Session(SessionWith("\"title\":\"one\\ntwo\\r\\tthree\"")).Title;

            _ = result.Should().Be("one two  three");
        }

        [Fact]
        public void A_text_field_of_the_wrong_type_is_invalid()
        {
            BrowserParseResult result = Parse(SessionWith("\"artist\":5"));

            _ = result.Error.Should().Be(BrowserParseError.InvalidField);
            _ = result.Detail.Should().Be("artist");
        }

        [Fact]
        public void Artwork_keeps_https_and_blob_urls_and_drops_every_other_scheme()
        {
            BrowserSessionReport s = Session(SessionWith("""
                "artwork":[{"src":"http://a.example/1.jpg"},{"src":"https://a.example/2.jpg","sizes":"96x96"},
                           {"src":"blob:https://music.youtube.com/abc"},{"src":"data:image/png;base64,AAAA"},
                           {"src":"javascript:alert(1)"},{"src":"file:///c:/x.png"},{"src":"not a url"},{"nosrc":1}]
                """));

            _ = s.Artwork.Select(a => a.Src).Should().Equal("https://a.example/2.jpg", "blob:https://music.youtube.com/abc");
        }

        [Fact]
        public void Artwork_is_capped_in_count_and_url_length()
        {
            string entries = string.Join(",", Enumerable.Range(0, BrowserProtocol.MaxArtwork + 4).Select(i => $"{{\"src\":\"https://a.example/{i}.jpg\"}}"));
            string tooLong = "https://a.example/" + new string('x', BrowserProtocol.MaxUrlLength);

            _ = Session(SessionWith($"\"artwork\":[{entries}]")).Artwork.Should().HaveCount(BrowserProtocol.MaxArtwork);
            _ = Session(SessionWith($"\"artwork\":[{{\"src\":\"{tooLong}\"}}]")).Artwork.Should().BeEmpty();
        }

        [Fact]
        public void Artwork_that_is_not_a_list_is_invalid()
        {
            BrowserParseResult result = Parse(SessionWith("\"artwork\":\"https://a.example/1.jpg\""));

            _ = result.Error.Should().Be(BrowserParseError.InvalidField);
            _ = result.Detail.Should().Be("artwork");
        }

        [Fact]
        public void A_removed_message_names_the_tab()
        {
            BrowserParseResult result = Parse("""{"type":"removed","tabId":7}""");

            _ = result.Message.Should().Be(new BrowserSessionRemoved(7));
        }

        [Fact]
        public void A_removed_message_without_a_tab_is_invalid()
        {
            BrowserParseResult result = Parse("""{"type":"removed"}""");

            _ = result.Error.Should().Be(BrowserParseError.InvalidField);
            _ = result.Detail.Should().Be("tabId");
        }

        [Fact]
        public void A_ping_is_recognised()
        {
            _ = Parse("""{"type":"ping"}""").Message.Should().Be(BrowserPing.Instance);
        }

        [Fact]
        public void Unknown_fields_at_any_depth_are_ignored()
        {
            BrowserSessionReport s = Session(SessionWith("""
                "future":{"a":[1,2,{"b":null}]},"artwork":[{"src":"https://a.example/1.jpg","extra":true}]
                """));

            _ = s.Artwork.Should().ContainSingle();
        }

        [Fact]
        public void An_unknown_message_type_is_reported_so_the_caller_can_ignore_it()
        {
            BrowserParseResult result = Parse("""{"type":"telemetry","x":1}""");

            _ = result.Message.Should().BeNull();
            _ = result.Error.Should().Be(BrowserParseError.UnknownType);
        }

        [Theory]
        [InlineData("""{"tabId":7}""")]
        [InlineData("""{"type":5}""")]
        [InlineData("""{"type":null}""")]
        public void A_message_without_a_string_type_is_invalid(string json)
        {
            _ = Parse(json).Error.Should().Be(BrowserParseError.MissingType);
        }

        [Theory]
        [InlineData("")]
        [InlineData("not json")]
        [InlineData("""{"type":"ping""")]
        public void Input_that_is_not_json_is_rejected(string json)
        {
            _ = Parse(json).Error.Should().Be(BrowserParseError.InvalidJson);
        }

        [Theory]
        [InlineData("[]")]
        [InlineData("5")]
        [InlineData("\"ping\"")]
        [InlineData("null")]
        public void Json_that_is_not_an_object_is_rejected(string json)
        {
            _ = Parse(json).Error.Should().Be(BrowserParseError.NotAnObject);
        }

        [Fact]
        public void Deeply_nested_input_is_rejected_rather_than_walked()
        {
            string json = "{\"type\":\"ping\",\"x\":" + new string('[', 200) + new string(']', 200) + "}";

            _ = Parse(json).Error.Should().Be(BrowserParseError.InvalidJson);
        }

        [Fact]
        public void A_message_over_the_size_cap_is_rejected_without_being_parsed()
        {
            byte[] atCap = new byte[BrowserProtocol.MaxMessageBytes];
            byte[] overCap = new byte[BrowserProtocol.MaxMessageBytes + 1];

            _ = BrowserProtocol.Parse(atCap).Error.Should().Be(BrowserParseError.InvalidJson, "at the cap it is parsed, and zero bytes are not JSON");
            _ = BrowserProtocol.Parse(overCap).Error.Should().Be(BrowserParseError.TooLarge);
        }

        [Theory]
        [InlineData(BrowserCommandAction.Like, "like")]
        [InlineData(BrowserCommandAction.Dislike, "dislike")]
        [InlineData(BrowserCommandAction.Clear, "clear")]
        public void A_command_to_the_extension_names_the_tab_and_the_wanted_state(BrowserCommandAction action, string wire)
        {
            byte[] bytes = BrowserProtocol.SerializeCommand(7, action);

            _ = Encoding.UTF8.GetString(bytes).Should().Be($"{{\"type\":\"command\",\"tabId\":7,\"action\":\"{wire}\"}}");
        }
    }
}
