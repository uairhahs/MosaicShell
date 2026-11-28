using FluentAssertions;
using MosaicShell.Core.Modules.Settings;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// Instant apply: every change is saved as it is made, and Core says what has to follow so the running module
    /// shows it. The Host only runs the timer and the action.
    /// </summary>
    public class ModuleSettingsApplyPolicyTests
    {
        [Theory]
        [InlineData(SettingApplyEffect.Live, true, true, SettingFollowUp.None)]
        [InlineData(SettingApplyEffect.ReArm, true, false, SettingFollowUp.ReArm)]
        [InlineData(SettingApplyEffect.ReArm, false, false, SettingFollowUp.None)]
        [InlineData(SettingApplyEffect.RelaunchWidget, false, true, SettingFollowUp.RelaunchWidget)]
        [InlineData(SettingApplyEffect.RelaunchWidget, false, false, SettingFollowUp.None)]
        [InlineData(SettingApplyEffect.RestartHost, false, false, SettingFollowUp.OfferRestart)]
        [InlineData(SettingApplyEffect.RestartHost, true, true, SettingFollowUp.OfferRestart)]
        public void A_change_is_followed_only_by_what_the_running_module_needs(
            SettingApplyEffect effect, bool armed, bool widgetOpen, SettingFollowUp expected)
        {
            _ = ModuleSettingsApplyPolicy.FollowUp(effect, armed, widgetOpen).Should().Be(expected);
        }

        [Fact]
        public void Changes_made_together_are_followed_once_by_the_strongest()
        {
            _ = ModuleSettingsApplyPolicy.Combine(SettingFollowUp.None, SettingFollowUp.ReArm).Should().Be(SettingFollowUp.ReArm);
            _ = ModuleSettingsApplyPolicy.Combine(SettingFollowUp.ReArm, SettingFollowUp.None).Should().Be(SettingFollowUp.ReArm);
            _ = ModuleSettingsApplyPolicy.Combine(SettingFollowUp.RelaunchWidget, SettingFollowUp.OfferRestart).Should().Be(SettingFollowUp.OfferRestart);
            _ = ModuleSettingsApplyPolicy.Combine(SettingFollowUp.None, SettingFollowUp.None).Should().Be(SettingFollowUp.None);
        }

        [Theory]
        [InlineData(SettingKind.Toggle, 0)]
        [InlineData(SettingKind.Choice, 0)]
        [InlineData(SettingKind.Style, 0)]
        [InlineData(SettingKind.Hotkey, 0)]
        [InlineData(SettingKind.Number, 300)]
        [InlineData(SettingKind.TextList, 500)]
        [InlineData(SettingKind.ActionList, 500)]
        public void Clicks_save_at_once_and_dragging_or_typing_waits_until_it_pauses(SettingKind kind, int ms)
        {
            _ = ModuleSettingsApplyPolicy.DebounceMs(kind).Should().Be(ms);
        }

        [Fact]
        public void Only_a_restart_setting_warns_before_it_is_changed()
        {
            _ = ModuleSettingsApplyPolicy.BeforeChangeNote(SettingApplyEffect.RestartHost)
                .Should().Be("Takes effect after MosaicShell restarts.");
            _ = Enum.GetValues<SettingApplyEffect>().Where(e => e != SettingApplyEffect.RestartHost)
                .Should().OnlyContain(e => ModuleSettingsApplyPolicy.BeforeChangeNote(e) == null);
        }

        [Fact]
        public void Every_built_in_row_has_a_debounce_and_a_follow_up()
        {
            foreach (string id in ModuleSettingsSchemas.GenericModuleIds)
            {
                _ = ModuleSettingsSchemas.TryGet(id, out ModuleSettingsSchema? schema).Should().BeTrue();
                foreach (SettingRow row in schema!.Rows)
                {
                    _ = ModuleSettingsApplyPolicy.DebounceMs(row.Kind).Should().BeGreaterThanOrEqualTo(0);
                    _ = Enum.IsDefined(ModuleSettingsApplyPolicy.FollowUp(row.Apply, true, true)).Should().BeTrue();
                }
            }
        }
    }
}
