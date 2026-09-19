namespace MosaicShell.Core.Tests
{
    /// <summary>A clock a test moves by hand, so timeouts and backoff are tested without waiting.</summary>
    internal sealed class ManualTimeProvider : TimeProvider
    {
        private DateTimeOffset _now = new(2026, 9, 19, 12, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow()
        {
            return _now;
        }

        public void Advance(TimeSpan by)
        {
            _now += by;
        }
    }
}
