using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.LogicalTree;
using FluentAssertions;
using MosaicShell.Core.HostPlatform;
using MosaicShell.Core.Modules;
using MosaicShell.Core.Modules.Settings;
using MosaicShell.Core.Runtime;
using MosaicShell.Core.Settings;
using MosaicShell.Host.ViewModels;
using MosaicShell.Host.ViewModels.ModulePage;
using MosaicShell.Host.Views;

namespace MosaicShell.Host.Tests
{
    /// <summary>
    /// The module page drawn from a Core schema: each row kind renders and saves its setting as it changes, a row the
    /// desktop cannot do is greyed out with Core's line, the on switch runs its steps, and a change to a running module
    /// is followed once by what it needs.
    /// </summary>
    public sealed class ModulePageTests : HeadlessHostTest
    {
        private static ModulePageViewModel Page(string moduleId, FakePageHost host)
        {
            _ = ModuleCatalog.TryGet(moduleId, out ModuleInfo? info).Should().BeTrue();
            _ = ModuleSettingsSchemas.TryGet(moduleId, out ModuleSettingsSchema? schema).Should().BeTrue();
            return new ModulePageViewModel(info!, schema!, host);
        }

        [AvaloniaFact]
        public void Every_module_but_Tessera_opens_on_the_generic_page()
        {
            MainViewModel vm = new();

            vm.OpenModuleConfigById(ModuleIds.Mixdeck);
            _ = vm.ModulePage.Should().NotBeNull();
            _ = vm.ModulePage!.Sections.Select(s => s.Title).Should().Equal("Look", "Opening and closing");

            vm.OpenModuleConfigById(ModuleIds.Tessera);
            _ = vm.ModulePage.Should().BeNull("Tessera keeps its own page until step 4");
        }

        [AvaloniaFact]
        public void A_third_party_module_with_settings_in_its_manifest_opens_on_the_same_page()
        {
            InstallManifest("Acme", """
                { "Id": "Acme", "DisplayName": "Acme", "Kind": "Widget",
                  "Settings": [ { "Section": "Behaviour", "Rows": [ { "Key": "Sound", "Kind": "Toggle", "Label": "Play a sound" } ] } ] }
                """);
            InstallManifest("Bare", """{ "Id": "Bare", "DisplayName": "Bare", "Kind": "Widget" }""");
            MainViewModel vm = new();

            vm.OpenModuleConfigById("Acme");
            _ = vm.ModulePage.Should().NotBeNull();
            _ = vm.ModulePage!.Row("Sound").Label.Should().Be("Play a sound");

            vm.OpenModuleConfigById("Bare");
            _ = vm.ModulePage.Should().BeNull("a manifest without Settings keeps the plain page");
        }

        private static void InstallManifest(string id, string json)
        {
            string path = ModuleManifest.PathInModule(id);
            _ = Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, json);
        }

        [AvaloniaFact]
        public async Task A_switch_change_is_saved_at_once()
        {
            ModulePageViewModel page = Page(ModuleIds.Mixdeck, new FakePageHost());

            ((ToggleRowViewModel)page.Row("CloseOnEscape")).Value = false;

            _ = ModuleSettingsStore.Load(ModuleIds.Mixdeck, () => new MixdeckSettings()).CloseOnEscape.Should().BeFalse();
            await page.FlushAsync();
        }

        [AvaloniaFact]
        public void A_choice_is_saved_at_once()
        {
            ModulePageViewModel page = Page(ModuleIds.Mixdeck, new FakePageHost());
            ChoiceRowViewModel scheme = (ChoiceRowViewModel)page.Row("ColorScheme");

            scheme.Selected = scheme.Options.Single(o => o.Value == "Midnight");

            _ = ModuleSettingsStore.Load(ModuleIds.Mixdeck, () => new MixdeckSettings()).ColorScheme.Should().Be("Midnight");
        }

        [AvaloniaFact]
        public async Task A_number_waits_for_the_drag_to_pause_then_saves_the_last_value()
        {
            ModulePageViewModel page = Page(ModuleIds.Slate, new FakePageHost());
            NumberRowViewModel idle = (NumberRowViewModel)page.Row("IdleSeconds");

            idle.Value = 60;
            idle.Value = 90;
            _ = ModuleSettingsStore.Load(ModuleIds.Slate, () => new SlateSettings()).IdleSeconds.Should().Be(300, "nothing is written mid-drag");

            await page.FlushAsync();
            _ = ModuleSettingsStore.Load(ModuleIds.Slate, () => new SlateSettings()).IdleSeconds.Should().Be(90);
        }

