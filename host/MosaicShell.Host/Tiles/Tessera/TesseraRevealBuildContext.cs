namespace MosaicShell.Host.Tiles.Tessera
{
    /// <summary>Ambient preview/Ani while building TesseraRevealHost trees.</summary>
    internal static class TesseraRevealBuildContext
    {
        [field: ThreadStatic] public static bool IsPreview { get => IsActive && field; private set; }
        [field: ThreadStatic]
        public static int Ani { get; private set; }
        [field: ThreadStatic]
        public static bool IsActive { get; private set; }
        [field: ThreadStatic]
        public static bool SessionAlreadyShowing { get => IsActive && field; private set; }

        public static IDisposable Begin(bool isPreview, int ani, bool sessionAlreadyShowing = false)
        {
            IsPreview = isPreview;
            Ani = ani;
            SessionAlreadyShowing = sessionAlreadyShowing;
            IsActive = true;
            return new Scope();
        }

        private sealed class Scope : IDisposable
        {
            public void Dispose()
            {
                IsPreview = false;
                Ani = 0;
                SessionAlreadyShowing = false;
                IsActive = false;
            }
        }
    }
}
