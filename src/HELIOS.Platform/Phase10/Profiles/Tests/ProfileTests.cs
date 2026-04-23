using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using HELIOS.Platform.Phase10.Profiles;

namespace HELIOS.Platform.Phase10.Profiles.Tests;

public class ProfileManagerTests
{
    private readonly string _testProfilePath;
    private readonly ProfileManager _manager;

    public ProfileManagerTests()
    {
        _testProfilePath = Path.Combine(Path.GetTempPath(), $"helios_test_profiles_{Guid.NewGuid()}");
        _manager = new ProfileManager(_testProfilePath);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testProfilePath))
        {
            Directory.Delete(_testProfilePath, true);
        }
    }

    [Fact]
    public async Task CreateProfileAsync_WithValidSettings_ReturnsTrue()
    {
        var settings = new Dictionary<string, object> { { "key", "value" } };
        var result = await _manager.CreateProfileAsync("TestProfile", settings);
        Assert.True(result);
    }

    [Fact]
    public async Task CreateProfileAsync_WithEmptyName_ThrowsException()
    {
        var settings = new Dictionary<string, object>();
        await Assert.ThrowsAsync<ArgumentException>(() => _manager.CreateProfileAsync("", settings));
    }

    [Fact]
    public async Task CreateProfileAsync_WithDuplicateName_ThrowsException()
    {
        var settings = new Dictionary<string, object> { { "key", "value" } };
        await _manager.CreateProfileAsync("DuplicateProfile", settings);
        await Assert.ThrowsAsync<InvalidOperationException>(() => _manager.CreateProfileAsync("DuplicateProfile", settings));
    }

    [Fact]
    public async Task ReadProfileAsync_WithValidName_ReturnsSettings()
    {
        var originalSettings = new Dictionary<string, object> { { "key1", "value1" }, { "key2", 42 } };
        await _manager.CreateProfileAsync("ReadTest", originalSettings);

        var readSettings = await _manager.ReadProfileAsync("ReadTest");

        Assert.NotNull(readSettings);
        Assert.Contains("key1", readSettings.Keys);
    }

    [Fact]
    public async Task ReadProfileAsync_WithNonexistentName_ThrowsException()
    {
        await Assert.ThrowsAsync<FileNotFoundException>(() => _manager.ReadProfileAsync("NonexistentProfile"));
    }

    [Fact]
    public async Task UpdateProfileAsync_WithValidSettings_ReturnsTrue()
    {
        var initialSettings = new Dictionary<string, object> { { "key", "value1" } };
        await _manager.CreateProfileAsync("UpdateTest", initialSettings);

        var updatedSettings = new Dictionary<string, object> { { "key", "value2" } };
        var result = await _manager.UpdateProfileAsync("UpdateTest", updatedSettings);

        Assert.True(result);
    }

    [Fact]
    public async Task UpdateProfileAsync_WithNonexistentName_ThrowsException()
    {
        var settings = new Dictionary<string, object>();
        await Assert.ThrowsAsync<FileNotFoundException>(() => _manager.UpdateProfileAsync("NonexistentProfile", settings));
    }

    [Fact]
    public async Task DeleteProfileAsync_WithValidName_ReturnsTrue()
    {
        var settings = new Dictionary<string, object> { { "key", "value" } };
        await _manager.CreateProfileAsync("DeleteTest", settings);

        var result = await _manager.DeleteProfileAsync("DeleteTest");

        Assert.True(result);
    }

    [Fact]
    public async Task DeleteProfileAsync_WithNonexistentName_ThrowsException()
    {
        await Assert.ThrowsAsync<FileNotFoundException>(() => _manager.DeleteProfileAsync("NonexistentProfile"));
    }

    [Fact]
    public async Task ListProfilesAsync_WithMultipleProfiles_ReturnsAllNames()
    {
        var settings = new Dictionary<string, object> { { "key", "value" } };
        await _manager.CreateProfileAsync("Profile1", settings);
        await _manager.CreateProfileAsync("Profile2", settings);
        await _manager.CreateProfileAsync("Profile3", settings);

        var profiles = await _manager.ListProfilesAsync();

        Assert.Equal(3, profiles.Count);
        Assert.Contains("Profile1", profiles);
        Assert.Contains("Profile2", profiles);
        Assert.Contains("Profile3", profiles);
    }

    [Fact]
    public async Task ListProfilesAsync_WithNoProfiles_ReturnsEmptyList()
    {
        var profiles = await _manager.ListProfilesAsync();
        Assert.Empty(profiles);
    }

    [Fact]
    public async Task ExportProfileAsync_WithValidName_ReturnsJsonString()
    {
        var settings = new Dictionary<string, object> { { "key", "value" } };
        await _manager.CreateProfileAsync("ExportTest", settings);

        var json = await _manager.ExportProfileAsync("ExportTest");

        Assert.NotNull(json);
        Assert.Contains("ExportTest", json);
    }

    [Fact]
    public async Task ExportProfileAsync_WithNonexistentName_ThrowsException()
    {
        await Assert.ThrowsAsync<FileNotFoundException>(() => _manager.ExportProfileAsync("NonexistentProfile"));
    }

    [Fact]
    public async Task ImportProfileAsync_WithValidJson_ReturnsTrue()
    {
        var json = "{\"name\":\"ImportTest\",\"created\":\"2024-01-01T00:00:00Z\",\"modified\":\"2024-01-01T00:00:00Z\",\"settings\":{}}";
        var result = await _manager.ImportProfileAsync("ImportTest", json);

        Assert.True(result);
    }

    [Fact]
    public async Task ImportProfileAsync_WithEmptyJson_ThrowsException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _manager.ImportProfileAsync("ImportTest", ""));
    }

    [Fact]
    public async Task ImportProfileAsync_WithInvalidJson_ThrowsException()
    {
        await Assert.ThrowsAsync<Exception>(() => _manager.ImportProfileAsync("ImportTest", "invalid json"));
    }
}

