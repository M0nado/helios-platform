using HELIOS.Platform.Contracts.Monadoblade;
using Xunit;

namespace HELIOS.Platform.Contracts.Tests;

public sealed class MonadobladeProfileContractsTests
{
    [Fact]
    public void SysAdminValidationRejectsWeakActivationPolicy()
    {
        var profile = CreateProfile(
            MonadobladeProfileId.SysAdmin,
            isAdministrator: true,
            hidden: true,
            enabledByDefault: false,
            activationPolicy: new ProfileActivationPolicy(
                PhysicalPresenceRequired: true,
                MinimumFactors: 1,
                AllowedFactors: ["hardware-key"],
                RemoteActivationDenied: true,
                CloudActivationDenied: true,
                AiActivationDenied: true));

        var exception = Assert.Throws<InvalidOperationException>(() => profile.Validate());
        Assert.Contains("SysAdmin requires two-factor local physical authorization", exception.Message);
    }

    [Fact]
    public void NonSysAdminAdministratorIsRejected()
    {
        var profile = CreateProfile(
            MonadobladeProfileId.Developer,
            isAdministrator: true,
            hidden: false,
            enabledByDefault: true,
            activationPolicy: ProfileActivationPolicy.StandardUser);

        var exception = Assert.Throws<InvalidOperationException>(() => profile.Validate());
        Assert.Contains("Only SysAdmin may be an administrator profile", exception.Message);
    }

    [Fact]
    public void ValidateAndFreezeRejectsDuplicateProfileIds()
    {
        var duplicateProfiles = new[]
        {
            CreateProfile(MonadobladeProfileId.Developer, false, false, true, ProfileActivationPolicy.StandardUser),
            CreateProfile(MonadobladeProfileId.Developer, false, false, true, ProfileActivationPolicy.StandardUser)
        };

        var exception = Assert.Throws<InvalidOperationException>(() => MonadobladeProfileCatalog.ValidateAndFreeze(duplicateProfiles));
        Assert.Contains("Duplicate profile IDs", exception.Message);
    }

    [Fact]
    public void HeliosFabricEnvelopeRejectsNonUtcTimestamp()
    {
        var envelope = new HeliosFabricEnvelope<string>(
            MessageType: "profile.changed",
            SchemaVersion: new Version(2, 0),
            CorrelationId: Guid.NewGuid(),
            CausationId: Guid.NewGuid(),
            IdempotencyKey: "idempotent-key",
            ProfileId: MonadobladeProfileId.Developer,
            SecurityClassification: FabricSecurityClassification.Internal,
            CreatedAtUtc: new DateTimeOffset(2026, 8, 6, 12, 0, 0, TimeSpan.FromHours(-4)),
            EvidenceReference: "evidence://run-1",
            Payload: "ok");

        var exception = Assert.Throws<InvalidOperationException>(() => envelope.Validate());
        Assert.Contains("Fabric timestamps must be UTC", exception.Message);
    }

    [Fact]
    public void CrossPartitionReceiptRejectsInvalidHashLength()
    {
        var receipt = new CrossPartitionReceipt(
            CorrelationId: Guid.NewGuid(),
            Source: "domains/games",
            Destination: "domains/personal",
            OriginalSha256: "abc",
            SanitizedSha256: "abc",
            FileType: "zip",
            SignatureStatus: "signed",
            ScannerVersions: new Dictionary<string, string> { ["av"] = "1.0.0" },
            Findings: [],
            SandboxResult: "clean",
            PolicyDecision: "allow",
            ApprovingIdentity: "ops@local",
            ApprovedAt: DateTimeOffset.UtcNow,
            RollbackInstructions: "Restore from immutable snapshot");

        var exception = Assert.Throws<InvalidOperationException>(() => receipt.Validate());
        Assert.Contains("requires SHA-256 hashes", exception.Message);
    }

    private static MonadobladeProfileDefinition CreateProfile(
        MonadobladeProfileId id,
        bool isAdministrator,
        bool hidden,
        bool enabledByDefault,
        ProfileActivationPolicy activationPolicy)
        => new(
            Id: id,
            DisplayName: id.ToString(),
            WindowsRole: "StandardUser",
            Interactive: true,
            IsAdministrator: isAdministrator,
            Hidden: hidden,
            EnabledByDefault: enabledByDefault,
            Color: "#7A4CFF",
            Glyph: "profile",
            SoundCue: "neutral",
            Background: "default",
            SoftwareBundles: ["core"],
            Roots: ["Domains/common"],
            ResourcePolicy: new ProfileResourcePolicy(
                CpuMode: "balanced",
                GpuMode: "balanced",
                MemoryReserveGb: 4,
                ContainerBudgetGb: 8,
                VmBudgetGb: 8,
                ModelBudgetGb: 8,
                BackgroundPriority: "normal"),
            NetworkPolicy: "governed",
            AiHubMode: "bounded",
            DashboardWidgets: ["health"],
            ActivationPolicy: activationPolicy);
}
