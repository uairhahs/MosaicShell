using FluentAssertions;
using MosaicShell.Core.Settings;
using MosaicShell.Core.Styles;

namespace MosaicShell.Core.Tests;

public class StyleIdsTests
{
    [Theory]
    [InlineData("Pixel", "MaterialYou")]
    [InlineData("Win11", "Windows11")]
    [InlineData("Modern", "ModernFlyouts")]
    [InlineData("Amber", "Meter")]
    [InlineData("Center", "Square")]
    [InlineData("Simple", "Compact")]
    [InlineData("Plainext", "PlainText")]
    [InlineData("Smouti", "Radial")]
    [InlineData("pixel", "MaterialYou")]
    [InlineData("MaterialYou", "MaterialYou")]
    [InlineData("Windows11", "Windows11")]
    [InlineData("Fluent", "Fluent")]
    [InlineData("Gnome", "Gnome")]
    [InlineData("CoreUI", "CoreUI")]
    public void Normalize_maps_legacy_and_canonical(string input, string expected) =>
        StyleIds.Normalize(input).Should().Be(expected);

    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("  ", "")]
    [InlineData("Fluent10", "Fluent10")]
    [InlineData("MIUI", "MIUI")]
    public void Normalize_passes_through_unknown_and_empty(string? input, string expected) =>
        StyleIds.Normalize(input).Should().Be(expected);

    [Theory]
    [InlineData("MaterialYou", "Material You")]
    [InlineData("Pixel", "Material You")]
    [InlineData("Windows11", "Windows 11")]
    [InlineData("Win11", "Windows 11")]
    [InlineData("Gnome", "GNOME")]
    [InlineData("ModernFlyouts", "Modern Flyouts")]
    [InlineData("Fluent10", "Fluent10")]
    public void DisplayName_uses_friendly_labels(string input, string expected) =>
        StyleIds.DisplayName(input).Should().Be(expected);

    [Theory]
    [InlineData("Pixel", "MaterialYou", true)]
    [InlineData("MaterialYou", "MaterialYou", false)]
    [InlineData("Win11", "Windows11", true)]
    public void TryMigratePersistedStyle_rewrites_legacy_ids(string input, string expected, bool changed)
    {
        var settings = new TesseraSettings { Style = input };
        StyleIds.TryMigratePersistedStyle(settings).Should().Be(changed);
        settings.Style.Should().Be(expected);
    }
}
