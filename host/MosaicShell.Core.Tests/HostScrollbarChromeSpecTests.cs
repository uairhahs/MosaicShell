using FluentAssertions;
using MosaicShell.Core.Modules;

namespace MosaicShell.Core.Tests
{
    public class HostScrollbarChromeSpecTests
    {
        [Fact]
        public void Host_must_leave_fluent_scrollbar_chrome_unmodified()
        {
            _ = HostScrollbarChromeSpec.OverrideFluentChrome.Should().BeFalse();
            _ = HostScrollbarChromeSpec.OverrideScrollBarSize.Should().BeFalse();
            _ = HostScrollbarChromeSpec.OverrideThumbBrushes.Should().BeFalse();
            _ = HostScrollbarChromeSpec.HideLineButtons.Should().BeFalse();
        }
    }
}
