using System.Runtime.InteropServices;

namespace MosaicShell.Core.Services.BrowserUi
{
    /// <summary>
    /// A browser source that needs nothing installed or configured. For the track Windows says a browser is playing, it
    /// finds that browser's window through the accessibility tree and reads YouTube Music's real like and dislike state,
    /// which Windows' media session does not carry, and it presses the buttons when asked. Title, artist and cover come
    /// from Windows itself, so this source supplies only the rating.
    /// </summary>
    /// <remarks>
    /// The accessibility tree holds only the page a window is showing, so a player in a background tab is not seen and
    /// reports nothing; the flyout then simply has no rating for it. Nothing is read unless a browser session is playing.
    /// Every call into <see cref="IBrowserUi"/> happens on a thread-pool thread, never the caller's, so a slow or hung
    /// browser cannot stall the Host's UI thread.
    /// </remarks>
    public sealed class BrowserUiSource : IBrowserMediaSource
    {
        public static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);

        private static readonly TimeSpan CommandRecheck = TimeSpan.FromMilliseconds(400);

        /// <summary>After a track change the page needs a moment to show the new track before it can be read.</summary>
        private static readonly TimeSpan TrackChangeRecheck = TimeSpan.FromMilliseconds(300);

        private readonly IBrowserUi _ui;
        private readonly Func<MediaSessionInfo?> _session;
        private readonly TimeProvider _clock;
        /// <summary>Guards the state below. Held only briefly, never across a call into the browser.</summary>
        private readonly Lock _gate = new();

        /// <summary>Serializes reads of the browser, which can be slow. Kept apart from <see cref="_gate"/> so a reader of <see cref="Active"/> never waits on it.</summary>
        private readonly Lock _readGate = new();
        private readonly ITimer _poll;
        private ITimer? _recheck;
        private bool _recheckPending;
        private BrowserPlayerSnapshot? _active;
        private bool _disposed;

        public BrowserUiSource(IBrowserUi ui, Func<MediaSessionInfo?> session, TimeProvider clock)
        {
            _ui = ui;
            _session = session;
            _clock = clock;
            _poll = clock.CreateTimer(_ => Refresh(), null, TimeSpan.Zero, PollInterval);
        }

        public event EventHandler? Changed;

        /// <summary>
        /// The player as last read, but never for a track that is no longer the one playing: a rating read for the previous
        /// track must not be shown for the next. When they differ this asks for a recheck and reports nothing meanwhile.
        /// </summary>
        public BrowserPlayerSnapshot? Active
        {
            get
            {
                lock (_gate)
                {
                    if (_active is null || IsTrack(_active.Title, _session()))
                    {
                        return _active;
                    }

                    Recheck(TrackChangeRecheck, replacePending: false);
                    return null;
                }
            }
        }

        /// <summary>
        /// Reads the page again and raises <see cref="Changed"/> if what the flyout shows would differ. Skipped when a
        /// read is already under way: another one queued behind a slow browser would only pile up.
        /// </summary>
        public void Refresh()
        {
            if (!_readGate.TryEnter())
            {
                return;
            }

            bool changed;
            try
            {
                lock (_gate)
                {
                    if (_disposed)
                    {
                        return;
                    }

                    _recheckPending = false;
                }

                BrowserPlayerSnapshot? next = Read().Snapshot;
                lock (_gate)
                {
                    changed = !Same(_active, next);
                    _active = next;
                }
            }
            finally
            {
                _readGate.Exit();
            }

            if (changed)
            {
                Changed?.Invoke(this, EventArgs.Empty);
            }
        }

        public Task SetLikedAsync(bool liked)
        {
            return Press(controls => controls.PressForLike(liked));
        }

        public Task SetDislikedAsync(bool disliked)
        {
            return Press(controls => controls.PressForDislike(disliked));
        }

        /// <summary>Not offered: no player declares the capability, so the composite never asks.</summary>
        public Task ToggleShuffleAsync()
        {
            return Task.CompletedTask;
        }

        /// <summary>Not offered: no player declares the capability, so the composite never asks.</summary>
        public Task ToggleRepeatAsync()
        {
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            lock (_gate)
            {
                _disposed = true;
                _poll.Dispose();
                _recheck?.Dispose();
            }
        }

