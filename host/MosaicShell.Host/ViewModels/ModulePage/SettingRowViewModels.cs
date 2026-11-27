using System.Collections.ObjectModel;
using Avalonia.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MosaicShell.Core.Capabilities.BuiltIn;
using MosaicShell.Core.HostPlatform;
using MosaicShell.Core.Modules.Settings;
using MosaicShell.Host.Input;

namespace MosaicShell.Host.ViewModels.ModulePage
{
    /// <summary>
    /// One row of a module page, drawn from its <see cref="SettingRow"/>. A change goes to the page, which saves it
    /// (after the row kind's pause) and runs what follows; the row then shows the value as saved.
    /// </summary>
    public abstract partial class SettingRowViewModel(ModulePageViewModel page, SettingRow row, SettingAvailability availability)
        : ObservableObject
    {
        private bool _loading;

        public SettingRow Row { get; } = row;

        public string Key => Row.Key;

        public string Label => Row.Label;

        public string Description => Row.Description ?? "";

        public bool HasDescription => !string.IsNullOrEmpty(Row.Description);

        /// <summary>Said before a change that only shows after a restart.</summary>
        public string Note => ModuleSettingsApplyPolicy.BeforeChangeNote(Row.Apply) ?? "";

        public bool HasNote => Note.Length > 0;

        public bool IsAvailable { get; } = availability.IsAvailable;

        public string UnavailableReason { get; } = availability.Reason ?? "";

        [ObservableProperty] private bool _isVisible = true;

        protected ModulePageViewModel Page { get; } = page;

        /// <summary>Shows <paramref name="value"/> without treating it as a change.</summary>
        internal void Load(object value)
        {
            _loading = true;
            try
            {
                Apply(value);
            }
            finally
            {
                _loading = false;
            }
        }

        protected abstract void Apply(object value);

        protected void Changed(object value)
        {
            if (!_loading)
            {
                Page.Commit(this, value);
            }
        }
    }

    public sealed partial class ToggleRowViewModel(ModulePageViewModel page, SettingRow row, SettingAvailability availability)
        : SettingRowViewModel(page, row, availability)
    {
        [ObservableProperty] private bool _value;

        protected override void Apply(object value)
        {
            Value = (bool)value;
        }

        partial void OnValueChanged(bool value)
        {
            Changed(value);
        }
    }

    /// <summary>A choice or a style: one of the row's options.</summary>
    public sealed partial class ChoiceRowViewModel(ModulePageViewModel page, SettingRow row, SettingAvailability availability)
        : SettingRowViewModel(page, row, availability)
    {
        public IReadOnlyList<SettingOption> Options => Row.Options;

        [ObservableProperty] private SettingOption? _selected;

        protected override void Apply(object value)
        {
            string id = (string)value;
            Selected = Options.FirstOrDefault(o => o.Value.Equals(id, StringComparison.OrdinalIgnoreCase)) ?? Options.FirstOrDefault();
        }

        partial void OnSelectedChanged(SettingOption? value)
        {
            if (value is not null)
            {
                Changed(value.Value);
            }
        }
    }

    public sealed partial class NumberRowViewModel(ModulePageViewModel page, SettingRow row, SettingAvailability availability)
        : SettingRowViewModel(page, row, availability)
    {
        public double Minimum => Row.Min ?? 0;

        public double Maximum => Row.Max ?? 100;

        public double Step => Row.Step ?? 1;

        public string Unit => Row.Unit ?? "";

        [ObservableProperty] private double _value;

        public string ValueText => $"{Value:0} {Unit}".Trim();

        protected override void Apply(object value)
        {
            Value = (double)value;
        }

        partial void OnValueChanged(double value)
        {
            OnPropertyChanged(nameof(ValueText));
            Changed(value);
        }
    }

