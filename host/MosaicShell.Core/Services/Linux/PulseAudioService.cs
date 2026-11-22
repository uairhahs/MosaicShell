using System.Diagnostics;
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
            _pactl = pactl ?? new ProcessPactl();
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
            string? sinkOut = await _pactl.RunAsync(["get-default-sink"], ct).ConfigureAwait(false);
            string? volumeOut = await _pactl.RunAsync(["-f", "json", "get-sink-volume", "@DEFAULT_SINK@"], ct).ConfigureAwait(false);
            string? muteOut = await _pactl.RunAsync(["-f", "json", "get-sink-mute", "@DEFAULT_SINK@"], ct).ConfigureAwait(false);
            if (!PulseVolumePolicy.TryParseVolume(volumeOut, out IReadOnlyList<PulseChannel> channels)
                || !PulseVolumePolicy.TryParseMute(muteOut, out bool muted))
            {
                return false;
            }

            string sink = sinkOut?.Trim() ?? "";
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

    /// <summary>Runs the real pactl, with no shell, as a child process.</summary>
    public sealed class ProcessPactl : IPactl
    {
        private static readonly TimeSpan CallTimeout = TimeSpan.FromSeconds(3);

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
            }
        }

        private static Process? Start(IReadOnlyList<string> args)
        {
            ProcessStartInfo info = new("pactl")
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
                return Process.Start(info);
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
