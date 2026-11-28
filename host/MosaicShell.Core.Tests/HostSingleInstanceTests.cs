using FluentAssertions;
using MosaicShell.Core.Capabilities.Platform;
using MosaicShell.Core.HostPlatform;
using static MosaicShell.Core.Tests.TestWaits;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// One Host per data folder: the first launch holds a lock in the folder and listens; a later launch finds the lock
    /// held, asks the first to show its Hub, and exits instead of arming a second set of capabilities.
    /// </summary>
    public sealed class HostSingleInstanceTests : IDisposable
    {
        private readonly string _root = Directory.CreateTempSubdirectory("ms-instance-").FullName;

        public void Dispose()
        {
            try { Directory.Delete(_root, recursive: true); } catch { /* a held lock on Windows; the OS cleans temp */ }
        }

        [Fact]
        public void The_first_launch_is_primary_and_a_second_is_not()
        {
            using HostSingleInstance? first = HostSingleInstance.TryAcquire(_root);
            using HostSingleInstance? second = HostSingleInstance.TryAcquire(_root);

            _ = first.Should().NotBeNull();
            _ = second.Should().BeNull();
        }

        [Fact]
        public void The_lock_is_free_again_once_the_primary_is_gone()
        {
            HostSingleInstance? first = HostSingleInstance.TryAcquire(_root);
            first!.Dispose();

            using HostSingleInstance? next = HostSingleInstance.TryAcquire(_root);

            _ = next.Should().NotBeNull();
        }

        [Fact]
        public void Separate_data_folders_each_get_their_own_primary()
        {
            string other = Directory.CreateTempSubdirectory("ms-instance-other-").FullName;
            try
            {
                using HostSingleInstance? a = HostSingleInstance.TryAcquire(_root);
                using HostSingleInstance? b = HostSingleInstance.TryAcquire(other);

                _ = a.Should().NotBeNull();
                _ = b.Should().NotBeNull("a throwaway MOSAICSHELL_HOME runs beside the user's own Host");
            }
            finally
            {
                try { Directory.Delete(other, recursive: true); } catch { /* ignore */ }
            }
        }

        [Fact]
        public async Task A_second_launch_asks_the_primary_to_show_itself()
        {
            using HostSingleInstance primary = HostSingleInstance.TryAcquire(_root)!;
            int requests = 0;
            primary.ActivationRequested += (_, _) => Interlocked.Increment(ref requests);
            primary.StartListening();

            bool delivered = HostSingleInstance.SignalPrimary(_root, Patience);
            bool again = HostSingleInstance.SignalPrimary(_root, Patience);

            _ = delivered.Should().BeTrue();
            _ = again.Should().BeTrue("the primary keeps listening after each request");
            _ = (await EventuallyAsync(() => Volatile.Read(ref requests) == 2)).Should().BeTrue();
        }

        [Fact]
        public async Task A_burst_of_launches_is_answered_every_time()
        {
            // On Unix the pipe's instances share one listening socket: a listener that closed its instance before opening
            // the next dropped a launch that connected in between, though that launch had already reported success.
            using HostSingleInstance primary = HostSingleInstance.TryAcquire(_root)!;
            int requests = 0;
            primary.ActivationRequested += (_, _) => Interlocked.Increment(ref requests);
            primary.StartListening();

            int delivered = Enumerable.Range(0, 10).Count(_ => HostSingleInstance.SignalPrimary(_root, Patience));

            _ = delivered.Should().Be(10);
            _ = (await EventuallyAsync(() => Volatile.Read(ref requests) == 10)).Should().BeTrue();
        }

        [Fact]
        public void Signalling_with_no_primary_listening_fails_quickly_instead_of_hanging()
        {
            DateTime started = DateTime.UtcNow;

            bool delivered = HostSingleInstance.SignalPrimary(_root, TimeSpan.FromMilliseconds(300));

            _ = delivered.Should().BeFalse();
            _ = (DateTime.UtcNow - started).Should().BeLessThan(TimeSpan.FromSeconds(3));
        }

        [Fact]
        public void A_stopped_primary_no_longer_answers()
        {
            HostSingleInstance primary = HostSingleInstance.TryAcquire(_root)!;
            primary.StartListening();
            primary.Dispose();

            bool delivered = HostSingleInstance.SignalPrimary(_root, TimeSpan.FromMilliseconds(300));

            _ = delivered.Should().BeFalse();
        }

        [Fact]
        public async Task A_socket_left_by_a_Host_that_died_does_not_stop_the_next_one_answering()
        {
            if (OperatingSystem.IsWindows())
            {
                return;
            }

            string leftover = Path.Combine(Path.GetTempPath(), "CoreFxPipe_" + HostSingleInstance.PipeNameFor(_root));
            await File.WriteAllTextAsync(leftover, "stale");
            using HostSingleInstance primary = HostSingleInstance.TryAcquire(_root)!;
            int requests = 0;
            primary.ActivationRequested += (_, _) => Interlocked.Increment(ref requests);
            primary.StartListening();

            bool delivered = HostSingleInstance.SignalPrimary(_root, Patience);

            _ = delivered.Should().BeTrue();
            _ = (await EventuallyAsync(() => Volatile.Read(ref requests) == 1)).Should().BeTrue();
        }

        [Theory]
        [InlineData(HostLaunchMode.FullHub, true)]
        [InlineData(HostLaunchMode.TrayOnly, false)]
        public void A_second_launch_shows_the_Hub_unless_it_was_started_for_the_tray(HostLaunchMode mode, bool expected)
        {
            _ = HostSingleInstance.ShouldSignal(mode).Should().Be(expected, "a login entry starting a second copy must not pop the Hub open");
        }
    }
}
