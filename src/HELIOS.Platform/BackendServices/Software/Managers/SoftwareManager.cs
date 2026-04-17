using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using HELIOS.Platform.BackendServices.Software.Models;

namespace HELIOS.Platform.BackendServices.Software.Managers
{
    /// <summary>
    /// Interface for software package discovery
    /// </summary>
    public interface ISoftwareDiscoveryService
    {
        Task<List<SoftwarePackage>> DiscoverInstalledSoftwareAsync();
        Task<List<SoftwarePackage>> SearchPackagesAsync(string query);
        Task<SoftwarePackage> GetPackageDetailsAsync(string packageId);
        Task<bool> IsPackageInstalledAsync(string packageName);
        Task<string> GetInstalledVersionAsync(string packageName);
    }

    /// <summary>
    /// Interface for software installation operations
    /// </summary>
    public interface ISoftwareInstallerService
    {
        Task<bool> InstallPackageAsync(SoftwarePackage package, string installMethod = null);
        Task<bool> UninstallPackageAsync(SoftwarePackage package);
        Task<bool> UpdatePackageAsync(SoftwarePackage package);
        Task<InstallationResult> BulkInstallAsync(List<SoftwarePackage> packages);
        Task<bool> RollbackInstallationAsync(SoftwarePackage package);
    }

    /// <summary>
    /// Interface for update management
    /// </summary>
    public interface IUpdateManager
    {
        Task<List<SoftwarePackage>> CheckForUpdatesAsync(List<SoftwarePackage> packages);
        Task<bool> UpdateAllAsync(List<SoftwarePackage> packages);
        Task<bool> ScheduleUpdatesAsync(List<SoftwarePackage> packages, TimeSpan offPeakStart, TimeSpan offPeakEnd);
        Task<UpdateSchedule> GetUpdateScheduleAsync();
    }

    /// <summary>
    /// Installation result tracking
    /// </summary>
    public class InstallationResult
    {
        public int TotalAttempted { get; set; }
        public int SuccessfulInstallations { get; set; }
        public int FailedInstallations { get; set; }
        public List<InstallationError> Errors { get; set; } = new List<InstallationError>();
        public DateTime CompletedAt { get; set; }
        public TimeSpan Duration { get; set; }
        
        public bool IsSuccessful => FailedInstallations == 0;
    }

    /// <summary>
    /// Installation error details
    /// </summary>
    public class InstallationError
    {
        public string PackageName { get; set; }
        public string ErrorMessage { get; set; }
        public string ErrorCode { get; set; }
        public DateTime Timestamp { get; set; }
        public Exception Exception { get; set; }
    }

    /// <summary>
    /// Update schedule definition
    /// </summary>
    public class UpdateSchedule
    {
        public bool AutoUpdateEnabled { get; set; }
        public DayOfWeek UpdateDay { get; set; } = DayOfWeek.Sunday;
        public TimeSpan UpdateTime { get; set; } = new TimeSpan(2, 0, 0); // 2 AM
        public bool OnlyOffPeakHours { get; set; } = true;
        public TimeSpan OffPeakStart { get; set; } = new TimeSpan(22, 0, 0); // 10 PM
        public TimeSpan OffPeakEnd { get; set; } = new TimeSpan(6, 0, 0); // 6 AM
        public List<string> ExcludedPackages { get; set; } = new List<string>();
    }

    /// <summary>
    /// Main software manager coordinating all operations
    /// </summary>
    public class SoftwareManager
    {
        private readonly ISoftwareDiscoveryService _discoveryService;
        private readonly ISoftwareInstallerService _installerService;
        private readonly IUpdateManager _updateManager;
        private readonly List<SoftwarePackage> _packageRegistry = new List<SoftwarePackage>();
        private readonly string _cacheDirectory;

        public SoftwareManager(
            ISoftwareDiscoveryService discoveryService,
            ISoftwareInstallerService installerService,
            IUpdateManager updateManager,
            string cacheDirectory = null)
        {
            _discoveryService = discoveryService;
            _installerService = installerService;
            _updateManager = updateManager;
            _cacheDirectory = cacheDirectory ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "HELIOS", "Software", "Cache");

            EnsureCacheDirectory();
        }

        private void EnsureCacheDirectory()
        {
            if (!Directory.Exists(_cacheDirectory))
            {
                Directory.CreateDirectory(_cacheDirectory);
            }
        }

        /// <summary>
        /// Scan the system for installed software
        /// </summary>
        public async Task<List<SoftwarePackage>> ScanInstalledSoftwareAsync()
        {
            var installed = await _discoveryService.DiscoverInstalledSoftwareAsync();
            _packageRegistry.Clear();
            _packageRegistry.AddRange(installed);
            return installed;
        }

