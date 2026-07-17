using Helios.Connect.Api;
using Xunit;

namespace Helios.Connect.Tests;

public sealed class SetupWizardTests
{
    private readonly SetupWizardService _wizard = new();

    [Fact]
    public void Bootstrap_is_deterministic_plan_only_and_contains_no_secret()
    {
        var request = new SetupBootstrapRequest("11111111-1111-1111-1111-111111111111", "22222222-2222-2222-2222-222222222222", "helios-dev-rg", "dev");
        var first = _wizard.CreateBootstrap(request);
        var second = _wizard.CreateBootstrap(request);
        Assert.Equal(first.ScriptSha256, second.ScriptSha256);
        Assert.False(first.ContainsSecrets);
        Assert.False(first.AppliesChanges);
        Assert.Contains("-Mode Diagnose", first.Script);
        Assert.Contains("-Mode Plan", first.Script);
        Assert.DoesNotContain("-Mode Apply", first.Script);
        Assert.DoesNotContain("keyvault secret", first.Script, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("group'; az group delete --name bad; '")]
    [InlineData("group\nmalicious")]
    public void Bootstrap_rejects_shell_injection(string group)
    {
        var request = new SetupBootstrapRequest("11111111-1111-1111-1111-111111111111", "22222222-2222-2222-2222-222222222222", group, "dev");
        Assert.Throws<ArgumentException>(() => _wizard.CreateBootstrap(request));
    }

    [Fact]
    public void Bootstrap_can_select_subscription_and_persist_multi_shell_evidence()
    {
        var request = new SetupBootstrapRequest("11111111-1111-1111-1111-111111111111", "", "helios-dev-rg", "dev");
        var result = _wizard.CreateBootstrap(request);

        Assert.Equal("unique-resource-group-match-then-interactive-fallback", result.SubscriptionSelection);
        Assert.Contains("az account list --all", result.Script);
        Assert.Contains("az group exists", result.Script);
        Assert.Contains("clouddrive/helios-evidence", result.Script);
        Assert.Equal(4, result.ShellPackets.Count);
        Assert.False(result.AppliesChanges);
    }

    [Fact]
    public void Cleanup_plan_protects_unknown_and_shared_resources()
    {
        var plan = new EdgeAutomationPlanner().CreatePlan(new("cleanup-owned-resources", "dev", "helios-dev-rg"));

        Assert.False(plan.CanApplyFromMcp);
        Assert.Equal("plan-only", plan.Mode);
        Assert.Contains(plan.Steps, step => step.Gate == "unknown-or-shared-resources-protected");
        Assert.Contains(plan.Steps, step => step.Mutating && step.Executor == "protected-workflow");
    }

    [Fact]
    public void Upgrade_is_deterministic_and_requires_draft_pr_promotion()
    {
        var request = new UpgradeProposalRequest("dns/private-zone-planner", "Add a reviewed private DNS planning module.");
        var proposal = _wizard.CreateUpgradeProposal(request);
        Assert.False(proposal.AutomaticApply);
        Assert.False(proposal.AutomaticMerge);
        Assert.Equal("task-branch-and-draft-pull-request", proposal.Promotion);
        Assert.Contains("protected-review", proposal.RequiredChecks);
        Assert.Equal(proposal.ProposalId, _wizard.CreateUpgradeProposal(request).ProposalId);
    }
}
