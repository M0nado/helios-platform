using System.Text.Json;
using Helios.Operator;

namespace Helios.Operator.Tests;

public sealed class OperatorPolicyValidatorTests
{
    private const string Manifest = """
      {"version":"1.0","environment":"production","declared":{"githubApps":["approved-app"],"secretReads":["kv/approved"],"oidcSubjects":["repo:M0nado/helios-platform:environment:production"]}}
      """;

    public static TheoryData<string, string> ProhibitedActions => new()
    {
        { "{\"type\":\"azure.rbac.assign\",\"target\":\"11111111-1111-1111-1111-111111111111\"}", "own RBAC" },
        { "{\"type\":\"github.environment.reviewer.add\",\"target\":\"helios-agent\"}", "itself" },
        { "{\"type\":\"github.branch_rule.update\",\"requireStatusChecks\":false}", "weakened" },
        { "{\"type\":\"github.app.use\",\"app\":\"unknown-app\"}", "not declared" },
        { "{\"type\":\"secret.read\",\"secret\":\"kv/unknown\"}", "not declared" },
        { "{\"type\":\"azure.oidc.configure\",\"subject\":\"repo:M0nado/*\"}", "wildcards" },
        { "{\"type\":\"container.deploy\",\"image\":\"ghcr.io/m0nado/helios:latest\"}", "immutable digest" },
        { "{\"type\":\"repository.checkout\",\"ref\":\"main\"}", "commit SHAs" },
        { "{\"type\":\"command.execute\",\"args\":[\"--client-secret=plaintext\"]}", "CLI arguments" },
        { "{\"type\":\"identity.bind\",\"developmentIdentity\":\"shared\",\"productionIdentity\":\"shared\"}", "must not reuse" },
        { "{\"type\":\"approval.grant\",\"approver\":\"agent-1\"}", "own changes" }
    };

    [Theory]
    [MemberData(nameof(ProhibitedActions))]
    public void RejectsEachProhibitedPolicyCondition(string action, string diagnostic)
    {
        using var manifest = JsonDocument.Parse(Manifest);
        using var plan = JsonDocument.Parse(Plan(action));

        var errors = OperatorPolicyValidator.Validate(manifest.RootElement, plan.RootElement);

        Assert.Contains(errors, error => error.Message.Contains(diagnostic, StringComparison.OrdinalIgnoreCase));
        Assert.All(errors, error => Assert.StartsWith("plan/actions/0", error.Path));
    }

    [Theory]
    [InlineData("{\"source\":\"\",\"correlationId\":\"not-a-uuid\",\"evidenceLinks\":[]}", "0.8")]
    [InlineData("{\"source\":\"test\",\"correlationId\":\"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa\",\"evidenceLinks\":[\"https://example.test/evidence\"]}", "1.1")]
    [InlineData("{\"source\":\"test\",\"correlationId\":\"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa\",\"evidenceLinks\":[\"https://example.test/evidence\"]}", "\"high\"")]
    public void SchemaValidationRejectsMalformedProvenanceOrConfidence(string provenance, string confidence)
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var manifestPath = Path.Combine(directory, "manifest.json");
        var planPath = Path.Combine(directory, "plan.json");
        File.WriteAllText(manifestPath, Manifest);
        File.WriteAllText(planPath, Plan("{\"type\":\"noop\"}", provenance, confidence));

        var errors = Program.Validate(manifestPath, planPath);

        Assert.NotEmpty(errors);
        Assert.Contains(errors, error => error.Path.Contains("provenance") || error.Path.Contains("confidence"));
        Directory.Delete(directory, true);
    }

    private static string Plan(string action, string? provenance = null, string confidence = "0.8") => $$"""
      {
        "schemaVersion":"1.0",
        "actor":{"id":"agent-1","githubLogin":"helios-agent","azurePrincipalId":"11111111-1111-1111-1111-111111111111"},
        "actions":[{{action}}],
        "provenance":{{provenance ?? "{\"source\":\"test\",\"correlationId\":\"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa\",\"evidenceLinks\":[\"https://example.test/evidence\"]}"}},
        "confidence":{{confidence}}
      }
      """;
}
