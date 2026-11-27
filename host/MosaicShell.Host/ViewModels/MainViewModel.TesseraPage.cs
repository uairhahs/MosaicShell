using System.Collections.ObjectModel;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using MosaicShell.Core.HostPlatform;
using MosaicShell.Core.Modules;
using MosaicShell.Core.Modules.Settings;
using MosaicShell.Core.Modules.Tessera;
using MosaicShell.Core.Runtime;
using MosaicShell.Core.Settings;
using MosaicShell.Core.Styles;
using MosaicShell.Host.ViewModels.ModulePage;

namespace MosaicShell.Host.ViewModels
{
    /// <summary>
    /// Tessera on the module page: its own controls (style, accent, material, motion, position, volume keys) are custom
    /// slots that keep using the Hub's Tessera properties. While the page is open, a change to one of those properties
    /// saves the fields that slot owns, so it never overwrites a plain row of the same page.
    /// </summary>
    public partial class MainViewModel
    {
        /// <summary>Property name to the slot it belongs to, and how long to wait for more changes before saving.</summary>
        private static readonly Dictionary<string, (string Key, int DebounceMs)> TesseraSlotProperties = new()
        {
            [nameof(ModuleStyle)] = ("Style", 0),
            [nameof(TesseraAccentHex)] = ("AccentColor", 300),
            [nameof(SelectedTesseraMaterial)] = ("Material", 0),
            [nameof(TesseraAni)] = ("Motion", 300),
            [nameof(TesseraAniDir)] = ("Motion", 300),
            [nameof(TesseraAniEase)] = ("Motion", 300),
            [nameof(TesseraAniSteps)] = ("Motion", 300),
            [nameof(TesseraAnimationDisplacement)] = ("Motion", 300),
            [nameof(SelectedTesseraPosition)] = ("Position", 0),
            [nameof(SelectedTesseraMonitor)] = ("Position", 0),
            [nameof(TesseraLegacyVol)] = ("VolumeKeys", 0),
            [nameof(TesseraLegacyStepPercent)] = ("VolumeKeys", 300),
        };

        /// <summary>True once Tessera's page has loaded its fields, so loading itself is not taken as a change.</summary>
        private bool _tesseraSlotsLive;

        /// <summary>The monitors as the OS names them, for the Position slot; index 1 is the first screen.</summary>
        public ObservableCollection<TesseraNamedChoice> TesseraMonitorChoices { get; } = [];

        [ObservableProperty] private TesseraNamedChoice? _selectedTesseraMonitor;

        partial void OnSelectedTesseraMonitorChanged(TesseraNamedChoice? value)
        {
            if (value is not null && int.TryParse(value.Code, out int index))
            {
                TesseraMonitorIndex = index;
            }
        }

        CustomRowViewModel IModulePageHost.CreateCustomRow(ModulePageViewModel page, SettingRow row, SettingAvailability availability)
        {
            return row.CustomId switch
            {
                "TesseraLook" => new TesseraLookSlotViewModel(page, row, availability, this),
                "TesseraAccent" => new TesseraAccentSlotViewModel(page, row, availability, this),
                "TesseraMaterial" => new TesseraMaterialSlotViewModel(page, row, availability, this),
                "TesseraMotion" => new TesseraMotionSlotViewModel(page, row, availability, this),
                "TesseraPosition" => new TesseraPositionSlotViewModel(page, row, availability, this),
                "TesseraVolumeKeys" => new TesseraVolumeKeysSlotViewModel(page, row, availability, this),
                _ => new CustomRowViewModel(page, row, availability),
            };
        }

        void IModulePageHost.Saved(string moduleId, string key)
        {
            if (!ModuleIds.IsTessera(moduleId))
            {
                return;
            }

            // The style preview draws the media strip from the Hub's copy; keep it in step with the switch.
            TesseraSettings saved = ModuleSettingsStore.Load(ModuleIds.Tessera, () => new TesseraSettings());
            TesseraMediaStrip = saved.ShowMediaStripOnVolume;
            TesseraFlyoutScalePercent = saved.FlyoutScalePercent;
        }

