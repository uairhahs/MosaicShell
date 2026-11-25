using FluentAssertions;
using MosaicShell.Core.Services.Proc;

namespace MosaicShell.Core.Tests
{
    /// <summary>CPU, memory and disks from Linux's <c>/proc</c>, mapped to what Windows' metrics report.</summary>
    public sealed class ProcMetricsPolicyTests
    {
        private const string Stat =
            "cpu  1000 20 300 5000 100 40 30 0 50 0\n"
            + "cpu0 1 2 3 4 5 6 7 0 0 0\n";

        private const string MemInfo =
            "MemTotal:       16000000 kB\nMemFree:         1000000 kB\nMemAvailable:    6000000 kB\n";

        [Fact]
        public void Cpu_times_count_iowait_as_idle_and_leave_out_guest_time()
        {
            // Guest time is already inside user and nice, so adding it would count it twice.
            _ = ProcMetricsPolicy.TryParseCpuTimes(Stat, out CpuTimes times).Should().BeTrue();
            _ = times.Idle.Should().Be(5000UL + 100);
            _ = times.Total.Should().Be(1000UL + 20 + 300 + 5000 + 100 + 40 + 30 + 0);
        }

        [Theory]
        [InlineData("")]
        [InlineData("cpu0 1 2 3 4\n")]
        [InlineData("cpu  x y z\n")]
        public void Cpu_times_need_the_aggregate_line(string stat)
        {
            _ = ProcMetricsPolicy.TryParseCpuTimes(stat, out _).Should().BeFalse();
        }

        [Fact]
        public void Cpu_percent_is_the_busy_share_between_two_samples()
        {
            CpuTimes before = new(Total: 1000, Idle: 800);
            CpuTimes after = new(Total: 1200, Idle: 950);
            _ = ProcMetricsPolicy.CpuPercent(before, after).Should().BeApproximately(25, 1e-9);
        }

        [Fact]
        public void Cpu_percent_is_zero_when_no_time_passed_or_counters_went_back()
        {
            CpuTimes same = new(Total: 1000, Idle: 800);
            _ = ProcMetricsPolicy.CpuPercent(same, same).Should().Be(0);
            _ = ProcMetricsPolicy.CpuPercent(same, new CpuTimes(Total: 900, Idle: 700)).Should().Be(0);
        }

        [Fact]
        public void Memory_used_is_total_less_available_as_windows_reports_it()
        {
            // Windows' available physical memory includes reclaimable cache, which is what MemAvailable estimates.
            _ = ProcMetricsPolicy.TryParseMemory(MemInfo, out ulong totalBytes, out ulong availableBytes).Should().BeTrue();
            _ = totalBytes.Should().Be(16000000UL * 1024);
            _ = availableBytes.Should().Be(6000000UL * 1024);
        }

        [Fact]
        public void Memory_without_mem_available_falls_back_to_mem_free()
        {
            _ = ProcMetricsPolicy.TryParseMemory("MemTotal: 2048 kB\nMemFree: 512 kB\n", out ulong total, out ulong available)
                .Should().BeTrue();
            _ = total.Should().Be(2048UL * 1024);
            _ = available.Should().Be(512UL * 1024);
        }

        [Fact]
        public void Memory_needs_a_total()
        {
            _ = ProcMetricsPolicy.TryParseMemory("MemFree: 512 kB\n", out _, out _).Should().BeFalse();
        }

        // mountinfo: id parent major:minor root mount-point options [optional fields] - fstype source super-options
        private const string MountInfo =
            "24 1 259:2 / / rw,relatime shared:1 - ext4 /dev/nvme0n1p2 rw\n"
            + "25 24 0:26 / /run rw,nosuid shared:2 - tmpfs tmpfs rw\n"
            + "26 24 259:2 /nix/store /nix/store ro,relatime shared:3 - ext4 /dev/nvme0n1p2 rw\n"
            + "27 24 259:5 / /data rw,relatime shared:4 - ext4 /dev/nvme1n1p1 rw\n"
            + "28 24 8:16 / /mnt/backup rw,relatime shared:5 - ext4 /dev/sdb rw\n"
            + "29 24 259:1 / /boot rw,relatime shared:6 - vfat /dev/nvme0n1p1 rw\n"
            + "30 24 0:52 / /tank rw shared:7 - zfs tank rw\n"
            + "31 30 0:53 / /tank/media rw shared:8 - zfs tank/media rw\n"
            + "32 24 230:16 / /var/lib/containers rw shared:9 - ext4 /dev/zd16 rw\n"
            + "33 24 0:61 / /srv/pool rw shared:10 - fuse.mergerfs mergerfs rw\n"
            + "34 24 0:116 / /var/lib/containers/storage/overlay/x/merged rw - overlay overlay rw\n"
            + "35 24 7:0 / /snap/core/1 ro shared:11 - squashfs /dev/loop0 ro\n"
            + "36 24 8:80 / /run/media/me/My\\040Stick rw shared:12 - exfat /dev/sdf1 rw\n";

        [Fact]
        public void Disks_are_one_per_device_or_pool_named_by_their_mount_point()
        {
            // As Windows lists fixed drives: storage a user would name, not tmpfs, overlays, boot or system trees, and not
            // removable media (udisks mounts it under /run/media; Windows leaves removable drives out too).
            _ = ProcMetricsPolicy.ParseDiskMounts(MountInfo).Should().Equal("/", "/data", "/mnt/backup", "/tank");
        }

        [Fact]
        public void A_device_mounted_twice_is_listed_once_by_its_shortest_mount_point()
        {
            const string twice =
                "40 1 8:1 / /data/deep rw - ext4 /dev/sda1 rw\n"
                + "41 1 8:1 / /data rw - ext4 /dev/sda1 rw\n";
            _ = ProcMetricsPolicy.ParseDiskMounts(twice).Should().Equal("/data");
        }

        [Fact]
        public void Escaped_characters_in_mount_points_are_decoded()
        {
            _ = ProcMetricsPolicy.ParseDiskMounts("40 1 8:1 / /mnt/My\\040Disk rw - ext4 /dev/sda1 rw\n")
                .Should().Equal("/mnt/My Disk");
        }

        [Fact]
        public void Malformed_mount_lines_are_skipped()
        {
            _ = ProcMetricsPolicy.ParseDiskMounts("garbage\n24 1 259:5 / / rw\n").Should().BeEmpty();
        }
    }
}
