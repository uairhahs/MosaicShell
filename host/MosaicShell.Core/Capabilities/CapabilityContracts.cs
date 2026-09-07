using System.Reflection;
using MosaicShell.Core.Capabilities.Platform;
using MosaicShell.Core.Modules.Tessera;
using MosaicShell.Core.Runtime;

namespace MosaicShell.Core.Capabilities
{
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
        IHostUiBridge HostUi { get; }

        /// <summary>
        /// Run on the Host message-pump thread (Avalonia UI thread). Tests may run inline.
        /// Required for WH_KEYBOARD_LL hook install/start per TesseraArmPolicy.
        /// </summary>
        void RunOnHostThread(Action action);
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
        string AniDir = "Left",
        string AniEase = TesseraFlyoutAnimationPolicy.DefaultEase,
        int AniSteps = TesseraFlyoutAnimationPolicy.DefaultAniSteps,
        int AnimationDisplacement = TesseraFlyoutAnimationPolicy.DefaultDisplacementPx);

    public interface IFlyoutPresenter
    {
        /// <summary>Auto-dismiss or outside-click hide (SoftFrost transient dismiss, not Disarm Hide).</summary>
        event Action<string>? TransientDismissed;

        void Show(FlyoutRequest request);
        void Update(FlyoutRequest request);
        /// <summary>Patch visible flyout UI without resetting auto-dismiss (progress / live pump).</summary>
        void SoftRefresh(FlyoutRequest request);
        void Hide(string moduleId);
        void HideAll();
        bool IsVisible(string moduleId);

        /// <summary>
        /// IPC and capability routing. Default is <see cref="IsVisible"/> with an empty session.
        /// Host echoes the live Tessera snapshot after each apply.
        /// </summary>
        TesseraFlyoutSessionSnapshot GetSessionSnapshot(string moduleId)
        {
            bool visible = IsVisible(moduleId);
            return new(visible, 0, TesseraFlyoutSessionMode.None, "", null, visible ? TesseraFlyoutPhase.Shown : TesseraFlyoutPhase.Hidden);
        }
    }

    public interface ICapabilityFactory
    {
        string ModuleId { get; }
        IModuleCapability Create(ModuleManifest manifest, ICapabilityContext context);
    }

    public sealed class CapabilityRegistry
    {
        private readonly Dictionary<string, ICapabilityFactory> _factories = new(StringComparer.OrdinalIgnoreCase);

        public void Register(ICapabilityFactory factory)
        {
            _factories[factory.ModuleId] = factory;
        }

        public bool TryGetFactory(string moduleId, out ICapabilityFactory? factory)
        {
            return _factories.TryGetValue(moduleId, out factory);
        }

        public IReadOnlyCollection<string> RegisteredModuleIds => [.. _factories.Keys];

        /// <summary>
        /// Optional external plugin: Modules\{id}\module.dll or capability.dll exporting ICapabilityFactory.
        /// Built-ins always win if already registered.
        /// </summary>
        public void TryLoadExternal(string moduleId, string modulesRoot)
        {
            if (_factories.ContainsKey(moduleId))
            {
                return;
            }

            foreach (string? name in new[] { "module.dll", "capability.dll" })
            {
                string dll = Path.Combine(modulesRoot, moduleId, name);
                if (!File.Exists(dll))
                {
                    continue;
                }

                try
                {
                    Assembly asm = Assembly.LoadFrom(dll);
                    Type? type = asm.GetTypes()
                        .FirstOrDefault(t => typeof(ICapabilityFactory).IsAssignableFrom(t) && !t.IsAbstract && t.GetConstructor(Type.EmptyTypes) is not null);
                    if (type is null)
                    {
                        continue;
                    }

                    if (Activator.CreateInstance(type) is ICapabilityFactory factory)
                    {
                        Register(factory);
                    }

                    return;
                }
                catch
                {
                    // External plugins are best-effort.
                }
            }
        }
    }
}
