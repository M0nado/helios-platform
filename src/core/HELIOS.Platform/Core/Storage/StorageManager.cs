using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HELIOS.Platform.Core.Storage
{
    /// <summary>
    /// Disk and partition management system.
    /// </summary>
    public class StorageManager
    {
        private readonly Core.Logging.ILogger _logger;

        public StorageManager()
        {
            _logger = ServiceContainer.Instance.GetService<Core.Logging.ILogger>();
        }

        /// <summary>
        /// Get information about all disk drives.
        /// </summary>
        public async Task<List<DiskInfo>> GetDiskInfoAsync()
        {
            try
            {
                var drives = new List<DiskInfo>();
                foreach (var drive in DriveInfo.GetDrives())
                {
                    if (drive.IsReady)
                    {
                        drives.Add(new DiskInfo
                        {
                            DriveLetter = drive.Name,
                            Name = drive.VolumeLabel,
                            FileSystem = drive.DriveFormat,
                            TotalSize = drive.TotalSize,
                            AvailableSpace = drive.AvailableFreeSpace,
                            UsedSpace = drive.TotalSize - drive.AvailableFreeSpace,
                            DriveType = drive.DriveType.ToString()
                        });
                    }
                }

                _logger?.Debug($"Retrieved info for {drives.Count} drives");
                await Task.Delay(10);
                return drives;
            }
            catch (Exception ex)
            {
                _logger?.Error("Failed to get disk information", ex);
                throw;
            }
        }

        /// <summary>
        /// Get disk usage percentage for a drive.
        /// </summary>
        public async Task<double> GetDiskUsagePercentAsync(string driveLetter)
        {
            try
            {
                var drive = DriveInfo.GetDrives().FirstOrDefault(d => d.Name == driveLetter);
                if (drive == null || !drive.IsReady)
                    return -1;

                double percentage = ((double)drive.TotalSize - drive.AvailableFreeSpace) / drive.TotalSize * 100;
                _logger?.Debug($"Disk usage for {driveLetter}: {percentage:F1}%");
                await Task.Delay(10);
                return percentage;
            }
            catch (Exception ex)
            {
                _logger?.Error("Failed to calculate disk usage percentage", ex);
                throw;
            }
        }

        /// <summary>
        /// Get largest files in a directory.
        /// </summary>
        public async Task<List<FileInfo>> GetLargestFilesAsync(string path, int count = 10)
        {
            try
            {
                var dir = new DirectoryInfo(path);
                var files = dir.GetFiles("*", SearchOption.AllDirectories)
                    .OrderByDescending(f => f.Length)
                    .Take(count)
                    .ToList();

                _logger?.Debug($"Found {files.Count} largest files in {path}");
                await Task.Delay(10);
                return files;
            }
            catch (Exception ex)
            {
                _logger?.Error($"Failed to get largest files from {path}", ex);
                return new List<FileInfo>();
            }
        }

        /// <summary>
        /// Get directory size in bytes.
        /// </summary>
        public async Task<long> GetDirectorySizeAsync(string path)
        {
            try
            {
                var dir = new DirectoryInfo(path);
                long totalSize = 0;

                if (dir.Exists)
                {
                    totalSize = dir.GetFiles("*", SearchOption.AllDirectories)
                        .Sum(f => f.Length);
                }

                _logger?.Debug($"Directory size: {totalSize} bytes");
                await Task.Delay(10);
                return totalSize;
            }
            catch (Exception ex)
            {
                _logger?.Error($"Failed to calculate directory size for {path}", ex);
                return -1;
            }
        }
    }

    /// <summary>
    /// Disk drive information data model.
    /// </summary>
    public class DiskInfo
    {
        public string DriveLetter { get; set; }
        public string Name { get; set; }
        public string FileSystem { get; set; }
        public long TotalSize { get; set; }
        public long AvailableSpace { get; set; }
        public long UsedSpace { get; set; }
        public string DriveType { get; set; }

        public double UsagePercent => TotalSize > 0 ? (UsedSpace / (double)TotalSize) * 100 : 0;
    }
}
