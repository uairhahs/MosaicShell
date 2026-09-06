using System.Management;
using System.Runtime.InteropServices;

namespace MosaicShell.Core.Services
{
    public sealed class WindowsBrightnessService : IBrightnessService
    {
        public bool IsSupported
        {
            get
            {
                try
                {
                    using ManagementObjectSearcher searcher = new("root\\WMI", "SELECT * FROM WmiMonitorBrightness");
                    return searcher.Get().Count > 0;
                }
                catch
                {
                    return false;
                }
            }
        }

        public double Brightness
        {
            get
            {
                try
                {
                    using ManagementObjectSearcher searcher = new("root\\WMI", "SELECT CurrentBrightness FROM WmiMonitorBrightness");
                    foreach (ManagementObject obj in searcher.Get().Cast<ManagementObject>())
                    {
                        return Convert.ToDouble(obj["CurrentBrightness"]) / 100.0;
                    }
                }
                catch { /* unsupported */ }
                return 0.5;
            }
            set
            {
                try
                {
                    byte level = (byte)Math.Clamp((int)Math.Round(value * 100), 0, 100);
                    using ManagementObjectSearcher searcher = new("root\\WMI", "SELECT * FROM WmiMonitorBrightnessMethods");
                    foreach (ManagementObject obj in searcher.Get().Cast<ManagementObject>())
                    {
                        _ = obj.InvokeMethod("WmiSetBrightness", [uint.MaxValue, level]);
                        break;
                    }
                }
                catch { /* ignore */ }
            }
        }
    }

    public sealed class WindowsSystemMetricsService : ISystemMetricsService
    {
        private readonly System.Diagnostics.PerformanceCounter? _cpu;
        private bool _primed;

        public WindowsSystemMetricsService()
        {
            try
            {
                _cpu = new System.Diagnostics.PerformanceCounter("Processor", "% Processor Time", "_Total");
                _ = _cpu.NextValue();
                _primed = true;
            }
            catch
            {
                _cpu = null;
            }
        }

        public SystemMetricsSnapshot Sample()
        {
            double cpu = 0;
            if (_cpu is not null)
            {
                cpu = _cpu.NextValue();
                if (!_primed)
                {
                    Thread.Sleep(50);
                    cpu = _cpu.NextValue();
                    _primed = true;
                }
            }

            MEMORYSTATUSEX memStatus = new()
            {
                dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>()
            };
            _ = GlobalMemoryStatusEx(ref memStatus);
            double totalGb = memStatus.ullTotalPhys / (1024d * 1024d * 1024d);
            double availGb = memStatus.ullAvailPhys / (1024d * 1024d * 1024d);
            double usedGb = totalGb - availGb;
            double usedPct = totalGb <= 0 ? 0 : usedGb / totalGb * 100;

            List<DiskMetric> disks = [.. DriveInfo.GetDrives()
                .Where(d => d.IsReady && d.DriveType == DriveType.Fixed)
                .Select(d => new DiskMetric(
                    d.Name.TrimEnd('\\'),
                    d.AvailableFreeSpace / (1024d * 1024d * 1024d),
                    d.TotalSize / (1024d * 1024d * 1024d)))];

            return new SystemMetricsSnapshot(
                Math.Round(cpu, 1),
                Math.Round(usedPct, 1),
                Math.Round(usedGb, 2),
                Math.Round(totalGb, 2),
                disks,
                Environment.MachineName);
        }

        public void Dispose()
        {
            _cpu?.Dispose();
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);
    }
}
