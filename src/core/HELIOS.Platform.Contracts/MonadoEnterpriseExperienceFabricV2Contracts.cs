using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;

namespace HELIOS.Platform.Contracts.Monadoblade;

/// <summary>
/// Permanent profile catalog for Monado enterprise experience fabric v2.
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
    SysAdmin,
}

/// <summary>
/// ALVIS tool budget guardrails by profile.
/// </summary>
public sealed record AlvisProfileToolBudget(
    MonadoEnterpriseProfileId ProfileId,
    int MaxToolCallsPerPlan,
    bool AllowPrivilegedProposal,
    bool ApplyDeniedWithoutExplicitApproval)
{
    public void Validate()
    {
        if (MaxToolCallsPerPlan < 1)
        {
            throw new InvalidOperationException($"Profile {ProfileId} has an invalid ALVIS tool budget.");
        }
    }
}

/// <summary>
/// Proposal requester boundary used for typed proposal contracts.
/// </summary>
public sealed record MonadoProposalRequester(
    string Type,
    string Id)
{
    private static readonly HashSet<string> AllowedTypes = new(StringComparer.Ordinal)
    {
        "human",
        "agent",
        "workflow",
    };

    public void Validate()
    {
        if (!AllowedTypes.Contains(Type))
        {
            throw new InvalidOperationException("Proposal requester type must be human, agent, or workflow.");
        }

        if (string.IsNullOrWhiteSpace(Id) || Id.Length < 2)
        {
            throw new InvalidOperationException("Proposal requester id is required.");
        }
    }
}

/// <summary>
/// Evidence reference used by typed proposal contracts.
/// </summary>
public sealed record MonadoEvidenceLink(
    string Rel,
    Uri Href)
{
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Rel) || Rel.Length < 2)
        {
            throw new InvalidOperationException("Evidence relation is required.");
        }

        if (!Href.IsAbsoluteUri)
        {
            throw new InvalidOperationException("Evidence href must be an absolute URI.");
        }
    }
}

/// <summary>
/// Rollback plan carried with proposal contracts.
/// </summary>
public sealed record MonadoRollbackPlan(
    string Strategy,
    IReadOnlyList<string> Steps)
{
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Strategy) || Strategy.Length < 4)
        {
            throw new InvalidOperationException("Rollback strategy is required.");
        }

        if (Steps is null || Steps.Count == 0)
        {
            throw new InvalidOperationException("At least one rollback step is required.");
        }

        if (Steps.Any(step => string.IsNullOrWhiteSpace(step) || step.Length < 4))
        {
            throw new InvalidOperationException("Rollback steps must be non-empty strings.");
        }
    }
}

/// <summary>
/// Approval gate carried with proposal contracts.
/// </summary>
public sealed record MonadoApprovalGate(
    bool Required,
    string Status,
    string? Reference)
{
    private static readonly HashSet<string> AllowedStatuses = new(StringComparer.Ordinal)
    {
        "pending",
        "approved",
        "rejected",
        "expired",
    };

    public void Validate()
    {
        if (!AllowedStatuses.Contains(Status))
        {
            throw new InvalidOperationException("Approval status must be pending, approved, rejected, or expired.");
        }
    }
}

