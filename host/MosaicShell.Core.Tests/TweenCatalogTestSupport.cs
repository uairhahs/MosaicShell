using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Core.Tests
{
    /// <summary>The two style-profile questions the tween-catalog tests ask, answered from the profile and the channels.</summary>
    internal static class TweenCatalogTestSupport
    {
        public static bool TryResolveMediaRestSizeDip(string? styleId, out double widthDip, out double heightDip)
        {
            TesseraFlyoutStyleProfile profile = TesseraFlyoutTweenTargetCatalog.ResolveProfile(styleId);
            widthDip = profile.MediaWidthDip;
            heightDip = profile.MediaHeightDip;
            return profile.MediaWidthDip > 1 || profile.MediaHeightDip > 1;
        }

        public static bool VolumeStaysOpaqueDuringPhase2(string? styleId)
        {
            return !TesseraFlyoutTweenTargetCatalog.HasChannel(styleId, TesseraTweenChannel.VolumeFillOpacity);
        }
    }
}