        /// <summary>
        /// Get all registered packages
        /// </summary>
        public List<SoftwarePackage> GetAllPackages()
        {
            return _packageRegistry.ToList();
        }

        /// <summary>
        /// Get packages by category
        /// </summary>
        public List<SoftwarePackage> GetPackagesByCategory(string category)
        {
            return _packageRegistry.Where(p => p.Category == category).ToList();
        }

        /// <summary>
        /// Search for packages by name or tag
        /// </summary>
        public async Task<List<SoftwarePackage>> SearchAsync(string query)
        {
            return await _discoveryService.SearchPackagesAsync(query);
        }

        /// <summary>
        /// Install a single package
        /// </summary>
        public async Task<bool> InstallAsync(string packageName, string method = null)
        {
            var package = _packageRegistry.FirstOrDefault(p => p.Name == packageName);
            if (package == null)
                return false;

            return await _installerService.InstallPackageAsync(package, method);
        }

        /// <summary>
        /// Uninstall a package
        /// </summary>
        public async Task<bool> UninstallAsync(string packageName)
        {
            var package = _packageRegistry.FirstOrDefault(p => p.Name == packageName);
            if (package == null)
                return false;

            return await _installerService.UninstallPackageAsync(package);
        }

        /// <summary>
        /// Update a single package
        /// </summary>
        public async Task<bool> UpdateAsync(string packageName)
        {
            var package = _packageRegistry.FirstOrDefault(p => p.Name == packageName);
            if (package == null)
                return false;

            return await _installerService.UpdatePackageAsync(package);
        }

        /// <summary>
        /// Check for updates for all packages
        /// </summary>
        public async Task<List<SoftwarePackage>> CheckForUpdatesAsync()
        {
            var updatable = await _updateManager.CheckForUpdatesAsync(_packageRegistry);
            return updatable;
        }

        /// <summary>
        /// Update all packages with available updates
        /// </summary>
        public async Task<bool> UpdateAllAsync()
        {
            return await _updateManager.UpdateAllAsync(_packageRegistry);
        }

        /// <summary>
        /// Bulk install multiple packages
        /// </summary>
        public async Task<InstallationResult> BulkInstallAsync(List<string> packageNames)
        {
            var packages = _packageRegistry
                .Where(p => packageNames.Contains(p.Name))
                .ToList();

            return await _installerService.BulkInstallAsync(packages);
        }

        /// <summary>
        /// Verify health of installed software
        /// </summary>
        public async Task<SoftwareHealthReport> VerifyHealthAsync()
        {
            var report = new SoftwareHealthReport
            {
                ScanTime = DateTime.Now,
                TotalPackages = _packageRegistry.Count,
                InstalledPackages = _packageRegistry.Count(p => p.IsInstalled),
                FailedInstallations = _packageRegistry.Count(p => p.Status == SoftwareStatus.Failed),
                PackagesPendingUpdate = _packageRegistry.Count(p => p.CurrentVersion != p.LatestVersion)
            };

            foreach (var package in _packageRegistry)
            {
                if (package.VerifyCommand != null)
                {
                    var verified = await ExecuteCommandAsync(package.VerifyCommand);
                    package.IsInstalled = verified;
                }
            }

            return report;
        }

        /// <summary>
        /// Get software registry
        /// </summary>
        public List<SoftwarePackage> GetRegistry()
        {
            return _packageRegistry.ToList();
        }

        /// <summary>
        /// Register a package
        /// </summary>
        public void RegisterPackage(SoftwarePackage package)
        {
            var existing = _packageRegistry.FirstOrDefault(p => p.Id == package.Id);
            if (existing != null)
            {
                _packageRegistry.Remove(existing);
            }
            _packageRegistry.Add(package);
        }

        /// <summary>
        /// Execute a shell command
        /// </summary>
        public async Task<bool> ExecuteCommandAsync(string command)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {command}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(psi))
                {
                    await Task.Run(() => process.WaitForExit(30000)); // 30 second timeout
                    return process.ExitCode == 0;
                }
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Software health report
    /// </summary>
    public class SoftwareHealthReport
    {
        public DateTime ScanTime { get; set; }
        public int TotalPackages { get; set; }
        public int InstalledPackages { get; set; }
        public int FailedInstallations { get; set; }
        public int PackagesPendingUpdate { get; set; }

        public double InstallationRate => TotalPackages > 0 
            ? (InstalledPackages / (double)TotalPackages) * 100 
            : 0;

        public double UpdateRequiredRate => TotalPackages > 0
            ? (PackagesPendingUpdate / (double)TotalPackages) * 100
            : 0;
    }
}
