using FluentAssertions;
using MosaicShell.Core.Modules;
using MosaicShell.Core.Runtime;

namespace MosaicShell.Core.Tests
{
    public class ModuleRemovalPolicyTests : IDisposable
    {
        private readonly string _home;

        public ModuleRemovalPolicyTests()
        {
            _home = Path.Combine(Path.GetTempPath(), "ms-removal-" + Guid.NewGuid().ToString("N"));
            AppPaths.SetRootOverride(_home);
            AppPaths.EnsureLayout();
        }

        public void Dispose()
        {
            AppPaths.ClearRootOverride();
            try { Directory.Delete(_home, recursive: true); } catch { /* ignore */ }
        }

        [Fact]
        public void Every_bundled_module_is_removed_at_once_with_undo()
        {
            foreach (ModuleInfo info in ModuleCatalog.BuiltIns)
            {
                _ = ModuleCatalog.IsBundled(info.Id).Should().BeTrue(info.Id);
                _ = ModuleRemovalPolicy.ModeFor(info.Id).Should().Be(ModuleRemovalMode.RemoveWithUndo, info.Id);
            }
        }

        [Fact]
        public void Bundled_check_ignores_case()
        {
            _ = ModuleCatalog.IsBundled("tessera").Should().BeTrue();
        }

        [Fact]
        public void A_third_party_module_asks_first()
        {
            _ = Directory.CreateDirectory(Path.Combine(AppPaths.ModulesDirectory, "Gizmo"));
            ModuleManifest.WriteDefault("Gizmo", "Gizmo");

            _ = ModuleCatalog.IsBundled("Gizmo").Should().BeFalse();
            _ = ModuleRemovalPolicy.ModeFor("Gizmo").Should().Be(ModuleRemovalMode.ConfirmFirst);
        }

        [Fact]
        public void Remove_is_offered_only_for_installed_modules()
        {
            _ = ModuleRemovalPolicy.CanRemove("Tessera").Should().BeFalse();
            _ = Directory.CreateDirectory(Path.Combine(AppPaths.ModulesDirectory, "Tessera"));
            _ = ModuleRemovalPolicy.CanRemove("Tessera").Should().BeTrue();
        }

        [Fact]
        public void Confirmation_copy_names_the_module_and_says_it_cannot_be_brought_back()
        {
            _ = ModuleRemovalPolicy.ConfirmTitle("Gizmo").Should().Be("Remove Gizmo?");
            _ = ModuleRemovalPolicy.ConfirmBody("Gizmo").Should()
                .Be("MosaicShell did not ship Gizmo, so you will need its package to add it again.");
        }

        [Fact]
        public void Status_copy_is_plain()
        {
            _ = ModuleRemovalPolicy.RemovedStatus("Slate").Should().Be("Removed Slate.");
            _ = ModuleRemovalPolicy.RestoredStatus("Slate").Should().Be("Added Slate back.");
            _ = ModuleRemovalPolicy.UndoLabel.Should().Be("Undo");
        }
    }
}
