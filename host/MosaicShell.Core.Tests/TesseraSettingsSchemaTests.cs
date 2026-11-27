using FluentAssertions;
using MosaicShell.Core.Modules;
using MosaicShell.Core.Modules.Settings;
using MosaicShell.Core.Runtime;
using MosaicShell.Core.Settings;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// Tessera's page as Core describes it: Look first, then Motion, Position, What to show, Timing, Advanced. Its
    /// richer controls (style cards, motion looks, accent, material, the monitor picture, volume keys) are custom slots
    /// the Hub fills, saving through the same document.
    /// </summary>
    public sealed class TesseraSettingsSchemaTests : IDisposable
    {
        private readonly string _root;

        public TesseraSettingsSchemaTests()
        {
            _root = Path.Combine(Path.GetTempPath(), "MosaicTesseraSchema_" + Guid.NewGuid().ToString("N"));
            AppPaths.SetRootOverride(_root);
            AppPaths.EnsureLayout();
        }

        public void Dispose()
        {
            AppPaths.ClearRootOverride();
            try { Directory.Delete(_root, true); } catch { /* ignore */ }
        }

        private static ModuleSettingsSchema Schema => ModuleSettingsSchemas.Tessera;

        private static TesseraSettings Saved => ModuleSettingsStore.Load(ModuleIds.Tessera, () => new TesseraSettings());

        [Fact]
        public void Sections_follow_how_often_they_are_used()
        {
            _ = Schema.Sections.Select(s => s.Title).Should().Equal("Look", "Motion", "Position", "What to show", "Timing", "Advanced");
        }

        [Fact]
        public void The_rich_controls_are_custom_slots_in_page_order()
        {
            _ = Schema.Rows.Where(r => r.Kind == SettingKind.Custom).Select(r => r.CustomId).Should().Equal(
                "TesseraLook", "TesseraAccent", "TesseraMaterial", "TesseraMotion", "TesseraPosition", "TesseraVolumeKeys");
        }

        [Fact]
        public void A_custom_slot_has_no_value_of_its_own()
        {
            ModuleSettingsDocument doc = Schema.Open();

            Action get = () => doc.Get("Style");
            Action set = () => doc.Set("Style", "Fluent");

            _ = get.Should().Throw<InvalidOperationException>();
            _ = set.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void A_custom_slot_saves_through_the_document_and_keeps_the_rest()
        {
            ModuleSettingsStore.Save(ModuleIds.Tessera, new TesseraSettings { AutoDismissMs = 4000 });
            ModuleSettingsDocument doc = Schema.Open();

            doc.Update<TesseraSettings>("Style", s => s.Style = "Windows11");

            _ = Saved.Style.Should().Be("Windows11");
            _ = Saved.AutoDismissMs.Should().Be(4000);
        }

        [Fact]
        public void A_plain_row_never_undoes_what_a_custom_slot_saved()
        {
            ModuleSettingsDocument doc = Schema.Open();
            doc.Update<TesseraSettings>("Style", s => s.Style = "Windows11");

            doc.Set("UseFocusDim", false);

            _ = Saved.Style.Should().Be("Windows11", "each change reads the file it writes");
            _ = Saved.UseFocusDim.Should().BeFalse();
        }

        [Fact]
        public void Only_a_custom_slot_takes_an_update()
        {
            Action update = () => Schema.Open().Update<TesseraSettings>("UseFocusDim", s => s.UseFocusDim = false);

            _ = update.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void Size_is_a_percentage_from_50_to_150()
        {
            ModuleSettingsDocument doc = Schema.Open();
            SettingRow size = Schema.Row("FlyoutScalePercent");

            doc.Set("FlyoutScalePercent", 400);

            _ = (size.Min, size.Max, size.Unit).Should().Be((50d, 150d, "%"));
            _ = Saved.FlyoutScalePercent.Should().Be(150);
        }

        [Fact]
        public void One_distance_from_the_edge_sets_both_insets()
        {
            ModuleSettingsStore.Save(ModuleIds.Tessera, new TesseraSettings { XPad = 12, YPad = 40 });
            ModuleSettingsDocument doc = Schema.Open();
            _ = doc.Get<double>("Inset").Should().Be(12, "it reads the horizontal inset");

            doc.Set("Inset", 30.0);

            _ = (Saved.XPad, Saved.YPad).Should().Be((30, 30));
        }

        [Fact]
        public void How_long_it_stays_is_in_seconds_and_stored_in_milliseconds()
        {
            ModuleSettingsDocument doc = Schema.Open();
            SettingRow staysFor = Schema.Row("StaysFor");

            _ = doc.Get<double>("StaysFor").Should().Be(2);
            doc.Set("StaysFor", 3.25);
            _ = Saved.AutoDismissMs.Should().Be(3250);
            doc.Set("StaysFor", 99.0);
            _ = Saved.AutoDismissMs.Should().Be(20000);
            _ = (staysFor.Min, staysFor.Step, staysFor.Unit).Should().Be((0.5, 0.25, "s"));
        }

        [Theory]
        [InlineData("EnableMediaFlyouts", SettingApplyEffect.ReArm)]
        [InlineData("ShowMediaStripOnVolume", SettingApplyEffect.ReArm)]
        [InlineData("EnableLockFlyouts", SettingApplyEffect.ReArm)]
        [InlineData("EnableFlightFlyouts", SettingApplyEffect.ReArm)]
        [InlineData("VolumeKeys", SettingApplyEffect.ReArm)]
        [InlineData("Style", SettingApplyEffect.Live)]
        [InlineData("Motion", SettingApplyEffect.Live)]
        [InlineData("Position", SettingApplyEffect.Live)]
        [InlineData("StaysFor", SettingApplyEffect.Live)]
        [InlineData("UseFocusDim", SettingApplyEffect.Live)]
        public void Kinds_of_flyout_and_the_key_hook_rearm_and_the_rest_show_on_the_next_flyout(string key, SettingApplyEffect effect)
        {
            _ = Schema.Row(key).Apply.Should().Be(effect);
        }

        [Fact]
        public void The_what_to_show_switches_read_and_write_their_settings()
        {
            ModuleSettingsDocument doc = Schema.Open();

            doc.Set("EnableLockFlyouts", false);
            doc.Set("ShowMediaStripOnVolume", false);

            _ = Saved.EnableLockFlyouts.Should().BeFalse();
            _ = Saved.ShowMediaStripOnVolume.Should().BeFalse();
            _ = doc.Get<bool>("EnableMediaFlyouts").Should().BeTrue();
        }

        [Fact]
        public void Tessera_stays_off_the_generic_list_until_its_slots_exist()
        {
            _ = ModuleSettingsSchemas.TryGet(ModuleIds.Tessera, out _).Should().BeFalse();
            _ = ModuleSettingsSchemas.Tessera.ModuleId.Should().Be(ModuleIds.Tessera);
        }
    }
}
