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

        private static readonly Dictionary<string, string> UnreferencedMemberAllowList = new(StringComparer.Ordinal)
        {
            ["FlyoutSyncTriggerMapping.cs:ToPlatform"] = D3,
            ["TesseraAccentColor.cs:IsConfigured"] = D3,
            ["TesseraArmPolicy.cs:MustInstallKeyboardHooksOnHostMessagePumpThread"] = D3,
            ["TesseraArmPolicy.cs:RequiresHostThreadForStart"] = D3,
            ["TesseraCoreUiLayoutSpec.cs:ArtColumnWidthDip"] = D3,
            ["TesseraCoreUiLayoutSpec.cs:MediaLayoutMustUseWrappedRowWidth"] = D3,
            ["TesseraEasePreviewSpec.cs:MustReplayWhenEaseOrStepsChange"] = D3,
            ["TesseraFlyoutAnimatedTargetSpec.cs:AnimatedMediaMustDissolveWithTweenNode1"] = D3,
            ["TesseraFlyoutAnimatedTargetSpec.cs:CoreUiMediaMustClipOnly"] = D3,
            ["TesseraFlyoutAnimatedTargetSpec.cs:FluentVolumeStaysOpaqueDuringPhase2"] = D3,
            ["TesseraFlyoutAnimatedTargetSpec.cs:GnomeVolumeMustNotUseContentScale"] = D3,
            ["TesseraFlyoutAnimatedTargetSpec.cs:MediaOverlayIsContainerMaskNotCover"] = D3,
            ["TesseraFlyoutAnimatedTargetSpec.cs:ResolveCoreUiMediaSlideOffsetDip"] = D3,
            ["TesseraFlyoutAnimatedTargetSpec.cs:ResolveCoreUiMediaSlideWidthFactor"] = D3,
            ["TesseraFlyoutAnimatedTargetSpec.cs:ResolveCoreUiWrappedRowLayoutWidthDip"] = D3,
            ["TesseraFlyoutAnimatedTargetSpec.cs:ResolveFluentShellWidthDip"] = D3,
            ["TesseraFlyoutAnimatedTargetSpec.cs:ResolveFluentStackedMediaRegionWidthDip"] = D3,
            ["TesseraFlyoutAnimatedTargetSpec.cs:ResolveWin11ShellClipHeightDip"] = D3,
            ["TesseraFlyoutAnimatedTargetSpec.cs:Win11XorFrostMustNotPaintCoverOverlay"] = D3,
            ["TesseraFlyoutAnimationPolicy.cs:AniNoneIgnoresAniSteps"] = D3,
            ["TesseraFlyoutAnimationPolicy.cs:AniNoneUsesBuiltInFade"] = D3,
            ["TesseraFlyoutAnimationPolicy.cs:EncodedActionTimerIntervalMs"] = D3,
            ["TesseraFlyoutAnimationPolicy.cs:EntranceOpacityMustStayVisibleDuringInEase"] = D3,
            ["TesseraFlyoutAnimationPolicy.cs:FadeInMustInverseFadeOut"] = D3,
            ["TesseraFlyoutAnimationPolicy.cs:InterpolateForward"] = D3,
            ["TesseraFlyoutAnimationPolicy.cs:InvertEaseVariant"] = D3,
            ["TesseraFlyoutAnimationPolicy.cs:MinPerceptiblePhaseDurationMs"] = D3,
            ["TesseraFlyoutAnimationPolicy.cs:Phase2MustNotMutateLayoutMeasure"] = D3,
            ["TesseraFlyoutAnimationPolicy.cs:Phase2MustNotResizeHwnd"] = D3,
            ["TesseraFlyoutAnimationPolicy.cs:Phase2MustPumpEachAniStepOnDispatcher"] = D3,
            ["TesseraFlyoutAnimationPolicy.cs:Phase2ShowMidBandMax"] = D3,
            ["TesseraFlyoutAnimationPolicy.cs:Phase2ShowMidBandMinDurationFraction"] = D3,
            ["TesseraFlyoutAnimationPolicy.cs:Phase2ShowMustNotInvertToInEase"] = D3,
            ["TesseraFlyoutAnimationPolicy.cs:Phase2ShowMustNotSnapMissingHostsToRest"] = D3,
            ["TesseraFlyoutAnimationPolicy.cs:RelayoutAllowedDuringPhase2"] = D3,
            ["TesseraFlyoutAnimationPolicy.cs:ResolveEntranceSequence"] = D3,
            ["TesseraFlyoutAnimationPolicy.cs:ResolveExitSequence"] = D3,
            ["TesseraFlyoutAnimationPolicy.cs:ResolveOpacityFromTweenNode"] = D3,
            ["TesseraFlyoutAnimationPolicy.cs:SampleEase"] = D3,
            ["TesseraFlyoutAnimationPolicy.cs:SampleEaseContinuous"] = D3,
            ["TesseraFlyoutAnimationPolicy.cs:StackedPhase2MustNotBeMediaSlotOnly"] = D3,
            ["TesseraFlyoutAnimationPolicy.cs:SteppedKeyframesMustUseLinearInterpolation"] = D3,
            ["TesseraFlyoutAnimationPolicy.cs:SupersededMotionMustCancelInFlightTweens"] = D3,
            ["TesseraFlyoutDismissCoordinator.cs:IpcMustEchoSessionSnapshot"] = D3,
            ["TesseraFlyoutGlassPolicy.cs:SkiaGlassAllowedWithoutTransparentHwnd"] = D3,
            ["TesseraFlyoutHwndRegionSpec.cs:CollapsedMediaRegionMustStayRenderable"] = D3,
            ["TesseraFlyoutHwndRegionSpec.cs:CollapsedRevealRegionMustHideRestChrome"] = D3,
            ["TesseraFlyoutHwndRegionSpec.cs:CoreUiSingleHwndMustKeepRestRegion"] = D3,
            ["TesseraFlyoutHwndRegionSpec.cs:Phase2MustAnimateRegionHeightInPlace"] = D3,
            ["TesseraFlyoutHwndRegionSpec.cs:Phase2MustClipMediaChromeToRevealProgress"] = D3,
            ["TesseraFlyoutHwndRegionSpec.cs:RegionRectInclusivePaddingPx"] = D3,
            ["TesseraFlyoutHwndRegionSpec.cs:RestRevealMustUseSignedClientRegion"] = D3,
            ["TesseraFlyoutHwndRegionSpec.cs:RevealRegionProgressMustUseMaxHost"] = D3,
            ["TesseraFlyoutHwndRegionSpec.cs:ShouldZeroWindowOpacityForCollapsedRegion"] = D3,
            ["TesseraFlyoutLiveSyncPolicy.cs:ClosedMustOnlyUnregisterSameInstance"] = D3,
            ["TesseraFlyoutLiveSyncPolicy.cs:MustCoalesceBeforeUiPost"] = D3,
            ["TesseraFlyoutLiveSyncPolicy.cs:NonStatusShowMustCoalesceThroughUpdateGate"] = D3,
            ["TesseraFlyoutLiveSyncPolicy.cs:PatchImpliesOutsideClickRearm"] = D3,
            ["TesseraFlyoutLiveSyncPolicy.cs:PatchImpliesPresent"] = D3,
            ["TesseraFlyoutLiveSyncPolicy.cs:PatchImpliesWin32Restack"] = D3,
            ["TesseraFlyoutLiveSyncPolicy.cs:PumpMayAdvanceMediaTimeline"] = D3,
            ["TesseraFlyoutLiveSyncPolicy.cs:PumpMayWriteVolumeBindings"] = D3,
            ["TesseraFlyoutLiveSyncPolicy.cs:ResolveAction"] = D3,
            ["TesseraFlyoutMotionPlan.cs:MotionKinds"] = D3,
            ["TesseraFlyoutOutsideClickPolicy.cs:BoundsSnapshotOnUiThreadOnly"] = D3,
            ["TesseraFlyoutOutsideClickPolicy.cs:MustUseUnionBounds"] = D3,
            ["TesseraFlyoutOutsideClickPolicy.cs:RearmOnlyOnPresent"] = D3,
            ["TesseraFlyoutOutsideClickPolicy.cs:SingleWindowUsesPrimaryBounds"] = D3,
            ["TesseraFlyoutPresentHandoffPolicy.cs:HideAllMustRunHideSynchronouslyOnUiThread"] = D3,
            ["TesseraFlyoutRevealSpec.cs:HostMustDispatchRevealFromCatalogKind"] = D3,
            ["TesseraFlyoutRevealSpec.cs:NormalizeRevealProgress"] = D3,
            ["TesseraFlyoutRevealSpec.cs:StyleCompact"] = D3,
            ["TesseraFlyoutRevealSpec.cs:StyleMaterialYou"] = D3,
            ["TesseraFlyoutRevealSpec.cs:StyleMeter"] = D3,
            ["TesseraFlyoutRevealSpec.cs:StyleModernFlyouts"] = D3,
            ["TesseraFlyoutRevealSpec.cs:StylePlainText"] = D3,
            ["TesseraFlyoutRevealSpec.cs:StyleSmouti"] = D3,
            ["TesseraFlyoutRevealSpec.cs:StyleSquare"] = D3,
            ["TesseraFlyoutRevealSpec.cs:UsesDedicatedRevealBinder"] = D3,
            ["TesseraFlyoutSessionState.cs:HostMustExecutePresenterCommandWithoutReResolve"] = D3,
            ["TesseraFlyoutSessionState.cs:HostMustUseSingleIngressQueue"] = D3,
            ["TesseraFlyoutTweenTargetCatalog.cs:IndependentMediaChildrenMustInheritContainer"] = D3,
            ["TesseraFlyoutTweenTargetCatalog.cs:TryResolveMediaRestSizeDip"] = D3,
            ["TesseraFlyoutTweenTargetCatalog.cs:VolumeStaysOpaqueDuringPhase2"] = D3,
            ["TesseraFlyoutWindowPolicy.cs:ForbidDebugTitleChrome"] = D3,
            ["TesseraFocusDimPolicy.cs:InstantDismissOnOutsideClick"] = D3,
            ["TesseraFocusDimPolicy.cs:MaxCompositionFallbackAlpha"] = D3,
            ["TesseraFocusDimPolicy.cs:MustPassThroughInput"] = D3,
            ["TesseraLayoutCoverage.cs:AllLayoutFidelitySignedOff"] = D3,
            ["TesseraLayoutCoverage.cs:CoversLayoutFidelity"] = D3,
            ["TesseraLayoutCoverage.cs:IsApproximate"] = D3,
            ["TesseraLayoutCoverage.cs:IsPolished"] = D3,
            ["TesseraLayoutCoverage.cs:LayoutFidelityProofRelativeDirectory"] = D3,
            ["TesseraLayoutCoverage.cs:RequiresLiveVolumePercentLabel"] = D3,
            ["TesseraMediaFlyoutPolicy.cs:ArmedTimelinePollMs"] = D3,
            ["TesseraMediaFlyoutPolicy.cs:MustPollTimelineWhileArmed"] = D3,
            ["TesseraMediaPresentPolicy.cs:ShouldPumpBeforeBuild"] = D3,
            ["TesseraOsAcrylicSignOffPolicy.cs:AllH3StackedStylesSignedOff"] = D3,
            ["TesseraOsAcrylicSignOffPolicy.cs:SignedOffDate"] = D3,
            ["TesseraOsAcrylicStackedPolicy.cs:PanelCornerRadius"] = D3,
            ["TesseraOsAcrylicStackedPolicy.cs:PresentMustRestackAllSlots"] = D3,
            ["TesseraOsAcrylicStackedPolicy.cs:RoleOwnsLiveHost"] = D3,
            ["TesseraOsAcrylicStackedPolicy.cs:TrialRequested"] = D3,
            ["TesseraOsAcrylicTrialPolicy.cs:OsIsWindows11PrimaryTarget"] = D3,
            ["TesseraStackedPlacementPolicy.cs:HorizontalGapDip"] = D3,
            ["TesseraStackedPlacementSpec.cs:MeterMediaMinContentHeightDip"] = D3,
            ["TesseraStackedPlacementSpec.cs:MeterPanelHeightDip"] = D3,
            ["TesseraStatusFlyoutPolicy.cs:StatusLayoutMustPreferDesiredSizeOverStaleBounds"] = D3,
            ["TesseraVolumeAdjustPolicy.cs:MustClearDragOnPointerCaptureLost"] = D3,
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
