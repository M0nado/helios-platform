using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HELIOS.Platform.Phase10.Profiles;

/// <summary>
/// Interface for profile management services
/// </summary>
public interface IProfileService
{
    /// <summary>
    /// Gets the profile name
    /// </summary>
    string ProfileName { get; }

    /// <summary>
    /// Gets the profile description
    /// </summary>
    string ProfileDescription { get; }

    /// <summary>
    /// Applies the profile to the system
    /// </summary>
    Task<bool> ApplyAsync();

    /// <summary>
    /// Validates if the profile can be applied
    /// </summary>
    Task<bool> ValidateAsync();

    /// <summary>
    /// Reverts the profile
    /// </summary>
    Task<bool> RevertAsync();
}

/// <summary>
/// Interface for profile manager operations
/// </summary>
public interface IProfileManager
{
    /// <summary>
    /// Creates a new profile
    /// </summary>
    Task<bool> CreateProfileAsync(string name, Dictionary<string, object> settings);

    /// <summary>
    /// Reads a profile
    /// </summary>
    Task<Dictionary<string, object>> ReadProfileAsync(string name);

    /// <summary>
    /// Updates a profile
    /// </summary>
    Task<bool> UpdateProfileAsync(string name, Dictionary<string, object> settings);

    /// <summary>
    /// Deletes a profile
    /// </summary>
    Task<bool> DeleteProfileAsync(string name);

    /// <summary>
    /// Lists all profiles
    /// </summary>
    Task<List<string>> ListProfilesAsync();

    /// <summary>
    /// Exports a profile to JSON
    /// </summary>
    Task<string> ExportProfileAsync(string name);

    /// <summary>
    /// Imports a profile from JSON
    /// </summary>
    Task<bool> ImportProfileAsync(string name, string jsonContent);
}

/// <summary>
/// Interface for profile detection
/// </summary>
public interface IProfileDetector
{
    /// <summary>
    /// Detects the optimal profile based on hardware and usage
    /// </summary>
    Task<string> DetectOptimalProfileAsync();

    /// <summary>
    /// Analyzes hardware capabilities
    /// </summary>
    Task<Dictionary<string, object>> AnalyzeHardwareAsync();

    /// <summary>
    /// Detects typical usage patterns
    /// </summary>
    Task<Dictionary<string, object>> DetectUsageAsync();

    /// <summary>
    /// Learns from user behavior
    /// </summary>
    Task<bool> LearnBehaviorAsync(string profileUsed, TimeSpan duration, Dictionary<string, object> metrics);
}

/// <summary>
/// Interface for profile switching
/// </summary>
public interface IProfileSwitcher
{
    /// <summary>
    /// Switches to a profile
    /// </summary>
    Task<bool> SwitchProfileAsync(string profileName);

    /// <summary>
    /// Gets the current active profile
    /// </summary>
    Task<string> GetCurrentProfileAsync();

    /// <summary>
    /// Undoes the last profile switch
    /// </summary>
    Task<bool> UndoProfileSwitchAsync();
}

/// <summary>
/// Interface for profile analysis
/// </summary>
public interface IProfileAnalyzer
{
    /// <summary>
    /// Analyzes performance after profile application
    /// </summary>
    Task<Dictionary<string, object>> AnalyzePerformanceAsync(string profileName, TimeSpan duration);

    /// <summary>
    /// Generates a performance report
    /// </summary>
    Task<string> GenerateReportAsync();

    /// <summary>
    /// Recommends profile tuning
    /// </summary>
    Task<List<string>> RecommendTuningAsync(string profileName);
}
