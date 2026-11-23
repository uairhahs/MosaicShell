using System.Diagnostics;
using System.Runtime.InteropServices;
using MosaicShell.Core.Services.Pulse;

namespace MosaicShell.Core.Services.Linux
{
    /// <summary>The pactl calls <see cref="PulseAudioService"/> needs; a seam so tests never touch real audio.</summary>
    public interface IPactl
    {
        /// <summary>Runs pactl with these arguments; its standard output, or null when it failed or is missing.</summary>
        Task<string?> RunAsync(IReadOnlyList<string> args, CancellationToken cancellationToken);

        /// <summary>The lines of <c>pactl subscribe</c> until it exits or is cancelled.</summary>
        IAsyncEnumerable<string> SubscribeAsync(CancellationToken cancellationToken);
    }

    /// <summary>
    /// Master volume and mute of the default output on Linux, through pactl (PulseAudio, or PipeWire's PulseAudio
    /// layer). <c>pactl subscribe</c> drives re-reads; the getters return the last state read, so they never block.
    /// </summary>
    /// <remarks>
    /// Changed follows WindowsAudioService: an external volume or mute change raises it; this service's own volume
    /// write does not (it pre-arms the value, as Windows does); its own mute write does; a new default output is read
    /// without raising it; changes under 0.4% are noise. Writes run off the caller's thread, latest wins, and the
    /// cached value moves at once so a slider never jumps back. Without pactl or a server everything reads zero and
    /// nothing throws.
    /// </remarks>
    public sealed class PulseAudioService : IAudioService
    {
        private const float NoiseThreshold = 0.004f;
        private static readonly TimeSpan MaxResubscribeDelay = TimeSpan.FromSeconds(30);

        private readonly IPactl _pactl;
        private readonly ProcessPactl? _ownedPactl;
        private readonly TimeSpan _resubscribeDelay;
        private readonly CancellationTokenSource _stop = new();
        private readonly Lock _gate = new();
        private readonly SemaphoreSlim _writeSignal = new(0);

        private IReadOnlyList<PulseChannel> _channels = [];
        private bool _muted;
        private bool _hasState;
        private string? _sink;

        // Baseline for Changed, as WindowsAudioService's _lastVol and _lastMute.
        private double _lastVolume = double.NaN;
        private bool _lastMute;

        private IReadOnlyList<uint>? _pendingVolume;
        private bool? _pendingMute;
        private bool _writing;

        public PulseAudioService(IPactl? pactl = null, TimeSpan? resubscribeDelay = null)
        {
            _ownedPactl = pactl is null ? new ProcessPactl() : null;
            _pactl = pactl ?? _ownedPactl!;
            _resubscribeDelay = resubscribeDelay ?? TimeSpan.FromSeconds(1);
            _ = Task.Run(() => WatchAsync(_stop.Token));
            _ = Task.Run(() => WriteAsync(_stop.Token));
        }

        public event EventHandler? Changed;

        public double MasterVolume
        {
            get
            {
                lock (_gate)
                {
                    return _hasState ? PulseVolumePolicy.Master(_channels) : 0;
                }
            }

            set
            {
                double target = VolumePercent.Quantize(value);
                lock (_gate)
                {
                    if (!_hasState || Math.Abs(PulseVolumePolicy.Master(_channels) - target) < NoiseThreshold)
                    {
                        return;
                    }

                    IReadOnlyList<uint> values = PulseVolumePolicy.ScaleTo(_channels, target);
                    _channels = [.. _channels.Select((c, i) => c with { Value = values[i] })];
                    _pendingVolume = values;

                    // Pre-arm, so the echo of our own write is not an external change.
                    _lastVolume = target;
                }

                _ = _writeSignal.Release();
            }
        }

        public bool IsMuted
        {
            get
            {
                lock (_gate)
                {
                    return _hasState && _muted;
                }
            }

            set
            {
                lock (_gate)
                {
                    if (!_hasState)
                    {
                        return;
                    }

                    _muted = value;
                    _pendingMute = value;
                }

                _ = _writeSignal.Release();
            }
        }

