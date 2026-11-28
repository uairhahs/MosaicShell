namespace MosaicShell.Core.HubShell
{
    /// <summary>
    /// The Hub's left rail: Home, Modules, Widgets, then Settings and About. Folds to icons on a narrow window.
    /// </summary>
    public static class HubRail
    {
        /// <summary>Below this width the rail shows icons only.</summary>
        public const double CollapseBelowDip = 900;

        public static bool IsCollapsed(double widthDip)
        {
            return widthDip < CollapseBelowDip;
        }
    }
}
