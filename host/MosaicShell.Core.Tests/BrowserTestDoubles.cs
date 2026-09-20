using System.Text;
using MosaicShell.Core.Services;
using MosaicShell.Core.Services.BrowserBridge;

namespace MosaicShell.Core.Tests
{
    /// <summary>The Host's end of a relay connection, recording what the hub sends it.</summary>
    internal sealed class FakeBrowserConnection : IBrowserConnection
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

    /// <summary>A Windows media session the test sets by hand.</summary>
    internal sealed class FakeSmtc : IMediaSessionService
    {
        public MediaSessionInfo? Current { get; private set; }
        public event EventHandler? Changed;
        public event EventHandler? ProgressChanged
        {
            add { }
            remove { }
        }

        public void Set(MediaSessionInfo info)
        {
            Current = info;
            Changed?.Invoke(this, EventArgs.Empty);
        }

        public void PumpTimeline()
        {
        }

        public Task PlayPauseAsync()
        {
            return Task.CompletedTask;
        }

        public Task NextAsync()
        {
            return Task.CompletedTask;
        }

        public Task PreviousAsync()
        {
            return Task.CompletedTask;
        }

        public Task SeekAsync(double positionSeconds)
        {
            return Task.CompletedTask;
        }

        public Task ToggleShuffleAsync()
        {
            return Task.CompletedTask;
        }

        public Task ToggleRepeatAsync()
        {
            return Task.CompletedTask;
        }

        public Task ToggleLikeAsync(bool wantLiked)
        {
            return Task.CompletedTask;
        }

        public Task ToggleDislikeAsync(bool wantDisliked)
        {
            return Task.CompletedTask;
        }

        public void Dispose()
        {
        }
    }

    /// <summary>An HTTP handler that answers from a delegate and counts the requests it received.</summary>
    internal sealed class StubHttpHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond) : HttpMessageHandler
    {
        private int _calls;

        public int Calls => _calls;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _ = Interlocked.Increment(ref _calls);
            return respond(request, cancellationToken);
        }
    }
}
