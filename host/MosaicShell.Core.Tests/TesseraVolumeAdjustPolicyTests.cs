using FluentAssertions;
using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Core.Tests;

public class TesseraVolumeAdjustPolicyTests
{
    [Fact]
    public void Volume_controls_must_clear_drag_on_pointer_capture_lost()
    {
        TesseraVolumeAdjustPolicy.MustClearDragOnPointerCaptureLost.Should().BeTrue();
    }

    [Fact]
    public void User_adjust_grace_matches_ring_and_track_mark_window()
    {
        TesseraVolumeAdjustPolicy.UserAdjustGracePeriod.Should().Be(TimeSpan.FromMilliseconds(350));
    }

    [Fact]
    public void Volume_ring_arc_must_invalidate_visual_on_sweep_change()
    {
        TesseraVolumeAdjustPolicy.VolumeRingArcMustInvalidateVisualOnSweepChange.Should().BeTrue();
    }
}
