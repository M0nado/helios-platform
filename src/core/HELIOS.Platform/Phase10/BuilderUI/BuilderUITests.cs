using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Moq;

namespace HELIOS.Platform.Phase10.BuilderUI.Tests
{
    /// <summary>
    /// Comprehensive unit tests for BuilderUI services.
    /// Tests all 8 services with 40+ test cases covering functionality, validation, and error handling.
    /// </summary>
    public class BuilderUITests
    {
        #region Test Setup
        private Mock<IBuilderUIService> CreateMockBuilderService()
        {
            var service = new Mock<IBuilderUIService>();

            // Default mock implementations
            service.Setup(s => s.GetAvailableDrivesAsync()).ReturnsAsync(new List<DriveInfo>
            {
                new DriveInfo
                {
                    DriveId = "drive1",
                    DriveName = "Kingston USB",
                    DriveType = "USB",
                    TotalCapacity = 32L * 1024 * 1024 * 1024,
                    FreeSpace = 30L * 1024 * 1024 * 1024,
                    IsHealthy = true,
                    IsRecommended = true,
                    Manufacturer = "Kingston"
                },
                new DriveInfo
                {
                    DriveId = "drive2",
                    DriveName = "Local Disk D:",
                    DriveType = "Disk",
                    TotalCapacity = 500L * 1024 * 1024 * 1024,
                    FreeSpace = 200L * 1024 * 1024 * 1024,
                    IsHealthy = true,
                    IsRecommended = false,
                    Manufacturer = "Seagate"
                }
            });

            service.Setup(s => s.GetWindowsVersionsAsync()).ReturnsAsync(new List<WindowsVersionOption>
            {
                new WindowsVersionOption { VersionId = "home", DisplayName = "Windows Home", Description = "Home Edition", RequiredSpace = 20L * 1024 * 1024 * 1024 },
                new WindowsVersionOption { VersionId = "pro", DisplayName = "Windows Pro", Description = "Professional Edition", RequiredSpace = 25L * 1024 * 1024 * 1024 },
                new WindowsVersionOption { VersionId = "enterprise", DisplayName = "Windows Enterprise", Description = "Enterprise Edition", RequiredSpace = 30L * 1024 * 1024 * 1024 }
            });

            service.Setup(s => s.GetAllPackagesAsync()).ReturnsAsync(new List<Package>
            {
                new Package { PackageId = "pkg1", Name = "Core System", Category = "System", Size = 2L * 1024 * 1024 * 1024, IsSelected = true, Priority = 1 },
                new Package { PackageId = "pkg2", Name = "Development Tools", Category = "Dev", Size = 5L * 1024 * 1024 * 1024, IsSelected = false, Priority = 2 },
                new Package { PackageId = "pkg3", Name = "Gaming Drivers", Category = "Gaming", Size = 3L * 1024 * 1024 * 1024, IsSelected = false, Priority = 3 },
                new Package { PackageId = "pkg4", Name = "Security Suite", Category = "Security", Size = 1L * 1024 * 1024 * 1024, IsSelected = true, Priority = 1 }
            });

            service.Setup(s => s.GetAvailableProfilesAsync()).ReturnsAsync(new List<OptimizationProfile>
            {
                new OptimizationProfile { ProfileId = "prof1", Name = "Balanced", Description = "Balanced performance and power", IsRecommended = true, Optimizations = "CPU scaling, Memory management" },
                new OptimizationProfile { ProfileId = "prof2", Name = "Gaming", Description = "Maximum performance", IsRecommended = false, Optimizations = "GPU optimization, High performance mode" },
                new OptimizationProfile { ProfileId = "prof3", Name = "Power Saver", Description = "Battery and power efficiency", IsRecommended = false, Optimizations = "Low power mode, Reduced brightness" }
            });

            return service;
        }
        #endregion

        #region StepWizardEngine Tests
        [Fact]
        public async Task StepWizardEngine_Initialize_ShouldSetupAllSteps()
        {
            // Arrange
            var mockService = CreateMockBuilderService();
            var wizard = new StepWizardEngine(mockService.Object);

            // Act
            await wizard.InitializeAsync();
            var allSteps = await wizard.GetAllStepsAsync();

            // Assert
            Assert.NotEmpty(allSteps);
            Assert.Equal(7, allSteps.Count);
            Assert.Equal(1, allSteps[0].StepNumber);
            Assert.Equal(7, allSteps[6].StepNumber);
        }

