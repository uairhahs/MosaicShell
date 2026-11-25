namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// Waiting in a test for something another thread or process does (a bus signal, a timer, a pipe), without a fixed
    /// sleep: the condition is polled until it holds or <see cref="Patience"/> runs out. Imported with
    /// <c>using static</c> so each test reads <c>await EventuallyAsync(...)</c>.
    /// </summary>
    internal static class TestWaits
    {
        /// <summary>How long a test waits for a condition before it fails.</summary>
        public static readonly TimeSpan Patience = TimeSpan.FromSeconds(5);

        private const int PollMs = 20;

        public static Task<bool> EventuallyAsync(Func<bool> condition)
        {
            return EventuallyAsync(() => Task.FromResult(condition()));
        }

        public static async Task<bool> EventuallyAsync(Func<Task<bool>> condition)
        {
            DateTime until = DateTime.UtcNow + Patience;
            while (DateTime.UtcNow < until)
            {
                if (await condition().ConfigureAwait(false))
                {
                    return true;
                }

                await Task.Delay(PollMs).ConfigureAwait(false);
            }

            return await condition().ConfigureAwait(false);
        }
    }
}
