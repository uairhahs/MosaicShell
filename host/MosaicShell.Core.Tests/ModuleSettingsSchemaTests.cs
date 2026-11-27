using FluentAssertions;
using MosaicShell.Core.HostPlatform;
using MosaicShell.Core.Modules.Settings;
using MosaicShell.Core.Runtime;
using MosaicShell.Core.Settings;

namespace MosaicShell.Core.Tests
{
    /// <summary>Each module page as Core describes it, read and written against a private settings root.</summary>
    public class ModuleSettingsSchemaTests : IDisposable
    {
        private readonly string _root;

        public ModuleSettingsSchemaTests()
        {
            _root = Path.Combine(Path.GetTempPath(), "MosaicSchema_" + Guid.NewGuid().ToString("N"));
            AppPaths.SetRootOverride(_root);
            AppPaths.EnsureLayout();
        }

        public void Dispose()
        {
            AppPaths.ClearRootOverride();
            try { Directory.Delete(_root, true); } catch { /* ignore */ }
        }

        public static TheoryData<string> GenericModules => [.. ModuleSettingsSchemas.GenericModuleIds];

        [Fact]
        public void Every_built_in_module_but_Tessera_has_a_generic_page()
        {
            _ = ModuleSettingsSchemas.GenericModuleIds.Should().BeEquivalentTo(
                ["Mixdeck", "Inlay", "Slate", "Chord", "Substrate", "Chrono", "Phono", "Pulse", "Canvas"]);
            _ = ModuleSettingsSchemas.TryGet("Tessera", out _).Should().BeFalse();
        }

        [Theory]
        [InlineData("mixdeck")]
        [InlineData("CHRONO")]
        public void Ids_match_whatever_their_case(string moduleId)
        {
            _ = ModuleSettingsSchemas.TryGet(moduleId, out ModuleSettingsSchema? schema).Should().BeTrue();
            _ = schema!.ModuleId.Should().BeOneOf("Mixdeck", "Chrono");
        }

        [Theory]
        [MemberData(nameof(GenericModules))]
        public void Row_keys_are_unique_and_every_section_has_rows(string moduleId)
        {
            ModuleSettingsSchema schema = Schema(moduleId);

            _ = schema.Sections.Should().NotBeEmpty().And.OnlyContain(s => s.Rows.Count > 0);
            _ = schema.Rows.Select(r => r.Key).Should().OnlyHaveUniqueItems();
        }

        [Theory]
        [MemberData(nameof(GenericModules))]
        public void A_row_shown_only_when_another_is_on_names_a_toggle_of_the_same_page(string moduleId)
        {
            ModuleSettingsSchema schema = Schema(moduleId);

            foreach (SettingRow row in schema.Rows.Where(r => r.VisibleWhen is not null))
            {
                _ = schema.Row(row.VisibleWhen!).Kind.Should().Be(SettingKind.Toggle);
            }
        }

        [Theory]
        [MemberData(nameof(GenericModules))]
        public void Every_row_reads_without_a_settings_file(string moduleId)
        {
            ModuleSettingsDocument doc = Schema(moduleId).Open();

            foreach (SettingRow row in doc.Schema.Rows)
            {
                _ = doc.Get(row.Key).Should().NotBeNull(row.Key);
            }
        }

        [Theory]
        [MemberData(nameof(GenericModules))]
        public void Every_page_has_a_style_row_with_its_catalog_styles(string moduleId)
        {
            SettingRow style = Schema(moduleId).Rows.Single(r => r.Kind == SettingKind.Style);

            _ = style.Options.Select(o => o.Value).Should().Equal(Styles.StyleCatalog.IdsFor(moduleId));
        }

        [Fact]
        public void Chrono_reads_its_defaults()
        {
            ModuleSettingsDocument doc = Schema("Chrono").Open();

            _ = doc.Get<bool>("ShowSeconds").Should().BeTrue();
            _ = doc.Get<bool>("TwentyFourHour").Should().BeTrue();
            _ = doc.Get<string>("Style").Should().Be("Square");
        }

        [Fact]
        public void A_change_is_saved_at_once_and_reads_back_after_reopening()
        {
            ModuleSettingsDocument doc = Schema("Chrono").Open();

            doc.Set("ShowSeconds", false);

            _ = ModuleSettingsStore.Load("Chrono", () => new ChronoSettings()).ShowSeconds.Should().BeFalse();
            _ = Schema("Chrono").Open().Get<bool>("ShowSeconds").Should().BeFalse();
        }

        [Fact]
        public void A_change_keeps_settings_the_page_does_not_show()
        {
            ModuleSettingsStore.Save("Inlay", new InlaySettings { Pins = ["code"] });

            Schema("Inlay").Open().Set("CloseOnEscape", false);

            InlaySettings saved = ModuleSettingsStore.Load("Inlay", () => new InlaySettings());
            _ = saved.Pins.Should().Equal("code");
            _ = saved.CloseOnEscape.Should().BeFalse();
        }

        [Theory]
        [InlineData(5.0, 30)]
        [InlineData(90.0, 90)]
        [InlineData(99999.0, 3600)]
        public void Slate_idle_time_is_clamped_to_its_range(double asked, int stored)
        {
            ModuleSettingsDocument doc = Schema("Slate").Open();
            SettingRow idle = doc.Schema.Row("IdleSeconds");

            doc.Set("IdleSeconds", asked);

            _ = ModuleSettingsStore.Load("Slate", () => new SlateSettings()).IdleSeconds.Should().Be(stored);
            _ = idle.Min.Should().Be(30);
            _ = idle.Max.Should().Be(3600);
            _ = idle.Unit.Should().Be("s");
        }

