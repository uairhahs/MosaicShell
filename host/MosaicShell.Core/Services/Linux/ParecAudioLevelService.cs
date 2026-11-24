using System.Diagnostics;

namespace MosaicShell.Core.Services.Linux
{
    /// <summary>A recorder's raw output, chunk by chunk; a seam so tests never touch real audio.</summary>
    public interface IAudioCapture
    {
        /// <summary>Runs the recorder with these arguments and yields its standard output until it exits or is cancelled.</summary>
        IAsyncEnumerable<byte[]> ReadAsync(IReadOnlyList<string> args, CancellationToken cancellationToken);
    }

    /// <summary>
    /// The Pulse tile's levels on Linux: parec records the default output's monitor (PulseAudio, or PipeWire's PulseAudio
    /// layer) as 32-bit float, the loopback Windows captures, and <see cref="AudioLevelMeter"/> turns it into the same
    /// peak and bands. parec runs only between Start and Stop.
    /// </summary>
    /// <remarks>Without parec or a server the levels stay at zero and nothing throws.</remarks>
    public sealed class ParecAudioLevelService : IAudioLevelService
    {
        /// <summary>The default output's monitor, as interleaved float32 stereo at 48 kHz with about 25 ms per chunk.</summary>
        public static readonly IReadOnlyList<string> Args =
            ["--device=@DEFAULT_MONITOR@", "--format=float32le", "--rate=48000", "--channels=2", "--latency-msec=25", "--raw"];

        private readonly IAudioCapture _capture;
        private readonly ProcessPactl? _ownedCapture;
        private readonly Lock _gate = new();
        private readonly float[] _bands = new float[AudioLevelMeter.BandCount];
        private CancellationTokenSource? _run;

        public ParecAudioLevelService(IAudioCapture? capture = null)
        {
            _ownedCapture = capture is null ? ProcessPactl.Parec() : null;
            _capture = capture ?? _ownedCapture!;
        }

        public double Peak
        {
            get
            {
                lock (_gate)
                {
                    return field;
                }
            }

            private set;
        }

        public IReadOnlyList<double> Bands
        {
            get
            {
                lock (_gate)
                {
                    return [.. _bands.Select(static b => (double)b)];
                }
            }
        }

        public void Start()
        {
            CancellationTokenSource run;
            lock (_gate)
            {
                if (_run is not null)
                {
                    return;
                }

                run = new CancellationTokenSource();
                _run = run;
            }

            _ = Task.Run(() => RunAsync(run.Token));
        }

        public void Stop()
        {
            CancellationTokenSource? run;
            lock (_gate)
            {
                run = _run;
                _run = null;
            }

            run?.Cancel();
            run?.Dispose();
        }

        public void Dispose()
        {
            Stop();
            _ownedCapture?.Dispose();
        }

        private async Task RunAsync(CancellationToken ct)
        {
            byte[] carry = [];
            float[] bands = new float[AudioLevelMeter.BandCount];
            try
            {
                await foreach (byte[] chunk in _capture.ReadAsync(Args, ct).ConfigureAwait(false))
                {
                    // A sample can straddle two reads; keep its first bytes for the next one.
                    byte[] data = carry.Length == 0 ? chunk : [.. carry, .. chunk];
                    int whole = data.Length / 4 * 4;
                    carry = data[whole..];
                    if (whole < 4)
                    {
                        continue;
                    }

                    AudioLevelMeter.ProcessIeeeFloat(data.AsSpan(0, whole), bands, out double peak);
                    lock (_gate)
                    {
                        Peak = peak;
                        bands.CopyTo(_bands);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Stopped.
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ParecLevels] {ex.Message}");
            }
        }
    }
}
