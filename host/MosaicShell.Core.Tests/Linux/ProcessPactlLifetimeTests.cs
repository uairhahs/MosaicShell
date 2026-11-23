using System.Diagnostics;
using System.Runtime.Versioning;
using FluentAssertions;
using MosaicShell.Core.Services.Linux;

namespace MosaicShell.Core.Tests.Linux
{
    /// <summary>
    /// The pactl children must not outlive the Host. libpulse ignores SIGPIPE, so an orphaned <c>pactl subscribe</c>
    /// never notices its reader is gone and runs forever; nine were found alive after nine Host restarts (2026-11-23).
    /// </summary>
    public sealed class ProcessPactlLifetimeTests : IDisposable
    {
        private static readonly TimeSpan Patience = TimeSpan.FromSeconds(5);
        private readonly string _dir = Directory.CreateTempSubdirectory("pactl-lifetime-").FullName;

        public void Dispose()
        {
            Directory.Delete(_dir, recursive: true);
        }

        [UnixFact]
        public void KillAll_ends_every_tracked_child()
        {
            ChildProcessSet set = new();
            using Process sleeper = StartSleep();
            set.Add(sleeper);

            set.KillAll();

            _ = sleeper.WaitForExit(Patience).Should().BeTrue();
            _ = set.Ids.Should().BeEmpty();
        }

        [UnixFact]
        public void A_removed_child_is_left_alone()
        {
            ChildProcessSet set = new();
            using Process sleeper = StartSleep();
            set.Add(sleeper);
            set.Remove(sleeper);

            set.KillAll();

            try
            {
                _ = sleeper.HasExited.Should().BeFalse();
            }
            finally
            {
                sleeper.Kill();
            }
        }

        [UnixFact]
        [UnsupportedOSPlatform("windows")]
        public async Task Disposing_ProcessPactl_ends_a_running_subscribe()
        {
            string fake = Path.Combine(_dir, "pactl");
            await File.WriteAllTextAsync(fake, "#!/bin/sh\nexec sleep 30\n");
            File.SetUnixFileMode(fake, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);

            ProcessPactl pactl = new(fake);
            await using IAsyncEnumerator<string> lines = pactl.SubscribeAsync(CancellationToken.None).GetAsyncEnumerator();
            ValueTask<bool> pending = lines.MoveNextAsync();

            int pid = await WaitForChild(pactl);
            using Process child = Process.GetProcessById(pid);

            pactl.Dispose();

            _ = child.WaitForExit(Patience).Should().BeTrue();
            _ = (await pending).Should().BeFalse();
        }

        private static Process StartSleep()
        {
            return Process.Start(new ProcessStartInfo("sleep", "30") { UseShellExecute = false })!;
        }

        private static async Task<int> WaitForChild(ProcessPactl pactl)
        {
            DateTime deadline = DateTime.UtcNow + Patience;
            while (DateTime.UtcNow < deadline)
            {
                if (pactl.ChildIds.Count == 1)
                {
                    return pactl.ChildIds[0];
                }

                await Task.Delay(20);
            }

            throw new TimeoutException("pactl subscribe never started");
        }
    }
}