        private static bool IsCandidate(string windowTitle, string track)
        {
            return windowTitle.Contains(YouTubeMusicControls.SiteName, StringComparison.OrdinalIgnoreCase)
                || windowTitle.Contains(track, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsTrack(string track, MediaSessionInfo? session)
        {
            return string.Equals(TrackOf(session), track, StringComparison.OrdinalIgnoreCase);
        }

        private static string TrackOf(MediaSessionInfo? session)
        {
            return MediaTitleNormalizer.StripSiteSuffix(session?.Title)?.Trim() ?? "";
        }

        private static bool Same(BrowserPlayerSnapshot? a, BrowserPlayerSnapshot? b)
        {
            return a is null || b is null
                ? a is null && b is null
                : a.Title == b.Title && a.Artist == b.Artist && a.State == b.State && a.Rating == b.Rating && a.Capabilities == b.Capabilities;
        }

        private static bool IsUiFailure(Exception ex)
        {
            return ex is COMException or InvalidCastException or InvalidComObjectException or InvalidOperationException
                or UnauthorizedAccessException or TimeoutException or ArgumentException;
        }

        private (BrowserPlayerSnapshot? Snapshot, YouTubeMusicControls? Controls) Read()
        {
            MediaSessionInfo? session = _session();
            if (session is null || !BrowserSessionPolicy.LooksLikeBrowserSession(session.AppId))
            {
                return (null, null);
            }

            string track = TrackOf(session);
            if (track.Length == 0)
            {
                return (null, null);
            }

            try
            {
                // Only windows that could be the player are touched: the page title does not always name the playing
                // track, but a window that mentions neither YouTube Music nor the track cannot be it. A window whose
                // title names the track is tried first; each candidate must show that track in its own player.
                foreach (IUiWindow window in _ui.Windows()
                    .Where(w => IsCandidate(w.Title, track))
                    .OrderByDescending(w => w.Title.Contains(track, StringComparison.OrdinalIgnoreCase)))
                {
                    if (MediaTitleNormalizer.LooselyMatch(window.PlayerTitle(), session.Title)
                        && YouTubeMusicControls.Find(window.Toggles()) is { Rating: { } rating } controls)
                    {
                        return (Present(session, track, rating), controls);
                    }
                }

                return (null, null);
            }
            catch (Exception ex) when (IsUiFailure(ex))
            {
                return (null, null);
            }
        }

        private static BrowserPlayerSnapshot Present(MediaSessionInfo session, string track, BrowserRating rating)
        {
            return new BrowserPlayerSnapshot
            {
                Name = "YouTube Music",
                Title = track,
                Artist = session.Artist ?? "",
                State = session.IsPlaying ? BrowserPlaybackState.Playing : BrowserPlaybackState.Paused,
                Rating = rating,
                Capabilities = BrowserMediaCapabilities.Rating | BrowserMediaCapabilities.Dislike,
            };
        }

        /// <summary>
        /// Reads the state now, not as of the last poll, so the press is right even if the page changed since. Runs on
        /// a thread-pool thread, so the flyout that asked is not held up by the browser.
        /// </summary>
        private Task Press(Func<YouTubeMusicControls, UiToggle?> choose)
        {
            return Task.Run(() =>
            {
                lock (_readGate)
                {
                    lock (_gate)
                    {
                        if (_disposed)
                        {
                            return;
                        }
                    }

                    try
                    {
                        if (Read().Controls is { } controls)
                        {
                            choose(controls)?.Press();
                        }
                    }
                    catch (Exception ex) when (IsUiFailure(ex))
                    {
                        // The element went away between reading it and pressing it; the next poll sorts it out.
                    }

                    Recheck(CommandRecheck, replacePending: true);
                }
            });
        }

        /// <summary>
        /// Reads the page again after <paramref name="delay"/>. A recheck already waiting is kept unless
        /// <paramref name="replacePending"/>, so that being asked repeatedly cannot keep pushing it back.
        /// </summary>
        private void Recheck(TimeSpan delay, bool replacePending)
        {
            lock (_gate)
            {
                if (_disposed || (_recheckPending && !replacePending))
                {
                    return;
                }

                _recheck?.Dispose();
                _recheckPending = true;
                _recheck = _clock.CreateTimer(_ => Refresh(), null, delay, Timeout.InfiniteTimeSpan);
            }
        }
    }
}
