using System.Collections.ObjectModel;

namespace HELIOS.Platform.Contracts.Monadoblade;

/// <summary>
/// Canonical Monadoblade operating profiles.
/// </summary>
public enum MonadobladeProfileId
{
    Developer,
    SysAdmin,
    SysOps,
    Gamer,
    Studio,
    Personal,
    ServerBackground
}

/// <summary>
/// Security classification carried by every HELIOS Fabric message.
/// </summary>
public enum FabricSecurityClassification
{
    Public,
    Internal,
    SensitiveGated,
    LocalOnly,
    QuarantineEvidence
}

/// <summary>
/// Resource budgets and scheduling intent for one active profile.
/// </summary>
public sealed record ProfileResourcePolicy(
    string CpuMode,
    string GpuMode,
    int MemoryReserveGb,
    int ContainerBudgetGb,
    int VmBudgetGb,
    int ModelBudgetGb,
    string BackgroundPriority);

/// <summary>
/// Local activation controls. SysAdmin requires physical presence and two independent local factors.
/// </summary>
public sealed record ProfileActivationPolicy(
    bool PhysicalPresenceRequired,
    int MinimumFactors,
    IReadOnlyList<string> AllowedFactors,
    bool RemoteActivationDenied,
    bool CloudActivationDenied,
    bool AiActivationDenied)
{
    public static ProfileActivationPolicy StandardUser { get; } = new(
        PhysicalPresenceRequired: false,
        MinimumFactors: 0,
        AllowedFactors: Array.Empty<string>(),
        RemoteActivationDenied: false,
        CloudActivationDenied: false,
        AiActivationDenied: false);
}

/// <summary>
/// Complete typed profile definition consumed by Control Center, Profile Broker, and AIHub.
/// </summary>
public sealed record MonadobladeProfileDefinition(
    MonadobladeProfileId Id,
    string DisplayName,
    string WindowsRole,
    bool Interactive,
    bool IsAdministrator,
    bool Hidden,
    bool EnabledByDefault,
    string Color,
    string Glyph,
    string SoundCue,
    string Background,
    IReadOnlyList<string> SoftwareBundles,
    IReadOnlyList<string> Roots,
    ProfileResourcePolicy ResourcePolicy,
    string NetworkPolicy,
    string AiHubMode,
    IReadOnlyList<string> DashboardWidgets,
    ProfileActivationPolicy ActivationPolicy)
{
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(DisplayName))
        {
            throw new InvalidOperationException("Profile display name is required.");
        }

        if (ResourcePolicy.MemoryReserveGb < 0 ||
            ResourcePolicy.ContainerBudgetGb < 0 ||
            ResourcePolicy.VmBudgetGb < 0 ||
            ResourcePolicy.ModelBudgetGb < 0)
        {
            throw new InvalidOperationException($"Profile {Id} contains a negative resource budget.");
        }

        if (Id == MonadobladeProfileId.SysAdmin)
        {
            if (!IsAdministrator || !Hidden || EnabledByDefault)
            {
                throw new InvalidOperationException("SysAdmin must be the only hidden, disabled-by-default administrator profile.");
            }

            if (!ActivationPolicy.PhysicalPresenceRequired ||
                ActivationPolicy.MinimumFactors < 2 ||
                !ActivationPolicy.RemoteActivationDenied ||
                !ActivationPolicy.CloudActivationDenied ||
                !ActivationPolicy.AiActivationDenied)
            {
                throw new InvalidOperationException("SysAdmin requires two-factor local physical authorization and denies remote, cloud, and AI activation.");
            }
        }
        else if (IsAdministrator)
        {
            throw new InvalidOperationException($"Only SysAdmin may be an administrator profile; {Id} is invalid.");
        }
    }
}

