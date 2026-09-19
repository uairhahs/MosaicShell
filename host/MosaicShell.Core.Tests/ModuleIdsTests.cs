using FluentAssertions;
using MosaicShell.Core.Modules;

namespace MosaicShell.Core.Tests
{
    public class ModuleIdsTests
    {
        [Fact]
        public void Every_constant_matches_a_built_in_catalog_entry()
        {
            IReadOnlyList<string> catalog = [.. ModuleCatalog.BuiltIns.Select(m => m.Id)];

            _ = ModuleIds.BuiltIns.Should().BeEquivalentTo(catalog);
        }

        [Fact]
        public void Constants_are_the_exact_catalog_casing()
        {
            // Host compares module ids ordinally in hot paths, so a casing drift here is a real bug.
            foreach (string id in ModuleIds.BuiltIns)
            {
                _ = ModuleCatalog.BuiltIns.Should().Contain(m => m.Id == id);
            }
        }

        [Fact]
        public void Is_matches_case_insensitively_because_manifests_are_user_authored()
        {
            _ = ModuleIds.Is("tessera", ModuleIds.Tessera).Should().BeTrue();
            _ = ModuleIds.Is("TESSERA", ModuleIds.Tessera).Should().BeTrue();
            _ = ModuleIds.Is("Tessera", ModuleIds.Tessera).Should().BeTrue();
        }

        [Fact]
        public void Is_rejects_other_ids_and_blanks()
        {
            _ = ModuleIds.Is("Mixdeck", ModuleIds.Tessera).Should().BeFalse();
            _ = ModuleIds.Is(null, ModuleIds.Tessera).Should().BeFalse();
            _ = ModuleIds.Is("", ModuleIds.Tessera).Should().BeFalse();
            _ = ModuleIds.Is("  ", ModuleIds.Tessera).Should().BeFalse();
        }

        [Fact]
        public void IsTessera_is_the_named_shorthand_the_host_uses()
        {
            _ = ModuleIds.IsTessera("tessera").Should().BeTrue();
            _ = ModuleIds.IsTessera("Canvas").Should().BeFalse();
            _ = ModuleIds.IsTessera(null).Should().BeFalse();
        }

        [Fact]
        public void Every_built_in_id_is_a_valid_install_directory_name()
        {
            foreach (string id in ModuleIds.BuiltIns)
            {
                _ = Core.Install.ModulePackagePolicy.IsValidModuleId(id).Should().BeTrue($"'{id}' is installed to Modules/{id}");
            }
        }
    }
}
