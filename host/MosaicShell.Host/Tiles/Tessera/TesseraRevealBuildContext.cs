namespace MosaicShell.Host.Tiles.Tessera;

/// <summary>Ambient preview/Ani while building TesseraRevealHost trees.</summary>
internal static class TesseraRevealBuildContext
{
    [ThreadStatic] private static bool _isPreview;
    [ThreadStatic] private static int _ani;
    [ThreadStatic] private static bool _active;
    [ThreadStatic] private static bool _sessionAlreadyShowing;

    public static bool IsPreview => _active && _isPreview;
    public static int Ani => _ani;
    public static bool IsActive => _active;
    public static bool SessionAlreadyShowing => _active && _sessionAlreadyShowing;

    public static IDisposable Begin(bool isPreview, int ani, bool sessionAlreadyShowing = false)
    {
        _isPreview = isPreview;
        _ani = ani;
        _sessionAlreadyShowing = sessionAlreadyShowing;
        _active = true;
        return new Scope();
    }

    private sealed class Scope : IDisposable
    {
        public void Dispose()
        {
            _isPreview = false;
            _ani = 0;
            _sessionAlreadyShowing = false;
            _active = false;
        }
    }
}
