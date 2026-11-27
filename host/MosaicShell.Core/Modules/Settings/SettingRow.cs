using MosaicShell.Core.HostPlatform;

namespace MosaicShell.Core.Modules.Settings
{
    /// <summary>The control a row is drawn with, and so the type of its value.</summary>
    public enum SettingKind
    {
        /// <summary>A switch; the value is a <see cref="bool"/>.</summary>
        Toggle,

        /// <summary>One of <see cref="SettingRow.Options"/>; the value is the option's <see cref="string"/> value.</summary>
        Choice,

        /// <summary>A number from <see cref="SettingRow.Min"/> to <see cref="SettingRow.Max"/>; the value is a <see cref="double"/>.</summary>
        Number,

        /// <summary>A keyboard shortcut such as <c>Ctrl+Alt+X</c>; the value is a <see cref="string"/>.</summary>
        Hotkey,

        /// <summary>The module's look from its style catalog; the value is the style id.</summary>
        Style,

        /// <summary>An ordered list of texts; the value is an <see cref="IReadOnlyList{T}"/> of <see cref="string"/>.</summary>
        TextList,

        /// <summary>An ordered list of named targets; the value is an <see cref="IReadOnlyList{T}"/> of <see cref="SettingPair"/>.</summary>
        ActionList,

        /// <summary>
        /// A slot the Hub fills with its own control (<see cref="SettingRow.CustomId"/>), such as Tessera's style
        /// cards. It has no value of its own: the control changes the settings through
        /// <see cref="ModuleSettingsDocument.Update{T}"/>.
        /// </summary>
        Custom,
    }

    /// <summary>When a change to a row is seen: the page says so before the change, not after.</summary>
    public enum SettingApplyEffect
    {
        /// <summary>The module reads it the next time it opens; nothing else to do.</summary>
        Live,

        /// <summary>A running capability has to be armed again to pick it up.</summary>
        ReArm,

        /// <summary>A widget on the desktop has to be closed and opened again to pick it up.</summary>
        RelaunchWidget,

        /// <summary>Only a new MosaicShell process picks it up.</summary>
        RestartHost,
    }

    /// <summary>One option of a <see cref="SettingKind.Choice"/> or <see cref="SettingKind.Style"/> row.</summary>
    public sealed record SettingOption(string Value, string Label);

    /// <summary>One entry of a <see cref="SettingKind.ActionList"/> row: a name and what it opens.</summary>
    public sealed record SettingPair(string Name, string Value);

    /// <summary>
    /// One row of a module page: what it is called, how it is drawn and when a change takes effect. Its value is read
    /// and written only through <see cref="ModuleSettingsDocument"/>.
    /// </summary>
    public sealed class SettingRow
    {
        internal SettingRow(string key, SettingKind kind, string label, Func<object, object> read, Action<object, object> write)
        {
            Key = key;
            Kind = kind;
            Label = label;
            Read = read;
            Write = write;
        }

        /// <summary>Stable within the module: the settings property it stands for.</summary>
        public string Key { get; }

        public SettingKind Kind { get; }

        public string Label { get; }

        /// <summary>The line under the label, or null.</summary>
        public string? Description { get; init; }

        public SettingApplyEffect Apply { get; init; } = SettingApplyEffect.Live;

        /// <summary>The key of a <see cref="SettingKind.Toggle"/> row: this row is shown only while that one is on.</summary>
        public string? VisibleWhen { get; internal set; }

        /// <summary>What the desktop must offer for this row to work, or null when it works everywhere.</summary>
        public DesktopFeature? Requires { get; init; }

        /// <summary>The options of a choice or style row; empty for other kinds.</summary>
        public IReadOnlyList<SettingOption> Options { get; init; } = [];

        public double? Min { get; init; }

        public double? Max { get; init; }

        public double? Step { get; init; }

        /// <summary>The unit a number is shown in (for example <c>s</c>), or null.</summary>
        public string? Unit { get; init; }

        /// <summary>Which Hub control fills a <see cref="SettingKind.Custom"/> row; null for other kinds.</summary>
        public string? CustomId { get; init; }

        internal Func<object, object> Read { get; }

        internal Action<object, object> Write { get; }
    }
}
