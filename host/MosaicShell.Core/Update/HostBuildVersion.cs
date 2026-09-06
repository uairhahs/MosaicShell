using System.Reflection;

namespace MosaicShell.Core.Update
{
    /// <summary>Reads the stamped Host build label from the entry assembly.</summary>
    public static class HostBuildVersion
    {
        public static string ReadCurrent()
        {
            Assembly assembly = Assembly.GetEntryAssembly()
                ?? typeof(HostBuildVersion).Assembly;
            return ReadFromAssembly(assembly);
        }

        public static string ReadFromAssembly(Assembly assembly)
        {
            string? info = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                ?? assembly.GetName().Version?.ToString();
            if (string.IsNullOrWhiteSpace(info))
            {
                return HostBuildVersionPolicy.LocalDevLabel;
            }

            int plus = info.IndexOf('+');
            if (plus >= 0)
            {
                info = info[..plus];
            }

            info = info.Trim().TrimStart('v', 'V');
            return string.IsNullOrWhiteSpace(info)
                ? HostBuildVersionPolicy.LocalDevLabel
                : info;
        }
    }
}
