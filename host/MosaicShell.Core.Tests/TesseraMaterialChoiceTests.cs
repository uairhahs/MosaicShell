using FluentAssertions;
using MosaicShell.Core.Capabilities.Platform;
using MosaicShell.Core.Modules.Tessera;
using MosaicShell.Core.Settings;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// One Hub choice for the flyout material, replacing three checkboxes that looked alike: "Soft frost tint",
    /// "Skia backdrop blur" (simulated; it never sampled the desktop) and "Native Translucency". The saved settings
    /// keep their three keys, so older files and the flyout code read them unchanged.
    /// </summary>
    public sealed class TesseraMaterialChoiceTests : IDisposable
    {
        public void Dispose()
        {
            NativeTranslucencyPlatform.ResetForTests();
        }

        [Theory]
        [InlineData(false, false, true, TesseraMaterialChoice.Solid)]
        [InlineData(false, true, true, TesseraMaterialChoice.Solid)]
        [InlineData(true, false, true, TesseraMaterialChoice.Frosted)]
        [InlineData(true, true, true, TesseraMaterialChoice.NativeTranslucency)]
        [InlineData(true, true, false, TesseraMaterialChoice.Frosted)]
        public void Saved_settings_read_as_one_choice(
            bool frost, bool native, bool nativeOffered, TesseraMaterialChoice expected)
        {
            TesseraSettings s = new() { UseAcrylicBackdrop = frost, UseNativeTranslucency = native };

            _ = TesseraMaterialChoicePolicy.FromSettings(s, nativeOffered).Should().Be(expected);
        }

        [Theory]
        [InlineData(TesseraMaterialChoice.Solid, false, false)]
        [InlineData(TesseraMaterialChoice.Frosted, true, false)]
        [InlineData(TesseraMaterialChoice.NativeTranslucency, true, true)]
        public void A_choice_saves_the_settings_the_flyouts_read(
            TesseraMaterialChoice choice, bool frost, bool native)
        {
            TesseraSettings s = new() { UseBackdropBlur = false };

            TesseraMaterialChoicePolicy.Apply(s, choice);

            _ = s.UseAcrylicBackdrop.Should().Be(frost);
            _ = s.UseNativeTranslucency.Should().Be(native);
            _ = s.UseBackdropBlur.Should().BeTrue("the simulated blur has no control of its own any more; its default holds");
            _ = TesseraMaterialChoicePolicy.FromSettings(s, nativeOffered: true).Should().Be(choice);
        }

        [Theory]
        [InlineData(true, new[] { TesseraMaterialChoice.Solid, TesseraMaterialChoice.Frosted, TesseraMaterialChoice.NativeTranslucency })]
        [InlineData(false, new[] { TesseraMaterialChoice.Solid, TesseraMaterialChoice.Frosted })]
        public void Native_translucency_is_offered_only_where_it_can_be_drawn(
            bool nativeOffered, TesseraMaterialChoice[] expected)
        {
            _ = TesseraMaterialChoicePolicy.Offered(nativeOffered).Should().Equal(expected);
        }

        [Theory]
        [InlineData(NativeTranslucencyBackend.KWinBlur, true)]
        [InlineData(NativeTranslucencyBackend.None, false)]
        public void Kde_offers_native_translucency_in_the_hub_once_kwin_blur_is_found(
            NativeTranslucencyBackend backend, bool expected)
        {
            NativeTranslucencyPlatform.Set(backend);

            _ = TesseraNativeTranslucencySignOffPolicy.KdeEvalSignedOff.Should().BeTrue(
                "the Hub choice is on for KDE (2026-11-22) so the look can be compared there");
            if (!OperatingSystem.IsWindows())
            {
                _ = TesseraNativeTranslucencySignOffPolicy.HubOffersNativeTranslucency.Should().Be(expected);
            }
        }

        [Fact]
        public void The_backend_announces_a_change_so_the_hub_can_show_the_choice_late()
        {
            int changes = 0;
            void OnChanged(object? sender, EventArgs e)
            {
                changes++;
            }

            NativeTranslucencyPlatform.Changed += OnChanged;
            try
            {
                NativeTranslucencyPlatform.Set(NativeTranslucencyBackend.KWinBlur);
                NativeTranslucencyPlatform.Set(NativeTranslucencyBackend.KWinBlur);
            }
            finally
            {
                NativeTranslucencyPlatform.Changed -= OnChanged;
            }

            _ = changes.Should().Be(1, "setting the same backend again is not a change");
        }

        public static TheoryData<string> AllStyles =>
        [
            "MaterialYou", "Fluent", "Windows11", "Gnome", "CoreUI", "ModernFlyouts", "Meter", "Square", "Compact", "PlainText", "Radial",
        ];

        [Theory]
        [InlineData("MaterialYou", false)]
        [InlineData("PlainText", false)]
        [InlineData("Fluent", true)]
        [InlineData("ModernFlyouts", true)]
        [InlineData("Meter", true)]
        public void Material_is_selectable_only_for_styles_it_changes(string styleId, bool expected)
        {
            _ = TesseraMaterialChoicePolicy.IsSelectable(styleId).Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(AllStyles))]
        public void A_style_the_material_cannot_change_draws_the_same_for_every_choice(string styleId)
        {
            // Ties the Hub rule to what the flyouts draw: where the choice is selectable Solid and Frosted differ, and
            // where it is not every choice produces one material.
            TesseraFlyoutMaterial solid = TesseraFlyoutMaterialFactory.FromPayload(new Dictionary<string, string> { ["acrylic"] = "0" }, styleId, "vol");
            TesseraFlyoutMaterial frosted = TesseraFlyoutMaterialFactory.FromPayload(new Dictionary<string, string> { ["acrylic"] = "1" }, styleId, "vol");

            _ = (!SameMaterial(solid, frosted)).Should().Be(TesseraMaterialChoicePolicy.IsSelectable(styleId), styleId);
        }

        private static bool SameMaterial(TesseraFlyoutMaterial a, TesseraFlyoutMaterial b)
        {
            // Field by field: the record compares TransparencyHints by reference.
            return a.UseSoftFrost == b.UseSoftFrost
                && a.ShellAlpha == b.ShellAlpha
                && a.UseEdgeBlend == b.UseEdgeBlend
                && a.UseNativeTranslucency == b.UseNativeTranslucency
                && a.ShouldLockClientSize == b.ShouldLockClientSize
                && a.TransparencyHints.SequenceEqual(b.TransparencyHints);
        }
    }
}
