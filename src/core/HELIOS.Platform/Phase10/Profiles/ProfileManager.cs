using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace HELIOS.Platform.Phase10.Profiles;

/// <summary>
/// Manages profile CRUD operations and storage
/// </summary>
public class ProfileManager : IProfileManager
{
    private readonly string _profileStoragePath;
    private readonly JsonSerializerOptions _jsonOptions;

    public ProfileManager(string? profileStoragePath = null)
    {
        _profileStoragePath = profileStoragePath ?? GetDefaultProfilePath();
        _jsonOptions = new JsonSerializerOptions { WriteIndented = true };
        
        if (!Directory.Exists(_profileStoragePath))
        {
            Directory.CreateDirectory(_profileStoragePath);
        }
    }

    /// <summary>
    /// Gets the default profile storage path
    /// </summary>
    private static string GetDefaultProfilePath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "HELIOS", "Profiles");
    }

    /// <summary>
    /// Creates a new profile with settings
    /// </summary>
    public async Task<bool> CreateProfileAsync(string name, Dictionary<string, object> settings)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Profile name cannot be empty", nameof(name));

            var profilePath = Path.Combine(_profileStoragePath, $"{name}.json");
            
            if (File.Exists(profilePath))
                throw new InvalidOperationException($"Profile '{name}' already exists");

            var profile = new
            {
                name,
                created = DateTime.UtcNow,
                modified = DateTime.UtcNow,
                settings
            };

            var json = JsonSerializer.Serialize(profile, _jsonOptions);
            await File.WriteAllTextAsync(profilePath, json);

            return true;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to create profile '{name}'", ex);
        }
    }

    /// <summary>
    /// Reads profile settings
    /// </summary>
    public async Task<Dictionary<string, object>> ReadProfileAsync(string name)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Profile name cannot be empty", nameof(name));

            var profilePath = Path.Combine(_profileStoragePath, $"{name}.json");

            if (!File.Exists(profilePath))
                throw new FileNotFoundException($"Profile '{name}' not found");

            var json = await File.ReadAllTextAsync(profilePath);
            var document = JsonDocument.Parse(json);
            var settings = new Dictionary<string, object>();

            if (document.RootElement.TryGetProperty("settings", out var settingsElement))
            {
                foreach (var property in settingsElement.EnumerateObject())
                {
                    settings[property.Name] = ExtractValue(property.Value);
                }
            }

            return settings;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to read profile '{name}'", ex);
        }
    }

    /// <summary>
    /// Updates profile settings
    /// </summary>
    public async Task<bool> UpdateProfileAsync(string name, Dictionary<string, object> settings)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Profile name cannot be empty", nameof(name));

            var profilePath = Path.Combine(_profileStoragePath, $"{name}.json");

            if (!File.Exists(profilePath))
                throw new FileNotFoundException($"Profile '{name}' not found");

            var json = await File.ReadAllTextAsync(profilePath);
            var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            var updatedProfile = new
            {
                name = root.GetProperty("name").GetString(),
                created = DateTime.Parse(root.GetProperty("created").GetString() ?? DateTime.UtcNow.ToString()),
                modified = DateTime.UtcNow,
                settings
            };

            var updatedJson = JsonSerializer.Serialize(updatedProfile, _jsonOptions);
            await File.WriteAllTextAsync(profilePath, updatedJson);

            return true;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to update profile '{name}'", ex);
        }
    }

    /// <summary>
    /// Deletes a profile
    /// </summary>
    public async Task<bool> DeleteProfileAsync(string name)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Profile name cannot be empty", nameof(name));

            var profilePath = Path.Combine(_profileStoragePath, $"{name}.json");

            if (!File.Exists(profilePath))
                throw new FileNotFoundException($"Profile '{name}' not found");

            await Task.Run(() => File.Delete(profilePath));
            return true;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to delete profile '{name}'", ex);
        }
    }

    /// <summary>
    /// Lists all available profiles
    /// </summary>
    public async Task<List<string>> ListProfilesAsync()
    {
        try
        {
            return await Task.Run(() =>
            {
                var profiles = new List<string>();
                if (Directory.Exists(_profileStoragePath))
                {
                    var files = Directory.GetFiles(_profileStoragePath, "*.json");
                    foreach (var file in files)
                    {
                        profiles.Add(Path.GetFileNameWithoutExtension(file));
                    }
                }
                return profiles;
            });
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to list profiles", ex);
        }
    }

    /// <summary>
    /// Exports profile to JSON string
    /// </summary>
    public async Task<string> ExportProfileAsync(string name)
    {
        try
        {
            var profilePath = Path.Combine(_profileStoragePath, $"{name}.json");

            if (!File.Exists(profilePath))
                throw new FileNotFoundException($"Profile '{name}' not found");

            return await File.ReadAllTextAsync(profilePath);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to export profile '{name}'", ex);
        }
    }

    /// <summary>
    /// Imports profile from JSON string
    /// </summary>
    public async Task<bool> ImportProfileAsync(string name, string jsonContent)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Profile name cannot be empty", nameof(name));

            if (string.IsNullOrWhiteSpace(jsonContent))
                throw new ArgumentException("JSON content cannot be empty", nameof(jsonContent));

            JsonDocument.Parse(jsonContent);

            var profilePath = Path.Combine(_profileStoragePath, $"{name}.json");
            await File.WriteAllTextAsync(profilePath, jsonContent);

            return true;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to import profile '{name}'", ex);
        }
    }

    private static object ExtractValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonValueKind.Number => element.TryGetInt64(out var lng) ? lng : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Array => element.EnumerateArray().Select(ExtractValue).ToList(),
            JsonValueKind.Object => element.EnumerateObject().ToDictionary(p => p.Name, p => ExtractValue(p.Value)),
            _ => string.Empty
        };
    }
}
