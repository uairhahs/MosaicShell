using FluentAssertions;
using MosaicShell.Core.Capabilities.Platform;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// Exactly one process owns the capability daemon. These use a mutex name of their own so a running Host or Worker
    /// on the same machine cannot change the result.
    /// </summary>
    public class CapabilityDaemonCoordinatorTests
    {
        private static string NewMutexName()
        {
            return @"Local\MosaicShell.Test.Daemon." + Guid.NewGuid().ToString("N");
        }

        [Fact]
        public void The_production_lock_is_system_wide_so_host_and_worker_see_each_other()
        {
            CapabilityDaemonCoordinator.MutexName.Should().StartWith(@"Global\");
        }

        [Fact]
        public void The_first_process_to_ask_owns_the_daemon()
        {
            using CapabilityDaemonCoordinator coordinator = new(NewMutexName());

            coordinator.TryAcquireOwner().Should().BeTrue();

            coordinator.OwnsDaemon.Should().BeTrue();
        }

        [Fact]
        public void Asking_again_while_owning_is_still_true()
        {
            using CapabilityDaemonCoordinator coordinator = new(NewMutexName());
            _ = coordinator.TryAcquireOwner();

            coordinator.TryAcquireOwner().Should().BeTrue();
        }

        [Fact]
        public async Task A_second_process_does_not_get_the_daemon_while_the_first_holds_it()
        {
            string name = NewMutexName();
            using CapabilityDaemonCoordinator first = new(name);
            _ = first.TryAcquireOwner();

            // A mutex is owned by a thread, so the second contender must run on another one.
            bool? secondOwns = null;
            await Task.Run(() =>
            {
                using CapabilityDaemonCoordinator second = new(name);
                secondOwns = second.TryAcquireOwner();
                secondOwns = secondOwns.Value && second.OwnsDaemon;
            });

            secondOwns.Should().BeFalse();
        }

        [Fact]
        public async Task The_daemon_can_be_taken_over_once_the_owner_disposes()
        {
            string name = NewMutexName();
            CapabilityDaemonCoordinator first = new(name);
            _ = first.TryAcquireOwner();
            first.Dispose();

            first.OwnsDaemon.Should().BeFalse();
            bool takenOver = await Task.Run(() =>
            {
                using CapabilityDaemonCoordinator second = new(name);
                return second.TryAcquireOwner();
            });

            takenOver.Should().BeTrue();
        }

        [Fact]
        public void Disposing_a_coordinator_that_never_owned_is_harmless()
        {
            CapabilityDaemonCoordinator coordinator = new(NewMutexName());

            coordinator.Invoking(c => c.Dispose()).Should().NotThrow();
            coordinator.OwnsDaemon.Should().BeFalse();
        }

        [Fact]
        public void An_invalid_mutex_name_is_not_an_owner_and_does_not_throw()
        {
            using CapabilityDaemonCoordinator coordinator = new(@"Local\MosaicShell\Test\Bad");

            coordinator.TryAcquireOwner().Should().BeFalse();
            coordinator.OwnsDaemon.Should().BeFalse();
        }
    }
}
