namespace MosaicShell.Core.Services.BrowserUi
{
    public enum UiToggleState
    {
        Off,
        On,
        Indeterminate,
    }

    /// <summary>
    /// A toggle button in a browser window's accessibility tree, described by where it sits rather than by its name,
    /// which is localized. <see cref="Press"/> toggles it, as a click would.
    /// </summary>
    public sealed record UiToggle(string ParentClassName, int IndexInParent, UiToggleState State, Action Press);

    /// <summary>One top-level browser window.</summary>
    public interface IUiWindow
    {
        /// <summary>The window title, which a browser builds from the page title.</summary>
        string Title { get; }

        /// <summary>
        /// The track title the page's own player shows, or null when the page has no such player (or the browser has not
        /// built its accessibility tree yet). This, not the window title, says which track a window is playing: the page
        /// title does not always name it.
        /// </summary>
        string? PlayerTitle();

        /// <summary>The toggle buttons in the window's page. Costly, so asked for only after <see cref="PlayerTitle"/> matched.</summary>
        IReadOnlyList<UiToggle> Toggles();
    }

    /// <summary>What the Host can see of the user's browsers through the operating system's accessibility tree.</summary>
    public interface IBrowserUi
    {
        IReadOnlyList<IUiWindow> Windows();
    }
}
