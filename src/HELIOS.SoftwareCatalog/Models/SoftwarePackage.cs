namespace HELIOS.SoftwareCatalog.Models;

public sealed record SoftwarePackage(
    string Id,
    string DisplayName,
    string Source,
    string PackageId,
    string Category,
    bool PostInstallOnly,
    bool RequiresHardwareDetection,
    IReadOnlyList<string> Bundles);

public sealed record SoftwareBundle(
    string Id,
    string DisplayName,
    string Description,
    IReadOnlyList<string> PackageIds,
    bool RequiresApproval);