        [AvaloniaFact]
        public void A_captured_shortcut_is_saved_and_a_reserved_one_is_refused()
        {
            ModulePageViewModel page = Page(ModuleIds.Inlay, new FakePageHost());
            HotkeyRowViewModel hotkey = (HotkeyRowViewModel)page.Row("HotkeyGesture");

            hotkey.BeginCaptureCommand.Execute(null);
            _ = page.CapturingHotkey.Should().BeSameAs(hotkey);
            _ = hotkey.TryCapture(new KeyEventArgs { Key = Key.J, KeyModifiers = KeyModifiers.Control | KeyModifiers.Alt }).Should().BeTrue();

            _ = hotkey.Gesture.Should().Be("Ctrl+Alt+J");
            _ = page.CapturingHotkey.Should().BeNull();
            _ = ModuleSettingsStore.Load(ModuleIds.Inlay, () => new InlaySettings()).HotkeyGesture.Should().Be("Ctrl+Alt+J");

            hotkey.BeginCaptureCommand.Execute(null);
            _ = hotkey.Capture("Win+S").Should().BeTrue();
            _ = hotkey.IsCapturing.Should().BeTrue("a shortcut the OS keeps leaves the row listening");
            _ = ModuleSettingsStore.Load(ModuleIds.Inlay, () => new InlaySettings()).HotkeyGesture.Should().Be("Ctrl+Alt+J");
        }

        [AvaloniaFact]
        public async Task List_rows_add_and_remove_from_the_launch_targets()
        {
            FakePageHost host = new();
            ModulePageViewModel page = Page(ModuleIds.Chord, host);
            ActionListRowViewModel actions = (ActionListRowViewModel)page.Row("Actions");
            int before = actions.Items.Count;

            actions.Query = "Files";
            actions.AddCommand.Execute(null);
            await page.FlushAsync();

            _ = actions.Items.Should().HaveCount(before + 1);
            _ = ModuleSettingsStore.Load(ModuleIds.Chord, () => new ChordSettings()).Actions.Last()
                .Should().BeEquivalentTo(new ChordAction { Name = "Files display", Target = "files-target" });

            actions.RemoveCommand.Execute(actions.Items.Last());
            await page.FlushAsync();
            _ = ModuleSettingsStore.Load(ModuleIds.Chord, () => new ChordSettings()).Actions.Should().HaveCount(before);
        }

        [AvaloniaFact]
        public void On_GNOME_the_shortcut_row_is_greyed_out_with_Cores_line()
        {
            ModulePageViewModel page = Page(ModuleIds.Mixdeck, new FakePageHost { Features = DesktopFeatures.Resolve(false, "GNOME") });
            SettingRowViewModel hotkey = page.Row("HotkeyGesture");

            _ = hotkey.IsAvailable.Should().BeFalse();
            _ = hotkey.UnavailableReason.Should().Be("Shortcuts are not available on GNOME yet.");
            _ = page.Row("CloseOnEscape").IsAvailable.Should().BeTrue();
            _ = page.HowTo.Should().Be("Shortcuts are not available on GNOME yet.");
        }

        [AvaloniaFact]
        public async Task Changes_to_an_armed_capability_rearm_it_once()
        {
            FakePageHost host = new() { Armed = true };
            ModulePageViewModel page = Page(ModuleIds.Slate, host);

            ((NumberRowViewModel)page.Row("IdleSeconds")).Value = 120;
            ((ToggleRowViewModel)page.Row("HideOnFullscreen")).Value = false;
            await page.FlushAsync();

            _ = host.FollowUps.Should().Equal(SettingFollowUp.ReArm);
        }

        [AvaloniaFact]
        public async Task A_change_the_module_reads_next_time_needs_no_follow_up()
        {
            FakePageHost host = new() { Armed = true };
            ModulePageViewModel page = Page(ModuleIds.Mixdeck, host);

            ((ToggleRowViewModel)page.Row("CloseOnEscape")).Value = false;
            await page.FlushAsync();

            _ = host.FollowUps.Should().BeEmpty();
        }

        [AvaloniaFact]
        public async Task Turning_on_a_widget_that_is_not_installed_installs_then_shows_it()
        {
            FakePageHost host = new() { Installed = false };
            ModulePageViewModel page = Page(ModuleIds.Chrono, host);
            _ = page.IsOn.Should().BeFalse();

            await page.SetOnAsync(true);

            _ = host.Steps.Should().Equal(ModuleSwitchStep.Install, ModuleSwitchStep.ShowWidget);
            _ = page.IsOn.Should().BeTrue();
        }

        [AvaloniaFact]
        public async Task A_failed_step_stops_the_switch_and_leaves_it_off()
        {
            FakePageHost host = new() { Installed = false, FailInstall = true };
            ModulePageViewModel page = Page(ModuleIds.Chrono, host);

            await page.SetOnAsync(true);

            _ = host.Steps.Should().Equal(ModuleSwitchStep.Install);
            _ = page.IsOn.Should().BeFalse();
        }

