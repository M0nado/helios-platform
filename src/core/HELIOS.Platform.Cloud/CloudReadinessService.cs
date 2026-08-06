namespace HELIOS.Platform.Cloud;

public sealed class CloudReadinessService
{
    public CloudReadiness Snapshot() => new(
        HasGitHubToken: !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("HELIOS_AUTOMATION_TOKEN")),
        HasAzureSubscription: !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("AZURE_SUBSCRIPTION_ID")),
        HasOpenAiKey: !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OPENAI_API_KEY")));
}

public sealed record CloudReadiness(bool HasGitHubToken, bool HasAzureSubscription, bool HasOpenAiKey);