/// <summary>
/// Strict OpenAI proposal shape carried through approval gates.
/// </summary>
public sealed record MonadoOpenAiProposal(
    string SchemaVersion,
    string ProposalId,
    MonadoEnterpriseProfileId ProfileId,
    string CorrelationId,
    MonadoProposalRequester RequestedBy,
    string RiskLevel,
    string ActionType,
    string Summary,
    DateTimeOffset ExpiresAtUtc,
    IReadOnlyList<MonadoEvidenceLink> EvidenceLinks,
    MonadoRollbackPlan RollbackPlan,
    MonadoApprovalGate Approval)
{
    private static readonly HashSet<string> AllowedRiskLevels = new(StringComparer.Ordinal)
    {
        "low",
        "medium",
        "high",
        "critical",
    };

    private static readonly HashSet<string> AllowedActionTypes = new(StringComparer.Ordinal)
    {
        "read",
        "analysis",
        "proposal",
        "privileged-proposal",
        "deployment-proposal",
        "rollback-proposal",
    };

    private static readonly HashSet<string> PrivilegedActionTypes = new(StringComparer.Ordinal)
    {
        "privileged-proposal",
        "deployment-proposal",
        "rollback-proposal",
    };

    public void Validate(DateTimeOffset? utcNow = null)
    {
        if (!string.Equals(SchemaVersion, "2.0.0", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("SchemaVersion must be 2.0.0.");
        }

        if (string.IsNullOrWhiteSpace(ProposalId) || ProposalId.Length < 8)
        {
            throw new InvalidOperationException("Proposal ID is required.");
        }

        if (string.IsNullOrWhiteSpace(CorrelationId) || CorrelationId.Length < 4)
        {
            throw new InvalidOperationException("Correlation ID is required.");
        }

        RequestedBy.Validate();

        if (!AllowedRiskLevels.Contains(RiskLevel))
        {
            throw new InvalidOperationException("Risk level must be low, medium, high, or critical.");
        }

        if (!AllowedActionTypes.Contains(ActionType))
        {
            throw new InvalidOperationException("Action type is not supported.");
        }

        if (string.IsNullOrWhiteSpace(Summary) || Summary.Length < 10 || Summary.Length > 2000)
        {
            throw new InvalidOperationException("Proposal summary is required.");
        }

        if (EvidenceLinks is null || EvidenceLinks.Count == 0)
        {
            throw new InvalidOperationException("At least one evidence link is required.");
        }

        if (EvidenceLinks.Any(link => link is null))
        {
            throw new InvalidOperationException("Evidence links cannot contain null entries.");
        }

        foreach (var link in EvidenceLinks)
        {
            link.Validate();
        }

        RollbackPlan.Validate();
        Approval.Validate();

        if (ExpiresAtUtc.Offset != TimeSpan.Zero)
        {
            throw new InvalidOperationException("Proposal expiry must be expressed in UTC.");
        }

        var now = (utcNow ?? DateTimeOffset.UtcNow).ToOffset(TimeSpan.Zero);
        if (ExpiresAtUtc <= now)
        {
            throw new InvalidOperationException("Proposal expiry must be a future UTC instant.");
        }

        if (PrivilegedActionTypes.Contains(ActionType) && !Approval.Required)
        {
            throw new InvalidOperationException("Privileged proposal action types require approval.required=true.");
        }
    }
}

/// <summary>
/// Contract helpers for validating canonical v2 boundaries.
/// </summary>
public static class MonadoEnterpriseExperienceFabricV2Catalog
{
    private static readonly MonadoEnterpriseProfileId[] ExpectedProfiles =
    {
        MonadoEnterpriseProfileId.Core,
        MonadoEnterpriseProfileId.Developer,
        MonadoEnterpriseProfileId.Gamer,
        MonadoEnterpriseProfileId.Studio,
        MonadoEnterpriseProfileId.Personal,
        MonadoEnterpriseProfileId.SysOps,
        MonadoEnterpriseProfileId.AiServer,
        MonadoEnterpriseProfileId.SysAdmin,
    };

    public static IReadOnlyDictionary<MonadoEnterpriseProfileId, AlvisProfileToolBudget> ValidateAndFreezeBudgets(
        IEnumerable<AlvisProfileToolBudget> budgets)
    {
        ArgumentNullException.ThrowIfNull(budgets);

        var materialized = budgets.ToArray();
        foreach (var budget in materialized)
        {
            budget.Validate();
        }

        var duplicateProfiles = materialized
            .GroupBy(value => value.ProfileId)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();
        if (duplicateProfiles.Length > 0)
        {
            throw new InvalidOperationException($"Duplicate ALVIS profile budgets: {string.Join(", ", duplicateProfiles)}");
        }

        var missing = ExpectedProfiles.Except(materialized.Select(value => value.ProfileId)).ToArray();
        if (missing.Length > 0)
        {
            throw new InvalidOperationException($"Missing ALVIS profile budgets: {string.Join(", ", missing)}");
        }

        var sysAdmin = materialized.Single(value => value.ProfileId == MonadoEnterpriseProfileId.SysAdmin);
        if (!sysAdmin.ApplyDeniedWithoutExplicitApproval || !sysAdmin.AllowPrivilegedProposal)
        {
            throw new InvalidOperationException("SysAdmin ALVIS budget must remain explicit-approval only.");
        }

        return new ReadOnlyDictionary<MonadoEnterpriseProfileId, AlvisProfileToolBudget>(
            materialized.ToDictionary(value => value.ProfileId));
    }
}
