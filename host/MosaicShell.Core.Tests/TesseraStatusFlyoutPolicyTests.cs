using FluentAssertions;
using MosaicShell.Core;
using MosaicShell.Core.Capabilities.Platform;
using MosaicShell.Core.Modules.Tessera;
using MosaicShell.Core.Runtime;
using MosaicShell.Core.Services;
using MosaicShell.Core.Settings;
using MosaicShell.Core.Styles;

namespace MosaicShell.Core.Tests;

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
        HostLaunchOptions.Apply(Array.Empty<string>());
    }

    public void Dispose()
    {
        HostLaunchOptions.ResetForTests();
        HostLaunchOptions.Apply(Array.Empty<string>());
        AppPaths.ClearRootOverride();
        try { Directory.Delete(_root, true); } catch { /* ignore */ }
    }

    [Theory]
    [InlineData("locks", true)]
    [InlineData("flight", true)]
    [InlineData("vol", false)]
    [InlineData("media", false)]
    public void Status_kinds_recognized(string kind, bool expected) =>
        TesseraStatusFlyoutPolicy.IsStatusKind(kind).Should().Be(expected);

    [Theory]
    [InlineData("locks")]
    [InlineData("flight")]
    public void Status_kinds_never_use_stacked_os_acrylic(string kind)
    {
        TesseraStatusFlyoutPolicy.MustUseDedicatedSingleWindow(kind).Should().BeTrue();

        var stacked = new Dictionary<string, string> { ["showMediaStrip"] = "1", ["acrylic"] = "1" };
        TesseraOsAcrylicStackedPolicy
            .UseMultiWindow(stacked, StyleIds.ModernFlyouts, trialRequested: true, osSupportsWinUiAcrylic: true, kind: kind)
            .Should().BeFalse("caps/airplane must not fork into volume+media HWNDs");
        TesseraOsAcrylicStackedPolicy
            .UseMultiWindow(stacked, StyleIds.ModernFlyouts, trialRequested: true, osSupportsWinUiAcrylic: true, kind: "vol")
            .Should().BeTrue();
    }

    [Fact]
    public void Status_request_payload_clears_media_strip()
    {
        var settings = new TesseraSettings
        {
            Style = StyleIds.ModernFlyouts,
            ShowMediaStripOnVolume = true,
            UseOsAcrylic = true
        };
        var services = HostServicesFakes.Create();
        var builder = new TesseraFlyoutRequestBuilder();
        var locks = builder.Build(services, settings, "locks",
            new Dictionary<string, string> { ["lock"] = "CapsLock", ["on"] = "1" });
        locks.Payload!["showMediaStrip"].Should().Be("0");
        TesseraOsAcrylicStackedPolicy.UseMultiWindowFromPayload(locks.Payload, locks.StyleId, locks.Kind)
            .Should().BeFalse();

        var vol = builder.Build(services, settings, "vol");
        vol.Payload!["showMediaStrip"].Should().Be("1");
    }

    [Fact]
    public void Status_shell_is_content_sized_not_volume_sized()
    {
        TesseraStatusFlyoutPolicy.ForbidFixedVolumeShellSize.Should().BeTrue();
        TesseraStatusFlyoutPolicy.PreferOsAcrylicEdgeOnlyChrome.Should().BeTrue();
        TesseraStatusFlyoutPolicy.ChipHeightDip.Should().Be(50);
        TesseraStatusFlyoutPolicy.ResolveChipCornerRadiusDip(StyleIds.Windows11).Should().Be(12f);
        TesseraStatusFlyoutPolicy.ResolveChipCornerRadiusDip(StyleIds.Square).Should().Be(24f);
        TesseraStatusFlyoutPolicy.SupersededStatusMustCancelDismissBeforeClose.Should().BeTrue();
    }

    [Fact]
    public void Status_must_round_clip_hwnd_before_reveal_including_softfrost()
    {
        TesseraStatusFlyoutPolicy.MustRoundClipHwndBeforeReveal.Should().BeTrue();
        TesseraStatusFlyoutPolicy.SoftFrostMustRoundClipHwnd.Should().BeTrue();
        TesseraStatusFlyoutPolicy.RoundClipMustApplySynchronouslyWhenHandleReady.Should().BeTrue();
    }

    [Fact]
    public void Media_to_status_must_not_keep_stale_media_bounds()
    {
        TesseraStatusFlyoutPolicy.StatusLayoutMustPreferDesiredSizeOverStaleBounds.Should().BeTrue();
        TesseraStatusFlyoutPolicy.StackedToStatusMustHideSlotsBeforeStatusReveal.Should().BeTrue();
        TesseraStatusFlyoutPolicy.MustRecreateHwndAfterMediaShell.Should().BeTrue();
        TesseraStatusFlyoutPolicy
            .MustRecreateHwndAfterMediaShellKind("vol", "locks")
            .Should().BeTrue();
        TesseraStatusFlyoutPolicy
            .MustRecreateHwndAfterMediaShellKind("locks", "locks")
            .Should().BeFalse();

        TesseraStatusFlyoutPolicy.IsStaleMediaShellMeasure(320, 190).Should().BeTrue();
        TesseraStatusFlyoutPolicy.IsStaleMediaShellMeasure(168, 50).Should().BeFalse();

        var (w, h) = TesseraStatusFlyoutPolicy.ResolveStatusClientSizeDip(
            boundsWidth: 320, boundsHeight: 190, desiredWidth: 320, desiredHeight: 190);
        w.Should().BeLessThanOrEqualTo(TesseraStatusFlyoutPolicy.ChipMaxWidthDip);
        h.Should().BeLessThanOrEqualTo(TesseraStatusFlyoutPolicy.ChipMaxHeightDip);
    }
}
