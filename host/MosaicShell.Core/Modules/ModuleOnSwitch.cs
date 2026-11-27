namespace MosaicShell.Core.Modules
{
    /// <summary>One thing the Host does when a module's on switch changes.</summary>
    public enum ModuleSwitchStep
    {
        Install,
        Arm,
        Disarm,

        /// <summary>Put the widget on the desktop; its saved session brings it back each time MosaicShell starts.</summary>
        ShowWidget,

        /// <summary>Take the widget off the desktop and drop its saved session.</summary>
        CloseWidget,
    }

    /// <summary>
    /// The one on switch every module page has, in place of Arm, Disarm, Launch and Stop. A capability is on while it
    /// is armed; a widget is on while it is on the desktop, which also brings it back at each start. Off never removes
    /// a module: Remove does that.
    /// </summary>
    public static class ModuleOnSwitch
    {
        public static bool IsOn(ModuleKind kind, bool installed, bool armed, bool widgetOpen)
        {
            return installed && (IsBackground(kind) ? armed : widgetOpen);
        }

        /// <summary>The steps, in order, for turning the switch on or off.</summary>
        public static IReadOnlyList<ModuleSwitchStep> Plan(ModuleKind kind, bool installed, bool turnOn)
        {
            if (!turnOn)
            {
                return installed ? [IsBackground(kind) ? ModuleSwitchStep.Disarm : ModuleSwitchStep.CloseWidget] : [];
            }

            ModuleSwitchStep run = IsBackground(kind) ? ModuleSwitchStep.Arm : ModuleSwitchStep.ShowWidget;
            return installed ? [run] : [ModuleSwitchStep.Install, run];
        }

        public static string Label(string displayName)
        {
            return "Turn on " + displayName;
        }

        /// <summary>The line under the switch.</summary>
        public static string Description(ModuleKind kind, bool signInStartOn)
        {
            if (IsBackground(kind))
            {
                return "Runs in the background while MosaicShell is running.";
            }

            const string Shows = "Shows on the desktop now and every time MosaicShell starts.";
            return signInStartOn
                ? Shows
                : Shows + " MosaicShell does not start when you sign in, so after you sign in it shows once you open MosaicShell.";
        }

        /// <summary>Whether the row offers to turn on "Start MosaicShell when I sign in".</summary>
        public static bool OffersSignInStart(ModuleKind kind, bool signInStartOn)
        {
            return !IsBackground(kind) && !signInStartOn;
        }

        private static bool IsBackground(ModuleKind kind)
        {
            return kind is ModuleKind.Capability or ModuleKind.Hybrid;
        }
    }
}