public class ProfileDetectorTests
{
    private readonly ProfileDetector _detector;

    public ProfileDetectorTests()
    {
        _detector = new ProfileDetector();
    }

    [Fact]
    public async Task AnalyzeHardwareAsync_ReturnsValidDictionary()
    {
        var hardware = await _detector.AnalyzeHardwareAsync();

        Assert.NotNull(hardware);
        Assert.Contains("CPUCores", hardware.Keys);
        Assert.Contains("TotalRam", hardware.Keys);
        Assert.Contains("OSVersion", hardware.Keys);
    }

    [Fact]
    public async Task AnalyzeHardwareAsync_CPUCoresGreaterThanZero()
    {
        var hardware = await _detector.AnalyzeHardwareAsync();

        var cpuCores = (int)hardware["CPUCores"];
        Assert.True(cpuCores > 0);
    }

    [Fact]
    public async Task AnalyzeHardwareAsync_ArchitectureValid()
    {
        var hardware = await _detector.AnalyzeHardwareAsync();

        var arch = hardware["Architecture"].ToString();
        Assert.True(arch == "x64" || arch == "x86");
    }

    [Fact]
    public async Task DetectUsageAsync_ReturnsValidDictionary()
    {
        var usage = await _detector.DetectUsageAsync();

        Assert.NotNull(usage);
        Assert.Contains("RunningProcesses", usage.Keys);
        Assert.Contains("InstalledApps", usage.Keys);
    }

    [Fact]
    public async Task DetectUsageAsync_RunningProcessesNotNull()
    {
        var usage = await _detector.DetectUsageAsync();

        var processes = usage["RunningProcesses"] as List<string>;
        Assert.NotNull(processes);
    }

    [Fact]
    public async Task DetectOptimalProfileAsync_ReturnsValidProfile()
    {
        var profile = await _detector.DetectOptimalProfileAsync();

        Assert.NotNull(profile);
        var validProfiles = new[] { "Gaming", "Work", "Development", "Secure" };
        Assert.Contains(profile, validProfiles);
    }

    [Fact]
    public async Task LearnBehaviorAsync_WithValidData_ReturnsTrue()
    {
        var metrics = new Dictionary<string, object> { { "fps", 60 } };
        var result = await _detector.LearnBehaviorAsync("Gaming", TimeSpan.FromMinutes(30), metrics);

        Assert.True(result);
    }
}

public class GamingProfileTests
{
    private readonly GamingProfile _profile;

    public GamingProfileTests()
    {
        _profile = new GamingProfile();
    }

    [Fact]
    public void ProfileName_ReturnsGaming()
    {
        Assert.Equal("Gaming", _profile.ProfileName);
    }

    [Fact]
    public void ProfileDescription_NotEmpty()
    {
        Assert.NotEmpty(_profile.ProfileDescription);
    }

    [Fact]
    public async Task ValidateAsync_ReturnsBoolean()
    {
        var result = await _profile.ValidateAsync();
        Assert.IsType<bool>(result);
    }

    [Fact]
    public async Task ApplyAsync_ReturnsTrue()
    {
        var result = await _profile.ApplyAsync();
        Assert.True(result);
    }

    [Fact]
    public async Task RevertAsync_ReturnsTrue()
    {
        await _profile.ApplyAsync();
        var result = await _profile.RevertAsync();
        Assert.True(result);
    }
}

