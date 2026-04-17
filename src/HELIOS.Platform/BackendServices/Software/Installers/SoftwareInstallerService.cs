using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using HELIOS.Platform.BackendServices.Software.Models;

namespace HELIOS.Platform.BackendServices.Software.Installers
{
    /// <summary>
    /// Implements software installation across multiple package managers and methods
    /// </summary>
    public class SoftwareInstallerService : ISoftwareInstallerService
    {
        private readonly string _downloadCacheDirectory;
        private readonly IInstallationLogger _logger;

        public SoftwareInstallerService(string downloadCacheDirectory = null, IInstallationLogger logger = null)
        {
            _downloadCacheDirectory = downloadCacheDirectory ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "HELIOS", "Software", "Downloads");
            _logger = logger ?? new ConsoleInstallationLogger();

            EnsureDirectories();
        }

        private void EnsureDirectories()
        {
            if (!Directory.Exists(_downloadCacheDirectory))
            {
                Directory.CreateDirectory(_downloadCacheDirectory);
            }
        }

        /// <summary>
        /// Install a package using the specified or preferred method
        /// </summary>
        public async Task<bool> InstallPackageAsync(SoftwarePackage package, string installMethod = null)
        {
            try
            {
                _logger.Log($"Starting installation of {package.Name}...");

                if (package.IsInstalled)
                {
                    _logger.Log($"{package.Name} is already installed");
                    return true;
                }

                package.Status = SoftwareStatus.Pending;

                var method = installMethod ?? package.PreferredMethod ?? InstallationMethod.Winget;

                bool success = false;

                switch (method.ToLower())
                {
                    case InstallationMethod.Winget:
                        success = await InstallViaWingetAsync(package);
                        break;
                    case InstallationMethod.Chocolatey:
                        success = await InstallViaChocolateyAsync(package);
                        break;
                    case InstallationMethod.Scoop:
                        success = await InstallViaScoopAsync(package);
                        break;
                    case InstallationMethod.Official:
                        success = await InstallViaOfficialAsync(package);
                        break;
                    case InstallationMethod.Portable:
                        success = await InstallPortableAsync(package);
                        break;
                    case InstallationMethod.Docker:
                        success = await InstallViaDockerAsync(package);
                        break;
                    case InstallationMethod.WSL:
                        success = await InstallViaWSLAsync(package);
                        break;
                    case InstallationMethod.Steam:
                        success = await InstallViaSteamAsync(package);
                        break;
                    case InstallationMethod.NPM:
                        success = await InstallViaNpmAsync(package);
                        break;
                    case InstallationMethod.Pip:
                        success = await InstallViaPipAsync(package);
                        break;
                    case InstallationMethod.Cargo:
                        success = await InstallViaCargoAsync(package);
                        break;
                    default:
                        _logger.Log($"Unknown installation method: {method}", LogLevel.Error);
                        success = false;
                        break;
                }

                if (success)
                {
                    package.Status = SoftwareStatus.Installed;
                    package.IsInstalled = true;
                    package.LastInstalled = DateTime.Now;
                    _logger.Log($"Successfully installed {package.Name}", LogLevel.Success);
                }
                else
                {
                    package.Status = SoftwareStatus.Failed;
                    _logger.Log($"Failed to install {package.Name}", LogLevel.Error);
                }

                return success;
            }
            catch (Exception ex)
            {
                package.Status = SoftwareStatus.Failed;
                package.StatusMessage = ex.Message;
                _logger.Log($"Installation error for {package.Name}: {ex.Message}", LogLevel.Error);
                return false;
            }
        }

        private async Task<bool> InstallViaWingetAsync(SoftwarePackage package)
        {
            var command = package.InstallCommand ?? $"winget install -e --id {package.Id} -h --accept-package-agreements --accept-source-agreements";
            return await ExecuteInstallCommandAsync(command);
        }

        private async Task<bool> InstallViaChocolateyAsync(SoftwarePackage package)
        {
            var command = package.InstallCommand ?? $"choco install {package.Id} -y";
            return await ExecuteInstallCommandAsync(command, requiresAdmin: true);
        }

        private async Task<bool> InstallViaScoopAsync(SoftwarePackage package)
        {
            var command = package.InstallCommand ?? $"scoop install {package.Id}";
            return await ExecuteInstallCommandAsync(command);
        }

