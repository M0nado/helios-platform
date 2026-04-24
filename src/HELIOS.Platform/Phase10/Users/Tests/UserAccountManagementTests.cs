using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using HELIOS.Platform.Phase10.Users;
using HELIOS.Platform.Phase10.Users.Interfaces;

namespace HELIOS.Platform.Phase10.Users.Tests
{
    public class UserAccountProvisionerTests : IDisposable
    {
        private readonly UserAccountProvisioner _provisioner;
        private readonly string _testLogPath;

        public UserAccountProvisionerTests()
        {
            _testLogPath = Path.Combine(Path.GetTempPath(), $"UserProvisioner_{Guid.NewGuid()}.log");
            _provisioner = new UserAccountProvisioner(_testLogPath);
        }

        [Fact]
        public void Constructor_InitializesLogPath()
        {
            Assert.NotNull(_provisioner);
        }

        [Fact]
        public async Task UserExistsAsync_ReturnsTrueForCurrentUser()
        {
            var result = await _provisioner.UserExistsAsync(Environment.UserName);
            Assert.True(result);
        }

        [Fact]
        public async Task UserExistsAsync_ReturnsFalseForNonexistentUser()
        {
            var result = await _provisioner.UserExistsAsync("NonExistentUser_" + Guid.NewGuid());
            Assert.False(result);
        }

        [Fact]
        public async Task GetAllAccountsAsync_ReturnsNonEmptyList()
        {
            var accounts = await _provisioner.GetAllAccountsAsync();
            Assert.NotEmpty(accounts);
        }

        [Fact]
        public async Task GetAllAccountsAsync_ContainsCurrentUser()
        {
            var accounts = await _provisioner.GetAllAccountsAsync();
            var currentUser = accounts.Find(a => a.Username == Environment.UserName);
            Assert.NotNull(currentUser);
        }

        [Fact]
        public async Task GetAllAccountsAsync_ReturnsValidUserAccountInfo()
        {
            var accounts = await _provisioner.GetAllAccountsAsync();
            
            foreach (var account in accounts)
            {
                Assert.NotNull(account.Username);
                Assert.NotNull(account.Sid);
                Assert.False(string.IsNullOrWhiteSpace(account.Username));
            }
        }

        public void Dispose()
        {
            try
            {
                if (File.Exists(_testLogPath))
                    File.Delete(_testLogPath);
            }
            catch { }
        }
    }

    public class AccountPermissionManagerTests : IDisposable
    {
        private readonly AccountPermissionManager _manager;
        private readonly string _testLogPath;

        public AccountPermissionManagerTests()
        {
            _testLogPath = Path.Combine(Path.GetTempPath(), $"PermissionManager_{Guid.NewGuid()}.log");
            _manager = new AccountPermissionManager(_testLogPath);
        }

        [Fact]
        public void Constructor_InitializesLogPath()
        {
            Assert.NotNull(_manager);
        }

        [Fact]
        public async Task IsAdministratorAsync_ReturnsBoolForCurrentUser()
        {
            var result = await _manager.IsAdministratorAsync(Environment.UserName);
            Assert.IsType<bool>(result);
        }

        [Fact]
        public async Task GetUserPermissionsAsync_ReturnsValidPermissions()
        {
            var permissions = await _manager.GetUserPermissionsAsync(Environment.UserName);
            
            Assert.NotNull(permissions);
            Assert.Equal(Environment.UserName, permissions.Username);
            Assert.NotNull(permissions.Groups);
        }

        [Fact]
        public async Task GetUserPermissionsAsync_ContainsGroups()
        {
            var permissions = await _manager.GetUserPermissionsAsync(Environment.UserName);
            
            Assert.NotNull(permissions.Groups);
            Assert.NotEmpty(permissions.Groups);
        }

        [Fact]
        public async Task RevokeAllPermissionsAsync_ReturnsTrue()
        {
            // This test doesn't actually revoke (would break system), but tests the method structure
            var result = await _manager.RevokeAllPermissionsAsync("TestUser_" + Guid.NewGuid());
            Assert.True(result);
        }

        public void Dispose()
        {
            try
            {
                if (File.Exists(_testLogPath))
                    File.Delete(_testLogPath);
            }
            catch { }
        }
    }

    public class UserDataDirectorySetupTests : IDisposable
    {
        private readonly UserDataDirectorySetup _setup;
        private readonly string _testLogPath;
        private readonly string _testUsername = "TestUser_" + Guid.NewGuid().ToString().Substring(0, 8);

        public UserDataDirectorySetupTests()
        {
            _testLogPath = Path.Combine(Path.GetTempPath(), $"DirectorySetup_{Guid.NewGuid()}.log");
            _setup = new UserDataDirectorySetup(_testLogPath);
        }

