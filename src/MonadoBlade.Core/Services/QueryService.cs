namespace MonadoBlade.Core.Services;

public class QueryService : ServiceBase
{
    public QueryService(ILogger logger) : base(logger) { }

    public async Task<T?> GetByIdAsync<T>(string id, CancellationToken cancellationToken = default) where T : class
    {
        LogInfo("Retrieving entity with ID {EntityId}", id);
        await Task.Delay(5, cancellationToken);
        return null;
    }

    public async Task<List<T>> QueryAsync<T>(
        IQueryable<T> query,
        string? cacheKey = null,
        TimeSpan? cacheDuration = null,
        CancellationToken cancellationToken = default) where T : class
    {
        LogInfo("Executing query");
        await Task.Delay(5, cancellationToken);
        return new();
    }

    public async Task<T?> FirstOrDefaultAsync<T>(
        IQueryable<T> query,
        CancellationToken cancellationToken = default) where T : class
    {
        LogInfo("Retrieving first entity");
        await Task.Delay(5, cancellationToken);
        return null;
    }
}
