namespace MosaicShell.Core.Modules
{
    /// <summary>
    /// Canonical first-party module ids. Host and Core compare against these instead of restating
    /// the string, so a rename is one edit and a typo is a compile error.
    /// <see cref="ModuleIdsTests"/> pins them to <see cref="ModuleCatalog.BuiltIns"/>.
    /// </summary>
    public static class ModuleIds
    {
        public const string Tessera = "Tessera";
        public const string Mixdeck = "Mixdeck";
        public const string Inlay = "Inlay";
        public const string Slate = "Slate";
        public const string Chord = "Chord";
        public const string Substrate = "Substrate";
        public const string Chrono = "Chrono";
        public const string Phono = "Phono";
        public const string Pulse = "Pulse";
        public const string Canvas = "Canvas";

        public static IReadOnlyList<string> BuiltIns { get; } =
        [
            Tessera, Mixdeck, Inlay, Slate, Chord, Substrate, Chrono, Phono, Pulse, Canvas,
        ];

        /// <summary>Ids arrive from user-authored manifests and settings, so compare case-insensitively.</summary>
        public static bool Is(string? moduleId, string expected)
        {
            return !string.IsNullOrWhiteSpace(moduleId)
                && moduleId.Equals(expected, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsTessera(string? moduleId)
        {
            return Is(moduleId, Tessera);
        }
    }
}
