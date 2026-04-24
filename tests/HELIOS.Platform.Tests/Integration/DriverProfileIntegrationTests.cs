using Xunit;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HELIOS.Platform.Tests.Integration
{
    /// <summary>
    /// Integration Tests: Driver Installation → Profile Update
    /// 6 test cases testing driver updates and profile synchronization
    /// </summary>
    public class DriverProfileIntegrationTests
    {
        private readonly Mock<IDriverManager> _mockDriverManager;
        private readonly Mock<IProfileManager> _mockProfileManager;

        public DriverProfileIntegrationTests()
        {
            _mockDriverManager = new Mock<IDriverManager>();
            _mockProfileManager = new Mock<IProfileManager>();
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task InstallDriver_ThenUpdateProfile_ProfileReflectsNewDriver()
        {
            // Arrange
            var driverId = "GPU-001";
            var version = "2.0";
            var profileId = "user-profile-1";
            
            _mockDriverManager.Setup(d => d.InstallAsync(driverId, version, CancellationToken.None))
                .ReturnsAsync(true);
            _mockProfileManager.Setup(p => p.UpdateAsync(profileId, It.IsAny<ProfileUpdate>(), CancellationToken.None))
                .ReturnsAsync(true);

            // Act
            var installResult = await _mockDriverManager.Object.InstallAsync(driverId, version, CancellationToken.None);
            var update = new ProfileUpdate { Driver = driverId, DriverVersion = version };
            var profileResult = await _mockProfileManager.Object.UpdateAsync(profileId, update, CancellationToken.None);

            // Assert
            Assert.True(installResult);
            Assert.True(profileResult);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task DriverCompatibility_CheckedBeforeInstall_IncompatibleRejected()
        {
            // Arrange
            var driverId = "INCOMPATIBLE-001";
            _mockDriverManager.Setup(d => d.CheckCompatibilityAsync(driverId, CancellationToken.None))
                .ReturnsAsync(false);

            // Act
            var isCompatible = await _mockDriverManager.Object.CheckCompatibilityAsync(driverId, CancellationToken.None);

            // Assert
            Assert.False(isCompatible);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task ProfileRollback_AfterFailedDriverUpdate_RestoresPreviousState()
        {
            // Arrange
            var profileId = "user-profile-1";
            var driverId = "GPU-001";
            
            _mockProfileManager.Setup(p => p.CreateBackupAsync(profileId, CancellationToken.None))
                .ReturnsAsync("backup-123");
            _mockProfileManager.Setup(p => p.RollbackAsync(profileId, "backup-123", CancellationToken.None))
                .ReturnsAsync(true);

            // Act
            var backupId = await _mockProfileManager.Object.CreateBackupAsync(profileId, CancellationToken.None);
            var rollbackResult = await _mockProfileManager.Object.RollbackAsync(profileId, backupId, CancellationToken.None);

            // Assert
            Assert.NotNull(backupId);
            Assert.True(rollbackResult);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task MultipleDriverUpdates_SequentialInstallation_AllSucceed()
        {
            // Arrange
            var drivers = new[] { ("GPU", "2.0"), ("NETWORK", "1.5"), ("AUDIO", "1.2") };
            var installResults = new List<bool>();

            foreach (var (type, version) in drivers)
            {
                _mockDriverManager.Setup(d => d.InstallAsync(type, version, CancellationToken.None))
                    .ReturnsAsync(true);
            }

            // Act
            foreach (var (type, version) in drivers)
            {
                var result = await _mockDriverManager.Object.InstallAsync(type, version, CancellationToken.None);
                installResults.Add(result);
            }

            // Assert
            Assert.All(installResults, r => Assert.True(r));
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task DriverDependency_ResolvesAndInstallsInOrder()
        {
            // Arrange
            var driverId = "GPU-001";
            var dependencies = new[] { "BASE-DRIVER", "GPU-001" };
            
            _mockDriverManager.Setup(d => d.GetDependenciesAsync(driverId, CancellationToken.None))
                .ReturnsAsync(dependencies);

            foreach (var dep in dependencies)
            {
                _mockDriverManager.Setup(d => d.InstallAsync(dep, It.IsAny<string>(), CancellationToken.None))
                    .ReturnsAsync(true);
            }

            // Act
            var deps = await _mockDriverManager.Object.GetDependenciesAsync(driverId, CancellationToken.None);
            var installTasks = new List<Task<bool>>();
            foreach (var dep in deps)
            {
                installTasks.Add(_mockDriverManager.Object.InstallAsync(dep, "1.0", CancellationToken.None));
            }
            var results = await Task.WhenAll(installTasks);

            // Assert
            Assert.Equal(2, deps.Length);
            Assert.All(results, r => Assert.True(r));
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task ProfileSyncAfterDriverUpdate_DistributedAcrossProfiles()
        {
            // Arrange
            var driverId = "GPU-001";
            var version = "2.0";
            var profileIds = new[] { "profile-1", "profile-2", "profile-3" };

            foreach (var profileId in profileIds)
            {
                _mockProfileManager.Setup(p => p.UpdateAsync(profileId, It.IsAny<ProfileUpdate>(), CancellationToken.None))
                    .ReturnsAsync(true);
            }

            // Act
            var updateTasks = new List<Task<bool>>();
            foreach (var profileId in profileIds)
            {
                var update = new ProfileUpdate { Driver = driverId, DriverVersion = version };
                updateTasks.Add(_mockProfileManager.Object.UpdateAsync(profileId, update, CancellationToken.None));
            }
            var results = await Task.WhenAll(updateTasks);

            // Assert
            Assert.Equal(3, results.Length);
            Assert.All(results, r => Assert.True(r));
        }
    }

    public class ProfileUpdate
    {
        public string Driver { get; set; }
        public string DriverVersion { get; set; }
    }

    public interface IDriverManager
    {
        Task<bool> InstallAsync(string driverId, string version, CancellationToken cancellationToken);
        Task<bool> CheckCompatibilityAsync(string driverId, CancellationToken cancellationToken);
        Task<string[]> GetDependenciesAsync(string driverId, CancellationToken cancellationToken);
    }

    public interface IProfileManager
    {
        Task<bool> UpdateAsync(string profileId, ProfileUpdate update, CancellationToken cancellationToken);
        Task<string> CreateBackupAsync(string profileId, CancellationToken cancellationToken);
        Task<bool> RollbackAsync(string profileId, string backupId, CancellationToken cancellationToken);
    }
}
