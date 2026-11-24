using System.Runtime.CompilerServices;
using Avalonia.Headless.XUnit;
using FluentAssertions;
using MosaicShell.Core.Modules;
using MosaicShell.Host.ViewModels;

namespace MosaicShell.Host.Tests
{
    /// <summary>
    /// The Hub view model watches the process-wide native translucency backend. That watch must not keep a view model
    /// alive: a strong subscription to the static event rooted every view model that ever opened Tessera's settings.
    /// </summary>
    public sealed class HubViewModelLifetimeTests : HeadlessHostTest
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static WeakReference OpenTesseraSettingsAndDrop()
        {
            MainViewModel vm = new();
            vm.OpenModuleConfigById(ModuleIds.Tessera);
            return new WeakReference(vm);
        }

        [AvaloniaFact]
        public void A_view_model_that_watched_native_translucency_can_be_collected()
        {
            WeakReference vm = OpenTesseraSettingsAndDrop();
            // Run what opening the settings posted (it captures the view model), then collect.
            HeadlessPump.For(100);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            _ = vm.IsAlive.Should().BeFalse("only the Hub may keep its view model alive, not a static event");
        }
    }
}
