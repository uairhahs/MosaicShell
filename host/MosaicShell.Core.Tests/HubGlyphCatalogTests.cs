using FluentAssertions;
using MosaicShell.Core.Modules;
using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Core.Tests
{
    public class HubGlyphCatalogTests
    {
        [Fact]
        public void Every_builtin_module_has_a_named_material_glyph()
        {
            foreach (ModuleInfo module in ModuleCatalog.BuiltIns)
            {
                HubGlyph glyph = HubGlyphCatalog.ForModule(module.Id);
                _ = glyph.Kind.Should().NotBeNullOrWhiteSpace();
                _ = glyph.Kind.Should().MatchRegex("^[A-Z][A-Za-z0-9]+$");
                bool parsed = TesseraAccentColor.TryParse(glyph.TintHex, out _, out _, out _);
                _ = parsed.Should().BeTrue($"{module.Id} tint {glyph.TintHex} must be #RRGGBB");
            }
        }

        [Fact]
        public void Builtin_module_glyphs_match_hub_contract()
        {
            _ = HubGlyphCatalog.ForModule("Tessera").Kind.Should().Be("VolumeHigh");
            _ = HubGlyphCatalog.ForModule("Mixdeck").Kind.Should().Be("TuneVertical");
            _ = HubGlyphCatalog.ForModule("Inlay").Kind.Should().Be("Apps");
            _ = HubGlyphCatalog.ForModule("Slate").Kind.Should().Be("ClockOutline");
            _ = HubGlyphCatalog.ForModule("Chord").Kind.Should().Be("KeyboardOutline");
            _ = HubGlyphCatalog.ForModule("Substrate").Kind.Should().Be("Tune");
            _ = HubGlyphCatalog.ForModule("Chrono").Kind.Should().Be("Clock");
            _ = HubGlyphCatalog.ForModule("Phono").Kind.Should().Be("MusicNote");
            _ = HubGlyphCatalog.ForModule("Pulse").Kind.Should().Be("Equalizer");
            _ = HubGlyphCatalog.ForModule("Canvas").Kind.Should().Be("CardTextOutline");
        }

        [Fact]
        public void Home_cards_have_material_glyphs()
        {
            _ = HubGlyphCatalog.ForHomeCard("Welcome").Kind.Should().Be("PartyPopper");
            _ = HubGlyphCatalog.ForHomeCard("Tiles").Kind.Should().Be("Widgets");
            _ = HubGlyphCatalog.ForHomeCard("About").Kind.Should().Be("InformationOutline");
        }

        [Fact]
        public void Unknown_ids_fall_back_to_puzzle()
        {
            HubGlyph glyph = HubGlyphCatalog.ForModule("NotAModule");
            _ = glyph.Kind.Should().Be("Puzzle");
            _ = HubGlyphCatalog.ForHomeCard("Nope").Kind.Should().Be("Apps");
        }
    }
}
