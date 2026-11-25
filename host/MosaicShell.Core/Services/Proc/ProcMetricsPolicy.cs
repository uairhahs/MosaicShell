using System.Globalization;
using System.Text;

namespace MosaicShell.Core.Services.Proc
{
    /// <summary>Cumulative CPU time from the aggregate line of <c>/proc/stat</c>, in clock ticks.</summary>
    public readonly record struct CpuTimes(ulong Total, ulong Idle);

    /// <summary>
    /// Linux's <c>/proc</c> read as Windows' system metrics are: CPU busy share between two readings, memory used as total
    /// less available, and the disks a user would name. Kept pure so it is tested on every OS.
    /// </summary>
    public static class ProcMetricsPolicy
    {
        /// <summary>Mount trees that hold the system's own or transient filesystems, not storage a user names.</summary>
        private static readonly string[] SystemTrees = ["/boot", "/efi", "/run", "/var", "/snap", "/nix", "/proc", "/sys", "/dev", "/tmp"];

        /// <summary>
        /// Reads the aggregate <c>cpu</c> line: user nice system idle iowait irq softirq steal [guest guest_nice]. Guest
        /// time is already inside user and nice, so it is left out; iowait counts as idle.
        /// </summary>
        public static bool TryParseCpuTimes(string? stat, out CpuTimes times)
        {
            times = default;
            foreach (string line in Lines(stat))
            {
                string[] fields = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (fields.Length < 9 || fields[0] != "cpu")
                {
                    continue;
                }

                ulong total = 0;
                ulong[] values = new ulong[8];
                for (int i = 0; i < 8; i++)
                {
                    if (!ulong.TryParse(fields[i + 1], NumberStyles.None, CultureInfo.InvariantCulture, out values[i]))
                    {
                        return false;
                    }

                    total += values[i];
                }

                times = new CpuTimes(total, values[3] + values[4]);
                return true;
            }

            return false;
        }

        /// <summary>Busy share (0 to 100) between two readings; 0 when no time passed or the counters went back.</summary>
        public static double CpuPercent(CpuTimes before, CpuTimes after)
        {
            if (after.Total <= before.Total || after.Idle < before.Idle)
            {
                return 0;
            }

            double total = after.Total - before.Total;
            double idle = after.Idle - before.Idle;
            return Math.Clamp((total - idle) / total * 100, 0, 100);
        }

        /// <summary>Reads MemTotal and MemAvailable (MemFree on kernels without it) from <c>/proc/meminfo</c>, in bytes.</summary>
        public static bool TryParseMemory(string? memInfo, out ulong totalBytes, out ulong availableBytes)
        {
            ulong? total = null;
            ulong? available = null;
            ulong? free = null;
            foreach (string line in Lines(memInfo))
            {
                string[] fields = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (fields.Length < 2
                    || !ulong.TryParse(fields[1], NumberStyles.None, CultureInfo.InvariantCulture, out ulong kb))
                {
                    continue;
                }

                switch (fields[0])
                {
                    case "MemTotal:":
                        total = kb * 1024;
                        break;
                    case "MemAvailable:":
                        available = kb * 1024;
                        break;
                    case "MemFree:":
                        free = kb * 1024;
                        break;
                }
            }

            totalBytes = total ?? 0;
            availableBytes = Math.Min(available ?? free ?? 0, totalBytes);
            return total is not null;
        }

        /// <summary>
        /// The mount points of the disks a user would name, from <c>/proc/self/mountinfo</c>, as Windows lists fixed drives:
        /// filesystems on a block device, or a ZFS pool's root dataset, outside the system's own trees; one per device,
        /// named by its shortest mount point.
        /// </summary>
        public static IReadOnlyList<string> ParseDiskMounts(string? mountInfo)
        {
            List<string> names = [];
            Dictionary<string, int> byDevice = [];
            foreach (string line in Lines(mountInfo))
            {
                if (!TryReadMount(line, out string device, out string mountPoint, out string fsType, out string source)
                    || !IsDisk(fsType, source)
                    || InSystemTree(mountPoint))
                {
                    continue;
                }

                if (byDevice.TryGetValue(device, out int index))
                {
                    if (mountPoint.Length < names[index].Length)
                    {
                        names[index] = mountPoint;
                    }

                    continue;
                }

                byDevice[device] = names.Count;
                names.Add(mountPoint);
            }

            return names;
        }

        private static bool TryReadMount(string line, out string device, out string mountPoint, out string fsType, out string source)
        {
            device = mountPoint = fsType = source = string.Empty;
            string[] fields = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            int separator = Array.IndexOf(fields, "-");
            if (separator < 6 || fields.Length < separator + 3)
            {
                return false;
            }

            device = fields[2];
            mountPoint = Unescape(fields[4]);
            fsType = fields[separator + 1];
            source = Unescape(fields[separator + 2]);
            return true;
        }

        private static bool IsDisk(string fsType, string source)
        {
            return fsType == "zfs"
                ? !source.Contains('/')
                : source.StartsWith("/dev/", StringComparison.Ordinal) && !source.StartsWith("/dev/loop", StringComparison.Ordinal);
        }

        private static bool InSystemTree(string mountPoint)
        {
            return SystemTrees.Any(tree => mountPoint == tree || mountPoint.StartsWith(tree + "/", StringComparison.Ordinal));
        }

        /// <summary>mountinfo writes space, tab, newline and backslash as three-digit octal escapes (<c>\040</c>).</summary>
        private static string Unescape(string field)
        {
            if (!field.Contains('\\'))
            {
                return field;
            }

            StringBuilder text = new(field.Length);
            for (int i = 0; i < field.Length; i++)
            {
                if (field[i] == '\\' && IsOctal(field, i + 1))
                {
                    _ = text.Append((char)Convert.ToInt32(field.Substring(i + 1, 3), 8));
                    i += 3;
                }
                else
                {
                    _ = text.Append(field[i]);
                }
            }

            return text.ToString();
        }

        private static bool IsOctal(string field, int start)
        {
            return start + 3 <= field.Length && field.AsSpan(start, 3).IndexOfAnyExceptInRange('0', '7') < 0;
        }

        private static IEnumerable<string> Lines(string? text)
        {
            return string.IsNullOrEmpty(text) ? [] : text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
