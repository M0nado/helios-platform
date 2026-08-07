using HELIOS.Platform.Contracts.Monadoblade;

namespace HELIOS.Platform.Tests.Contracts;

public sealed class MonadoEnterpriseExperienceFabricV2ContractTests
{
    [Fact]
    public void ValidateAndFreezeBudgets_RequiresAllProfiles()
    {
        var budgets = BuildBudgets().Where(value => value.ProfileId != MonadoEnterpriseProfileId.Studio);

        var exception = Assert.Throws<InvalidOperationException>(
            () => MonadoEnterpriseExperienceFabricV2Catalog.ValidateAndFreezeBudgets(budgets));
        Assert.Contains("Missing ALVIS profile budgets", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ValidateAndFreezeBudgets_RequiresSysAdminApprovalBoundary()
    {
        var budgets = BuildBudgets().Select(
            value => value.ProfileId == MonadoEnterpriseProfileId.SysAdmin
                ? value with { ApplyDeniedWithoutExplicitApproval = false }
                : value);

        var exception = Assert.Throws<InvalidOperationException>(
            () => MonadoEnterpriseExperienceFabricV2Catalog.ValidateAndFreezeBudgets(budgets));
        Assert.Contains("explicit-approval only", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void MonadoOpenAiProposal_RequiresUtcExpiryAndEvidence()
    {
        var proposal = new MonadoOpenAiProposal(
            ProposalId: "proposal-206",
            ProfileId: MonadoEnterpriseProfileId.Developer,
            CorrelationId: "corr-206",
            RiskLevel: "medium",
            ActionType: "proposal",
            Summary: "Bounded proposal with no runtime side effects.",
            ExpiresAtUtc: DateTimeOffset.Now,
            EvidenceLinks: Array.Empty<Uri>(),
            RollbackStrategy: "No apply was performed.",
            ApprovalRequired: true);

        var exception = Assert.Throws<InvalidOperationException>(() => proposal.Validate());
        Assert.Contains("At least one evidence link is required", exception.Message, StringComparison.Ordinal);
    }

    private static IReadOnlyList<AlvisProfileToolBudget> BuildBudgets() =>
        new[]
        {
            new AlvisProfileToolBudget(MonadoEnterpriseProfileId.Core, 20, false, false),
            new AlvisProfileToolBudget(MonadoEnterpriseProfileId.Developer, 120, false, false),
            new AlvisProfileToolBudget(MonadoEnterpriseProfileId.Gamer, 25, false, false),
            new AlvisProfileToolBudget(MonadoEnterpriseProfileId.Studio, 40, false, false),
            new AlvisProfileToolBudget(MonadoEnterpriseProfileId.Personal, 20, false, false),
            new AlvisProfileToolBudget(MonadoEnterpriseProfileId.SysOps, 80, true, false),
            new AlvisProfileToolBudget(MonadoEnterpriseProfileId.AiServer, 100, true, false),
            new AlvisProfileToolBudget(MonadoEnterpriseProfileId.SysAdmin, 30, true, true),
        };
}
