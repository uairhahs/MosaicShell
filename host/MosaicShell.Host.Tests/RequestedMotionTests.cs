using System.Text.RegularExpressions;
using Avalonia.Headless.XUnit;
using FluentAssertions;
using MosaicShell.Core;
using MosaicShell.Core.Capabilities;
using MosaicShell.Core.Modules;
using MosaicShell.Core.Services;
using MosaicShell.Host.Capabilities;

namespace MosaicShell.Host.Tests
{
    /// <summary>
    /// The motion the user picked is the motion that plays. Twice on 2026-11-26 the suites stayed green while it was not:
    /// the OS reduce-motion setting forced every flyout to a fade, and a reused flyout kept its old look. The A5 goldens
    /// do not record the requested motion, so this reads the presenter's own <c>motion plan</c> line for it.
    /// </summary>
    public sealed partial class RequestedMotionTests : HeadlessHostTest
    {
        private static FlyoutRequest Volume(int ani, int steps, string ease)
        {
            return new FlyoutRequest(ModuleIds.Tessera, "vol", "Fluent", Ani: ani, AniSteps: steps, AniEase: ease);
        }

        /// <summary>Shows each request in turn (hiding in between) and returns the entrance plans the presenter logged.</summary>
        private static List<(int Ani, int Steps)> PlayEntrances(params FlyoutRequest[] requests)
        {
            string logDirectory = Path.Combine(AppPaths.CacheDirectory, "motion-" + Guid.NewGuid().ToString("N"));
            DiagnosticLog.Start(logDirectory, DiagnosticLogLevel.Debug);
            try
            {
                AvaloniaFlyoutPresenter presenter = new(HostServicesFakes.Create());
                foreach (FlyoutRequest request in requests)
                {
                    presenter.Show(request);
                    _ = HeadlessPump.Until(() => presenter.IsVisible(ModuleIds.Tessera)).Should().BeTrue();
                    HeadlessPump.For(1200);
                    presenter.Hide(ModuleIds.Tessera);
                    _ = HeadlessPump.Until(() => !presenter.IsVisible(ModuleIds.Tessera)).Should().BeTrue();
                    HeadlessPump.For(1200);
                }
            }
            finally
            {
                DiagnosticLog.Stop();
            }

            string logPath = Path.Combine(logDirectory, "flyout.log");
            string[] lines = File.Exists(logPath) ? File.ReadAllLines(logPath) : [];
            return [.. lines
                .Select(static l => EntrancePlan().Match(l))
                .Where(static m => m.Success)
                .Select(static m => (int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value)))];
        }

        [GeneratedRegex(@"motion plan entrance=True .* ani=(\d+) steps=(\d+) ")]
        private static partial Regex EntrancePlan();

        [AvaloniaFact]
        public void The_chosen_look_plays_even_when_the_system_reduces_motion()
        {
            // "Follow the system" is off by default (a fresh settings root), so the OS setting must not turn this
            // slide into a fade, whatever this machine's animation effects are set to.
            List<(int Ani, int Steps)> plans = PlayEntrances(Volume(ani: 2, steps: 28, ease: "OutCubic"));

            _ = plans.Should().NotBeEmpty("the presenter logs a motion plan for every entrance");
            _ = plans[0].Should().Be((2, 28));
        }

        [AvaloniaFact]
        public void A_second_show_plays_the_new_look_not_the_old_one()
        {
            List<(int Ani, int Steps)> plans = PlayEntrances(
                Volume(ani: 1, steps: 40, ease: "OutElastic"),
                Volume(ani: 2, steps: 10, ease: "OutCubic"));

            _ = plans.Should().HaveCountGreaterThanOrEqualTo(2);
            _ = plans[0].Should().Be((1, 40));
            _ = plans[^1].Should().Be((2, 10));
        }
    }
}