        private async Task<bool> InstallViaOfficialAsync(SoftwarePackage package)
        {
            if (string.IsNullOrEmpty(package.DownloadUrl))
            {
                _logger.Log($"No download URL provided for {package.Name}", LogLevel.Error);
                return false;
            }

            var filePath = await DownloadFileAsync(package.DownloadUrl, package.Name);
            if (string.IsNullOrEmpty(filePath))
                return false;

            return await ExecuteInstallerAsync(filePath, package.InstallCommand);
        }

        private async Task<bool> InstallPortableAsync(SoftwarePackage package)
        {
            if (string.IsNullOrEmpty(package.DownloadUrl))
                return false;

            var filePath = await DownloadFileAsync(package.DownloadUrl, package.Name);
            if (string.IsNullOrEmpty(filePath))
                return false;

            // For portable apps, just extract/copy
            _logger.Log($"Portable package {package.Name} downloaded to {filePath}");
            return true;
        }

        private async Task<bool> InstallViaDockerAsync(SoftwarePackage package)
        {
            var command = package.InstallCommand ?? $"docker pull {package.Id}";
            return await ExecuteInstallCommandAsync(command);
        }

        private async Task<bool> InstallViaWSLAsync(SoftwarePackage package)
        {
            var command = $"wsl apt-get install -y {package.Id}";
            return await ExecuteInstallCommandAsync(command);
        }

        private async Task<bool> InstallViaSteamAsync(SoftwarePackage package)
        {
            _logger.Log($"Steam installation requires manual action: {package.Name}", LogLevel.Warning);
            return false; // Steam requires manual intervention
        }

        private async Task<bool> InstallViaNpmAsync(SoftwarePackage package)
        {
            var command = package.InstallCommand ?? $"npm install -g {package.Id}";
            return await ExecuteInstallCommandAsync(command);
        }

        private async Task<bool> InstallViaPipAsync(SoftwarePackage package)
        {
            var command = package.InstallCommand ?? $"pip install {package.Id}";
            return await ExecuteInstallCommandAsync(command);
        }

        private async Task<bool> InstallViaCargoAsync(SoftwarePackage package)
        {
            var command = package.InstallCommand ?? $"cargo install {package.Id}";
            return await ExecuteInstallCommandAsync(command);
        }

