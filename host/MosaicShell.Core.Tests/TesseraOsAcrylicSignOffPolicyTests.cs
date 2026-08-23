using FluentAssertions;
using MosaicShell.Core;
using MosaicShell.Core.Capabilities.Platform;
using MosaicShell.Core.Modules.Tessera;
using MosaicShell.Core.Runtime;
using MosaicShell.Core.Settings;
using MosaicShell.Core.Styles;

namespace MosaicShell.Core.Tests;

public class TesseraOsAcrylicSignOffPolicyTests : IDisposable
{
    private readonly string _root;

    public TesseraOsAcrylicSignOffPolicyTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "MosaicOsAcrylicSignOff_" + Guid.NewGuid().ToString("N"));
        AppPaths.SetRootOverride(_root);
        AppPaths.EnsureLayout();
        ModuleSettingsStore.Save("Tessera", new TesseraSettings());
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

    [Fact]
    public void Win11_os_acrylic_eval_is_signed_off_in_core()
    {
        TesseraOsAcrylicSignOffPolicy.Win11EvalComplete.Should().BeTrue();
        TesseraOsAcrylicSignOffPolicy.H2Win11EvalSignedOff.Should().BeTrue();
        TesseraOsAcrylicSignOffPolicy.H3StackedVolumeMediaSignedOff.Should().BeTrue();
        TesseraOsAcrylicSignOffPolicy.AllH3StackedStylesSignedOff().Should().BeTrue();
        HostLaunchOptions.TesseraOsAcrylicTrial.Should().BeFalse(
            "sign-off does not flip alpha default; frost stays unless flag or setting is on");
        TesseraOsAcrylicTrialPolicy.IsTrialRequested().Should().BeFalse();
    }

    [Theory]
    [InlineData(StyleIds.Meter)]
    [InlineData(StyleIds.Gnome)]
    [InlineData(StyleIds.Compact)]
    [InlineData(StyleIds.ModernFlyouts)]
    public void H3_stacked_styles_are_signed_off(string styleId)
    {
        TesseraStackedPlacementPolicy.SupportsStackedOsAcrylic(styleId).Should().BeTrue();
        TesseraOsAcrylicSignOffPolicy.IsH3StackedStyleSignedOff(styleId).Should().BeTrue();
    }

    [Theory]
    [InlineData(StyleIds.Fluent)]
    [InlineData(StyleIds.Windows11)]
    [InlineData(StyleIds.MaterialYou)]
    public void Unified_shell_styles_stay_on_single_hwnd_path_not_h3_split(string styleId)
    {
        TesseraStackedPlacementPolicy.SupportsStackedOsAcrylic(styleId).Should().BeFalse();
        TesseraOsAcrylicSignOffPolicy.IsH3StackedStyleSignedOff(styleId).Should().BeFalse();
    }
}
