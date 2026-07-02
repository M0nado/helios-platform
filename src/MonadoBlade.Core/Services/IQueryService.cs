namespace MonadoBlade.Core.Services;

/// <summary>
/// Query service providing read-only operations with caching-friendly design.
/// Responsible for data retrieval, filtering, and pagination without side effects.
/// </summary>
/// <remarks>
/// Segregation Pattern: Read-Only Operations
/// - No mutations
/// - Cache-aware design
/// - Optimized for throughput
/// - IDisposable for cache cleanup
/// </remarks>
public interface IQueryService : IService
{
    /// <summary>
    /// Retrieves a single entity by identifier with caching support.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="id">The entity identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The entity if found; otherwise null.</returns>
    /// <exception cref="OperationFailedException">Thrown when retrieval fails.</exception>
    Task<T?> GetByIdAsync<T>(string id, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Executes a read-only query with optional caching.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="query">The LINQ query to execute.</param>
    /// <param name="cacheKey">Optional cache key for result caching.</param>
    /// <param name="cacheDuration">Optional cache duration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of entities matching the query.</returns>
    /// <exception cref="OperationFailedException">Thrown when query execution fails.</exception>
    Task<List<T>> QueryAsync<T>(
        IQueryable<T> query,
        string? cacheKey = null,
        TimeSpan? cacheDuration = null,
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Retrieves paginated results with efficient query execution.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="page">The page number (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="orderBy">Optional order-by selector.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paged result containing items and metadata.</returns>
    /// <exception cref="OperationFailedException">Thrown when pagination fails.</exception>
    Task<PagedResult<T>> PageAsync<T>(
        int page,
        int pageSize,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Searches entities by text query with ranking.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="searchText">The search text.</param>
    /// <param name="searchFields">Fields to search in.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of ranked search results.</returns>
    Task<List<SearchResult<T>>> SearchAsync<T>(
        string searchText,
        string[] searchFields,
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Counts entities matching a query condition.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="query">The LINQ query condition.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The count of matching entities.</returns>
    Task<int> CountAsync<T>(IQueryable<T> query, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Checks if an entity with the given ID exists.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="id">The entity identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if entity exists; otherwise false.</returns>
    Task<bool> ExistsAsync<T>(string id, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Retrieves multiple entities by identifiers efficiently.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="ids">The entity identifiers.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of found entities.</returns>
    Task<List<T>> GetByIdsAsync<T>(IEnumerable<string> ids, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Clears all cached query results.
    /// </summary>
    /// <returns>A task representing the cache clear operation.</returns>
    Task ClearCacheAsync();

    /// <summary>
    /// Gets cache statistics.
    /// </summary>
    /// <returns>Dictionary of cache statistics.</returns>
    Task<Dictionary<string, object>> GetCacheStatsAsync();
}

/// <summary>
/// Represents a search result with ranking information.
/// </summary>
public class SearchResult<T> where T : class
{
    /// <summary>Gets or sets the entity.</summary>
    public T Entity { get; set; } = null!;

    /// <summary>Gets or sets the relevance score (0-1).</summary>
    public float RelevanceScore { get; set; }

    /// <summary>Gets or sets the matched fields.</summary>
    public List<string> MatchedFields { get; set; } = new();
}