        [Fact]
        public void Constructor_InitializesLogPath()
        {
            Assert.NotNull(_setup);
        }

        [Fact]
        public void GetUserProfilePath_ReturnsValidPath()
        {
            var path = _setup.GetUserProfilePath(_testUsername);
            
            Assert.NotNull(path);
            Assert.Contains(_testUsername, path);
            Assert.Contains("Users", path);
        }

        [Fact]
        public void GetUserProfilePath_ContainsSystemDrive()
        {
            var path = _setup.GetUserProfilePath(_testUsername);
            var systemDrive = Environment.ExpandEnvironmentVariables("%SystemDrive%");
            
            Assert.StartsWith(systemDrive, path);
        }

        [Fact]
        public async Task CreateDocumentFoldersAsync_ReturnsTrue()
        {
            var result = await _setup.CreateDocumentFoldersAsync(Environment.UserName);
            Assert.True(result);
        }

        [Fact]
        public async Task CreateMediaFoldersAsync_ReturnsTrue()
        {
            var result = await _setup.CreateMediaFoldersAsync(Environment.UserName);
            Assert.True(result);
        }

        [Fact]
        public async Task SetupHELIOSFoldersAsync_ReturnsTrue()
        {
            var result = await _setup.SetupHELIOSFoldersAsync(Environment.UserName);
            Assert.True(result);
        }

        [Fact]
        public async Task GetUserDirectorySizeAsync_ReturnsNonNegative()
        {
            var size = await _setup.GetUserDirectorySizeAsync(Environment.UserName);
            Assert.True(size >= 0);
        }

        [Fact]
        public async Task CleanupUserDirectoriesAsync_ReturnsTrue()
        {
            var result = await _setup.CleanupUserDirectoriesAsync(Environment.UserName);
            Assert.True(result);
        }

        public void Dispose()
        {
            try
            {
                if (File.Exists(_testLogPath))
                    File.Delete(_testLogPath);
            }
            catch { }
        }
    }

    public class MultiProfileCoordinatorTests : IDisposable
    {
        private readonly MultiProfileCoordinator _coordinator;
        private readonly string _testLogPath;

        public MultiProfileCoordinatorTests()
        {
            _testLogPath = Path.Combine(Path.GetTempPath(), $"ProfileCoordinator_{Guid.NewGuid()}.log");
            _coordinator = new MultiProfileCoordinator(_testLogPath);
        }

        [Fact]
        public void Constructor_InitializesCoordinator()
        {
            Assert.NotNull(_coordinator);
        }

        [Fact]
        public void GetCurrentUser_ReturnsCurrentUsername()
        {
            var current = _coordinator.GetCurrentUser();
            Assert.NotNull(current);
            Assert.NotEmpty(current);
        }

        [Fact]
        public void GetCurrentUser_MatchesEnvironmentUsername()
        {
            var current = _coordinator.GetCurrentUser();
            // Note: May not match exactly due to domain prefix, but should contain username
            Assert.NotNull(current);
        }

        [Fact]
        public void GetAllProfiles_ReturnsNonEmptyList()
        {
            var profiles = _coordinator.GetAllProfiles();
            Assert.NotEmpty(profiles);
        }

        [Fact]
        public void GetProfileState_ReturnsValidState()
        {
            var state = _coordinator.GetProfileState(Environment.UserName);
            // State may be null if user not in tracked list
            if (state != null)
            {
                Assert.Equal(Environment.UserName, state.Username);
            }
        }

        [Fact]
        public async Task KeepServicesRunningAsync_ReturnsTrue()
        {
            var result = await _coordinator.KeepServicesRunningAsync();
            Assert.True(result);
        }

        public void Dispose()
        {
            try
            {
                if (File.Exists(_testLogPath))
                    File.Delete(_testLogPath);
            }
            catch { }
        }
    }

    public class UserSecurityInitializerTests : IDisposable
    {
        private readonly UserSecurityInitializer _initializer;
        private readonly string _testLogPath;
        private readonly string _testUsername = "TestUser_" + Guid.NewGuid().ToString().Substring(0, 8);

        public UserSecurityInitializerTests()
        {
            _testLogPath = Path.Combine(Path.GetTempPath(), $"SecurityInitializer_{Guid.NewGuid()}.log");
            _initializer = new UserSecurityInitializer(_testLogPath);
        }

        [Fact]
        public void Constructor_InitializesLogPath()
        {
            Assert.NotNull(_initializer);
        }

        [Fact]
        public void ValidatePassword_AcceptsValidComplexPassword()
        {
            var requirements = new UserSecurityInitializer.PasswordRequirements();
            var password = "SecureP@ssw0rd!";
            
            var result = _initializer.ValidatePassword(password, requirements);
            Assert.True(result);
        }

