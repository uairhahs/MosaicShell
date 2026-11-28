using System.Diagnostics;
using FluentAssertions;
using MosaicShell.Core.Services;
using MosaicShell.Core.Services.Kde;
using MosaicShell.Core.Services.Linux;
using Tmds.DBus.Protocol;

namespace MosaicShell.Core.Tests.Linux
{
    /// <summary>
    /// <see cref="KWinFullscreenProbe"/> on a private bus against a stand-in KWin whose scripting reports a chosen active
    /// window when the probe script starts, as the real script does.
    /// </summary>
    public sealed class KWinFullscreenProbeTests : IAsyncLifetime
    {
        private const string Fullscreen = "full=1;desktop=0;frame=0,0,1920,1080;screen=0,0,1920,1080";
        private const string Windowed = "full=0;desktop=0;frame=100,100,800,600;screen=0,0,1920,1080";

        private PrivateDBus? _dbus;

        public Task InitializeAsync()
        {
            if (new LinuxBusFactAttribute().Skip is null)
            {
                _dbus = PrivateDBus.Start();
            }

            return Task.CompletedTask;
        }

        public Task DisposeAsync()
        {
            _dbus?.Dispose();
            return Task.CompletedTask;
        }

        [LinuxBusFact]
        public async Task A_fullscreen_active_window_reads_as_fullscreen()
        {
            using FakeKWinReporter kwin = await FakeKWinReporter.StartAsync(_dbus!.Address, Fullscreen);
            using KWinFullscreenProbe probe = new(_dbus.Address);

            _ = probe.IsForegroundFullscreen.Should().BeTrue();
        }

        [LinuxBusFact]
        public async Task A_windowed_active_window_does_not()
        {
            using FakeKWinReporter kwin = await FakeKWinReporter.StartAsync(_dbus!.Address, Windowed);
            using KWinFullscreenProbe probe = new(_dbus.Address);

            _ = probe.IsForegroundFullscreen.Should().BeFalse();
        }

        [LinuxBusFact]
        public async Task Each_query_loads_the_script_runs_it_and_unloads_it()
        {
            using FakeKWinReporter kwin = await FakeKWinReporter.StartAsync(_dbus!.Address, Fullscreen);
            using KWinFullscreenProbe probe = new(_dbus.Address);

            _ = probe.IsForegroundFullscreen;
            _ = probe.IsForegroundFullscreen;

            string load = "loadScript " + KWinFullscreenPolicy.ScriptName;
            string unload = "unloadScript " + KWinFullscreenPolicy.ScriptName;
            _ = kwin.Calls().Should().Equal(unload, load, "start", unload, unload, load, "start", unload);
            _ = kwin.LoadedScript.Should().Be(KWinFullscreenPolicy.Script);
        }

        [LinuxBusFact]
        public async Task A_kwin_that_never_reports_reads_as_not_fullscreen_after_a_short_wait()
        {
            using FakeKWinReporter kwin = await FakeKWinReporter.StartAsync(_dbus!.Address, report: null);
            using KWinFullscreenProbe probe = new(_dbus.Address);
            Stopwatch watch = Stopwatch.StartNew();

            _ = probe.IsForegroundFullscreen.Should().BeFalse();

            _ = watch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(3));
        }

        [LinuxBusFact]
        public void Without_kwin_it_reads_as_not_fullscreen_without_throwing()
        {
            using KWinFullscreenProbe probe = new(_dbus!.Address);

            _ = probe.IsForegroundFullscreen.Should().BeFalse();
        }

        [Fact]
        public void Without_a_bus_it_reads_as_not_fullscreen_without_throwing()
        {
            using KWinFullscreenProbe probe = new(address: "");

            _ = probe.IsForegroundFullscreen.Should().BeFalse();
        }

        [Fact]
        public void Platform_defaults_on_linux_ask_kwin()
        {
            if (!OperatingSystem.IsLinux())
            {
                return;
            }

            using HostServices hub = HostServices.CreatePlatformDefaults();

            // KWin's probe on Plasma only (LinuxDesktopRoutesTests); this checks the defaults take the desktop's route,
            // whatever session the tests run in (CI's runner has none).
            bool plasma = Services.Xdg.XdgCurrentDesktop.IsPlasma(Services.Xdg.XdgCurrentDesktop.Current);
            _ = hub.Fullscreen.Should().BeOfType(plasma ? typeof(KWinFullscreenProbe) : typeof(NullFullscreenProbe));
        }
    }

    /// <summary>
    /// KWin's /Scripting for the probe: records loadScript, unloadScript and start, and on start calls the probe's
    /// Report with its configured facts (or never, when there are none).
    /// </summary>
    internal sealed class FakeKWinReporter : IPathMethodHandler, IDisposable
    {
        private readonly DBusConnection _connection;
        private readonly string? _report;
        private readonly List<string> _calls = [];
#pragma warning disable IDE0032 // LoadedScript reads under the lock the D-Bus handler writes under; an auto property would not.
        private string? _loadedScript;
#pragma warning restore IDE0032

        private FakeKWinReporter(DBusConnection connection, string? report)
        {
            _connection = connection;
            _report = report;
        }

        public string Path => KWinOsdScriptPolicy.ScriptingPath;
        public bool HandlesChildPaths => false;

        public string? LoadedScript
        {
            get
            {
                lock (_calls)
                {
                    return _loadedScript;
                }
            }
        }

        public static async Task<FakeKWinReporter> StartAsync(string address, string? report)
        {
            DBusConnection connection = new(address);
            await connection.ConnectAsync();
            FakeKWinReporter kwin = new(connection, report);
            connection.AddMethodHandler(kwin);
            await connection.RequestNameAsync(KWinOsdScriptPolicy.KWinBusName);
            return kwin;
        }

        public string[] Calls()
        {
            lock (_calls)
            {
                return [.. _calls];
            }
        }

        public ValueTask HandleMethodAsync(MethodContext context)
        {
            string member = context.Request.MemberAsString ?? "";
            Reader reader = context.Request.GetBodyReader();
            using MessageWriter w = context.CreateReplyWriter(member switch
            {
                "loadScript" => "i",
                "unloadScript" => "b",
                _ => null,
            });
            lock (_calls)
            {
                switch (member)
                {
                    case "loadScript":
                        string file = reader.ReadString();
                        _calls.Add("loadScript " + reader.ReadString());
                        _loadedScript = File.ReadAllText(file);
                        w.WriteInt32(0);
                        break;
                    case "unloadScript":
                        _calls.Add("unloadScript " + reader.ReadString());
                        w.WriteBool(true);
                        break;
                    default:
                        _calls.Add(member);
                        break;
                }
            }

            context.Reply(w.CreateMessage());
            if (member == "start" && _report is not null)
            {
                _ = ReportAsync(_report);
            }

            return ValueTask.CompletedTask;
        }

        private Task ReportAsync(string report)
        {
            MessageBuffer message;
            using (MessageWriter w = _connection.GetMessageWriter())
            {
                w.WriteMethodCallHeader(
                    KWinFullscreenPolicy.ProbeBusName, KWinFullscreenPolicy.ProbePath, KWinFullscreenPolicy.ProbeInterface,
                    KWinFullscreenPolicy.ReportMethod, "s");
                w.WriteString(report);
                message = w.CreateMessage();
            }

            return _connection.CallMethodAsync(message);
        }

        public void Dispose()
        {
            _connection.Dispose();
        }
    }
}
