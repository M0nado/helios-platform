using System.Collections.ObjectModel;

namespace HELIOS.Platform.Contracts.Monadoblade;

/// <summary>
/// The closed set of identities shown on the Monadoblade wheel.
/// Legacy v1 profiles remain supported as migration inputs but cannot extend this enum.
/// </summary>
public enum MonadobladeIdentityId
{
    Core,
    Developer,
    Studio,
    Gamer,
    AiServer,
    SysAdmin
}

/// <summary>
/// Optional capability bundles that alter an identity without becoming login identities.
/// </summary>
public enum MonadobladeCapabilityOverlayId
{
    Personal,
    SysOps
}

/// <summary>
/// Governed operating states. Recovery and Quarantine are deliberately not identities.
/// </summary>
public enum MonadobladeWorkflowStateId
{
    Standard,
    AirGap,
    Recovery,
    Quarantine
}

/// <summary>
/// Consequence class for an ALVIS tool exposed through MCP or the local command surface.
/// </summary>
public enum AlvisToolEffect
{
    ReadOnly,
    PlanOnly,
    ApprovalRequest
}

/// <summary>
/// Visual and audible identity energy. Color is always paired with Kanji and audio/shape cues.
/// </summary>
public sealed record MonadobladeEnergySignature(
    string Kanji,
    string Color,
    string AudioCue)
{
    public void Validate(MonadobladeIdentityId identity)
    {
        if (string.IsNullOrWhiteSpace(Kanji))
        {
            throw new InvalidOperationException($"Identity {identity} requires a Kanji.");
        }

        if (Color.Length != 7 || Color[0] != '#' || !Color[1..].All(Uri.IsHexDigit))
        {
            throw new InvalidOperationException($"Identity {identity} requires a #RRGGBB color.");
        }

        if (string.IsNullOrWhiteSpace(AudioCue))
        {
            throw new InvalidOperationException($"Identity {identity} requires an accessible audio cue name.");
        }
    }
}

/// <summary>
/// One permanent v2 identity consumed by the platform, GUI framework, renderer, and USB preview.
/// </summary>
public sealed record MonadobladeIdentityDefinition(
    MonadobladeIdentityId Id,
    string DisplayName,
    MonadobladeEnergySignature Energy,
    string BackgroundPreset,
    string UiGrammar,
    string WindowsRole,
    bool Interactive,
    bool IsAdministrator,
    bool Hidden,
    bool EnabledByDefault,
    string NetworkMode,
    string AlvisMode,
    IReadOnlyList<MonadobladeCapabilityOverlayId> AllowedOverlays,
    IReadOnlyList<string> ResourceIntent,
    ProfileActivationPolicy ActivationPolicy)
{
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(DisplayName) ||
            string.IsNullOrWhiteSpace(BackgroundPreset) ||
            string.IsNullOrWhiteSpace(UiGrammar) ||
            string.IsNullOrWhiteSpace(WindowsRole) ||
            string.IsNullOrWhiteSpace(NetworkMode) ||
            string.IsNullOrWhiteSpace(AlvisMode))
        {
            throw new InvalidOperationException($"Identity {Id} contains a blank required field.");
        }

        Energy.Validate(Id);

        if (!Interactive)
        {
            throw new InvalidOperationException($"Permanent identity {Id} must be interactive.");
        }

        if (Id == MonadobladeIdentityId.SysAdmin)
        {
            ActivationPolicy.ValidateLocalAdministratorFactors();

            if (!IsAdministrator || !Hidden || EnabledByDefault)
            {
                throw new InvalidOperationException("SysAdmin must be hidden, disabled by default, and the only administrator identity.");
            }

            if (!string.Equals(NetworkMode, "offline-local-only", StringComparison.Ordinal) ||
                !ActivationPolicy.PhysicalPresenceRequired ||
                ActivationPolicy.MinimumFactors < 2 ||
                !ActivationPolicy.RemoteActivationDenied ||
                !ActivationPolicy.CloudActivationDenied ||
                !ActivationPolicy.AiActivationDenied)
            {
                throw new InvalidOperationException("SysAdmin requires offline, local, two-factor physical authorization and denies remote, cloud, and AI activation.");
            }
        }
        else
        {
            if (IsAdministrator)
            {
                throw new InvalidOperationException($"Only SysAdmin may be an administrator; {Id} is invalid.");
            }

            if (Hidden || !EnabledByDefault)
            {
                throw new InvalidOperationException($"Non-admin identity {Id} must be visible and enabled by default.");
            }
        }
    }
}

/// <summary>
/// A capability overlay changes tools and layout without adding a profile-wheel sector.
/// </summary>
public sealed record MonadobladeCapabilityOverlay(
    MonadobladeCapabilityOverlayId Id,
    string DisplayName,
    bool BecomesIdentity,
    IReadOnlyList<MonadobladeIdentityId> AllowedIdentities,
    IReadOnlyList<string> Capabilities,
    bool WriteActionsRequireApproval)
{
    public void Validate()
    {
        if (BecomesIdentity)
        {
            throw new InvalidOperationException($"Overlay {Id} cannot become a login identity.");
        }

        if (AllowedIdentities.Count == 0 || Capabilities.Count == 0)
        {
            throw new InvalidOperationException($"Overlay {Id} requires identities and bounded capabilities.");
        }

        if (AllowedIdentities.Contains(MonadobladeIdentityId.SysAdmin))
        {
            throw new InvalidOperationException($"Overlay {Id} cannot alter the sealed SysAdmin identity.");
        }
    }
}

