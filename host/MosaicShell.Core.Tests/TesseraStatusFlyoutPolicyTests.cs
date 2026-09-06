using FluentAssertions;
using MosaicShell.Core.Capabilities;
using MosaicShell.Core.Capabilities.Platform;
using MosaicShell.Core.Modules.Tessera;
using MosaicShell.Core.Runtime;
using MosaicShell.Core.Services;
using MosaicShell.Core.Settings;
using MosaicShell.Core.Styles;

namespace MosaicShell.Core.Tests
{
    public class TesseraStatusFlyoutPolicyTests : IDisposable
    {
        private readonly string _root;

        public TesseraStatusFlyoutPolicyTests()
        {
            _root = Path.Combine(Path.GetTempPath(), "MosaicStatusFlyout_" + Guid.NewGuid().ToString("N"));
            AppPaths.SetRootOverride(_root);
            AppPaths.EnsureLayout();
            ModuleSettingsStore.Save("Tessera", new TesseraSettings { UseOsAcrylic = true, ShowMediaStripOnVolume = true });
            HostLaunchOptions.ResetForTests();
            HostLaunchOptions.Apply([]);
        }

        public void Dispose()
        {
            HostLaunchOptions.ResetForTests();
            HostLaunchOptions.Apply([]);
            AppPaths.ClearRootOverride();
            try { Directory.Delete(_root, true); } catch { /* ignore */ }
        }

        [Theory]
        [InlineData("locks", true)]
        [InlineData("flight", true)]
        [InlineData("vol", false)]
        [InlineData("media", false)]
        public void Status_kinds_recognized(string kind, bool expected)
        {
            _ = TesseraStatusFlyoutPolicy.IsStatusKind(kind).Should().Be(expected);
        }

        [Theory]
        [InlineData("locks")]
        [InlineData("flight")]
        public void Status_kinds_never_use_stacked_os_acrylic(string kind)
        {
            _ = TesseraStatusFlyoutPolicy.MustUseDedicatedSingleWindow(kind).Should().BeTrue();

            Dictionary<string, string> stacked = new() { ["showMediaStrip"] = "1", ["acrylic"] = "1" };
            _ = TesseraOsAcrylicStackedPolicy
                .UseMultiWindow(stacked, StyleIds.ModernFlyouts, trialRequested: true, osSupportsWinUiAcrylic: true, kind: kind)
                .Should().BeFalse("caps/airplane must not fork into volume+media HWNDs");
            _ = TesseraOsAcrylicStackedPolicy
                .UseMultiWindow(stacked, StyleIds.ModernFlyouts, trialRequested: true, osSupportsWinUiAcrylic: true, kind: "vol")
                .Should().BeTrue();
        }

        [Fact]
        public void Status_request_payload_clears_media_strip()
        {
            TesseraSettings settings = new()
            {
                Style = StyleIds.ModernFlyouts,
                ShowMediaStripOnVolume = true,
                UseOsAcrylic = true
            };
            HostServices services = HostServicesFakes.Create();
            TesseraFlyoutRequestBuilder builder = new();
            FlyoutRequest locks = builder.Build(services, settings, "locks",
                new Dictionary<string, string> { ["lock"] = "CapsLock", ["on"] = "1" });
            _ = locks.Payload!["showMediaStrip"].Should().Be("0");
            _ = TesseraOsAcrylicStackedPolicy.UseMultiWindowFromPayload(locks.Payload, locks.StyleId, locks.Kind)
                .Should().BeFalse();

            FlyoutRequest vol = builder.Build(services, settings, "vol");
            _ = vol.Payload!["showMediaStrip"].Should().Be("1");
        }

        [Fact]
        public void Status_shell_is_content_sized_not_volume_sized()
        {
            _ = TesseraStatusFlyoutPolicy.ForbidFixedVolumeShellSize.Should().BeTrue();
            _ = TesseraStatusFlyoutPolicy.PreferOsAcrylicEdgeOnlyChrome.Should().BeTrue();
            _ = TesseraStatusFlyoutPolicy.ChipHeightDip.Should().Be(50);
            _ = TesseraStatusFlyoutPolicy.ResolveChipCornerRadiusDip(StyleIds.Windows11).Should().Be(12f);
            _ = TesseraStatusFlyoutPolicy.ResolveChipCornerRadiusDip(StyleIds.Square).Should().Be(24f);
            _ = TesseraStatusFlyoutPolicy.SupersededStatusMustCancelDismissBeforeClose.Should().BeTrue();
        }

        [Fact]
        public void Status_must_round_clip_hwnd_before_reveal_including_softfrost()
        {
            _ = TesseraStatusFlyoutPolicy.MustRoundClipHwndBeforeReveal.Should().BeTrue();
            _ = TesseraStatusFlyoutPolicy.SoftFrostMustRoundClipHwnd.Should().BeTrue();
            _ = TesseraStatusFlyoutPolicy.RoundClipMustApplySynchronouslyWhenHandleReady.Should().BeTrue();
        }

        [Fact]
        public void Media_to_status_must_not_keep_stale_media_bounds()
        {
            _ = TesseraStatusFlyoutPolicy.StatusLayoutMustPreferDesiredSizeOverStaleBounds.Should().BeTrue();
            _ = TesseraStatusFlyoutPolicy.StackedToStatusMustHideSlotsBeforeStatusReveal.Should().BeTrue();
            _ = TesseraStatusFlyoutPolicy.MustRecreateHwndAfterMediaShell.Should().BeTrue();
            _ = TesseraStatusFlyoutPolicy
                .MustRecreateHwndAfterMediaShellKind("vol", "locks")
                .Should().BeTrue();
            _ = TesseraStatusFlyoutPolicy
                .MustRecreateHwndAfterMediaShellKind("locks", "locks")
                .Should().BeFalse();

            _ = TesseraStatusFlyoutPolicy.IsStaleMediaShellMeasure(320, 190).Should().BeTrue();
            _ = TesseraStatusFlyoutPolicy.IsStaleMediaShellMeasure(168, 50).Should().BeFalse();

            (double w, double h) = TesseraStatusFlyoutPolicy.ResolveStatusClientSizeDip(
                boundsWidth: 320, boundsHeight: 190, desiredWidth: 320, desiredHeight: 190);
            _ = w.Should().BeLessThanOrEqualTo(TesseraStatusFlyoutPolicy.ChipMaxWidthDip);
            _ = h.Should().BeLessThanOrEqualTo(TesseraStatusFlyoutPolicy.ChipMaxHeightDip);
        }
    }
}
