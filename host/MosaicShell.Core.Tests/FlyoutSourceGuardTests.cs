using System.Text.RegularExpressions;
using FluentAssertions;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// A4 ratchet guards (audit F12 and F20). Each guard fails the build on a new offender. Each is
    /// seeded with today's offenders; an allow-list entry that no longer matches also fails, so the
    /// lists can only shrink. The task named on each entry is the one that removes it.
    /// </summary>
    public partial class FlyoutSourceGuardTests
    {
        private static readonly string[] ProductionProjects = ["MosaicShell.Core", "MosaicShell.Host", "MosaicShell.Worker"];

        private static readonly string[] PolicyDirectories =
        [
            Path.Combine("MosaicShell.Core", "Modules", "Tessera"),
            Path.Combine("MosaicShell.Core", "Capabilities", "Platform"),
        ];

        // Guard 2: blocking marshaling and sync-over-async, per file, with the count allowed today.
        private static readonly Dictionary<string, (int Count, string Reason)> BlockingAllowList = new(StringComparer.OrdinalIgnoreCase)
        {
            ["AvaloniaFlyoutPresenter.cs"] = (2, "C8.3: Dispatcher.UIThread.Invoke in the ingress path; becomes InvokeAsync with the ordered queue"),
            ["BuiltInCapabilities.cs"] = (2, "C8.5: Dispose blocks on DisarmAsync; becomes IAsyncDisposable"),
            ["CapabilityIpcControlServer.cs"] = (6, "C8.6: HandleRequest blocks on daemon calls; becomes async"),
            ["CapabilityIpcFlyoutServer.cs"] = (1, "C8.6: server dispatch blocks on the host thread call"),
            ["IpcFlyoutPresenter.cs"] = (1, "C8.6: TrySend blocks on the pipe write"),
            ["RemoteCapabilityHost.cs"] = (2, "C8.6: control client blocks on connect and request"),
            ["SessionAndSettings.cs"] = (1, "C8.5: session teardown blocks on DisarmAsync"),
            ["TesseraCapability.cs"] = (1, "C8.5: Dispose blocks on DisarmAsync; becomes IAsyncDisposable"),
        };

        // Guard 1: public policy members with no production reference ("File.cs:Member"). Seeded 2026-10-04 with
        // the 111 found then (the audit counted 117 on 2026-09-19). D3 triages each one: wire it in, record it in
        // the style-profile doc and delete it, or delete it.
        private const string D3 = "D3: flag drift triage (wire in, document, or delete)";

        // Not dead on purpose: honesty-gate support that the layout-fidelity and sign-off tests read, and the hook-thread
        // predicate that the planned C8.1 arm rework builds on. They stay until that feature work lands.
        private const string KeptOnPurpose = "Kept: layout-fidelity / sign-off gate support or planned feature work";

        private static readonly Dictionary<string, string> UnreferencedMemberAllowList = new(StringComparer.Ordinal)
        {
            ["TesseraArmPolicy.cs:RequiresHostThreadForStart"] = KeptOnPurpose,
            ["TesseraLayoutCoverage.cs:AllLayoutFidelitySignedOff"] = KeptOnPurpose,
            ["TesseraLayoutCoverage.cs:CoversLayoutFidelity"] = KeptOnPurpose,
            ["TesseraLayoutCoverage.cs:IsApproximate"] = KeptOnPurpose,
            ["TesseraLayoutCoverage.cs:IsPolished"] = KeptOnPurpose,
            ["TesseraLayoutCoverage.cs:LayoutFidelityProofRelativeDirectory"] = KeptOnPurpose,
            ["TesseraLayoutCoverage.cs:RequiresLiveVolumePercentLabel"] = KeptOnPurpose,
            ["TesseraOsAcrylicSignOffPolicy.cs:AllH3StackedStylesSignedOff"] = KeptOnPurpose,
        };

        [Fact]
        public void No_new_blocking_marshaling_or_sync_over_async()
        {
            Dictionary<string, int> found = new(StringComparer.OrdinalIgnoreCase);
            foreach (string file in SourceTree.EnumerateSources("MosaicShell.Core", "MosaicShell.Host"))
            {
                int count = CodeLines(file).Count(static l => BlockingCall().IsMatch(l));
                if (count > 0)
                {
                    found[Path.GetFileName(file)] = count;
                }
            }

            List<string> problems = [];
            foreach ((string file, int count) in found)
            {
                int allowed = BlockingAllowList.TryGetValue(file, out (int Count, string Reason) entry) ? entry.Count : 0;
                if (count > allowed)
                {
                    problems.Add($"{file}: {count} blocking calls, allowed {allowed}. Use InvokeAsync/Post or await instead.");
                }
            }

            foreach ((string file, (int count, string _)) in BlockingAllowList)
            {
                int now = found.GetValueOrDefault(file);
                if (now < count)
                {
                    problems.Add($"{file}: allow-list says {count}, now {now}. Lower the entry (the list only shrinks).");
                }
            }

            _ = problems.Should().BeEmpty(string.Join('\n', problems));
        }

        [Fact]
        public void No_new_unreferenced_public_policy_members()
        {
            List<string> production = [.. SourceTree.EnumerateSources(ProductionProjects)];
            Dictionary<string, string[]> codeByFile = production.ToDictionary(static f => f, static f => CodeLines(f).ToArray());

            HashSet<string> offenders = new(StringComparer.Ordinal);
            foreach (string dir in PolicyDirectories)
            {
                foreach (string file in SourceTree.EnumerateSources(dir))
                {
                    string[] lines = codeByFile[file];
                    for (int i = 0; i < lines.Length; i++)
                    {
                        Match m = PublicStaticMember().Match(lines[i]);
                        if (!m.Success)
                        {
                            continue;
                        }

                        string name = m.Groups["name"].Value;
                        if (!IsReferenced(name, file, i, codeByFile))
                        {
                            _ = offenders.Add($"{Path.GetFileName(file)}:{name}");
                        }
                    }
                }
            }

            List<string> added = [.. offenders.Where(static o => !UnreferencedMemberAllowList.ContainsKey(o)).Order(StringComparer.Ordinal)];
            List<string> stale = [.. UnreferencedMemberAllowList.Keys.Where(o => !offenders.Contains(o)).Order(StringComparer.Ordinal)];

            _ = added.Should().BeEmpty(
                "a public policy member nothing in production reads enforces nothing; wire it in or delete it:\n"
                + string.Join('\n', added));
            _ = stale.Should().BeEmpty(
                "these are referenced (or gone) now; remove them from the allow-list:\n" + string.Join('\n', stale));
        }

        private static bool IsReferenced(string name, string definingFile, int definitionLine, Dictionary<string, string[]> codeByFile)
        {
            Regex word = new($@"\b{Regex.Escape(name)}\b", RegexOptions.CultureInvariant);
            foreach ((string file, string[] lines) in codeByFile)
            {
                for (int i = 0; i < lines.Length; i++)
                {
                    if (file == definingFile && i == definitionLine)
                    {
                        continue;
                    }

                    if (word.IsMatch(lines[i]))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>Source lines with line comments and XML doc comments blanked (line numbers kept).</summary>
        private static IEnumerable<string> CodeLines(string file)
        {
            foreach (string line in File.ReadLines(file))
            {
                string trimmed = line.TrimStart();
                if (trimmed.StartsWith("//", StringComparison.Ordinal) || trimmed.StartsWith('*') || trimmed.StartsWith("/*", StringComparison.Ordinal))
                {
                    yield return string.Empty;
                    continue;
                }

                int comment = line.IndexOf("//", StringComparison.Ordinal);
                yield return comment >= 0 && !line[..comment].Contains('"') ? line[..comment] : line;
            }
        }

        [GeneratedRegex(@"Dispatcher\.UIThread\.Invoke\(|\.GetAwaiter\(\)\.GetResult\(\)", RegexOptions.CultureInvariant)]
        private static partial Regex BlockingCall();

        // public const ..., public static [readonly|extern|...] Type Name followed by ( = ; { or =>
        // Excludes type declarations (public static class, record, struct, enum, interface).
        [GeneratedRegex(
            @"^\s*public\s+(?:const|static)\s+(?:(?:readonly|extern|async|new|unsafe|partial)\s+)*(?!class\b|record\b|struct\b|enum\b|interface\b|delegate\b)[\w<>\[\],\.\?\(\)\s]+?\s+(?<name>[A-Za-z_]\w*)\s*(?:<[^>]*>)?\s*(?:\(|=|;|\{)",
            RegexOptions.CultureInvariant)]
        private static partial Regex PublicStaticMember();
    }
}
