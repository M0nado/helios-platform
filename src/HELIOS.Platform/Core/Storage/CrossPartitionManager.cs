using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HELIOS.Platform.Core.Logging;
using HELIOS.Platform.Core.Administration;

namespace HELIOS.Platform.Core.Storage
{
    /// <summary>
    /// Cross-partition management for unified storage operations.
    /// </summary>
    public interface ICrossPartitionManager
    {
        Task<List<PartitionInfo>> GetAllPartitionsAsync();
        Task<PartitionInfo?> GetPartitionByLetterAsync(string driveLetter);
        Task<bool> MountPartitionAsync(string driveLetter, string mountPath);
        Task<bool> UnmountPartitionAsync(string driveLetter);
        Task<UnifiedStorageView> GetUnifiedStorageViewAsync();
        Task<MigrationReport> MigrateDataAsync(string sourceDrive, string targetDrive, string pattern = "*");
        Task<bool> CreateSymlinkAsync(string source, string target);
        Task<List<PartitionBalance>> AnalyzeStorageBalanceAsync();
    }

    public class PartitionInfo
    {
        public string DriveLetter { get; set; } = string.Empty;
        public string FileSystem { get; set; } = string.Empty;
        public long TotalBytes { get; set; }
        public long FreeBytes { get; set; }
        public string Label { get; set; } = string.Empty;
        public bool IsRemovable { get; set; }
        public DateTime MountedAt { get; set; }
        public List<string> MountPoints { get; set; } = new();

        public double UsagePercent => TotalBytes > 0 ? (100.0 - ((double)FreeBytes / TotalBytes * 100)) : 0;
    }

    public class UnifiedStorageView
    {
        public List<PartitionInfo> Partitions { get; set; } = new();
        public long TotalCapacity { get; set; }
        public long TotalUsed { get; set; }
        public long TotalFree { get; set; }
        public double OverallUsagePercent { get; set; }
        public Dictionary<string, string> MountMapping { get; set; } = new();
    }

    public class MigrationReport
    {
        public string SourceDrive { get; set; } = string.Empty;
        public string TargetDrive { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; }
        public DateTime CompletedAt { get; set; }
        public long BytesMigrated { get; set; }
        public int FilesMigrated { get; set; }
        public List<string> Errors { get; set; } = new();
        public bool Success { get; set; }

        public TimeSpan Duration => CompletedAt - StartedAt;
        public double MegabytesMigrated => BytesMigrated / (1024.0 * 1024.0);
    }

    public class PartitionBalance
    {
        public string DriveLetter { get; set; } = string.Empty;
        public double UsagePercent { get; set; }
        public string BalanceStatus { get; set; } = string.Empty;
        public List<string> Recommendations { get; set; } = new();
    }

    /// <summary>
    /// Manages operations across multiple partitions.
    /// </summary>
    public class CrossPartitionManager : ICrossPartitionManager
    {
        private readonly Core.Logging.ILogger? _logger;

        public CrossPartitionManager(Core.Logging.ILogger? logger = null)
        {
            _logger = logger;
        }

        public async Task<List<PartitionInfo>> GetAllPartitionsAsync()
        {
            var partitions = new List<PartitionInfo>();

            try
            {
                var drives = DriveInfo.GetDrives();

                foreach (var drive in drives)
                {
                    try
                    {
                        var partition = new PartitionInfo
                        {
                            DriveLetter = drive.Name.TrimEnd(Path.DirectorySeparatorChar),
                            FileSystem = drive.DriveFormat,
                            TotalBytes = drive.TotalSize,
                            FreeBytes = drive.AvailableFreeSpace,
                            Label = drive.VolumeLabel,
                            IsRemovable = drive.DriveType == DriveType.Removable,
                            MountedAt = DateTime.UtcNow,
                            MountPoints = new List<string> { drive.RootDirectory.FullName }
                        };

                        partitions.Add(partition);
                    }
                    catch (Exception ex)
                    {
                        _logger?.Warning($"Failed to read drive {drive.Name}: {ex.Message}");
                    }
                }

                _logger?.Info($"Retrieved {partitions.Count} partitions");
            }
            catch (Exception ex)
            {
                _logger?.Error($"Error getting partitions: {ex.Message}");
            }

            return partitions;
        }

