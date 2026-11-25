using FluentAssertions;
using MosaicShell.Core.Services;
using MosaicShell.Core.Services.Linux;

namespace MosaicShell.Core.Tests.Linux
{
    /// <summary><see cref="ProcSystemMetricsService"/> against a stand-in <c>/proc</c>, and once against the real one.</summary>
    public sealed class ProcSystemMetricsServiceTests
    {
        private const string MemInfo = "MemTotal: 8388608 kB\nMemAvailable: 2097152 kB\n";
        private const string Mounts =
            "24 1 259:2 / / rw - ext4 /dev/nvme0n1p2 rw\n"
            + "25 24 8:16 / /data rw - ext4 /dev/sdb rw\n"
            + "26 24 8:32 / /mnt/gone rw - ext4 /dev/sdc rw\n";

        private sealed class FakeProc
        {
            public Queue<string> Stats { get; } = new();
            public string? MemInfo { get; set; } = ProcSystemMetricsServiceTests.MemInfo;
            public string? Mounts { get; set; } = ProcSystemMetricsServiceTests.Mounts;
            public int Waits { get; private set; }

            public string? Read(string path)
            {
                return path switch
                {
                    ProcSystemMetricsService.StatPath => Stats.Count > 0 ? Stats.Dequeue() : null,
                    ProcSystemMetricsService.MemInfoPath => MemInfo,
                    ProcSystemMetricsService.MountInfoPath => Mounts,
                    _ => null,
                };
            }

            public void Wait()
            {
                Waits++;
            }
        }

        private static (double FreeGb, double TotalGb)? Space(string mountPoint)
        {
            return mountPoint switch
            {
                "/" => (100, 500),
                "/data" => (1500, 2000),
                _ => null,
            };
        }

        private static string Stat(ulong busy, ulong idle)
        {
            return $"cpu  {busy} 0 0 {idle} 0 0 0 0 0 0\n";
        }

        [Fact]
        public void The_first_sample_waits_briefly_so_cpu_has_two_readings()
        {
            // Windows' processor counter is primed the same way: one reading alone gives no rate.
            FakeProc proc = new();
            proc.Stats.Enqueue(Stat(busy: 100, idle: 900));
            proc.Stats.Enqueue(Stat(busy: 150, idle: 950));
            using ProcSystemMetricsService metrics = new(proc.Read, Space, proc.Wait);

            SystemMetricsSnapshot s = metrics.Sample();

            _ = proc.Waits.Should().Be(1);
            _ = s.CpuPercent.Should().Be(50);
        }

        [Fact]
        public void Later_samples_measure_cpu_since_the_previous_sample_without_waiting()
        {
            FakeProc proc = new();
            proc.Stats.Enqueue(Stat(busy: 100, idle: 900));
            proc.Stats.Enqueue(Stat(busy: 150, idle: 950));
            proc.Stats.Enqueue(Stat(busy: 160, idle: 1040));
            using ProcSystemMetricsService metrics = new(proc.Read, Space, proc.Wait);
            _ = metrics.Sample();

            SystemMetricsSnapshot s = metrics.Sample();

            _ = proc.Waits.Should().Be(1);
            _ = s.CpuPercent.Should().Be(10);
        }

        [Fact]
        public void Memory_is_reported_in_gigabytes_and_percent_used()
        {
            FakeProc proc = new();
            using ProcSystemMetricsService metrics = new(proc.Read, Space, proc.Wait);

            SystemMetricsSnapshot s = metrics.Sample();

            _ = s.RamTotalGb.Should().Be(8);
            _ = s.RamUsedGb.Should().Be(6);
            _ = s.RamUsedPercent.Should().Be(75);
            _ = s.MachineName.Should().Be(Environment.MachineName);
        }

        [Fact]
        public void Disks_list_each_mount_whose_space_can_be_read()
        {
            FakeProc proc = new();
            using ProcSystemMetricsService metrics = new(proc.Read, Space, proc.Wait);

            IReadOnlyList<DiskMetric> disks = metrics.Sample().Disks;

            _ = disks.Should().Equal(new DiskMetric("/", 100, 500), new DiskMetric("/data", 1500, 2000));
        }

        [Fact]
        public void Without_proc_the_sample_is_empty_but_named_and_nothing_throws()
        {
            FakeProc proc = new() { MemInfo = null, Mounts = null };
            using ProcSystemMetricsService metrics = new(proc.Read, Space, proc.Wait);

            SystemMetricsSnapshot s = metrics.Sample();

            _ = s.Should().BeEquivalentTo(new SystemMetricsSnapshot(0, 0, 0, 0, [], Environment.MachineName));
        }

        [Fact]
        public void The_real_proc_reports_memory_and_a_cpu_share()
        {
            if (!OperatingSystem.IsLinux())
            {
                return;
            }

            using ProcSystemMetricsService metrics = new();
            SystemMetricsSnapshot s = metrics.Sample();

            _ = s.RamTotalGb.Should().BeGreaterThan(0);
            _ = s.RamUsedPercent.Should().BeInRange(0, 100);
            _ = s.CpuPercent.Should().BeInRange(0, 100);
            _ = s.Disks.Should().OnlyContain(d => d.TotalGb > 0 && d.FreeGb >= 0 && d.FreeGb <= d.TotalGb);
        }
    }
}
