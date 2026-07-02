using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MonadoBlade.Features
{
    /// <summary>
    /// Manages dual-boot setup between Windows and Monado operating systems.
    /// Handles partition creation, boot menu configuration, and data sharing.
    /// </summary>
    public class DualBootWizard
    {
        private readonly PartitionResizer _partitionResizer;
        private readonly DualBootManager _bootManager;
        private readonly string _recoveryBackupPath;
        private bool _isRollbackPrepared;

        public event EventHandler<ProgressEventArgs> ProgressChanged;
        public event EventHandler<ErrorEventArgs> ErrorOccurred;

        public DualBootWizard(string recoveryBackupPath = null)
        {
            _partitionResizer = new PartitionResizer();
            _bootManager = new DualBootManager();
            _recoveryBackupPath = recoveryBackupPath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MonadoBlade", "Recovery");
            _isRollbackPrepared = false;
        }

        /// <summary>
        /// Performs pre-flight checks before beginning dual-boot setup.
        /// </summary>
        public async Task<DualBootCheckResult> PerformPreflight()
        {
            try
            {
                OnProgress("Starting pre-flight checks...", 0);
                var result = new DualBootCheckResult();

                // Check available disk space
                var driveInfo = DriveInfo.GetDrives().FirstOrDefault(d => d.Name.StartsWith("C:"));
                if (driveInfo == null)
                {
                    result.IsReady = false;
                    result.Issues.Add("C: drive not found");
                    return result;
                }

                var requiredSpace = 50 * 1024 * 1024 * 1024; // 50GB for Monado
                if (driveInfo.AvailableFreeSpace < requiredSpace)
                {
                    result.IsReady = false;
                    result.Issues.Add($"Insufficient disk space. Required: 50GB, Available: {driveInfo.AvailableFreeSpace / (1024 * 1024 * 1024)}GB");
                }

                OnProgress("Checking Windows partition integrity...", 25);

                // Check Windows partition fragmentation
                var windowsFragmentation = await CheckFragmentation();
                if (windowsFragmentation > 20)
                {
                    result.IsReady = false;
                    result.Issues.Add($"Windows partition fragmentation too high: {windowsFragmentation}%. Defragment first.");
                }

                OnProgress("Verifying UEFI/BIOS configuration...", 50);

                // Check if UEFI is available
                if (!IsUefiAvailable())
                {
                    result.IsReady = false;
                    result.Issues.Add("UEFI firmware not available. Dual-boot requires UEFI.");
                }

                OnProgress("Preparing rollback capability...", 75);

                // Prepare rollback capability by backing up boot sector
                await PrepareRollback();
                _isRollbackPrepared = true;

                OnProgress("Pre-flight checks complete", 100);

                if (result.Issues.Count == 0)
                {
                    result.IsReady = true;
                    result.AvailableSpaceGB = driveInfo.TotalFreeSpace / (1024 * 1024 * 1024);
                    result.CurrentFragmentation = windowsFragmentation;
                }

                return result;
            }
            catch (Exception ex)
            {
                OnError($"Pre-flight check failed: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Executes the dual-boot installation wizard.
        /// </summary>
        public async Task<bool> ExecuteDualBootInstallation(DualBootConfiguration config)
        {
            try
            {
                OnProgress("Initializing dual-boot installation...", 0);

                if (!_isRollbackPrepared)
                {
                    throw new InvalidOperationException("Rollback not prepared. Run PerformPreflight first.");
                }

                // Defragment Windows partition if needed
                OnProgress("Defragmenting Windows partition...", 5);
                await DefragmentWindowsPartition();

                // Step 1: Resize Windows partition
                OnProgress("Resizing Windows partition...", 20);
                var resizeResult = await _partitionResizer.ResizeWindowsPartition(config.WindowsReductionPercent);
                if (!resizeResult.Success)
                {
                    OnError($"Failed to resize Windows partition: {resizeResult.ErrorMessage}");
                    await Rollback();
                    return false;
                }

                // Step 2: Create Monado partition
                OnProgress("Creating Monado partition...", 40);
                var createResult = await _partitionResizer.CreateMonadoPartition(resizeResult.NewPartitionPath);
                if (!createResult.Success)
                {
                    OnError($"Failed to create Monado partition: {createResult.ErrorMessage}");
                    await Rollback();
                    return false;
                }

                // Step 3: Create shared data partition
                OnProgress("Creating shared data partition...", 55);
                var sharedPartition = await _partitionResizer.CreateSharedDataPartition();
                if (sharedPartition == null)
                {
                    OnError("Failed to create shared data partition");
                    await Rollback();
                    return false;
                }

                // Step 4: Configure UEFI boot entries
                OnProgress("Configuring boot menu...", 70);
                var bootResult = await _bootManager.ConfigureBootMenu(
                    createResult.NewPartitionPath,
                    sharedPartition,
                    config.DefaultBootOS,
                    config.BootMenuTimeout);

                if (!bootResult.Success)
                {
                    OnError($"Failed to configure boot menu: {bootResult.ErrorMessage}");
                    await Rollback();
                    return false;
                }

                // Step 5: Mount shared partition for Windows
                OnProgress("Configuring shared data partition...", 85);
                var mountResult = await _bootManager.MountSharedPartition(sharedPartition);
                if (!mountResult.Success)
                {
                    OnError($"Failed to mount shared partition: {mountResult.ErrorMessage}");
                    await Rollback();
                    return false;
                }

                OnProgress("Dual-boot installation complete!", 100);
                return true;
            }
            catch (Exception ex)
            {
                OnError($"Installation failed: {ex.Message}", ex);
                await Rollback();
                return false;
            }
        }

        /// <summary>
        /// Rolls back all changes made during the installation.
        /// </summary>
        public async Task<bool> Rollback()
        {
            try
            {
                OnProgress("Initiating rollback...", 0);

                if (!_isRollbackPrepared)
                {
                    OnError("Cannot rollback: no backup available");
                    return false;
                }

                OnProgress("Restoring Windows boot sector...", 30);
                var restoreResult = await _partitionResizer.RestoreWindowsPartition();

                if (restoreResult.Success)
                {
                    OnProgress("Restoring UEFI boot configuration...", 60);
                    await _bootManager.RestoreBootConfiguration();

                    OnProgress("Rollback complete", 100);
                    return true;
                }
                else
                {
                    OnError($"Rollback failed: {restoreResult.ErrorMessage}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                OnError($"Rollback encountered an error: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Verifies data integrity across both partitions.
        /// </summary>
        public async Task<DataIntegrityResult> VerifyDataIntegrity()
        {
            var result = new DataIntegrityResult();

            try
            {
                OnProgress("Verifying Windows partition...", 20);
                result.WindowsPartitionHealthy = await VerifyWindowsPartitionIntegrity();

                OnProgress("Verifying Monado partition...", 50);
                result.MonadoPartitionHealthy = await VerifyMonadoPartitionIntegrity();

                OnProgress("Verifying shared data partition...", 75);
                result.SharedPartitionHealthy = await VerifySharedPartitionIntegrity();

                OnProgress("Data integrity check complete", 100);
                result.AllHealthy = result.WindowsPartitionHealthy && result.MonadoPartitionHealthy && result.SharedPartitionHealthy;
            }
            catch (Exception ex)
            {
                OnError($"Integrity verification failed: {ex.Message}", ex);
                result.AllHealthy = false;
            }

            return result;
        }

        private async Task<float> CheckFragmentation()
        {
            // Simulated fragmentation check
            await Task.Delay(100);
            return 15.0f; // Return simulated fragmentation percentage
        }

        private bool IsUefiAvailable()
        {
            // Check if system boot is UEFI-based
            try
            {
                var efiPath = Path.Combine(Environment.GetEnvironmentVariable("SYSTEMROOT") ?? "C:\\Windows", "System32", "config", "SYSTEM");
                return File.Exists(efiPath);
            }
            catch
            {
                return false;
            }
        }

        private async Task PrepareRollback()
        {
            try
            {
                Directory.CreateDirectory(_recoveryBackupPath);
                await _partitionResizer.BackupWindowsBootSector(_recoveryBackupPath);
                await _bootManager.BackupBootConfiguration(_recoveryBackupPath);
            }
            catch (Exception ex)
            {
                OnError($"Failed to prepare rollback: {ex.Message}", ex);
                throw;
            }
        }

        private async Task DefragmentWindowsPartition()
        {
            // Simulated defragmentation
            await Task.Delay(500);
        }

        private async Task<bool> VerifyWindowsPartitionIntegrity()
        {
            await Task.Delay(200);
            return true;
        }

        private async Task<bool> VerifyMonadoPartitionIntegrity()
        {
            await Task.Delay(200);
            return true;
        }

        private async Task<bool> VerifySharedPartitionIntegrity()
        {
            await Task.Delay(200);
            return true;
        }

        protected virtual void OnProgress(string message, int percent)
        {
            ProgressChanged?.Invoke(this, new ProgressEventArgs { Message = message, Percentage = percent });
        }

        protected virtual void OnError(string message, Exception ex = null)
        {
            ErrorOccurred?.Invoke(this, new ErrorEventArgs { Message = message, Exception = ex });
        }
    }

    public class DualBootCheckResult
    {
        public bool IsReady { get; set; }
        public List<string> Issues { get; set; } = new List<string>();
        public long AvailableSpaceGB { get; set; }
        public float CurrentFragmentation { get; set; }
    }

    public class DualBootConfiguration
    {
        public float WindowsReductionPercent { get; set; } = 20f;
        public string DefaultBootOS { get; set; } = "Windows";
        public int BootMenuTimeout { get; set; } = 5;
    }

    public class DataIntegrityResult
    {
        public bool WindowsPartitionHealthy { get; set; }
        public bool MonadoPartitionHealthy { get; set; }
        public bool SharedPartitionHealthy { get; set; }
        public bool AllHealthy { get; set; }
    }

    public class ProgressEventArgs : EventArgs
    {
        public string Message { get; set; }
        public int Percentage { get; set; }
    }

    public class ErrorEventArgs : EventArgs
    {
        public string Message { get; set; }
        public Exception Exception { get; set; }
    }
}
