using MosaicShell.Core.Runtime;
using MosaicShell.Core.Services;

namespace MosaicShell.Core.Capabilities;

public enum CapabilityModuleKind
{
    Widget,
    Capability,
    Hybrid
}

public interface IModuleCapability : IDisposable
{
    string ModuleId { get; }
    bool IsArmed { get; }
    Task ArmAsync(CancellationToken cancellationToken = default);
    Task DisarmAsync(CancellationToken cancellationToken = default);
}

/// <summary>UI bridges implemented by the Host (Avalonia).</summary>
public interface ICapabilityUiBridge
{
    IFlyoutPresenter Flyouts { get; }
}

public sealed record FlyoutRequest(
    string ModuleId,
    string Kind,
    string? StyleId = null,
    string? Anchor = null,
    int AutoDismissMs = 2500,
    IReadOnlyDictionary<string, string>? Payload = null,
    int MonitorIndex = 1,
    int XPad = 20,
    int YPad = 20,
    int Ani = 2,
    string AniDir = "Left");

public interface IFlyoutPresenter
{
    void Show(FlyoutRequest request);
    void Update(FlyoutRequest request);
    void Hide(string moduleId);
    void HideAll();
    bool IsVisible(string moduleId);
}

public interface ICapabilityFactory
{
    string ModuleId { get; }
    IModuleCapability Create(ModuleManifest manifest, HostServices services, ICapabilityUiBridge ui);
}

public sealed class CapabilityRegistry
{
    private readonly Dictionary<string, ICapabilityFactory> _factories = new(StringComparer.OrdinalIgnoreCase);

    public void Register(ICapabilityFactory factory) =>
        _factories[factory.ModuleId] = factory;

    public bool TryGetFactory(string moduleId, out ICapabilityFactory? factory) =>
        _factories.TryGetValue(moduleId, out factory);

    public IReadOnlyCollection<string> RegisteredModuleIds => _factories.Keys.ToList();

    /// <summary>
    /// Optional external plugin: Modules\{id}\capability.dll exporting a single ICapabilityFactory.
    /// Built-ins always win if already registered.
    /// </summary>
    public void TryLoadExternal(string moduleId, string modulesRoot)
    {
        if (_factories.ContainsKey(moduleId)) return;
        var dll = Path.Combine(modulesRoot, moduleId, "capability.dll");
        if (!File.Exists(dll)) return;
        try
        {
            var asm = System.Reflection.Assembly.LoadFrom(dll);
            var type = asm.GetTypes()
                .FirstOrDefault(t => typeof(ICapabilityFactory).IsAssignableFrom(t) && !t.IsAbstract && t.GetConstructor(Type.EmptyTypes) is not null);
            if (type is null) return;
            if (Activator.CreateInstance(type) is ICapabilityFactory factory)
                Register(factory);
        }
        catch
        {
            // External plugins are best-effort.
        }
    }
}
