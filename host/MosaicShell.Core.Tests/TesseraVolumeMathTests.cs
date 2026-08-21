using FluentAssertions;
using MosaicShell.Core.Services;

namespace MosaicShell.Core.Tests;

public class TesseraVolumeMathTests
{
    [Theory]
    [InlineData(0.504, 50)]
    [InlineData(0.495, 50)]
    [InlineData(0.5, 50)]
    [InlineData(0.0, 0)]
    [InlineData(1.0, 100)]
    [InlineData(0.999, 100)]
    public void ToPercent_rounds_stably(double v, int expected) =>
        VolumePercent.ToPercent(v).Should().Be(expected);

    [Fact]
    public void Step_moves_by_whole_percents()
    {
        VolumePercent.Step(0.50, +2).Should().BeApproximately(0.52, 1e-9);
        VolumePercent.Step(0.50, -2).Should().BeApproximately(0.48, 1e-9);
        VolumePercent.Step(0.01, -2).Should().Be(0);
        VolumePercent.Step(0.99, +2).Should().Be(1);
    }

    [Fact]
    public void Quantize_removes_sub_percent_noise() =>
        VolumePercent.Quantize(0.521).Should().BeApproximately(0.52, 1e-9);
}
