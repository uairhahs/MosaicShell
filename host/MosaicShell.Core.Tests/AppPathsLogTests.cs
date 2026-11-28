using FluentAssertions;

namespace MosaicShell.Core.Tests
{
    /// <summary>The flyout log the Hub points at is where the Host really writes it, on every OS and data folder.</summary>
    public sealed class AppPathsLogTests : IDisposable
    {
        public void Dispose()
        {
            AppPaths.ClearRootOverride();
        }

        [Fact]
        public void The_flyout_log_is_in_the_data_folders_cache()
        {
            string root = Path.Combine(Path.GetTempPath(), "MosaicLogPath_" + Guid.NewGuid().ToString("N"));
            AppPaths.SetRootOverride(root);

            _ = AppPaths.FlyoutLogPath.Should().Be(Path.Combine(root, "Cache", "flyout.log"));
            _ = AppPaths.FlyoutLogPath.Should().NotContain("%");
        }
    }
}
