// ═══════════════════════════════════════════════════════════════════════════
// Service Interfaces for Testability & Dependency Injection
// ═══════════════════════════════════════════════════════════════════════════

using System;
using System.Threading;
using System.Threading.Tasks;
using HELIOS.Platform.Phase11.GUI;
using HELIOS.Platform.Phase11.UpdateSystem;

namespace HELIOS.Platform.Interfaces
{
    /// <summary>
    /// Update service interface for dependency injection and mocking
    /// Enables unit testing without requiring actual downloads/installations
    /// </summary>
    public interface IUpdateService
    {
        Task<bool> ExecuteFullUpdateWorkflowAsync(
            UpdateChannel channel,
            CancellationToken cancellationToken = default);

        Task<UpdateManifest> CheckForUpdatesAsync(
            UpdateChannel channel,
            CancellationToken cancellationToken = default);

        Task DownloadComponentsAsync(
            UpdateManifest manifest,
            CancellationToken cancellationToken = default);

        Task<InstallationPlan> AnalyzeDependenciesAsync(
            UpdateManifest manifest,
            CancellationToken cancellationToken = default);

        Task LogUpdateSuccessAsync(UpdateManifest manifest);
    }

    /// <summary>
    /// Profile manager interface for runtime profile switching
    /// Enables enterprise deployment customization
    /// </summary>
    public interface IProfileManager
    {
        Task SwitchProfileAsync(OperationalProfile newProfile);
        string GetProfileDescription(OperationalProfile profile);
        OperationalProfile CurrentProfile { get; }
    }

    /// <summary>
    /// USB management interface for deployment and recovery
    /// Enables testing USB creation and deployment workflows
    /// </summary>
    public interface IUSBManager
    {
        Task<bool> CreateBootableUSBAsync(
            string usbDrivePath,
            UpdateManifest manifest,
            CancellationToken cancellationToken = default);

        Task<bool> VerifyUSBContentsAsync(
            string usbDrivePath,
            CancellationToken cancellationToken = default);

        Task<bool> DeployFromUSBAsync(
            string usbDrivePath,
            string targetDrive,
            CancellationToken cancellationToken = default);

        Task<bool> CreateRecoveryUSBAsync(
            string usbDrivePath,
            string snapshotPath,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Configuration service interface for runtime settings
    /// </summary>
    public interface IConfigurationService
    {
        UpdateChannel GetUpdateChannel();
        void SetUpdateChannel(UpdateChannel channel);
        
        string GetStagingPath();
        string GetRecoveryPath();
        
        bool IsAutoUpdateEnabled();
        void SetAutoUpdateEnabled(bool enabled);
    }
}
