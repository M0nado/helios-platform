using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using HELIOS.AIHub.Abstractions;
using HELIOS.AIHub.Providers;

namespace HELIOS.Platform.Tests.AIHub;

public sealed class LocalAiProviderTests
{
    [Fact]
    public async Task HermesXCoreDefaultPrefersLocalOfflinePlanning()
    {
        var provider = new LocalAiProvider(LocalAiProviderOptions.HermesXCoreDefault);

        var available = await provider.IsAvailableAsync();

        Assert.True(available);
        Assert.Equal(AiProviderKind.LocalLlm, provider.Kind);
        Assert.Contains("aihub-integration", provider.Capabilities);
        Assert.Contains(AiDataClass.Sensitive, provider.AllowedDataClasses);
    }

    [Fact]
    public async Task ExecuteAsyncRejectsSideEffectModes()
    {
        var provider = new LocalAiProvider(LocalAiProviderOptions.HermesXCoreDefault);
        var request = new AiTaskRequest(
            TaskId: "task-1",
            Intent: "merge repositories",
            Prompt: "Cross repository integration should be planned before execution.",
            DataClass: AiDataClass.Internal,
            RequestedMode: AiExecutionMode.Execute,
            Context: new Dictionary<string, string>());

        var response = await provider.ExecuteAsync(request);

        Assert.False(response.Success);
        Assert.Equal("true", response.Metadata["rejected"]);
    }

    [Fact]
    public async Task ExecuteAsyncReturnsLocalOnlyMetadataForDrafts()
    {
        var provider = new LocalAiProvider(LocalAiProviderOptions.HermesXCoreDefault);
        var request = new AiTaskRequest(
            TaskId: "task-2",
            Intent: "optimize AIHub integration",
            Prompt: "Prepare a local AI integration plan.",
            DataClass: AiDataClass.Sensitive,
            RequestedMode: AiExecutionMode.Draft,
            Context: new Dictionary<string, string>
            {
                ["repository"] = "helios-platform",
                ["adjacentSystems"] = "helios-control,hermes-fleet-production"
            });

        var response = await provider.ExecuteAsync(request);

        Assert.True(response.Success);
        Assert.Equal("local-hermes-xcore", response.ProviderId);
        Assert.Equal("true", response.Metadata["localOnly"]);
        Assert.Contains("Local AI guardrails", response.Content);
    }
}