        /// <summary>
        /// Uninstall a package
        /// </summary>
        public async Task<bool> UninstallPackageAsync(SoftwarePackage package)
        {
            try
            {
                _logger.Log($"Uninstalling {package.Name}...");

                if (!package.IsInstalled)
                {
                    _logger.Log($"{package.Name} is not installed");
                    return true;
                }

                var command = package.UninstallCommand ?? $"winget uninstall -e --id {package.Id}";
                var success = await ExecuteInstallCommandAsync(command, requiresAdmin: true);

                if (success)
                {
                    package.IsInstalled = false;
                    package.Status = SoftwareStatus.NotInstalled;
                    _logger.Log($"Successfully uninstalled {package.Name}", LogLevel.Success);
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.Log($"Uninstall error: {ex.Message}", LogLevel.Error);
                return false;
            }
        }

        /// <summary>
        /// Update a package
        /// </summary>
        public async Task<bool> UpdatePackageAsync(SoftwarePackage package)
        {
            try
            {
                _logger.Log($"Updating {package.Name}...");

                package.Status = SoftwareStatus.Updating;

                var command = package.UpdateCommand ?? $"winget upgrade -e --id {package.Id}";
                var success = await ExecuteInstallCommandAsync(command);

                if (success)
                {
                    package.LastUpdated = DateTime.Now;
                    package.Status = SoftwareStatus.Installed;
                    _logger.Log($"Successfully updated {package.Name}", LogLevel.Success);
                }
                else
                {
                    package.Status = SoftwareStatus.Failed;
                }

                return success;
            }
            catch (Exception ex)
            {
                package.Status = SoftwareStatus.Failed;
                _logger.Log($"Update error: {ex.Message}", LogLevel.Error);
                return false;
            }
        }

        /// <summary>
        /// Bulk install multiple packages
        /// </summary>
        public async Task<InstallationResult> BulkInstallAsync(List<SoftwarePackage> packages)
        {
            var startTime = DateTime.Now;
            var result = new InstallationResult
            {
                TotalAttempted = packages.Count
            };

            foreach (var package in packages)
            {
                var success = await InstallPackageAsync(package);
                if (success)
                {
                    result.SuccessfulInstallations++;
                }
                else
                {
                    result.FailedInstallations++;
                    result.Errors.Add(new InstallationError
                    {
                        PackageName = package.Name,
                        ErrorMessage = package.StatusMessage ?? "Installation failed",
                        Timestamp = DateTime.Now
                    });
                }
            }

            result.CompletedAt = DateTime.Now;
            result.Duration = result.CompletedAt - startTime;

            _logger.Log($"Bulk installation complete: {result.SuccessfulInstallations}/{result.TotalAttempted} successful", 
                result.IsSuccessful ? LogLevel.Success : LogLevel.Warning);

            return result;
        }

        /// <summary>
        /// Rollback a failed installation
        /// </summary>
        public async Task<bool> RollbackInstallationAsync(SoftwarePackage package)
        {
            try
            {
                package.Status = SoftwareStatus.Rollback;
                _logger.Log($"Rolling back installation of {package.Name}...");
                
                return await UninstallPackageAsync(package);
            }
            catch (Exception ex)
            {
                _logger.Log($"Rollback failed: {ex.Message}", LogLevel.Error);
                return false;
            }
        }

        private async Task<bool> ExecuteInstallCommandAsync(string command, bool requiresAdmin = false)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {command}",
                    UseShellExecute = requiresAdmin,
                    Verb = requiresAdmin ? "runas" : null,
                    RedirectStandardOutput = !requiresAdmin,
                    RedirectStandardError = !requiresAdmin,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(psi))
                {
                    if (!requiresAdmin)
                    {
                        var output = await process.StandardOutput.ReadToEndAsync();
                        var error = await process.StandardError.ReadToEndAsync();
                    }

                    await Task.Run(() => process.WaitForExit(300000)); // 5 minute timeout
                    return process.ExitCode == 0;
                }
            }
            catch (Exception ex)
            {
                _logger.Log($"Command execution error: {ex.Message}", LogLevel.Error);
                return false;
            }
        }

        private async Task<bool> ExecuteInstallerAsync(string installerPath, string arguments = null)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = installerPath,
                    Arguments = arguments ?? "/S", // Silent install
                    UseShellExecute = true,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(psi))
                {
                    await Task.Run(() => process.WaitForExit(300000)); // 5 minute timeout
                    return process.ExitCode == 0;
                }
            }
            catch (Exception ex)
            {
                _logger.Log($"Installer execution error: {ex.Message}", LogLevel.Error);
                return false;
            }
        }

        private async Task<string> DownloadFileAsync(string downloadUrl, string packageName)
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.Timeout = TimeSpan.FromMinutes(10);

                    var response = await httpClient.GetAsync(downloadUrl);
                    response.EnsureSuccessStatusCode();

                    var filename = Path.GetFileName(downloadUrl);
                    if (string.IsNullOrEmpty(filename) || filename.Contains("?"))
                    {
                        filename = packageName + ".exe";
                    }

                    var filePath = Path.Combine(_downloadCacheDirectory, filename);

                    using (var fs = File.Create(filePath))
                    {
                        await response.Content.CopyToAsync(fs);
                    }

                    _logger.Log($"Downloaded {packageName} to {filePath}");
                    return filePath;
                }
            }
            catch (Exception ex)
            {
                _logger.Log($"Download error for {packageName}: {ex.Message}", LogLevel.Error);
                return null;
            }
        }
    }

    /// <summary>
    /// Logging interface for installation operations
    /// </summary>
    public interface IInstallationLogger
    {
        void Log(string message, LogLevel level = LogLevel.Info);
    }

    /// <summary>
    /// Log level enumeration
    /// </summary>
    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error,
        Success
    }

    /// <summary>
    /// Console implementation of installation logger
    /// </summary>
    public class ConsoleInstallationLogger : IInstallationLogger
    {
        public void Log(string message, LogLevel level = LogLevel.Info)
        {
            var color = level switch
            {
                LogLevel.Error => ConsoleColor.Red,
                LogLevel.Warning => ConsoleColor.Yellow,
                LogLevel.Success => ConsoleColor.Green,
                LogLevel.Debug => ConsoleColor.Gray,
                _ => ConsoleColor.White
            };

            var originalColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {level}: {message}");
            Console.ForegroundColor = originalColor;
        }
    }
}
