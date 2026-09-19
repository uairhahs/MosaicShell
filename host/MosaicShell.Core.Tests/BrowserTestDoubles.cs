using MosaicShell.Core.Services;

namespace MosaicShell.Core.Tests
{
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
}