        public void Dispose()
        {
            _stop.Cancel();
            _ = _writeSignal.Release();
            // Synchronously: the cancelled loops' own cleanup may not run before the process exits.
            _ownedPactl?.Dispose();
        }

        private async Task WatchAsync(CancellationToken ct)
        {
            TimeSpan delay = _resubscribeDelay;
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    // Read before (re)subscribing, so nothing missed while unsubscribed is lost.
                    if (await RefreshAsync(ct).ConfigureAwait(false))
                    {
                        delay = _resubscribeDelay;
                    }

                    await foreach (string line in _pactl.SubscribeAsync(ct).ConfigureAwait(false))
                    {
                        if (PulseVolumePolicy.NeedsRefresh(line))
                        {
                            _ = await RefreshAsync(ct).ConfigureAwait(false);
                        }
                    }
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[PulseAudio] watch: {ex.Message}");
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

        /// <summary>Reads the default output; false when pactl gave nothing usable.</summary>
        private async Task<bool> RefreshAsync(CancellationToken ct)
        {
            // `list sinks` rather than `get-sink-volume`/`get-sink-mute`: pactl 17.0 as NixOS ships it prints those two
            // as text even with `-f json`, so neither parsed and the flyout showed 0% and muted (2026-11-23).
            string? sinkOut = await _pactl.RunAsync(["get-default-sink"], ct).ConfigureAwait(false);
            string sink = sinkOut?.Trim() ?? "";
            string? listOut = await _pactl.RunAsync(["-f", "json", "list", "sinks"], ct).ConfigureAwait(false);
            if (!PulseVolumePolicy.TryParseSink(listOut, sink, out IReadOnlyList<PulseChannel> channels, out bool muted))
            {
                return false;
            }

            bool raise = false;
            lock (_gate)
            {
                if (_writing || _pendingVolume is not null || _pendingMute is not null)
                {
                    // A read that raced our own write would move the slider back; the writer reads again when done.
                    return true;
                }

                _channels = channels;
                _muted = muted;
                _hasState = true;
                double volume = PulseVolumePolicy.Master(channels);
                if (_sink != sink)
                {
                    // A new default output: read, not a change (WindowsAudioService rebinds without raising).
                    _sink = sink;
                    _lastVolume = volume;
                    _lastMute = muted;
                }
                else if (double.IsNaN(_lastVolume)
                    || Math.Abs(volume - _lastVolume) >= NoiseThreshold
                    || muted != _lastMute)
                {
                    raise = !double.IsNaN(_lastVolume);
                    _lastVolume = volume;
                    _lastMute = muted;
                }
            }

            if (raise)
            {
                RaiseChanged();
            }

            return true;
        }

        private async Task WriteAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await _writeSignal.WaitAsync(ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return;
                }

                IReadOnlyList<uint>? volume;
                bool? mute;
                lock (_gate)
                {
                    volume = _pendingVolume;
                    mute = _pendingMute;
                    _pendingVolume = null;
                    _pendingMute = null;
                    _writing = volume is not null || mute is not null;
                }

                if (volume is null && mute is null)
                {
                    continue;
                }

                try
                {
                    if (volume is not null)
                    {
                        _ = await _pactl.RunAsync(
                            ["set-sink-volume", "@DEFAULT_SINK@", .. volume.Select(static v => v.ToString(System.Globalization.CultureInfo.InvariantCulture))],
                            ct).ConfigureAwait(false);
                    }

                    if (mute is { } m)
                    {
                        _ = await _pactl.RunAsync(["set-sink-mute", "@DEFAULT_SINK@", m ? "1" : "0"], ct).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[PulseAudio] write: {ex.Message}");
                }

                bool more;
                lock (_gate)
                {
                    _writing = false;
                    more = _pendingVolume is not null || _pendingMute is not null;
                }

                if (!more)
                {
                    try
                    {
                        // Reads skipped while writing are caught up here.
                        _ = await RefreshAsync(ct).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (ct.IsCancellationRequested)
                    {
                        return;
                    }
                }
            }
        }

        private void RaiseChanged()
        {
            try
            {
                Changed?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PulseAudio] handler: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Runs the real pactl, with no shell, as a child process. Every child is tracked and ended on <see cref="Dispose"/>,
    /// on a normal process exit, and on SIGTERM, SIGINT, SIGHUP and SIGQUIT: libpulse ignores SIGPIPE, so an orphaned
    /// <c>pactl subscribe</c> never notices its reader is gone and would run forever. .NET does not raise ProcessExit
    /// for SIGTERM (checked 2026-11-23), so the signals are registered here and then left to their default action.
    /// SIGKILL or a crash of the Host can still leave one behind.
    /// </summary>
    public sealed class ProcessPactl : IPactl, IDisposable
    {
        private static readonly TimeSpan CallTimeout = TimeSpan.FromSeconds(3);
        private readonly string _executable;
        private readonly ChildProcessSet _children = new();
        private readonly EventHandler _onProcessExit;
        private readonly List<PosixSignalRegistration> _signals = [];
        private int _disposed;

        public ProcessPactl()
            : this("pactl")
        {
        }

        /// <summary>For tests: a stand-in for pactl at <paramref name="executable"/>.</summary>
        internal ProcessPactl(string executable)
        {
            _executable = executable;
            _onProcessExit = (_, _) => _children.KillAll();
            AppDomain.CurrentDomain.ProcessExit += _onProcessExit;
            foreach (PosixSignal signal in (PosixSignal[])[PosixSignal.SIGTERM, PosixSignal.SIGINT, PosixSignal.SIGHUP, PosixSignal.SIGQUIT])
            {
                try
                {
                    // The context is not cancelled, so the signal's default action (exit) still follows.
                    _signals.Add(PosixSignalRegistration.Create(signal, _ => _children.KillAll()));
                }
                catch (PlatformNotSupportedException)
                {
                    // Not on this OS; Dispose and ProcessExit still cover a normal exit.
                }
            }
        }

        /// <summary>Ids of the pactl children still running, for diagnostics and tests.</summary>
        internal IReadOnlyList<int> ChildIds => _children.Ids;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }

            AppDomain.CurrentDomain.ProcessExit -= _onProcessExit;
            foreach (PosixSignalRegistration registration in _signals)
            {
                registration.Dispose();
            }

            _children.KillAll();
        }

        public async Task<string?> RunAsync(IReadOnlyList<string> args, CancellationToken cancellationToken)
        {
            using Process? process = Start(args);
            if (process is null)
            {
                return null;
            }

            using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(CallTimeout);
            try
            {
                string output = await process.StandardOutput.ReadToEndAsync(timeout.Token).ConfigureAwait(false);
                await process.WaitForExitAsync(timeout.Token).ConfigureAwait(false);
                return process.ExitCode == 0 ? output : null;
            }
            catch (OperationCanceledException)
            {
                Kill(process);
                cancellationToken.ThrowIfCancellationRequested();
                return null;
            }
            finally
            {
                _children.Remove(process);
            }
        }

        public async IAsyncEnumerable<string> SubscribeAsync(
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            using Process? process = Start(["subscribe"]);
            if (process is null)
            {
                yield break;
            }

            try
            {
                while (true)
                {
                    string? line = await process.StandardOutput.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                    if (line is null)
                    {
                        yield break;
                    }

                    yield return line;
                }
            }
            finally
            {
                Kill(process);
                _children.Remove(process);
            }
        }

        private Process? Start(IReadOnlyList<string> args)
        {
            if (Volatile.Read(ref _disposed) != 0)
            {
                return null;
            }

            ProcessStartInfo info = new(_executable)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            foreach (string arg in args)
            {
                info.ArgumentList.Add(arg);
            }

            try
            {
                Process? process = Process.Start(info);
                if (process is not null)
                {
                    _children.Add(process);
                    if (Volatile.Read(ref _disposed) != 0)
                    {
                        // Disposed while this one started: KillAll may already have run without it.
                        _children.KillAll();
                    }
                }

                return process;
            }
            catch (System.ComponentModel.Win32Exception)
            {
                // pactl is not installed.
                return null;
            }
        }

        private static void Kill(Process process)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill();
                }
            }
            catch (InvalidOperationException)
            {
                // Already gone.
            }
        }
    }
}
