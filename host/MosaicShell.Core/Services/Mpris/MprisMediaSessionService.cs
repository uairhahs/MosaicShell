namespace MosaicShell.Core.Services.Mpris
{
    /// <summary>
    /// The current media session from MPRIS players on the session bus: the Linux counterpart of
    /// <c>WindowsMediaSessionService</c>. It raises the same events at the same moments, by the same rules
    /// (<see cref="MediaSessionChangePolicy"/>), so Tessera presents identically on both:
    /// <list type="bullet">
    /// <item>Changed for a new moment: track, artist, app, play state, or a skip seen only as a position jump.</item>
    /// <item>ProgressChanged for position (seek, the timeline poll) and for art that arrives after its track.</item>
    /// <item>No stop-then-start blip: a vanished player only clears the session after the null-session grace window.</item>
    /// </list>
    /// All state changes run one at a time under <c>_gate</c>; events are raised after it is released.
    /// </summary>
    public sealed class MprisMediaSessionService : IMediaSessionService
    {
        private readonly IMprisBus _bus;
        private readonly IMprisArtSource _art;
        private readonly TimeProvider _clock;
        private readonly SemaphoreSlim _gate = new(1, 1);
        private readonly Dictionary<string, MprisPlayerState> _players = new(StringComparer.Ordinal);
        private readonly Dictionary<string, DateTimeOffset> _playingSince = new(StringComparer.Ordinal);
        private MprisPlayerState? _active;
        private ITimer? _timelinePoll;
        private ITimer? _nullSessionGrace;
        private double _lastApiPos;
        private bool _hasLastApi;
        private bool _disposed;

        public MprisMediaSessionService(IMprisBus bus, IMprisArtSource art, TimeProvider? clock = null)
        {
            _bus = bus;
            _art = art;
            _clock = clock ?? TimeProvider.System;
            Initialization = InitAsync();
        }

        public MediaSessionInfo? Current { get; private set; }
        public event EventHandler? Changed;

        /// <summary>
        /// The browser process behind the player shown: the one a mirror names in <c>kde:pid</c>, else the player's
        /// own. Null when nothing is shown.
        /// </summary>
        public int? ActiveProcessId => _active is { } active ? active.MirroredPid ?? active.OwnerPid : null;

        /// <summary>Timeline / position / late art only: does not open flyouts.</summary>
        public event EventHandler? ProgressChanged;

        /// <summary>Completes once the bus is connected and the first read is done (or failed). For tests.</summary>
        internal Task Initialization { get; }

        private async Task InitAsync()
        {
            try
            {
                await _bus.ConnectAsync().ConfigureAwait(false);
                _bus.PlayersChanged += OnPlayersChanged;
                _bus.PlayerChanged += OnPlayerChanged;
                _art.Fetched += OnArtFetched;
                await RefreshAllAsync().ConfigureAwait(false);
                if (MediaSessionChangePolicy.MustPollTimelineIndependentlyOfFlyout && !_disposed)
                {
                    TimeSpan every = TimeSpan.FromMilliseconds(MediaSessionChangePolicy.TimelinePollMs);
                    _timelinePoll = _clock.CreateTimer(_ => PumpTimeline(), null, every, every);
                }
            }
            catch (Exception ex)
            {
                // No session bus (a bare TTY, a container): there is simply no media session.
                System.Diagnostics.Debug.WriteLine($"[MPRIS] unavailable: {ex.Message}");
                Current = null;
            }
        }

        private void OnPlayersChanged(object? sender, EventArgs e)
        {
            _ = RefreshAllAsync();
        }

        private void OnPlayerChanged(object? sender, MprisPlayerSignal signal)
        {
            _ = RefreshPlayerAsync(signal.BusName, signal.Seeked);
        }

        private void OnArtFetched(object? sender, EventArgs e)
        {
            _ = RunExclusiveAsync(() =>
            {
                Recompute(raiseProgress: false);
                return Task.CompletedTask;
            });
        }

        internal Task RefreshAllAsync()
        {
            return RunExclusiveAsync(async () =>
            {
                IReadOnlyList<string> names = await _bus.ListPlayersAsync().ConfigureAwait(false);
                HashSet<string> present = [.. names.Where(MprisPolicy.IsPlayerBusName)];
                foreach (string gone in _players.Keys.Where(k => !present.Contains(k)).ToList())
                {
                    _ = _players.Remove(gone);
                    _ = _playingSince.Remove(gone);
                }

                foreach (string name in present)
                {
                    await ReadIntoAsync(name).ConfigureAwait(false);
                }

                Recompute(raiseProgress: false);
            });
        }

        private Task RefreshPlayerAsync(string busName, bool seeked)
        {
            return RunExclusiveAsync(async () =>
            {
                await ReadIntoAsync(busName).ConfigureAwait(false);
                Recompute(raiseProgress: seeked);
            });
        }

        /// <summary>Reads one player into <c>_players</c> (removing it when gone). Call under the gate.</summary>
        private async Task ReadIntoAsync(string busName)
        {
            MprisPlayerState? state;
            try
            {
                state = await _bus.ReadPlayerAsync(busName).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MPRIS] read {busName}: {ex.Message}");
                state = null;
            }

            if (state is null)
            {
                _ = _players.Remove(busName);
                _ = _playingSince.Remove(busName);
                return;
            }

            bool wasPlaying = _players.TryGetValue(busName, out MprisPlayerState? before)
                && before.PlaybackStatus == MprisPlaybackStatus.Playing;
            if (state.PlaybackStatus == MprisPlaybackStatus.Playing && !wasPlaying)
            {
                _playingSince[busName] = _clock.GetUtcNow();
            }

            _players[busName] = state;
        }

        /// <summary>Picks the active player and publishes it as <see cref="Current"/>. Call under the gate.</summary>
        private void Recompute(bool raiseProgress)
        {
            IReadOnlyList<MprisPlayerState> shown = MprisPolicy.WithoutMirrored([.. _players.Values]);
            MprisPlayerState? active = MprisPolicy.PickActive(shown, _active?.BusName, _playingSince);
            if (active is null)
            {
                _active = null;
                _hasLastApi = false;
                if (Current is not null)
                {
                    ScheduleNullSession();
                }

                return;
            }

            CancelNullSession();
            if (!string.Equals(_active?.BusName, active.BusName, StringComparison.Ordinal))
            {
                _hasLastApi = false;
            }

            _active = active;
            byte[]? thumb = active.ArtUrl is { Length: > 0 } url ? _art.TryGet(url) : null;
            MediaSessionInfo next = MprisPolicy.ToSessionInfo(active, thumb);
            MediaSessionInfo? prev = Current;

            double apiPos = next.PositionSeconds;
            if (next.IsPlaying && prev is not null && MediaSessionChangePolicy.MustNotRewindPlayingScrubber && !raiseProgress
                && string.Equals(prev.Title, next.Title, StringComparison.Ordinal))
            {
                bool apiMoved = !_hasLastApi
                    || Math.Abs(apiPos - _lastApiPos) >= MediaSessionChangePolicy.PlayingPositionJitterSeconds;
                next = next with
                {
                    PositionSeconds = MediaSessionChangePolicy.ResolvePlayingPosition(
                        prev.PositionSeconds, apiPos, playing: true, incomingReportedChange: apiMoved),
                };
            }

            _lastApiPos = apiPos;
            _hasLastApi = true;
            Current = next;

            if (MediaSessionChangePolicy.IsMeaningfulSessionChange(prev, next))
            {
                Queue(Changed);
            }
            else if (raiseProgress
                || (prev is not null && MediaSessionChangePolicy.IsArtOnlyRefresh(
                    prev.ThumbnailPng?.Length ?? 0, next.ThumbnailPng?.Length ?? 0))
                || (prev is not null && Math.Abs(prev.PositionSeconds - next.PositionSeconds) >= 0.05))
            {
                Queue(ProgressChanged);
            }
        }

        /// <summary>
        /// The same grace as Windows (<see cref="MediaSessionChangePolicy.NullSessionGraceMs"/>): a browser player that
        /// is replaced on every track must not surface as a stop, but a real stop still lands.
        /// </summary>
        private void ScheduleNullSession()
        {
            if (_nullSessionGrace is not null)
            {
                return;
            }

            _nullSessionGrace = _clock.CreateTimer(
                _ => _ = RunExclusiveAsync(() =>
                {
                    CancelNullSession();
                    if (_active is null && Current is not null)
                    {
                        Current = null;
                        Queue(Changed);
                    }

                    return Task.CompletedTask;
                }),
                null,
                TimeSpan.FromMilliseconds(MediaSessionChangePolicy.NullSessionGraceMs),
                Timeout.InfiniteTimeSpan);
        }

        private void CancelNullSession()
        {
            _nullSessionGrace?.Dispose();
            _nullSessionGrace = null;
        }

        /// <summary>
        /// MPRIS never signals position as it advances, so it is polled; this is also what the UI timer calls.
        /// The player computes Position itself, so no extrapolation is needed, but the no-rewind and new-track rules
        /// apply exactly as on Windows.
        /// </summary>
        public void PumpTimeline()
        {
            _ = PumpTimelineAsync();
        }

        internal Task PumpTimelineAsync()
        {
            return RunExclusiveAsync(async () =>
            {
                if (_active is not { } active || Current is not { } prev)
                {
                    return;
                }

                long? micros;
                try
                {
                    micros = await _bus.ReadPositionAsync(active.BusName).ConfigureAwait(false);
                }
                catch
                {
                    micros = null;
                }

                if (micros is null)
                {
                    return;
                }

                double apiPos = Math.Max(0, micros.Value) / 1_000_000.0;
                bool apiMoved = !_hasLastApi
                    || Math.Abs(apiPos - _lastApiPos) >= MediaSessionChangePolicy.PlayingPositionJitterSeconds;
                _lastApiPos = apiPos;
                _hasLastApi = true;
                _players[active.BusName] = active with { PositionMicros = micros.Value };
                _active = _players[active.BusName];

                double pos = prev.IsPlaying
                    ? MediaSessionChangePolicy.ResolvePlayingPosition(prev.PositionSeconds, apiPos, playing: true, incomingReportedChange: apiMoved)
                    : apiPos;
                if (Math.Abs(prev.PositionSeconds - pos) < 0.05)
                {
                    return;
                }

                Current = prev with { PositionSeconds = pos };
                Queue(MediaSessionChangePolicy.LooksLikeNewTrackPosition(prev.PositionSeconds, pos) ? Changed : ProgressChanged);
            });
        }

        public Task PlayPauseAsync()
        {
            return CommandAsync(bus => _bus.PlayPauseAsync(bus.BusName));
        }

        public Task NextAsync()
        {
            return CommandAsync(bus => _bus.NextAsync(bus.BusName));
        }

        public Task PreviousAsync()
        {
            return CommandAsync(bus => _bus.PreviousAsync(bus.BusName));
        }

        public Task SeekAsync(double positionSeconds)
        {
            long target = (long)Math.Round(Math.Max(0, positionSeconds) * 1_000_000.0);
            return CommandAsync(player => MprisPolicy.CanSeekAbsolute(player.TrackId)
                ? _bus.SetPositionAsync(player.BusName, player.TrackId!, target)
                : _bus.SeekByAsync(player.BusName, target - player.PositionMicros));
        }

        public Task ToggleShuffleAsync()
        {
            return CommandAsync(player => _bus.SetShuffleAsync(player.BusName, !(player.Shuffle ?? false)));
        }

        public Task ToggleRepeatAsync()
        {
            return CommandAsync(player => _bus.SetLoopStatusAsync(player.BusName, MprisPolicy.NextLoopStatus(player.LoopStatus)));
        }

        /// <summary>MPRIS has no like API, as SMTC has none.</summary>
        public Task ToggleLikeAsync(bool wantLiked)
        {
            return Task.CompletedTask;
        }

        public Task ToggleDislikeAsync(bool wantDisliked)
        {
            return Task.CompletedTask;
        }

        private async Task CommandAsync(Func<MprisPlayerState, Task> send)
        {
            if (_disposed)
            {
                return;
            }

            if (_active is null)
            {
                await RefreshAllAsync().ConfigureAwait(false);
            }

            if (_active is not { } player)
            {
                return;
            }

            try
            {
                await send(player).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // The player can leave the bus between the read and the call, or refuse (CanControl false).
                System.Diagnostics.Debug.WriteLine($"[MPRIS] command to {player.BusName}: {ex.Message}");
            }
        }

        private readonly List<EventHandler?> _pending = [];

        private void Queue(EventHandler? handler)
        {
            _pending.Add(handler);
        }

        private async Task RunExclusiveAsync(Func<Task> work)
        {
            if (_disposed)
            {
                return;
            }

            EventHandler?[] raise;
            await _gate.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_disposed)
                {
                    return;
                }

                await work().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // A ThreadPool exception would take the Host down; a failed refresh only leaves the last state.
                System.Diagnostics.Debug.WriteLine($"[MPRIS] {ex}");
            }
            finally
            {
                raise = [.. _pending];
                _pending.Clear();
                _ = _gate.Release();
            }

            foreach (EventHandler? handler in raise)
            {
                handler?.Invoke(this, EventArgs.Empty);
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _timelinePoll?.Dispose();
            _timelinePoll = null;
            CancelNullSession();
            _bus.PlayersChanged -= OnPlayersChanged;
            _bus.PlayerChanged -= OnPlayerChanged;
            _art.Fetched -= OnArtFetched;
            _bus.Dispose();
            _art.Dispose();
        }
    }
}
