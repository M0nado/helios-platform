using HELIOS.AIHub.Abstractions;

namespace HELIOS.AIHub.Routing;

public sealed class SecurityFirstAgentRouter : IAgentRouter
{
    public Task<IAiProvider?> SelectProviderAsync(AiTaskRequest request, IEnumerable<IAiProvider> providers)
    {
        if (request.DataClass == AiDataClass.Secret)
        {
            return Task.FromResult<IAiProvider?>(null);
        }

        if (request.DataClass == AiDataClass.Dangerous && request.RequestedMode is AiExecutionMode.Execute or AiExecutionMode.ElevatedExecute)
        {
            return Task.FromResult<IAiProvider?>(null);
        }

        var allowed = providers
            .Where(provider => provider.AllowedDataClasses.Contains(request.DataClass))
            .OrderBy(provider => provider.Kind == AiProviderKind.LocalLlm ? 0 : 1)
            .FirstOrDefault();

        return Task.FromResult(allowed);
    }

    public Task<IHermesAgent?> SelectAgentAsync(AiTaskRequest request, IEnumerable<IHermesAgent> agents)
    {
        var selected = agents.FirstOrDefault(agent => agent.AllowedModes.Contains(request.RequestedMode));
        return Task.FromResult(selected);
    }
}
