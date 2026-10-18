using FluentAssertions;
using MosaicShell.Core.Capabilities;
using MosaicShell.Core.Capabilities.Ipc;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// Transport-level coverage for the control plane: <see cref="RemoteCapabilityHost"/> (Host side) against
    /// <see cref="CapabilityIpcControlServer"/> (Worker side) over a real named pipe. The flyout pair has had this since
    /// the E2 dedupe was planned; the control pair had only codec framing tests.
    /// </summary>
    public class CapabilityIpcControlRoundTripTests
    {
        private static string NewPipeName()
        {
            return "MosaicShell.Test.Control." + Guid.NewGuid().ToString("N");
        }

        private static RemoteCapabilityHost Connect(string pipeName)
        {
            RemoteCapabilityHost client = new();
            client.TryConnect(pipeName: pipeName).Should().BeTrue("the server is listening on the test pipe");
            return client;
        }

        [Fact]
        public void A_client_with_no_server_does_not_connect()
        {
            using RemoteCapabilityHost client = new();

            client.TryConnect(pipeName: NewPipeName()).Should().BeFalse();
            client.IsConnected.Should().BeFalse();
        }

        [Fact]
        public void Requests_without_a_connection_throw()
        {
            using RemoteCapabilityHost client = new();

            Action act = () => client.IsArmed("Tessera");

            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void A_disposed_client_refuses_to_connect_or_send()
        {
            RemoteCapabilityHost client = new();
            client.Dispose();

            client.Invoking(c => c.TryConnect()).Should().Throw<ObjectDisposedException>();
            client.Invoking(c => c.IsArmed("Tessera")).Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public async Task Arm_disarm_and_rearm_reach_the_daemon_with_their_arguments_and_return_its_answer()
        {
            string pipe = NewPipeName();
            FakeCapabilityHost daemon = new();
            using CapabilityIpcControlServer server = new(daemon, pipe);
            server.Start();
            using RemoteCapabilityHost client = Connect(pipe);

            (await client.ArmAsync("Tessera", persist: false)).Should().BeTrue();
            (await client.ArmAsync("Refused")).Should().BeFalse("the daemon's answer is what comes back");
            (await client.DisarmAsync("Tessera", persist: true)).Should().BeTrue();
            (await client.ReArmAsync("Tessera")).Should().BeTrue();

            daemon.Calls.Should().Equal(
                "Arm Tessera persist=False",
                "Arm Refused persist=True",
                "Disarm Tessera persist=True",
                "ReArm Tessera");
        }

        [Fact]
        public void Queries_return_what_the_daemon_reports()
        {
            string pipe = NewPipeName();
            FakeCapabilityHost daemon = new() { Armed = ["Tessera", "Inlay"], HotkeyError = "Ctrl+Alt+M is taken" };
            using CapabilityIpcControlServer server = new(daemon, pipe);
            server.Start();
            using RemoteCapabilityHost client = Connect(pipe);

            client.IsArmed("Tessera").Should().BeTrue();
            client.IsArmed("Chord").Should().BeFalse();
            client.ArmedModuleIds.Should().BeEquivalentTo("Tessera", "Inlay");
            client.GetHotkeyError("Inlay").Should().Be("Ctrl+Alt+M is taken");

            daemon.HotkeyError = null;
            client.GetHotkeyError("Inlay").Should().BeNull();
        }

        [Fact]
        public async Task Restore_disarm_all_and_persist_run_on_the_daemon()
        {
            string pipe = NewPipeName();
            FakeCapabilityHost daemon = new();
            using CapabilityIpcControlServer server = new(daemon, pipe);
            server.Start();
            using RemoteCapabilityHost client = Connect(pipe);

            await client.RestoreAsync();
            await client.DisarmAllAsync();
            client.Persist();

            daemon.Calls.Should().Equal("Restore", "DisarmAll", "Persist");
        }

        [Fact]
        public async Task A_daemon_that_throws_answers_false_instead_of_hanging_the_client()
        {
            string pipe = NewPipeName();
            FakeCapabilityHost daemon = new() { ThrowOnArm = true };
            using CapabilityIpcControlServer server = new(daemon, pipe);
            server.Start();
            using RemoteCapabilityHost client = Connect(pipe);

            (await client.ArmAsync("Tessera")).Should().BeFalse("the error reply carries a message, not a true");
            client.IsArmed("Tessera").Should().BeFalse("the connection survives the failed request");
        }

        [Fact]
        public void Responses_are_matched_to_requests_by_id_when_calls_overlap()
        {
            string pipe = NewPipeName();
            FakeCapabilityHost daemon = new() { Armed = ["Tessera"] };
            using CapabilityIpcControlServer server = new(daemon, pipe);
            server.Start();
            using RemoteCapabilityHost client = Connect(pipe);

            bool[] results = new bool[40];
            _ = Parallel.For(0, results.Length, i => results[i] = client.IsArmed(i % 2 == 0 ? "Tessera" : "Other"));

            results.Select((value, i) => value == (i % 2 == 0)).Should().OnlyContain(ok => ok);
        }

        [Fact]
        public void A_restarted_host_can_reconnect_after_the_previous_one_went_away()
        {
            string pipe = NewPipeName();
            FakeCapabilityHost daemon = new() { Armed = ["Tessera"] };
            using CapabilityIpcControlServer server = new(daemon, pipe);
            server.Start();
            RemoteCapabilityHost first = Connect(pipe);
            first.IsArmed("Tessera").Should().BeTrue();
            first.Dispose();

            // The server notices the peer is gone on its own thread; give the next instance a moment to open.
            RemoteCapabilityHost? second = null;
            bool reconnected = SpinWait.SpinUntil(
                () => (second = new RemoteCapabilityHost()).TryConnect(pipeName: pipe), TimeSpan.FromSeconds(10));

            reconnected.Should().BeTrue("a Worker that outlives its Host must accept the next Host");
            using (second)
            {
                second!.IsArmed("Tessera").Should().BeTrue();
            }
        }

        [Fact]
        public async Task An_idle_connected_client_does_not_make_the_server_spin()
        {
            string pipe = NewPipeName();
            using CapabilityIpcControlServer server = new(new FakeCapabilityHost(), pipe);
            server.Start();
            using RemoteCapabilityHost client = Connect(pipe);
            await Task.Delay(200);

            double share = await IdleCpu.LowestShareAsync();

            share.Should().BeLessThan(0.5, "the accept loop must wait for the connection to end, not retry in a tight loop");
        }

        [Fact]
        public void Disposing_the_server_ends_the_clients_connection()
        {
            string pipe = NewPipeName();
            FakeCapabilityHost daemon = new();
            CapabilityIpcControlServer server = new(daemon, pipe);
            server.Start();
            using RemoteCapabilityHost client = Connect(pipe);

            server.Dispose();

            SpinWait.SpinUntil(() => !client.IsConnected, TimeSpan.FromSeconds(15)).Should().BeTrue();
            client.Invoking(c => c.IsArmed("Tessera")).Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void The_server_cannot_be_started_after_it_is_disposed_and_start_is_idempotent()
        {
            CapabilityIpcControlServer server = new(new FakeCapabilityHost(), NewPipeName());
            server.Start();
            server.Invoking(s => s.Start()).Should().NotThrow();

            server.Dispose();
            server.Dispose();

            server.Invoking(s => s.Start()).Should().Throw<ObjectDisposedException>();
        }

        private sealed class FakeCapabilityHost : ICapabilityHost
        {
            private readonly List<string> _calls = [];

            public IReadOnlyList<string> Calls
            {
                get { lock (_calls) { return [.. _calls]; } }
            }

            public IReadOnlyList<string> Armed { get; set; } = [];
            public string? HotkeyError { get; set; }
            public bool ThrowOnArm { get; set; }

            public IReadOnlyList<string> ArmedModuleIds => Armed;

            public bool IsArmed(string moduleId)
            {
                return Armed.Contains(moduleId);
            }

            public Task<bool> ArmAsync(string moduleId, bool persist = true, CancellationToken cancellationToken = default)
            {
                Record($"Arm {moduleId} persist={persist}");
                return ThrowOnArm
                    ? throw new InvalidOperationException("arm failed")
                    : Task.FromResult(moduleId != "Refused");
            }

            public Task<bool> DisarmAsync(string moduleId, bool persist = true, CancellationToken cancellationToken = default)
            {
                Record($"Disarm {moduleId} persist={persist}");
                return Task.FromResult(true);
            }

            public Task<bool> ReArmAsync(string moduleId, CancellationToken cancellationToken = default)
            {
                Record($"ReArm {moduleId}");
                return Task.FromResult(true);
            }

            public string? GetHotkeyError(string moduleId)
            {
                return HotkeyError;
            }

            public Task RestoreAsync(CancellationToken cancellationToken = default)
            {
                Record("Restore");
                return Task.CompletedTask;
            }

            public Task DisarmAllAsync(CancellationToken cancellationToken = default)
            {
                Record("DisarmAll");
                return Task.CompletedTask;
            }

            public void Persist()
            {
                Record("Persist");
            }

            public void Dispose() { }

            private void Record(string call)
            {
                lock (_calls)
                {
                    _calls.Add(call);
                }
            }
        }
    }
}
