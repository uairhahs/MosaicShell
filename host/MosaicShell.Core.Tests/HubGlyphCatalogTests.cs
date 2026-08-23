using FluentAssertions;
using MosaicShell.Core.Modules;
using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Core.Tests;

public class HubGlyphCatalogTests
{
    [Fact]
    public void Every_builtin_module_has_a_named_material_glyph()
    {
        foreach (var module in ModuleCatalog.BuiltIns)
        {
            var glyph = HubGlyphCatalog.ForModule(module.Id);
            glyph.Kind.Should().NotBeNullOrWhiteSpace();
            glyph.Kind.Should().MatchRegex("^[A-Z][A-Za-z0-9]+$");
            var parsed = TesseraAccentColor.TryParse(glyph.TintHex, out _, out _, out _);
            parsed.Should().BeTrue($"{module.Id} tint {glyph.TintHex} must be #RRGGBB");
        }
    }

    [Fact]
    public void Builtin_module_glyphs_match_hub_contract()
    {
        HubGlyphCatalog.ForModule("Tessera").Kind.Should().Be("VolumeHigh");
        HubGlyphCatalog.ForModule("Mixdeck").Kind.Should().Be("TuneVertical");
        HubGlyphCatalog.ForModule("Inlay").Kind.Should().Be("Apps");
        HubGlyphCatalog.ForModule("Slate").Kind.Should().Be("ClockOutline");
        HubGlyphCatalog.ForModule("Chord").Kind.Should().Be("KeyboardOutline");
        HubGlyphCatalog.ForModule("Substrate").Kind.Should().Be("Tune");
        HubGlyphCatalog.ForModule("Chrono").Kind.Should().Be("Clock");
        HubGlyphCatalog.ForModule("Phono").Kind.Should().Be("MusicNote");
        HubGlyphCatalog.ForModule("Pulse").Kind.Should().Be("Equalizer");
        HubGlyphCatalog.ForModule("Canvas").Kind.Should().Be("CardTextOutline");
    }

    [Fact]
    public void Home_cards_have_material_glyphs()
    {
        HubGlyphCatalog.ForHomeCard("Welcome").Kind.Should().Be("PartyPopper");
        HubGlyphCatalog.ForHomeCard("Tiles").Kind.Should().Be("Widgets");
        HubGlyphCatalog.ForHomeCard("About").Kind.Should().Be("InformationOutline");
    }

    [Fact]
    public void Unknown_ids_fall_back_to_puzzle()
    {
        var glyph = HubGlyphCatalog.ForModule("NotAModule");
        glyph.Kind.Should().Be("Puzzle");
        HubGlyphCatalog.ForHomeCard("Nope").Kind.Should().Be("Apps");
    }
}
