using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Host.ViewModels
{
    /// <summary>
    /// The Tessera motion settings as the Hub shows them: a handful of named looks, and a few plain controls (slide, reveal,
    /// duration, distance, direction, curve) to tune one. The stored values stay the engine's own (<c>Ani</c>, <c>AniSteps</c>,
    /// <c>AniEase</c>, <c>AnimationDisplacement</c>, <c>AniDir</c>); everything here is a view of them, kept in step by
    /// <see cref="RefreshTesseraMotion"/>.
    /// </summary>
    public partial class MainViewModel
    {
        private bool _syncingMotion;

        public ObservableCollection<TesseraMotionPresetItem> TesseraMotionPresetItems { get; } =
            [.. TesseraMotionPresets.All.Select(static p => new TesseraMotionPresetItem(p))];

        public ObservableCollection<TesseraMotionCurveChoice> TesseraMotionCurves { get; } =
            [.. TesseraMotionCurveChoice.Standard];

        [ObservableProperty] private TesseraMotionCurveChoice? _selectedTesseraMotionCurve;
        [ObservableProperty] private bool _tesseraMotionSlides = true;
        [ObservableProperty] private bool _tesseraMotionRevealsMedia = true;
        [ObservableProperty] private bool _tesseraMotionRevealEnabled = true;
        [ObservableProperty] private double _tesseraMotionDurationMs = 320;
        [ObservableProperty] private string _tesseraMotionDurationLabel = "320 ms";
        [ObservableProperty] private string _tesseraMotionDistanceLabel = "30 px";
        [ObservableProperty] private string _tesseraMotionPresetName = string.Empty;
        [ObservableProperty] private string _tesseraMotionSummary = string.Empty;
        [ObservableProperty] private bool _tesseraMotionIsCustom;
        [ObservableProperty] private bool _tesseraMotionFromLeft = true;
        [ObservableProperty] private bool _tesseraMotionFromRight;
        [ObservableProperty] private bool _tesseraMotionFromTop;
        [ObservableProperty] private bool _tesseraMotionFromBottom;

        /// <summary>The look the Hub previews: the settings as they stand, so a tuned value shows at once.</summary>
        public TesseraMotionPresetItem TesseraMotionCurrent { get; } = new(
            new TesseraMotionPreset("current", "Current", string.Empty, 2, 20, "OutQuart", 30));

        [RelayCommand]
        private void SelectTesseraMotionPreset(TesseraMotionPresetItem? item)
        {
            if (item is null)
            {
                return;
            }

            TesseraAni = item.Ani;
            TesseraAniSteps = TesseraFlyoutAnimationPolicy.NormalizeAniSteps(item.AniSteps);
            TesseraAniEase = TesseraFlyoutAnimationPolicy.NormalizeEase(item.AniEase);
            ApplyEasePartsFromStored(TesseraAniEase);
            TesseraAnimationDisplacement = TesseraFlyoutAnimationPolicy.NormalizeDisplacementPx(item.DisplacementPx);
            RefreshTesseraMotion();
        }

        [RelayCommand]
        private void SetTesseraMotionDirection(string? direction)
        {
            if (!string.IsNullOrWhiteSpace(direction))
            {
                TesseraAniDir = direction;
            }
        }

        partial void OnTesseraAniChanged(int value)
        {
            RefreshTesseraMotion();
        }

        partial void OnTesseraAniStepsChanged(int value)
        {
            RefreshTesseraMotion();
        }

        partial void OnTesseraAniEaseChanged(string value)
        {
            RefreshTesseraMotion();
        }

        partial void OnTesseraAnimationDisplacementChanged(int value)
        {
            RefreshTesseraMotion();
        }

        partial void OnTesseraAniDirChanged(string value)
        {
            RefreshTesseraMotion();
        }

        partial void OnTesseraMotionSlidesChanged(bool value)
        {
            if (_syncingMotion)
            {
                return;
            }

            TesseraAni = !value ? 0 : TesseraMotionRevealsMedia ? 2 : 1;
        }

        partial void OnTesseraMotionRevealsMediaChanged(bool value)
        {
            if (_syncingMotion || !TesseraMotionSlides)
            {
                return;
            }

            TesseraAni = value ? 2 : 1;
        }

        partial void OnTesseraMotionDurationMsChanged(double value)
        {
            if (_syncingMotion)
            {
                return;
            }

            TesseraAniSteps = TesseraMotionPresets.StepsFromDurationMs((int)Math.Round(value));
        }

        partial void OnSelectedTesseraMotionCurveChanged(TesseraMotionCurveChoice? value)
        {
            if (_syncingMotion || value is null || value.IsCustomPlaceholder)
            {
                return;
            }

            TesseraAniEase = value.Id;
            ApplyEasePartsFromStored(TesseraAniEase);
        }

        partial void OnTesseraMotionFromLeftChanged(bool value) => ChooseDirection(value, "Left");

        partial void OnTesseraMotionFromRightChanged(bool value) => ChooseDirection(value, "Right");

        partial void OnTesseraMotionFromTopChanged(bool value) => ChooseDirection(value, "Top");

        partial void OnTesseraMotionFromBottomChanged(bool value) => ChooseDirection(value, "Bottom");

        private void ChooseDirection(bool isChecked, string direction)
        {
            if (!_syncingMotion && isChecked)
            {
                TesseraAniDir = direction;
            }
        }

        /// <summary>Brings every motion control and label in line with the stored values.</summary>
        private void RefreshTesseraMotion()
        {
            if (_syncingMotion)
            {
                return;
            }

            _syncingMotion = true;
            try
            {
                int ani = Math.Clamp(TesseraAni, 0, 2);
                string ease = TesseraFlyoutAnimationPolicy.NormalizeEase(TesseraAniEase);
                int steps = TesseraFlyoutAnimationPolicy.NormalizeAniSteps(TesseraAniSteps);
                int distance = TesseraFlyoutAnimationPolicy.NormalizeDisplacementPx(TesseraAnimationDisplacement);
                string direction = string.IsNullOrWhiteSpace(TesseraAniDir) ? "Left" : TesseraAniDir;

                TesseraMotionSlides = ani > 0;
                if (ani > 0)
                {
                    // A fade has no reveal to show; keep the last choice so switching the slide back on restores it.
                    TesseraMotionRevealsMedia = ani >= 2;
                }

                TesseraMotionRevealEnabled = ani > 0;
                TesseraMotionDurationMs = TesseraMotionPresets.DurationMsFromSteps(steps);
                TesseraMotionDurationLabel = ani > 0
                    ? $"{TesseraMotionPresets.DurationMsFromSteps(steps)} ms"
                    : $"{TesseraFlyoutAnimationPolicy.FadeDurationMs} ms (a fade is always this long)";
                TesseraMotionDistanceLabel = ani > 0 ? $"{distance} px" : "none (a fade does not move)";
                TesseraMotionFromLeft = direction.Equals("Left", StringComparison.OrdinalIgnoreCase);
                TesseraMotionFromRight = direction.Equals("Right", StringComparison.OrdinalIgnoreCase);
                TesseraMotionFromTop = direction.Equals("Top", StringComparison.OrdinalIgnoreCase);
                TesseraMotionFromBottom = direction.Equals("Bottom", StringComparison.OrdinalIgnoreCase);

                TesseraMotionCurveChoice? standard = TesseraMotionCurves.FirstOrDefault(
                    c => !c.IsCustomPlaceholder && c.Id.Equals(ease, StringComparison.OrdinalIgnoreCase));
                for (int i = TesseraMotionCurves.Count - 1; i >= 0; i--)
                {
                    if (TesseraMotionCurves[i].IsCustomPlaceholder)
                    {
                        TesseraMotionCurves.RemoveAt(i);
                    }
                }

                if (standard is null)
                {
                    standard = TesseraMotionCurveChoice.CustomPlaceholder(ease);
                    TesseraMotionCurves.Add(standard);
                }

                SelectedTesseraMotionCurve = standard;

                TesseraMotionPreset? preset = TesseraMotionPresets.Match(ani, steps, ease, distance);
                foreach (TesseraMotionPresetItem item in TesseraMotionPresetItems)
                {
                    item.IsSelected = preset is not null && item.Id == preset.Id;
                    item.AniDir = direction;
                }

                TesseraMotionIsCustom = preset is null;
                TesseraMotionPresetName = preset?.Name ?? "Custom";
                TesseraMotionSummary = preset is not null
                    ? preset.Description
                    : "Your own mix. Pick a look above to start from one.";

                TesseraMotionCurrent.Ani = ani;
                TesseraMotionCurrent.AniSteps = steps;
                TesseraMotionCurrent.AniEase = ease;
                TesseraMotionCurrent.DisplacementPx = distance;
                TesseraMotionCurrent.AniDir = direction;
            }
            finally
            {
                _syncingMotion = false;
            }
        }
    }

    /// <summary>One look in the Hub grid: the preset's values, plus the direction the previews slide from.</summary>
    public sealed partial class TesseraMotionPresetItem : ObservableObject
    {
        public TesseraMotionPresetItem(TesseraMotionPreset preset)
        {
            Id = preset.Id;
            Name = preset.Name;
            Description = preset.Description;
            Ani = preset.Ani;
            AniSteps = preset.AniSteps;
            AniEase = preset.AniEase;
            DisplacementPx = preset.DisplacementPx;
        }

        public string Id { get; }
        public string Name { get; }
        public string Description { get; }

        [ObservableProperty] private int _ani;
        [ObservableProperty] private int _aniSteps;
        [ObservableProperty] private string _aniEase;
        [ObservableProperty] private int _displacementPx;
        [ObservableProperty] private string _aniDir = "Left";
        [ObservableProperty] private bool _isSelected;
    }

    /// <summary>A curve the Hub offers by feel rather than by family and variant.</summary>
    public sealed record TesseraMotionCurveChoice(string Id, string Name, bool IsCustomPlaceholder = false)
    {
        public static IReadOnlyList<TesseraMotionCurveChoice> Standard { get; } =
        [
            new(TesseraFlyoutAnimationPolicy.EaseLinear, "Even"),
            new(TesseraFlyoutAnimationPolicy.EaseOutQuart, "Snappy"),
            new(TesseraFlyoutAnimationPolicy.EaseOutCubic, "Soft landing"),
            new(TesseraFlyoutAnimationPolicy.EaseOutExpo, "Burst"),
            new(TesseraFlyoutAnimationPolicy.EaseInOutSine, "Gentle"),
            new(TesseraFlyoutAnimationPolicy.EaseInOutCubic, "Ease in and out"),
            new(TesseraFlyoutAnimationPolicy.EaseInCubic, "Slow start"),
            new(TesseraFlyoutAnimationPolicy.EaseOutBack, "Overshoot"),
            new(TesseraFlyoutAnimationPolicy.EaseOutElastic, "Spring"),
            new(TesseraFlyoutAnimationPolicy.EaseOutBounce, "Bounce"),
        ];

        public static TesseraMotionCurveChoice CustomPlaceholder(string id)
        {
            return new(id, $"Custom curve ({id})", IsCustomPlaceholder: true);
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
