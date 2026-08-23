using MosaicShell.Core;
using MosaicShell.Core.Capabilities;
using MosaicShell.Core.Capabilities.BuiltIn;
using MosaicShell.Core.Capabilities.Ipc;
using MosaicShell.Core.Capabilities.Platform;
using MosaicShell.Core.Runtime;
using MosaicShell.Core.Services;

namespace MosaicShell.Worker;

internal static class Program
{
    [STAThread]
    public static async Task<int> Main(string[] args)
    {
        AppPaths.EnsureLayout();

        using var coordinator = new CapabilityDaemonCoordinator();
        if (!coordinator.TryAcquireOwner())
        {
            Console.Error.WriteLine(
                "Another MosaicShell process already owns the capability daemon " +
                "(usually MosaicShell.Host). Exit Host or use Host only; " +
                "many modules can still be armed at once in a single daemon.");
            return 1;
        }

        using var services = HostServices.CreateWindowsDefaults();
        using var flyouts = new IpcFlyoutPresenter();

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

        var ui = new WorkerCapabilityUiBridge(flyouts);
        var registry = new CapabilityRegistry();
        BuiltInCapabilityFactories.RegisterAll(registry);
        using var daemon = new CapabilityDaemon(registry, services, ui);
        using var controlServer = new CapabilityIpcControlServer(daemon);
        controlServer.Start();

        await daemon.RestoreAsync().ConfigureAwait(false);
        var armed = daemon.ArmedModuleIds;
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
        await Task.Run(Console.ReadLine).ConfigureAwait(false);

        await daemon.DisarmAllAsync().ConfigureAwait(false);
        return 0;
    }
}

internal sealed class WorkerCapabilityUiBridge(IFlyoutPresenter flyouts) : ICapabilityUiBridge
{
    public IFlyoutPresenter Flyouts { get; } = flyouts;
    public IHostUiBridge HostUi { get; } = NullHostUiBridge.Instance;

    /// <summary>Worker main thread is STA; hooks and capability logic run inline.</summary>
    public void RunOnHostThread(Action action) => action();
}
