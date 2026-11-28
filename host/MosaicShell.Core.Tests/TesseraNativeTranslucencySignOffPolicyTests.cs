using FluentAssertions;
using MosaicShell.Core.Capabilities.Platform;
using MosaicShell.Core.Modules.Tessera;
using MosaicShell.Core.Runtime;
using MosaicShell.Core.Settings;
using MosaicShell.Core.Styles;

namespace MosaicShell.Core.Tests
{
    public class TesseraNativeTranslucencySignOffPolicyTests : IDisposable
    {
        private readonly string _root;

        public TesseraNativeTranslucencySignOffPolicyTests()
        {
            _root = Path.Combine(Path.GetTempPath(), "MosaicNativeTranslucencySignOff_" + Guid.NewGuid().ToString("N"));
            AppPaths.SetRootOverride(_root);
            AppPaths.EnsureLayout();
            ModuleSettingsStore.Save("Tessera", new TesseraSettings());
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

        [Fact]
        public void Each_platform_reads_its_own_sign_off()
        {
            _ = TesseraNativeTranslucencySignOffPolicy.EvalCompleteForThisPlatform.Should().Be(
                OperatingSystem.IsWindows()
                    ? TesseraNativeTranslucencySignOffPolicy.Win11EvalComplete
                    : TesseraNativeTranslucencySignOffPolicy.KdeEvalSignedOff);
        }

        [Fact]
        public void Win11_native_translucency_eval_is_signed_off_in_core()
        {
            _ = TesseraNativeTranslucencySignOffPolicy.Win11EvalComplete.Should().BeTrue();
            _ = TesseraNativeTranslucencySignOffPolicy.H2Win11EvalSignedOff.Should().BeTrue();
            _ = TesseraNativeTranslucencySignOffPolicy.H3StackedVolumeMediaSignedOff.Should().BeTrue();
            _ = TesseraNativeTranslucencySignOffPolicy.AllH3StackedStylesSignedOff().Should().BeTrue();
            _ = HostLaunchOptions.TesseraNativeTranslucencyTrial.Should().BeFalse(
                "sign-off does not set the launch flag; the saved material decides");
            ModuleSettingsStore.Save("Tessera", new TesseraSettings { UseAcrylicBackdrop = false });
            _ = TesseraNativeTranslucencyPolicy.IsTrialRequested().Should().BeFalse("Solid never asks for the blur");
        }

        [Theory]
        [InlineData(StyleIds.Meter)]
        [InlineData(StyleIds.Gnome)]
        [InlineData(StyleIds.Compact)]
        [InlineData(StyleIds.ModernFlyouts)]
        public void H3_stacked_styles_are_signed_off(string styleId)
        {
            _ = TesseraStackedPlacementPolicy.SupportsStackedNativeTranslucency(styleId).Should().BeTrue();
            _ = TesseraNativeTranslucencySignOffPolicy.IsH3StackedStyleSignedOff(styleId).Should().BeTrue();
        }

        [Theory]
        [InlineData(StyleIds.Fluent)]
        [InlineData(StyleIds.Windows11)]
        [InlineData(StyleIds.MaterialYou)]
        public void Unified_shell_styles_stay_on_single_hwnd_path_not_h3_split(string styleId)
        {
            _ = TesseraStackedPlacementPolicy.SupportsStackedNativeTranslucency(styleId).Should().BeFalse();
            _ = TesseraNativeTranslucencySignOffPolicy.IsH3StackedStyleSignedOff(styleId).Should().BeFalse();
        }
    }
}
