namespace MosaicShell.Core.Services.Mpris
{
    /// <summary>
    /// Publishes Grout's report as the Host's own MPRIS player (<see cref="MprisExportPolicy"/>), so every MPRIS client
    /// gets the page's title, artist and cover for the whole track. The cover is written to a file named by its content
    /// that stays while the track plays (plasma-browser-integration deletes its own partway through). The player's
    /// transport goes back through the Host's media service, and only while the current session is the browser, so it
    /// never steers another player. Position and length come from that session too: Grout reports no timing.
    /// </summary>
    public sealed class GroutMprisBridge : IMprisExportCommands, IDisposable
    {
        private const string CoverPattern = "cover-*";

        private readonly IBrowserMediaSource _browser;
        private readonly IMediaSessionService _media;
        private readonly IMprisExport _export;
        private readonly string _artDirectory;
        private readonly Func<int?> _browserProcessId;
        private readonly Func<string, int?, string> _desktopEntryFor;
        private readonly Lock _gate = new();
        private Task _settled = Task.CompletedTask;
        private string? _currentArt;
        private string? _previousArt;
        private bool _disposed;

        public GroutMprisBridge(
            IBrowserMediaSource browser,
            IMediaSessionService media,
            Func<IMprisExportCommands, IMprisExport> createExport,
            string artDirectory,
            Func<int?>? browserProcessId = null,
            Func<string, int?, string>? desktopEntryFor = null)
        {
            _browser = browser;
            _media = media;
            _artDirectory = artDirectory;
            _browserProcessId = browserProcessId ?? (static () => null);
            _desktopEntryFor = desktopEntryFor ?? (static (appId, _) => appId);
            _export = createExport(this);
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

        public Task PlayPauseAsync()
        {
            return IsBrowserSession() ? _media.PlayPauseAsync() : Task.CompletedTask;
        }

        public Task PlayAsync()
        {
            return IsBrowserSession() && _media.Current?.IsPlaying != true ? _media.PlayPauseAsync() : Task.CompletedTask;
        }

        public Task PauseAsync()
        {
            return IsBrowserSession() && _media.Current?.IsPlaying == true ? _media.PlayPauseAsync() : Task.CompletedTask;
        }

        public Task NextAsync()
        {
            return IsBrowserSession() ? _media.NextAsync() : Task.CompletedTask;
        }

        public Task PreviousAsync()
        {
            return IsBrowserSession() ? _media.PreviousAsync() : Task.CompletedTask;
        }

        public Task SeekToAsync(double positionSeconds)
        {
            return IsBrowserSession() ? _media.SeekAsync(positionSeconds) : Task.CompletedTask;
        }

        public void Dispose()
        {
            lock (_gate)
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
            }

            _browser.Changed -= OnChanged;
            _media.Changed -= OnChanged;
            _media.ProgressChanged -= OnChanged;
            _export.Dispose();
        }

        private bool IsBrowserSession()
        {
            return BrowserSessionPolicy.LooksLikeBrowserSession(_media.Current?.AppId);
        }

        private void OnChanged(object? sender, EventArgs e)
        {
            lock (_gate)
            {
                if (_disposed)
                {
                    return;
                }

                // One publish at a time, each reading the latest report when it runs.
                _settled = _settled.ContinueWith(_ => PublishLatestAsync(), TaskScheduler.Default).Unwrap();
            }
        }

        private async Task PublishLatestAsync()
        {
            try
            {
                BrowserPlayerSnapshot? report = _browser.Active;
                string? artUrl = report?.CoverPng is { Length: > 0 } cover ? WriteArt(cover) : null;
                MediaSessionInfo? session = _media.Current;
                int? browserProcess = _browserProcessId();
                string? desktopEntry = BrowserSessionPolicy.LooksLikeBrowserSession(session?.AppId)
                    ? _desktopEntryFor(session!.AppId!, browserProcess)
                    : null;
                await _export.PublishAsync(MprisExportPolicy.From(report, artUrl, desktopEntry, session, browserProcess))
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MPRIS export] publish failed: {ex.Message}");
            }
        }

        /// <summary>Writes the cover once and returns its file URL; keeps only the current and previous covers.</summary>
        private string? WriteArt(byte[] cover)
        {
            string name = MprisExportPolicy.ArtFileName(cover);
            string path = Path.Combine(_artDirectory, name);
            try
            {
                if (name != _currentArt)
                {
                    _ = Directory.CreateDirectory(_artDirectory);
                    if (!File.Exists(path))
                    {
                        File.WriteAllBytes(path, cover);
                    }

                    _previousArt = _currentArt;
                    _currentArt = name;
                    Prune();
                }

                return new Uri(path).AbsoluteUri;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                return null;
            }
        }

        private void Prune()
        {
            foreach (string file in Directory.EnumerateFiles(_artDirectory, CoverPattern))
            {
                string name = Path.GetFileName(file);
                if (name != _currentArt && name != _previousArt)
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                    {
                        // Another reader may still hold it; the next cover tries again.
                    }
                }
            }
        }
    }
}
