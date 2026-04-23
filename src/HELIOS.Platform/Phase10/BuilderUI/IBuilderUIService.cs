using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HELIOS.Platform.Phase10.BuilderUI
{
    /// <summary>
    /// Represents drive information for selection.
    /// </summary>
    public class DriveInfo
    {
        public string DriveId { get; set; }
        public string DriveName { get; set; }
        public string DriveType { get; set; } // USB or Disk
        public long TotalCapacity { get; set; }
        public long FreeSpace { get; set; }
        public bool IsHealthy { get; set; }
        public bool IsRecommended { get; set; }
        public string Manufacturer { get; set; }
        public DateTime LastHealthCheck { get; set; }
        public List<string> HealthWarnings { get; set; } = new();
    }

    /// <summary>
    /// Represents Windows version option.
    /// </summary>
    public class WindowsVersionOption
    {
        public string VersionId { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public long RequiredSpace { get; set; }
        public bool IsSelected { get; set; }
    }

    /// <summary>
    /// Represents installable package.
    /// </summary>
    public class Package
    {
        public string PackageId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Category { get; set; } // System, Dev, Gaming, Security, Media
        public long Size { get; set; }
        public bool IsSelected { get; set; }
        public List<string> Dependencies { get; set; } = new();
        public int Priority { get; set; }
    }

    /// <summary>
    /// Represents optimization profile.
    /// </summary>
    public class OptimizationProfile
    {
        public string ProfileId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Optimizations { get; set; }
        public bool IsRecommended { get; set; }
        public bool IsCustom { get; set; }
        public List<string> IncludedPackages { get; set; } = new();
    }

    /// <summary>
    /// Represents deployment summary information.
    /// </summary>
    public class DeploymentSummary
    {
        public string TargetDrive { get; set; }
        public string WindowsVersion { get; set; }
        public string SelectedProfile { get; set; }
        public List<Package> SelectedPackages { get; set; } = new();
        public long TotalSize { get; set; }
        public int EstimatedMinutes { get; set; }
        public bool TermsAccepted { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Represents wizard step information.
    /// </summary>
    public class WizardStep
    {
        public int StepNumber { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Content { get; set; }
        public bool CanGoBack { get; set; }
        public bool CanGoForward { get; set; }
        public bool IsValid { get; set; }
        public List<string> ValidationErrors { get; set; } = new();
    }

    /// <summary>
    /// Represents builder progress update.
    /// </summary>
    public class BuilderProgressUpdate
    {
        public int OverallPercentage { get; set; }
        public int SubtaskPercentage { get; set; }
        public string CurrentOperation { get; set; }
        public TimeSpan TimeRemaining { get; set; }
        public bool IsPaused { get; set; }
        public string ErrorMessage { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Comprehensive builder UI service interface.
    /// </summary>
    public interface IBuilderUIService
    {
        #region Lifecycle
        /// <summary>
        /// Initialize builder UI service.
        /// </summary>
        Task<bool> InitializeAsync();

        /// <summary>
        /// Shutdown builder UI service.
        /// </summary>
        Task ShutdownAsync();

        /// <summary>
        /// Get service status.
        /// </summary>
        Task<bool> GetStatusAsync();
        #endregion

        #region Wizard Management
        /// <summary>
        /// Get current step information.
        /// </summary>
        Task<WizardStep> GetCurrentStepAsync();

        /// <summary>
        /// Navigate to next step.
        /// </summary>
        Task<bool> GoToNextStepAsync();

        /// <summary>
        /// Navigate to previous step.
        /// </summary>
        Task<bool> GoToPreviousStepAsync();

        /// <summary>
        /// Go to specific step.
        /// </summary>
        Task<bool> GoToStepAsync(int stepNumber);

        /// <summary>
        /// Validate current step.
        /// </summary>
        Task<List<string>> ValidateCurrentStepAsync();

        /// <summary>
        /// Get all wizard steps.
        /// </summary>
        Task<List<WizardStep>> GetAllStepsAsync();
        #endregion

        #region Drive Management
        /// <summary>
        /// Get available drives for installation.
        /// </summary>
        Task<List<DriveInfo>> GetAvailableDrivesAsync();

        /// <summary>
        /// Select target drive for installation.
        /// </summary>
        Task<bool> SelectDriveAsync(string driveId);

        /// <summary>
        /// Get recommended drive.
        /// </summary>
        Task<DriveInfo> GetRecommendedDriveAsync();

        /// <summary>
        /// Verify selected drive.
        /// </summary>
        Task<bool> VerifyDriveAsync(string driveId);

        /// <summary>
        /// Check drive health.
        /// </summary>
        Task<bool> CheckDriveHealthAsync(string driveId);
        #endregion

        #region Windows Version
        /// <summary>
        /// Get available Windows versions.
        /// </summary>
        Task<List<WindowsVersionOption>> GetWindowsVersionsAsync();

        /// <summary>
        /// Select Windows version.
        /// </summary>
        Task<bool> SelectWindowsVersionAsync(string versionId);

        /// <summary>
        /// Get selected Windows version.
        /// </summary>
        Task<string> GetSelectedWindowsVersionAsync();
        #endregion

        #region Package Management
        /// <summary>
        /// Get all available packages.
        /// </summary>
        Task<List<Package>> GetAllPackagesAsync();

        /// <summary>
        /// Get packages by category.
        /// </summary>
        Task<List<Package>> GetPackagesByCategoryAsync(string category);

        /// <summary>
        /// Select package.
        /// </summary>
        Task<bool> SelectPackageAsync(string packageId);

        /// <summary>
        /// Deselect package.
        /// </summary>
        Task<bool> DeselectPackageAsync(string packageId);

        /// <summary>
        /// Get selected packages.
        /// </summary>
        Task<List<Package>> GetSelectedPackagesAsync();

        /// <summary>
        /// Calculate total size of selected packages.
        /// </summary>
        Task<long> CalculateTotalSizeAsync();

        /// <summary>
        /// Get package dependencies.
        /// </summary>
        Task<List<string>> GetPackageDependenciesAsync(string packageId);
        #endregion

        #region Profiles
        /// <summary>
        /// Get available profiles.
        /// </summary>
        Task<List<OptimizationProfile>> GetAvailableProfilesAsync();

        /// <summary>
        /// Select profile.
        /// </summary>
        Task<bool> SelectProfileAsync(string profileId);

        /// <summary>
        /// Get selected profile.
        /// </summary>
        Task<OptimizationProfile> GetSelectedProfileAsync();

        /// <summary>
        /// Get recommended profile.
        /// </summary>
        Task<OptimizationProfile> GetRecommendedProfileAsync();

        /// <summary>
        /// Create custom profile.
        /// </summary>
        Task<bool> CreateCustomProfileAsync(OptimizationProfile profile);

        /// <summary>
        /// Get profile preview with package changes.
        /// </summary>
        Task<List<Package>> GetProfilePreviewAsync(string profileId);
        #endregion

        #region Summary & Deployment
        /// <summary>
        /// Get deployment summary.
        /// </summary>
        Task<DeploymentSummary> GetDeploymentSummaryAsync();

        /// <summary>
        /// Accept terms and conditions.
        /// </summary>
        Task<bool> AcceptTermsAsync();

        /// <summary>
        /// Start deployment process.
        /// </summary>
        Task<bool> StartDeploymentAsync();

        /// <summary>
        /// Pause ongoing deployment.
        /// </summary>
        Task<bool> PauseDeploymentAsync();

        /// <summary>
        /// Resume paused deployment.
        /// </summary>
        Task<bool> ResumeDeploymentAsync();

        /// <summary>
        /// Cancel ongoing deployment.
        /// </summary>
        Task<bool> CancelDeploymentAsync();

        /// <summary>
        /// Get deployment progress.
        /// </summary>
        Task<BuilderProgressUpdate> GetProgressAsync();
        #endregion

        #region Events
        /// <summary>
        /// Event raised when step changes.
        /// </summary>
        event EventHandler<int> OnStepChanged;

        /// <summary>
        /// Event raised when progress updates.
        /// </summary>
        event EventHandler<BuilderProgressUpdate> OnProgressUpdated;

        /// <summary>
        /// Event raised on error.
        /// </summary>
        event EventHandler<string> OnError;

        /// <summary>
        /// Event raised when deployment completes.
        /// </summary>
        event EventHandler<bool> OnDeploymentCompleted;
        #endregion
    }
}
