using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HELIOS.Platform.Phase10.Sandbox
{
    /// <summary>
    /// Manages sandbox snapshots and rollback capabilities
    /// </summary>
    public class SandboxSnapshotManager : ISandboxSnapshotManager
    {
        private readonly string _snapshotDirectory;
        private readonly Dictionary<string, List<SandboxSnapshot>> _snapshots;
        private bool _initialized;

        public SandboxSnapshotManager(string snapshotDirectory = null)
        {
            _snapshotDirectory = snapshotDirectory ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "snapshots");
            _snapshots = new Dictionary<string, List<SandboxSnapshot>>();
            _initialized = false;
        }

        public async Task<bool> InitializeAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (!Directory.Exists(_snapshotDirectory))
                {
                    Directory.CreateDirectory(_snapshotDirectory);
                }

                _initialized = true;
                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Snapshot manager initialization failed: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
        {
            return await Task.FromResult(_initialized && Directory.Exists(_snapshotDirectory));
        }

        public async Task ShutdownAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _snapshots.Clear();
                _initialized = false;
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Snapshot manager shutdown error: {ex.Message}");
            }
        }

        public async Task<SandboxSnapshot> CreateSnapshotAsync(SandboxInstance sandbox, string snapshotName, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!_initialized)
                {
                    await InitializeAsync(cancellationToken);
                }

                var snapshotId = Guid.NewGuid().ToString();
                var snapshotPath = Path.Combine(_snapshotDirectory, $"{sandbox.Id}_{snapshotId}");

                // Create snapshot directory
                if (!Directory.Exists(snapshotPath))
                {
                    Directory.CreateDirectory(snapshotPath);
                }

                // Create snapshot metadata file
                var metadataPath = Path.Combine(snapshotPath, "metadata.json");
                var metadata = $@"{{
  ""SnapshotId"": ""{snapshotId}"",
  ""SandboxId"": ""{sandbox.Id}"",
  ""SnapshotName"": ""{snapshotName}"",
  ""CreatedAt"": ""{DateTime.UtcNow:O}"",
  ""SandboxStatus"": ""{sandbox.Status}""
}}";
                File.WriteAllText(metadataPath, metadata);

                var snapshot = new SandboxSnapshot
                {
                    Id = snapshotId,
                    Name = snapshotName,
                    CreatedAt = DateTime.UtcNow,
                    SizeMb = 0,
                    Compressed = false,
                    Path = snapshotPath,
                    Hash = ComputeDirectoryHash(snapshotPath)
                };

                if (!_snapshots.ContainsKey(sandbox.Id))
                {
                    _snapshots[sandbox.Id] = new List<SandboxSnapshot>();
                }

                _snapshots[sandbox.Id].Add(snapshot);

                return await Task.FromResult(snapshot);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Create snapshot failed: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> RestoreFromSnapshotAsync(SandboxInstance sandbox, SandboxSnapshot snapshot, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!Directory.Exists(snapshot.Path))
                {
                    return false;
                }

                // Simulate restoring from snapshot
                // In a real implementation, this would restore VM/sandbox state
                Debug.WriteLine($"Restoring sandbox {sandbox.Id} from snapshot {snapshot.Id}");

                sandbox.Status = SandboxStatus.Created;
                sandbox.Metadata["LastRestoredSnapshot"] = snapshot.Id;
                sandbox.Metadata["RestoredAt"] = DateTime.UtcNow;

                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Restore from snapshot failed: {ex.Message}");
                return false;
            }
        }

        public async Task<IEnumerable<SandboxSnapshot>> GetSnapshotsAsync(SandboxInstance sandbox, CancellationToken cancellationToken = default)
        {
            try
            {
                if (_snapshots.ContainsKey(sandbox.Id))
                {
                    return await Task.FromResult(_snapshots[sandbox.Id].ToList());
                }

                return new List<SandboxSnapshot>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Get snapshots failed: {ex.Message}");
                return new List<SandboxSnapshot>();
            }
        }

        public async Task<bool> ScheduleSnapshotAsync(SandboxInstance sandbox, TimeSpan interval, CancellationToken cancellationToken = default)
        {
            try
            {
                if (interval.TotalSeconds <= 0)
                {
                    return false;
                }

                sandbox.Metadata["SnapshotInterval"] = interval.TotalSeconds;
                sandbox.Metadata["SnapshotScheduled"] = true;
                sandbox.Metadata["LastSnapshotScheduleTime"] = DateTime.UtcNow;

                // Start background snapshot scheduler
                _ = SnapshotSchedulerAsync(sandbox, interval, cancellationToken);

                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Schedule snapshot failed: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> CompressSnapshotAsync(SandboxSnapshot snapshot, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!Directory.Exists(snapshot.Path))
                {
                    return false;
                }

                var zipPath = snapshot.Path + ".zip";

                if (File.Exists(zipPath))
                {
                    File.Delete(zipPath);
                }

                // Create zip archive
                ZipFile.CreateFromDirectory(snapshot.Path, zipPath);

                var fileInfo = new FileInfo(zipPath);
                snapshot.SizeMb = fileInfo.Length / (1024 * 1024);
                snapshot.Compressed = true;

                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Compress snapshot failed: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> RapidRollbackAsync(SandboxInstance sandbox, CancellationToken cancellationToken = default)
        {
            try
            {
                var snapshots = _snapshots.ContainsKey(sandbox.Id) ? _snapshots[sandbox.Id] : new List<SandboxSnapshot>();

                if (snapshots.Count == 0)
                {
                    return false;
                }

                // Get the most recent snapshot
                var latestSnapshot = snapshots.OrderByDescending(s => s.CreatedAt).First();

                return await RestoreFromSnapshotAsync(sandbox, latestSnapshot, cancellationToken);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Rapid rollback failed: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteSnapshotAsync(SandboxSnapshot snapshot, CancellationToken cancellationToken = default)
        {
            try
            {
                if (Directory.Exists(snapshot.Path))
                {
                    Directory.Delete(snapshot.Path, true);
                }

                var zipPath = snapshot.Path + ".zip";
                if (File.Exists(zipPath))
                {
                    File.Delete(zipPath);
                }

                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Delete snapshot failed: {ex.Message}");
                return false;
            }
        }

        public async Task<SnapshotManagementReport> GetManagementReportAsync(SandboxInstance sandbox, CancellationToken cancellationToken = default)
        {
            try
            {
                var snapshots = _snapshots.ContainsKey(sandbox.Id) ? _snapshots[sandbox.Id] : new List<SandboxSnapshot>();

                long totalStorage = 0;
                foreach (var snapshot in snapshots)
                {
                    if (Directory.Exists(snapshot.Path))
                    {
                        totalStorage += GetDirectorySizeBytes(snapshot.Path) / (1024 * 1024);
                    }

                    var zipPath = snapshot.Path + ".zip";
                    if (File.Exists(zipPath))
                    {
                        var fileInfo = new FileInfo(zipPath);
                        totalStorage += fileInfo.Length / (1024 * 1024);
                    }
                }

                var report = new SnapshotManagementReport
                {
                    TotalSnapshots = snapshots.Count,
                    TotalStorageMb = totalStorage,
                    Snapshots = snapshots,
                    GeneratedAt = DateTime.UtcNow
                };

                return await Task.FromResult(report);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Get management report failed: {ex.Message}");
                return new SnapshotManagementReport
                {
                    TotalSnapshots = 0,
                    TotalStorageMb = 0,
                    Snapshots = new List<SandboxSnapshot>(),
                    GeneratedAt = DateTime.UtcNow
                };
            }
        }

        // ========== Private Helper Methods ==========

        private async Task SnapshotSchedulerAsync(SandboxInstance sandbox, TimeSpan interval, CancellationToken cancellationToken)
        {
            try
            {
                while (sandbox.Metadata.ContainsKey("SnapshotScheduled") &&
                       (bool)sandbox.Metadata["SnapshotScheduled"] &&
                       !cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(interval, cancellationToken);

                    var snapshotName = $"Auto_{DateTime.UtcNow:yyyyMMdd_HHmmss}";
                    await CreateSnapshotAsync(sandbox, snapshotName, cancellationToken);

                    sandbox.Metadata["LastSnapshotTime"] = DateTime.UtcNow;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Snapshot scheduler failed: {ex.Message}");
            }
        }

        private string ComputeDirectoryHash(string directoryPath)
        {
            try
            {
                if (!Directory.Exists(directoryPath))
                {
                    return "empty";
                }

                var files = Directory.GetFiles(directoryPath, "*.*", SearchOption.AllDirectories);
                return files.Length.ToString();
            }
            catch
            {
                return "unknown";
            }
        }

        private long GetDirectorySizeBytes(string directoryPath)
        {
            try
            {
                var directoryInfo = new DirectoryInfo(directoryPath);
                return directoryInfo.EnumerateFiles("*.*", SearchOption.AllDirectories).Sum(f => f.Length);
            }
            catch
            {
                return 0;
            }
        }
    }
}