public class WorkProfileTests
{
    private readonly WorkProfile _profile;

    public WorkProfileTests()
    {
        _profile = new WorkProfile();
    }

    [Fact]
    public void ProfileName_ReturnsWork()
    {
        Assert.Equal("Work", _profile.ProfileName);
    }

    [Fact]
    public void ProfileDescription_NotEmpty()
    {
        Assert.NotEmpty(_profile.ProfileDescription);
    }

    [Fact]
    public async Task ValidateAsync_ReturnsBoolean()
    {
        var result = await _profile.ValidateAsync();
        Assert.IsType<bool>(result);
    }

    [Fact]
    public async Task ApplyAsync_ReturnsTrue()
    {
        var result = await _profile.ApplyAsync();
        Assert.True(result);
    }

    [Fact]
    public async Task RevertAsync_ReturnsTrue()
    {
        await _profile.ApplyAsync();
        var result = await _profile.RevertAsync();
        Assert.True(result);
    }
}

public class DevelopmentProfileTests
{
    private readonly DevelopmentProfile _profile;

    public DevelopmentProfileTests()
    {
        _profile = new DevelopmentProfile();
    }

    [Fact]
    public void ProfileName_ReturnsDevelopment()
    {
        Assert.Equal("Development", _profile.ProfileName);
    }

    [Fact]
    public void ProfileDescription_NotEmpty()
    {
        Assert.NotEmpty(_profile.ProfileDescription);
    }

    [Fact]
    public async Task ValidateAsync_ReturnsBoolean()
    {
        var result = await _profile.ValidateAsync();
        Assert.IsType<bool>(result);
    }

    [Fact]
    public async Task ApplyAsync_ReturnsTrue()
    {
        var result = await _profile.ApplyAsync();
        Assert.True(result);
    }

    [Fact]
    public async Task RevertAsync_ReturnsTrue()
    {
        await _profile.ApplyAsync();
        var result = await _profile.RevertAsync();
        Assert.True(result);
    }
}

public class SecureProfileTests
{
    private readonly SecureProfile _profile;

    public SecureProfileTests()
    {
        _profile = new SecureProfile();
    }

    [Fact]
    public void ProfileName_ReturnsSecure()
    {
        Assert.Equal("Secure", _profile.ProfileName);
    }

    [Fact]
    public void ProfileDescription_NotEmpty()
    {
        Assert.NotEmpty(_profile.ProfileDescription);
    }

    [Fact]
    public async Task ValidateAsync_ReturnsBoolean()
    {
        var result = await _profile.ValidateAsync();
        Assert.IsType<bool>(result);
    }

    [Fact]
    public async Task ApplyAsync_ReturnsTrue()
    {
        var result = await _profile.ApplyAsync();
        Assert.True(result);
    }

    [Fact]
    public async Task RevertAsync_ReturnsTrue()
    {
        await _profile.ApplyAsync();
        var result = await _profile.RevertAsync();
        Assert.True(result);
    }
}

public class ProfileSwitcherTests
{
    private readonly ProfileSwitcher _switcher;

    public ProfileSwitcherTests()
    {
        var profiles = new Dictionary<string, IProfileService>
        {
            { "Gaming", new GamingProfile() },
            { "Work", new WorkProfile() },
            { "Development", new DevelopmentProfile() },
            { "Secure", new SecureProfile() }
        };
        _switcher = new ProfileSwitcher(profiles);
    }

    [Fact]
    public async Task SwitchProfileAsync_WithValidProfile_ReturnsTrue()
    {
        var result = await _switcher.SwitchProfileAsync("Gaming");
        Assert.True(result);
    }

    [Fact]
    public async Task SwitchProfileAsync_WithEmptyName_ThrowsException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _switcher.SwitchProfileAsync(""));
    }

    [Fact]
    public async Task SwitchProfileAsync_WithNonexistentProfile_ThrowsException()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => _switcher.SwitchProfileAsync("NonexistentProfile"));
    }

    [Fact]
    public async Task GetCurrentProfileAsync_ReturnsCurrentProfile()
    {
        await _switcher.SwitchProfileAsync("Gaming");
        var current = await _switcher.GetCurrentProfileAsync();
        Assert.Equal("Gaming", current);
    }

    [Fact]
    public async Task UndoProfileSwitchAsync_WithValidHistory_ReturnsTrue()
    {
        await _switcher.SwitchProfileAsync("Gaming");
        var result = await _switcher.UndoProfileSwitchAsync();
        Assert.True(result);
    }

    [Fact]
    public async Task UndoProfileSwitchAsync_WithNoHistory_ThrowsException()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => _switcher.UndoProfileSwitchAsync());
    }

    [Fact]
    public async Task SwitchProfileAsync_MultipleProfilesUpdatesCurrent()
    {
        await _switcher.SwitchProfileAsync("Gaming");
        var first = await _switcher.GetCurrentProfileAsync();

        await _switcher.SwitchProfileAsync("Work");
        var second = await _switcher.GetCurrentProfileAsync();

        Assert.NotEqual(first, second);
    }
}

