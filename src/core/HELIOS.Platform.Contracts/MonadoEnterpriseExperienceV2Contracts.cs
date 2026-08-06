namespace HELIOS.Platform.Contracts.MonadoEnterprise.V2;

/// <summary>
/// Permanent Monado enterprise profile catalog for v2 contracts.
/// </summary>
public enum MonadoEnterpriseProfileId
{
    Core,
    Developer,
    Gamer,
    Studio,
    Personal,
    SysOps,
    AiServer,
    SysAdmin
}

/// <summary>
/// Typed profile boundary consumed by C# services and validators.
/// </summary>
public sealed record MonadoEnterpriseProfileDefinition(
    MonadoEnterpriseProfileId Id,
    bool Interactive,
    bool Administrator,
    bool Hidden,
    bool EnabledByDefault);

/// <summary>
/// Minimal storage checkpoint used to verify safety-critical v2 storage invariants.
/// </summary>
public sealed record MonadoEnterpriseStorageCheckpoint(
    bool HasCoreCrossPhysicalVolume,
    bool HasDynamicDevDriveVhdx,
    bool HasBitLockerVaultVhdx,
    bool VaultAutoMountEnabled);

/// <summary>
/// Minimal synchronization checkpoint used to verify governance-safe v2 sync invariants.
/// </summary>
public sealed record MonadoEnterpriseSyncCheckpoint(
    string ExecutionMode,
    bool DirectExternalDeliveryEnabled,
    bool AzureDevOpsReadOnly,
    bool AdobeDesignWritesEnabled);

/// <summary>
/// Fail-closed validation helper for v2 profile, storage, and synchronization boundaries.
/// </summary>
public static class MonadoEnterpriseExperienceContractValidator
{
    private static readonly HashSet<MonadoEnterpriseProfileId> ExpectedProfiles = new()
    {
        MonadoEnterpriseProfileId.Core,
        MonadoEnterpriseProfileId.Developer,
        MonadoEnterpriseProfileId.Gamer,
        MonadoEnterpriseProfileId.Studio,
        MonadoEnterpriseProfileId.Personal,
        MonadoEnterpriseProfileId.SysOps,
        MonadoEnterpriseProfileId.AiServer,
        MonadoEnterpriseProfileId.SysAdmin
    };

    public static void ValidateProfileCatalog(IReadOnlyCollection<MonadoEnterpriseProfileDefinition> profiles)
    {
        ArgumentNullException.ThrowIfNull(profiles);

        var ids = profiles.Select(profile => profile.Id).ToArray();
        var idSet = ids.ToHashSet();
        if (!idSet.SetEquals(ExpectedProfiles) || ids.Length != ExpectedProfiles.Count)
        {
            throw new InvalidOperationException("Profile catalog must contain exactly the eight permanent v2 profiles.");
        }

        var administrators = profiles.Where(profile => profile.Administrator).ToArray();
        if (administrators.Length != 1 || administrators[0].Id != MonadoEnterpriseProfileId.SysAdmin)
        {
            throw new InvalidOperationException("SysAdmin must be the only administrator profile.");
        }

        var sysAdmin = administrators[0];
        if (!sysAdmin.Hidden || sysAdmin.EnabledByDefault)
        {
            throw new InvalidOperationException("SysAdmin must be hidden and disabled by default.");
        }
    }

    public static void ValidateStorageCheckpoint(MonadoEnterpriseStorageCheckpoint checkpoint)
    {
        ArgumentNullException.ThrowIfNull(checkpoint);

        if (!checkpoint.HasCoreCrossPhysicalVolume)
        {
            throw new InvalidOperationException("Disk 0 must include the physical X: CORE_CROSS volume.");
        }

        if (!checkpoint.HasDynamicDevDriveVhdx)
        {
            throw new InvalidOperationException("Disk 1 must include a dynamic D: ReFS dev drive VHDX.");
        }

        if (!checkpoint.HasBitLockerVaultVhdx)
        {
            throw new InvalidOperationException("Disk 1 must include a dynamic V: BitLocker vault VHDX.");
        }

        if (checkpoint.VaultAutoMountEnabled)
        {
            throw new InvalidOperationException("Vault VHDX must never auto-mount.");
        }
    }

    public static void ValidateSyncCheckpoint(MonadoEnterpriseSyncCheckpoint checkpoint)
    {
        ArgumentNullException.ThrowIfNull(checkpoint);

        if (!string.Equals(checkpoint.ExecutionMode, "proposal-only", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Synchronization execution mode must remain proposal-only.");
        }

        if (checkpoint.DirectExternalDeliveryEnabled)
        {
            throw new InvalidOperationException("Direct external delivery must remain disabled.");
        }

        if (!checkpoint.AzureDevOpsReadOnly)
        {
            throw new InvalidOperationException("Azure DevOps mirror must remain read-only.");
        }

        if (checkpoint.AdobeDesignWritesEnabled)
        {
            throw new InvalidOperationException("Adobe design integration is evidence-reference only and cannot write.");
        }
    }
}
