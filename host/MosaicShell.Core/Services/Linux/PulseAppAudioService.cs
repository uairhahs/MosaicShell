using System.Diagnostics;
using MosaicShell.Core.Services.Pulse;

namespace MosaicShell.Core.Services.Linux
{
    /// <summary>
    /// Per-app volume and mute on Linux through pactl (PulseAudio, or PipeWire's PulseAudio layer), one session per app
    /// as <see cref="PulseAppAudioPolicy"/> groups its streams. <c>pactl subscribe</c> drives re-reads, and the getters
    /// return the last list read, so they never block.
    /// </summary>
    /// <remarks>
    /// Starts on the first <see cref="GetSessions"/>, so a Host or Worker with no app audio tile runs no pactl. The first
    /// read, and every read that changes the list, raises SessionsChanged. A write moves the cached session at once and
    /// raises SessionsChanged, as Windows does; writes run in order off the caller's thread, latest wins per stream, and
    /// a re-read that lands while writes are pending is applied only after them, so a slider never jumps back. Without
    /// pactl or a server there are no sessions and nothing throws.
    /// <para>
    /// It also lists the output devices and sets the default (<see cref="PulseOutputDevicePolicy"/>), on the same
    /// subscription, so one pactl serves both; Windows only lists them.
    /// </para>
    /// </remarks>
    public sealed class PulseAppAudioService : IAppAudioService, IAudioDeviceService
    {
        private static readonly TimeSpan MaxResubscribeDelay = TimeSpan.FromSeconds(30);

        private readonly IPactl _pactl;
        private readonly ProcessPactl? _ownedPactl;
        private readonly TimeSpan _resubscribeDelay;
        private readonly CancellationTokenSource _stop = new();
        private readonly Lock _gate = new();
        private readonly SemaphoreSlim _writeSignal = new(0);
        private readonly List<string[]> _pending = [];
        private IReadOnlyList<PulseApp> _apps = [];
        private IReadOnlyList<AudioOutputDevice> _outputs = [];
        private bool _writing;
        private bool _rereadAfterWrites;
        private bool _firstRead;
        private int _started;
        private int _disposed;

        public PulseAppAudioService(IPactl? pactl = null, TimeSpan? resubscribeDelay = null)
        {
            _ownedPactl = pactl is null ? new ProcessPactl() : null;
            _pactl = pactl ?? _ownedPactl!;
            _resubscribeDelay = resubscribeDelay ?? TimeSpan.FromSeconds(1);
        }

        public event EventHandler? SessionsChanged;

        public IReadOnlyList<AppAudioSession> GetSessions()
        {
            EnsureStarted();
            lock (_gate)
            {
                return [.. _apps.Select(static a => a.ToSession())];
            }
        }

        public IReadOnlyList<AudioOutputDevice> GetOutputDevices()
        {
            EnsureStarted();
            lock (_gate)
            {
                return _outputs;
            }
        }

        public void SetDefaultOutput(string deviceId)
        {
            lock (_gate)
            {
                if (!_outputs.Any(o => o.Id == deviceId))
                {
                    return;
                }

                _outputs = [.. _outputs.Select(o => o with { IsDefault = o.Id == deviceId })];
                Queue(["set-default-sink", deviceId]);
            }

            _ = _writeSignal.Release();
        }

        private void EnsureStarted()
        {
            if (Interlocked.Exchange(ref _started, 1) == 0 && Volatile.Read(ref _disposed) == 0)
            {
                _ = Task.Run(() => WatchAsync(_stop.Token));
                _ = Task.Run(() => WriteAsync(_stop.Token));
            }
        }

        /// <summary>Latest wins: an earlier write of the same kind to the same stream, or any earlier default, is dropped.</summary>
        private void Queue(string[] command)
        {
            _ = _pending.RemoveAll(p => p[0] == command[0] && (p[0] == "set-default-sink" || p[1] == command[1]));
            _pending.Add(command);
        }

        public void SetVolume(string sessionId, double volume)
        {
            double target = VolumePercent.Quantize(volume);
            Write(sessionId, app => (
                PulseAppAudioPolicy.VolumeCommands(app, target),
                app with { Streams = [.. app.Streams.Select(s => s with { Channels = Scaled(s.Channels, target) })] }));
        }

        public void SetMuted(string sessionId, bool muted)
        {
            Write(sessionId, app => (
                PulseAppAudioPolicy.MuteCommands(app, muted),
                app with { Streams = [.. app.Streams.Select(s => s with { Muted = muted })] }));
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }

            _stop.Cancel();
            _ownedPactl?.Dispose();
        }

        private static IReadOnlyList<PulseChannel> Scaled(IReadOnlyList<PulseChannel> channels, double target)
        {
            IReadOnlyList<uint> values = PulseVolumePolicy.ScaleTo(channels, target);
            return [.. channels.Select((c, i) => c with { Value = values[i] })];
        }

