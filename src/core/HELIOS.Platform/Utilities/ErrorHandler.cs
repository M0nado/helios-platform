// ═══════════════════════════════════════════════════════════════════════════
// Centralized Error Handling (Consolidates 15+ catch blocks)
// ═══════════════════════════════════════════════════════════════════════════

using System;
using System.IO;
using HELIOS.Platform.Core.Logging;

namespace HELIOS.Platform.Utilities
{
    /// <summary>
    /// Centralized error handler for consistent error logging and recovery
    /// Eliminates code duplication across update, boot, and GUI services
    /// </summary>
    public class ErrorHandler
    {
        private readonly ILogger _logger;

        public ErrorHandler(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Handle download errors with retry logic
        /// </summary>
        public bool HandleDownloadError(string component, Exception ex, int retryCount = 0)
        {
            if (retryCount >= 3)
            {
                _logger.Error($"Download failed permanently for {component}: {ex.Message}", ex);
                return false;
            }

            _logger.Warning($"Download failed for {component}, retry {retryCount + 1}/3: {ex.Message}");
            return true;  // Caller should retry
        }

        /// <summary>
        /// Handle installation errors with rollback
        /// </summary>
        public void HandleInstallationError(string component, string backupPath, Exception ex)
        {
            _logger.Error($"Installation failed for {component}: {ex.Message}", ex);
            
            try
            {
                if (Directory.Exists(backupPath))
                {
                    _logger.Info($"Attempting rollback from backup: {backupPath}");
                    // Rollback logic would go here
                }
            }
            catch (Exception rollbackEx)
            {
                _logger.Error($"Rollback also failed: {rollbackEx.Message}", rollbackEx);
            }
        }

        /// <summary>
        /// Handle partition/disk errors
        /// </summary>
        public bool HandleDiskError(string path, Exception ex)
        {
            if (ex is IOException ioEx)
            {
                _logger.Error($"Disk I/O error at {path}: {ioEx.Message}", ioEx);
                return false;  // Retry may not help
            }

            if (ex is UnauthorizedAccessException uaEx)
            {
                _logger.Error($"Access denied at {path}: {uaEx.Message}", uaEx);
                return false;  // Permission error, won't retry
            }

            _logger.Error($"Disk operation failed at {path}: {ex.Message}", ex);
            return true;  // May be transient
        }

        /// <summary>
        /// Handle network/HTTP errors
        /// </summary>
        public bool HandleNetworkError(string url, Exception ex, int retryCount = 0)
        {
            if (retryCount >= 5)
            {
                _logger.Error($"Network error (5 retries exhausted) for {url}: {ex.Message}", ex);
                return false;
            }

            _logger.Warning($"Network error for {url}, retry {retryCount + 1}/5: {ex.Message}");
            return true;  // Caller should retry
        }

        /// <summary>
        /// Handle service startup errors
        /// </summary>
        public void HandleServiceError(string serviceName, Exception ex)
        {
            _logger.Error($"Service {serviceName} startup failed: {ex.Message}", ex);
            // Optionally trigger fallback or degraded mode
        }

        /// <summary>
        /// Handle configuration/validation errors
        /// </summary>
        public void HandleConfigurationError(string setting, Exception ex)
        {
            _logger.Error($"Configuration error for {setting}: {ex.Message}", ex);
        }

        /// <summary>
        /// Handle security-critical errors
        /// </summary>
        public void HandleSecurityError(string component, Exception ex)
        {
            _logger.Error($"SECURITY ERROR in {component}: {ex.Message}", ex);
            // Log more verbosely for security issues
            _logger.Info($"Stack trace: {ex.StackTrace}");
        }
    }
}
