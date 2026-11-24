using System.Threading.Channels;
using FluentAssertions;
using MosaicShell.Core.Services;
using MosaicShell.Core.Services.Linux;

namespace MosaicShell.Core.Tests.Linux
{
    /// <summary><see cref="PulseAppAudioService"/> against a stand-in pactl that serves sink-inputs and records writes.</summary>
    public sealed class PulseAppAudioServiceTests
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

        [Fact]
        public async Task Nothing_runs_until_the_sessions_are_asked_for()
        {
            FakeStreamsPactl pactl = new();
            using PulseAppAudioService apps = new(pactl);
            await Task.Delay(100);

            _ = pactl.Calls.Should().BeEmpty("a Host or Worker with no app audio tile runs no pactl");
        }

        [Fact]
        public async Task The_first_read_lists_each_app_and_raises_SessionsChanged()
        {
            FakeStreamsPactl pactl = new();
            pactl.Add(367, "Microsoft Edge", "msedge", 65536);
            pactl.Add(371, "Microsoft Edge", "msedge", 32768);
            using PulseAppAudioService apps = new(pactl);
            int raised = 0;
            apps.SessionsChanged += (_, _) => Interlocked.Increment(ref raised);

            _ = apps.GetSessions();

            _ = (await EventuallyAsync(() => Volatile.Read(ref raised) == 1)).Should().BeTrue();
            _ = apps.GetSessions().Should().Equal(new AppAudioSession("app:msedge", "Microsoft Edge", 1.0, false));
        }

        [Fact]
        public async Task A_new_stream_is_read_and_raised_but_an_unrelated_event_is_not()
        {
            FakeStreamsPactl pactl = new();
            using PulseAppAudioService apps = new(pactl);
            int raised = 0;
            apps.SessionsChanged += (_, _) => Interlocked.Increment(ref raised);
            _ = apps.GetSessions();
            _ = (await EventuallyAsync(() => pactl.Subscribed)).Should().BeTrue();
            int reads = pactl.Reads;

            pactl.Emit("Event 'change' on sink #51");
            pactl.Add(380, "Spotify", "spotify", 65536);
            pactl.Emit("Event 'new' on sink-input #380");

            _ = (await EventuallyAsync(() => apps.GetSessions().Count == 1)).Should().BeTrue();
            _ = pactl.Reads.Should().Be(reads + 1, "only the stream event needs a re-read");
            _ = Volatile.Read(ref raised).Should().Be(2);
        }

        [Fact]
        public async Task A_volume_change_moves_at_once_and_reaches_every_stream_of_the_app()
        {
            FakeStreamsPactl pactl = new();
            pactl.Add(367, "Microsoft Edge", "msedge", 65536);
            pactl.Add(371, "Microsoft Edge", "msedge", 65536);
            using PulseAppAudioService apps = new(pactl);
            _ = apps.GetSessions();
            _ = (await EventuallyAsync(() => apps.GetSessions().Count == 1)).Should().BeTrue();

            apps.SetVolume("app:msedge", 0.5);

            _ = apps.GetSessions()[0].Volume.Should().Be(0.5, "the slider must not jump back while the write runs");
            _ = (await EventuallyAsync(() => pactl.Writes.Count == 2)).Should().BeTrue();
            _ = pactl.Writes.Should().Equal("set-sink-input-volume 367 32768 32768", "set-sink-input-volume 371 32768 32768");
        }

        [Fact]
        public async Task A_drag_writes_only_the_latest_volume_once_the_running_write_is_done()
        {
            FakeStreamsPactl pactl = new() { WriteDelay = TimeSpan.FromMilliseconds(150) };
            pactl.Add(367, "mpv", "mpv", 65536);
            using PulseAppAudioService apps = new(pactl);
            _ = apps.GetSessions();
            _ = (await EventuallyAsync(() => apps.GetSessions().Count == 1)).Should().BeTrue();

            apps.SetVolume("app:mpv", 0.9);
            await Task.Delay(30);
            apps.SetVolume("app:mpv", 0.7);
            apps.SetVolume("app:mpv", 0.5);

            _ = (await EventuallyAsync(() => pactl.Writes.Count == 2)).Should().BeTrue();
            await Task.Delay(200);
            _ = pactl.Writes.Should().Equal("set-sink-input-volume 367 58982 58982", "set-sink-input-volume 367 32768 32768");
        }

