using MosaicShell.Core.Modules.Settings;

namespace MosaicShell.Host.ViewModels.ModulePage
{
    /// <summary>A Tessera slot: its control binds to the Hub's Tessera properties through <see cref="Hub"/>.</summary>
    public abstract class TesseraSlotViewModel(ModulePageViewModel page, SettingRow row, SettingAvailability availability, MainViewModel hub)
        : CustomRowViewModel(page, row, availability)
    {
        public MainViewModel Hub { get; } = hub;
    }

    /// <summary>Tessera's live preview in the page's pinned header.</summary>
    public sealed class TesseraHeaderPreviewViewModel(MainViewModel hub)
    {
        public MainViewModel Hub { get; } = hub;
    }

    /// <summary>Style: the style choice, first on the page; its preview is pinned in the header.</summary>
    public sealed class TesseraLookSlotViewModel(ModulePageViewModel page, SettingRow row, SettingAvailability availability, MainViewModel hub)
        : TesseraSlotViewModel(page, row, availability, hub);

    public sealed class TesseraAccentSlotViewModel(ModulePageViewModel page, SettingRow row, SettingAvailability availability, MainViewModel hub)
        : TesseraSlotViewModel(page, row, availability, hub);

    public sealed class TesseraMaterialSlotViewModel(ModulePageViewModel page, SettingRow row, SettingAvailability availability, MainViewModel hub)
        : TesseraSlotViewModel(page, row, availability, hub);

    public sealed class TesseraMotionSlotViewModel(ModulePageViewModel page, SettingRow row, SettingAvailability availability, MainViewModel hub)
        : TesseraSlotViewModel(page, row, availability, hub);

    /// <summary>Position: the monitor picture with nine anchors, and the monitor by name.</summary>
    public sealed class TesseraPositionSlotViewModel(ModulePageViewModel page, SettingRow row, SettingAvailability availability, MainViewModel hub)
        : TesseraSlotViewModel(page, row, availability, hub);

    public sealed class TesseraVolumeKeysSlotViewModel(ModulePageViewModel page, SettingRow row, SettingAvailability availability, MainViewModel hub)
        : TesseraSlotViewModel(page, row, availability, hub);
}
