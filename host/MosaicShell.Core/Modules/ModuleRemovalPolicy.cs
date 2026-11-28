namespace MosaicShell.Core.Modules
{
    /// <summary>How the Hub removes a module.</summary>
    public enum ModuleRemovalMode
    {
        /// <summary>Remove at once and offer Undo: the files come back through Mosaicist.</summary>
        RemoveWithUndo = 0,

        /// <summary>Ask first: MosaicShell cannot bring the files back.</summary>
        ConfirmFirst = 1,
    }

    /// <summary>
    /// When the Hub offers Remove, whether it asks first, and the words it uses. Settings are kept on
    /// removal (see <see cref="Runtime.ModuleUninstaller.Remove"/>), so a bundled module loses nothing.
    /// </summary>
    public static class ModuleRemovalPolicy
    {
        public const string UndoLabel = "Undo";

        public static bool CanRemove(string moduleId)
        {
            return ModuleCatalog.IsInstalled(moduleId);
        }

        public static ModuleRemovalMode ModeFor(string moduleId)
        {
            return ModuleCatalog.IsBundled(moduleId) ? ModuleRemovalMode.RemoveWithUndo : ModuleRemovalMode.ConfirmFirst;
        }

        public static string ConfirmTitle(string displayName)
        {
            return $"Remove {displayName}?";
        }

        public static string ConfirmBody(string displayName)
        {
            return $"MosaicShell did not ship {displayName}, so you will need its package to add it again.";
        }

        public static string RemovedStatus(string displayName)
        {
            return $"Removed {displayName}.";
        }

        public static string RestoredStatus(string displayName)
        {
            return $"Added {displayName} back.";
        }
    }
}