        [Fact]
        public async Task Mute_reaches_every_stream()
        {
            FakeStreamsPactl pactl = new();
            pactl.Add(367, "Microsoft Edge", "msedge", 65536);
            pactl.Add(371, "Microsoft Edge", "msedge", 65536);
            using PulseAppAudioService apps = new(pactl);
            _ = apps.GetSessions();
            _ = (await EventuallyAsync(() => apps.GetSessions().Count == 1)).Should().BeTrue();

            apps.SetMuted("app:msedge", true);

            _ = apps.GetSessions()[0].IsMuted.Should().BeTrue();
            _ = (await EventuallyAsync(() => pactl.Writes.Count == 2)).Should().BeTrue();
            _ = pactl.Writes.Should().Equal("set-sink-input-mute 367 1", "set-sink-input-mute 371 1");
        }

        [Fact]
        public async Task Without_pactl_there_are_no_sessions_and_nothing_throws()
        {
            FakeStreamsPactl pactl = new() { Unavailable = true };
            using PulseAppAudioService apps = new(pactl, TimeSpan.FromMilliseconds(10));

            _ = apps.GetSessions().Should().BeEmpty();
            apps.SetVolume("app:none", 0.5);
            await Task.Delay(100);

            _ = apps.GetSessions().Should().BeEmpty();
        }

        [Fact]
        public async Task The_outputs_are_listed_with_the_default_marked()
        {
            FakeStreamsPactl pactl = new();
            pactl.AddSink("alsa_output.hdmi", "HDMI");
            pactl.AddSink("bluez_output.1", "Headphones");
            pactl.DefaultSink = "alsa_output.hdmi";
            using PulseAppAudioService audio = new(pactl);

            _ = audio.GetOutputDevices();

            _ = (await EventuallyAsync(() => audio.GetOutputDevices().Count == 2)).Should().BeTrue();
            _ = audio.GetOutputDevices().Should().Equal(
                new AudioOutputDevice("alsa_output.hdmi", "HDMI", true),
                new AudioOutputDevice("bluez_output.1", "Headphones", false));
        }

        [Fact]
        public async Task Setting_the_default_moves_at_once_and_reaches_pactl()
        {
            FakeStreamsPactl pactl = new();
            pactl.AddSink("alsa_output.hdmi", "HDMI");
            pactl.AddSink("bluez_output.1", "Headphones");
            pactl.DefaultSink = "alsa_output.hdmi";
            using PulseAppAudioService audio = new(pactl);
            _ = audio.GetOutputDevices();
            _ = (await EventuallyAsync(() => audio.GetOutputDevices().Count == 2)).Should().BeTrue();

            audio.SetDefaultOutput("bluez_output.1");
            audio.SetDefaultOutput("unknown");

            _ = audio.GetOutputDevices().Single(d => d.IsDefault).Id.Should().Be("bluez_output.1");
            _ = (await EventuallyAsync(() => pactl.Writes.Count == 1)).Should().BeTrue();
            await Task.Delay(100);
            _ = pactl.Writes.Should().Equal("set-default-sink bluez_output.1");
        }

        [Fact]
        public async Task A_new_default_is_read_from_a_server_event()
        {
            FakeStreamsPactl pactl = new();
            pactl.AddSink("alsa_output.hdmi", "HDMI");
            pactl.AddSink("bluez_output.1", "Headphones");
            pactl.DefaultSink = "alsa_output.hdmi";
            using PulseAppAudioService audio = new(pactl);
            _ = audio.GetOutputDevices();
            _ = (await EventuallyAsync(() => pactl.Subscribed)).Should().BeTrue();

            pactl.DefaultSink = "bluez_output.1";
            pactl.Emit("Event 'change' on server #4294967295");

            _ = (await EventuallyAsync(() => audio.GetOutputDevices().Any(d => d.IsDefault && d.Id == "bluez_output.1"))).Should().BeTrue();
        }

