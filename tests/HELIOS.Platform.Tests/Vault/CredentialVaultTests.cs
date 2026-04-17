using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Microsoft.Extensions.Logging;
using Moq;
using HELIOS.Platform.BackendServices.SecurityVault;
using HELIOS.Platform.BackendServices.Encryption;

namespace HELIOS.Platform.Tests.Vault
{
    public class CredentialVaultTests
    {
        private readonly Mock<IEncryptionService> _mockEncryption;
        private readonly Mock<ILogger<CredentialVault>> _mockLogger;
        private readonly CredentialVault _vault;

        public CredentialVaultTests()
        {
            _mockEncryption = new Mock<IEncryptionService>();
            _mockLogger = new Mock<ILogger<CredentialVault>>();
            
            // Setup mock encryption service
            _mockEncryption.Setup(e => e.GenerateSalt(It.IsAny<int>()))
                .Returns((int length) => new byte[length]);
            
            _mockEncryption.Setup(e => e.GenerateRandomBytes(It.IsAny<int>()))
                .Returns((int length) => new byte[length]);

            _mockEncryption.Setup(e => e.EncryptWithMasterPassword(It.IsAny<string>(), It.IsAny<string>()))
                .Returns((string data, string pwd) => $"encrypted_{data}");

            _mockEncryption.Setup(e => e.DecryptWithMasterPassword(It.IsAny<string>(), It.IsAny<string>()))
                .Returns((string encrypted, string pwd) => encrypted.Replace("encrypted_", ""));

            _vault = new CredentialVault(_mockEncryption.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task InitializeVault_WithValidPassword_Succeeds()
        {
            // Act
            var result = await _vault.InitializeVaultAsync("SecurePassword123!");

            // Assert
            Assert.True(result);
            Assert.True(await _vault.IsVaultLockedAsync() == false);
        }

        [Fact]
        public async Task InitializeVault_WithShortPassword_Fails()
        {
            // Act
            var result = await _vault.InitializeVaultAsync("short");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task StoreCredential_AfterInitialize_Succeeds()
        {
            // Arrange
            await _vault.InitializeVaultAsync("SecurePassword123!");

            // Act
            var result = await _vault.StoreCredentialAsync(
                "TestApiKey",
                "api_key_value_123",
                CredentialType.ApiKey);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task LockVault_ClearsEncryptionKey()
        {
            // Arrange
            await _vault.InitializeVaultAsync("SecurePassword123!");

            // Act
            var lockResult = await _vault.LockVaultAsync();

            // Assert
            Assert.True(lockResult);
            Assert.True(await _vault.IsVaultLockedAsync());
        }

        [Fact]
        public async Task UnlockVault_WithCorrectPassword_Succeeds()
        {
            // Arrange
            await _vault.InitializeVaultAsync("SecurePassword123!");
            await _vault.LockVaultAsync();

            // Act
            var result = await _vault.UnlockVaultAsync("SecurePassword123!");

            // Assert
            Assert.True(result);
            Assert.False(await _vault.IsVaultLockedAsync());
        }

        [Fact]
        public async Task UnlockVault_WithWrongPassword_Fails()
        {
            // Arrange
            await _vault.InitializeVaultAsync("SecurePassword123!");
            await _vault.LockVaultAsync();

            // Act
            var result = await _vault.UnlockVaultAsync("WrongPassword123!");

            // Assert
            Assert.False(result);
            Assert.True(await _vault.IsVaultLockedAsync());
        }

        [Fact]
        public async Task ListCredentials_ReturnsAllCredentials()
        {
            // Arrange
            await _vault.InitializeVaultAsync("SecurePassword123!");
            await _vault.StoreCredentialAsync("Cred1", "value1", CredentialType.ApiKey);
            await _vault.StoreCredentialAsync("Cred2", "value2", CredentialType.DatabasePassword);

            // Act
            var credentials = await _vault.ListCredentialsAsync();

            // Assert
            Assert.NotEmpty(credentials);
            Assert.Equal(2, credentials.Count);
        }

        [Fact]
        public async Task DeleteCredential_RemovesFromVault()
        {
            // Arrange
            await _vault.InitializeVaultAsync("SecurePassword123!");
            await _vault.StoreCredentialAsync("TestCred", "value", CredentialType.ApiKey);
            var credentials = await _vault.ListCredentialsAsync();
            var credentialId = credentials[0].Id;

            // Act
            var result = await _vault.DeleteCredentialAsync(credentialId);

            // Assert
            Assert.True(result);
            var remainingCredentials = await _vault.ListCredentialsAsync();
            Assert.Empty(remainingCredentials);
        }

        [Fact]
        public async Task RevokeCredential_DeactivatesWithoutDeleting()
        {
            // Arrange
            await _vault.InitializeVaultAsync("SecurePassword123!");
            await _vault.StoreCredentialAsync("TestCred", "value", CredentialType.ApiKey);
            var credentials = await _vault.ListCredentialsAsync();
            var credentialId = credentials[0].Id;

            // Act
            var result = await _vault.RevokeCredentialAsync(credentialId);
            var credential = await _vault.GetCredentialInfoAsync(credentialId);

            // Assert
            Assert.True(result);
            Assert.False(credential.IsActive);
        }

        [Fact]
        public async Task SecurelyDeleteVault_ClearsAllData()
        {
            // Arrange
            await _vault.InitializeVaultAsync("SecurePassword123!");
            await _vault.StoreCredentialAsync("Cred1", "value1", CredentialType.ApiKey);

            // Act
            var result = await _vault.SecurelyDeleteVaultAsync();

            // Assert
            Assert.True(result);
            var credentials = await _vault.ListCredentialsAsync();
            Assert.Empty(credentials);
        }
    }
}
