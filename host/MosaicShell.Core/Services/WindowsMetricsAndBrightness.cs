using System.Management;
using System.Runtime.InteropServices;

namespace MosaicShell.Core.Services;

public sealed class WindowsBrightnessService : IBrightnessService
{
    public bool IsSupported
    {
        get
        {
            try
            {
                using var searcher = new ManagementObjectSearcher("root\\WMI", "SELECT * FROM WmiMonitorBrightness");
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
                using var searcher = new ManagementObjectSearcher("root\\WMI", "SELECT CurrentBrightness FROM WmiMonitorBrightness");
                foreach (ManagementObject obj in searcher.Get())
                    return Convert.ToDouble(obj["CurrentBrightness"]) / 100.0;
            }
            catch { /* unsupported */ }
            return 0.5;
        }
        set
        {
            try
            {
                var level = (byte)Math.Clamp((int)Math.Round(value * 100), 0, 100);
                using var searcher = new ManagementObjectSearcher("root\\WMI", "SELECT * FROM WmiMonitorBrightnessMethods");
                foreach (ManagementObject obj in searcher.Get())
                {
                    obj.InvokeMethod("WmiSetBrightness", new object[] { uint.MaxValue, level });
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

        var memStatus = new MEMORYSTATUSEX();
        memStatus.dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>();
        GlobalMemoryStatusEx(ref memStatus);
        var totalGb = memStatus.ullTotalPhys / (1024d * 1024d * 1024d);
        var availGb = memStatus.ullAvailPhys / (1024d * 1024d * 1024d);
        var usedGb = totalGb - availGb;
        var usedPct = totalGb <= 0 ? 0 : usedGb / totalGb * 100;

        var disks = DriveInfo.GetDrives()
            .Where(d => d.IsReady && d.DriveType == DriveType.Fixed)
            .Select(d => new DiskMetric(
                d.Name.TrimEnd('\\'),
                d.AvailableFreeSpace / (1024d * 1024d * 1024d),
                d.TotalSize / (1024d * 1024d * 1024d)))
            .ToList();

        return new SystemMetricsSnapshot(
            Math.Round(cpu, 1),
            Math.Round(usedPct, 1),
            Math.Round(usedGb, 2),
            Math.Round(totalGb, 2),
            disks,
            Environment.MachineName);
    }

    public void Dispose() => _cpu?.Dispose();

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
