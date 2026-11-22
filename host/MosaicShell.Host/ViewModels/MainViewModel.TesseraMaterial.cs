using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using MosaicShell.Core.Capabilities.Platform;
using MosaicShell.Core.Modules;
using MosaicShell.Core.Modules.Tessera;
using MosaicShell.Core.Runtime;
using MosaicShell.Core.Settings;

namespace MosaicShell.Host.ViewModels
{
    /// <summary>
    /// The flyout material as one Hub choice (Solid, Frosted, Native Translucency), mapped to the saved settings by
    /// <see cref="TesseraMaterialChoicePolicy"/>. Native Translucency is listed only where this system can draw it; on
    /// Linux that is known shortly after start, so the list is rebuilt when the backend is found.
    /// </summary>
    public partial class MainViewModel
    {
        private bool _watchingNativeTranslucency;

        public ObservableCollection<TesseraMaterialOption> TesseraMaterialOptions { get; } = [];

        [ObservableProperty] private TesseraMaterialOption? _selectedTesseraMaterial;

        private TesseraMaterialChoice SelectedTesseraMaterialChoice =>
            SelectedTesseraMaterial?.Choice ?? TesseraMaterialChoice.Frosted;

        private void LoadTesseraMaterial(TesseraSettings settings)
        {
            WatchNativeTranslucency();
            SyncTesseraMaterialOptions(TesseraMaterialChoicePolicy.FromSettings(
                settings, TesseraNativeTranslucencySignOffPolicy.HubOffersNativeTranslucency));
        }

        private void PersistTesseraMaterial(TesseraSettings settings)
        {
            TesseraMaterialChoicePolicy.Apply(settings, SelectedTesseraMaterialChoice);
        }

        private void SyncTesseraMaterialOptions(TesseraMaterialChoice wanted)
        {
            TesseraMaterialOptions.Clear();
            foreach (TesseraMaterialChoice choice in TesseraMaterialChoicePolicy.Offered(
                         TesseraNativeTranslucencySignOffPolicy.HubOffersNativeTranslucency))
            {
                TesseraMaterialOptions.Add(TesseraMaterialOption.For(choice));
            }

            SelectedTesseraMaterial = TesseraMaterialOptions.FirstOrDefault(o => o.Choice == wanted)
                ?? TesseraMaterialOptions.First(static o => o.Choice == TesseraMaterialChoice.Frosted);
        }

        private void WatchNativeTranslucency()
        {
            if (_watchingNativeTranslucency)
            {
                return;
            }

            _watchingNativeTranslucency = true;
            NativeTranslucencyPlatform.Changed += (_, _) => Dispatcher.UIThread.Post(() =>
            {
                // Keep an unsaved pick; a saved Native choice that just became drawable is selected again.
                TesseraMaterialChoice wanted = SelectedTesseraMaterialChoice;
                TesseraSettings saved = ModuleSettingsStore.Load(ModuleIds.Tessera, () => new TesseraSettings());
                if (wanted == TesseraMaterialChoice.Frosted
                    && TesseraMaterialChoicePolicy.FromSettings(saved, TesseraNativeTranslucencySignOffPolicy.HubOffersNativeTranslucency)
                        == TesseraMaterialChoice.NativeTranslucency)
                {
                    wanted = TesseraMaterialChoice.NativeTranslucency;
                }

                SyncTesseraMaterialOptions(wanted);
            });
        }
    }

    public sealed record TesseraMaterialOption(TesseraMaterialChoice Choice, string Label)
    {
        public static TesseraMaterialOption For(TesseraMaterialChoice choice)
        {
            return new TesseraMaterialOption(choice, choice switch
            {
                TesseraMaterialChoice.Solid => "Solid",
                TesseraMaterialChoice.NativeTranslucency => "Native Translucency",
                _ => "Frosted",
            });
        }

        public override string ToString()
        {
            return Label;
        }
    }
}