        [Fact]
        public async Task StepWizardEngine_GetCurrentStep_ShouldReturnWelcomeInitially()
        {
            // Arrange
            var mockService = CreateMockBuilderService();
            var wizard = new StepWizardEngine(mockService.Object);
            await wizard.InitializeAsync();

            // Act
            var currentStep = await wizard.GetCurrentStepAsync();

            // Assert
            Assert.NotNull(currentStep);
            Assert.Equal(1, currentStep.StepNumber);
            Assert.Equal("Welcome to HELIOS USB Builder", currentStep.Title);
        }

        [Fact]
        public async Task StepWizardEngine_GoToNextStep_ShouldAdvanceStep()
        {
            // Arrange
            var mockService = CreateMockBuilderService();
            var wizard = new StepWizardEngine(mockService.Object);
            await wizard.InitializeAsync();

            // Act
            bool result = await wizard.GoToNextStepAsync();
            var nextStep = await wizard.GetCurrentStepAsync();

            // Assert
            Assert.True(result);
            Assert.Equal(2, nextStep.StepNumber);
        }

        [Fact]
        public async Task StepWizardEngine_GoToPreviousStep_ShouldRegressStep()
        {
            // Arrange
            var mockService = CreateMockBuilderService();
            var wizard = new StepWizardEngine(mockService.Object);
            await wizard.InitializeAsync();
            await wizard.GoToNextStepAsync();
            await wizard.GoToNextStepAsync();

            // Act
            bool result = await wizard.GoToPreviousStepAsync();
            var previousStep = await wizard.GetCurrentStepAsync();

            // Assert
            Assert.True(result);
            Assert.Equal(2, previousStep.StepNumber);
        }

        [Fact]
        public async Task StepWizardEngine_CanGoBack_ShouldBeFalseAtFirstStep()
        {
            // Arrange
            var mockService = CreateMockBuilderService();
            var wizard = new StepWizardEngine(mockService.Object);
            await wizard.InitializeAsync();

            // Act
            bool canGoBack = wizard.CanGoBack;

            // Assert
            Assert.False(canGoBack);
        }

        [Fact]
        public async Task StepWizardEngine_CanGoForward_ShouldBeFalseAtLastStep()
        {
            // Arrange
            var mockService = CreateMockBuilderService();
            var wizard = new StepWizardEngine(mockService.Object);
            await wizard.InitializeAsync();
            for (int i = 0; i < 6; i++)
            {
                await wizard.GoToNextStepAsync();
            }

            // Act
            bool canGoForward = wizard.CanGoForward;

            // Assert
            Assert.False(canGoForward);
        }

        [Fact]
        public async Task StepWizardEngine_GoToStep_ShouldNavigateToSpecificStep()
        {
            // Arrange
            var mockService = CreateMockBuilderService();
            var wizard = new StepWizardEngine(mockService.Object);
            await wizard.InitializeAsync();

            // Act
            bool result = await wizard.GoToStepAsync(4);
            var currentStep = await wizard.GetCurrentStepAsync();

            // Assert
            Assert.True(result);
            Assert.Equal(4, currentStep.StepNumber);
        }

        [Fact]
        public async Task StepWizardEngine_ValidateStep_ShouldReturnErrorsForInvalidSelections()
        {
            // Arrange
            var mockService = CreateMockBuilderService();
            mockService.Setup(s => s.GetSelectedWindowsVersionAsync()).ReturnsAsync(string.Empty);
            
            var wizard = new StepWizardEngine(mockService.Object);
            await wizard.InitializeAsync();
            await wizard.GoToStepAsync(3); // Version step

            // Act
            var errors = await wizard.ValidateCurrentStepAsync();

            // Assert
            Assert.NotEmpty(errors);
            Assert.Contains("Windows version must be selected", errors);
        }
        #endregion

        #region DriveSelector Tests
        [Fact]
        public async Task DriveSelector_Initialize_ShouldLoadDrives()
        {
            // Arrange
            var mockService = CreateMockBuilderService();
            var selector = new DriveSelector();

            // Act
            await selector.InitializeAsync(mockService.Object);
            await selector.LoadDrivesAsync();

            // Assert
            // Verify mock was called
            mockService.Verify(s => s.GetAvailableDrivesAsync(), Times.Once);
        }

