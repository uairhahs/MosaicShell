namespace MosaicShell.Core.Tests
{
    /// <summary>A clock a test moves by hand, so timeouts, backoff and timers are tested without waiting.</summary>
    internal sealed class ManualTimeProvider : TimeProvider
    {
        private readonly List<ManualTimer> _timers = [];
        private DateTimeOffset _now = new(2026, 9, 19, 12, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow()
        {
            return _now;
        }

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            ManualTimer timer = new(this, callback, state);
            _ = timer.Change(dueTime, period);
            _timers.Add(timer);
            return timer;
        }

        /// <summary>Moves time forward, running every timer that comes due on the way, in order.</summary>
        public void Advance(TimeSpan by)
        {
            DateTimeOffset end = _now + by;
            while (true)
            {
                ManualTimer? next = _timers.Where(t => t.Due is not null && t.Due <= end).OrderBy(t => t.Due).FirstOrDefault();
                if (next is null)
                {
                    break;
                }

                _now = next.Due!.Value;
                next.Fire();
            }

            _now = end;
        }

        private sealed class ManualTimer(ManualTimeProvider owner, TimerCallback callback, object? state) : ITimer
        {
            private TimeSpan _period;

            public DateTimeOffset? Due { get; private set; }

            public bool Change(TimeSpan dueTime, TimeSpan period)
            {
                Due = dueTime == Timeout.InfiniteTimeSpan ? null : owner._now + dueTime;
                _period = period;
                return true;
            }

            public void Fire()
            {
                Due = _period == Timeout.InfiniteTimeSpan || _period == TimeSpan.Zero ? null : owner._now + _period;
                callback(state);
            }

            public void Dispose()
            {
                Due = null;
                _ = owner._timers.Remove(this);
            }

            public ValueTask DisposeAsync()
            {
                Dispose();
                return ValueTask.CompletedTask;
            }
        }
    }
}
