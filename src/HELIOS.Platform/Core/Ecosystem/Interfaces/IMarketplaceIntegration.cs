using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HELIOS.Platform.Core.Ecosystem.Interfaces
{
    /// <summary>
    /// Manages plugin and service marketplace integration.
    /// Handles plugin discovery, installation, versioning, and lifecycle management.
    /// </summary>
    public interface IMarketplaceIntegration
    {
        /// <summary>
        /// Gets all available plugins from the marketplace.
        /// </summary>
        /// <param name="category">Optional category filter</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of available plugins</returns>
        Task<IEnumerable<PluginMetadata>> DiscoverPluginsAsync(string? category = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Installs a plugin from the marketplace.
        /// </summary>
        /// <param name="pluginId">Plugin identifier</param>
        /// <param name="version">Plugin version to install</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Installation result</returns>
        Task<InstallationResult> InstallPluginAsync(string pluginId, string version, CancellationToken cancellationToken = default);

        /// <summary>
        /// Uninstalls a previously installed plugin.
        /// </summary>
        /// <param name="pluginId">Plugin identifier</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Uninstallation result</returns>
        Task<UninstallationResult> UninstallPluginAsync(string pluginId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets detailed information about a specific plugin.
        /// </summary>
        /// <param name="pluginId">Plugin identifier</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Plugin details or null if not found</returns>
        Task<PluginDetails?> GetPluginDetailsAsync(string pluginId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all installed plugins on the system.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of installed plugins</returns>
        Task<IEnumerable<InstalledPlugin>> GetInstalledPluginsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks for plugin updates.
        /// </summary>
        /// <param name="pluginId">Plugin identifier</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Available update information or null if no updates</returns>
        Task<PluginUpdate?> CheckForUpdatesAsync(string pluginId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates a plugin to a newer version.
        /// </summary>
        /// <param name="pluginId">Plugin identifier</param>
        /// <param name="targetVersion">Target version</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Update result</returns>
        Task<UpdateResult> UpdatePluginAsync(string pluginId, string targetVersion, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates plugin compatibility before installation.
        /// </summary>
        /// <param name="pluginId">Plugin identifier</param>
        /// <param name="version">Plugin version</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Compatibility validation result</returns>
        Task<CompatibilityResult> ValidateCompatibilityAsync(string pluginId, string version, CancellationToken cancellationToken = default);

        /// <summary>
        /// Rates a plugin in the marketplace.
        /// </summary>
        /// <param name="pluginId">Plugin identifier</param>
        /// <param name="rating">Rating (1-5)</param>
        /// <param name="review">Optional review text</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Rating result</returns>
        Task<RatingResult> RatePluginAsync(string pluginId, int rating, string? review = null, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Metadata for a plugin available in the marketplace.
    /// </summary>
    public class PluginMetadata
    {
        /// <summary>Gets or sets the plugin identifier.</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>Gets or sets the plugin name.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Gets or sets the plugin description.</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>Gets or sets the latest available version.</summary>
        public string LatestVersion { get; set; } = string.Empty;

        /// <summary>Gets or sets the publisher name.</summary>
        public string Publisher { get; set; } = string.Empty;

        /// <summary>Gets or sets the plugin category.</summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>Gets or sets the average rating (0-5 scale).</summary>
        public double Rating { get; set; }

        /// <summary>Gets or sets the number of downloads.</summary>
        public long Downloads { get; set; }

        /// <summary>Gets or sets whether the plugin is verified.</summary>
        public bool IsVerified { get; set; }

        /// <summary>Gets or sets the list of supported platforms.</summary>
        public IEnumerable<string> SupportedPlatforms { get; set; } = Array.Empty<string>();
    }

    /// <summary>
    /// Detailed information about a plugin.
    /// </summary>
    public class PluginDetails
    {
        /// <summary>Gets or sets the plugin identifier.</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>Gets or sets the plugin name.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Gets or sets the full description.</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>Gets or sets the available versions.</summary>
        public IEnumerable<string> AvailableVersions { get; set; } = Array.Empty<string>();

        /// <summary>Gets or sets the minimum system requirements.</summary>
        public SystemRequirements Requirements { get; set; } = new();

        /// <summary>Gets or sets the license information.</summary>
        public string License { get; set; } = string.Empty;

        /// <summary>Gets or sets the release notes.</summary>
        public string ReleaseNotes { get; set; } = string.Empty;

        /// <summary>Gets or sets the dependencies.</summary>
        public IEnumerable<PluginDependency> Dependencies { get; set; } = Array.Empty<PluginDependency>();
    }

    /// <summary>
    /// Information about an installed plugin.
    /// </summary>
    public class InstalledPlugin
    {
        /// <summary>Gets or sets the plugin identifier.</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>Gets or sets the installed version.</summary>
        public string InstalledVersion { get; set; } = string.Empty;

        /// <summary>Gets or sets the installation date.</summary>
        public DateTime InstalledDate { get; set; }

        /// <summary>Gets or sets whether the plugin is enabled.</summary>
        public bool IsEnabled { get; set; }

        /// <summary>Gets or sets the plugin status.</summary>
        public string Status { get; set; } = string.Empty;
    }

    /// <summary>
    /// Information about available plugin update.
    /// </summary>
    public class PluginUpdate
    {
        /// <summary>Gets or sets the plugin identifier.</summary>
        public string PluginId { get; set; } = string.Empty;

        /// <summary>Gets or sets the new available version.</summary>
        public string NewVersion { get; set; } = string.Empty;

        /// <summary>Gets or sets the current version.</summary>
        public string CurrentVersion { get; set; } = string.Empty;

        /// <summary>Gets or sets the update release notes.</summary>
        public string ReleaseNotes { get; set; } = string.Empty;

        /// <summary>Gets or sets whether the update is critical.</summary>
        public bool IsCritical { get; set; }
    }

    /// <summary>
    /// System requirements for a plugin.
    /// </summary>
    public class SystemRequirements
    {
        /// <summary>Gets or sets the minimum platform version.</summary>
        public string MinimumVersion { get; set; } = string.Empty;

        /// <summary>Gets or sets the minimum RAM in GB.</summary>
        public int MinimumRamGb { get; set; }

        /// <summary>Gets or sets the required dependencies.</summary>
        public IEnumerable<string> RequiredDependencies { get; set; } = Array.Empty<string>();
    }

    /// <summary>
    /// Plugin dependency information.
    /// </summary>
    public class PluginDependency
    {
        /// <summary>Gets or sets the dependency identifier.</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>Gets or sets the required version range.</summary>
        public string VersionRange { get; set; } = string.Empty;
    }

    /// <summary>
    /// Result of a plugin installation operation.
    /// </summary>
    public class InstallationResult
    {
        /// <summary>Gets or sets whether installation succeeded.</summary>
        public bool Success { get; set; }

        /// <summary>Gets or sets the plugin identifier.</summary>
        public string PluginId { get; set; } = string.Empty;

        /// <summary>Gets or sets the installed version.</summary>
        public string InstalledVersion { get; set; } = string.Empty;

        /// <summary>Gets or sets any error messages.</summary>
        public string? ErrorMessage { get; set; }

        /// <summary>Gets or sets the installation timestamp.</summary>
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Result of a plugin uninstallation operation.
    /// </summary>
    public class UninstallationResult
    {
        /// <summary>Gets or sets whether uninstallation succeeded.</summary>
        public bool Success { get; set; }

        /// <summary>Gets or sets the plugin identifier.</summary>
        public string PluginId { get; set; } = string.Empty;

        /// <summary>Gets or sets any error messages.</summary>
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Result of a plugin update operation.
    /// </summary>
    public class UpdateResult
    {
        /// <summary>Gets or sets whether update succeeded.</summary>
        public bool Success { get; set; }

        /// <summary>Gets or sets the plugin identifier.</summary>
        public string PluginId { get; set; } = string.Empty;

        /// <summary>Gets or sets the new version.</summary>
        public string NewVersion { get; set; } = string.Empty;

        /// <summary>Gets or sets the previous version.</summary>
        public string PreviousVersion { get; set; } = string.Empty;

        /// <summary>Gets or sets any error messages.</summary>
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Result of plugin compatibility validation.
    /// </summary>
    public class CompatibilityResult
    {
        /// <summary>Gets or sets whether the plugin is compatible.</summary>
        public bool IsCompatible { get; set; }

        /// <summary>Gets or sets compatibility issues found.</summary>
        public IEnumerable<string> Issues { get; set; } = Array.Empty<string>();

        /// <summary>Gets or sets any warnings.</summary>
        public IEnumerable<string> Warnings { get; set; } = Array.Empty<string>();
    }

    /// <summary>
    /// Result of a plugin rating operation.
    /// </summary>
    public class RatingResult
    {
        /// <summary>Gets or sets whether rating was successful.</summary>
        public bool Success { get; set; }

        /// <summary>Gets or sets the new average rating.</summary>
        public double NewAverageRating { get; set; }

        /// <summary>Gets or sets any error messages.</summary>
        public string? ErrorMessage { get; set; }
    }
}
