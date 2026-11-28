using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MosaicShell.Core.Modules;

namespace MosaicShell.Host.ViewModels
{
    /// <summary>
    /// The search box over the library. The matching rule and the words are <see cref="HubLibrarySearch"/>'s; this file
    /// only shows and hides rows.
    /// </summary>
    public partial class MainViewModel
    {
        [ObservableProperty] private string _librarySearch = "";
        [ObservableProperty] private bool _hasNoModuleMatches;
        [ObservableProperty] private bool _hasNoWidgetMatches;

        public string LibrarySearchPlaceholder => HubLibrarySearch.Placeholder;
        public string LibraryNoResultsText => HubLibrarySearch.NoResultsText(LibrarySearch);
        public bool HasLibrarySearch => !string.IsNullOrWhiteSpace(LibrarySearch);

        partial void OnLibrarySearchChanged(string value)
        {
            ApplyLibrarySearch();
        }

        [RelayCommand]
        private void ClearLibrarySearch()
        {
            LibrarySearch = "";
        }

        private void ApplyLibrarySearch()
        {
            HasNoModuleMatches = !ApplyLibrarySearch(Modules) && Modules.Count > 0;
            HasNoWidgetMatches = !ApplyLibrarySearch(Widgets) && Widgets.Count > 0;
            OnPropertyChanged(nameof(LibraryNoResultsText));
            OnPropertyChanged(nameof(HasLibrarySearch));
        }

        /// <summary>Shows the rows that match and returns whether any did.</summary>
        private bool ApplyLibrarySearch(IEnumerable<LibraryItemViewModel> rows)
        {
            bool any = false;
            foreach (LibraryItemViewModel row in rows)
            {
                row.IsShownBySearch = HubLibrarySearch.Matches(LibrarySearch, row.Id, row.Name, row.Description);
                any |= row.IsShownBySearch;
            }

            return any;
        }
    }
}
