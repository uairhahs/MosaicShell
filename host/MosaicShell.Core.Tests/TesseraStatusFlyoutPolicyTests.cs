using FluentAssertions;
using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Core.Tests;

public class TesseraStatusFlyoutPolicyTests
{
    [Theory]
    [InlineData("locks", true)]
    [InlineData("flight", true)]
    [InlineData("vol", false)]
    [InlineData("media", false)]
    public void Status_kinds_recognized(string kind, bool expected) =>
        TesseraStatusFlyoutPolicy.IsStatusKind(kind).Should().Be(expected);
}
