using System.Threading.Channels;
using FluentAssertions;
using MosaicShell.Core.Services.Linux;
using MosaicShell.Core.Services.Pulse;

namespace MosaicShell.Core.Tests.Linux
{
    /// <summary>
    /// <see cref="PulseAudioService"/> against a stand-in pactl, so no real audio is touched. Change rules follow
    /// WindowsAudioService: external volume or mute changes raise Changed; the service's own volume write does not; its
    /// own mute write does; a new default output is read without raising Changed; sub-0.4% noise is ignored.
    /// </summary>
    public sealed class PulseAudioServiceTests : IDisposable
    {
        private static readonly TimeSpan Patience = TimeSpan.FromSeconds(5);
        private readonly FakePactl _pactl = new();
        private PulseAudioService? _service;
        private int _changes;

        public void Dispose()
        {
            _service?.Dispose();
        }

        private PulseAudioService Start()
        {
            _service = new PulseAudioService(_pactl, resubscribeDelay: TimeSpan.FromMilliseconds(20));
            _service.Changed += (_, _) => Interlocked.Increment(ref _changes);
            return _service;
        }

        private static async Task<bool> EventuallyAsync(Func<bool> condition)
        {
            DateTime until = DateTime.UtcNow + Patience;
            while (DateTime.UtcNow < until)
            {
                if (condition())
                {
                    return true;
                }

                await Task.Delay(10);
            }

            return condition();
        }

        private int Changes => Volatile.Read(ref _changes);

        [Fact]
        public async Task Reads_the_default_output_at_start()
        {
            _pactl.Set("sink-a", [new("front-left", 36700), new("front-right", 36700)], muted: true);

            PulseAudioService audio = Start();

            _ = (await EventuallyAsync(() => audio.IsMuted)).Should().BeTrue();
            _ = audio.MasterVolume.Should().BeApproximately(0.56, 0.001);
        }

        [Fact]
        public async Task An_external_change_raises_Changed_with_the_new_value()
        {
            _pactl.Set("sink-a", [new("mono", 32768)]);
            PulseAudioService audio = Start();
            _ = (await EventuallyAsync(() => _pactl.Subscribed)).Should().BeTrue();
            _ = (await EventuallyAsync(() => audio.MasterVolume > 0.49)).Should().BeTrue();

            _pactl.Set("sink-a", [new("mono", 13107)]);
            _pactl.Emit("Event 'change' on sink #78");

            _ = (await EventuallyAsync(() => Changes == 1)).Should().BeTrue();
            _ = audio.MasterVolume.Should().BeApproximately(0.2, 0.001);
        }

        [Fact]
        public async Task Events_that_cannot_change_the_output_do_not_read_it_again()
        {
            _pactl.Set("sink-a", [new("mono", 32768)]);
            _ = Start();
            _ = (await EventuallyAsync(() => _pactl.Subscribed)).Should().BeTrue();
            int reads = _pactl.VolumeReads;

            _pactl.Emit("Event 'change' on client #2410");
            _pactl.Emit("Event 'change' on card #52");
            _pactl.Emit("Event 'change' on sink #78");

            _ = (await EventuallyAsync(() => _pactl.VolumeReads == reads + 1)).Should().BeTrue();
            await Task.Delay(100);
            _ = _pactl.VolumeReads.Should().Be(reads + 1);
        }

        [Fact]
        public async Task Setting_the_volume_keeps_the_balance_and_does_not_echo_as_a_change()
        {
            _pactl.Set("sink-a", [new("front-left", 32768), new("front-right", 16384)]);
            PulseAudioService audio = Start();
            _ = (await EventuallyAsync(() => audio.MasterVolume > 0.49)).Should().BeTrue();

            audio.MasterVolume = 0.25;

            _ = audio.MasterVolume.Should().Be(0.25, "the slider must not jump back while the write is in flight");
            _ = (await EventuallyAsync(() => _pactl.Writes.Contains("set-sink-volume @DEFAULT_SINK@ 16384 8192"))).Should().BeTrue();
            _pactl.Emit("Event 'change' on sink #78");
            await Task.Delay(150);
            _ = Changes.Should().Be(0, "Windows pre-arms its own write so the echo is not an external change");
        }

