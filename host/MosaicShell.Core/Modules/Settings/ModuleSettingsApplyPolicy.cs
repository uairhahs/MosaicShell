namespace MosaicShell.Core.Modules.Settings
{
    /// <summary>What has to happen after a change is saved, weakest first so changes made together combine to the strongest.</summary>
    public enum SettingFollowUp
    {
        /// <summary>Nothing: the module reads the setting the next time it opens.</summary>
        None,

        /// <summary>Arm the capability again.</summary>
        ReArm,

        /// <summary>Close the widget and open it again.</summary>
        RelaunchWidget,

        /// <summary>Offer "Restart now": only a new MosaicShell process picks it up.</summary>
        OfferRestart,
    }

    /// <summary>
    /// Instant apply: every change is written as it is made (after a short pause for dragging and typing), and the row's
    /// <see cref="SettingApplyEffect"/> plus the module's state decide what follows. The Host owns the timer and runs
    /// the follow-up; the numbers and the decision are here.
    /// </summary>
    public static class ModuleSettingsApplyPolicy
    {
        /// <summary>How long after the last change to a number before it is written (a slider being dragged).</summary>
        public const int NumberDebounceMs = 300;

        /// <summary>How long after the last change to a list before it is written (text being typed).</summary>
        public const int ListDebounceMs = 500;

        public static SettingFollowUp FollowUp(SettingApplyEffect effect, bool capabilityArmed, bool widgetOpen)
        {
            return effect switch
            {
                SettingApplyEffect.ReArm when capabilityArmed => SettingFollowUp.ReArm,
                SettingApplyEffect.RelaunchWidget when widgetOpen => SettingFollowUp.RelaunchWidget,
                SettingApplyEffect.RestartHost => SettingFollowUp.OfferRestart,
                _ => SettingFollowUp.None,
            };
        }

        public static SettingFollowUp Combine(SettingFollowUp first, SettingFollowUp second)
        {
            return first >= second ? first : second;
        }

        /// <summary>0 writes at once (a click); more waits for the change to pause.</summary>
        public static int DebounceMs(SettingKind kind)
        {
            return kind switch
            {
                SettingKind.Number => NumberDebounceMs,
                SettingKind.TextList or SettingKind.ActionList => ListDebounceMs,
                _ => 0,
            };
        }

        /// <summary>The note a row shows before it is changed, or null when the change shows by itself.</summary>
        public static string? BeforeChangeNote(SettingApplyEffect effect)
        {
            return effect == SettingApplyEffect.RestartHost ? "Takes effect after MosaicShell restarts." : null;
        }
    }
}
