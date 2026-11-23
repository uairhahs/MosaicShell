using System.ComponentModel;
using System.Diagnostics;

namespace MosaicShell.Core.Services.Linux
{
    /// <summary>
    /// Child processes that must not outlive their owner. A child is added when started and removed when its owner
    /// has finished with it; <see cref="KillAll"/> ends whatever is still tracked, synchronously, so it is safe to call
    /// from a process-exit handler where no async continuation will run.
    /// </summary>
    public sealed class ChildProcessSet
    {
        private static readonly TimeSpan ExitWait = TimeSpan.FromMilliseconds(500);
        private readonly Lock _gate = new();
        private readonly HashSet<Process> _live = [];

        public void Add(Process process)
        {
            lock (_gate)
            {
                _ = _live.Add(process);
            }
        }

        public void Remove(Process process)
        {
            lock (_gate)
            {
                _ = _live.Remove(process);
            }
        }

        /// <summary>Ids of the tracked children that are still running, for diagnostics and tests.</summary>
        internal IReadOnlyList<int> Ids
        {
            get
            {
                lock (_gate)
                {
                    List<int> ids = [];
                    foreach (Process process in _live)
                    {
                        try
                        {
                            if (!process.HasExited)
                            {
                                ids.Add(process.Id);
                            }
                        }
                        catch (InvalidOperationException)
                        {
                            // Disposed or never started.
                        }
                    }

                    return ids;
                }
            }
        }

        public void KillAll()
        {
            Process[] all;
            lock (_gate)
            {
                all = [.. _live];
                _live.Clear();
            }

            foreach (Process process in all)
            {
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill();
                        _ = process.WaitForExit(ExitWait);
                    }
                }
                catch (Exception ex) when (ex is InvalidOperationException or Win32Exception)
                {
                    // Already gone, or disposed by its owner in the meantime.
                }
            }
        }
    }
}