        /// <summary>Called once Tessera's fields are loaded into the Hub for its page.</summary>
        private void StartTesseraSlots()
        {
            LoadTesseraMonitors();
            _tesseraSlotsLive = true;
        }

        private void StopTesseraSlots()
        {
            _tesseraSlotsLive = false;
        }

        private void OnTesseraSlotPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (!_tesseraSlotsLive
                || e.PropertyName is not string name
                || !TesseraSlotProperties.TryGetValue(name, out (string Key, int DebounceMs) slot)
                || ModulePage?.Rows.OfType<CustomRowViewModel>().FirstOrDefault(r => r.Key == slot.Key) is not CustomRowViewModel row)
            {
                return;
            }

            row.Commit(settings => WriteTesseraSlot(slot.Key, (TesseraSettings)settings), slot.DebounceMs);
        }

        /// <summary>Writes the fields slot <paramref name="key"/> owns from the Hub's Tessera properties.</summary>
        private void WriteTesseraSlot(string key, TesseraSettings s)
        {
            switch (key)
            {
                case "Style":
                    s.Style = StyleIds.Normalize(ModuleStyle);
                    break;
                case "AccentColor":
                    s.AccentColor = TesseraAccentColor.NormalizeOrEmpty(TesseraAccentHex);
                    break;
                case "Material":
                    bool wasNative = s.UseNativeTranslucency;
                    PersistTesseraMaterial(s);
                    if (OperatingSystem.IsWindows() && s.UseNativeTranslucency != wasNative)
                    {
                        StatusMessage = "Saved. Restart MosaicShell so the new corners apply.";
                    }

                    break;
                case "Motion":
                    s.Ani = TesseraAni;
                    s.AniDir = TesseraAniDir;
                    s.AniEase = TesseraFlyoutAnimationPolicy.NormalizeEase(TesseraAniEase);
                    s.AniSteps = TesseraFlyoutAnimationPolicy.NormalizeAniSteps(TesseraAniSteps);
                    s.AnimationDisplacement = TesseraFlyoutAnimationPolicy.NormalizeDisplacementPx(TesseraAnimationDisplacement);
                    break;
                case "Position":
                    s.Position = SelectedTesseraPosition?.Code ?? TesseraPosition;
                    s.MonitorIndex = Math.Clamp((int)TesseraMonitorIndex, 1, 8);
                    break;
                case "VolumeKeys":
                    s.UseLegacyVolumeHooks = TesseraLegacyVol;
                    if (VolumeKeyStepPolicy.HasHookSwitch(VolumeStepSource))
                    {
                        s.LegacyVolumeStep = Math.Clamp((double)TesseraLegacyStepPercent, 1, 25) / 100.0;
                    }
                    else if (VolumeStepSource != VolumeKeyStepSource.None)
                    {
                        _ = _services.VolumeKeyStep.Write((int)TesseraLegacyStepPercent);
                    }

                    break;
                default:
                    break;
            }
        }

        private void LoadTesseraMonitors()
        {
            TesseraMonitorChoices.Clear();
            IReadOnlyList<Screen> screens = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime
            {
                MainWindow.Screens.All: { } all,
            }
                ? all
                : [];
            for (int i = 0; i < screens.Count; i++)
            {
                string name = string.IsNullOrWhiteSpace(screens[i].DisplayName) ? $"Display {i + 1}" : screens[i].DisplayName!;
                TesseraMonitorChoices.Add(new TesseraNamedChoice((i + 1).ToString(System.Globalization.CultureInfo.InvariantCulture),
                    screens[i].IsPrimary ? name + " (main)" : name));
            }

            int saved = Math.Clamp((int)TesseraMonitorIndex, 1, 8);
            if (TesseraMonitorChoices.Count < saved)
            {
                // The saved monitor is not connected now: keep it listed so it is not changed by opening the page.
                TesseraMonitorChoices.Add(new TesseraNamedChoice(saved.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    $"Display {saved} (not connected)"));
            }

            SelectedTesseraMonitor = TesseraMonitorChoices.FirstOrDefault(c => c.Code == saved.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
    }
}