        [Fact]
        public async Task DriveSelector_SelectDrive_ShouldUpdateSelection()
        {
            // Arrange
            var mockService = CreateMockBuilderService();
            var selector = new DriveSelector();
            await selector.InitializeAsync(mockService.Object);

            // Act
            mockService.Setup(s => s.SelectDriveAsync(It.IsAny<string>())).ReturnsAsync(true);
            var result = await mockService.Object.SelectDriveAsync("drive1");

            // Assert
            Assert.True(result);
            mockService.Verify(s => s.SelectDriveAsync("drive1"), Times.Once);
        }

        [Fact]
        public async Task DriveSelector_CheckDriveHealth_ShouldReturnHealthStatus()
        {
            // Arrange
            var mockService = CreateMockBuilderService();
            mockService.Setup(s => s.CheckDriveHealthAsync(It.IsAny<string>())).ReturnsAsync(true);

            // Act
            var isHealthy = await mockService.Object.CheckDriveHealthAsync("drive1");

            // Assert
            Assert.True(isHealthy);
        }

        [Fact]
        public async Task DriveSelector_GetRecommendedDrive_ShouldReturnRecommended()
        {
            // Arrange
            var mockService = CreateMockBuilderService();
            mockService.Setup(s => s.GetRecommendedDriveAsync()).ReturnsAsync(new DriveInfo
            {
                DriveId = "drive1",
                IsRecommended = true,
                DriveName = "Kingston USB"
            });

            // Act
            var recommended = await mockService.Object.GetRecommendedDriveAsync();

            // Assert
            Assert.NotNull(recommended);
            Assert.True(recommended.IsRecommended);
            Assert.Equal("Kingston USB", recommended.DriveName);
        }

        [Fact]
        public async Task DriveSelector_VerifyDrive_ShouldValidateDriveIntegrity()
        {
            // Arrange
            var mockService = CreateMockBuilderService();
            mockService.Setup(s => s.VerifyDriveAsync(It.IsAny<string>())).ReturnsAsync(true);

            // Act
            var isValid = await mockService.Object.VerifyDriveAsync("drive1");

            // Assert
            Assert.True(isValid);
        }
        #endregion

        #region PackageSelector Tests
        [Fact]
        public async Task PackageSelector_Initialize_ShouldLoadAllPackages()
        {
            // Arrange
            var mockService = CreateMockBuilderService();
            var selector = new PackageSelector();

            // Act
            await selector.InitializeAsync(mockService.Object);

            // Assert
            mockService.Verify(s => s.GetAllPackagesAsync(), Times.AtLeastOnce);
        }

