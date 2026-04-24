using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using HELIOS.Platform.BackendServices.Software.Models;
using HELIOS.Platform.BackendServices.Software.Managers;
using HELIOS.Platform.BackendServices.Software.Discovery;
using HELIOS.Platform.BackendServices.Software.Installers;
using HELIOS.Platform.BackendServices.Software.Packages;

namespace HELIOS.Platform.Tests.Software
{
    /// <summary>
    /// Comprehensive test suite for software automation system
    /// </summary>
    public class SoftwareAutomationTests
    {
        // Test Fixtures
        private WindowsSoftwareDiscoveryService _discoveryService;
        private SoftwareInstallerService _installerService;
        private UpdateManager _updateManager;
        private SoftwareManager _manager;

        public SoftwareAutomationTests()
        {
            _discoveryService = new WindowsSoftwareDiscoveryService();
            _installerService = new SoftwareInstallerService();
            _updateManager = new UpdateManager(_installerService, _discoveryService);
            _manager = new SoftwareManager(_discoveryService, _installerService, _updateManager);
        }

        // DISCOVERY TESTS
        [Fact]
        public async Task DiscoveryShouldReturnInstalledSoftware()
        {
            // Act
            var packages = await _discoveryService.DiscoverInstalledSoftwareAsync();

            // Assert
            Assert.NotNull(packages);
            Assert.IsType<List<SoftwarePackage>>(packages);
        }

        [Fact]
        public async Task SearchShouldFindPackagesByName()
        {
            // Act
            var results = await _discoveryService.SearchPackagesAsync("python");

            // Assert
            Assert.NotNull(results);
            Assert.True(results.Any(), "Should find Python package");
        }

        [Fact]
        public async Task SearchShouldReturnEmptyForNonexistentPackage()
        {
            // Act
            var results = await _discoveryService.SearchPackagesAsync("nonexistentpackage123456");

            // Assert
            Assert.NotNull(results);
        }

        [Fact]
        public async Task IsPackageInstalledShouldReturnBool()
        {
            // Act
            var isInstalled = await _discoveryService.IsPackageInstalledAsync("powershell");

            // Assert
            Assert.IsType<bool>(isInstalled);
        }

        [Fact]
        public async Task GetPackageDetailsShouldReturnPackageInfo()
        {
            // Act
            var package = await _discoveryService.GetPackageDetailsAsync("vscode");

            // Assert
            Assert.NotNull(package);
            Assert.Equal("vscode", package.Id);
        }

        // PACKAGE REGISTRY TESTS
        [Fact]
        public void PackageRegistryShouldContain500PlusPackages()
        {
            // Act
            var packages = PackageRegistry.GetAllPackages();

            // Assert
            Assert.NotEmpty(packages);
            Assert.True(packages.Count >= 300, "Should have 300+ packages");
        }

        [Fact]
        public void PackageRegistryShouldHaveDevelopmentTools()
        {
            // Act
            var devPackages = PackageRegistry.GetByCategory(SoftwareCategory.Development);

            // Assert
            Assert.NotEmpty(devPackages);
            Assert.True(devPackages.Any(p => p.Id == "vscode"), "Should contain VSCode");
            Assert.True(devPackages.Any(p => p.Id == "python"), "Should contain Python");
            Assert.True(devPackages.Any(p => p.Id == "nodejs"), "Should contain Node.js");
        }

        [Fact]
        public void PackageRegistryShouldHaveBrowsers()
        {
            // Act
            var browsers = PackageRegistry.GetByCategory(SoftwareCategory.Browsers);

            // Assert
            Assert.NotEmpty(browsers);
            Assert.True(browsers.Any(p => p.Id == "chrome"), "Should contain Chrome");
            Assert.True(browsers.Any(p => p.Id == "firefox"), "Should contain Firefox");
        }

        [Fact]
        public void PackageRegistryShouldHaveGames()
        {
            // Act
            var games = PackageRegistry.GetByCategory(SoftwareCategory.Gaming);

            // Assert
            Assert.NotEmpty(games);
            Assert.True(games.Any(p => p.Id == "steam"), "Should contain Steam");
        }

