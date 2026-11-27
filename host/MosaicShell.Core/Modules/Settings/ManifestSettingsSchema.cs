using System.Text.Json;
using MosaicShell.Core.Runtime;

namespace MosaicShell.Core.Modules.Settings
{
    /// <summary>One section of a module page as a manifest writes it.</summary>
    public sealed class ManifestSettingSection
    {
        public string Section { get; set; } = "";

        public List<ManifestSettingRow> Rows { get; set; } = [];
    }

    /// <summary>
    /// One row as a manifest writes it: the same kinds the built-in pages use (<see cref="SettingKind"/>), by name.
    /// </summary>
    public sealed class ManifestSettingRow
    {
        /// <summary>The key in the module's settings file. A Style row always uses <c>Style</c>.</summary>
        public string? Key { get; set; }

        public string? Kind { get; set; }

        /// <summary>Shown on the page; the key when omitted.</summary>
        public string? Label { get; set; }

        public string? Description { get; set; }

        /// <summary>
        /// When a change takes effect (<see cref="SettingApplyEffect"/>): <c>Live</c>, <c>ReArm</c>,
        /// <c>RelaunchWidget</c> or <c>RestartHost</c>. Omitted: a widget relaunches, a capability re-arms.
        /// </summary>
        public string? Apply { get; set; }

        /// <summary>The value before anything is saved, in the row kind's JSON type.</summary>
        public JsonElement? Default { get; set; }

        public List<SettingOption>? Options { get; set; }

        public double? Min { get; set; }

        public double? Max { get; set; }

        public double? Step { get; set; }

        public string? Unit { get; set; }

        /// <summary>The key of a Toggle row of the same page: this row shows only while that one is on.</summary>
        public string? ShownWhen { get; set; }
    }

    /// <summary>A third-party module's settings file: plain JSON keys, so the module reads them with any JSON reader.</summary>
    public sealed class ManifestSettingsBag : Dictionary<string, JsonElement>
    {
    }

    /// <summary>
    /// Builds a module page from the <c>Settings</c> a third-party manifest declares, over its own settings file. A row
    /// the Hub cannot draw (unknown kind, no key, a key already used, a choice without options, a number without a
    /// range) is skipped; a page with no row left is no page.
    /// </summary>
    public static class ManifestSettingsSchema
    {
        public static ModuleSettingsSchema? From(ModuleManifest manifest)
        {
            if (string.IsNullOrWhiteSpace(manifest.Id))
            {
                return null;
            }

            // A manifest that lists styles but no page still gets one: its style choice.
            List<ManifestSettingSection>? sections = manifest.Settings is { Count: > 0 }
                ? manifest.Settings
                : manifest.Styles is { Count: > 0 }
                    ? [new ManifestSettingSection { Section = "Look", Rows = [new ManifestSettingRow { Kind = nameof(SettingKind.Style) }] }]
                    : null;
            if (sections is null)
            {
                return null;
            }

            SettingApplyEffect byKind = manifest.Kind.Equals("Widget", StringComparison.OrdinalIgnoreCase)
                ? SettingApplyEffect.RelaunchWidget
                : SettingApplyEffect.ReArm;
            ModuleSettingsSchemaBuilder<ManifestSettingsBag> builder = ModuleSettingsSchema.For<ManifestSettingsBag>(manifest.Id);
            HashSet<string> keys = [];
            HashSet<string> toggles = [];
            List<(string Key, string Toggle)> shownWhen = [];
            bool any = false;

            foreach (ManifestSettingSection section in sections)
            {
                // One row at a time, so a key taken earlier in the same section is seen.
                List<ManifestSettingRow> drawable = [];
                foreach (ManifestSettingRow candidate in section.Rows)
                {
                    if (CanDraw(candidate, keys))
                    {
                        drawable.Add(candidate);
                        _ = keys.Add(KeyOf(candidate));
                    }
                }

                if (drawable.Count == 0)
                {
                    continue;
                }

                _ = builder.Section(string.IsNullOrWhiteSpace(section.Section) ? "Settings" : section.Section);
                foreach (ManifestSettingRow row in drawable)
                {
                    SettingKind kind = Enum.Parse<SettingKind>(row.Kind!, ignoreCase: true);
                    string key = KeyOf(row);
                    if (kind == SettingKind.Toggle)
                    {
                        _ = toggles.Add(key);
                    }

                    if (!string.IsNullOrWhiteSpace(row.ShownWhen))
                    {
                        shownWhen.Add((key, row.ShownWhen));
                    }

                    AddRow(builder, row, kind, key, ParseApply(row.Apply) ?? byKind);
                    any = true;
                }
            }

            if (!any)
            {
                return null;
            }

            ModuleSettingsSchema schema = builder.Build();
            foreach ((string key, string toggle) in shownWhen)
            {
                if (toggles.Contains(toggle) && toggle != key)
                {
                    schema.Row(key).VisibleWhen = toggle;
                }
            }

            return schema;
        }