/// <summary>
/// Managed volume information resolved by manifest and unique volume identity rather than a hard-coded drive letter.
/// </summary>
public sealed record ManagedVolumeDefinition(
    string Id,
    string Label,
    string PreferredLetter,
    string FileSystem,
    bool IsDevDrive,
    bool RequiresBitLocker,
    bool AutoMount,
    IReadOnlyList<string> Purposes,
    IReadOnlyList<string> AllowedProfiles);

/// <summary>
/// Chain-of-custody receipt for a CrossPartition transfer.
/// </summary>
public sealed record CrossPartitionReceipt(
    Guid CorrelationId,
    string Source,
    string Destination,
    string OriginalSha256,
    string SanitizedSha256,
    string FileType,
    string SignatureStatus,
    IReadOnlyDictionary<string, string> ScannerVersions,
    IReadOnlyList<string> Findings,
    string SandboxResult,
    string PolicyDecision,
    string ApprovingIdentity,
    DateTimeOffset ApprovedAt,
    string RollbackInstructions)
{
    public void Validate()
    {
        if (CorrelationId == Guid.Empty)
        {
            throw new InvalidOperationException("CrossPartition receipt requires a correlation ID.");
        }

        if (OriginalSha256.Length != 64 || SanitizedSha256.Length != 64)
        {
            throw new InvalidOperationException("CrossPartition receipt requires SHA-256 hashes.");
        }

        if (string.IsNullOrWhiteSpace(RollbackInstructions))
        {
            throw new InvalidOperationException("CrossPartition receipt requires rollback instructions.");
        }
    }
}

/// <summary>
/// Common message envelope for automatic communication across HELIOS services.
/// </summary>
public sealed record HeliosFabricEnvelope<TPayload>(
    string MessageType,
    Version SchemaVersion,
    Guid CorrelationId,
    Guid? CausationId,
    string IdempotencyKey,
    MonadobladeProfileId? ProfileId,
    FabricSecurityClassification SecurityClassification,
    DateTimeOffset CreatedAtUtc,
    string EvidenceReference,
    TPayload Payload)
{
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(MessageType))
        {
            throw new InvalidOperationException("Fabric message type is required.");
        }

        if (CorrelationId == Guid.Empty)
        {
            throw new InvalidOperationException("Fabric correlation ID is required.");
        }

        if (string.IsNullOrWhiteSpace(IdempotencyKey))
        {
            throw new InvalidOperationException("Fabric idempotency key is required.");
        }

        if (CreatedAtUtc.Offset != TimeSpan.Zero)
        {
            throw new InvalidOperationException("Fabric timestamps must be UTC.");
        }
    }
}

/// <summary>
/// Helpers for validating a complete profile catalog.
/// </summary>
public static class MonadobladeProfileCatalog
{
    public static IReadOnlyDictionary<MonadobladeProfileId, MonadobladeProfileDefinition> ValidateAndFreeze(
        IEnumerable<MonadobladeProfileDefinition> profiles)
    {
        ArgumentNullException.ThrowIfNull(profiles);

        var materialized = profiles.ToArray();
        foreach (var profile in materialized)
        {
            profile.Validate();
        }

        var duplicates = materialized
            .GroupBy(profile => profile.Id)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();

        if (duplicates.Length > 0)
        {
            throw new InvalidOperationException($"Duplicate profile IDs: {string.Join(", ", duplicates)}");
        }

        var expected = Enum.GetValues<MonadobladeProfileId>();
        var missing = expected.Except(materialized.Select(profile => profile.Id)).ToArray();
        if (missing.Length > 0)
        {
            throw new InvalidOperationException($"Missing profile definitions: {string.Join(", ", missing)}");
        }

        var administrators = materialized.Where(profile => profile.IsAdministrator).ToArray();
        if (administrators.Length != 1 || administrators[0].Id != MonadobladeProfileId.SysAdmin)
        {
            throw new InvalidOperationException("SysAdmin must be the only administrator profile.");
        }

        return new ReadOnlyDictionary<MonadobladeProfileId, MonadobladeProfileDefinition>(
            materialized.ToDictionary(profile => profile.Id));
    }
}
