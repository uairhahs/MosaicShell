using FluentAssertions;
using MosaicShell.Core.Modules;

namespace MosaicShell.Core.Tests;

public class HostScrollbarChromeSpecTests
{
    [Fact]
    public void Host_must_leave_fluent_scrollbar_chrome_unmodified()
    {
        HostScrollbarChromeSpec.OverrideFluentChrome.Should().BeFalse();
        HostScrollbarChromeSpec.OverrideScrollBarSize.Should().BeFalse();
        HostScrollbarChromeSpec.OverrideThumbBrushes.Should().BeFalse();
        HostScrollbarChromeSpec.HideLineButtons.Should().BeFalse();
    }
}
