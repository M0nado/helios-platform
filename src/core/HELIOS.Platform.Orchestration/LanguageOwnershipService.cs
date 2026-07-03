using System.Text.Json;

namespace HELIOS.Platform.Orchestration;

public sealed class LanguageOwnershipService
{
    public async Task<IReadOnlyDictionary<string, LanguageOwnership>> LoadAsync(string path, CancellationToken cancellationToken = default)
    {
        await using var stream = File.OpenRead(path);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        return document.RootElement.GetProperty("languages").EnumerateObject()
            .ToDictionary(item => item.Name, item => new LanguageOwnership(
                item.Name,
                item.Value.GetProperty("role").GetString() ?? string.Empty,
                item.Value.GetProperty("ownerAgent").GetString() ?? string.Empty,
                item.Value.GetProperty("mergeWeight").GetInt32(),
                item.Value.GetProperty("ownedPaths").EnumerateArray().Select(x => x.GetString() ?? string.Empty).ToArray(),
                item.Value.GetProperty("requiredChecks").EnumerateArray().Select(x => x.GetString() ?? string.Empty).ToArray()));
    }
}

public sealed record LanguageOwnership(string Language, string Role, string OwnerAgent, int MergeWeight, IReadOnlyList<string> OwnedPaths, IReadOnlyList<string> RequiredChecks);
