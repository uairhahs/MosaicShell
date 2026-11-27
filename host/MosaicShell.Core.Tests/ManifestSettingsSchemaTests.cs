using System.Text.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using MosaicShell.Core.HostPlatform;
using MosaicShell.Core.Modules.Settings;
using MosaicShell.Core.Runtime;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// A third-party module describes its page in <c>module.manifest.json</c> with the same row kinds the built-ins
    /// use, and gets the same page. Its values live in its own settings file as plain JSON keys. A row the Hub cannot
    /// draw is skipped, never fatal.
    /// </summary>
    public sealed class ManifestSettingsSchemaTests : IDisposable
    {
        private const string Id = "Acme";
        private readonly string _root;

        public ManifestSettingsSchemaTests()
        {
            _root = Path.Combine(Path.GetTempPath(), "MosaicManifestSchema_" + Guid.NewGuid().ToString("N"));
            AppPaths.SetRootOverride(_root);
            AppPaths.EnsureLayout();
        }

        public void Dispose()
        {
            AppPaths.ClearRootOverride();
            try { Directory.Delete(_root, true); } catch { /* ignore */ }
        }

        private static void Install(string id, string kind, string settingsJson, string styles = "[\"Plain\", \"Bold\"]")
        {
            string path = ModuleManifest.PathInModule(id);
            _ = Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, $$"""
                {
                  "Id": "{{id}}",
                  "DisplayName": "{{id}}",
                  "Kind": "{{kind}}",
                  "Styles": {{styles}},
                  "Settings": {{settingsJson}}
                }
                """);
        }

        private const string Page = """
            [
              { "Section": "Look", "Rows": [
                { "Kind": "Style" },
                { "Key": "Theme", "Kind": "Choice", "Label": "Theme", "Default": "dark",
                  "Options": [ { "Value": "dark", "Label": "Dark" }, { "Value": "light", "Label": "Light" } ] }
              ] },
              { "Section": "Behaviour", "Rows": [
                { "Key": "Sound", "Kind": "Toggle", "Label": "Play a sound", "Description": "When it opens.", "Default": true },
                { "Key": "Volume", "Kind": "Number", "Label": "Sound volume", "Min": 0, "Max": 1, "Step": 0.1, "Default": 0.5,
                  "ShownWhen": "Sound" },
                { "Key": "Shortcut", "Kind": "Hotkey", "Label": "Shortcut", "Default": "Ctrl+Alt+H", "Apply": "Live" },
                { "Key": "Sites", "Kind": "TextList", "Label": "Sites", "Default": [ "example.org" ] },
                { "Key": "Links", "Kind": "ActionList", "Label": "Links" }
              ] }
            ]
            """;

        private static ModuleSettingsSchema Schema(string id = Id)
        {
            _ = ModuleSettingsSchemas.TryGet(id, out ModuleSettingsSchema? schema).Should().BeTrue();
            return schema!;
        }

        private static JsonObject Stored(string id = Id)
        {
            return JsonNode.Parse(File.ReadAllText(ModuleSettingsStore.PathFor(id)))!.AsObject();
        }

        [Fact]
        public void A_manifest_with_settings_gets_a_page_with_its_sections_and_rows()
        {
            Install(Id, "Capability", Page);

            ModuleSettingsSchema schema = Schema();

            _ = schema.ModuleId.Should().Be(Id);
            _ = schema.Sections.Select(s => s.Title).Should().Equal("Look", "Behaviour");
            _ = schema.Rows.Select(r => (r.Key, r.Kind)).Should().Equal(
                ("Style", SettingKind.Style),
                ("Theme", SettingKind.Choice),
                ("Sound", SettingKind.Toggle),
                ("Volume", SettingKind.Number),
                ("Shortcut", SettingKind.Hotkey),
                ("Sites", SettingKind.TextList),
                ("Links", SettingKind.ActionList));
            _ = schema.Row("Sound").Description.Should().Be("When it opens.");
            _ = schema.Row("Volume").VisibleWhen.Should().Be("Sound");
            _ = schema.Row("Style").Options.Select(o => o.Value).Should().Equal("Plain", "Bold");
            _ = schema.Row("Shortcut").Requires.Should().Be(DesktopFeature.GlobalHotkeys);
        }

        [Fact]
        public void A_manifest_without_settings_has_no_page()
        {
            string path = ModuleManifest.PathInModule("Bare");
            _ = Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, """{ "Id": "Bare", "Kind": "Widget" }""");

            _ = ModuleSettingsSchemas.TryGet("Bare", out _).Should().BeFalse();
        }

        [Fact]
        public void Rows_read_their_manifest_defaults_before_anything_is_saved()
        {
            Install(Id, "Capability", Page);
            ModuleSettingsDocument doc = Schema().Open();

            _ = doc.Get<bool>("Sound").Should().BeTrue();
            _ = doc.Get<double>("Volume").Should().Be(0.5);
            _ = doc.Get<string>("Theme").Should().Be("dark");
            _ = doc.Get<string>("Shortcut").Should().Be("Ctrl+Alt+H");
            _ = doc.Get<IReadOnlyList<string>>("Sites").Should().Equal("example.org");
            _ = doc.Get<IReadOnlyList<SettingPair>>("Links").Should().BeEmpty();
            _ = doc.Get<string>("Style").Should().Be("Plain");
        }

        [Fact]
        public void Changes_are_stored_as_plain_keys_in_the_modules_own_file()
        {
            Install(Id, "Capability", Page);
            ModuleSettingsDocument doc = Schema().Open();

            doc.Set("Sound", false);
            doc.Set("Volume", 5.0);
            doc.Set("Theme", "LIGHT");
            doc.Set("Links", (IReadOnlyList<SettingPair>)[new("Docs", "https://example.org")]);

            JsonObject stored = Stored();
            _ = stored["Sound"]!.GetValue<bool>().Should().BeFalse();
            _ = stored["Volume"]!.GetValue<double>().Should().Be(1, "clamped to the row's maximum");
            _ = stored["Theme"]!.GetValue<string>().Should().Be("light");
            _ = stored["Links"]![0]!["Name"]!.GetValue<string>().Should().Be("Docs");
            _ = Schema().Open().Get<bool>("Sound").Should().BeFalse();
        }

        [Fact]
        public void A_value_the_module_wrote_itself_in_another_type_reads_as_the_default()
        {
            Install(Id, "Capability", Page);
            _ = Directory.CreateDirectory(Path.GetDirectoryName(ModuleSettingsStore.PathFor(Id))!);
            File.WriteAllText(ModuleSettingsStore.PathFor(Id), """{ "Sound": "yes", "Custom": 42 }""");

            ModuleSettingsDocument doc = Schema().Open();
            _ = doc.Get<bool>("Sound").Should().BeTrue();

            doc.Set("Theme", "light");
            _ = Stored()["Custom"]!.GetValue<int>().Should().Be(42, "keys the page does not show are kept");
        }

        [Fact]
        public void Rows_the_Hub_cannot_draw_are_skipped_not_fatal()
        {
            Install(Id, "Capability", """
                [
                  { "Section": "Mixed", "Rows": [
                    { "Key": "Ok", "Kind": "Toggle", "Label": "Fine" },
                    { "Key": "Dial", "Kind": "Knob", "Label": "Unknown kind" },
                    { "Key": "Look", "Kind": "Custom", "Label": "A Hub control" },
                    { "Kind": "Toggle", "Label": "No key" },
                    { "Key": "Ok", "Kind": "Toggle", "Label": "Same key again" },
                    { "Key": "Pick", "Kind": "Choice", "Label": "No options" },
                    { "Key": "Count", "Kind": "Number", "Label": "No range" },
                    { "Key": "Style", "Kind": "Toggle", "Label": "Takes the style's key" },
                    { "Key": "Lost", "Kind": "Toggle", "Label": "Shown when nothing", "ShownWhen": "Missing" }
                  ] },
                  { "Section": "Empty", "Rows": [ { "Key": "X", "Kind": "Knob" } ] }
                ]
                """);

            ModuleSettingsSchema schema = Schema();

            _ = schema.Rows.Select(r => r.Key).Should().Equal("Ok", "Lost");
            _ = schema.Row("Ok").Label.Should().Be("Fine");
            _ = schema.Row("Lost").VisibleWhen.Should().BeNull("a row is never hidden behind a switch that does not exist");
            _ = schema.Sections.Select(s => s.Title).Should().Equal("Mixed");
        }

        [Fact]
        public void A_manifest_with_styles_but_no_settings_gets_a_page_with_its_style()
        {
            string path = ModuleManifest.PathInModule("Styled");
            _ = Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, """{ "Id": "Styled", "Kind": "Widget", "Styles": [ "Plain", "Bold" ], "DefaultStyle": "Bold" }""");

            ModuleSettingsSchema schema = Schema("Styled");

            _ = schema.Rows.Select(r => (r.Key, r.Kind)).Should().Equal(("Style", SettingKind.Style));
            _ = schema.Sections.Single().Title.Should().Be("Look");
            _ = schema.Open().Get<string>("Style").Should().Be("Bold");
        }

        [Fact]
        public void A_page_with_no_drawable_row_is_no_page()
        {
            Install(Id, "Capability", """[ { "Section": "S", "Rows": [ { "Key": "X", "Kind": "Knob" } ] } ]""");

            _ = ModuleSettingsSchemas.TryGet(Id, out _).Should().BeFalse();
        }

        [Fact]
        public void A_built_in_id_keeps_its_own_page_whatever_a_manifest_says()
        {
            Install("Mixdeck", "Capability", """[ { "Section": "S", "Rows": [ { "Key": "X", "Kind": "Toggle" } ] } ]""");

            _ = Schema("Mixdeck").Rows.Should().NotContain(r => r.Key == "X");
        }

        [Theory]
        [InlineData("Widget", SettingApplyEffect.RelaunchWidget)]
        [InlineData("Capability", SettingApplyEffect.ReArm)]
        public void Without_an_Apply_a_row_takes_effect_the_way_its_module_kind_does(string kind, SettingApplyEffect effect)
        {
            Install(Id, kind, Page);

            _ = Schema().Row("Sound").Apply.Should().Be(effect);
            _ = Schema().Row("Shortcut").Apply.Should().Be(SettingApplyEffect.ReArm, "a shortcut always needs a re-arm");
        }

        [Fact]
        public void A_row_label_defaults_to_its_key()
        {
            Install(Id, "Capability", """[ { "Section": "S", "Rows": [ { "Key": "Sound", "Kind": "Toggle" } ] } ]""");

            _ = Schema().Row("Sound").Label.Should().Be("Sound");
        }

        [Fact]
        public void The_store_keeps_its_JSON_shape_for_the_module_to_read()
        {
            Install(Id, "Capability", Page);
            Schema().Open().Set("Sound", false);

            Dictionary<string, JsonElement>? plain = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
                File.ReadAllText(ModuleSettingsStore.PathFor(Id)));

            _ = plain.Should().ContainKey("Sound");
            _ = plain!["Sound"].ValueKind.Should().Be(JsonValueKind.False);
        }
    }
}
