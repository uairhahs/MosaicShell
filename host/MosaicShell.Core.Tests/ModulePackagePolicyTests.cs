using FluentAssertions;
using MosaicShell.Core.Install;

namespace MosaicShell.Core.Tests
{
    public class ModulePackagePolicyTests
    {
        [Theory]
        [InlineData("Tessera")]
        [InlineData("Canvas")]
        [InlineData("HelloTile")]
        [InlineData("my-module")]
        [InlineData("my_module")]
        [InlineData("Module.2")]
        [InlineData("a")]
        public void Accepts_plain_single_segment_ids(string id)
        {
            _ = ModulePackagePolicy.IsValidModuleId(id).Should().BeTrue();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Rejects_missing_ids(string? id)
        {
            _ = ModulePackagePolicy.IsValidModuleId(id).Should().BeFalse();
        }

        [Theory]
        [InlineData("..")]
        [InlineData(".")]
        [InlineData("../Tessera")]
        [InlineData("..\\..\\Windows")]
        [InlineData("sub/dir")]
        [InlineData("sub\\dir")]
        public void Rejects_path_traversal(string id)
        {
            _ = ModulePackagePolicy.IsValidModuleId(id).Should().BeFalse();
        }

        [Theory]
        [InlineData("C:\\Windows")]
        [InlineData("C:Tessera")]
        [InlineData("\\\\server\\share")]
        [InlineData("/etc")]
        public void Rejects_rooted_and_drive_relative_ids(string id)
        {
            _ = ModulePackagePolicy.IsValidModuleId(id).Should().BeFalse();
        }

        [Theory]
        [InlineData("CON")]
        [InlineData("con")]
        [InlineData("PRN")]
        [InlineData("AUX")]
        [InlineData("NUL")]
        [InlineData("COM1")]
        [InlineData("LPT9")]
        [InlineData("COM1.json")]
        public void Rejects_reserved_windows_device_names(string id)
        {
            _ = ModulePackagePolicy.IsValidModuleId(id).Should().BeFalse();
        }

        [Theory]
        [InlineData("bad:name")]
        [InlineData("bad*name")]
        [InlineData("bad?name")]
        [InlineData("bad|name")]
        [InlineData("bad\"name")]
        [InlineData("bad<name")]
        [InlineData("bad>name")]
        public void Rejects_invalid_file_name_characters(string id)
        {
            _ = ModulePackagePolicy.IsValidModuleId(id).Should().BeFalse();
        }

        [Theory]
        [InlineData("Tessera ")]
        [InlineData(" Tessera")]
        [InlineData("Tessera.")]
        public void Rejects_ids_windows_would_silently_trim(string id)
        {
            _ = ModulePackagePolicy.IsValidModuleId(id).Should().BeFalse();
        }

        [Fact]
        public void Rejects_ids_longer_than_the_cap()
        {
            _ = ModulePackagePolicy.IsValidModuleId(new string('a', 65)).Should().BeFalse();
            _ = ModulePackagePolicy.IsValidModuleId(new string('a', 64)).Should().BeTrue();
        }

        [Fact]
        public void EnsureValidModuleId_returns_the_id_when_valid()
        {
            _ = ModulePackagePolicy.EnsureValidModuleId("Tessera").Should().Be("Tessera");
        }

        [Fact]
        public void EnsureValidModuleId_throws_and_names_the_offending_id()
        {
            Action act = () => ModulePackagePolicy.EnsureValidModuleId("../../Windows");

            _ = act.Should().Throw<InvalidOperationException>()
                .WithMessage("*../../Windows*");
        }

        [Fact]
        public void ResolveModuleDirectory_stays_inside_the_modules_root()
        {
            string root = Path.Combine(Path.GetTempPath(), "ms-modroot");

            string resolved = ModulePackagePolicy.ResolveModuleDirectory(root, "Tessera");

            _ = resolved.Should().Be(Path.Combine(Path.GetFullPath(root), "Tessera"));
        }

        [Fact]
        public void ResolveModuleDirectory_rejects_an_escaping_id()
        {
            string root = Path.Combine(Path.GetTempPath(), "ms-modroot");

            Action act = () => ModulePackagePolicy.ResolveModuleDirectory(root, "../escape");

            _ = act.Should().Throw<InvalidOperationException>();
        }
    }
}