/// <summary>
/// A local workflow that may be requested by an identity but is activated through policy.
/// </summary>
public sealed record MonadobladeWorkflowState(
    MonadobladeWorkflowStateId Id,
    bool SelectableOnIdentityWheel,
    bool RequiresSysAdmin,
    string Purpose)
{
    public void Validate()
    {
        if (SelectableOnIdentityWheel)
        {
            throw new InvalidOperationException($"Workflow {Id} cannot appear on the identity wheel.");
        }

        if (string.IsNullOrWhiteSpace(Purpose))
        {
            throw new InvalidOperationException($"Workflow {Id} requires a purpose.");
        }

        if (Id != MonadobladeWorkflowStateId.Standard && !RequiresSysAdmin)
        {
            throw new InvalidOperationException($"Workflow {Id} requires SysAdmin authorization.");
        }
    }
}

/// <summary>
/// One ALVIS tool registration. ALVIS can read, plan, or submit an approval request; it cannot execute directly.
/// </summary>
public sealed record AlvisToolPolicy(
    string ToolName,
    AlvisToolEffect Effect,
    bool RequiresHumanApproval)
{
    public void Validate()
    {
        var expectedPrefix = Effect switch
        {
            AlvisToolEffect.ReadOnly => new[] { "search_", "fetch_" },
            AlvisToolEffect.PlanOnly => new[] { "plan_" },
            AlvisToolEffect.ApprovalRequest => new[] { "request_" },
            _ => Array.Empty<string>()
        };

        if (!expectedPrefix.Any(prefix => ToolName.StartsWith(prefix, StringComparison.Ordinal)))
        {
            throw new InvalidOperationException($"ALVIS tool {ToolName} does not match effect {Effect}.");
        }

        if (Effect == AlvisToolEffect.ApprovalRequest && !RequiresHumanApproval)
        {
            throw new InvalidOperationException($"ALVIS request tool {ToolName} requires human approval.");
        }
    }
}

/// <summary>
/// Validation helpers for the complete v2 delivery-fabric catalog.
/// </summary>
public static class MonadobladeDeliveryFabricCatalog
{
    private static (string Kanji, string Color, string AudioCue) CanonicalEnergyFor(MonadobladeIdentityId identity) =>
        identity switch
        {
            MonadobladeIdentityId.Core => ("核", "#53F6FF", "wyvern-core-harmonic"),
            MonadobladeIdentityId.Developer => ("創", "#2F86FF", "wyvern-developer-code-rise"),
            MonadobladeIdentityId.Studio => ("響", "#D64DFF", "wyvern-studio-harmonic-bloom"),
            MonadobladeIdentityId.Gamer => ("迅", "#62FF4A", "wyvern-gamer-velocity-lock"),
            MonadobladeIdentityId.AiServer => ("智", "#7A6BFF", "wyvern-ai-topology-pulse"),
            MonadobladeIdentityId.SysAdmin => ("統", "#FFB000", "wyvern-sysadmin-mechanical-lock"),
            _ => throw new ArgumentOutOfRangeException(nameof(identity), identity, "Unknown Monadoblade identity.")
        };

    public static IReadOnlyDictionary<MonadobladeIdentityId, MonadobladeIdentityDefinition> ValidateIdentities(
        IEnumerable<MonadobladeIdentityDefinition> identities)
    {
        ArgumentNullException.ThrowIfNull(identities);

        var materialized = identities.ToArray();
        foreach (var identity in materialized)
        {
            identity.Validate();
            var canonical = CanonicalEnergyFor(identity.Id);
            if (!string.Equals(identity.Energy.Kanji, canonical.Kanji, StringComparison.Ordinal) ||
                !string.Equals(identity.Energy.Color, canonical.Color, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(identity.Energy.AudioCue, canonical.AudioCue, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Identity {identity.Id} requires canonical energy {canonical.Kanji}/{canonical.Color}/{canonical.AudioCue}.");
            }
        }

        var duplicates = materialized
            .GroupBy(identity => identity.Id)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();
        if (duplicates.Length > 0)
        {
            throw new InvalidOperationException($"Duplicate identity IDs: {string.Join(", ", duplicates)}");
        }

        var expected = Enum.GetValues<MonadobladeIdentityId>();
        var missing = expected.Except(materialized.Select(identity => identity.Id)).ToArray();
        if (missing.Length > 0 || materialized.Length != expected.Length)
        {
            throw new InvalidOperationException($"The identity wheel must contain exactly six canonical identities; missing: {string.Join(", ", missing)}");
        }

        var duplicateKanji = materialized
            .GroupBy(identity => identity.Energy.Kanji, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();
        var duplicateColors = materialized
            .GroupBy(identity => identity.Energy.Color, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();
        if (duplicateKanji.Length > 0 || duplicateColors.Length > 0)
        {
            throw new InvalidOperationException("Each permanent identity requires a unique Kanji and color.");
        }

        var administrators = materialized.Where(identity => identity.IsAdministrator).ToArray();
        if (administrators.Length != 1 || administrators[0].Id != MonadobladeIdentityId.SysAdmin)
        {
            throw new InvalidOperationException("SysAdmin must be the only administrator identity.");
        }

        return new ReadOnlyDictionary<MonadobladeIdentityId, MonadobladeIdentityDefinition>(
            materialized.ToDictionary(identity => identity.Id));
    }

    public static void ValidateAlvisTools(IEnumerable<AlvisToolPolicy> tools)
    {
        ArgumentNullException.ThrowIfNull(tools);
        foreach (var tool in tools)
        {
            tool.Validate();
        }
    }
}
