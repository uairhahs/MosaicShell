namespace MosaicShell.Host.Tiles.Tessera;

/// <summary>Ambient preview/Ani while building TesseraRevealHost trees.</summary>
internal static class TesseraRevealBuildContext
{
    [ThreadStatic] private static bool _isPreview;
    [ThreadStatic] private static int _ani;
    [ThreadStatic] private static bool _active;

    public static bool IsPreview => _active && _isPreview;
    public static int Ani => _ani;
    public static bool IsActive => _active;

    public static IDisposable Begin(bool isPreview, int ani)
    {
        _isPreview = isPreview;
        _ani = ani;
        _active = true;
        return new Scope();
    }

    private sealed class Scope : IDisposable
    {
        public void Dispose()
        {
            _isPreview = false;
            _ani = 0;
            _active = false;
        }
    }
}
