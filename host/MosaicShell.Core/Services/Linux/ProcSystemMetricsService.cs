using MosaicShell.Core.Services.Proc;

namespace MosaicShell.Core.Services.Linux
{
    /// <summary>
    /// System metrics on Linux from <c>/proc</c> and the mounted filesystems' free space, read as
    /// <see cref="WindowsSystemMetricsService"/> reports them (see <see cref="ProcMetricsPolicy"/>).
    /// </summary>
    /// <remarks>Without a readable <c>/proc</c> the figures stay at zero and nothing throws.</remarks>
    /// <param name="read">Reads a <c>/proc</c> file, or null if it cannot; tests pass a stand-in.</param>
    /// <param name="space">Free and total space of a mount point in GB, or null if it cannot be read.</param>
    /// <param name="wait">The first sample's wait between CPU readings.</param>
    public sealed class ProcSystemMetricsService(
        Func<string, string?>? read = null,
        Func<string, (double FreeGb, double TotalGb)?>? space = null,
        Action? wait = null) : ISystemMetricsService
    {
        public const string StatPath = "/proc/stat";
        public const string MemInfoPath = "/proc/meminfo";
        public const string MountInfoPath = "/proc/self/mountinfo";

        /// <summary>How long the first sample waits between its two CPU readings, as Windows primes its counter.</summary>
        private const int FirstSampleWaitMs = 50;

        private const double BytesPerGb = 1024d * 1024d * 1024d;

        private readonly Func<string, string?> _read = read ?? ReadFile;
        private readonly Func<string, (double FreeGb, double TotalGb)?> _space = space ?? ReadSpace;
        private readonly Action _wait = wait ?? (() => Thread.Sleep(FirstSampleWaitMs));
        private readonly Lock _gate = new();
        private CpuTimes? _previous;

        public SystemMetricsSnapshot Sample()
        {
            double cpu = SampleCpu();

            double totalGb = 0;
            double usedGb = 0;
            double usedPct = 0;
            if (ProcMetricsPolicy.TryParseMemory(_read(MemInfoPath), out ulong totalBytes, out ulong availableBytes) && totalBytes > 0)
            {
                totalGb = totalBytes / BytesPerGb;
                usedGb = (totalBytes - availableBytes) / BytesPerGb;
                usedPct = usedGb / totalGb * 100;
            }

            List<DiskMetric> disks = [];
            foreach (string mountPoint in ProcMetricsPolicy.ParseDiskMounts(_read(MountInfoPath)))
            {
                if (_space(mountPoint) is (double free, double total) && total > 0)
                {
                    disks.Add(new DiskMetric(mountPoint, free, total));
                }
            }

            return new SystemMetricsSnapshot(
                Math.Round(cpu, 1),
                Math.Round(usedPct, 1),
                Math.Round(usedGb, 2),
                Math.Round(totalGb, 2),
                disks,
                Environment.MachineName);
        }

        public void Dispose() { }

        /// <summary>Busy share since the previous sample; the first sample takes two readings a moment apart.</summary>
        private double SampleCpu()
        {
            lock (_gate)
            {
                if (_previous is null)
                {
                    if (!ProcMetricsPolicy.TryParseCpuTimes(_read(StatPath), out CpuTimes first))
                    {
                        return 0;
                    }

                    _previous = first;
                    _wait();
                }

                if (!ProcMetricsPolicy.TryParseCpuTimes(_read(StatPath), out CpuTimes now))
                {
                    return 0;
                }

                double percent = ProcMetricsPolicy.CpuPercent(_previous.Value, now);
                _previous = now;
                return percent;
            }
        }

        private static string? ReadFile(string path)
        {
            try
            {
                return File.ReadAllText(path);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                return null;
            }
        }

        private static (double FreeGb, double TotalGb)? ReadSpace(string mountPoint)
        {
            try
            {
                DriveInfo drive = new(mountPoint);
                return (drive.AvailableFreeSpace / BytesPerGb, drive.TotalSize / BytesPerGb);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
            {
                return null;
            }
        }
    }
}
