using System.IO.Pipes;
using FluentAssertions;
using MosaicShell.Core.Capabilities.Ipc;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// The Worker used to give up if Host's flyout pipe was not there within one 4 s attempt, so the
    /// two had to be started in a hurry and in the right order. It now keeps trying for a window.
    /// </summary>
    public sealed class IpcFlyoutPresenterConnectTests
    {
        private static string NewPipeName()
        {
            return "MosaicShell.Test." + Guid.NewGuid().ToString("N");
        }

        [Fact]
        public async Task Connects_when_Host_starts_listening_after_the_Worker_began_waiting()
        {
            string pipeName = NewPipeName();
            using IpcFlyoutPresenter presenter = new();

            Task connect = presenter.ConnectWithRetryAsync(
                TimeSpan.FromSeconds(10), pipeName: pipeName, attemptTimeoutMs: 100, retryDelayMs: 50);
            await Task.Delay(400);
            presenter.IsConnected.Should().BeFalse("nothing was listening yet");

            await using NamedPipeServerStream server = new(
                pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            await server.WaitForConnectionAsync(new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token);
            await connect;

            presenter.IsConnected.Should().BeTrue();
        }

        [Fact]
        public async Task Gives_up_with_a_timeout_when_Host_never_appears()
        {
            using IpcFlyoutPresenter presenter = new();

            Func<Task> act = () => presenter.ConnectWithRetryAsync(
                TimeSpan.FromMilliseconds(400), pipeName: NewPipeName(), attemptTimeoutMs: 100, retryDelayMs: 50);

            await act.Should().ThrowAsync<TimeoutException>();
            presenter.IsConnected.Should().BeFalse();
        }

        [Fact]
        public async Task Stops_waiting_when_cancelled()
        {
            using IpcFlyoutPresenter presenter = new();
            using CancellationTokenSource cts = new(TimeSpan.FromMilliseconds(200));

            Func<Task> act = () => presenter.ConnectWithRetryAsync(
                TimeSpan.FromSeconds(30), cts.Token, NewPipeName(), attemptTimeoutMs: 100, retryDelayMs: 50);

            await act.Should().ThrowAsync<OperationCanceledException>();
        }
    }
}