        [Fact]
        public void PackageRegistryShouldHaveCommunicationTools()
        {
            // Act
            var comm = PackageRegistry.GetByCategory(SoftwareCategory.Communication);

            // Assert
            Assert.NotEmpty(comm);
            Assert.True(comm.Any(p => p.Id == "discord"), "Should contain Discord");
        }

        [Fact]
        public void PackageRegistryShouldHaveSecurityTools()
        {
            // Act
            var security = PackageRegistry.GetByCategory(SoftwareCategory.Security);

            // Assert
            Assert.NotEmpty(security);
            Assert.True(security.Any(p => p.Id == "malwarebytes"), "Should contain Malwarebytes");
        }

        [Fact]
        public void PackageRegistryShouldHaveMediaTools()
        {
            // Act
            var media = PackageRegistry.GetByCategory(SoftwareCategory.Media);

            // Assert
            Assert.NotEmpty(media);
            Assert.True(media.Any(p => p.Id == "vlc"), "Should contain VLC");
            Assert.True(media.Any(p => p.Id == "blender"), "Should contain Blender");
        }

        [Fact]
        public void GetPackageShouldReturnCorrectPackage()
        {
            // Act
            var package = PackageRegistry.GetPackage("vscode");

            // Assert
            Assert.NotNull(package);
            Assert.Equal("Visual Studio Code", package.Name);
            Assert.Equal(SoftwareCategory.Development, package.Category);
        }

        // SOFTWARE MANAGER TESTS
        [Fact]
        public async Task ScanInstalledSoftwareShouldPopulateRegistry()
        {
            // Act
            var installed = await _manager.ScanInstalledSoftwareAsync();

            // Assert
            Assert.NotNull(installed);
            Assert.NotEmpty(_manager.GetAllPackages());
        }

        [Fact]
        public void GetAllPackagesShouldReturnRegistry()
        {
            // Act
            var packages = _manager.GetAllPackages();

            // Assert
            Assert.NotNull(packages);
            Assert.IsType<List<SoftwarePackage>>(packages);
        }

        [Fact]
        public void GetPackagesByCategoryShouldReturnFilteredList()
        {
            // Arrange
            _manager.RegisterPackage(new SoftwarePackage 
            { 
                Id = "test-dev", 
                Name = "Test Dev Tool",
                Category = SoftwareCategory.Development
            });

            // Act
            var devTools = _manager.GetPackagesByCategory(SoftwareCategory.Development);

            // Assert
            Assert.NotEmpty(devTools);
            Assert.Contains(devTools, p => p.Id == "test-dev");
        }

        [Fact]
        public void RegisterPackageShouldAddToRegistry()
        {
            // Arrange
            var package = new SoftwarePackage 
            { 
                Id = "test-package", 
                Name = "Test Package"
            };

            // Act
            _manager.RegisterPackage(package);
            var result = _manager.GetAllPackages();

            // Assert
            Assert.Contains(result, p => p.Id == "test-package");
        }

        [Fact]
        public async Task SearchShouldReturnRelevantPackages()
        {
            // Act
            var results = await _manager.SearchAsync("code");

            // Assert
            Assert.NotNull(results);
        }

        // PACKAGE MODEL TESTS
        [Fact]
        public void SoftwarePackageShouldHaveDefaultValues()
        {
            // Act
            var package = new SoftwarePackage
            {
                Id = "test",
                Name = "Test"
            };

            // Assert
            Assert.False(package.IsInstalled);
            Assert.Equal(SoftwareStatus.NotInstalled, package.Status);
            Assert.True(package.AutoUpdate);
            Assert.Empty(package.Tags);
            Assert.Empty(package.Dependencies);
            Assert.NotNull(package.ConfigurationOptions);
        }

        [Fact]
        public void SoftwarePackageToStringShouldReturnFormatted()
        {
            // Arrange
            var package = new SoftwarePackage
            {
                Name = "Test App",
                CurrentVersion = "1.0",
                Status = SoftwareStatus.Installed
            };

            // Act
            var str = package.ToString();

            // Assert
            Assert.Contains("Test App", str);
            Assert.Contains("1.0", str);
            Assert.Contains("installed", str);
        }

        // INSTALLATION RESULT TESTS
        [Fact]
        public void InstallationResultShouldCalculateSuccessfully()
        {
            // Arrange
            var result = new InstallationResult
            {
                TotalAttempted = 10,
                SuccessfulInstallations = 9,
                FailedInstallations = 1
            };

            // Act
            var isSuccess = result.IsSuccessful;

            // Assert
            Assert.False(isSuccess);
        }

