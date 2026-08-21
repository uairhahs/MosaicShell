using NAudio.CoreAudioApi;

namespace MosaicShell.Core.Services;

public sealed class WindowsAudioService : IAudioService
{
    private readonly MMDeviceEnumerator _enum = new();
    private readonly MMDevice _device;
    private bool _disposed;

    public WindowsAudioService()
    {
        _device = _enum.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
        _lastVol = _device.AudioEndpointVolume.MasterVolumeLevelScalar;
        _lastMute = _device.AudioEndpointVolume.Mute;
        _device.AudioEndpointVolume.OnVolumeNotification += OnVol;
    }

    public double MasterVolume
    {
        get => _device.AudioEndpointVolume.MasterVolumeLevelScalar;
        set
        {
            var v = Math.Clamp((float)value, 0f, 1f);
            _device.AudioEndpointVolume.MasterVolumeLevelScalar = v;
        }
    }

    public bool IsMuted
    {
        get => _device.AudioEndpointVolume.Mute;
        set => _device.AudioEndpointVolume.Mute = value;
    }

    public event EventHandler? Changed;

    private float _lastVol = float.NaN;
    private bool _lastMute;

    private void OnVol(AudioVolumeNotificationData data)
    {
        // Ignore no-op notifications some drivers emit without a real change
        var vol = data.MasterVolume;
        var mute = data.Muted;
        if (!float.IsNaN(_lastVol)
            && Math.Abs(vol - _lastVol) < 0.0005f
            && mute == _lastMute)
            return;
        _lastVol = vol;
        _lastMute = mute;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try { _device.AudioEndpointVolume.OnVolumeNotification -= OnVol; } catch { /* ignore */ }
        _device.Dispose();
        _enum.Dispose();
    }
}

public sealed class WindowsAppAudioService : IAppAudioService
{
    private readonly MMDeviceEnumerator _enum = new();
    private readonly MMDevice _device;

    public WindowsAppAudioService()
    {
        _device = _enum.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
    }

    public event EventHandler? SessionsChanged;

    public IReadOnlyList<AppAudioSession> GetSessions()
    {
        var list = new List<AppAudioSession>();
        var managers = _device.AudioSessionManager.Sessions;
        for (var i = 0; i < managers.Count; i++)
        {
            using var s = managers[i];
            if (s.State == NAudio.CoreAudioApi.Interfaces.AudioSessionState.AudioSessionStateExpired)
                continue;
            var name = s.DisplayName;
            if (string.IsNullOrWhiteSpace(name))
                name = $"Session {i}";
            var id = s.GetSessionIdentifier ?? $"{i}";
            list.Add(new AppAudioSession(id, name, s.SimpleAudioVolume.Volume, s.SimpleAudioVolume.Mute));
        }

        return list;
    }

    public void SetVolume(string sessionId, double volume)
    {
        foreach (var s in Enumerate())
        {
            if (!string.Equals(s.GetSessionIdentifier, sessionId, StringComparison.Ordinal))
            {
                s.Dispose();
                continue;
            }

            s.SimpleAudioVolume.Volume = Math.Clamp((float)volume, 0f, 1f);
            s.Dispose();
            SessionsChanged?.Invoke(this, EventArgs.Empty);
            return;
        }
    }

    public void SetMuted(string sessionId, bool muted)
    {
        foreach (var s in Enumerate())
        {
            if (!string.Equals(s.GetSessionIdentifier, sessionId, StringComparison.Ordinal))
            {
                s.Dispose();
                continue;
            }

            s.SimpleAudioVolume.Mute = muted;
            s.Dispose();
            SessionsChanged?.Invoke(this, EventArgs.Empty);
            return;
        }
    }

    private IEnumerable<AudioSessionControl> Enumerate()
    {
        var managers = _device.AudioSessionManager.Sessions;
        for (var i = 0; i < managers.Count; i++)
            yield return managers[i];
    }

    public void Dispose()
    {
        _device.Dispose();
        _enum.Dispose();
    }
}