        private void Write(string sessionId, Func<PulseApp, (IReadOnlyList<string[]> Commands, PulseApp Moved)> change)
        {
            lock (_gate)
            {
                int at = _apps.ToList().FindIndex(a => a.Id == sessionId);
                if (at < 0)
                {
                    return;
                }

                (IReadOnlyList<string[]> commands, PulseApp moved) = change(_apps[at]);
                _apps = [.. _apps.Select((a, i) => i == at ? moved : a)];
                foreach (string[] command in commands)
                {
                    Queue(command);
                }
            }

            _ = _writeSignal.Release();
            Raise();
        }

        private async Task WriteAsync(CancellationToken ct)
        {
            try
            {
                while (true)
                {
                    await _writeSignal.WaitAsync(ct).ConfigureAwait(false);
                    while (true)
                    {
                        string[] command;
                        lock (_gate)
                        {
                            if (_pending.Count == 0)
                            {
                                _writing = false;
                                break;
                            }

                            _writing = true;
                            command = _pending[0];
                            _pending.RemoveAt(0);
                        }

                        _ = await _pactl.RunAsync(command, ct).ConfigureAwait(false);
                    }

                    bool reread;
                    lock (_gate)
                    {
                        reread = _rereadAfterWrites && _pending.Count == 0;
                        _rereadAfterWrites &= !reread;
                    }

                    if (reread)
                    {
                        _ = await RefreshAppsAsync(ct).ConfigureAwait(false);
                        _ = await RefreshOutputsAsync(ct).ConfigureAwait(false);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Disposed.
            }
        }

        private async Task WatchAsync(CancellationToken ct)
        {
            TimeSpan delay = _resubscribeDelay;
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    // Read before (re)subscribing, so nothing missed while unsubscribed is lost.
                    bool apps = await RefreshAppsAsync(ct).ConfigureAwait(false);
                    if (await RefreshOutputsAsync(ct).ConfigureAwait(false) | apps)
                    {
                        delay = _resubscribeDelay;
                    }

                    await foreach (string line in _pactl.SubscribeAsync(ct).ConfigureAwait(false))
                    {
                        if (PulseAppAudioPolicy.NeedsRefresh(line))
                        {
                            _ = await RefreshAppsAsync(ct).ConfigureAwait(false);
                        }
                        else if (PulseVolumePolicy.NeedsRefresh(line))
                        {
                            // A sink came or went, or the default changed.
                            _ = await RefreshOutputsAsync(ct).ConfigureAwait(false);
                        }
                    }
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[PulseAppAudio] watch: {ex.Message}");
                }

                try
                {
                    await Task.Delay(delay, ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return;
                }

                delay = TimeSpan.FromTicks(Math.Min(delay.Ticks * 2, MaxResubscribeDelay.Ticks));
            }
        }

        /// <summary>Reads the outputs and the default; false when pactl gave nothing usable.</summary>
        private async Task<bool> RefreshOutputsAsync(CancellationToken ct)
        {
            string? sink = await _pactl.RunAsync(["get-default-sink"], ct).ConfigureAwait(false);
            string? json = await _pactl.RunAsync(["-f", "json", "list", "sinks"], ct).ConfigureAwait(false);
            if (json is null || json.TrimStart() is not ['[', ..])
            {
                return false;
            }

            IReadOnlyList<AudioOutputDevice> outputs = PulseOutputDevicePolicy.ParseOutputs(json, sink);
            lock (_gate)
            {
                if (_writing || _pending.Count > 0)
                {
                    _rereadAfterWrites = true;
                    return true;
                }

                _outputs = outputs;
            }

            return true;
        }

        /// <summary>Reads the streams; false when pactl gave nothing usable.</summary>
        private async Task<bool> RefreshAppsAsync(CancellationToken ct)
        {
            string? json = await _pactl.RunAsync(["-f", "json", "list", "sink-inputs"], ct).ConfigureAwait(false);
            if (json is null || json.TrimStart() is not ['[', ..])
            {
                return false;
            }

            IReadOnlyList<PulseApp> apps = PulseAppAudioPolicy.ParseApps(json);
            bool changed;
            lock (_gate)
            {
                if (_writing || _pending.Count > 0)
                {
                    _rereadAfterWrites = true;
                    return true;
                }

                changed = !_firstRead || !Same(_apps, apps);
                _firstRead = true;
                _apps = apps;
            }

            if (changed)
            {
                Raise();
            }

            return true;
        }

        private static bool Same(IReadOnlyList<PulseApp> a, IReadOnlyList<PulseApp> b)
        {
            return a.Count == b.Count && a.Zip(b).All(p =>
                p.First.Id == p.Second.Id
                && p.First.DisplayName == p.Second.DisplayName
                && p.First.Streams.Count == p.Second.Streams.Count
                && p.First.Streams.Zip(p.Second.Streams).All(s =>
                    s.First.Index == s.Second.Index && s.First.Muted == s.Second.Muted && s.First.Channels.SequenceEqual(s.Second.Channels)));
        }

        private void Raise()
        {
            try
            {
                SessionsChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                // A subscriber's failure must not stop the watch.
                Debug.WriteLine($"[PulseAppAudio] handler: {ex.Message}");
            }
        }
    }
}