        [Fact]
        public void Platform_defaults_on_linux_take_app_audio_and_outputs_from_one_pactl_service()
        {
            if (!OperatingSystem.IsLinux())
            {
                return;
            }

            using HostServices hub = HostServices.CreatePlatformDefaults();

            _ = hub.AppAudio.Should().BeOfType<PulseAppAudioService>();
            _ = hub.AudioDevices.Should().BeSameAs(hub.AppAudio, "one pactl subscription serves both");
        }
    }

    /// <summary>pactl as far as sink inputs go: list, set volume and mute, and subscribe.</summary>
    internal sealed class FakeStreamsPactl : IPactl
    {
        private readonly Lock _gate = new();
        private readonly List<(uint Index, string Name, string Binary, uint Value, bool Muted)> _streams = [];
        private readonly List<(string Name, string Description)> _sinks = [];
        private readonly List<string> _writes = [];
        private readonly List<string> _calls = [];
        private Channel<string>? _events;
        private int _reads;

        public bool Unavailable { get; set; }
        public string DefaultSink { get; set; } = "";
        public TimeSpan WriteDelay { get; set; }
        public bool Subscribed => _events is not null;
        public int Reads => Volatile.Read(ref _reads);

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

        public IReadOnlyList<string> Calls
        {
            get
            {
                lock (_gate)
                {
                    return [.. _calls];
                }
            }
        }

        public void Add(uint index, string name, string binary, uint value)
        {
            lock (_gate)
            {
                _streams.Add((index, name, binary, value, false));
            }
        }

        public void AddSink(string name, string description)
        {
            lock (_gate)
            {
                _sinks.Add((name, description));
            }
        }

        public void Emit(string line)
        {
            _ = _events?.Writer.TryWrite(line);
        }

        public async Task<string?> RunAsync(IReadOnlyList<string> args, CancellationToken cancellationToken)
        {
            string command = string.Join(' ', args);
            lock (_gate)
            {
                _calls.Add(command);
            }

            if (Unavailable)
            {
                return null;
            }

            if (command.StartsWith("set-", StringComparison.Ordinal))
            {
                await Task.Delay(WriteDelay, cancellationToken);
                lock (_gate)
                {
                    _writes.Add(command);
                }

                return "";
            }

            if (command == "get-default-sink")
            {
                return DefaultSink + "\n";
            }

            if (command == "-f json list sinks")
            {
                lock (_gate)
                {
                    return "[" + string.Join(",", _sinks.Select(k => $"{{\"name\": \"{k.Name}\", \"description\": \"{k.Description}\"}}")) + "]";
                }
            }

            if (command == "-f json list sink-inputs")
            {
                _ = Interlocked.Increment(ref _reads);
                lock (_gate)
                {
                    return "[" + string.Join(",", _streams.Select(s =>
                        $"{{\"index\": {s.Index}, \"mute\": {(s.Muted ? "true" : "false")}, \"volume\": {{\"front-left\": {{\"value\": {s.Value}}}, "
                        + $"\"front-right\": {{\"value\": {s.Value}}}}}, \"properties\": {{\"application.name\": \"{s.Name}\", "
                        + $"\"application.process.binary\": \"{s.Binary}\"}}}}")) + "]";
                }
            }

            return null;
        }

        public async IAsyncEnumerable<string> SubscribeAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            if (Unavailable)
            {
                yield break;
            }

            Channel<string> events = Channel.CreateUnbounded<string>();
            _events = events;
            await foreach (string line in events.Reader.ReadAllAsync(cancellationToken))
            {
                yield return line;
            }
        }
    }
}
