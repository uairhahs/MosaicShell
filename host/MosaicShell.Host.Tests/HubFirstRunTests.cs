using Avalonia.Headless.XUnit;
using FluentAssertions;
using MosaicShell.Core;
using MosaicShell.Core.Capabilities;
using MosaicShell.Core.Install;
using MosaicShell.Core.Modules;
using MosaicShell.Core.Runtime;
using MosaicShell.Core.Services;
using MosaicShell.Core.Settings;
using MosaicShell.Host.ViewModels;

namespace MosaicShell.Host.Tests
{
    /// <summary>
    /// First run (U25): the Host adds the default modules, turns them on and shows one screen with them, then Done lands
    /// on Home. An existing install (any saved settings) never sees it, so an update does not turn modules back on.
    /// </summary>
    public sealed class HubFirstRunTests : HeadlessHostTest
    {
        private sealed class ArmsEverything : ICapabilityHost
        {
            private readonly HashSet<string> _armed = new(StringComparer.OrdinalIgnoreCase);

            public IReadOnlyList<string> ArmedModuleIds => [.. _armed];

            public bool IsArmed(string moduleId) => _armed.Contains(moduleId);

            public Task<bool> ArmAsync(string moduleId, bool persist = true, CancellationToken cancellationToken = default)
            {
                _ = _armed.Add(moduleId);
                return Task.FromResult(true);
            }

            public Task<bool> DisarmAsync(string moduleId, bool persist = true, CancellationToken cancellationToken = default)
            {
                _ = _armed.Remove(moduleId);
                return Task.FromResult(true);
            }

            public Task<bool> ReArmAsync(string moduleId, CancellationToken cancellationToken = default) => Task.FromResult(true);

            public string? GetHotkeyError(string moduleId) => null;

            public Task RestoreAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

            public Task DisarmAllAsync(bool persist = true, CancellationToken cancellationToken = default)
            {
                _armed.Clear();
                return Task.CompletedTask;
            }

            public void Persist()
            {
            }

            public void Dispose()
            {
            }
        }

        /// <summary>
        /// As the Host reads it at the top of Main, before anything is written: call it before building the Hub, whose
        /// construction saves settings of its own.
        /// </summary>
        private static bool SavedNow()
        {
            return FirstRunPolicy.AnySettingsSaved(AppPaths.ConfigDirectory);
        }

        private static MainViewModel NewHub(ArmsEverything capabilities)
        {
            return new MainViewModel(new TileRuntime(new NullSurfaces()), HostServicesFakes.Create(), null, capabilities);
        }

        private sealed class NullSurfaces : ITileSurfaceHost
        {
            public bool Show(string moduleId, out string? error)
            {
                error = "test";
                return false;
            }

            public void Focus(string moduleId)
            {
            }

            public void Close(string moduleId)
            {
            }
        }

        [AvaloniaFact]
        public async Task A_fresh_install_adds_the_defaults_turns_them_on_and_shows_them()
        {
            bool saved = SavedNow();
            ArmsEverything capabilities = new();
            MainViewModel vm = NewHub(capabilities);

            await vm.RunFirstRunIfDueAsync(saved);

            _ = vm.IsFirstRunPage.Should().BeTrue();
            _ = ModuleCatalog.IsInstalled(ModuleIds.Tessera).Should().BeTrue();
            _ = capabilities.IsArmed(ModuleIds.Tessera).Should().BeTrue("first run shows the modules already on");
            _ = vm.InstalledModules.Select(m => m.Id).Should().Contain(ModuleIds.Tessera);
            _ = vm.InstalledModules.Should().OnlyContain(m => m.IsOn);
            _ = ModuleSettingsStore.Load("Hub", () => new HubSettings()).DefaultModulesOffered.Should().BeTrue();
        }

        [AvaloniaFact]
        public async Task An_install_with_saved_settings_never_sees_first_run()
        {
            _ = Directory.CreateDirectory(Path.Combine(AppPaths.ConfigDirectory, "modules"));
            File.WriteAllText(Path.Combine(AppPaths.ConfigDirectory, "modules", "Chrono.json"), "{}");
            bool saved = SavedNow();
            ArmsEverything capabilities = new();
            MainViewModel vm = NewHub(capabilities);

            await vm.RunFirstRunIfDueAsync(saved);

            _ = vm.IsFirstRunPage.Should().BeFalse("an update keeps the user's choices");
            _ = ModuleCatalog.IsInstalled(ModuleIds.Tessera).Should().BeFalse("nothing removed comes back");
            _ = capabilities.ArmedModuleIds.Should().BeEmpty();
            _ = ModuleSettingsStore.Load("Hub", () => new HubSettings()).DefaultModulesOffered.Should().BeTrue();
        }

        [AvaloniaFact]
        public async Task The_second_start_has_no_first_run()
        {
            bool saved = SavedNow();
            await NewHub(new ArmsEverything()).RunFirstRunIfDueAsync(saved);

            bool savedAgain = SavedNow();
            MainViewModel again = NewHub(new ArmsEverything());
            await again.RunFirstRunIfDueAsync(savedAgain);

            _ = again.IsFirstRunPage.Should().BeFalse();
        }

        [AvaloniaFact]
        public async Task Done_lands_on_Home()
        {
            bool saved = SavedNow();
            MainViewModel vm = NewHub(new ArmsEverything());
            await vm.RunFirstRunIfDueAsync(saved);
            _ = vm.IsFirstRunPage.Should().BeTrue();

            vm.FinishFirstRunCommand.Execute(null);

            _ = vm.IsFirstRunPage.Should().BeFalse();
            _ = vm.IsHome.Should().BeTrue();
        }
    }
}