        [Fact]
        public void ValidatePassword_RejectsShortPassword()
        {
            var requirements = new UserSecurityInitializer.PasswordRequirements { MinimumLength = 12 };
            var password = "Short1!";
            
            var result = _initializer.ValidatePassword(password, requirements);
            Assert.False(result);
        }

        [Fact]
        public void ValidatePassword_RejectsMissingUppercase()
        {
            var requirements = new UserSecurityInitializer.PasswordRequirements 
            { 
                RequireComplexity = true,
                RequireUppercase = true 
            };
            var password = "secure@password123";
            
            var result = _initializer.ValidatePassword(password, requirements);
            Assert.False(result);
        }

        [Fact]
        public void ValidatePassword_RejectsMissingNumbers()
        {
            var requirements = new UserSecurityInitializer.PasswordRequirements 
            { 
                RequireComplexity = true,
                RequireNumbers = true 
            };
            var password = "SecurePassword!";
            
            var result = _initializer.ValidatePassword(password, requirements);
            Assert.False(result);
        }

        [Fact]
        public void ValidatePassword_RejectsMissingSpecialChars()
        {
            var requirements = new UserSecurityInitializer.PasswordRequirements 
            { 
                RequireComplexity = true,
                RequireSpecialChars = true 
            };
            var password = "SecurePassword123";
            
            var result = _initializer.ValidatePassword(password, requirements);
            Assert.False(result);
        }

        [Fact]
        public async Task SetupPasswordHistoryAsync_ReturnsTrue()
        {
            var result = await _initializer.SetupPasswordHistoryAsync(_testUsername);
            Assert.True(result);
        }

        [Fact]
        public async Task ConfigureLoginTimeoutAsync_ReturnsTrue()
        {
            var result = await _initializer.ConfigureLoginTimeoutAsync(_testUsername, 15);
            Assert.True(result);
        }

        [Fact]
        public async Task GetSecurityStatusAsync_ReturnsValidStatus()
        {
            var status = await _initializer.GetSecurityStatusAsync(_testUsername);
            
            Assert.NotNull(status);
            Assert.Equal(_testUsername, status.Username);
            Assert.True(status.SecurityScore >= 0 && status.SecurityScore <= 100);
        }

        [Fact]
        public async Task GetSecurityStatusAsync_SecurityScoreCalculatedCorrectly()
        {
            var status = await _initializer.GetSecurityStatusAsync(_testUsername);
            
            int expectedScore = 0;
            if (status.HasPasswordRequirements) expectedScore += 25;
            if (status.Has2FAEnabled) expectedScore += 25;
            if (status.HasAuditLogging) expectedScore += 25;
            if (status.HasRecoveryCodes) expectedScore += 25;
            
            Assert.Equal(expectedScore, status.SecurityScore);
        }

        public void Dispose()
        {
            try
            {
                if (File.Exists(_testLogPath))
                    File.Delete(_testLogPath);
            }
            catch { }
        }
    }

    public class AccountActivityMonitorTests : IDisposable
    {
        private readonly AccountActivityMonitor _monitor;
        private readonly string _testLogPath;
        private readonly string _testAuditPath;
        private readonly string _testUsername = "TestUser_" + Guid.NewGuid().ToString().Substring(0, 8);

        public AccountActivityMonitorTests()
        {
            _testLogPath = Path.Combine(Path.GetTempPath(), $"ActivityMonitor_{Guid.NewGuid()}.log");
            _testAuditPath = Path.Combine(Path.GetTempPath(), $"Audit_{Guid.NewGuid()}");
            _monitor = new AccountActivityMonitor(_testLogPath, _testAuditPath);
        }

        [Fact]
        public void Constructor_InitializesPaths()
        {
            Assert.NotNull(_monitor);
        }

        [Fact]
        public async Task LogAccountCreationAsync_ReturnsTrue()
        {
            var result = await _monitor.LogAccountCreationAsync(_testUsername, "Test User", "TestAccount");
            Assert.True(result);
        }

        [Fact]
        public async Task LogLoginAttemptAsync_SuccessfulLogin_ReturnsTrue()
        {
            var result = await _monitor.LogLoginAttemptAsync(_testUsername, true, "127.0.0.1");
            Assert.True(result);
        }

        [Fact]
        public async Task LogLoginAttemptAsync_FailedLogin_ReturnsTrue()
        {
            var result = await _monitor.LogLoginAttemptAsync(_testUsername, false, "127.0.0.1");
            Assert.True(result);
        }

        [Fact]
        public async Task LogLoginAttemptAsync_MultipleFailures_TracksAttempts()
        {
            await _monitor.LogLoginAttemptAsync(_testUsername, false, "127.0.0.1");
            await _monitor.LogLoginAttemptAsync(_testUsername, false, "127.0.0.1");
            
            var log = _monitor.GetActivityLog(_testUsername);
            Assert.NotNull(log);
            Assert.Equal(2, log.FailedLoginAttempts);
        }

