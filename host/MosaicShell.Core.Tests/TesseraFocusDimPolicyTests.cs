using FluentAssertions;
using MosaicShell.Core.Capabilities;
using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Core.Tests;

public class TesseraFocusDimPolicyTests
{
    [Fact]
    public void Overlay_alpha_is_subtle()
    {
        TesseraFocusDimPolicy.OverlayAlpha.Should().BeInRange((byte)56, (byte)80);
    }

    [Fact]
    public void Payload_defaults_to_enabled()
    {
        TesseraFocusDimPolicy.EnabledFromPayload(null).Should().BeTrue();
        TesseraFocusDimPolicy.EnabledFromPayload(new Dictionary<string, string>()).Should().BeTrue();
    }

    [Fact]
    public void Payload_zero_disables()
    {
        TesseraFocusDimPolicy.EnabledFromPayload(
            new Dictionary<string, string> { ["focusDim"] = "0" }).Should().BeFalse();
    }

    [Fact]
    public void Must_never_block_input_or_linger_on_click()
    {
        TesseraFocusDimPolicy.MustPassThroughInput.Should().BeTrue();
        TesseraFocusDimPolicy.InstantDismissOnOutsideClick.Should().BeTrue();
    }
}
