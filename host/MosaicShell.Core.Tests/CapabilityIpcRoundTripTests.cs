using FluentAssertions;
using MosaicShell.Core.Capabilities;
using MosaicShell.Core.Capabilities.Ipc;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// In-process named-pipe coverage for <see cref="CapabilityIpcFlyoutServer"/> +
    /// <see cref="IpcFlyoutPresenter"/>. This is the safety net the backlog's IPC codec/server
    /// dedupe (jaxcore-implementation-gaps E2) needs before merging it with
    /// <see cref="CapabilityIpcControlCodec"/> / <see cref="CapabilityIpcControlServer"/>: those
    /// paths had no transport-level test, only <c>CapabilityIpcCodecTests</c>' framing checks.
    /// </summary>
    public class CapabilityIpcRoundTripTests
    {
        [Fact]
        public async Task Show_request_reaches_server_presenter_and_client_observes_echoed_snapshot()
        {
            string pipeName = "MosaicShell.Test.Flyout." + Guid.NewGuid().ToString("N");
            TaskCompletionSource<FlyoutRequest> shown = new(TaskCreationOptions.RunContinuationsAsynchronously);
            RecordingPresenter presenter = new(shown);

            using CapabilityIpcFlyoutServer server = new(presenter, action => action(), pipeName);
            server.Start();

            using IpcFlyoutPresenter client = new();
            await client.ConnectAsync(pipeName: pipeName).WaitAsync(TimeSpan.FromSeconds(5));

            FlyoutRequest request = new("Tessera", "vol", StyleId: "Fluent", AutoDismissMs: 1200);
            client.Show(request);

            FlyoutRequest received = await shown.Task.WaitAsync(TimeSpan.FromSeconds(5));
            _ = received.ModuleId.Should().Be("Tessera");
            _ = received.Kind.Should().Be("vol");
            _ = received.StyleId.Should().Be("Fluent");
            _ = received.AutoDismissMs.Should().Be(1200);

            await WaitForAsync(() => client.IsVisible("Tessera"), TimeSpan.FromSeconds(5));
            _ = client.IsVisible("Tessera").Should().BeTrue("the server echoes a session snapshot back after Show");
        }

        [Fact]
        public async Task Hide_request_reaches_server_presenter()
        {
            string pipeName = "MosaicShell.Test.Flyout." + Guid.NewGuid().ToString("N");
            TaskCompletionSource<string> hidden = new(TaskCreationOptions.RunContinuationsAsynchronously);
            RecordingPresenter presenter = new(hidden: hidden);

            using CapabilityIpcFlyoutServer server = new(presenter, action => action(), pipeName);
            server.Start();

            using IpcFlyoutPresenter client = new();
            await client.ConnectAsync(pipeName: pipeName).WaitAsync(TimeSpan.FromSeconds(5));

            client.Hide("Tessera");

            string hiddenModuleId = await hidden.Task.WaitAsync(TimeSpan.FromSeconds(5));
            _ = hiddenModuleId.Should().Be("Tessera");
        }

        [Fact]
        public async Task A_restarted_worker_can_reconnect_after_the_previous_one_went_away()
        {
            string pipeName = "MosaicShell.Test.Flyout." + Guid.NewGuid().ToString("N");
            TaskCompletionSource<string> hidden = new(TaskCreationOptions.RunContinuationsAsynchronously);
            using CapabilityIpcFlyoutServer server = new(new RecordingPresenter(hidden: hidden), action => action(), pipeName);
            server.Start();
            IpcFlyoutPresenter first = new();
            await first.ConnectAsync(pipeName: pipeName).WaitAsync(TimeSpan.FromSeconds(5));
            first.Dispose();

            using IpcFlyoutPresenter second = new();
            await second.ConnectWithRetryAsync(TimeSpan.FromSeconds(8), pipeName: pipeName, attemptTimeoutMs: 500, retryDelayMs: 50).WaitAsync(TimeSpan.FromSeconds(12));
            second.Hide("Tessera");

            (await hidden.Task.WaitAsync(TimeSpan.FromSeconds(5))).Should().Be("Tessera");
        }

        [Fact]
        public async Task A_worker_reconnects_by_itself_when_the_host_restarts()
        {
            string pipeName = "MosaicShell.Test.Flyout." + Guid.NewGuid().ToString("N");
            TaskCompletionSource<string> hiddenOnFirst = new(TaskCreationOptions.RunContinuationsAsynchronously);
            TaskCompletionSource<string> hiddenOnSecond = new(TaskCreationOptions.RunContinuationsAsynchronously);
            CapabilityIpcFlyoutServer firstHost = new(new RecordingPresenter(hidden: hiddenOnFirst), action => action(), pipeName);
            firstHost.Start();
            using IpcFlyoutPresenter worker = new();
            using CancellationTokenSource stop = new();
            Task keepConnected = worker.KeepConnectedAsync(stop.Token, pipeName, attemptTimeoutMs: 300, retryDelayMs: 50);
            await WaitForAsync(() => worker.IsConnected, TimeSpan.FromSeconds(5));
            worker.Hide("Tessera");
            _ = await hiddenOnFirst.Task.WaitAsync(TimeSpan.FromSeconds(5));

            firstHost.Dispose();
            await WaitForAsync(() => !worker.IsConnected, TimeSpan.FromSeconds(5));
            using CapabilityIpcFlyoutServer secondHost = new(new RecordingPresenter(hidden: hiddenOnSecond), action => action(), pipeName);
            secondHost.Start();
            await WaitForAsync(() => worker.IsConnected, TimeSpan.FromSeconds(10));
            worker.Hide("Tessera");

            (await hiddenOnSecond.Task.WaitAsync(TimeSpan.FromSeconds(5))).Should().Be("Tessera");
            stop.Cancel();
            await keepConnected.WaitAsync(TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task Keeping_connected_stops_quietly_when_cancelled_even_if_no_host_ever_appears()
        {
            using IpcFlyoutPresenter worker = new();
            using CancellationTokenSource stop = new();
            Task keepConnected = worker.KeepConnectedAsync(
                stop.Token, "MosaicShell.Test.Flyout." + Guid.NewGuid().ToString("N"), attemptTimeoutMs: 100, retryDelayMs: 20);
            await Task.Delay(300);

            stop.Cancel();

            await keepConnected.WaitAsync(TimeSpan.FromSeconds(5));
            worker.IsConnected.Should().BeFalse();
        }

        [Fact]
        public async Task An_idle_connected_client_does_not_make_the_flyout_server_spin()
        {
            string pipeName = "MosaicShell.Test.Flyout." + Guid.NewGuid().ToString("N");
            using CapabilityIpcFlyoutServer server = new(new RecordingPresenter(), action => action(), pipeName);
            server.Start();
            using IpcFlyoutPresenter client = new();
            await client.ConnectAsync(pipeName: pipeName).WaitAsync(TimeSpan.FromSeconds(5));
            await Task.Delay(200);

            double share = await IdleCpu.LowestShareAsync();

            share.Should().BeLessThan(0.5, "the accept loop must wait for the connection to end, not retry in a tight loop");
        }

        private static async Task WaitForAsync(Func<bool> condition, TimeSpan timeout)
        {
            DateTime deadline = DateTime.UtcNow + timeout;
            while (DateTime.UtcNow < deadline)
            {
                if (condition())
                {
                    return;
                }

                await Task.Delay(20);
            }

            throw new TimeoutException("Condition was not met within the timeout.");
        }

        private sealed class RecordingPresenter(
            TaskCompletionSource<FlyoutRequest>? shown = null,
            TaskCompletionSource<string>? hidden = null) : IFlyoutPresenter
        {
            public event Action<string>? TransientDismissed { add { } remove { } }

            public void Show(FlyoutRequest request)
            {
                _ = shown?.TrySetResult(request);
            }

            public void Update(FlyoutRequest request) { }
            public void SoftRefresh(FlyoutRequest request) { }

            public void Hide(string moduleId)
            {
                _ = hidden?.TrySetResult(moduleId);
            }

            public void HideAll() { }

            public bool IsVisible(string moduleId)
            {
                return shown?.Task.IsCompletedSuccessfully == true;
            }
        }
    }
}
