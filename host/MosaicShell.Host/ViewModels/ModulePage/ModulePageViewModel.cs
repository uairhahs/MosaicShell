using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MosaicShell.Core.Modules;
using MosaicShell.Core.Modules.Settings;

namespace MosaicShell.Host.ViewModels.ModulePage
{
    /// <summary>A titled group of rows on the page.</summary>
    public sealed record SettingSectionViewModel(string Title, IReadOnlyList<SettingRowViewModel> Rows);

    /// <summary>
    /// A module's page drawn from its Core schema: the on switch, its sections of rows, and Remove. Every change is
    /// saved as it is made (instant apply, <see cref="ModuleSettingsApplyPolicy"/>): clicks at once, dragging and typing
    /// once they pause. What the running module needs next runs once for changes made together.
    /// </summary>
    public sealed partial class ModulePageViewModel : ObservableObject
    {
        /// <summary>How long after the last saved change before its follow-up (a re-arm, a relaunch) runs.</summary>
        public const int FollowUpDelayMs = 400;

        private readonly ModuleInfo _info;
        private readonly IModulePageHost _host;
        private readonly ModuleSettingsDocument _document;
        private readonly Dictionary<string, (DispatcherTimer Timer, object Value)> _pendingWrites = [];
        private readonly DispatcherTimer _followUpTimer;
        private SettingFollowUp _pendingFollowUp;
        private bool _loadingSwitch;

        public ModulePageViewModel(ModuleInfo info, ModuleSettingsSchema schema, IModulePageHost host)
        {
            _info = info;
            _host = host;
            _document = schema.Open();
            _followUpTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(FollowUpDelayMs) };
            _followUpTimer.Tick += async (_, _) =>
            {
                _followUpTimer.Stop();
                await RunFollowUpAsync();
            };

            Sections = [.. schema.Sections.Select(s => new SettingSectionViewModel(s.Title, [.. s.Rows.Select(CreateRow)]))];
            foreach (SettingRowViewModel row in Rows.Where(static r => r is not CustomRowViewModel))
            {
                row.Load(_document.Get(row.Key));
            }

            RefreshVisibility();
            RefreshSwitch();
        }

        public string ModuleId => _info.Id;

        public string Title => _info.DisplayName;

        public string Summary => ModuleUsageGuide.Summary(_info.Id);

        public string HowTo => ModuleUsageGuide.HowToTrigger(_info.Id, _host.Features);

        public bool HasHowTo => HowTo.Length > 0;

        public IReadOnlyList<SettingSectionViewModel> Sections { get; }

        public IEnumerable<SettingRowViewModel> Rows => Sections.SelectMany(s => s.Rows);

        public string SwitchLabel => ModuleOnSwitch.Label(_info.DisplayName);

        public string SwitchDescription => ModuleOnSwitch.Description(_info.Kind, _host.IsSignInStartOn);

        public bool OffersSignInStart => ModuleOnSwitch.OffersSignInStart(_info.Kind, _host.IsSignInStartOn);

        /// <summary>Capabilities with an overlay can be shown on screen from the page; widgets are on the desktop already.</summary>
        public bool CanPreview => _info.Kind is ModuleKind.Capability or ModuleKind.Hybrid;

        public string RemoveLabel => "Remove " + _info.DisplayName;

        public IReadOnlyList<string> LaunchTargetLabels => _host.LaunchTargetLabels;

        [ObservableProperty] private bool _isOn;
        [ObservableProperty] private bool _isSwitching;

        /// <summary>
        /// What the pinned header shows beside "Show it on screen", or null: Tessera's live preview, so it stays in view
        /// while rows far down the page change.
        /// </summary>
        [ObservableProperty] private object? _headerPreview;

        /// <summary>The shortcut row listening for a key press, if any; the window routes key presses to it.</summary>
        [ObservableProperty] private HotkeyRowViewModel? _capturingHotkey;

        public SettingRowViewModel Row(string key)
        {
            return Rows.First(r => r.Key == key);
        }

        internal bool TryResolveLaunchTarget(string label, out string target, out string displayName)
        {
            return _host.TryResolveLaunchTarget(label, out target, out displayName);
        }

        /// <summary>Takes a row's change: saved at once for a click, after a pause for dragging and typing.</summary>
        internal void Commit(SettingRowViewModel row, object value)
        {
            Schedule(row, value, ModuleSettingsApplyPolicy.DebounceMs(row.Row.Kind));
        }

        /// <summary>Takes a custom slot's change: the fields it owns, saved after the slot's own pause.</summary>
        internal void CommitCustom(CustomRowViewModel row, Action<object> change, int debounceMs)
        {
            Schedule(row, change, debounceMs);
        }

