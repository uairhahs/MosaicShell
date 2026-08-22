using FluentAssertions;
using MosaicShell.Core.Scale;

namespace MosaicShell.Core.Tests;

public class ScaleContractTests
{
    [Fact]
    public void UiScale_equals_UserScale()
    {
        var c = new ScaleContract();
        c.SetUserScale(1.0);
        c.UiScale.Should().Be(1.0);

        c.SetUserScale(0.8);
        c.UiScale.Should().Be(0.8);
    }

    [Theory]
    [InlineData(0.74)]
    [InlineData(2.01)]
    public void SetUserScale_rejects_out_of_range(double bad)
    {
        var c = new ScaleContract();
        var act = () => c.SetUserScale(bad);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ResetUserScale_returns_to_one()
    {
        var c = new ScaleContract();
        c.SetUserScale(1.25);
        c.ResetUserScale();
        c.UserScale.Should().Be(1.0);
    }

    [Fact]
    public void Roundtrip_settings_preserves_user_scale_only()
    {
        var c = new ScaleContract();
        c.SetUserScale(1.1);
        var again = ScaleContract.FromSettings(c.ToSettings());
        again.UserScale.Should().Be(1.1);
        again.UiScale.Should().Be(1.1);
    }

    [Fact]
    public void Load_ignores_legacy_DpiScale_in_json()
    {
        var path = Path.Combine(Path.GetTempPath(), $"mosaic-scale-{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(path, """{"DpiScale":1.5,"UserScale":1.2}""");
            var settings = ScaleSettingsStore.Load(path);
            settings.DpiScale.Should().Be(1.0);
            settings.UserScale.Should().Be(1.2);
            var c = ScaleContract.FromSettings(settings);
            c.UiScale.Should().Be(1.2);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
