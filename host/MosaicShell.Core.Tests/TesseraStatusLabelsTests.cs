using FluentAssertions;
using MosaicShell.Core.Capabilities;

namespace MosaicShell.Core.Tests;

public class TesseraStatusLabelsTests
{
    [Fact]
    public void Format_locks_uses_lock_name_from_payload()
    {
        var request = new FlyoutRequest(
            "Tessera", "locks", "Fluent", "TL", 3000,
            new Dictionary<string, string> { ["lock"] = "CapsLock", ["on"] = "1" },
            1, 0, 0, 0, "Left");

        TesseraStatusLabels.Format(request).Should().Be("CapsLock On");
    }

    [Fact]
    public void Format_flight_uses_airplane_mode_label()
    {
        var request = new FlyoutRequest(
            "Tessera", "flight", "Fluent", "TL", 3000,
            new Dictionary<string, string> { ["on"] = "0" },
            1, 0, 0, 0, "Left");

        TesseraStatusLabels.Format(request).Should().Be("Airplane mode Off");
    }
}
