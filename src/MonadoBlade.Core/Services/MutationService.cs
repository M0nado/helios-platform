namespace MonadoBlade.Core.Services;

public class MutationService : ServiceBase
{
    public MutationService(ILogger logger) : base(logger) { }
    
    public async Task<T> CreateAsync<T>(T entity, CancellationToken cancellationToken = default) where T : class
    {
        LogInfo("Creating entity");
        await Task.Delay(10, cancellationToken);
        return entity;
    }

    public async Task<T> UpdateAsync<T>(string id, T entity, CancellationToken cancellationToken = default) where T : class
    {
        LogInfo("Updating entity");
        await Task.Delay(10, cancellationToken);
        return entity;
    }

    public async Task DeleteAsync<T>(string id, CancellationToken cancellationToken = default) where T : class
    {
        LogInfo("Deleting entity");
        await Task.Delay(5, cancellationToken);
    }
}
