using FluentAssertions;
using MosaicShell.Core.Services.Linux;
using Tmds.DBus.Protocol;

namespace MosaicShell.Core.Tests.Linux
{
    /// <summary>
    /// <see cref="KWinBlurProbe"/> against a real dbus-daemon: it asks KWin, as Plasma answers on the session bus,
    /// whether the blur effect is loaded, and treats every failure as "no blur" so the flyouts keep the frost.
    /// </summary>
    public sealed class KWinBlurProbeTests
    {
        [LinuxBusFact]
        public async Task Blur_is_reported_when_kwin_has_the_effect_loaded()
        {
            using PrivateDBus dbus = PrivateDBus.Start();
            using FakeKWinEffects kwin = await FakeKWinEffects.StartAsync(dbus.Address, ["blur", "slide"]);

            _ = (await KWinBlurProbe.IsBlurLoadedAsync(dbus.Address)).Should().BeTrue();
            _ = kwin.Asked.Should().Equal("blur");
        }

        [LinuxBusFact]
        public async Task No_blur_when_the_effect_is_not_loaded()
        {
            using PrivateDBus dbus = PrivateDBus.Start();
            using FakeKWinEffects kwin = await FakeKWinEffects.StartAsync(dbus.Address, ["slide"]);

            _ = (await KWinBlurProbe.IsBlurLoadedAsync(dbus.Address)).Should().BeFalse();
        }

        [LinuxBusFact]
        public async Task No_blur_without_kwin_on_the_bus()
        {
            // GNOME, or Plasma with KWin restarting.
            using PrivateDBus dbus = PrivateDBus.Start();

            _ = (await KWinBlurProbe.IsBlurLoadedAsync(dbus.Address)).Should().BeFalse();
        }

        [LinuxBusFact]
        public async Task No_blur_without_a_bus()
        {
            string missing = "unix:path=" + Path.Combine(Path.GetTempPath(), "mosaic-no-bus-" + Guid.NewGuid().ToString("N"));

            _ = (await KWinBlurProbe.IsBlurLoadedAsync(missing)).Should().BeFalse();
            _ = (await KWinBlurProbe.IsBlurLoadedAsync("")).Should().BeFalse();
        }

        [LinuxBusFact]
        public async Task A_kwin_that_never_answers_times_out_as_no_blur()
        {
            using PrivateDBus dbus = PrivateDBus.Start();
            using FakeKWinEffects kwin = await FakeKWinEffects.StartAsync(dbus.Address, ["blur"], answer: false);

            DateTime started = DateTime.UtcNow;
            bool loaded = await KWinBlurProbe.IsBlurLoadedAsync(dbus.Address, TimeSpan.FromMilliseconds(300));

            _ = loaded.Should().BeFalse();
            _ = (DateTime.UtcNow - started).Should().BeLessThan(TimeSpan.FromSeconds(3));
        }
    }

    /// <summary>KWin's <c>/Effects</c> object, answering <c>isEffectLoaded</c> from a fixed list.</summary>
    internal sealed class FakeKWinEffects : IPathMethodHandler, IDisposable
    {
        private readonly DBusConnection _connection;
        private readonly HashSet<string> _loaded;
        private readonly bool _answer;
        private readonly List<string> _asked = [];

        private FakeKWinEffects(DBusConnection connection, IEnumerable<string> loaded, bool answer)
        {
            _connection = connection;
            _loaded = [.. loaded];
            _answer = answer;
        }

        public string Path => "/Effects";
        public bool HandlesChildPaths => false;

        public IReadOnlyList<string> Asked
        {
            get
            {
                lock (_asked)
                {
                    return [.. _asked];
                }
            }
        }

        public static async Task<FakeKWinEffects> StartAsync(string address, string[] loaded, bool answer = true)
        {
            DBusConnection connection = new(address);
            await connection.ConnectAsync();
            FakeKWinEffects kwin = new(connection, loaded, answer);
            connection.AddMethodHandler(kwin);
            await connection.RequestNameAsync("org.kde.KWin");
            return kwin;
        }

        public ValueTask HandleMethodAsync(MethodContext context)
        {
            Message request = context.Request;
            if (request.InterfaceAsString != "org.kde.kwin.Effects" || request.MemberAsString != "isEffectLoaded")
            {
                context.ReplyUnknownMethodError();
                return ValueTask.CompletedTask;
            }

            string name = request.GetBodyReader().ReadString();
            lock (_asked)
            {
                _asked.Add(name);
            }

            if (!_answer)
            {
                // Hold the call unanswered, as a hung compositor would.
                return ValueTask.CompletedTask;
            }

            using MessageWriter w = context.CreateReplyWriter("b");
            w.WriteBool(_loaded.Contains(name));
            context.Reply(w.CreateMessage());
            return ValueTask.CompletedTask;
        }

        public void Dispose()
        {
            _connection.Dispose();
        }
    }
}
