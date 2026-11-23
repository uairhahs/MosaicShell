using FluentAssertions;
using MosaicShell.Core.Capabilities;
using MosaicShell.Core.Modules;
using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// The structure key is what a built flyout was made from. A request whose key equals the built one may be
    /// patched in place; any other must rebuild. It exists so a patch cannot leave the window describing a structure
    /// the content does not have (audit F02). Values that change on every tick are deliberately not in it.
    /// </summary>
    public sealed class FlyoutStructureKeyTests
    {
        private static FlyoutRequest Request(
            string kind = "vol",
            string? style = "Fluent",
            Dictionary<string, string>? payload = null,
            int ani = 2,
            int autoDismissMs = 2500,
            int monitorIndex = 1)
        {
            return new FlyoutRequest(
                ModuleIds.Tessera, kind, style, Payload: payload, Ani: ani, AutoDismissMs: autoDismissMs, MonitorIndex: monitorIndex);
        }

        private static FlyoutStructureKey KeyOf(FlyoutRequest request, TesseraFlyoutSessionMode topology = TesseraFlyoutSessionMode.Single)
        {
            return FlyoutStructureKey.From(request, topology);
        }

        [Fact]
        public void The_same_request_has_the_same_key()
        {
            _ = KeyOf(Request()).Should().Be(KeyOf(Request()));
            _ = KeyOf(Request()).GetHashCode().Should().Be(KeyOf(Request()).GetHashCode());
        }

        [Theory]
        [InlineData("volume", "10")]
        [InlineData("muted", "1")]
        [InlineData("mediaTitle", "Another track")]
        [InlineData("mediaArtist", "Another artist")]
        [InlineData("mediaPlaying", "1")]
        [InlineData("lock", "NumLock")]
        [InlineData("on", "1")]
        public void A_value_that_changes_on_every_tick_does_not_change_the_key(string name, string value)
        {
            FlyoutRequest before = Request(payload: new() { ["volume"] = "0.5", ["mediaTitle"] = "A", ["on"] = "0" });
            FlyoutRequest after = Request(payload: new() { ["volume"] = "0.5", ["mediaTitle"] = "A", ["on"] = "0", [name] = value });

            _ = KeyOf(after).Should().Be(KeyOf(before));
        }

        [Fact]
        public void Switching_native_translucency_must_rebuild()
        {
            // A KWin window gets its blur region and an AcrylicBlur window its hint only when built; a patch kept the
            // old material, so every Hub material looked the same (2026-11-22).
            FlyoutStructureKey built = KeyOf(Request());
            FlyoutStructureKey next = built with { NativeTranslucency = !built.NativeTranslucency };

            _ = next.CanPatch(built).Should().BeFalse();
            _ = next.DifferingFields(built).Should().Equal("nativeTranslucency");
        }

        [Fact]
        public void Motion_dismissal_and_monitor_are_not_structure()
        {
            _ = KeyOf(Request(ani: 0, autoDismissMs: 9000, monitorIndex: 2)).Should().Be(KeyOf(Request()));
        }

        [Fact]
        public void A_changed_media_strip_is_a_different_structure_so_it_is_not_a_patch()
        {
            FlyoutStructureKey withStrip = KeyOf(Request(payload: new() { ["showMediaStrip"] = "1" }));
            FlyoutStructureKey without = KeyOf(Request(payload: new() { ["showMediaStrip"] = "0" }));

            _ = withStrip.Should().NotBe(without);
            _ = withStrip.CanPatch(builtFrom: without).Should().BeFalse();
        }

        [Fact]
        public void An_equal_key_can_patch()
        {
            _ = KeyOf(Request()).CanPatch(builtFrom: KeyOf(Request())).Should().BeTrue();
        }

        [Fact]
        public void Each_structural_input_changes_the_key()
        {
            FlyoutStructureKey baseline = KeyOf(Request());

            _ = KeyOf(Request(kind: "media")).Should().NotBe(baseline);
            _ = KeyOf(Request(style: "Windows11")).Should().NotBe(baseline);
            _ = KeyOf(Request(payload: new() { ["flyoutScale"] = "125" })).Should().NotBe(baseline);
            _ = KeyOf(Request(payload: new() { ["acrylic"] = "0" })).Should().NotBe(baseline);
            _ = KeyOf(Request(payload: new() { ["backdropBlur"] = "0" })).Should().NotBe(baseline);
            _ = KeyOf(Request(payload: new() { ["accent"] = "#FF8800" })).Should().NotBe(baseline);
            _ = KeyOf(Request(), TesseraFlyoutSessionMode.Stacked).Should().NotBe(baseline);
        }

        [Fact]
        public void A_missing_payload_means_the_defaults_so_it_equals_a_payload_that_states_them()
        {
            FlyoutRequest explicitDefaults = Request(payload: new()
            {
                ["showMediaStrip"] = "0",
                ["flyoutScale"] = "100",
                ["acrylic"] = "1",
                ["backdropBlur"] = "1",
                ["accent"] = "",
            });

            _ = KeyOf(Request()).Should().Be(KeyOf(explicitDefaults));
        }

        [Fact]
        public void The_legacy_baked_frost_key_means_the_same_as_backdrop_blur()
        {
            _ = KeyOf(Request(payload: new() { ["bakedFrost"] = "0" }))
                .Should().Be(KeyOf(Request(payload: new() { ["backdropBlur"] = "0" })));
        }

        [Fact]
        public void Spelling_that_the_readers_already_treat_alike_does_not_split_the_key()
        {
            _ = KeyOf(Request(style: "fluent", kind: "VOL")).Should().Be(KeyOf(Request(style: "Fluent", kind: "vol")));
            _ = KeyOf(Request(payload: new() { ["accent"] = "#ff8800" })).Should().Be(KeyOf(Request(payload: new() { ["accent"] = "#FF8800" })));
        }

        [Fact]
        public void The_scale_is_clamped_the_way_the_builder_clamps_it()
        {
            _ = KeyOf(Request(payload: new() { ["flyoutScale"] = "999" })).Should().Be(KeyOf(Request(payload: new() { ["flyoutScale"] = "150" })));
            _ = KeyOf(Request(payload: new() { ["flyoutScale"] = "nonsense" })).Should().Be(KeyOf(Request()));
        }

        [Fact]
        public void The_differing_fields_are_named_in_a_fixed_order_for_the_log()
        {
            // Every field is flipped by hand, not derived from a request: NativeTranslucency follows the process's
            // backend, which is WindowsAcrylic on Windows and None on Linux, so a request-built pair differs by OS.
            FlyoutStructureKey built = KeyOf(Request());
            FlyoutStructureKey next = built with
            {
                Kind = "brightness",
                Style = "windows11",
                ScalePercent = built.ScalePercent + 25,
                ShowMedia = !built.ShowMedia,
                Topology = TesseraFlyoutSessionMode.Stacked,
                Acrylic = !built.Acrylic,
                BackdropBlur = !built.BackdropBlur,
                Accent = "#00ff00",
                NativeTranslucency = !built.NativeTranslucency,
            };

            _ = next.DifferingFields(built).Should().Equal(
                "kind", "style", "scale", "showMedia", "topology", "acrylic", "backdropBlur", "accent", "nativeTranslucency");
            _ = built.DifferingFields(built).Should().BeEmpty();
        }
    }
}