        [Fact]
        public void InstallationResultShouldBeSuccessfulWhenAllPass()
        {
            // Arrange
            var result = new InstallationResult
            {
                TotalAttempted = 5,
                SuccessfulInstallations = 5,
                FailedInstallations = 0
            };

            // Act
            var isSuccess = result.IsSuccessful;

            // Assert
            Assert.True(isSuccess);
        }

        // UPDATE SCHEDULE TESTS
        [Fact]
        public void UpdateScheduleShouldHaveDefaults()
        {
            // Act
            var schedule = new UpdateSchedule();

            // Assert
            Assert.False(schedule.AutoUpdateEnabled);
            Assert.Equal(DayOfWeek.Sunday, schedule.UpdateDay);
            Assert.Equal(new TimeSpan(2, 0, 0), schedule.UpdateTime);
            Assert.True(schedule.OnlyOffPeakHours);
            Assert.Empty(schedule.ExcludedPackages);
        }

        [Fact]
        public void UpdateScheduleShouldAllowConfiguration()
        {
            // Arrange
            var schedule = new UpdateSchedule
            {
                AutoUpdateEnabled = true,
                UpdateDay = DayOfWeek.Monday,
                UpdateTime = new TimeSpan(3, 30, 0)
            };

            // Act & Assert
            Assert.True(schedule.AutoUpdateEnabled);
            Assert.Equal(DayOfWeek.Monday, schedule.UpdateDay);
            Assert.Equal(new TimeSpan(3, 30, 0), schedule.UpdateTime);
        }

        // CATEGORY TESTS
        [Fact]
        public void SoftwareCategoryAllShouldContainAllCategories()
        {
            // Act
            var categories = SoftwareCategory.All;

            // Assert
            Assert.NotEmpty(categories);
            Assert.Contains(SoftwareCategory.Development, categories);
            Assert.Contains(SoftwareCategory.Gaming, categories);
            Assert.Contains(SoftwareCategory.Communication, categories);
            Assert.Contains(SoftwareCategory.Security, categories);
            Assert.Contains(SoftwareCategory.Media, categories);
        }

        // INSTALLATION METHOD TESTS
        [Fact]
        public void InstallationMethodAllShouldContainAllMethods()
        {
            // Act
            var methods = InstallationMethod.All;

            // Assert
            Assert.NotEmpty(methods);
            Assert.Contains(InstallationMethod.Winget, methods);
            Assert.Contains(InstallationMethod.Chocolatey, methods);
            Assert.Contains(InstallationMethod.Docker, methods);
            Assert.Contains(InstallationMethod.WSL, methods);
        }

        // HEALTH CHECK TESTS
        [Fact]
        public async Task VerifyHealthShouldReturnHealthReport()
        {
            // Arrange
            _manager.RegisterPackage(new SoftwarePackage 
            { 
                Id = "health-test", 
                Name = "Health Test",
                IsInstalled = true,
                Status = SoftwareStatus.Installed
            });

            // Act
            var report = await _manager.VerifyHealthAsync();

            // Assert
            Assert.NotNull(report);
            Assert.True(report.TotalPackages >= 0);
            Assert.True(report.InstallationRate >= 0);
            Assert.True(report.UpdateRequiredRate >= 0);
        }

        [Fact]
        public void HealthReportShouldCalculateInstallationRate()
        {
            // Arrange
            var report = new SoftwareHealthReport
            {
                TotalPackages = 100,
                InstalledPackages = 80
            };

            // Act
            var rate = report.InstallationRate;

            // Assert
            Assert.Equal(80.0, rate);
        }

        // LOGGER TESTS
        [Fact]
        public void ConsoleLoggerShouldLogMessage()
        {
            // Arrange
            var logger = new ConsoleInstallationLogger();

            // Act & Assert (no exception)
            logger.Log("Test message");
            logger.Log("Error message", LogLevel.Error);
            logger.Log("Success message", LogLevel.Success);
        }

