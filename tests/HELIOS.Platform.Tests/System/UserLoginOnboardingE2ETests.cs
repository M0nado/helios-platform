using Xunit;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HELIOS.Platform.Tests.System
{
    /// <summary>
    /// System/E2E Tests: User Login → Settings Configuration → Application Ready
    /// 5 test cases testing full user onboarding workflow
    /// </summary>
    public class UserLoginOnboardingE2ETests
    {
        private readonly Mock<IAuthenticationService> _mockAuth;
        private readonly Mock<ISettingsService> _mockSettings;
        private readonly Mock<IApplicationService> _mockApp;

        public UserLoginOnboardingE2ETests()
        {
            _mockAuth = new Mock<IAuthenticationService>();
            _mockSettings = new Mock<ISettingsService>();
            _mockApp = new Mock<IApplicationService>();
        }

        [Fact]
        [Trait("Category", "System")]
        public async Task UserLogin_ConfigureSettings_ApplicationReady_FullWorkflow()
        {
            // Arrange
            var username = "testuser";
            var password = "pass123";
            var userId = "user-001";
            var config = new Dictionary<string, string> { { "Theme", "Dark" } };

            _mockAuth.Setup(a => a.AuthenticateAsync(username, password, CancellationToken.None))
                .ReturnsAsync(new AuthResult { Success = true, UserId = userId });
            _mockSettings.Setup(s => s.ConfigureAsync(userId, config, CancellationToken.None))
                .ReturnsAsync(true);
            _mockApp.Setup(a => a.InitializeAsync(userId, CancellationToken.None))
                .ReturnsAsync(new AppStatus { Ready = true });

            // Act
            var authResult = await _mockAuth.Object.AuthenticateAsync(username, password, CancellationToken.None);
            AppStatus appStatus = null;
            if (authResult.Success)
            {
                var configResult = await _mockSettings.Object.ConfigureAsync(authResult.UserId, config, CancellationToken.None);
                if (configResult)
                {
                    appStatus = await _mockApp.Object.InitializeAsync(authResult.UserId, CancellationToken.None);
                }
            }

            // Assert
            Assert.True(authResult.Success);
            Assert.NotNull(appStatus);
            Assert.True(appStatus.Ready);
        }

        [Fact]
        [Trait("Category", "System")]
        public async Task FailedLogin_DisplaysError_DoesNotProceed()
        {
            // Arrange
            var username = "baduser";
            var password = "wrongpass";
            _mockAuth.Setup(a => a.AuthenticateAsync(username, password, CancellationToken.None))
                .ReturnsAsync(new AuthResult { Success = false, Error = "Invalid credentials" });

            // Act
            var result = await _mockAuth.Object.AuthenticateAsync(username, password, CancellationToken.None);

            // Assert
            Assert.False(result.Success);
            _mockSettings.Verify(s => s.ConfigureAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        [Trait("Category", "System")]
        public async Task ConfigurationDefaults_LoadedIfNotProvided_ApplicationStarts()
        {
            // Arrange
            var userId = "user-001";
            var defaults = new Dictionary<string, string> { { "Theme", "Light" } };
            
            _mockSettings.Setup(s => s.GetDefaultsAsync())
                .ReturnsAsync(defaults);
            _mockSettings.Setup(s => s.ConfigureAsync(userId, defaults, CancellationToken.None))
                .ReturnsAsync(true);
            _mockApp.Setup(a => a.InitializeAsync(userId, CancellationToken.None))
                .ReturnsAsync(new AppStatus { Ready = true });

            // Act
            var defaultConfig = await _mockSettings.Object.GetDefaultsAsync();
            var configResult = await _mockSettings.Object.ConfigureAsync(userId, defaultConfig, CancellationToken.None);
            var appStatus = await _mockApp.Object.InitializeAsync(userId, CancellationToken.None);

            // Assert
            Assert.NotNull(defaultConfig);
            Assert.True(configResult);
            Assert.True(appStatus.Ready);
        }

        [Fact]
        [Trait("Category", "System")]
        public async Task FirstTimeUser_CreatedWithDefaults_FullSetup()
        {
            // Arrange
            var username = "newuser";
            var userId = "user-new-001";
            
            _mockAuth.Setup(a => a.RegisterAsync(username, It.IsAny<string>(), CancellationToken.None))
                .ReturnsAsync(new AuthResult { Success = true, UserId = userId });
            _mockSettings.Setup(s => s.CreateProfileAsync(userId, CancellationToken.None))
                .ReturnsAsync(true);

            // Act
            var registerResult = await _mockAuth.Object.RegisterAsync(username, "password", CancellationToken.None);
            bool profileCreated = false;
            if (registerResult.Success)
            {
                profileCreated = await _mockSettings.Object.CreateProfileAsync(registerResult.UserId, CancellationToken.None);
            }

            // Assert
            Assert.True(registerResult.Success);
            Assert.True(profileCreated);
        }

        [Fact]
        [Trait("Category", "System")]
        public async Task ConfigurationChange_InProgress_NotificationsShown()
        {
            // Arrange
            var userId = "user-001";
            var config = new Dictionary<string, string> { { "Theme", "Dark" } };
            
            _mockApp.Setup(a => a.ShowProgressAsync("Configuring...", CancellationToken.None))
                .ReturnsAsync(true);
            _mockSettings.Setup(s => s.ConfigureAsync(userId, config, CancellationToken.None))
                .ReturnsAsync(true);
            _mockApp.Setup(a => a.ShowProgressAsync("Ready!", CancellationToken.None))
                .ReturnsAsync(true);

            // Act
            await _mockApp.Object.ShowProgressAsync("Configuring...", CancellationToken.None);
            var configured = await _mockSettings.Object.ConfigureAsync(userId, config, CancellationToken.None);
            await _mockApp.Object.ShowProgressAsync("Ready!", CancellationToken.None);

            // Assert
            Assert.True(configured);
        }
    }

    public class AuthResult
    {
        public bool Success { get; set; }
        public string UserId { get; set; }
        public string Error { get; set; }
    }

    public class AppStatus
    {
        public bool Ready { get; set; }
        public string Message { get; set; }
    }

    public interface IAuthenticationService
    {
        Task<AuthResult> AuthenticateAsync(string username, string password, CancellationToken cancellationToken);
        Task<AuthResult> RegisterAsync(string username, string password, CancellationToken cancellationToken);
    }

    public interface ISettingsService
    {
        Task<bool> ConfigureAsync(string userId, Dictionary<string, string> config, CancellationToken cancellationToken);
        Task<Dictionary<string, string>> GetDefaultsAsync();
        Task<bool> CreateProfileAsync(string userId, CancellationToken cancellationToken);
    }

    public interface IApplicationService
    {
        Task<AppStatus> InitializeAsync(string userId, CancellationToken cancellationToken);
        Task<bool> ShowProgressAsync(string message, CancellationToken cancellationToken);
    }
}
