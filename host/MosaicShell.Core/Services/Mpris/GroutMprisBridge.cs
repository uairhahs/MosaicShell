namespace MosaicShell.Core.Services.Mpris
{
    /// <summary>
    /// Publishes each tab Grout reports as its own MPRIS player (<see cref="MprisExportPolicy"/>), so every MPRIS client
    /// gets the page's title, artist and cover for the whole track, and Plasma lists one entry per tab. A cover is written
    /// to a file named by its content that stays while the track plays (plasma-browser-integration deletes its own
    /// partway through). Grout reports no timing and cannot drive the page, so position, length and transport come
    /// from the browser's own player through the Host's media service, which only acts on the browser's active tab
    /// (<see cref="MprisExportPolicy.IsControllableTab"/>). A source that cannot list its tabs publishes its active
    /// player under <see cref="MprisExportPolicy.BusName"/>.
    /// </summary>
    public sealed class GroutMprisBridge : IDisposable
    {
        private const string CoverPattern = "cover-*";

        private readonly IBrowserMediaSource _browser;
        private readonly IMediaSessionService _media;
        private readonly Func<string, IMprisExportCommands, IMprisExport> _createExport;
        private readonly string _artDirectory;
        private readonly Func<int?> _browserProcessId;
        private readonly Func<string, int?, string> _desktopEntryFor;
        private readonly Lock _gate = new();
        private readonly Dictionary<string, Tab> _tabs = [];
        private Task _settled = Task.CompletedTask;
        private bool _disposed;
        private string? _browserAppId;
        private int? _browserProcess;

        public GroutMprisBridge(
            IBrowserMediaSource browser,
            IMediaSessionService media,
            Func<string, IMprisExportCommands, IMprisExport> createExport,
            string artDirectory,
            Func<int?>? browserProcessId = null,
            Func<string, int?, string>? desktopEntryFor = null)
        {
            _browser = browser;
            _media = media;
            _createExport = createExport;
            _artDirectory = artDirectory;
            _browserProcessId = browserProcessId ?? (static () => null);
            _desktopEntryFor = desktopEntryFor ?? (static (appId, _) => appId);
            _browser.Changed += OnChanged;
            _media.Changed += OnChanged;
            _media.ProgressChanged += OnChanged;
        }

        /// <summary>Completes when every publish started so far has finished. For tests.</summary>
        public Task SettledAsync()
        {
            lock (_gate)
            {
                return _settled;
            }
        }

        public void Dispose()
        {
            Tab[] tabs;
            lock (_gate)
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                tabs = [.. _tabs.Values];
                _tabs.Clear();
            }

            _browser.Changed -= OnChanged;
            _media.Changed -= OnChanged;
            _media.ProgressChanged -= OnChanged;
            foreach (Tab tab in tabs)
            {
                tab.Export.Dispose();
            }
        }

        private void OnChanged(object? sender, EventArgs e)
        {
            lock (_gate)
            {
                if (_disposed)
                {
                    return;
                }

                // One publish at a time, each reading the latest reports when it runs.
                _settled = _settled.ContinueWith(_ => PublishLatestAsync(), TaskScheduler.Default).Unwrap();
            }
        }

        private IReadOnlyList<BrowserTabPlayer> CurrentPlayers()
        {
            return _browser is IBrowserPlayerList list
                ? list.Players
                : _browser.Active is { } active ? [new BrowserTabPlayer("", active)] : [];
        }

        private async Task PublishLatestAsync()
        {
            try
            {
                IReadOnlyList<BrowserTabPlayer> players = CurrentPlayers();
                MediaSessionInfo? session = _media.Current;
                if (BrowserSessionPolicy.LooksLikeBrowserSession(session?.AppId))
                {
                    // Remembered, so a tab keeps its browser's icon and process while another app is the session; the
                    // active process is then that app's, which must never be claimed.
                    _browserAppId = session!.AppId;
                    _browserProcess = _browserProcessId() ?? _browserProcess;
                }

                string? desktopEntry = _browserAppId is { } appId ? _desktopEntryFor(appId, _browserProcess) : null;
                HashSet<string> seen = [];
                foreach (BrowserTabPlayer player in players)
                {
                    _ = seen.Add(player.Key);
                    Tab tab = TabFor(player.Key);
                    if (tab is null)
                    {
                        return;
                    }

                    tab.Snapshot = player.Snapshot;
                    string? artUrl = player.Snapshot.CoverPng is { Length: > 0 } cover ? WriteArt(tab, cover) : null;
                    bool controllable = MprisExportPolicy.IsControllableTab(session, player.Snapshot);
                    MprisExportedState? state = MprisExportPolicy.From(
                        player.Snapshot, artUrl, desktopEntry, session, desktopEntry is null ? null : _browserProcess, controllable);
                    await tab.Export.PublishAsync(state).ConfigureAwait(false);
                }

                foreach (Tab gone in Withdraw(seen))
                {
                    await gone.Export.PublishAsync(null).ConfigureAwait(false);
                    gone.Export.Dispose();
                }

                Prune();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MPRIS export] publish failed: {ex.Message}");
            }
        }

        private Tab TabFor(string key)
        {
            lock (_gate)
            {
                if (!_tabs.TryGetValue(key, out Tab? tab))
                {
                    tab = new Tab(this);
                    tab.Export = _createExport(key.Length == 0 ? MprisExportPolicy.BusName : MprisExportPolicy.BusNameFor(key), tab);
                    _tabs[key] = tab;
                }

                return tab;
            }
        }

        private List<Tab> Withdraw(HashSet<string> keep)
        {
            lock (_gate)
            {
                List<string> gone = [.. _tabs.Keys.Where(k => !keep.Contains(k))];
                List<Tab> tabs = [.. gone.Select(k => _tabs[k])];
                foreach (string key in gone)
                {
                    _ = _tabs.Remove(key);
                }

                return tabs;
            }
        }

        /// <summary>Writes the cover once and returns its file URL; the tab remembers its current and previous covers.</summary>
        private string? WriteArt(Tab tab, byte[] cover)
        {
            string name = MprisExportPolicy.ArtFileName(cover);
            string path = Path.Combine(_artDirectory, name);
            try
            {
                if (name != tab.CurrentArt)
                {
                    _ = Directory.CreateDirectory(_artDirectory);
                    if (!File.Exists(path))
                    {
                        File.WriteAllBytes(path, cover);
                    }

                    tab.PreviousArt = tab.CurrentArt;
                    tab.CurrentArt = name;
                }

                return new Uri(path).AbsoluteUri;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                return null;
            }
        }

        /// <summary>Deletes every cover no tab still shows or has just replaced.</summary>
        private void Prune()
        {
            HashSet<string> keep;
            lock (_gate)
            {
                keep = [.. _tabs.Values.SelectMany(t => new[] { t.CurrentArt, t.PreviousArt }).OfType<string>()];
            }

            if (!Directory.Exists(_artDirectory))
            {
                return;
            }

            foreach (string file in Directory.EnumerateFiles(_artDirectory, CoverPattern))
            {
                if (!keep.Contains(Path.GetFileName(file)))
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                    {
                        // Another reader may still hold it; the next publish tries again.
                    }
                }
            }
        }

        /// <summary>One tab's player: its export, its covers, and transport that acts only while it is controllable.</summary>
        private sealed class Tab(GroutMprisBridge bridge) : IMprisExportCommands
        {
            public IMprisExport Export { get; set; } = null!;

            public BrowserPlayerSnapshot? Snapshot { get; set; }

            public string? CurrentArt { get; set; }

            public string? PreviousArt { get; set; }

            private IMediaSessionService Media => bridge._media;

            private bool Controllable => Snapshot is { } snapshot && MprisExportPolicy.IsControllableTab(Media.Current, snapshot);

            public Task PlayPauseAsync()
            {
                return Controllable ? Media.PlayPauseAsync() : Task.CompletedTask;
            }

            public Task PlayAsync()
            {
                return Controllable && Media.Current?.IsPlaying != true ? Media.PlayPauseAsync() : Task.CompletedTask;
            }

            public Task PauseAsync()
            {
                return Controllable && Media.Current?.IsPlaying == true ? Media.PlayPauseAsync() : Task.CompletedTask;
            }

            public Task NextAsync()
            {
                return Controllable ? Media.NextAsync() : Task.CompletedTask;
            }

            public Task PreviousAsync()
            {
                return Controllable ? Media.PreviousAsync() : Task.CompletedTask;
            }

            public Task SeekToAsync(double positionSeconds)
            {
                return Controllable ? Media.SeekAsync(positionSeconds) : Task.CompletedTask;
            }
        }
    }
}
