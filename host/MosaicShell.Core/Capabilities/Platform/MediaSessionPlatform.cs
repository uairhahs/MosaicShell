
using MosaicShell.Core.Services;

namespace MosaicShell.Core.Capabilities.Platform
{
    /// <summary>
    /// Shared media signal normalization. SMTC/WNP polling lives in <see cref="IMediaSessionService"/>;
    /// this layer classifies Changed vs Progress and track boundaries for capabilities.
    /// </summary>
    public sealed class MediaSessionPlatform(IMediaSessionService media) : IDisposable
    {
        private readonly IMediaSessionService _media = media;
        private readonly Lock _settleGate = new();
        private int _consumers;
        private MediaSessionInfo? _lastIdentity;
        private Timer? _settleTimer;
        private bool _disposed;

        public event Action<MediaSessionSignal>? Signal;

        public MediaSessionInfo? Current => _media.Current;

        /// <summary>Call from capability Arm when module cares about media flyouts or strip refresh.</summary>
        public void Acquire()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_consumers++ != 0)
            {
                return;
            }

            _media.Changed += OnChanged;
            _media.ProgressChanged += OnProgress;
            _lastIdentity = _media.Current;
        }

        /// <summary>Call from capability Disarm.</summary>
        public void Release()
        {
            if (_disposed)
            {
                return;
            }

            if (--_consumers > 0)
            {
                return;
            }

            Detach();
        }

        /// <summary>
        /// Unsubscribe and drop per-session state. Separate from <see cref="Release"/> so
        /// <see cref="Dispose"/> can tear down without going through the refcount - Release
        /// early-returns once disposed, so draining consumers through it would never decrement
        /// <c>_consumers</c> and would spin forever.
        /// </summary>
        private void Detach()
        {
            _consumers = 0;
            _media.Changed -= OnChanged;
            _media.ProgressChanged -= OnProgress;
            CancelSettleTimer();
            _lastIdentity = null;
        }

        public void PumpTimeline()
        {
            try { _media.PumpTimeline(); }
            catch { /* soft-fail */ }
        }

        private void OnChanged(object? sender, EventArgs e)
        {
            MediaSessionInfo? current = _media.Current;
            bool trackBoundary = MediaFlyoutRouter.IsTrackBoundary(_lastIdentity, current);

            // A skip arrives as a settling sequence, not a single event: placeholder title, then
            // tab title, then real metadata. Announcing a boundary for each stage rebinds the
            // flyout mid-entrance and shows placeholder text (see MediaSessionChangePolicy).
            // Hold the boundary while metadata is still settling and let the stage that carries
            // a real artist announce it - or the settle timer, if none ever does.
            if (trackBoundary
                && MediaSessionChangePolicy.IsMetadataSettling(current?.Title, current?.Artist))
            {
                ArmSettleTimer();

                // Still surface the change as a non-boundary so an already-visible flyout keeps
                // soft-refreshing; only the "present a new track" classification is withheld.
                Raise(new MediaSessionSignal(current, MediaSessionSignalKind.SessionChanged, false));
                return;
            }

            if (trackBoundary)
            {
                CancelSettleTimer();
                _lastIdentity = current;
            }

            Raise(new MediaSessionSignal(current, MediaSessionSignalKind.SessionChanged, trackBoundary));
        }

        /// <summary>
        /// Fallback for a track that genuinely has no artist: settling must delay a boundary,
        /// never cancel it. Re-armed on each settling stage so the window measures quiet time,
        /// not time since the first stage.
        /// </summary>
        private void ArmSettleTimer()
        {
            lock (_settleGate)
            {
                if (_disposed)
                {
                    return;
                }

                _settleTimer?.Dispose();
                _settleTimer = new Timer(
                    _ => OnSettleElapsed(),
                    null,
                    MediaSessionChangePolicy.MetadataSettleMs,
                    Timeout.Infinite);
            }
        }

        private void CancelSettleTimer()
        {
            lock (_settleGate)
            {
                _settleTimer?.Dispose();
                _settleTimer = null;
            }
        }

        private void OnSettleElapsed()
        {
            // Runs on a thread-pool thread; an escape here would take down the process.
            try
            {
                CancelSettleTimer();
                if (_disposed || _consumers == 0)
                {
                    return;
                }

                MediaSessionInfo? current = _media.Current;
                if (!MediaFlyoutRouter.IsTrackBoundary(_lastIdentity, current))
                {
                    return;
                }

                _lastIdentity = current;
                Raise(new MediaSessionSignal(current, MediaSessionSignalKind.SessionChanged, true));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MediaSessionPlatform settle] {ex}");
            }
        }

        private void OnProgress(object? sender, EventArgs e)
        {
            Raise(new MediaSessionSignal(_media.Current, MediaSessionSignalKind.Progress, false));
        }

        private void Raise(MediaSessionSignal signal)
        {
            try { Signal?.Invoke(signal); }
            catch { /* soft-fail */ }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            Detach();
        }
    }

    public enum MediaSessionSignalKind
    {
        SessionChanged,
        Progress,
    }

    public sealed record MediaSessionSignal(
        MediaSessionInfo? Current,
        MediaSessionSignalKind Kind,
        bool IsTrackBoundary);
}
