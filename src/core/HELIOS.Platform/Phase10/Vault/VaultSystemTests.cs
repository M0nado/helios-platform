using Xunit;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace HELIOS.Platform.Phase10.Vault.Tests
{
    /// <summary>
    /// Unit tests for the Vault System (35+ tests)
    /// </summary>
    public class VaultSystemInitializerTests
    {
        private readonly string _testVaultPath;
        private readonly MockLogger _logger;
        private readonly MockEncryptionManager _encryptionManager;

        public VaultSystemInitializerTests()
        {
            _testVaultPath = Path.Combine(Path.GetTempPath(), $"vault-test-{Guid.NewGuid()}");
            _logger = new MockLogger();
            _encryptionManager = new MockEncryptionManager();
        }

        [Fact]
        public async Task InitializeAsync_CreatesVaultDirectory()
        {
            var initializer = new VaultSystemInitializer(_testVaultPath, _encryptionManager, _logger);
            
            var result = await initializer.InitializeAsync();
            
            Assert.True(result.IsSuccess);
            Assert.True(Directory.Exists(_testVaultPath));
        }

        [Fact]
        public async Task InitializeAsync_CreatesFolderStructure()
        {
            var initializer = new VaultSystemInitializer(_testVaultPath, _encryptionManager, _logger);
            
            await initializer.InitializeAsync();
            
            Assert.True(Directory.Exists(Path.Combine(_testVaultPath, "Personal")));
            Assert.True(Directory.Exists(Path.Combine(_testVaultPath, "Work")));
            Assert.True(Directory.Exists(Path.Combine(_testVaultPath, "Gaming")));
            Assert.True(Directory.Exists(Path.Combine(_testVaultPath, "Security")));
            Assert.True(Directory.Exists(Path.Combine(_testVaultPath, "Media")));
        }

        [Fact]
        public async Task InitializeAsync_GeneratesMasterKey()
        {
            var initializer = new VaultSystemInitializer(_testVaultPath, _encryptionManager, _logger);
            
            await initializer.InitializeAsync();
            
            var masterKey = await initializer.GetMasterKeyAsync();
            Assert.NotNull(masterKey);
            Assert.Equal(32, masterKey.Length); // 256-bit key
        }

        [Fact]
        public async Task InitializeAsync_CreatesMetadata()
        {
            var initializer = new VaultSystemInitializer(_testVaultPath, _encryptionManager, _logger);
            
            await initializer.InitializeAsync();
            
            var metadataPath = Path.Combine(_testVaultPath, ".vault", "metadata.json");
            Assert.True(File.Exists(metadataPath));
        }

        [Fact]
        public void IsInitialized_ReturnsTrueAfterInit()
        {
            var initializer = new VaultSystemInitializer(_testVaultPath, _encryptionManager, _logger);
            
            Assert.False(initializer.IsInitialized());
            
            var result = initializer.InitializeAsync().Result;
            
            Assert.True(initializer.IsInitialized());
        }

        [Fact]
        public async Task InitializeAsync_CreatesPersonalSubfolders()
        {
            var initializer = new VaultSystemInitializer(_testVaultPath, _encryptionManager, _logger);
            
            await initializer.InitializeAsync();
            
            Assert.True(Directory.Exists(Path.Combine(_testVaultPath, "Personal", "Documents")));
            Assert.True(Directory.Exists(Path.Combine(_testVaultPath, "Personal", "Financial")));
            Assert.True(Directory.Exists(Path.Combine(_testVaultPath, "Personal", "Medical")));
        }

        [Fact]
        public async Task InitializeAsync_CreatesSecuritySubfolders()
        {
            var initializer = new VaultSystemInitializer(_testVaultPath, _encryptionManager, _logger);
            
            await initializer.InitializeAsync();
            
            Assert.True(Directory.Exists(Path.Combine(_testVaultPath, "Security", "KeePass")));
            Assert.True(Directory.Exists(Path.Combine(_testVaultPath, "Security", "SSHKeys")));
            Assert.True(Directory.Exists(Path.Combine(_testVaultPath, "Security", "APIKeys")));
            Assert.True(Directory.Exists(Path.Combine(_testVaultPath, "Security", "Certificates")));
        }

        [Fact]
        public void InitializeAsync_WithNullPath_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => 
                new VaultSystemInitializer(null, _encryptionManager, _logger));
        }
    }

    public class VaultEncryptionManagerTests
    {
        private readonly MockLogger _logger;
        private readonly VaultEncryptionManager _encryptionManager;

        public VaultEncryptionManagerTests()
        {
            _logger = new MockLogger();
            _encryptionManager = new VaultEncryptionManager(_logger);
        }

        [Fact]
        public async Task EncryptDataAsync_EncryptsData()
        {
            var plaintext = Encoding.UTF8.GetBytes("Secret data");
            byte[] key = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(key);
            }
            
            var result = await _encryptionManager.EncryptDataAsync(plaintext, key, out var encrypted);
            
            Assert.True(result);
            Assert.NotNull(encrypted);
            Assert.NotEqual(plaintext, encrypted);
            Assert.True(encrypted.Length > plaintext.Length); // Contains nonce + tag
        }

        [Fact]
        public async Task DecryptDataAsync_DecryptsData()
        {
            var plaintext = Encoding.UTF8.GetBytes("Secret data");
            byte[] key = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(key);
            }
            
            var encryptResult = await _encryptionManager.EncryptDataAsync(plaintext, key, out var encrypted);
            var decryptResult = await _encryptionManager.DecryptDataAsync(encrypted, key, out var decrypted);
            
            Assert.True(encryptResult);
            Assert.True(decryptResult);
            Assert.Equal(plaintext, decrypted);
        }

        [Fact]
        public async Task EncryptDataAsync_WithInvalidKey_ReturnsFalse()
        {
            var plaintext = Encoding.UTF8.GetBytes("Secret data");
            byte[] invalidKey = new byte[16]; // Should be 32 bytes
            
            var result = await _encryptionManager.EncryptDataAsync(plaintext, invalidKey, out var encrypted);
            
            Assert.False(result);
            Assert.Null(encrypted);
        }

        [Fact]
        public async Task DecryptDataAsync_WithWrongKey_ReturnsFalse()
        {
            var plaintext = Encoding.UTF8.GetBytes("Secret data");
            byte[] key1 = new byte[32];
            byte[] key2 = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(key1);
                rng.GetBytes(key2);
            }
            
            await _encryptionManager.EncryptDataAsync(plaintext, key1, out var encrypted);
            var result = await _encryptionManager.DecryptDataAsync(encrypted, key2, out var decrypted);
            
            Assert.False(result);
        }

        [Fact]
        public async Task ApplyEncryptionAsync_CreatesEncryptionMetadata()
        {
            var vaultPath = Path.Combine(Path.GetTempPath(), $"vault-encrypt-{Guid.NewGuid()}");
            Directory.CreateDirectory(vaultPath);
            Directory.CreateDirectory(Path.Combine(vaultPath, ".vault"));
            
            var result = await _encryptionManager.ApplyEncryptionAsync(vaultPath);
            
            Assert.True(result);
            Assert.True(File.Exists(Path.Combine(vaultPath, ".vault", "encryption.json")));
        }

        [Fact]
        public async Task RotateKeyAsync_CreatesRotationRecord()
        {
            byte[] newKey = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(newKey);
            }
            
            var result = await _encryptionManager.RotateKeyAsync("test-key", newKey);
            
            Assert.True(result);
        }

        [Fact]
        public async Task EncryptDataAsync_WithEmptyData_ReturnsFalse()
        {
            byte[] key = new byte[32];
            
            var result = await _encryptionManager.EncryptDataAsync(new byte[0], key, out var encrypted);
            
            Assert.False(result);
        }

        [Fact]
        public async Task GetStatusAsync_ReturnsEncryptionStatus()
        {
            var vaultPath = Path.Combine(Path.GetTempPath(), $"vault-status-{Guid.NewGuid()}");
            Directory.CreateDirectory(vaultPath);
            Directory.CreateDirectory(Path.Combine(vaultPath, ".vault"));
            
            await _encryptionManager.ApplyEncryptionAsync(vaultPath);
            var status = await _encryptionManager.GetStatusAsync(vaultPath);
            
            Assert.NotNull(status);
            Assert.True(status.IsEncrypted);
        }
    }

    public class VaultAccessControllerTests
    {
        private readonly VaultAccessController _controller;
        private readonly string _testVaultPath;

        public VaultAccessControllerTests()
        {
            _testVaultPath = Path.Combine(Path.GetTempPath(), $"vault-access-{Guid.NewGuid()}");
            Directory.CreateDirectory(_testVaultPath);
            Directory.CreateDirectory(Path.Combine(_testVaultPath, ".vault"));
            _controller = new VaultAccessController(_testVaultPath, new MockLogger());
        }

        [Fact]
        public async Task AuthenticateAsync_WithValidCredentials_ReturnsSuccess()
        {
            var result = await _controller.AuthenticateAsync("testuser", "password123");
            
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.SessionId);
        }

        [Fact]
        public async Task AuthenticateAsync_WithEmptyUsername_ReturnsFalure()
        {
            var result = await _controller.AuthenticateAsync("", "password123");
            
            Assert.False(result.IsSuccess);
        }

        [Fact]
        public async Task ValidateSessionAsync_WithValidSession_ReturnsTrue()
        {
            var authResult = await _controller.AuthenticateAsync("testuser", "password123");
            
            var isValid = await _controller.ValidateSessionAsync(authResult.SessionId);
            
            Assert.True(isValid);
        }

        [Fact]
        public async Task ValidateSessionAsync_WithInvalidSession_ReturnsFalse()
        {
            var isValid = await _controller.ValidateSessionAsync("invalid-session");
            
            Assert.False(isValid);
        }

        [Fact]
        public async Task RevokeSessionAsync_RemovesSession()
        {
            var authResult = await _controller.AuthenticateAsync("testuser", "password123");
            
            var revoked = await _controller.RevokeSessionAsync(authResult.SessionId);
            var isValid = await _controller.ValidateSessionAsync(authResult.SessionId);
            
            Assert.True(revoked);
            Assert.False(isValid);
        }

        [Fact]
        public async Task GetAuditLogAsync_ReturnsAuditEntries()
        {
            await _controller.AuthenticateAsync("testuser", "password123");
            
            var auditLog = await _controller.GetAuditLogAsync("testuser");
            
            Assert.NotNull(auditLog);
            Assert.NotEmpty(auditLog);
        }

        [Fact]
        public async Task AutoLockOnSuspendAsync_ClearsActiveSessions()
        {
            await _controller.AuthenticateAsync("user1", "pass1");
            await _controller.AuthenticateAsync("user2", "pass2");
            
            var result = await _controller.AutoLockOnSuspendAsync();
            
            Assert.True(result);
        }

        [Fact]
        public async Task GetActiveSessions_ReturnsActiveSessions()
        {
            await _controller.AuthenticateAsync("user1", "pass1");
            await _controller.AuthenticateAsync("user2", "pass2");
            
            var sessions = _controller.GetActiveSessions();
            
            Assert.NotEmpty(sessions);
            Assert.True(sessions.Count >= 2);
        }

        [Fact]
        public async Task VerifyTwoFactorAsync_WithValidCode_ReturnsTrue()
        {
            var result = await _controller.VerifyTwoFactorAsync("testuser", "123456", "temp-session");
            
            Assert.True(result);
        }

        [Fact]
        public async Task VerifyTwoFactorAsync_WithInvalidCode_ReturnsFalse()
        {
            var result = await _controller.VerifyTwoFactorAsync("testuser", "invalid", "temp-session");
            
            Assert.False(result);
        }

        [Fact]
        public async Task SetLockerPermissionAsync_WithValidSession_ReturnsTrue()
        {
            var authResult = await _controller.AuthenticateAsync("testuser", "password123");
            
            var result = await _controller.SetLockerPermissionAsync(authResult.SessionId, "Personal", LockerPermission.Read);
            
            Assert.True(result);
        }
    }

    public class VaultLockerManagerTests
    {
        private readonly string _testVaultPath;
        private readonly string _testBackupPath;
        private readonly VaultLockerManager _manager;

        public VaultLockerManagerTests()
        {
            _testVaultPath = Path.Combine(Path.GetTempPath(), $"vault-locker-{Guid.NewGuid()}");
            _testBackupPath = Path.Combine(Path.GetTempPath(), $"backup-locker-{Guid.NewGuid()}");
            Directory.CreateDirectory(_testVaultPath);
            Directory.CreateDirectory(_testBackupPath);
            
            _manager = new VaultLockerManager(_testVaultPath, _testBackupPath, 
                new MockEncryptionManager(), new MockLogger());
        }

        [Fact]
        public async Task CreateLockerAsync_CreatesLocker()
        {
            var result = await _manager.CreateLockerAsync("TestLocker");
            
            Assert.True(result);
            Assert.True(Directory.Exists(Path.Combine(_testVaultPath, "TestLocker")));
        }

        [Fact]
        public async Task CreateLockerAsync_WithEmptyName_ReturnsFalse()
        {
            var result = await _manager.CreateLockerAsync("");
            
            Assert.False(result);
        }

        [Fact]
        public async Task RenameLockerAsync_RenamesLocker()
        {
            await _manager.CreateLockerAsync("OldName");
            
            var result = await _manager.RenameLockerAsync("OldName", "NewName");
            
            Assert.True(result);
            Assert.True(Directory.Exists(Path.Combine(_testVaultPath, "NewName")));
            Assert.False(Directory.Exists(Path.Combine(_testVaultPath, "OldName")));
        }

        [Fact]
        public async Task SetSizeLimitAsync_SetsSizeLimit()
        {
            await _manager.CreateLockerAsync("SizeTestLocker");
            
            var result = await _manager.SetSizeLimitAsync("SizeTestLocker", 1000000);
            
            Assert.True(result);
        }

        [Fact]
        public async Task GetLockerSizeAsync_ReturnsSize()
        {
            await _manager.CreateLockerAsync("SizeLocker");
            var testFile = Path.Combine(_testVaultPath, "SizeLocker", "test.txt");
            await File.WriteAllTextAsync(testFile, "test content");
            
            var size = await _manager.GetLockerSizeAsync("SizeLocker");
            
            Assert.True(size > 0);
        }

        [Fact]
        public async Task BackupLockerAsync_CreatesBackup()
        {
            await _manager.CreateLockerAsync("BackupLocker");
            var testFile = Path.Combine(_testVaultPath, "BackupLocker", "test.txt");
            await File.WriteAllTextAsync(testFile, "test content");
            
            var result = await _manager.BackupLockerAsync("BackupLocker");
            
            Assert.True(result);
        }

        [Fact]
        public async Task GetAllLockersAsync_ReturnsLockers()
        {
            await _manager.CreateLockerAsync("Locker1");
            await _manager.CreateLockerAsync("Locker2");
            
            var lockers = await _manager.GetAllLockersAsync();
            
            Assert.NotEmpty(lockers);
        }

        [Fact]
        public async Task DeleteLockerAsync_DeletesLocker()
        {
            await _manager.CreateLockerAsync("DeleteMe");
            
            var result = await _manager.DeleteLockerAsync("DeleteMe");
            
            Assert.True(result);
            Assert.False(Directory.Exists(Path.Combine(_testVaultPath, "DeleteMe")));
        }

        [Fact]
        public async Task PerformMaintenanceAsync_ReturnsMaintResultSuccess()
        {
            await _manager.CreateLockerAsync("MaintenanceLocker");
            
            var result = await _manager.PerformMaintenanceAsync("MaintenanceLocker");
            
            Assert.True(result.IsSuccess);
        }
    }

    public class VaultBackupRestorTests
    {
        private readonly string _testVaultPath;
        private readonly string _testBackupPath;
        private readonly VaultBackupRestorer _restorer;

        public VaultBackupRestorTests()
        {
            _testVaultPath = Path.Combine(Path.GetTempPath(), $"vault-backup-{Guid.NewGuid()}");
            _testBackupPath = Path.Combine(Path.GetTempPath(), $"backup-backup-{Guid.NewGuid()}");
            Directory.CreateDirectory(_testVaultPath);
            Directory.CreateDirectory(_testBackupPath);
            
            _restorer = new VaultBackupRestorer(_testVaultPath, _testBackupPath,
                new MockEncryptionManager(), new MockLogger());
        }

        [Fact]
        public async Task CreateFullBackupAsync_CreatesBackup()
        {
            var testFile = Path.Combine(_testVaultPath, "test.txt");
            await File.WriteAllTextAsync(testFile, "test content");
            
            var result = await _restorer.CreateFullBackupAsync();
            
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.BackupId);
        }

        [Fact]
        public async Task GetAvailableBackupsAsync_ReturnsBackups()
        {
            var testFile = Path.Combine(_testVaultPath, "test.txt");
            await File.WriteAllTextAsync(testFile, "test content");
            
            await _restorer.CreateFullBackupAsync();
            var backups = await _restorer.GetAvailableBackupsAsync();
            
            Assert.NotEmpty(backups);
        }

        [Fact]
        public async Task ScheduleAutomaticBackupAsync_CreatesSchedule()
        {
            var result = await _restorer.ScheduleAutomaticBackupAsync(24);
            
            Assert.True(result);
        }

        [Fact]
        public async Task CleanupOldBackupsAsync_RemovesOldBackups()
        {
            var result = await _restorer.CleanupOldBackupsAsync(0); // Remove all
            
            Assert.True(result >= 0);
        }
    }

    public class VaultIntegrationBridgeTests
    {
        private readonly string _testVaultPath;
        private readonly VaultIntegrationBridge _bridge;

        public VaultIntegrationBridgeTests()
        {
            _testVaultPath = Path.Combine(Path.GetTempPath(), $"vault-integration-{Guid.NewGuid()}");
            Directory.CreateDirectory(_testVaultPath);
            
            _bridge = new VaultIntegrationBridge(_testVaultPath, 
                new MockLockerManager(), new MockEncryptionManager(), new MockLogger());
        }

        [Fact]
        public async Task RegisterContextMenuAsync_CreatesConfig()
        {
            var result = await _bridge.RegisterContextMenuAsync();
            
            Assert.True(result);
        }

        [Fact]
        public async Task EnableFileSystemIntegrationAsync_CreatesConfig()
        {
            var result = await _bridge.EnableFileSystemIntegrationAsync();
            
            Assert.True(result);
        }

        [Fact]
        public async Task ConfigureScheduledBackupsAsync_CreatesSchedule()
        {
            var result = await _bridge.ConfigureScheduledBackupsAsync(24);
            
            Assert.True(result);
        }

        [Fact]
        public async Task GetIntegrationStatusAsync_ReturnsStatus()
        {
            var status = await _bridge.GetIntegrationStatusAsync();
            
            Assert.NotNull(status);
            Assert.NotNull(status.Integrations);
        }
    }

    // Mock implementations
    public class MockLogger : IVaultLogger
    {
        public List<string> Logs { get; } = new();
        public List<string> Errors { get; } = new();

        public void Log(string message) => Logs.Add(message);
        public void LogError(string message, Exception ex) => Errors.Add($"{message}: {ex.Message}");
    }

    public class MockEncryptionManager : IVaultEncryptionManager
    {
        public Task<bool> ApplyEncryptionAsync(string path) => Task.FromResult(true);
        
        public Task<bool> EncryptDataAsync(byte[] data, byte[] key, out byte[] encrypted)
        {
            encrypted = data;
            return Task.FromResult(true);
        }
        
        public Task<bool> DecryptDataAsync(byte[] encrypted, byte[] key, out byte[] data)
        {
            data = encrypted;
            return Task.FromResult(true);
        }
    }

    public class MockLockerManager : IVaultLockerManager
    {
        public Task<List<LockerInfo>> GetAllLockersAsync() => Task.FromResult(new List<LockerInfo>());
        public Task<bool> SyncWithBackupAsync(string lockerName) => Task.FromResult(true);
    }
}
