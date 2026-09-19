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
