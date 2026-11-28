using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using MosaicShell.Core;
using MosaicShell.Core.HubShell;
using MosaicShell.Core.Services;

namespace MosaicShell.Host.ViewModels
{
    /// <summary>
    /// The Settings page in its sections (<see cref="HubSettingsLayout"/>): General, Updates, Browser media, Diagnostics.
    /// Texts that depend on the desktop or on Grout come from Core.
    /// </summary>
    public partial class MainViewModel
    {
        public CloseChoice KeepRunningChoice => HubSettingsLayout.CloseChoices(_services.Features)[0];

        public CloseChoice QuitChoice => HubSettingsLayout.CloseChoices(_services.Features)[1];

        public string UpdateHint => HubSettingsLayout.UpdateHint(OperatingSystem.IsWindows());

        /// <summary>How many browsers Grout is connected from; null when this build has no browser bridge.</summary>
        private int? BrowserConnections => _services.Media is IBrowserConnectionStatus status ? status.BrowserConnections : null;

        public string BrowserMediaStatus => HubSettingsLayout.BrowserMediaStatus(BrowserConnections);

        public bool OffersGroutInstall => HubSettingsLayout.OffersGroutInstall(BrowserConnections);

        /// <summary>The status light beside Grout's line, read faster than the words: green connected, red not.</summary>
        public bool IsGroutConnected => HubSettingsLayout.GroutLightFor(BrowserConnections) == GroutLight.Connected;

        public bool IsGroutDisconnected => HubSettingsLayout.GroutLightFor(BrowserConnections) == GroutLight.Disconnected;

        /// <summary>Where the Host and Worker write their logs.</summary>
        public string LogFolderPath => AppPaths.CacheDirectory;

        /// <summary>How often Grout's light is read again while Settings is open, so it follows the connection.</summary>
        public static readonly TimeSpan GroutRefreshInterval = TimeSpan.FromSeconds(2);

        private DispatcherTimer? _settingsRefresh;

        /// <summary>
        /// Reads the live parts of Settings when the page opens, then keeps reading Grout's state while it stays open:
        /// a browser can connect or go away with the page in front of you.
        /// </summary>
        private void RefreshSettingsPage()
        {
            if (_settingsRefresh is null)
            {
                _settingsRefresh = new DispatcherTimer { Interval = GroutRefreshInterval };
                _settingsRefresh.Tick += (_, _) =>
                {
                    if (IsSettings)
                    {
                        RefreshGroutStatus();
                    }
                    else
                    {
                        _settingsRefresh.Stop();
                    }
                };
            }

            _settingsRefresh.Start();
            RefreshGroutStatus();
        }

        private void RefreshGroutStatus()
        {
            OnPropertyChanged(nameof(BrowserMediaStatus));
            OnPropertyChanged(nameof(OffersGroutInstall));
            OnPropertyChanged(nameof(IsGroutConnected));
            OnPropertyChanged(nameof(IsGroutDisconnected));
        }

        [RelayCommand]
        private static void OpenGrout()
        {
            TryOpenUrl(HubSettingsLayout.GroutStoreUrl);
        }
    }
}