        public async Task<PartitionInfo?> GetPartitionByLetterAsync(string driveLetter)
        {
            try
            {
                var drives = await GetAllPartitionsAsync();
                var normalized = driveLetter.ToUpper().TrimEnd(':');
                return drives.FirstOrDefault(p => p.DriveLetter.Contains(normalized));
            }
            catch (Exception ex)
            {
                _logger?.Error($"Error getting partition {driveLetter}: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> MountPartitionAsync(string driveLetter, string mountPath)
        {
            try
            {
                var partition = await GetPartitionByLetterAsync(driveLetter);
                if (partition == null)
                {
                    _logger?.Warning($"Partition {driveLetter} not found");
                    return false;
                }

                if (!Directory.Exists(mountPath))
                    Directory.CreateDirectory(mountPath);

                partition.MountPoints.Add(mountPath);
                _logger?.Info($"Partition {driveLetter} mounted to {mountPath}");
                return true;
            }
            catch (Exception ex)
            {
                _logger?.Error($"Mount failed for {driveLetter}: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UnmountPartitionAsync(string driveLetter)
        {
            try
            {
                var partition = await GetPartitionByLetterAsync(driveLetter);
                if (partition == null)
                    return false;

                partition.MountPoints.Clear();
                _logger?.Info($"Partition {driveLetter} unmounted");
                return true;
            }
            catch (Exception ex)
            {
                _logger?.Error($"Unmount failed for {driveLetter}: {ex.Message}");
                return false;
            }
        }

        public async Task<UnifiedStorageView> GetUnifiedStorageViewAsync()
        {
            var view = new UnifiedStorageView();

            try
            {
                view.Partitions = await GetAllPartitionsAsync();

                foreach (var partition in view.Partitions)
                {
                    view.TotalCapacity += partition.TotalBytes;
                    view.TotalUsed += (partition.TotalBytes - partition.FreeBytes);
                    view.TotalFree += partition.FreeBytes;

                    foreach (var mountPoint in partition.MountPoints)
                    {
                        view.MountMapping[partition.DriveLetter] = mountPoint;
                    }
                }

                view.OverallUsagePercent = view.TotalCapacity > 0
                    ? (100.0 - ((double)view.TotalFree / view.TotalCapacity * 100))
                    : 0;

                _logger?.Info($"Unified view generated: {view.TotalCapacity / (1024.0 * 1024.0 * 1024.0):F2} GB total");
            }
            catch (Exception ex)
            {
                _logger?.Error($"Error generating unified view: {ex.Message}");
            }

            return view;
        }

        public async Task<MigrationReport> MigrateDataAsync(string sourceDrive, string targetDrive, string pattern = "*")
        {
            var report = new MigrationReport
            {
                SourceDrive = sourceDrive,
                TargetDrive = targetDrive,
                StartedAt = DateTime.UtcNow
            };

            try
            {
                var sourcePartition = await GetPartitionByLetterAsync(sourceDrive);
                var targetPartition = await GetPartitionByLetterAsync(targetDrive);

                if (sourcePartition == null || targetPartition == null)
                {
                    report.Errors.Add("Source or target partition not found");
                    report.Success = false;
                    return report;
                }

                var sourcePath = sourcePartition.MountPoints.FirstOrDefault() ?? $"{sourceDrive}:";
                var targetPath = targetPartition.MountPoints.FirstOrDefault() ?? $"{targetDrive}:";

                var sourceDir = new DirectoryInfo(sourcePath);
                if (!sourceDir.Exists)
                {
                    report.Errors.Add($"Source directory not found: {sourcePath}");
                    report.Success = false;
                    return report;
                }

                var files = sourceDir.GetFiles(pattern, SearchOption.AllDirectories);

                foreach (var file in files)
                {
                    try
                    {
                        var relativePath = Path.GetRelativePath(sourcePath, file.FullName);
                        var destPath = Path.Combine(targetPath, relativePath);

                        Directory.CreateDirectory(Path.GetDirectoryName(destPath) ?? targetPath);
                        File.Copy(file.FullName, destPath, true);

                        report.BytesMigrated += file.Length;
                        report.FilesMigrated++;
                    }
                    catch (Exception ex)
                    {
                        report.Errors.Add($"Failed to migrate {file.Name}: {ex.Message}");
                    }
                }

                report.Success = report.Errors.Count == 0;
                _logger?.Info($"Migration complete: {report.FilesMigrated} files, {report.MegabytesMigrated:F2} MB");
            }
            catch (Exception ex)
            {
                report.Errors.Add($"Migration error: {ex.Message}");
                report.Success = false;
                _logger?.Error($"Migration failed: {ex.Message}");
            }
            finally
            {
                report.CompletedAt = DateTime.UtcNow;
            }

            return report;
        }

        public async Task<bool> CreateSymlinkAsync(string source, string target)
        {
            try
            {
                if (Directory.Exists(source))
                {
                    if (Directory.Exists(target))
                        Directory.Delete(target);

                    Directory.CreateSymbolicLink(target, source);
                    _logger?.Info($"Directory symlink created: {target} -> {source}");
                    return true;
                }
                else if (File.Exists(source))
                {
                    if (File.Exists(target))
                        File.Delete(target);

                    File.CreateSymbolicLink(target, source);
                    _logger?.Info($"File symlink created: {target} -> {source}");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger?.Error($"Failed to create symlink: {ex.Message}");
                return false;
            }
        }

        public async Task<List<PartitionBalance>> AnalyzeStorageBalanceAsync()
        {
            var balance = new List<PartitionBalance>();

            try
            {
                var partitions = await GetAllPartitionsAsync();

                foreach (var partition in partitions)
                {
                    var status = new PartitionBalance
                    {
                        DriveLetter = partition.DriveLetter,
                        UsagePercent = partition.UsagePercent
                    };

                    if (partition.UsagePercent < 50)
                    {
                        status.BalanceStatus = "Good";
                    }
                    else if (partition.UsagePercent < 80)
                    {
                        status.BalanceStatus = "Warning";
                        status.Recommendations.Add("Consider moving files to less-used partitions");
                    }
                    else if (partition.UsagePercent < 95)
                    {
                        status.BalanceStatus = "Critical";
                        status.Recommendations.Add("Urgent: Move files to less-used partitions or expand capacity");
                    }
                    else
                    {
                        status.BalanceStatus = "Full";
                        status.Recommendations.Add("Partition is full. Immediate action required.");
                    }

                    balance.Add(status);
                }

                _logger?.Info($"Storage balance analysis complete: {balance.Count} partitions analyzed");
            }
            catch (Exception ex)
            {
                _logger?.Error($"Error analyzing storage balance: {ex.Message}");
            }

            return balance;
        }
    }
}
