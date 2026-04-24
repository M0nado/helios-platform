// ═══════════════════════════════════════════════════════════════════════════
// Shared Configuration for Paths (Extracted for DRY Principle)
// ═══════════════════════════════════════════════════════════════════════════

using System;
using System.IO;

namespace HELIOS.Platform.Configuration
{
    /// <summary>
    /// Centralized path configuration (single source of truth)
    /// Eliminates 12+ hardcoded paths scattered throughout codebase
    /// </summary>
    public static class PathConfiguration
    {
        // Partition paths
        public static string CachePartitionPath => @"C:\Partitions\Cache";
        public static string SecurePartitionPath => @"C:\Partitions\Secure";
        public static string SystemPartitionPath => @"C:\Partitions\System";
        public static string UserPartitionPath => @"C:\Partitions\User";
        public static string WorkPartitionPath => @"C:\Partitions\Work";
        public static string DevelopmentPartitionPath => @"C:\Partitions\Development";
        public static string DataPartitionPath => @"C:\Partitions\Data";
        public static string CommonPartitionPath => @"C:\Partitions\Common";
        public static string VMPartitionPath => @"C:\Partitions\VM";

        // Installation paths
        public static string ProgramFilesHELIOSPath => @"C:\Program Files\HELIOS";
        public static string ProgramFilesRazorPath => @"C:\Program Files\Razer";
        public static string ProgramFilesMalwarebytesPath => @"C:\Program Files\Malwarebytes";
        public static string ProgramFilesMonadoPath => @"C:\Program Files\Monado";

        // Temporary & staging
        public static string TempStagingPath => Path.Combine(CachePartitionPath, "staging");
        public static string TempDownloadPath => Path.Combine(CachePartitionPath, "downloads");

        // Backup & recovery
        public static string BackupBasePath => Path.Combine(SecurePartitionPath, "backups");
        public static string SnapshotBasePath => Path.Combine(SecurePartitionPath, "snapshots");
        public static string RecoveryBasePath => Path.Combine(SecurePartitionPath, "recovery");

        // AI Models
        public static string AIModelsBasePath => Path.Combine(CommonPartitionPath, "ai-models");
        public static string ClaudeModelsPath => Path.Combine(AIModelsBasePath, "claude");
        public static string GPTModelsPath => Path.Combine(AIModelsBasePath, "gpt");
        public static string HermesModelsPath => Path.Combine(AIModelsBasePath, "hermes");
        public static string LocalModelsPath => Path.Combine(AIModelsBasePath, "local");

        // Configuration
        public static string ConfigurationBasePath => Path.Combine(SystemPartitionPath, "config");
        public static string UpdateConfigPath => Path.Combine(ConfigurationBasePath, "updates.config");
        public static string ProfileConfigPath => Path.Combine(ConfigurationBasePath, "profiles.config");
        public static string SecurityConfigPath => Path.Combine(ConfigurationBasePath, "security.config");

        // Drivers & firmware
        public static string DriversBasePath => Path.Combine(SystemPartitionPath, "drivers");
        public static string FirmwareBasePath => Path.Combine(SystemPartitionPath, "firmware");
        public static string RazerFirmwarePath => Path.Combine(FirmwareBasePath, "razer");

        // Logs
        public static string LogsBasePath => Path.Combine(SystemPartitionPath, "logs");
        public static string UpdateLogsPath => Path.Combine(LogsBasePath, "updates");
        public static string SystemLogsPath => Path.Combine(LogsBasePath, "system");
        public static string BootLogsPath => Path.Combine(LogsBasePath, "boot");

        /// <summary>
        /// Ensure all required directories exist
        /// </summary>
        public static void EnsureAllPathsExist()
        {
            var paths = new[]
            {
                CachePartitionPath, SecurePartitionPath, SystemPartitionPath, UserPartitionPath,
                WorkPartitionPath, DevelopmentPartitionPath, DataPartitionPath, CommonPartitionPath,
                VMPartitionPath, ProgramFilesHELIOSPath, TempStagingPath, TempDownloadPath,
                BackupBasePath, SnapshotBasePath, RecoveryBasePath, AIModelsBasePath,
                ConfigurationBasePath, DriversBasePath, FirmwareBasePath, LogsBasePath,
                UpdateLogsPath, SystemLogsPath, BootLogsPath
            };

            foreach (var path in paths)
            {
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
            }
        }
    }
}
