using MosaicShell.Core;
using MosaicShell.Core.Capabilities;
using MosaicShell.Core.Capabilities.BuiltIn;
using MosaicShell.Core.Capabilities.Ipc;
using MosaicShell.Core.Capabilities.Platform;
using MosaicShell.Core.Services;

namespace MosaicShell.Worker
{
    internal static class Program
    {
        [STAThread]
        public static async Task<int> Main()
        {
            AppPaths.EnsureLayout();

            using CapabilityDaemonCoordinator coordinator = new();
            if (!coordinator.TryAcquireOwner())
            {
                Console.Error.WriteLine(
                    "Another MosaicShell process already owns the capability daemon " +
                    "(usually MosaicShell.Host). Exit Host or use Host only; " +
                    "many modules can still be armed at once in a single daemon.");
                return 1;
            }

            using HostServices services = HostServices.CreateWindowsDefaults();
            using IpcFlyoutPresenter flyouts = new();

            Console.WriteLine("MosaicShell Worker: connecting flyout IPC to Host...");
            try
            {
                await flyouts.ConnectAsync().ConfigureAwait(false);
                Console.WriteLine("Connected to Host flyout presenter.");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(
                    "Could not connect to MosaicShell Host flyout IPC ({0}). " +
                    "Start Host first (tray-only: MosaicShell.Host.exe --tray-only).",
                    ex.Message);
                return 1;
            }

            WorkerCapabilityUiBridge ui = new(flyouts);
            CapabilityRegistry registry = new();
            BuiltInCapabilityFactories.RegisterAll(registry);
            using CapabilityDaemon daemon = new(registry, services, ui);
            using CapabilityIpcControlServer controlServer = new(daemon);
            controlServer.Start();

            await daemon.RestoreAsync().ConfigureAwait(false);
            IReadOnlyList<string> armed = daemon.ArmedModuleIds;
            if (armed.Count == 0)
            {
                Console.WriteLine(
                    "No armed capabilities in store. Arm Tessera, Mixdeck, Slate, etc. from Hub " +
                    "or Mosaicist; all armed modules run together in one daemon.");
            }
            else
            {
                Console.WriteLine($"Armed {armed.Count} module(s): {string.Join(", ", armed)}");
            }

            Console.WriteLine("Worker running. Press Enter to disarm all and exit.");
            _ = await Task.Run(Console.ReadLine).ConfigureAwait(false);

            await daemon.DisarmAllAsync().ConfigureAwait(false);
            return 0;
        }
    }

    internal sealed class WorkerCapabilityUiBridge(IFlyoutPresenter flyouts) : ICapabilityUiBridge
    {
        public IFlyoutPresenter Flyouts { get; } = flyouts;
        public IHostUiBridge HostUi { get; } = NullHostUiBridge.Instance;

        /// <summary>Worker main thread is STA; hooks and capability logic run inline.</summary>
        public void RunOnHostThread(Action action)
        {
            action();
        }
    }
}