public class ProfileAnalyzerTests
{
    private readonly ProfileAnalyzer _analyzer;

    public ProfileAnalyzerTests()
    {
        _analyzer = new ProfileAnalyzer();
    }

    [Fact]
    public async Task AnalyzePerformanceAsync_WithGamingProfile_ReturnsMetrics()
    {
        var metrics = await _analyzer.AnalyzePerformanceAsync("Gaming", TimeSpan.FromSeconds(10));

        Assert.NotNull(metrics);
        Assert.NotEmpty(metrics);
    }

    [Fact]
    public async Task AnalyzePerformanceAsync_WithWorkProfile_ReturnsMetrics()
    {
        var metrics = await _analyzer.AnalyzePerformanceAsync("Work", TimeSpan.FromSeconds(10));

        Assert.NotNull(metrics);
        Assert.NotEmpty(metrics);
    }

    [Fact]
    public async Task AnalyzePerformanceAsync_WithDevelopmentProfile_ReturnsMetrics()
    {
        var metrics = await _analyzer.AnalyzePerformanceAsync("Development", TimeSpan.FromSeconds(10));

        Assert.NotNull(metrics);
        Assert.NotEmpty(metrics);
    }

    [Fact]
    public async Task AnalyzePerformanceAsync_WithSecureProfile_ReturnsMetrics()
    {
        var metrics = await _analyzer.AnalyzePerformanceAsync("Secure", TimeSpan.FromSeconds(10));

        Assert.NotNull(metrics);
        Assert.NotEmpty(metrics);
    }

    [Fact]
    public async Task GenerateReportAsync_ReturnsNonEmptyString()
    {
        await _analyzer.AnalyzePerformanceAsync("Gaming", TimeSpan.FromSeconds(10));
        var report = await _analyzer.GenerateReportAsync();

        Assert.NotEmpty(report);
    }

    [Fact]
    public async Task GenerateReportAsync_ContainsProfileName()
    {
        await _analyzer.AnalyzePerformanceAsync("Gaming", TimeSpan.FromSeconds(10));
        var report = await _analyzer.GenerateReportAsync();

        Assert.Contains("Gaming", report);
    }

    [Fact]
    public async Task RecommendTuningAsync_WithGamingProfile_ReturnsRecommendations()
    {
        await _analyzer.AnalyzePerformanceAsync("Gaming", TimeSpan.FromSeconds(10));
        var recommendations = await _analyzer.RecommendTuningAsync("Gaming");

        Assert.NotNull(recommendations);
    }

    [Fact]
    public async Task RecommendTuningAsync_WithWorkProfile_ReturnsRecommendations()
    {
        await _analyzer.AnalyzePerformanceAsync("Work", TimeSpan.FromSeconds(10));
        var recommendations = await _analyzer.RecommendTuningAsync("Work");

        Assert.NotNull(recommendations);
    }

    [Fact]
    public async Task RecommendTuningAsync_WithDevelopmentProfile_ReturnsRecommendations()
    {
        await _analyzer.AnalyzePerformanceAsync("Development", TimeSpan.FromSeconds(10));
        var recommendations = await _analyzer.RecommendTuningAsync("Development");

        Assert.NotNull(recommendations);
    }

    [Fact]
    public async Task RecommendTuningAsync_WithSecureProfile_ReturnsRecommendations()
    {
        await _analyzer.AnalyzePerformanceAsync("Secure", TimeSpan.FromSeconds(10));
        var recommendations = await _analyzer.RecommendTuningAsync("Secure");

        Assert.NotNull(recommendations);
    }

    [Fact]
    public async Task AnalyzePerformanceAsync_MultipleProfiles_StoresAllMetrics()
    {
        await _analyzer.AnalyzePerformanceAsync("Gaming", TimeSpan.FromSeconds(10));
        await _analyzer.AnalyzePerformanceAsync("Work", TimeSpan.FromSeconds(10));
        await _analyzer.AnalyzePerformanceAsync("Development", TimeSpan.FromSeconds(10));

        var report = await _analyzer.GenerateReportAsync();

        Assert.Contains("Gaming", report);
        Assert.Contains("Work", report);
        Assert.Contains("Development", report);
    }
}
