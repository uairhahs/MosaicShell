namespace MosaicShell.Core.Install;

public sealed class HostInstallProgress
{
    public required string Stage { get; init; }
    public string? Detail { get; init; }
    public double? Fraction { get; init; }
}

public sealed class HostInstallRequest
{
    /// <summary>Extracted release bundle root (contains Host*/Mosaicist/Tiles).</summary>
    public required string BundleRoot { get; init; }

    /// <summary>Destination app directory (Host + Mosaicist + Tiles copied here).</summary>
    public required string TargetDirectory { get; init; }

    public HostInstallVariant Variant { get; init; } = HostInstallVariant.FrameworkDependent;

    public IReadOnlyList<string> ModuleIds { get; init; } = HostInstallPolicy.DefaultModuleIds;

    public bool CreateStartMenuShortcut { get; init; } = true;

    public bool EnableLogonAutostart { get; init; }

    /// <summary>When true, Hub welcome stays incomplete so Host shows onboarding.</summary>
    public bool ResetWelcomeCompleted { get; init; } = true;

    /// <summary>Test override for Start Menu Programs folder.</summary>
    public string? StartMenuDirectoryOverride { get; init; }

    /// <summary>Test override for Startup folder.</summary>
    public string? StartupDirectoryOverride { get; init; }

    /// <summary>Test override for DotNet root used by the desktop-runtime probe.</summary>
    public string? DotNetRootOverride { get; init; }
}

public sealed class HostInstallResult
{
    public required string TargetDirectory { get; init; }
    public required string HostExePath { get; init; }
    public IReadOnlyList<string> InstalledModules { get; init; } = [];
    public string? Version { get; init; }
}
