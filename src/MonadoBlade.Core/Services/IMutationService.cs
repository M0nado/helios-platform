namespace MonadoBlade.Core.Services;

/// <summary>
/// Mutation service providing write operations with invalidation awareness.
/// Responsible for create, update, delete operations and cache invalidation.
/// </summary>
/// <remarks>
/// Segregation Pattern: Write Operations with Side Effects
/// - Create, Update, Delete operations
/// - Cache invalidation after mutations
/// - Transaction support
/// - Concurrency control
/// </remarks>
public interface IMutationService : IService
{
    /// <summary>
    /// Creates a new entity and persists it.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entity">The entity to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created entity with identity populated.</returns>
    /// <exception cref="ValidationFailedException">Thrown when entity validation fails.</exception>
    /// <exception cref="ResourceConflictException">Thrown when unique constraint violated.</exception>
    /// <exception cref="OperationFailedException">Thrown when creation fails.</exception>
    Task<T> CreateAsync<T>(T entity, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Updates an existing entity with optimistic concurrency control.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="id">The entity identifier.</param>
    /// <param name="entity">The updated entity.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated entity.</returns>
    /// <exception cref="EntityNotFoundException">Thrown when entity not found.</exception>
    /// <exception cref="ConcurrencyConflictException">Thrown when optimistic concurrency check fails.</exception>
    /// <exception cref="ValidationFailedException">Thrown when validation fails.</exception>
    /// <exception cref="OperationFailedException">Thrown when update fails.</exception>
    Task<T> UpdateAsync<T>(string id, T entity, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Deletes an entity by identifier.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="id">The entity identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the delete operation.</returns>
    /// <exception cref="EntityNotFoundException">Thrown when entity not found.</exception>
    /// <exception cref="OperationFailedException">Thrown when deletion fails.</exception>
    Task DeleteAsync<T>(string id, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Batch creates multiple entities in a single transaction.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entities">The entities to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of created entities.</returns>
    /// <exception cref="OperationFailedException">Thrown when batch creation fails.</exception>
    Task<List<T>> CreateBatchAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Batch updates multiple entities in a single transaction.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entities">Dictionary of ID to entity updates.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of updated entities.</returns>
    Task<List<T>> UpdateBatchAsync<T>(Dictionary<string, T> entities, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Batch deletes multiple entities in a single transaction.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="ids">The entity identifiers to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the batch delete operation.</returns>
    Task DeleteBatchAsync<T>(IEnumerable<string> ids, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Validates an entity before persistence without saving it.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entity">The entity to validate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of validation errors; empty if valid.</returns>
    Task<ICollection<string>> ValidateAsync<T>(T entity, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Executes multiple write operations in a transactional context.
    /// </summary>
    /// <param name="operationFactory">Factory function to create transaction operations.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if transaction succeeded; false otherwise.</returns>
    Task<bool> ExecuteInTransactionAsync(Func<ITransaction, Task> operationFactory, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates cache for a specific entity type after mutations.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="specificKey">Optional specific cache key to invalidate.</param>
    /// <returns>A task representing the cache invalidation.</returns>
    Task InvalidateCacheAsync<T>(string? specificKey = null) where T : class;

    /// <summary>
    /// Performs a soft delete (marks as deleted without removing).
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="id">The entity identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the soft delete operation.</returns>
    Task SoftDeleteAsync<T>(string id, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Restores a soft-deleted entity.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="id">The entity identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The restored entity.</returns>
    Task<T> RestoreAsync<T>(string id, CancellationToken cancellationToken = default) where T : class;
}