        private static string KeyOf(ManifestSettingRow row)
        {
            return Enum.TryParse(row.Kind, ignoreCase: true, out SettingKind kind) && kind == SettingKind.Style ? "Style" : row.Key!.Trim();
        }

        private static bool CanDraw(ManifestSettingRow row, HashSet<string> taken)
        {
            // Custom slots are the Hub's own controls; a manifest cannot ask for one.
            if (!Enum.TryParse(row.Kind, ignoreCase: true, out SettingKind kind) || !Enum.IsDefined(kind) || kind == SettingKind.Custom)
            {
                return false;
            }

            string? key = kind == SettingKind.Style ? "Style" : row.Key?.Trim();
            return !string.IsNullOrEmpty(key) && !taken.Contains(key) && (kind == SettingKind.Style || key != "Style")
                && kind switch
                {
                    SettingKind.Choice => row.Options is { Count: > 0 } && row.Options.All(o => !string.IsNullOrEmpty(o.Value)),
                    SettingKind.Number => row.Min is double min && row.Max is double max && min < max,
                    _ => true,
                };
        }

        private static void AddRow(
            ModuleSettingsSchemaBuilder<ManifestSettingsBag> builder, ManifestSettingRow row, SettingKind kind, string key, SettingApplyEffect apply)
        {
            string label = string.IsNullOrWhiteSpace(row.Label) ? key : row.Label;
            switch (kind)
            {
                case SettingKind.Toggle:
                    bool on = Default(row, false);
                    _ = builder.Toggle(key, label, s => Read(s, key, on), (s, v) => Write(s, key, v), apply, row.Description);
                    break;
                case SettingKind.Choice:
                    string first = Default(row, row.Options![0].Value);
                    _ = builder.Choice(key, label, row.Options!, s => Read(s, key, first), (s, v) => Write(s, key, v), apply, row.Description);
                    break;
                case SettingKind.Number:
                    double start = Default(row, row.Min!.Value);
                    _ = builder.Number(
                        key,
                        label,
                        row.Min!.Value,
                        row.Max!.Value,
                        row.Step ?? 1,
                        row.Unit,
                        s => Read(s, key, start),
                        (s, v) => Write(s, key, v),
                        apply,
                        row.Description);
                    break;
                case SettingKind.Hotkey:
                    string gesture = Default(row, "");
                    _ = builder.Hotkey(key, label, s => Read(s, key, gesture), (s, v) => Write(s, key, v), row.Description);
                    break;
                case SettingKind.Style:
                    string style = Default(row, "");
                    _ = builder.Style(s => Read(s, key, style), (s, v) => Write(s, key, v), apply, string.IsNullOrWhiteSpace(row.Label) ? "Style" : row.Label);
                    break;
                case SettingKind.TextList:
                    List<string> items = Default<List<string>>(row, []);
                    _ = builder.TextList(key, label, [], s => Read(s, key, items), (s, v) => Write(s, key, v), apply, row.Description);
                    break;
                case SettingKind.ActionList:
                    List<SettingPair> pairs = Default<List<SettingPair>>(row, []);
                    _ = builder.ActionList(key, label, s => Read(s, key, pairs), (s, v) => Write(s, key, v.ToList()), apply, row.Description);
                    break;
                default:
                    break;
            }
        }

        private static SettingApplyEffect? ParseApply(string? apply)
        {
            return Enum.TryParse(apply, ignoreCase: true, out SettingApplyEffect effect) && Enum.IsDefined(effect) ? effect : null;
        }

        private static T Default<T>(ManifestSettingRow row, T fallback)
        {
            return row.Default is JsonElement element && TryConvert(element, out T? value) ? value! : fallback;
        }

        /// <summary>The stored value, or <paramref name="fallback"/> when it is missing or not of the row's type.</summary>
        private static T Read<T>(ManifestSettingsBag bag, string key, T fallback)
        {
            return bag.TryGetValue(key, out JsonElement element) && TryConvert(element, out T? value) ? value! : fallback;
        }

        private static void Write<T>(ManifestSettingsBag bag, string key, T value)
        {
            bag[key] = JsonSerializer.SerializeToElement(value);
        }

        private static bool TryConvert<T>(JsonElement element, out T? value)
        {
            try
            {
                value = element.Deserialize<T>();
                return value is not null;
            }
            catch (JsonException)
            {
                value = default;
                return false;
            }
            catch (InvalidOperationException)
            {
                value = default;
                return false;
            }
        }
    }
}