        private void Schedule(SettingRowViewModel row, object value, int debounce)
        {
            if (debounce == 0)
            {
                Write(row, value);
                return;
            }

            if (_pendingWrites.TryGetValue(row.Key, out (DispatcherTimer Timer, object Value) pending))
            {
                pending.Timer.Stop();
            }

            DispatcherTimer timer = new() { Interval = TimeSpan.FromMilliseconds(debounce) };
            timer.Tick += (_, _) =>
            {
                timer.Stop();
                if (_pendingWrites.Remove(row.Key, out (DispatcherTimer Timer, object Value) due))
                {
                    Write(row, due.Value);
                }
            };
            _pendingWrites[row.Key] = (timer, value);
            timer.Start();
        }

        /// <summary>Saves every change still waiting and runs its follow-up now (leaving the page, and tests).</summary>
        public async Task FlushAsync()
        {
            foreach ((string key, (DispatcherTimer timer, object value)) in _pendingWrites.ToList())
            {
                timer.Stop();
                _ = _pendingWrites.Remove(key);
                Write(Row(key), value);
            }

            _followUpTimer.Stop();
            await RunFollowUpAsync();
        }

        private void Write(SettingRowViewModel row, object value)
        {
            try
            {
                if (row is CustomRowViewModel)
                {
                    _document.Update(row.Key, (Action<object>)value);
                }
                else
                {
                    _document.Set(row.Key, value);
                }
            }
            catch (ArgumentException ex)
            {
                _host.Report(ex.Message);
            }

            // Show the value as saved: a clamped number or a refused shortcut reads back as what was stored.
            if (row is not CustomRowViewModel)
            {
                row.Load(_document.Get(row.Key));
            }

            RefreshVisibility();
            _host.Saved(ModuleId, row.Key);
            SettingFollowUp next = ModuleSettingsApplyPolicy.FollowUp(row.Row.Apply, _host.IsArmed(ModuleId), _host.IsWidgetOpen(ModuleId));
            _pendingFollowUp = ModuleSettingsApplyPolicy.Combine(_pendingFollowUp, next);
            if (_pendingFollowUp != SettingFollowUp.None)
            {
                _followUpTimer.Stop();
                _followUpTimer.Start();
            }
        }

        private async Task RunFollowUpAsync()
        {
            SettingFollowUp followUp = _pendingFollowUp;
            _pendingFollowUp = SettingFollowUp.None;
            if (followUp != SettingFollowUp.None)
            {
                await _host.FollowUpAsync(ModuleId, followUp);
            }
        }

        partial void OnIsOnChanged(bool value)
        {
            if (!_loadingSwitch)
            {
                _ = SetOnAsync(value);
            }
        }

        /// <summary>Runs the switch's steps in order and stops at the first that fails.</summary>
        public async Task SetOnAsync(bool turnOn)
        {
            IsSwitching = true;
            try
            {
                foreach (ModuleSwitchStep step in ModuleOnSwitch.Plan(_info.Kind, _host.IsInstalled(ModuleId), turnOn))
                {
                    if (!await _host.RunStepAsync(ModuleId, step))
                    {
                        break;
                    }
                }
            }
            finally
            {
                IsSwitching = false;
                RefreshSwitch();
            }
        }

        [RelayCommand]
        private void TurnOnSignInStart()
        {
            _host.TurnOnSignInStart();
            OnPropertyChanged(nameof(SwitchDescription));
            OnPropertyChanged(nameof(OffersSignInStart));
        }

        [RelayCommand]
        private Task Preview()
        {
            return _host.PreviewAsync(ModuleId);
        }

        [RelayCommand]
        private void Remove()
        {
            _host.Remove(ModuleId);
        }

        /// <summary>Reads the switch again, after the module changed outside the page.</summary>
        public void RefreshSwitch()
        {
            _loadingSwitch = true;
            try
            {
                IsOn = ModuleOnSwitch.IsOn(_info.Kind, _host.IsInstalled(ModuleId), _host.IsArmed(ModuleId), _host.IsWidgetOpen(ModuleId));
            }
            finally
            {
                _loadingSwitch = false;
            }
        }

        private void RefreshVisibility()
        {
            foreach (SettingRowViewModel row in Rows)
            {
                row.IsVisible = row.Row.VisibleWhen is not string toggle || _document.Get<bool>(toggle);
            }
        }

        private SettingRowViewModel CreateRow(SettingRow row)
        {
            SettingAvailability availability = SettingAvailability.Of(row, _host.Features);
            return row.Kind switch
            {
                SettingKind.Toggle => new ToggleRowViewModel(this, row, availability),
                SettingKind.Choice or SettingKind.Style => new ChoiceRowViewModel(this, row, availability),
                SettingKind.Number => new NumberRowViewModel(this, row, availability),
                SettingKind.Hotkey => new HotkeyRowViewModel(this, row, availability),
                SettingKind.TextList => new TextListRowViewModel(this, row, availability),
                SettingKind.ActionList => new ActionListRowViewModel(this, row, availability),
                SettingKind.Custom => _host.CreateCustomRow(this, row, availability),
                _ => throw new NotSupportedException($"No row for {row.Kind}."),
            };
        }
    }
}
