namespace MosaicShell.Core.Modules.Settings
{
    /// <summary>A titled group of rows on a module page.</summary>
    public sealed record SettingSection(string Title, IReadOnlyList<SettingRow> Rows);

    /// <summary>
    /// A module's page as sections of typed rows, bound to its settings class. The Hub draws any schema with one view;
    /// a module adds rows here instead of adding Host code.
    /// </summary>
    public sealed class ModuleSettingsSchema
    {
        private readonly Func<object> _load;
        private readonly Action<object> _save;
        private readonly Dictionary<string, SettingRow> _byKey;

        internal ModuleSettingsSchema(string moduleId, IReadOnlyList<SettingSection> sections, Func<object> load, Action<object> save)
        {
            ModuleId = moduleId;
            Sections = sections;
            _load = load;
            _save = save;
            _byKey = sections.SelectMany(s => s.Rows).ToDictionary(r => r.Key, StringComparer.Ordinal);
        }

        public string ModuleId { get; }

        public IReadOnlyList<SettingSection> Sections { get; }

        /// <summary>Every row, in page order.</summary>
        public IEnumerable<SettingRow> Rows => Sections.SelectMany(s => s.Rows);

        /// <exception cref="KeyNotFoundException">No row has that key.</exception>
        public SettingRow Row(string key)
        {
            return _byKey.TryGetValue(key, out SettingRow? row)
                ? row
                : throw new KeyNotFoundException($"{ModuleId} has no setting row '{key}'.");
        }

        /// <summary>Loads the module's settings file (or its defaults) for reading and writing rows.</summary>
        public ModuleSettingsDocument Open()
        {
            return new ModuleSettingsDocument(this);
        }

        internal object Load()
        {
            return _load();
        }

        internal void Save(object settings)
        {
            _save(settings);
        }

        /// <summary>Starts a schema for a module whose settings are a <typeparamref name="TSettings"/>.</summary>
        public static ModuleSettingsSchemaBuilder<TSettings> For<TSettings>(string moduleId)
            where TSettings : class, new()
        {
            return new ModuleSettingsSchemaBuilder<TSettings>(moduleId);
        }
    }

    /// <summary>
    /// One open page. Every read and every change goes to the settings file, so plain rows and custom slots that
    /// change the same file never undo each other. A change is checked against its row, normalised by the row and saved
    /// at once (instant apply).
    /// </summary>
    public sealed class ModuleSettingsDocument
    {
        internal ModuleSettingsDocument(ModuleSettingsSchema schema)
        {
            Schema = schema;
        }

        public ModuleSettingsSchema Schema { get; }

        /// <exception cref="InvalidOperationException">The row is a custom slot.</exception>
        public object Get(string key)
        {
            return Schema.Row(key).Read(Schema.Load());
        }

        public T Get<T>(string key)
        {
            return (T)Get(key);
        }

        /// <summary>The settings as saved now, for a custom slot to show.</summary>
        public T Read<T>()
            where T : class
        {
            return (T)Schema.Load();
        }

        /// <summary>Writes one row and saves the file.</summary>
        /// <exception cref="ArgumentException">The value is the wrong type for the row, or not one of a choice's options.</exception>
        /// <exception cref="InvalidOperationException">The row is a custom slot.</exception>
        public void Set(string key, object value)
        {
            SettingRow row = Schema.Row(key);
            object settings = Schema.Load();
            row.Write(settings, Checked(row, value));
            Schema.Save(settings);
        }

        /// <summary>Lets custom slot <paramref name="key"/> change the settings, then saves the file.</summary>
        /// <exception cref="InvalidOperationException">The row is not a custom slot.</exception>
        public void Update<T>(string key, Action<T> change)
            where T : class
        {
            if (Schema.Row(key).Kind != SettingKind.Custom)
            {
                throw new InvalidOperationException($"{key} is not a custom slot; use Set.");
            }

            T settings = (T)Schema.Load();
            change(settings);
            Schema.Save(settings);
        }

        private static object Checked(SettingRow row, object value)
        {
            switch (row.Kind)
            {
                case SettingKind.Toggle when value is bool:
                case SettingKind.Hotkey or SettingKind.Style when value is string:
                case SettingKind.TextList when value is IReadOnlyList<string>:
                case SettingKind.ActionList when value is IReadOnlyList<SettingPair>:
                    return value;
                case SettingKind.Choice when value is string choice:
                    return row.Options.FirstOrDefault(o => o.Value.Equals(choice, StringComparison.OrdinalIgnoreCase))?.Value
                        ?? throw new ArgumentException($"'{choice}' is not an option of {row.Key}.", nameof(value));
                case SettingKind.Number when value is double or float or int or long or decimal:
                    double number = Convert.ToDouble(value, System.Globalization.CultureInfo.InvariantCulture);
                    return Math.Clamp(number, row.Min ?? double.MinValue, row.Max ?? double.MaxValue);
                case SettingKind.Custom:
                    throw new InvalidOperationException($"{row.Key} is a custom slot; change it with Update.");
                default:
                    throw new ArgumentException($"{row.Key} is a {row.Kind} row; {value.GetType().Name} does not fit it.", nameof(value));
            }
        }
    }
}
