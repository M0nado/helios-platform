namespace HELIOS.Platform.Core.AdvancedOptimization.Interfaces
{
    /// <summary>
    /// Base interface for all services in the AdvancedOptimization module.
    /// </summary>
    public interface IService
    {
        /// <summary>
        /// Gets the name of the service.
        /// </summary>
        string ServiceName { get; }

        /// <summary>
        /// Initializes the service.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task InitializeAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Starts the service.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task StartAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Stops the service.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task StopAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if the service is running.
        /// </summary>
        /// <returns>True if running; otherwise false.</returns>
        bool IsRunning();

        /// <summary>
        /// Disposes the service resources.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        ValueTask DisposeAsync();
    }
}