        [Fact]
        public async Task Rapid_writes_coalesce_and_the_last_one_wins()
        {
            _pactl.Set("sink-a", [new("mono", 32768)]);
            _pactl.WriteDelay = TimeSpan.FromMilliseconds(30);
            PulseAudioService audio = Start();
            _ = (await EventuallyAsync(() => audio.MasterVolume > 0.49)).Should().BeTrue();

            for (int percent = 10; percent <= 40; percent++)
            {
                audio.MasterVolume = percent / 100.0;
            }

            _ = (await EventuallyAsync(() => _pactl.Writes.LastOrDefault() == "set-sink-volume @DEFAULT_SINK@ 26214")).Should().BeTrue();
            _ = _pactl.Writes.Count.Should().BeLessThan(31 / 2);
        }

        [Fact]
        public async Task Setting_mute_is_written_and_raises_Changed_as_on_windows()
        {
            _pactl.Set("sink-a", [new("mono", 32768)]);
            PulseAudioService audio = Start();
            _ = (await EventuallyAsync(() => _pactl.Subscribed)).Should().BeTrue();

            audio.IsMuted = true;

            _ = audio.IsMuted.Should().BeTrue();
            _ = (await EventuallyAsync(() => _pactl.Writes.Contains("set-sink-mute @DEFAULT_SINK@ 1"))).Should().BeTrue();
            _pactl.Emit("Event 'change' on sink #78");
            _ = (await EventuallyAsync(() => Changes == 1)).Should().BeTrue();
        }

        [Fact]
        public async Task A_new_default_output_is_read_without_raising_Changed()
        {
            _pactl.Set("sink-a", [new("mono", 32768)]);
            PulseAudioService audio = Start();
            _ = (await EventuallyAsync(() => _pactl.Subscribed)).Should().BeTrue();

            _pactl.Set("sink-b", [new("mono", 6554)]);
            _pactl.Emit("Event 'change' on server #-1");

            _ = (await EventuallyAsync(() => Math.Abs(audio.MasterVolume - 0.1) < 0.001)).Should().BeTrue();
            await Task.Delay(100);
            _ = Changes.Should().Be(0);
        }

        [Fact]
        public async Task Sub_percent_noise_is_not_a_change()
        {
            _pactl.Set("sink-a", [new("mono", 32768)]);
            _ = Start();
            _ = (await EventuallyAsync(() => _pactl.Subscribed)).Should().BeTrue();

            _pactl.Set("sink-a", [new("mono", 32900)]);
            _pactl.Emit("Event 'change' on sink #78");
            await Task.Delay(150);

            _ = Changes.Should().Be(0);
        }

        [Fact]
        public async Task A_subscription_that_ends_is_started_again()
        {
            _pactl.Set("sink-a", [new("mono", 32768)]);
            _ = Start();
            _ = (await EventuallyAsync(() => _pactl.Subscriptions == 1)).Should().BeTrue();

            _pactl.EndSubscription();

            _ = (await EventuallyAsync(() => _pactl.Subscriptions == 2)).Should().BeTrue();
        }

        [Fact]
        public async Task Without_pactl_or_a_server_nothing_throws_and_the_volume_reads_zero()
        {
            _pactl.Unavailable = true;

            PulseAudioService audio = Start();
            audio.MasterVolume = 0.5;
            audio.IsMuted = true;
            await Task.Delay(150);

            _ = audio.MasterVolume.Should().Be(0);
            _ = audio.IsMuted.Should().BeFalse();
            _ = Changes.Should().Be(0);
        }
    }

    /// <summary>The real pactl, read-only: skipped where it or a sound server is missing (CI without audio).</summary>
    public sealed class ProcessPactlTests
    {
        [Fact]
        public async Task Reads_the_real_default_output_as_json()
        {
            ProcessPactl pactl = new();
            string? sink = OperatingSystem.IsLinux() ? await pactl.RunAsync(["get-default-sink"], CancellationToken.None) : null;
            if (string.IsNullOrWhiteSpace(sink))
            {
                return;
            }

            // The read PulseAudioService makes. `get-sink-volume`/`get-sink-mute` are not JSON on every pactl 17.0.
            string? list = await pactl.RunAsync(["-f", "json", "list", "sinks"], CancellationToken.None);

            _ = PulseVolumePolicy.TryParseSink(list, sink.Trim(), out IReadOnlyList<PulseChannel> channels, out _).Should().BeTrue(list);
            _ = channels.Should().NotBeEmpty();
        }

