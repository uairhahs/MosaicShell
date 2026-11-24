using System.Threading.Channels;
using FluentAssertions;
using MosaicShell.Core.Services;
using MosaicShell.Core.Services.Linux;

namespace MosaicShell.Core.Tests.Linux
{
    /// <summary><see cref="ParecAudioLevelService"/> against a stand-in recorder that yields chunks of float32 samples.</summary>
    public sealed class ParecAudioLevelServiceTests
    {
        private static readonly TimeSpan Patience = TimeSpan.FromSeconds(5);

        private static async Task<bool> EventuallyAsync(Func<bool> condition)
        {
            DateTime until = DateTime.UtcNow + Patience;
            while (DateTime.UtcNow < until)
            {
                if (condition())
                {
                    return true;
                }

                await Task.Delay(20);
            }

            return condition();
        }

        private static byte[] Samples(float value, int count)
        {
            byte[] bytes = new byte[count * 4];
            for (int i = 0; i < count; i++)
            {
                BitConverter.TryWriteBytes(bytes.AsSpan(i * 4), value);
            }

            return bytes;
        }

        [Fact]
        public async Task Recorded_samples_give_the_same_levels_as_the_shared_meter()
        {
            FakeCapture capture = new();
            using ParecAudioLevelService levels = new(capture);
            byte[] chunk = Samples(0.1f, 960);
            Span<float> expected = stackalloc float[AudioLevelMeter.BandCount];
            AudioLevelMeter.ProcessIeeeFloat(chunk, expected, out double peak);
            float[] bands = expected.ToArray();

            levels.Start();
            _ = (await EventuallyAsync(() => capture.Running)).Should().BeTrue();
            capture.Yield(chunk);

            _ = (await EventuallyAsync(() => levels.Peak > 0)).Should().BeTrue();
            _ = levels.Peak.Should().Be(peak);
            _ = levels.Bands.Should().Equal(bands.Select(b => (double)b));
            _ = capture.Args.Should().Equal(ParecAudioLevelService.Args);
        }

        [Fact]
        public async Task A_sample_split_across_two_reads_is_joined()
        {
            FakeCapture capture = new();
            using ParecAudioLevelService levels = new(capture);
            byte[] chunk = Samples(0.25f, 8);
            levels.Start();
            _ = (await EventuallyAsync(() => capture.Running)).Should().BeTrue();

            capture.Yield(chunk[..6]);
            capture.Yield(chunk[6..]);

            _ = (await EventuallyAsync(() => levels.Peak > 0)).Should().BeTrue("both reads together hold whole samples");
        }

        [Fact]
        public async Task Stop_ends_the_recorder_and_nothing_runs_before_Start()
        {
            FakeCapture capture = new();
            using ParecAudioLevelService levels = new(capture);
            await Task.Delay(50);
            _ = capture.Starts.Should().Be(0);

            levels.Start();
            levels.Start();
            _ = (await EventuallyAsync(() => capture.Running)).Should().BeTrue();
            levels.Stop();

            _ = (await EventuallyAsync(() => !capture.Running)).Should().BeTrue();
            _ = capture.Starts.Should().Be(1, "a second Start while running starts nothing");
        }

        [Fact]
        public void Platform_defaults_on_linux_take_levels_from_parec()
        {
            if (!OperatingSystem.IsLinux())
            {
                return;
            }

            using HostServices hub = HostServices.CreatePlatformDefaults();

            _ = hub.AudioLevels.Should().BeOfType<ParecAudioLevelService>();
        }
    }

    internal sealed class FakeCapture : IAudioCapture
    {
        private Channel<byte[]>? _chunks;
        private int _starts;
        private volatile bool _running;

        public bool Running => _running;
        public int Starts => Volatile.Read(ref _starts);
        public IReadOnlyList<string> Args { get; private set; } = [];

        public void Yield(byte[] chunk)
        {
            _ = _chunks!.Writer.TryWrite(chunk);
        }

        public async IAsyncEnumerable<byte[]> ReadAsync(
            IReadOnlyList<string> args, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            Args = args;
            _ = Interlocked.Increment(ref _starts);
            _chunks = Channel.CreateUnbounded<byte[]>();
            _running = true;
            try
            {
                await foreach (byte[] chunk in _chunks.Reader.ReadAllAsync(cancellationToken))
                {
                    yield return chunk;
                }
            }
            finally
            {
                _running = false;
            }
        }
    }
}
