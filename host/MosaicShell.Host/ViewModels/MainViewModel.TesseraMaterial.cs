using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using MosaicShell.Core.Capabilities.Platform;
using MosaicShell.Core.Modules;
using MosaicShell.Core.Modules.Tessera;
using MosaicShell.Core.Runtime;
using MosaicShell.Core.Services.Xdg;
using MosaicShell.Core.Settings;
using MosaicShell.Core.Styles;

namespace MosaicShell.Host.ViewModels
{
    /// <summary>
    /// The flyout material as one Hub choice (Solid, Frosted, Native Translucency), mapped to the saved settings by
    /// <see cref="TesseraMaterialChoicePolicy"/>. Where this system cannot draw Native Translucency it is listed greyed
    /// out with the reason; on Linux the backend is known shortly after start, so the list is rebuilt when it is found.
    /// </summary>
    public partial class MainViewModel
    {
        private bool _watchingNativeTranslucency;

        public ObservableCollection<TesseraMaterialOption> TesseraMaterialOptions { get; } = [];

        [ObservableProperty] private TesseraMaterialOption? _selectedTesseraMaterial;

        private TesseraMaterialChoice SelectedTesseraMaterialChoice =>
            SelectedTesseraMaterial?.Choice ?? TesseraMaterialChoice.Frosted;

        /// <summary>
        /// False for a style the material does not change (<see cref="TesseraMaterialChoicePolicy.IsSelectable"/>). The
        /// selection itself is left alone, so the saved choice still applies to the other styles.
        /// </summary>
        public bool TesseraMaterialSelectable => TesseraMaterialChoicePolicy.IsSelectable(ModuleStyle);

        /// <summary>Shown under the disabled choice; empty where the material applies.</summary>
        public string TesseraMaterialFixedNote => TesseraMaterialSelectable
            ? ""
            : $"{StyleIds.DisplayName(ModuleStyle)} always uses a solid panel, so the material does not change it.";

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

        /// <summary>Why Native Translucency is greyed out; empty when it can be picked or is not listed.</summary>
        public string TesseraNativeUnavailableNote =>
            TesseraMaterialOptions.Any(static o => !o.IsAvailable)
                ? TesseraMaterialChoicePolicy.NativeUnavailableText(OperatingSystem.IsWindows(), XdgCurrentDesktop.Current)
                : "";

        private void SyncTesseraMaterialOptions(TesseraMaterialChoice wanted)
        {
            bool nativeDrawable = TesseraNativeTranslucencySignOffPolicy.HubOffersNativeTranslucency;
            TesseraMaterialOptions.Clear();
            foreach (TesseraMaterialChoice choice in TesseraMaterialChoicePolicy.Listed(
                         TesseraNativeTranslucencySignOffPolicy.EvalCompleteForThisPlatform))
            {
                TesseraMaterialOptions.Add(TesseraMaterialOption.For(choice, choice != TesseraMaterialChoice.NativeTranslucency || nativeDrawable));
            }

            SelectedTesseraMaterial = TesseraMaterialOptions.FirstOrDefault(o => o.Choice == wanted && o.IsAvailable)
                ?? TesseraMaterialOptions.First(static o => o.Choice == TesseraMaterialChoice.Frosted);
            OnPropertyChanged(nameof(TesseraNativeUnavailableNote));
        }

        private void WatchNativeTranslucency()
        {
            if (_watchingNativeTranslucency)
            {
                return;
            }

            _watchingNativeTranslucency = true;

            // The event is static, so it holds only a weak reference: a strong one kept every view model that ever
            // opened Tessera's settings alive. Once the view model is gone, the handler removes itself.
            WeakReference<MainViewModel> weak = new(this);
            void Handler(object? sender, EventArgs e)
            {
                if (!weak.TryGetTarget(out MainViewModel? vm))
                {
                    NativeTranslucencyPlatform.Changed -= Handler;
                    return;
                }

                Dispatcher.UIThread.Post(vm.OnNativeTranslucencyChanged);
            }

            NativeTranslucencyPlatform.Changed += Handler;
        }

        private void OnNativeTranslucencyChanged()
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
        }
    }

    /// <summary>One material choice; <c>IsAvailable</c> is false for one this system cannot draw (listed, not pickable).</summary>
    public sealed record TesseraMaterialOption(TesseraMaterialChoice Choice, string Label, bool IsAvailable = true)
    {
        public static TesseraMaterialOption For(TesseraMaterialChoice choice, bool isAvailable = true)
        {
            return new TesseraMaterialOption(choice, choice switch
            {
                TesseraMaterialChoice.Solid => "Solid",
                TesseraMaterialChoice.NativeTranslucency => "Native Translucency",
                _ => "Frosted",
            }, isAvailable);
        }

        public override string ToString()
        {
            return Label;
        }
    }
}