        [Fact]
        public void An_unknown_style_falls_back_to_the_modules_default()
        {
            Schema("Phono").Open().Set("Style", "NoSuchStyle");

            _ = ModuleSettingsStore.Load("Phono", () => new PhonoSettings()).Style
                .Should().Be(Styles.StyleCatalog.DefaultFor("Phono"));
        }

        [Fact]
        public void A_reserved_shortcut_is_stored_as_the_modules_safe_default()
        {
            Schema("Mixdeck").Open().Set("HotkeyGesture", "Win+S");

            _ = ModuleSettingsStore.Load("Mixdeck", () => new MixdeckSettings()).HotkeyGesture.Should().Be("Ctrl+Alt+X");
        }

        [Fact]
        public void Mixdeck_colour_schemes_are_the_ones_its_surface_draws()
        {
            SettingRow scheme = Schema("Mixdeck").Row("ColorScheme");

            _ = scheme.Kind.Should().Be(SettingKind.Choice);
            _ = scheme.Options.Select(o => o.Value).Should().Equal(
                "Default", "Dark", "Light", "Accent", "Frost", "Midnight", "Sunset");
        }

        [Fact]
        public void Pulse_visualiser_is_a_choice_of_bars_or_round()
        {
            ModuleSettingsDocument doc = Schema("Pulse").Open();

            _ = doc.Schema.Row("VisualizerType").Options.Select(o => o.Value).Should().Equal("Bar", "Round");
            doc.Set("VisualizerType", "Round");
            _ = ModuleSettingsStore.Load("Pulse", () => new PulseSettings()).VisualizerType.Should().Be("Round");
        }

        [Fact]
        public void A_choice_outside_its_options_is_refused()
        {
            ModuleSettingsDocument doc = Schema("Pulse").Open();

            Action set = () => doc.Set("VisualizerType", "Spiral");

            _ = set.Should().Throw<ArgumentException>();
            _ = ModuleSettingsStore.Load("Pulse", () => new PulseSettings()).VisualizerType.Should().Be("Bar");
        }

        [Fact]
        public void Inlay_pins_are_a_list_without_blanks_or_repeats_and_never_empty()
        {
            ModuleSettingsDocument doc = Schema("Inlay").Open();

            doc.Set("Pins", (IReadOnlyList<string>)["code", " ", "Code", "firefox"]);
            _ = ModuleSettingsStore.Load("Inlay", () => new InlaySettings()).Pins.Should().Equal("code", "firefox");

            doc.Set("Pins", (IReadOnlyList<string>)[]);
            _ = ModuleSettingsStore.Load("Inlay", () => new InlaySettings()).Pins.Should().Equal("notepad", "calc");
        }

        [Fact]
        public void Chord_actions_drop_rows_without_a_target_and_name_unnamed_ones_after_it()
        {
            Schema("Chord").Open().Set("Actions", (IReadOnlyList<SettingPair>)
            [
                new("", "firefox"),
                new("Mail", ""),
                new("Term", "konsole"),
            ]);

            _ = ModuleSettingsStore.Load("Chord", () => new ChordSettings()).Actions
                .Select(a => (a.Name, a.Target)).Should().Equal(("firefox", "firefox"), ("Term", "konsole"));
        }

        [Fact]
        public void A_value_of_the_wrong_type_is_refused()
        {
            ModuleSettingsDocument doc = Schema("Chrono").Open();

            Action set = () => doc.Set("ShowSeconds", "yes");

            _ = set.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void An_unknown_key_is_refused()
        {
            Action row = () => Schema("Chrono").Row("NoSuchRow");

            _ = row.Should().Throw<KeyNotFoundException>();
        }

        [Theory]
        [InlineData("Chrono", "ShowSeconds", SettingApplyEffect.RelaunchWidget)]
        [InlineData("Canvas", "ShowCpu", SettingApplyEffect.RelaunchWidget)]
        [InlineData("Slate", "IdleSeconds", SettingApplyEffect.ReArm)]
        [InlineData("Slate", "HideOnFullscreen", SettingApplyEffect.ReArm)]
        [InlineData("Mixdeck", "HotkeyGesture", SettingApplyEffect.ReArm)]
        [InlineData("Mixdeck", "ColorScheme", SettingApplyEffect.Live)]
        [InlineData("Inlay", "Pins", SettingApplyEffect.Live)]
        [InlineData("Substrate", "CloseOnEscape", SettingApplyEffect.Live)]
        public void Each_row_says_when_its_change_takes_effect(string moduleId, string key, SettingApplyEffect effect)
        {
            _ = Schema(moduleId).Row(key).Apply.Should().Be(effect);
        }

        [Fact]
        public void Slate_hide_on_full_screen_needs_the_full_screen_probe()
        {
            _ = Schema("Slate").Row("HideOnFullscreen").Requires.Should().Be(DesktopFeature.FullscreenProbe);
        }

        [Theory]
        [InlineData("Mixdeck")]
        [InlineData("Inlay")]
        [InlineData("Chord")]
        [InlineData("Substrate")]
        public void Shortcut_rows_need_global_hotkeys(string moduleId)
        {
            SettingRow hotkey = Schema(moduleId).Rows.Single(r => r.Kind == SettingKind.Hotkey);

            _ = hotkey.Requires.Should().Be(DesktopFeature.GlobalHotkeys);
        }

        private static ModuleSettingsSchema Schema(string moduleId)
        {
            _ = ModuleSettingsSchemas.TryGet(moduleId, out ModuleSettingsSchema? schema).Should().BeTrue(moduleId);
            return schema!;
        }
    }
}