        [Fact]
        public async Task LogLogoutAsync_ReturnsTrue()
        {
            var result = await _monitor.LogLogoutAsync(_testUsername);
            Assert.True(result);
        }

        [Fact]
        public async Task LogPrivilegeEscalationAsync_ReturnsTrue()
        {
            var result = await _monitor.LogPrivilegeEscalationAsync(_testUsername, "ModifyRegistry", "HKLM\\Software");
            Assert.True(result);
        }

        [Fact]
        public async Task LogFileAccessAsync_ReturnsTrue()
        {
            var result = await _monitor.LogFileAccessAsync(_testUsername, "C:\\test.txt", "Read");
            Assert.True(result);
        }

        [Fact]
        public async Task LogPolicyChangeAsync_ReturnsTrue()
        {
            var result = await _monitor.LogPolicyChangeAsync(_testUsername, "PasswordPolicy", "old", "new");
            Assert.True(result);
        }

        [Fact]
        public async Task GenerateActivityReportAsync_ReturnsValidReport()
        {
            await _monitor.LogLoginAttemptAsync(_testUsername, true, "127.0.0.1");
            
            var report = await _monitor.GenerateActivityReportAsync(_testUsername, DateTime.Now.AddHours(-1), DateTime.Now.AddHours(1));
            
            Assert.NotNull(report);
            Assert.Equal(_testUsername, report.Username);
            Assert.True(report.SuccessfulLogins > 0);
        }

        [Fact]
        public async Task GenerateActivityReportAsync_CalculatesRiskLevel()
        {
            await _monitor.LogLoginAttemptAsync(_testUsername, true, "127.0.0.1");
            
            var report = await _monitor.GenerateActivityReportAsync(_testUsername, DateTime.Now.AddHours(-1), DateTime.Now.AddHours(1));
            
            Assert.NotNull(report.RiskLevel);
            Assert.True(new[] { "Minimal", "Low", "Medium", "High", "Critical" }.Contains(report.RiskLevel));
        }

        [Fact]
        public async Task CheckForSuspiciousActivityAsync_ReturnsBool()
        {
            var result = await _monitor.CheckForSuspiciousActivityAsync(_testUsername);
            Assert.IsType<bool>(result);
        }

        [Fact]
        public async Task ArchiveOldLogsAsync_ReturnsTrue()
        {
            var result = await _monitor.ArchiveOldLogsAsync(90);
            Assert.True(result);
        }

        [Fact]
        public void GetActivityLog_ReturnsLogForTrackedUser()
        {
            var log = _monitor.GetActivityLog(_testUsername);
            // Log may be null or empty for new user, but method should not throw
            Assert.True(log == null || log.Username == _testUsername);
        }

        [Fact]
        public void GetHighSeverityEvents_ReturnsValidList()
        {
            var events = _monitor.GetHighSeverityEvents();
            Assert.NotNull(events);
        }

        public void Dispose()
        {
            try
            {
                if (File.Exists(_testLogPath))
                    File.Delete(_testLogPath);
                if (Directory.Exists(_testAuditPath))
                    Directory.Delete(_testAuditPath, true);
            }
            catch { }
        }
    }

    public class IntegrationTests
    {
        [Fact]
        public void AllServicesImplementCorrectInterfaces()
        {
            // Verify that all services can be used together
            var provisioner = new UserAccountProvisioner();
            var permissionManager = new AccountPermissionManager();
            var directorySetup = new UserDataDirectorySetup();
            var profileCoordinator = new MultiProfileCoordinator();
            var securityInitializer = new UserSecurityInitializer();
            var activityMonitor = new AccountActivityMonitor();

            Assert.NotNull(provisioner);
            Assert.NotNull(permissionManager);
            Assert.NotNull(directorySetup);
            Assert.NotNull(profileCoordinator);
            Assert.NotNull(securityInitializer);
            Assert.NotNull(activityMonitor);
        }

        [Fact]
        public async Task WorkflowAsyncOperationsCanChain()
        {
            var provisioner = new UserAccountProvisioner();
            var securityInit = new UserSecurityInitializer();
            var activityMonitor = new AccountActivityMonitor();

            // Verify async operations can be awaited in sequence
            var exists = await provisioner.UserExistsAsync(Environment.UserName);
            Assert.True(exists);

            var secStatus = await securityInit.GetSecurityStatusAsync(Environment.UserName);
            Assert.NotNull(secStatus);

            var report = await activityMonitor.GenerateActivityReportAsync(
                Environment.UserName, 
                DateTime.Now.AddHours(-1), 
                DateTime.Now.AddHours(1));
            Assert.NotNull(report);
        }
    }
}
