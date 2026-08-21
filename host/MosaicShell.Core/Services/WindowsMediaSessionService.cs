using Windows.Media.Control;

namespace MosaicShell.Core.Services;

public sealed class WindowsMediaSessionService : IMediaSessionService
{
    private GlobalSystemMediaTransportControlsSessionManager? _manager;
    private GlobalSystemMediaTransportControlsSession? _session;
    private bool _disposed;

    public WindowsMediaSessionService()
    {
        _ = InitAsync();
    }

    public MediaSessionInfo? Current { get; private set; }
    public event EventHandler? Changed;

    private async Task InitAsync()
    {
        try
        {
            _manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            _manager.CurrentSessionChanged += (_, _) => _ = RefreshAsync();
            await RefreshAsync();
        }
        catch
        {
            Current = null;
        }
    }

    private async Task RefreshAsync()
    {
        try
        {
            if (_manager is null) return;
            if (_session is not null)
            {
                _session.MediaPropertiesChanged -= OnProps;
                _session.PlaybackInfoChanged -= OnProps;
            }

            _session = _manager.GetCurrentSession();
            if (_session is null)
            {
                Current = null;
                Changed?.Invoke(this, EventArgs.Empty);
                return;
            }

            _session.MediaPropertiesChanged += OnProps;
            _session.PlaybackInfoChanged += OnProps;
            await UpdateFromSessionAsync(_session);
        }
        catch
        {
            Current = null;
        }
    }

    private void OnProps(GlobalSystemMediaTransportControlsSession sender, object args) =>
        _ = UpdateFromSessionAsync(sender);

    private async Task UpdateFromSessionAsync(GlobalSystemMediaTransportControlsSession session)
    {
        try
        {
            var props = await session.TryGetMediaPropertiesAsync();
            var playback = session.GetPlaybackInfo();
            Current = new MediaSessionInfo(
                props?.Title,
                props?.Artist,
                session.SourceAppUserModelId,
                playback.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing);
            Changed?.Invoke(this, EventArgs.Empty);
        }
        catch { /* ignore */ }
    }

    public async Task PlayPauseAsync()
    {
        if (_session is null) await RefreshAsync();
        if (_session is not null) await _session.TryTogglePlayPauseAsync();
    }

    public async Task NextAsync()
    {
        if (_session is null) await RefreshAsync();
        if (_session is not null) await _session.TrySkipNextAsync();
    }

    public async Task PreviousAsync()
    {
        if (_session is null) await RefreshAsync();
        if (_session is not null) await _session.TrySkipPreviousAsync();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_session is not null)
        {
            _session.MediaPropertiesChanged -= OnProps;
            _session.PlaybackInfoChanged -= OnProps;
        }
    }
}
