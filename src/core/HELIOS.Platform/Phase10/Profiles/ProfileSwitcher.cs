using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HELIOS.Platform.Phase10.Profiles;

/// <summary>
/// Handles switching between profiles with rollback capability
/// </summary>
public class ProfileSwitcher : IProfileSwitcher
{
    private readonly Dictionary<string, IProfileService> _profiles;
    private readonly Stack<string> _profileHistory = new();
    private string _currentProfile = "Default";

    public ProfileSwitcher(Dictionary<string, IProfileService>? profiles = null)
    {
        _profiles = profiles ?? InitializeDefaultProfiles();
    }

    /// <summary>
    /// Switches to a specified profile
    /// </summary>
    public async Task<bool> SwitchProfileAsync(string profileName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(profileName))
                throw new ArgumentException("Profile name cannot be empty", nameof(profileName));

            if (!_profiles.ContainsKey(profileName))
                throw new InvalidOperationException($"Profile '{profileName}' not found");

            var profileService = _profiles[profileName];

            if (!await profileService.ValidateAsync())
                throw new InvalidOperationException($"Profile '{profileName}' validation failed");

            _profileHistory.Push(_currentProfile);

            var result = await profileService.ApplyAsync();
            if (result)
            {
                _currentProfile = profileName;
                return true;
            }

            _profileHistory.Pop();
            return false;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to switch to profile '{profileName}'", ex);
        }
    }

    /// <summary>
    /// Gets the currently active profile
    /// </summary>
    public async Task<string> GetCurrentProfileAsync()
    {
        return await Task.FromResult(_currentProfile);
    }

    /// <summary>
    /// Undoes the last profile switch
    /// </summary>
    public async Task<bool> UndoProfileSwitchAsync()
    {
        try
        {
            if (_profileHistory.Count == 0)
                throw new InvalidOperationException("No profile history to undo");

            var previousProfile = _profileHistory.Pop();

            if (!_profiles.ContainsKey(_currentProfile))
                throw new InvalidOperationException($"Profile '{_currentProfile}' service not found for revert");

            var currentService = _profiles[_currentProfile];
            await currentService.RevertAsync();

            _currentProfile = previousProfile;
            return true;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to undo profile switch", ex);
        }
    }

    private static Dictionary<string, IProfileService> InitializeDefaultProfiles()
    {
        return new Dictionary<string, IProfileService>
        {
            { "Gaming", new GamingProfile() },
            { "Work", new WorkProfile() },
            { "Development", new DevelopmentProfile() },
            { "Secure", new SecureProfile() }
        };
    }
}
