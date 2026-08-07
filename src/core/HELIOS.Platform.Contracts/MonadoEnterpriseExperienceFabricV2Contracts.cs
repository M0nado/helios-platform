using System.Collections.ObjectModel;

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
/// Strict OpenAI proposal shape carried through approval gates.
/// </summary>
public sealed record MonadoOpenAiProposal(
    string ProposalId,
    MonadoEnterpriseProfileId ProfileId,
    string CorrelationId,
    string RiskLevel,
    string ActionType,
    string Summary,
    DateTimeOffset ExpiresAtUtc,
    IReadOnlyList<Uri> EvidenceLinks,
    string RollbackStrategy,
    bool ApprovalRequired)
{
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ProposalId) || ProposalId.Length < 8)
        {
            throw new InvalidOperationException("Proposal ID is required.");
        }

        if (string.IsNullOrWhiteSpace(CorrelationId))
        {
            throw new InvalidOperationException("Correlation ID is required.");
        }

        if (string.IsNullOrWhiteSpace(Summary))
        {
            throw new InvalidOperationException("Proposal summary is required.");
        }

        if (EvidenceLinks is null || EvidenceLinks.Count == 0)
        {
            throw new InvalidOperationException("At least one evidence link is required.");
        }

        if (ExpiresAtUtc.Offset != TimeSpan.Zero)
        {
            throw new InvalidOperationException("Proposal expiry must be expressed in UTC.");
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
