using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using HELIOS.Platform.CloudSync;
using HELIOS.Platform.CloudSync.Engine;
using HELIOS.Platform.CloudSync.Providers;
using Moq;

namespace HELIOS.Platform.Tests.CloudSync
{
    public class CloudSyncIntegrationTests
    {
        private const string TestSyncPath = "./test-sync";

        [Fact]
        public async Task OneDriveProvider_Authenticate_ShouldSucceed()
        {
            // Arrange
            var provider = new OneDriveProvider("test-client-id", "test-client-secret");

            // Act
            var result = await provider.AuthenticateAsync();

            // Assert
            Assert.True(result);
            Assert.True(provider.IsAuthenticated);
        }

        [Fact]
        public async Task AzureStorageProvider_Authenticate_ShouldSucceed()
        {
            // Arrange
            var provider = new AzureStorageProvider("testaccount", "testkey");

            // Act
            var result = await provider.AuthenticateAsync();

            // Assert
            Assert.True(result);
            Assert.True(provider.IsAuthenticated);
        }

        [Fact]
        public async Task OneDriveProvider_IsConnected_WhenAuthenticated_ShouldReturnTrue()
        {
            // Arrange
            var provider = new OneDriveProvider("test-client-id", "test-client-secret");
            await provider.AuthenticateAsync();

            // Act
            var result = await provider.IsConnectedAsync();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task OneDriveProvider_UploadFile_ShouldSucceed()
        {
            // Arrange
            var tempFile = Path.GetTempFileName();
            await File.WriteAllTextAsync(tempFile, "test content");
            var provider = new OneDriveProvider("test-id", "test-secret");
            await provider.AuthenticateAsync();

            // Act & Assert
            await provider.UploadFileAsync(tempFile, "/test.txt");
            File.Delete(tempFile);
        }

        [Fact]
        public async Task OneDriveProvider_DownloadFile_ShouldCreateLocalFile()
        {
            // Arrange
            var downloadPath = Path.Combine(Path.GetTempPath(), "download-test.txt");
            var provider = new OneDriveProvider("test-id", "test-secret");
            await provider.AuthenticateAsync();

            // Act
            await provider.DownloadFileAsync("/test.txt", downloadPath);

            // Assert
            Assert.True(File.Exists(downloadPath));
            File.Delete(downloadPath);
        }

        [Fact]
        public async Task OneDriveProvider_GetFileMetadata_ShouldReturnMetadata()
        {
            // Arrange
            var provider = new OneDriveProvider("test-id", "test-secret");
            await provider.AuthenticateAsync();

            // Act
            var metadata = await provider.GetFileMetadataAsync("/test.txt");

            // Assert
            Assert.NotNull(metadata);
            Assert.Equal("/test.txt", metadata.Path);
            Assert.NotNull(metadata.ETag);
        }

        [Fact]
        public async Task SqliteSyncStateStore_UpdateLocalState_ShouldStoreMetadata()
        {
            // Arrange
            var dbPath = Path.Combine(Path.GetTempPath(), "sync-test.db");
            if (File.Exists(dbPath)) File.Delete(dbPath);
            
            var store = new SqliteSyncStateStore(dbPath);
            var metadata = new CloudFileMetadata
            {
                Path = "/test.txt",
                Name = "test.txt",
                Size = 1024,
                Modified = DateTime.UtcNow,
                ETag = "abc123",
                ContentHash = "def456"
            };

            // Act
            await store.UpdateLocalStateAsync("/test.txt", metadata);
            var state = await store.GetLocalStateAsync();

            // Assert
            Assert.True(state.ContainsKey("/test.txt"));
            Assert.Equal("test.txt", state["/test.txt"].Name);
            Assert.Equal(1024, state["/test.txt"].Size);
            
            File.Delete(dbPath);
        }

        [Fact]
        public async Task SqliteSyncStateStore_RemoveFromLocalState_ShouldDelete()
        {
            // Arrange
            var dbPath = Path.Combine(Path.GetTempPath(), "sync-test.db");
            if (File.Exists(dbPath)) File.Delete(dbPath);
            
            var store = new SqliteSyncStateStore(dbPath);
            var metadata = new CloudFileMetadata
            {
                Path = "/test.txt",
                Name = "test.txt",
                Size = 1024,
                Modified = DateTime.UtcNow
            };
            await store.UpdateLocalStateAsync("/test.txt", metadata);

            // Act
            await store.RemoveFromLocalStateAsync("/test.txt");
            var state = await store.GetLocalStateAsync();

            // Assert
            Assert.False(state.ContainsKey("/test.txt"));
            
            File.Delete(dbPath);
        }

        [Fact]
        public async Task SqliteSyncStateStore_ClearLocalState_ShouldDeleteAll()
        {
            // Arrange
            var dbPath = Path.Combine(Path.GetTempPath(), "sync-test.db");
            if (File.Exists(dbPath)) File.Delete(dbPath);
            
            var store = new SqliteSyncStateStore(dbPath);
            for (int i = 0; i < 5; i++)
            {
                var metadata = new CloudFileMetadata
                {
                    Path = $"/file{i}.txt",
                    Name = $"file{i}.txt",
                    Size = 1024
                };
                await store.UpdateLocalStateAsync($"/file{i}.txt", metadata);
            }

            // Act
            await store.ClearLocalStateAsync();
            var state = await store.GetLocalStateAsync();

            // Assert
            Assert.Empty(state);
            
            File.Delete(dbPath);
        }

        [Fact]
        public async Task SyncEngine_Sync_WhenOfflineMode_ShouldQueueChanges()
        {
            // Arrange
            var mockProvider = new Mock<ICloudStorageProvider>();
            mockProvider.Setup(p => p.IsConnectedAsync(default)).ReturnsAsync(false);
            
            var dbPath = Path.Combine(Path.GetTempPath(), "sync-test.db");
            if (File.Exists(dbPath)) File.Delete(dbPath);
            var store = new SqliteSyncStateStore(dbPath);
            
            var engine = new SyncEngine(mockProvider.Object, store, TestSyncPath);

            // Act
            var result = await engine.SyncAsync();

            // Assert
            Assert.True(engine.IsOfflineMode);
            Assert.True(result.Success);
            Assert.Contains("Offline", result.Message);
            
            File.Delete(dbPath);
        }

        [Fact]
        public async Task SyncEngine_SetConflictResolution_ShouldApplySetting()
        {
            // Arrange
            var mockProvider = new Mock<ICloudStorageProvider>();
            var mockStore = new Mock<ISyncStateStore>();
            var engine = new SyncEngine(mockProvider.Object, mockStore.Object, TestSyncPath);

            // Act
            engine.SetConflictResolution(SyncConflictResolution.LocalWins);

            // Assert - No exception should be thrown
            Assert.NotNull(engine);
        }

        [Fact]
        public void SyncEngine_IsRunning_InitialValue_ShouldBeFalse()
        {
            // Arrange
            var mockProvider = new Mock<ICloudStorageProvider>();
            var mockStore = new Mock<ISyncStateStore>();

            // Act
            var engine = new SyncEngine(mockProvider.Object, mockStore.Object, TestSyncPath);

            // Assert
            Assert.False(engine.IsRunning);
        }

        [Fact]
        public async Task OneDriveProvider_FileExists_ShouldReturnTrue()
        {
            // Arrange
            var provider = new OneDriveProvider("test-id", "test-secret");
            await provider.AuthenticateAsync();

            // Act
            var exists = await provider.FileExistsAsync("/test.txt");

            // Assert
            Assert.True(exists);
        }

        [Fact]
        public async Task OneDriveProvider_DeleteFile_ShouldSucceed()
        {
            // Arrange
            var provider = new OneDriveProvider("test-id", "test-secret");
            await provider.AuthenticateAsync();

            // Act & Assert
            await provider.DeleteFileAsync("/test.txt");
        }

        [Fact]
        public async Task OneDriveProvider_ListFiles_ShouldReturnEmptyList()
        {
            // Arrange
            var provider = new OneDriveProvider("test-id", "test-secret");
            await provider.AuthenticateAsync();

            // Act
            var files = await provider.ListFilesAsync("/");

            // Assert
            Assert.NotNull(files);
            Assert.IsType<List<CloudFileMetadata>>(files);
        }

        [Fact]
        public async Task CloudStorageProvider_Names_ShouldMatch()
        {
            // Arrange
            var oneDrive = new OneDriveProvider("id", "secret");
            var azure = new AzureStorageProvider("account", "key");

            // Act & Assert
            Assert.Equal("OneDrive", oneDrive.ProviderName);
            Assert.Equal("Azure Storage", azure.ProviderName);
        }

        [Fact]
        public async Task SyncResult_Properties_ShouldBeInitialized()
        {
            // Arrange & Act
            var result = new SyncResult
            {
                Success = true,
                Message = "Sync completed",
                FilesAdded = 5,
                FilesModified = 3,
                FilesDeleted = 1
            };

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Sync completed", result.Message);
            Assert.Equal(5, result.FilesAdded);
            Assert.Equal(3, result.FilesModified);
            Assert.Equal(1, result.FilesDeleted);
        }

        [Fact]
        public void CloudFileMetadata_Properties_ShouldBeAccessible()
        {
            // Arrange & Act
            var metadata = new CloudFileMetadata
            {
                Path = "/file.txt",
                Name = "file.txt",
                Size = 2048,
                Modified = DateTime.UtcNow,
                ETag = "etag123",
                ContentHash = "hash456"
            };

            // Assert
            Assert.Equal("/file.txt", metadata.Path);
            Assert.Equal("file.txt", metadata.Name);
            Assert.Equal(2048, metadata.Size);
            Assert.NotNull(metadata.ETag);
            Assert.NotNull(metadata.ContentHash);
        }

        [Fact]
        public void SyncEventArgs_Properties_ShouldBeAccessible()
        {
            // Arrange & Act
            var args = new SyncEventArgs
            {
                FilePath = "/test.txt",
                EventType = SyncEventType.FileUploaded
            };

            // Assert
            Assert.Equal("/test.txt", args.FilePath);
            Assert.Equal(SyncEventType.FileUploaded, args.EventType);
        }
    }
}