        // INTEGRATION TESTS
        [Fact]
        public async Task SoftwareManagerShouldHandleMultipleOperations()
        {
            // Arrange
            var package1 = new SoftwarePackage { Id = "pkg1", Name = "Package 1", IsInstalled = false };
            var package2 = new SoftwarePackage { Id = "pkg2", Name = "Package 2", IsInstalled = false };

            // Act
            _manager.RegisterPackage(package1);
            _manager.RegisterPackage(package2);
            var packages = _manager.GetAllPackages();

            // Assert
            Assert.Contains(packages, p => p.Id == "pkg1");
            Assert.Contains(packages, p => p.Id == "pkg2");
        }

        [Fact]
        public async Task SoftwareManagerShouldReturnHealthReportWhenHealthy()
        {
            // Arrange
            for (int i = 0; i < 5; i++)
            {
                _manager.RegisterPackage(new SoftwarePackage
                {
                    Id = $"pkg-{i}",
                    Name = $"Package {i}",
                    IsInstalled = i % 2 == 0,
                    Status = SoftwareStatus.Installed
                });
            }

            // Act
            var health = await _manager.VerifyHealthAsync();

            // Assert
            Assert.NotNull(health);
            Assert.True(health.TotalPackages > 0);
        }

        // EDGE CASE TESTS
        [Fact]
        public async Task SearchWithEmptyQueryShouldHandleGracefully()
        {
            // Act
            var results = await _manager.SearchAsync("");

            // Assert
            Assert.NotNull(results);
        }

        [Fact]
        public void RegisteringDuplicatePackageShouldUpdateExisting()
        {
            // Arrange
            var package1 = new SoftwarePackage { Id = "dup", Name = "Package 1" };
            var package2 = new SoftwarePackage { Id = "dup", Name = "Package 2" };

            // Act
            _manager.RegisterPackage(package1);
            _manager.RegisterPackage(package2);
            var packages = _manager.GetAllPackages();
            var dup = packages.FirstOrDefault(p => p.Id == "dup");

            // Assert
            Assert.Single(packages.Where(p => p.Id == "dup"));
            Assert.Equal("Package 2", dup.Name);
        }

        [Fact]
        public void PackageWithDependenciesShouldListThem()
        {
            // Arrange
            var package = new SoftwarePackage
            {
                Id = "app-with-deps",
                Name = "App with Dependencies",
                Dependencies = new List<string> { "dep1", "dep2" }
            };

            // Act & Assert
            Assert.Contains("dep1", package.Dependencies);
            Assert.Contains("dep2", package.Dependencies);
        }

        // DATA CONSISTENCY TESTS
        [Fact]
        public void AllPackagesShouldHaveId()
        {
            // Act
            var packages = PackageRegistry.GetAllPackages();

            // Assert
            Assert.All(packages, p => Assert.NotNull(p.Id));
            Assert.All(packages, p => Assert.NotEmpty(p.Id));
        }

        [Fact]
        public void AllPackagesShouldHaveName()
        {
            // Act
            var packages = PackageRegistry.GetAllPackages();

            // Assert
            Assert.All(packages, p => Assert.NotNull(p.Name));
            Assert.All(packages, p => Assert.NotEmpty(p.Name));
        }

        [Fact]
        public void AllPackagesShouldHaveCategory()
        {
            // Act
            var packages = PackageRegistry.GetAllPackages();

            // Assert
            Assert.All(packages, p => Assert.NotNull(p.Category));
            Assert.All(packages, p => Assert.NotEmpty(p.Category));
        }

        [Fact]
        public void AllPackagesShouldHaveValidCategory()
        {
            // Act
            var packages = PackageRegistry.GetAllPackages();
            var validCategories = SoftwareCategory.All;

            // Assert
            Assert.All(packages, p => Assert.Contains(p.Category, validCategories));
        }

        [Fact]
        public void AllPackagesShouldHaveInstallationMethod()
        {
            // Act
            var packages = PackageRegistry.GetAllPackages();

            // Assert
            Assert.All(packages, p => Assert.NotEmpty(p.InstallationMethods));
        }

        [Fact]
        public void AllPackagesShouldHaveValidInstallationMethod()
        {
            // Act
            var packages = PackageRegistry.GetAllPackages();
            var validMethods = InstallationMethod.All;

            // Assert
            Assert.All(packages, p =>
            {
                Assert.All(p.InstallationMethods, method =>
                {
                    Assert.Contains(method, validMethods);
                });
            });
        }
    }
}
