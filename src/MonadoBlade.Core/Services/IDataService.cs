namespace MonadoBlade.Core.Services;

/// <summary>
/// Represents the result of a paged query operation.
/// </summary>
/// <typeparam name="T">The type of items in the page.</typeparam>
public class PagedResult<T>
{
    /// <summary>Gets the items in the current page.</summary>
    public List<T> Items { get; set; } = new();

    /// <summary>Gets the total number of items across all pages.</summary>
    public int TotalCount { get; set; }

    /// <summary>Gets the current page number (1-based).</summary>
    public int PageNumber { get; set; }

    /// <summary>Gets the size of each page.</summary>
    public int PageSize { get; set; }

    /// <summary>Gets the total number of pages.</summary>
    public int TotalPages => (TotalCount + PageSize - 1) / PageSize;

    /// <summary>Gets whether there are more pages after the current page.</summary>
    public bool HasNextPage => PageNumber < TotalPages;

    /// <summary>Gets whether there are pages before the current page.</summary>
    public bool HasPreviousPage => PageNumber > 1;
}

/// <summary>
/// Core data service providing CRUD operations and advanced querying capabilities.
/// Responsible for managing entity lifecycle, data validation, and persistence.
/// </summary>
public interface IDataService : IService
{
    /// <summary>
    /// Retrieves a single entity by its unique identifier.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="id">The entity identifier.</param>
    /// <returns>The entity if found; otherwise null.</returns>
    /// <exception cref="OperationFailedException">Thrown when retrieval fails.</exception>
    Task<T?> GetByIdAsync<T>(string id) where T : class;

    /// <summary>
    /// Executes a query against entities of type T.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="query">The LINQ query to execute.</param>
    /// <returns>A list of entities matching the query.</returns>
    /// <exception cref="OperationFailedException">Thrown when query execution fails.</exception>
    Task<List<T>> QueryAsync<T>(IQueryable<T> query) where T : class;

    /// <summary>
    /// Creates a new entity and persists it.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entity">The entity to create.</param>
    /// <returns>The created entity with identity populated.</returns>
    /// <exception cref="ValidationFailedException">Thrown when entity validation fails.</exception>
    /// <exception cref="OperationFailedException">Thrown when creation fails.</exception>
    Task<T> CreateAsync<T>(T entity) where T : class;

    /// <summary>
    /// Updates an existing entity.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="id">The entity identifier.</param>
    /// <param name="entity">The updated entity.</param>
    /// <returns>The updated entity.</returns>
    /// <exception cref="ValidationFailedException">Thrown when entity validation fails.</exception>
    /// <exception cref="ConcurrencyConflictException">Thrown when optimistic concurrency check fails.</exception>
    /// <exception cref="OperationFailedException">Thrown when update fails.</exception>
    Task<T> UpdateAsync<T>(string id, T entity) where T : class;

    /// <summary>
    /// Deletes an entity by its identifier.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="id">The entity identifier.</param>
    /// <exception cref="OperationFailedException">Thrown when deletion fails.</exception>
    Task DeleteAsync<T>(string id) where T : class;

    /// <summary>
    /// Retrieves a paginated result set.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="page">The page number (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns>A paged result containing items and pagination metadata.</returns>
    /// <exception cref="OperationFailedException">Thrown when pagination fails.</exception>
    Task<PagedResult<T>> PageAsync<T>(int page, int pageSize) where T : class;

    /// <summary>
    /// Validates an entity without persisting it.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entity">The entity to validate.</param>
    /// <returns>A collection of validation errors; empty if valid.</returns>
    Task<ICollection<string>> ValidateAsync<T>(T entity) where T : class;

    /// <summary>
    /// Executes multiple operations in a transaction.
    /// </summary>
    /// <param name="operationFactory">Factory function to create transaction operations.</param>
    /// <returns>True if transaction succeeded; false otherwise.</returns>
    Task<bool> ExecuteInTransactionAsync(Func<ITransaction, Task> operationFactory);
}

/// <summary>
/// Represents a transaction context for multi-operation consistency.
/// </summary>
public interface ITransaction : IAsyncDisposable
{
    /// <summary>Commits the transaction.</summary>
    Task CommitAsync();

    /// <summary>Rolls back the transaction.</summary>
    Task RollbackAsync();
}