        [Fact]
        public async Task A_failing_call_reads_as_null()
        {
            _ = (await new ProcessPactl().RunAsync(["no-such-command"], CancellationToken.None)).Should().BeNull();
        }
    }

    /// <summary>A pactl that answers from fields, records writes, and streams subscribe lines the test emits.</summary>
    internal sealed class FakePactl : IPactl
    {
        private readonly Lock _gate = new();
        private readonly List<string> _writes = [];
        private Channel<string>? _events;
        private string _sink = "";
        private PulseChannel[] _channels = [];
        private bool _muted;
        private int _volumeReads;
        private int _subscriptions;

        public bool Unavailable { get; set; }
        public TimeSpan WriteDelay { get; set; }
        public bool Subscribed => Volatile.Read(ref _subscriptions) > 0;
        public int Subscriptions => Volatile.Read(ref _subscriptions);
        public int VolumeReads => Volatile.Read(ref _volumeReads);

        public IReadOnlyList<string> Writes
        {
            get
            {
                lock (_gate)
                {
                    return [.. _writes];
                }
            }
        }

        public void Set(string sink, PulseChannel[] channels, bool muted = false)
        {
            lock (_gate)
            {
                _sink = sink;
                _channels = channels;
                _muted = muted;
            }
        }

        public void Emit(string line)
        {
            _ = _events?.Writer.TryWrite(line);
        }

        public void EndSubscription()
        {
            _ = _events?.Writer.TryComplete();
        }

        public async Task<string?> RunAsync(IReadOnlyList<string> args, CancellationToken cancellationToken)
        {
            if (Unavailable)
            {
                return null;
            }

            string command = string.Join(' ', args.Where(static a => a is not ("-f" or "json")));
            if (command.StartsWith("set-", StringComparison.Ordinal))
            {
                await Task.Delay(WriteDelay, cancellationToken);
                lock (_gate)
                {
                    _writes.Add(command);
                    string[] parts = command.Split(' ');
                    if (parts[0] == "set-sink-volume")
                    {
                        _channels = [.. _channels.Select((c, i) => c with { Value = uint.Parse(parts[2 + i], System.Globalization.CultureInfo.InvariantCulture) })];
                    }
                    else if (parts[0] == "set-sink-mute")
                    {
                        _muted = parts[2] == "1";
                    }
                }

                return "";
            }

            lock (_gate)
            {
                // As pactl 17.0 on NixOS answers (2026-11-23): JSON for `list sinks`, but text for `get-sink-*` even
                // with `-f json`, which the service once relied on and so read nothing.
                return command switch
                {
                    "get-default-sink" => _sink + "\n",
                    "list sinks" => ListSinksJson(),
                    "get-sink-volume @DEFAULT_SINK@" => "Volume: " + string.Join(",   ", _channels.Select(static c => $"{c.Name}: {c.Value} /  50% / -18.06 dB")) + "\n        balance 0.00\n",
                    "get-sink-mute @DEFAULT_SINK@" => _muted ? "Mute: yes\n" : "Mute: no\n",
                    _ => null,
                };
            }
        }

        private string ListSinksJson()
        {
            _ = Interlocked.Increment(ref _volumeReads);
            // A second, unrelated sink first, so reading the wrong entry shows up.
            string decoy = "{\"index\":1,\"name\":\"decoy.sink\",\"mute\":true,\"volume\":{\"mono\":{\"value\":65536}},\"balance\":0}";
            string sink = "{\"index\":2,\"name\":\"" + _sink + "\",\"mute\":" + (_muted ? "true" : "false") + ",\"volume\":{"
                + string.Join(',', _channels.Select(static c => $"\"{c.Name}\":{{\"value\":{c.Value}}}")) + "},\"balance\":0}";
            return "[" + decoy + "," + sink + "]";
        }

        public async IAsyncEnumerable<string> SubscribeAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            if (Unavailable)
            {
                yield break;
            }

            Channel<string> events = Channel.CreateUnbounded<string>();
            _events = events;
            _ = Interlocked.Increment(ref _subscriptions);
            await foreach (string line in events.Reader.ReadAllAsync(cancellationToken))
            {
                yield return line;
            }
        }
    }
}