        [AvaloniaFact]
        public void Without_sign_in_start_a_widget_offers_it_and_takes_it()
        {
            FakePageHost host = new() { SignInStart = false };
            ModulePageViewModel page = Page(ModuleIds.Chrono, host);
            _ = page.OffersSignInStart.Should().BeTrue();

            page.TurnOnSignInStartCommand.Execute(null);

            _ = host.SignInStart.Should().BeTrue();
            _ = page.OffersSignInStart.Should().BeFalse();
        }

        [AvaloniaFact]
        public void A_row_shown_only_while_a_switch_is_on_follows_it()
        {
            ModuleSettingsSchema schema = ModuleSettingsSchema.For<ChronoSettings>(ModuleIds.Chrono)
                .Section("Time")
                .Toggle("ShowSeconds", "Show seconds", s => s.ShowSeconds, (s, v) => s.ShowSeconds = v, SettingApplyEffect.Live)
                .Toggle("TwentyFourHour", "24-hour clock", s => s.TwentyFourHour, (s, v) => s.TwentyFourHour = v, SettingApplyEffect.Live)
                .ShownWhen("ShowSeconds")
                .Build();
            _ = ModuleCatalog.TryGet(ModuleIds.Chrono, out ModuleInfo? info);
            ModulePageViewModel page = new(info!, schema, new FakePageHost());

            _ = page.Row("TwentyFourHour").IsVisible.Should().BeTrue();
            ((ToggleRowViewModel)page.Row("ShowSeconds")).Value = false;
            _ = page.Row("TwentyFourHour").IsVisible.Should().BeFalse();
        }

        [AvaloniaFact]
        public void Each_row_kind_renders_with_its_control()
        {
            Window window = new() { Width = 900, Height = 2000 };
            ModulePageViewModel inlay = Page(ModuleIds.Inlay, new FakePageHost());
            window.Content = new ModulePageView { DataContext = inlay };
            window.Show();

            List<ILogical> all = [.. window.GetLogicalDescendants()];
            _ = all.OfType<ToggleSwitch>().Should().HaveCountGreaterThanOrEqualTo(2, "the on switch and Close with Esc");
            _ = all.OfType<ComboBox>().Should().NotBeEmpty("the style");
            _ = all.OfType<AutoCompleteBox>().Should().NotBeEmpty("the pinned apps");
            _ = all.OfType<Button>().Select(b => b.Content as string).Should().Contain(["Change", "Remove Inlay"]);

            window.Content = new ModulePageView { DataContext = Page(ModuleIds.Slate, new FakePageHost()) };
            window.UpdateLayout();
            _ = window.GetLogicalDescendants().OfType<Slider>().Should().ContainSingle("the idle time");
            window.Close();
        }

        private sealed class FakePageHost : IModulePageHost
        {
            public DesktopFeatures Features { get; init; } = DesktopFeatures.All;
            public bool SignInStart { get; set; } = true;
            public bool Installed { get; set; } = true;
            public bool Armed { get; set; }
            public bool WidgetOpen { get; set; }
            public bool FailInstall { get; init; }
            public List<ModuleSwitchStep> Steps { get; } = [];
            public List<SettingFollowUp> FollowUps { get; } = [];

            public bool IsSignInStartOn => SignInStart;

            public IReadOnlyList<string> LaunchTargetLabels { get; } = ["Files"];

            public bool IsInstalled(string moduleId)
            {
                return Installed;
            }

            public bool IsArmed(string moduleId)
            {
                return Armed;
            }

            public bool IsWidgetOpen(string moduleId)
            {
                return WidgetOpen;
            }

            public Task<bool> RunStepAsync(string moduleId, ModuleSwitchStep step)
            {
                Steps.Add(step);
                switch (step)
                {
                    case ModuleSwitchStep.Install when FailInstall:
                        return Task.FromResult(false);
                    case ModuleSwitchStep.Install:
                        Installed = true;
                        break;
                    case ModuleSwitchStep.Arm:
                        Armed = true;
                        break;
                    case ModuleSwitchStep.Disarm:
                        Armed = false;
                        break;
                    case ModuleSwitchStep.ShowWidget:
                        WidgetOpen = true;
                        break;
                    case ModuleSwitchStep.CloseWidget:
                        WidgetOpen = false;
                        break;
                    default:
                        break;
                }

                return Task.FromResult(true);
            }

            public Task FollowUpAsync(string moduleId, SettingFollowUp followUp)
            {
                FollowUps.Add(followUp);
                return Task.CompletedTask;
            }

            public void TurnOnSignInStart()
            {
                SignInStart = true;
            }

            public Task PreviewAsync(string moduleId)
            {
                return Task.CompletedTask;
            }

            public void Remove(string moduleId) { }

            public bool TryResolveLaunchTarget(string label, out string target, out string displayName)
            {
                target = label == "Files" ? "files-target" : "";
                displayName = label == "Files" ? "Files display" : "";
                return target.Length > 0;
            }

            public void Report(string message) { }
        }
    }
}