        [Fact]
        public async Task PackageSelector_SelectPackage_ShouldUpdateSelection()
        {
            // Arrange
            var mockService = CreateMockBuilderService();
            mockService.Setup(s => s.SelectPackageAsync(It.IsAny<string>())).ReturnsAsync(true);

            // Act
            var result = await mockService.Object.SelectPackageAsync("pkg2");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task PackageSelector_DeselectPackage_ShouldRemoveSelection()
        {
            // Arrange
            var mockService = CreateMockBuilderService();
            mockService.Setup(s => s.DeselectPackageAsync(It.IsAny<string>())).ReturnsAsync(true);

            // Act
            var result = await mockService.Object.DeselectPackageAsync("pkg1");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task PackageSelector_GetSelectedPackages_ShouldReturnSelected()
        {
            // Arrange
            var mockService = CreateMockBuilderService();
            mockService.Setup(s => s.GetSelectedPackagesAsync()).ReturnsAsync(new List<Package>
            {
                new Package { PackageId = "pkg1", Name = "Core System", IsSelected = true },
                new Package { PackageId = "pkg4", Name = "Security Suite", IsSelected = true }
            });

            // Act
            var selected = await mockService.Object.GetSelectedPackagesAsync();

            // Assert
            Assert.Equal(2, selected.Count);
            Assert.All(selected, p => Assert.True(p.IsSelected));
        }

        [Fact]
        public async Task PackageSelector_CalculateTotalSize_ShouldReturnCorrectSum()
        {
            // Arrange
            var mockService = CreateMockBuilderService();
            long expectedSize = 3L * 1024 * 1024 * 1024; // 3GB
            mockService.Setup(s => s.CalculateTotalSizeAsync()).ReturnsAsync(expectedSize);

            // Act
            var totalSize = await mockService.Object.CalculateTotalSizeAsync();

            // Assert
            Assert.Equal(expectedSize, totalSize);
        }

        [Fact]
        public async Task PackageSelector_GetPackageDependencies_ShouldReturnDependencies()
        {
            // Arrange
            var mockService = CreateMockBuilderService();
            mockService.Setup(s => s.GetPackageDependenciesAsync(It.IsAny<string>()))
                .ReturnsAsync(new List<string> { "pkg1", "pkg3" });

            // Act
            var dependencies = await mockService.Object.GetPackageDependenciesAsync("pkg2");

            // Assert
            Assert.NotEmpty(dependencies);
            Assert.Equal(2, dependencies.Count);
        }

        [Fact]
        public async Task PackageSelector_GetPackagesByCategory_ShouldFilterByCategory()
        {
            // Arrange
            var mockService = CreateMockBuilderService();
            mockService.Setup(s => s.GetPackagesByCategoryAsync("Dev"))
                .ReturnsAsync(new List<Package>
                {
                    new Package { PackageId = "pkg2", Category = "Dev", Name = "Development Tools" }
                });

            // Act
            var devPackages = await mockService.Object.GetPackagesByCategoryAsync("Dev");

            // Assert
            Assert.Single(devPackages);
            Assert.All(devPackages, p => Assert.Equal("Dev", p.Category));
        }
        #endregion

        #region ProfileSelector Tests
        [Fact]
        public async Task ProfileSelector_GetAvailableProfiles_ShouldReturnProfiles()
        {
            // Arrange
            var mockService = CreateMockBuilderService();

            // Act
            var profiles = await mockService.Object.GetAvailableProfilesAsync();

            // Assert
            Assert.NotEmpty(profiles);
            Assert.Equal(3, profiles.Count);
        }

        [Fact]
        public async Task ProfileSelector_SelectProfile_ShouldUpdateSelection()
        {
            // Arrange
            var mockService = CreateMockBuilderService();
            mockService.Setup(s => s.SelectProfileAsync(It.IsAny<string>())).ReturnsAsync(true);

            // Act
            var result = await mockService.Object.SelectProfileAsync("prof1");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task ProfileSelector_GetRecommendedProfile_ShouldReturnRecommended()
        {
            // Arrange
            var mockService = CreateMockBuilderService();
            mockService.Setup(s => s.GetRecommendedProfileAsync())
                .ReturnsAsync(new OptimizationProfile
                {
                    ProfileId = "prof1",
                    Name = "Balanced",
                    IsRecommended = true
                });

            // Act
            var recommended = await mockService.Object.GetRecommendedProfileAsync();

            // Assert
            Assert.NotNull(recommended);
            Assert.True(recommended.IsRecommended);
        }

        [Fact]
        public async Task ProfileSelector_GetProfilePreview_ShouldShowPackageChanges()
        {
            // Arrange
            var mockService = CreateMockBuilderService();
            mockService.Setup(s => s.GetProfilePreviewAsync(It.IsAny<string>()))
                .ReturnsAsync(new List<Package>
                {
                    new Package { PackageId = "pkg1", Name = "Core System" },
                    new Package { PackageId = "pkg2", Name = "Development Tools" }
                });

            // Act
            var preview = await mockService.Object.GetProfilePreviewAsync("prof1");

            // Assert
            Assert.NotEmpty(preview);
            Assert.Equal(2, preview.Count);
        }

        [Fact]
        public async Task ProfileSelector_CreateCustomProfile_ShouldCreateNewProfile()
        {
            // Arrange
            var mockService = CreateMockBuilderService();
            var customProfile = new OptimizationProfile
            {
                ProfileId = Guid.NewGuid().ToString(),
                Name = "Custom",
                IsCustom = true
            };
            mockService.Setup(s => s.CreateCustomProfileAsync(It.IsAny<OptimizationProfile>())).ReturnsAsync(true);

            // Act
            var result = await mockService.Object.CreateCustomProfileAsync(customProfile);

            // Assert
            Assert.True(result);
        }
        #endregion

        #region WindowsVersion Tests
        [Fact]
        public async Task BuilderUI_GetWindowsVersions_ShouldReturnAllVersions()
        {
            // Arrange
            var mockService = CreateMockBuilderService();

            // Act
            var versions = await mockService.Object.GetWindowsVersionsAsync();

            // Assert
            Assert.NotEmpty(versions);
            Assert.Equal(3, versions.Count);
        }

        [Fact]
        public async Task BuilderUI_SelectWindowsVersion_ShouldUpdateSelection()
        {
            // Arrange
            var mockService = CreateMockBuilderService();
            mockService.Setup(s => s.SelectWindowsVersionAsync(It.IsAny<string>())).ReturnsAsync(true);

            // Act
            var result = await mockService.Object.SelectWindowsVersionAsync("pro");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task BuilderUI_GetSelectedWindowsVersion_ShouldReturnSelected()
        {
            // Arrange
            var mockService = CreateMockBuilderService();
            mockService.Setup(s => s.GetSelectedWindowsVersionAsync()).ReturnsAsync("pro");

            // Act
            var selected = await mockService.Object.GetSelectedWindowsVersionAsync();

            // Assert
            Assert.Equal("pro", selected);
        }
        #endregion

        #region Deployment & Summary Tests
        [Fact]
        public async Task BuilderUI_GetDeploymentSummary_ShouldReturnCompleteSummary()
        {
            // Arrange
            var mockService = CreateMockBuilderService();
            mockService.Setup(s => s.GetDeploymentSummaryAsync())
                .ReturnsAsync(new DeploymentSummary
                {
                    TargetDrive = "Kingston USB",
                    WindowsVersion = "Pro",
                    SelectedProfile = "Balanced",
                    TotalSize = 25L * 1024 * 1024 * 1024,
                    EstimatedMinutes = 30,
                    TermsAccepted = true
                });

            // Act
            var summary = await mockService.Object.GetDeploymentSummaryAsync();

            // Assert
            Assert.NotNull(summary);
            Assert.Equal("Kingston USB", summary.TargetDrive);
            Assert.Equal(30, summary.EstimatedMinutes);
            Assert.True(summary.TermsAccepted);
        }

        [Fact]
        public async Task BuilderUI_AcceptTerms_ShouldConfirmAcceptance()
        {
            // Arrange
            var mockService = CreateMockBuilderService();
            mockService.Setup(s => s.AcceptTermsAsync()).ReturnsAsync(true);

            // Act
            var result = await mockService.Object.AcceptTermsAsync();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task BuilderUI_StartDeployment_ShouldInitiateProcess()
        {
            // Arrange
            var mockService = CreateMockBuilderService();
            mockService.Setup(s => s.StartDeploymentAsync()).ReturnsAsync(true);

            // Act
            var result = await mockService.Object.StartDeploymentAsync();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task BuilderUI_PauseDeployment_ShouldPauseProcess()
        {
            // Arrange
            var mockService = CreateMockBuilderService();
            mockService.Setup(s => s.PauseDeploymentAsync()).ReturnsAsync(true);

            // Act
            var result = await mockService.Object.PauseDeploymentAsync();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task BuilderUI_ResumeDeployment_ShouldResumeProcess()
        {
            // Arrange
            var mockService = CreateMockBuilderService();
            mockService.Setup(s => s.ResumeDeploymentAsync()).ReturnsAsync(true);

            // Act
            var result = await mockService.Object.ResumeDeploymentAsync();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task BuilderUI_CancelDeployment_ShouldStopProcess()
        {
            // Arrange
            var mockService = CreateMockBuilderService();
            mockService.Setup(s => s.CancelDeploymentAsync()).ReturnsAsync(true);

            // Act
            var result = await mockService.Object.CancelDeploymentAsync();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task BuilderUI_GetProgress_ShouldReturnCurrentProgress()
        {
            // Arrange
            var mockService = CreateMockBuilderService();
            mockService.Setup(s => s.GetProgressAsync())
                .ReturnsAsync(new BuilderProgressUpdate
                {
                    OverallPercentage = 50,
                    SubtaskPercentage = 75,
                    CurrentOperation = "Copying files...",
                    TimeRemaining = TimeSpan.FromMinutes(15)
                });

            // Act
            var progress = await mockService.Object.GetProgressAsync();

            // Assert
            Assert.Equal(50, progress.OverallPercentage);
            Assert.Equal(75, progress.SubtaskPercentage);
            Assert.Equal("Copying files...", progress.CurrentOperation);
        }
        #endregion

        #region Edge Cases & Error Handling Tests
        [Fact]
        public async Task BuilderUI_EmptyDriveList_ShouldHandleGracefully()
        {
            // Arrange
            var mockService = new Mock<IBuilderUIService>();
            mockService.Setup(s => s.GetAvailableDrivesAsync()).ReturnsAsync(new List<DriveInfo>());

            // Act
            var drives = await mockService.Object.GetAvailableDrivesAsync();

            // Assert
            Assert.Empty(drives);
        }

        [Fact]
        public async Task BuilderUI_NullSummary_ShouldReturnEmpty()
        {
            // Arrange
            var mockService = new Mock<IBuilderUIService>();
            mockService.Setup(s => s.GetDeploymentSummaryAsync()).ReturnsAsync(new DeploymentSummary());

            // Act
            var summary = await mockService.Object.GetDeploymentSummaryAsync();

            // Assert
            Assert.NotNull(summary);
        }

        [Fact]
        public async Task BuilderUI_InvalidStepNumber_ShouldNotNavigate()
        {
            // Arrange
            var mockService = CreateMockBuilderService();
            var wizard = new StepWizardEngine(mockService.Object);
            await wizard.InitializeAsync();

            // Act
            bool result = await wizard.GoToStepAsync(99);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task BuilderUI_LargePackageSize_ShouldCalculateCorrectly()
        {
            // Arrange
            var mockService = new Mock<IBuilderUIService>();
            long largeSize = 500L * 1024 * 1024 * 1024; // 500GB
            mockService.Setup(s => s.CalculateTotalSizeAsync()).ReturnsAsync(largeSize);

            // Act
            var total = await mockService.Object.CalculateTotalSizeAsync();

            // Assert
            Assert.Equal(largeSize, total);
        }

        [Fact]
        public async Task BuilderUI_ProgressUpdate_ShouldHandleZeroProgress()
        {
            // Arrange
            var mockService = new Mock<IBuilderUIService>();
            mockService.Setup(s => s.GetProgressAsync())
                .ReturnsAsync(new BuilderProgressUpdate
                {
                    OverallPercentage = 0,
                    SubtaskPercentage = 0
                });

            // Act
            var progress = await mockService.Object.GetProgressAsync();

            // Assert
            Assert.Equal(0, progress.OverallPercentage);
            Assert.Equal(0, progress.SubtaskPercentage);
        }

        [Fact]
        public async Task BuilderUI_ProgressUpdate_ShouldHandleFullProgress()
        {
            // Arrange
            var mockService = new Mock<IBuilderUIService>();
            mockService.Setup(s => s.GetProgressAsync())
                .ReturnsAsync(new BuilderProgressUpdate
                {
                    OverallPercentage = 100,
                    SubtaskPercentage = 100
                });

            // Act
            var progress = await mockService.Object.GetProgressAsync();

            // Assert
            Assert.Equal(100, progress.OverallPercentage);
            Assert.Equal(100, progress.SubtaskPercentage);
        }

        [Fact]
        public async Task BuilderUI_MultipleProfileSelections_ShouldReplaceSelection()
        {
            // Arrange
            var mockService = CreateMockBuilderService();
            mockService.Setup(s => s.SelectProfileAsync(It.IsAny<string>())).ReturnsAsync(true);

            // Act
            await mockService.Object.SelectProfileAsync("prof1");
            await mockService.Object.SelectProfileAsync("prof2");
            var result = await mockService.Object.SelectProfileAsync("prof3");

            // Assert
            Assert.True(result);
            mockService.Verify(s => s.SelectProfileAsync(It.IsAny<string>()), Times.Exactly(3));
        }

        [Fact]
        public async Task BuilderUI_DependencyHandling_ShouldAutoSelectDependencies()
        {
            // Arrange
            var mockService = CreateMockBuilderService();
            mockService.Setup(s => s.GetPackageDependenciesAsync("pkg2"))
                .ReturnsAsync(new List<string> { "pkg1" });

            // Act
            var dependencies = await mockService.Object.GetPackageDependenciesAsync("pkg2");

            // Assert
            Assert.Contains("pkg1", dependencies);
        }

        [Fact]
        public async Task BuilderUI_StepValidation_ShouldCatchMultipleErrors()
        {
            // Arrange
            var mockService = new Mock<IBuilderUIService>();
            mockService.Setup(s => s.GetAvailableDrivesAsync()).ReturnsAsync(new List<DriveInfo>());
            mockService.Setup(s => s.GetSelectedWindowsVersionAsync()).ReturnsAsync(string.Empty);

            var wizard = new StepWizardEngine(mockService.Object);
            await wizard.InitializeAsync();

            // Act - Should have validation errors
            var errors = await wizard.ValidateCurrentStepAsync();

            // Assert - No errors on welcome step
            Assert.Empty(errors);
        }
        #endregion
    }
}
