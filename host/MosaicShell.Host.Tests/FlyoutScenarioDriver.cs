using FluentAssertions;
using MosaicShell.Core;
using MosaicShell.Core.Capabilities;
using MosaicShell.Core.Capabilities.Platform;
using MosaicShell.Core.Modules;
using MosaicShell.Core.Services;
using MosaicShell.Host.Capabilities;

namespace MosaicShell.Host.Tests
{
    /// <summary>
    /// Replays a <see cref="FlyoutScenario"/> against the real presenter under Avalonia headless and returns
    /// what the presenter's own trace says it did. Every wait is on a state with a timeout, never a fixed
    /// delay, so the replay's result does not depend on machine speed. The log is written at Debug into a
    /// directory of its own per run, so one run's lines never mix with another's.
    /// </summary>
    internal static class FlyoutScenarioDriver
    {
        private const int WaitTimeoutMs = 8000;

        public static FlyoutTraceSummary Run(FlyoutScenario scenario)
        {
            string logDirectory = Path.Combine(AppPaths.CacheDirectory, "baseline-" + Guid.NewGuid().ToString("N"));
            string logPath = Path.Combine(logDirectory, "flyout.log");
            DiagnosticLog.Start(logDirectory, DiagnosticLogLevel.Debug);
            Exception? failure = null;
            try
            {
                AvaloniaFlyoutPresenter presenter = new(HostServicesFakes.Create());
                Dictionary<string, int> traceWaits = [];
                foreach (FlyoutScenarioStep step in scenario.Steps)
                {
                    Execute(presenter, scenario, step, logPath, traceWaits);
                }
            }
            catch (Exception ex)
            {
                failure = ex;
            }
            finally
            {
                DiagnosticLog.Stop();
            }

            string[] lines = File.Exists(logPath) ? File.ReadAllLines(logPath) : [];
            if (failure is not null)
            {
                // The trace says what the presenter did before the step that never completed.
                string tail = string.Join("\n", lines.Where(l => !l.Contains("motion frame") && !l.Contains("region seq=")).TakeLast(30));
                throw new InvalidOperationException($"{failure.Message}\nLast trace lines:\n{tail}", failure);
            }

            return FlyoutTraceSummary.Parse(lines);
        }

        private static void Execute(
            AvaloniaFlyoutPresenter presenter,
            FlyoutScenario scenario,
            FlyoutScenarioStep step,
            string logPath,
            Dictionary<string, int> traceWaits)
        {
            switch (step.Kind)
            {
                case FlyoutScenarioStepKind.Show:
                    presenter.Show(step.Request!);
                    break;
                case FlyoutScenarioStepKind.Update:
                    presenter.Update(step.Request!);
                    break;
                case FlyoutScenarioStepKind.SoftRefresh:
                    presenter.SoftRefresh(step.Request!);
                    break;
                case FlyoutScenarioStepKind.Hide:
                    presenter.Hide(ModuleIds.Tessera);
                    break;
                case FlyoutScenarioStepKind.WaitShown:
                    WaitFor(presenter, scenario, step, () => presenter.IsVisible(ModuleIds.Tessera));
                    break;
                case FlyoutScenarioStepKind.WaitPhase:
                    WaitFor(presenter, scenario, step, () => presenter.GetSessionSnapshot(ModuleIds.Tessera).Phase == step.Phase);
                    break;
                case FlyoutScenarioStepKind.WaitHidden:
                    WaitFor(presenter, scenario, step, () => !presenter.IsVisible(ModuleIds.Tessera));
                    break;
                case FlyoutScenarioStepKind.WaitTrace:
                    string prefix = step.TracePrefix!;
                    int needed = traceWaits.GetValueOrDefault(prefix) + 1;
                    traceWaits[prefix] = needed;
                    WaitFor(presenter, scenario, step, () => CountTraceLines(logPath, prefix) >= needed);
                    break;
                default:
                    throw new InvalidOperationException($"Unknown scenario step {step.Kind}.");
            }
        }

        /// <summary>Lines whose message starts with <paramref name="prefix"/>, read while the writer is still appending.</summary>
        private static int CountTraceLines(string logPath, string prefix)
        {
            if (!File.Exists(logPath))
            {
                return 0;
            }

            try
            {
                using FileStream stream = new(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                using StreamReader reader = new(stream);
                int count = 0;
                while (reader.ReadLine() is { } line)
                {
                    count += line.Contains(" " + prefix, StringComparison.Ordinal) ? 1 : 0;
                }

                return count;
            }
            catch (IOException)
            {
                return 0;
            }
        }

        private static void WaitFor(AvaloniaFlyoutPresenter presenter, FlyoutScenario scenario, FlyoutScenarioStep step, Func<bool> reached)
        {
            _ = HeadlessPump.Until(reached, WaitTimeoutMs).Should().BeTrue(
                $"scenario '{scenario.Name}' step {step.Kind} {step.Phase}{step.TracePrefix} never reached; snapshot: {presenter.GetSessionSnapshot(ModuleIds.Tessera)}");
        }
    }
}
