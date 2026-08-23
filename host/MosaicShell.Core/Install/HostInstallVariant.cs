namespace MosaicShell.Core.Install;

public enum HostInstallVariant
{
    /// <summary>Requires .NET 10 Desktop Runtime on the machine.</summary>
    FrameworkDependent = 0,

    /// <summary>Published with runtime included (larger footprint).</summary>
    SelfContained = 1,
}
