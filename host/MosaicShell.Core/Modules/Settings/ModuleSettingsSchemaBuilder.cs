using MosaicShell.Core.Capabilities.BuiltIn;
using MosaicShell.Core.HostPlatform;
using MosaicShell.Core.Runtime;
using MosaicShell.Core.Styles;

namespace MosaicShell.Core.Modules.Settings
{
    /// <summary>
    /// Builds a <see cref="ModuleSettingsSchema"/> whose rows read and write <typeparamref name="TSettings"/> properties
    /// through typed accessors, so a row can only stand for a property that exists. Rows go into the last section opened.
    /// </summary>
    public sealed class ModuleSettingsSchemaBuilder<TSettings>
        where TSettings : class, new()
    {
        private readonly string _moduleId;
        private readonly List<(string Title, List<SettingRow> Rows)> _sections = [];

        internal ModuleSettingsSchemaBuilder(string moduleId)
        {
            _moduleId = moduleId;
        }

        public ModuleSettingsSchemaBuilder<TSettings> Section(string title)
        {
            _sections.Add((title, []));
            return this;
        }

        /// <summary>The module's look, from <see cref="StyleCatalog"/>. An unknown id is stored as the catalog default.</summary>
        public ModuleSettingsSchemaBuilder<TSettings> Style(
            Func<TSettings, string> get, Action<TSettings, string> set, SettingApplyEffect apply, string label = "Style")
        {
            IReadOnlyList<SettingOption> options = [.. StyleCatalog.For(_moduleId).Select(d => new SettingOption(d.StyleId, d.DisplayName))];
            return Add(
                new SettingRow("Style", SettingKind.Style, label, s => Canonical(get((TSettings)s)), (s, v) => set((TSettings)s, Canonical((string)v)))
                {
                    Apply = apply,
                    Options = options,
                });
        }

        public ModuleSettingsSchemaBuilder<TSettings> Toggle(
            string key,
            string label,
            Func<TSettings, bool> get,
            Action<TSettings, bool> set,
            SettingApplyEffect apply,
            string? description = null,
            DesktopFeature? requires = null)
        {
            return Add(
                new SettingRow(key, SettingKind.Toggle, label, s => get((TSettings)s), (s, v) => set((TSettings)s, (bool)v))
                {
                    Apply = apply,
                    Description = description,
                    Requires = requires,
                });
        }

        public ModuleSettingsSchemaBuilder<TSettings> Choice(
            string key,
            string label,
            IReadOnlyList<SettingOption> options,
            Func<TSettings, string> get,
            Action<TSettings, string> set,
            SettingApplyEffect apply,
            string? description = null)
        {
            string Known(string value)
            {
                return options.FirstOrDefault(o => o.Value.Equals(value, StringComparison.OrdinalIgnoreCase))?.Value ?? options[0].Value;
            }

            return Add(
                new SettingRow(key, SettingKind.Choice, label, s => Known(get((TSettings)s)), (s, v) => set((TSettings)s, (string)v))
                {
                    Apply = apply,
                    Description = description,
                    Options = options,
                });
        }

        /// <summary>A whole number from <paramref name="min"/> to <paramref name="max"/>, stored as an <see cref="int"/>.</summary>
        public ModuleSettingsSchemaBuilder<TSettings> WholeNumber(
            string key,
            string label,
            int min,
            int max,
            int step,
            string? unit,
            Func<TSettings, int> get,
            Action<TSettings, int> set,
            SettingApplyEffect apply,
            string? description = null)
        {
            return Add(
                new SettingRow(
                    key,
                    SettingKind.Number,
                    label,
                    s => (double)Math.Clamp(get((TSettings)s), min, max),
                    (s, v) => set((TSettings)s, Math.Clamp((int)Math.Round((double)v), min, max)))
                {
                    Apply = apply,
                    Description = description,
                    Min = min,
                    Max = max,
                    Step = step,
                    Unit = unit,
                });
        }

        /// <summary>A number from <paramref name="min"/> to <paramref name="max"/>, stored as a <see cref="double"/>.</summary>
        public ModuleSettingsSchemaBuilder<TSettings> Number(
            string key,
            string label,
            double min,
            double max,
            double step,
            string? unit,
            Func<TSettings, double> get,
            Action<TSettings, double> set,
            SettingApplyEffect apply,
            string? description = null)
        {
            return Add(
                new SettingRow(
                    key,
                    SettingKind.Number,
                    label,
                    s => Math.Clamp(get((TSettings)s), min, max),
                    (s, v) => set((TSettings)s, Math.Clamp((double)v, min, max)))
                {
                    Apply = apply,
                    Description = description,
                    Min = min,
                    Max = max,
                    Step = step,
                    Unit = unit,
                });
        }

        /// <summary>A shortcut; one the OS keeps for itself, or one that does not parse, is stored as the module's safe default.</summary>
        public ModuleSettingsSchemaBuilder<TSettings> Hotkey(
            string key,
            string label,
            Func<TSettings, string> get,
            Action<TSettings, string> set,
            string? description = null)
        {
            return Add(
                new SettingRow(
                    key,
                    SettingKind.Hotkey,
                    label,
                    s => HotkeyGestureParser.EnsureRegisterable(_moduleId, get((TSettings)s)),
                    (s, v) => set((TSettings)s, HotkeyGestureParser.EnsureRegisterable(_moduleId, (string)v)))
                {
                    Apply = SettingApplyEffect.ReArm,
                    Description = description,
                    Requires = DesktopFeature.GlobalHotkeys,
                });
        }

        /// <summary>
        /// A list of texts, trimmed, without blanks or repeats (ignoring case). An empty list is stored as
        /// <paramref name="whenEmpty"/>.
        /// </summary>
        public ModuleSettingsSchemaBuilder<TSettings> TextList(
            string key,
            string label,
            IReadOnlyList<string> whenEmpty,
            Func<TSettings, IReadOnlyList<string>> get,
            Action<TSettings, List<string>> set,
            SettingApplyEffect apply,
            string? description = null)
        {
            List<string> Clean(IReadOnlyList<string> items)
            {
                List<string> kept = [.. items.Select(i => i.Trim()).Where(i => i.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase)];
                return kept.Count > 0 ? kept : [.. whenEmpty];
            }

            return Add(
                new SettingRow(key, SettingKind.TextList, label, s => (IReadOnlyList<string>)[.. get((TSettings)s)], (s, v) => set((TSettings)s, Clean((IReadOnlyList<string>)v)))
                {
                    Apply = apply,
                    Description = description,
                });
        }

        /// <summary>A list of named targets. A row without a target is dropped; one without a name is named after its target.</summary>
        public ModuleSettingsSchemaBuilder<TSettings> ActionList(
            string key,
            string label,
            Func<TSettings, IEnumerable<SettingPair>> get,
            Action<TSettings, IReadOnlyList<SettingPair>> set,
            SettingApplyEffect apply,
            string? description = null)
        {
            static IReadOnlyList<SettingPair> Clean(IReadOnlyList<SettingPair> pairs)
            {
                return [.. pairs
                    .Where(p => !string.IsNullOrWhiteSpace(p.Value))
                    .Select(p => new SettingPair(string.IsNullOrWhiteSpace(p.Name) ? p.Value.Trim() : p.Name.Trim(), p.Value.Trim()))];
            }

            return Add(
                new SettingRow(key, SettingKind.ActionList, label, s => (IReadOnlyList<SettingPair>)[.. get((TSettings)s)], (s, v) => set((TSettings)s, Clean((IReadOnlyList<SettingPair>)v)))
                {
                    Apply = apply,
                    Description = description,
                });
        }

        /// <summary>A slot the Hub fills with the control named <paramref name="customId"/>.</summary>
        public ModuleSettingsSchemaBuilder<TSettings> Custom(
            string key, string customId, string label, SettingApplyEffect apply, string? description = null)
        {
            return Add(
                new SettingRow(
                    key,
                    SettingKind.Custom,
                    label,
                    _ => throw new InvalidOperationException($"{key} is a custom slot; its control reads the settings itself."),
                    (_, _) => throw new InvalidOperationException($"{key} is a custom slot; change it with Update."))
                {
                    Apply = apply,
                    Description = description,
                    CustomId = customId,
                });
        }

        /// <summary>Shows the last row added only while the toggle <paramref name="toggleKey"/> is on.</summary>
        public ModuleSettingsSchemaBuilder<TSettings> ShownWhen(string toggleKey)
        {
            Current()[^1].VisibleWhen = toggleKey;
            return this;
        }

        public ModuleSettingsSchema Build()
        {
            return new ModuleSettingsSchema(
                _moduleId,
                [.. _sections.Select(s => new SettingSection(s.Title, s.Rows))],
                () => ModuleSettingsStore.Load(_moduleId, () => new TSettings()),
                s => ModuleSettingsStore.Save(_moduleId, (TSettings)s));
        }

        private ModuleSettingsSchemaBuilder<TSettings> Add(SettingRow row)
        {
            Current().Add(row);
            return this;
        }

        private List<SettingRow> Current()
        {
            return _sections.Count > 0
                ? _sections[^1].Rows
                : throw new InvalidOperationException("Open a Section before adding rows.");
        }

        private string Canonical(string styleId)
        {
            string normalized = StyleIds.Normalize(styleId);
            return StyleCatalog.IdsFor(_moduleId).FirstOrDefault(id => id.Equals(normalized, StringComparison.OrdinalIgnoreCase))
                ?? StyleCatalog.DefaultFor(_moduleId);
        }
    }
}
