using System.Text;
using FluentAssertions;
using MosaicShell.Core.Services.BrowserBridge;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// The hub holds every tab the connected browsers publish. It never trusts the extension: a connection that
    /// breaks the protocol, floods it, or claims too many tabs is dropped, and one that goes quiet ages out.
    /// </summary>
    public class BrowserSessionHubTests
    {
        private const string Hello = """{"type":"hello","protocol":1,"extensionVersion":"0.1.0","browser":"edge"}""";

        private readonly ManualTimeProvider _clock = new();

        private static string Session(int tab, string title, string state = "playing", string extra = "")
        {
            return $$"""{"type":"session","tabId":{{tab}},"windowId":1,"origin":"https://music.youtube.com","title":"{{title}}","playbackState":"{{state}}","audible":true{{extra}}}""";
        }

        private static void Send(BrowserSessionHub hub, int connection, string json)
        {
            hub.Receive(connection, Encoding.UTF8.GetBytes(json));
        }

        private (BrowserSessionHub Hub, FakeConnection Connection, int Id) Connected(int maxConnections = 8)
        {
            BrowserSessionHub hub = new(_clock, maxConnections);
            FakeConnection connection = new();
            int id = hub.Open(connection);
            Send(hub, id, Hello);
            return (hub, connection, id);
        }

        [Fact]
        public void A_hello_is_answered_with_a_resync_so_a_restarted_Host_learns_the_open_tabs()
        {
            (_, FakeConnection connection, _) = Connected();

            _ = connection.Sent.Should().Equal("""{"type":"resync"}""");
        }

        [Fact]
        public void A_session_report_becomes_the_selected_session()
        {
            (BrowserSessionHub hub, _, int id) = Connected();

            Send(hub, id, Session(7, "Humid"));

            BrowserSessionEntry? active = hub.Select(null);
            _ = active.Should().NotBeNull();
            _ = active!.Report.Title.Should().Be("Humid");
            _ = active.Report.TabId.Should().Be(7);
            _ = active.ConnectionId.Should().Be(id);
        }

        [Fact]
        public void The_Host_stamps_reports_with_its_own_clock()
        {
            (BrowserSessionHub hub, _, int id) = Connected();
            _clock.Advance(TimeSpan.FromMinutes(5));

            Send(hub, id, Session(7, "Humid"));

            _ = hub.Select(null)!.UpdatedAt.Should().Be(_clock.GetUtcNow());
        }

        [Fact]
        public void A_newer_report_for_the_same_tab_replaces_the_older_one()
        {
            (BrowserSessionHub hub, _, int id) = Connected();
            Send(hub, id, Session(7, "First"));
            Send(hub, id, Session(7, "Second"));

            _ = hub.Sessions.Should().ContainSingle().Which.Report.Title.Should().Be("Second");
        }

        [Fact]
        public void The_same_tab_id_on_two_connections_is_two_sessions()
        {
            BrowserSessionHub hub = new(_clock);
            int a = hub.Open(new FakeConnection());
            int b = hub.Open(new FakeConnection());
            Send(hub, a, Hello);
            Send(hub, b, Hello);
            Send(hub, a, Session(1, "In Edge"));
            Send(hub, b, Session(1, "In Chrome"));

            _ = hub.Sessions.Select(s => s.Report.Title).Should().BeEquivalentTo("In Edge", "In Chrome");
        }

        [Fact]
        public void A_change_raises_Changed_and_an_identical_repeat_does_not()
        {
            (BrowserSessionHub hub, _, int id) = Connected();
            int changes = 0;
            hub.Changed += (_, _) => changes++;

            Send(hub, id, Session(7, "Humid"));
            Send(hub, id, Session(7, "Humid"));
            Send(hub, id, Session(7, "Humid", extra: ",\"artist\":\"Moody Good\""));

            _ = changes.Should().Be(2);
        }

        [Fact]
        public void An_identical_repeat_does_not_make_the_session_look_newer()
        {
            (BrowserSessionHub hub, _, int id) = Connected();
            Send(hub, id, Session(7, "Humid"));
            DateTimeOffset first = hub.Select(null)!.UpdatedAt;
            _clock.Advance(TimeSpan.FromSeconds(30));

            Send(hub, id, Session(7, "Humid"));

            _ = hub.Select(null)!.UpdatedAt.Should().Be(first);
        }

        [Fact]
        public void A_removed_message_drops_the_tab_and_raises_Changed()
        {
            (BrowserSessionHub hub, _, int id) = Connected();
            Send(hub, id, Session(7, "Humid"));
            int changes = 0;
            hub.Changed += (_, _) => changes++;

            Send(hub, id, """{"type":"removed","tabId":7}""");

            _ = hub.Select(null).Should().BeNull();
            _ = changes.Should().Be(1);
        }

        [Fact]
        public void Removing_a_tab_that_was_never_reported_changes_nothing()
        {
            (BrowserSessionHub hub, _, int id) = Connected();
            int changes = 0;
            hub.Changed += (_, _) => changes++;

            Send(hub, id, """{"type":"removed","tabId":99}""");

            _ = changes.Should().Be(0);
        }

        [Fact]
        public void Closing_a_connection_drops_its_tabs_and_raises_Changed_once()
        {
            (BrowserSessionHub hub, _, int id) = Connected();
            Send(hub, id, Session(1, "A"));
            Send(hub, id, Session(2, "B"));
            int changes = 0;
            hub.Changed += (_, _) => changes++;

            hub.Close(id);

            _ = hub.Sessions.Should().BeEmpty();
            _ = hub.ConnectionCount.Should().Be(0);
            _ = changes.Should().Be(1);
        }

        [Fact]
        public void Closing_a_connection_that_published_nothing_raises_nothing()
        {
            (BrowserSessionHub hub, _, int id) = Connected();
            int changes = 0;
            hub.Changed += (_, _) => changes++;

            hub.Close(id);
            hub.Close(id);
            hub.Close(12345);

            _ = changes.Should().Be(0);
        }

        [Fact]
        public void Connections_beyond_the_cap_are_refused()
        {
            BrowserSessionHub hub = new(_clock, maxConnections: 2);

            int first = hub.Open(new FakeConnection());
            int second = hub.Open(new FakeConnection());
            int third = hub.Open(new FakeConnection());

            _ = first.Should().BeGreaterThanOrEqualTo(0);
            _ = second.Should().BeGreaterThanOrEqualTo(0).And.NotBe(first);
            _ = third.Should().Be(-1);
            _ = hub.ConnectionCount.Should().Be(2);
        }

        [Fact]
        public void A_closed_connection_frees_its_place_under_the_cap()
        {
            BrowserSessionHub hub = new(_clock, maxConnections: 1);
            int first = hub.Open(new FakeConnection());

            hub.Close(first);

            _ = hub.Open(new FakeConnection()).Should().BeGreaterThanOrEqualTo(0);
        }

        [Fact]
        public void A_session_before_the_hello_is_refused_and_the_connection_dropped()
        {
            BrowserSessionHub hub = new(_clock);
            FakeConnection connection = new();
            int id = hub.Open(connection);

            Send(hub, id, Session(1, "Too early"));

            _ = hub.Sessions.Should().BeEmpty();
            _ = connection.Closed.Should().BeTrue();
            _ = hub.ConnectionCount.Should().Be(0);
        }

        [Fact]
        public void A_hello_for_another_protocol_drops_the_connection()
        {
            BrowserSessionHub hub = new(_clock);
            FakeConnection connection = new();
            int id = hub.Open(connection);

            Send(hub, id, """{"type":"hello","protocol":2,"extensionVersion":"9","browser":"edge"}""");

            _ = connection.Closed.Should().BeTrue();
        }

        [Fact]
        public void A_second_hello_on_the_same_connection_is_a_violation_not_a_new_identity()
        {
            (BrowserSessionHub hub, FakeConnection connection, int id) = Connected();

            for (int i = 0; i < BrowserSessionHub.MaxViolations; i++)
            {
                Send(hub, id, Hello);
            }

            _ = connection.Closed.Should().BeTrue();
        }

        [Fact]
        public void An_oversized_message_drops_the_connection_at_once()
        {
            (BrowserSessionHub hub, FakeConnection connection, int id) = Connected();

            hub.Receive(id, new byte[BrowserProtocol.MaxMessageBytes + 1]);

            _ = connection.Closed.Should().BeTrue();
        }

        [Fact]
        public void Malformed_messages_are_tolerated_until_the_violation_limit()
        {
            (BrowserSessionHub hub, FakeConnection connection, int id) = Connected();

            for (int i = 0; i < BrowserSessionHub.MaxViolations - 1; i++)
            {
                Send(hub, id, "not json");
            }

            _ = connection.Closed.Should().BeFalse();

            Send(hub, id, "not json");

            _ = connection.Closed.Should().BeTrue();
        }

        [Fact]
        public void An_unknown_message_type_is_ignored_and_is_not_a_violation()
        {
            (BrowserSessionHub hub, FakeConnection connection, int id) = Connected();

            for (int i = 0; i < BrowserSessionHub.MaxViolations * 2; i++)
            {
                Send(hub, id, """{"type":"from-the-future"}""");
            }

            _ = connection.Closed.Should().BeFalse();
        }

        [Fact]
        public void A_connection_may_not_claim_more_than_the_per_connection_tab_limit()
        {
            (BrowserSessionHub hub, _, int id) = Connected();

            for (int tab = 0; tab < BrowserSessionHub.MaxSessionsPerConnection; tab++)
            {
                Send(hub, id, Session(tab, $"t{tab}"));
                _clock.Advance(TimeSpan.FromSeconds(1));
            }

            Send(hub, id, Session(1000, "one too many"));

            _ = hub.Sessions.Should().HaveCount(BrowserSessionHub.MaxSessionsPerConnection);
            _ = hub.Sessions.Should().NotContain(s => s.Report.Title == "one too many");
        }

        [Fact]
        public void A_tab_already_held_can_still_be_updated_at_the_limit()
        {
            (BrowserSessionHub hub, _, int id) = Connected();
            for (int tab = 0; tab < BrowserSessionHub.MaxSessionsPerConnection; tab++)
            {
                Send(hub, id, Session(tab, $"t{tab}"));
                _clock.Advance(TimeSpan.FromSeconds(1));
            }

            Send(hub, id, Session(3, "renamed"));

            _ = hub.Sessions.Should().Contain(s => s.Report.TabId == 3 && s.Report.Title == "renamed");
        }

        [Fact]
        public void A_flood_is_dropped_beyond_the_burst_and_recovers_as_time_passes()
        {
            (BrowserSessionHub hub, FakeConnection connection, int id) = Connected();

            for (int i = 0; i < BrowserSessionHub.BurstMessages; i++)
            {
                Send(hub, id, """{"type":"ping"}""");
            }

            Send(hub, id, Session(1, "over the burst"));

            _ = hub.Sessions.Should().BeEmpty("the message after the burst was dropped");
            _ = connection.Closed.Should().BeFalse("one dropped message is not yet grounds to disconnect");

            _clock.Advance(TimeSpan.FromSeconds(1));
            Send(hub, id, Session(1, "after a pause"));

            _ = hub.Sessions.Should().ContainSingle().Which.Report.Title.Should().Be("after a pause");
        }

        [Fact]
        public void A_sustained_flood_drops_the_connection()
        {
            (BrowserSessionHub hub, FakeConnection connection, int id) = Connected();

            for (int i = 0; i < BrowserSessionHub.BurstMessages + BrowserSessionHub.MaxViolations + 5; i++)
            {
                Send(hub, id, """{"type":"ping"}""");
            }

            _ = connection.Closed.Should().BeTrue();
        }

        [Fact]
        public void A_ping_keeps_a_quiet_connection_alive_past_the_staleness_timeout()
        {
            (BrowserSessionHub hub, _, int id) = Connected();
            Send(hub, id, Session(1, "Long song"));
            TimeSpan step = BrowserSessionSelector.StalenessTimeout * 0.8;

            _clock.Advance(step);
            Send(hub, id, """{"type":"ping"}""");
            _clock.Advance(step);

            _ = hub.Select(null).Should().NotBeNull();
        }

        [Fact]
        public void A_connection_that_goes_silent_ages_out()
        {
            (BrowserSessionHub hub, _, int id) = Connected();
            Send(hub, id, Session(1, "Ghost"));

            _clock.Advance(BrowserSessionSelector.StalenessTimeout + TimeSpan.FromSeconds(1));

            _ = hub.Select(null).Should().BeNull();
        }

        [Fact]
        public void Selection_uses_the_title_Windows_reports()
        {
            (BrowserSessionHub hub, _, int id) = Connected();
            Send(hub, id, Session(1, "Old song"));
            _clock.Advance(TimeSpan.FromSeconds(10));
            Send(hub, id, Session(2, "New song"));

            _ = hub.Select("Old song | YouTube Music")!.Report.Title.Should().Be("Old song");
            _ = hub.Select(null)!.Report.Title.Should().Be("New song");
        }

        [Fact]
        public void A_command_goes_to_the_connection_that_owns_the_tab()
        {
            BrowserSessionHub hub = new(_clock);
            FakeConnection edge = new();
            FakeConnection chrome = new();
            int a = hub.Open(edge);
            int b = hub.Open(chrome);
            Send(hub, a, Hello);
            Send(hub, b, Hello);
            Send(hub, a, Session(4, "In Edge"));
            Send(hub, b, Session(4, "In Chrome"));
            edge.Sent.Clear();
            chrome.Sent.Clear();

            bool sent = hub.SendCommand(hub.Sessions.Single(s => s.ConnectionId == b), BrowserCommandAction.Dislike);

            _ = sent.Should().BeTrue();
            _ = chrome.Sent.Should().Equal("""{"type":"command","tabId":4,"action":"dislike"}""");
            _ = edge.Sent.Should().BeEmpty();
        }

        [Fact]
        public void A_command_for_a_connection_that_has_gone_is_not_sent()
        {
            (BrowserSessionHub hub, _, int id) = Connected();
            Send(hub, id, Session(4, "Song"));
            BrowserSessionEntry entry = hub.Select(null)!;
            hub.Close(id);

            _ = hub.SendCommand(entry, BrowserCommandAction.Like).Should().BeFalse();
        }

        [Fact]
        public void A_connection_that_throws_while_being_sent_to_is_dropped_not_propagated()
        {
            BrowserSessionHub hub = new(_clock);
            FakeConnection connection = new();
            int id = hub.Open(connection);
            Send(hub, id, Hello);
            Send(hub, id, Session(4, "Song"));
            connection.ThrowOnSend = true;

            bool sent = hub.SendCommand(hub.Select(null)!, BrowserCommandAction.Like);

            _ = sent.Should().BeFalse();
            _ = hub.ConnectionCount.Should().Be(0);
        }

        private sealed class FakeConnection : IBrowserConnection
        {
            public List<string> Sent { get; } = [];
            public bool Closed { get; private set; }
            public bool ThrowOnSend { get; set; }

            public void Send(byte[] payload)
            {
                if (ThrowOnSend)
                {
                    throw new IOException("pipe broken");
                }

                Sent.Add(Encoding.UTF8.GetString(payload));
            }

            public void Close()
            {
                Closed = true;
            }
        }

        private sealed class ManualTimeProvider : TimeProvider
        {
            private DateTimeOffset _now = new(2026, 9, 19, 12, 0, 0, TimeSpan.Zero);

            public override DateTimeOffset GetUtcNow()
            {
                return _now;
            }

            public void Advance(TimeSpan by)
            {
                _now += by;
            }
        }
    }
}
