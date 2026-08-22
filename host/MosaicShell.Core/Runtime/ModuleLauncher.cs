namespace MosaicShell.Core.Runtime;

public enum ModuleLaunchBlocker
{
    None,
    NotInstalled,
    NativeRuntimeMissing,
}

public sealed record ModuleLaunchResult(
    bool Started,
    ModuleLaunchBlocker Blocker,
    string Message);

public interface IModuleLauncher
{
    ModuleLaunchResult TryLaunch(string moduleId);
}

/// <summary>
/// Starts installed modules via the native <see cref="ITileRuntime"/>.
/// </summary>
public sealed class ModuleLauncher : IModuleLauncher
{
    private readonly ITileRuntime _runtime;

    public ModuleLauncher(ITileRuntime runtime)
    {
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
    }

    public ModuleLaunchResult TryLaunch(string moduleId) => _runtime.Start(moduleId);

    public bool TryStop(string moduleId) => _runtime.Stop(moduleId);
}