    public sealed partial class HotkeyRowViewModel(ModulePageViewModel page, SettingRow row, SettingAvailability availability)
        : SettingRowViewModel(page, row, availability)
    {
        private const string IdleHint = "Choose Change, then press the new shortcut.";

        [ObservableProperty] private string _gesture = "";
        [ObservableProperty] private bool _isCapturing;
        [ObservableProperty] private string _hint = IdleHint;

        protected override void Apply(object value)
        {
            Gesture = (string)value;
        }

        [RelayCommand]
        private void BeginCapture()
        {
            IsCapturing = true;
            Hint = "Press the shortcut now. Esc cancels.";
            Page.CapturingHotkey = this;
        }

        [RelayCommand]
        private void CancelCapture()
        {
            IsCapturing = false;
            Hint = IdleHint;
            Page.CapturingHotkey = null;
        }

        /// <summary>Takes a key press while capturing; true when the press was used.</summary>
        public bool TryCapture(KeyEventArgs e)
        {
            if (!IsCapturing)
            {
                return false;
            }

            if (e.Key == Avalonia.Input.Key.Escape)
            {
                CancelCapture();
                e.Handled = true;
                return true;
            }

            if (!HotkeyCapture.TryFormat(e, out string gesture))
            {
                return false;
            }

            e.Handled = true;
            return Capture(gesture);
        }

        /// <summary>Uses <paramref name="gesture"/> as the new shortcut unless the OS keeps it.</summary>
        public bool Capture(string gesture)
        {
            if (HotkeyGestureParser.IsLikelyOsReserved(gesture))
            {
                Hint = HostPlatformText.Current.ReservedShortcut(gesture);
                return true;
            }

            CancelCapture();
            Gesture = gesture;
            Changed(gesture);
            return true;
        }
    }

    /// <summary>A list of texts picked from the launch targets (Inlay's pins).</summary>
    public sealed partial class TextListRowViewModel(ModulePageViewModel page, SettingRow row, SettingAvailability availability)
        : SettingRowViewModel(page, row, availability)
    {
        public ObservableCollection<string> Items { get; } = [];

        public IReadOnlyList<string> Choices => Page.LaunchTargetLabels;

        [ObservableProperty] private string _query = "";

        public bool IsEmpty => Items.Count == 0;

        protected override void Apply(object value)
        {
            Items.Clear();
            foreach (string item in (IReadOnlyList<string>)value)
            {
                Items.Add(item);
            }

            OnPropertyChanged(nameof(IsEmpty));
        }

        [RelayCommand]
        private void Add()
        {
            if (!Page.TryResolveLaunchTarget(Query, out string item, out _)
                || Items.Contains(item, StringComparer.OrdinalIgnoreCase))
            {
                return;
            }

            Items.Add(item);
            Query = "";
            Push();
        }

        [RelayCommand]
        private void Remove(string? item)
        {
            if (item is not null && Items.Remove(item))
            {
                Push();
            }
        }

        private void Push()
        {
            OnPropertyChanged(nameof(IsEmpty));
            Changed((IReadOnlyList<string>)[.. Items]);
        }
    }

    /// <summary>A list of named targets (Chord's actions).</summary>
    public sealed partial class ActionListRowViewModel(ModulePageViewModel page, SettingRow row, SettingAvailability availability)
        : SettingRowViewModel(page, row, availability)
    {
        public ObservableCollection<SettingPair> Items { get; } = [];

        public IReadOnlyList<string> Choices => Page.LaunchTargetLabels;

        [ObservableProperty] private string _name = "";
        [ObservableProperty] private string _query = "";

        public bool IsEmpty => Items.Count == 0;

        protected override void Apply(object value)
        {
            Items.Clear();
            foreach (SettingPair pair in (IReadOnlyList<SettingPair>)value)
            {
                Items.Add(pair);
            }

            OnPropertyChanged(nameof(IsEmpty));
        }

        [RelayCommand]
        private void Add()
        {
            if (!Page.TryResolveLaunchTarget(Query, out string target, out string display))
            {
                return;
            }

            Items.Add(new SettingPair(string.IsNullOrWhiteSpace(Name) ? display : Name.Trim(), target));
            Name = "";
            Query = "";
            Push();
        }

        [RelayCommand]
        private void Remove(SettingPair? item)
        {
            if (item is not null && Items.Remove(item))
            {
                Push();
            }
        }

        private void Push()
        {
            OnPropertyChanged(nameof(IsEmpty));
            Changed((IReadOnlyList<SettingPair>)[.. Items]);
        }
    }
}
