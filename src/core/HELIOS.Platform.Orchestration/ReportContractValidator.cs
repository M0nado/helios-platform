using System.Text.Json;

namespace HELIOS.Platform.Orchestration;

public sealed class ReportContractValidator
{
    public IReadOnlyList<string> ValidateFinalGate(JsonElement root)
    {
        var errors = new List<string>();
        Require(root, "status", errors);
        Require(root, "mergeReady", errors);
        Require(root, "results", errors);
        if (root.TryGetProperty("firstBlocker", out var blocker) && blocker.ValueKind is not JsonValueKind.Null)
        {
            Require(blocker, "id", errors);
            Require(blocker, "language", errors);
            Require(blocker, "ownerAgent", errors);
            Require(blocker, "recommendedFixer", errors);
        }
        return errors;
    }

    private static void Require(JsonElement element, string property, ICollection<string> errors)
    {
        if (!element.TryGetProperty(property, out _)) errors.Add($"Missing required property: {property}");
    }
}
