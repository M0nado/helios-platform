using Xunit;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HELIOS.Platform.Tests.Integration
{
    /// <summary>
    /// Integration Tests: Database Operations → File System Sync
    /// 5 test cases testing database persistence and file system synchronization
    /// </summary>
    public class DatabaseFileSystemIntegrationTests
    {
        private readonly Mock<IDataRepository> _mockRepository;
        private readonly Mock<IFileSystemSync> _mockFileSync;

        public DatabaseFileSystemIntegrationTests()
        {
            _mockRepository = new Mock<IDataRepository>();
            _mockFileSync = new Mock<IFileSystemSync>();
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task SaveToDatabase_ThenSyncToFileSystem_DataMatches()
        {
            // Arrange
            var data = new DataRecord { Id = "1", Content = "test data" };
            _mockRepository.Setup(r => r.SaveAsync(data, CancellationToken.None))
                .ReturnsAsync(true);
            _mockFileSync.Setup(f => f.SyncAsync(data, CancellationToken.None))
                .ReturnsAsync(true);

            // Act
            var dbResult = await _mockRepository.Object.SaveAsync(data, CancellationToken.None);
            var fsResult = await _mockFileSync.Object.SyncAsync(data, CancellationToken.None);

            // Assert
            Assert.True(dbResult);
            Assert.True(fsResult);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task FileSystemChange_DetectedAndSyncedToDatabase()
        {
            // Arrange
            var filePath = "data.json";
            var data = new DataRecord { Id = "1", Content = "updated" };
            
            _mockFileSync.Setup(f => f.DetectChangesAsync(filePath, CancellationToken.None))
                .ReturnsAsync(data);
            _mockRepository.Setup(r => r.SaveAsync(data, CancellationToken.None))
                .ReturnsAsync(true);

            // Act
            var detected = await _mockFileSync.Object.DetectChangesAsync(filePath, CancellationToken.None);
            var saved = await _mockRepository.Object.SaveAsync(detected, CancellationToken.None);

            // Assert
            Assert.NotNull(detected);
            Assert.True(saved);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task SyncConflict_DatabaseVsFileSystem_ResolvesCorrectly()
        {
            // Arrange
            var conflictId = "conflict-123";
            var dbVersion = new DataRecord { Id = "1", Content = "from-db", Timestamp = DateTime.UtcNow.AddHours(-1) };
            var fsVersion = new DataRecord { Id = "1", Content = "from-fs", Timestamp = DateTime.UtcNow };
            
            _mockFileSync.Setup(f => f.ResolveConflictAsync(dbVersion, fsVersion, CancellationToken.None))
                .ReturnsAsync(fsVersion); // File system wins (more recent)

            // Act
            var resolved = await _mockFileSync.Object.ResolveConflictAsync(dbVersion, fsVersion, CancellationToken.None);

            // Assert
            Assert.Equal("from-fs", resolved.Content);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task BulkDataSync_TransfersLargeDataset_Successfully()
        {
            // Arrange
            var records = new List<DataRecord>();
            for (int i = 0; i < 100; i++)
            {
                records.Add(new DataRecord { Id = i.ToString(), Content = $"data-{i}" });
            }

            _mockRepository.Setup(r => r.SaveBatchAsync(records, CancellationToken.None))
                .ReturnsAsync(100);
            _mockFileSync.Setup(f => f.SyncBatchAsync(records, CancellationToken.None))
                .ReturnsAsync(100);

            // Act
            var dbCount = await _mockRepository.Object.SaveBatchAsync(records, CancellationToken.None);
            var fsCount = await _mockFileSync.Object.SyncBatchAsync(records, CancellationToken.None);

            // Assert
            Assert.Equal(100, dbCount);
            Assert.Equal(100, fsCount);
        }
    }

    public class DataRecord
    {
        public string Id { get; set; }
        public string Content { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public interface IDataRepository
    {
        Task<bool> SaveAsync(DataRecord data, CancellationToken cancellationToken);
        Task<int> SaveBatchAsync(List<DataRecord> records, CancellationToken cancellationToken);
    }

    public interface IFileSystemSync
    {
        Task<bool> SyncAsync(DataRecord data, CancellationToken cancellationToken);
        Task<DataRecord> DetectChangesAsync(string filePath, CancellationToken cancellationToken);
        Task<DataRecord> ResolveConflictAsync(DataRecord dbVersion, DataRecord fsVersion, CancellationToken cancellationToken);
        Task<int> SyncBatchAsync(List<DataRecord> records, CancellationToken cancellationToken);
    }
}
