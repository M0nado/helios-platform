using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HELIOS.Platform.Core.Logging;

namespace HELIOS.Platform.Phase10.BootEnvironment
{
    /// <summary>
    /// Manages boot menus and boot options for both UEFI and legacy BIOS.
    /// Handles menu creation, entry management, default selection, and timeout configuration.
    /// </summary>
    public class BootMenuManager
    {
        private readonly ILogger _logger;
        private readonly SemaphoreSlim _semaphore;

        public BootMenuManager(ILogger logger = null)
        {
            _logger = logger ?? new ConsoleLogger();
            _semaphore = new SemaphoreSlim(1, 1);
        }

        /// <summary>
        /// Creates a new boot menu with specified entries.
        /// </summary>
        public async Task<BootConfiguration> CreateBootMenuAsync(List<string> menuItems)
        {
            await _semaphore.WaitAsync();
            try
            {
                _logger.Info($"Creating boot menu with {menuItems?.Count ?? 0} items");

                if (menuItems == null || menuItems.Count == 0)
                {
                    _logger.Error("Menu items list cannot be null or empty");
                    return null;
                }

                var config = new BootConfiguration
                {
                    DefaultBootOption = 0,
                    BootTimeoutSeconds = 30,
                    EnableGraphicalMenu = true,
                    EnableNetworkBoot = false,
                    MenuEntries = new List<BootMenuEntry>()
                };

                for (int i = 0; i < menuItems.Count; i++)
                {
                    var entry = new BootMenuEntry
                    {
                        OrderIndex = i,
                        DisplayName = menuItems[i],
                        Description = $"Boot option {i + 1}",
                        IsDefault = (i == 0),
                        LoaderPath = $"/boot/loader{i}.efi"
                    };

                    config.MenuEntries.Add(entry);
                    _logger.Debug($"Added menu entry: {entry.DisplayName}");
                }

                _logger.Info("Boot menu created successfully");
                return config;
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to create boot menu", ex);
                return null;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Updates boot menu configuration from file.
        /// </summary>
        public async Task<bool> UpdateBootMenuAsync(string bootConfigPath, BootConfiguration config)
        {
            await _semaphore.WaitAsync();
            try
            {
                _logger.Info($"Updating boot menu at: {bootConfigPath}");

                if (string.IsNullOrWhiteSpace(bootConfigPath))
                {
                    _logger.Error("Boot config path cannot be null or empty");
                    return false;
                }

                if (config == null)
                {
                    _logger.Error("Boot configuration cannot be null");
                    return false;
                }

                var configDir = Path.GetDirectoryName(bootConfigPath);
                if (!Directory.Exists(configDir))
                {
                    Directory.CreateDirectory(configDir);
                }

                // Write configuration to file
                var configContent = await SerializeBootConfigAsync(config);
                await File.WriteAllTextAsync(bootConfigPath, configContent);

                _logger.Info("Boot menu updated successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to update boot menu", ex);
                return false;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Sets the default boot option.
        /// </summary>
        public async Task<bool> SetDefaultBootOptionAsync(string bootConfigPath, int optionIndex)
        {
            await _semaphore.WaitAsync();
            try
            {
                _logger.Info($"Setting default boot option to index: {optionIndex}");

                if (string.IsNullOrWhiteSpace(bootConfigPath))
                {
                    _logger.Error("Boot config path cannot be null or empty");
                    return false;
                }

                if (optionIndex < 0)
                {
                    _logger.Error("Option index must be non-negative");
                    return false;
                }

                // Read current configuration
                var config = await ReadBootConfigAsync(bootConfigPath);
                if (config == null)
                {
                    _logger.Error("Failed to read boot configuration");
                    return false;
                }

                if (optionIndex >= config.MenuEntries.Count)
                {
                    _logger.Error($"Option index {optionIndex} exceeds menu entries");
                    return false;
                }

                // Update default option
                foreach (var entry in config.MenuEntries)
                {
                    entry.IsDefault = (entry.OrderIndex == optionIndex);
                }

                config.DefaultBootOption = optionIndex;

                // Write updated configuration
                var configContent = await SerializeBootConfigAsync(config);
                await File.WriteAllTextAsync(bootConfigPath, configContent);

                _logger.Info($"Default boot option set to: {optionIndex}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to set default boot option", ex);
                return false;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Sets boot timeout in seconds.
        /// </summary>
        public async Task<bool> SetBootTimeoutAsync(string bootConfigPath, int timeoutSeconds)
        {
            await _semaphore.WaitAsync();
            try
            {
                _logger.Info($"Setting boot timeout to {timeoutSeconds} seconds");

                if (timeoutSeconds < 0 || timeoutSeconds > 3600)
                {
                    _logger.Error("Timeout must be between 0 and 3600 seconds");
                    return false;
                }

                var config = await ReadBootConfigAsync(bootConfigPath);
                if (config == null)
                {
                    return false;
                }

                config.BootTimeoutSeconds = timeoutSeconds;

                var configContent = await SerializeBootConfigAsync(config);
                await File.WriteAllTextAsync(bootConfigPath, configContent);

                _logger.Info("Boot timeout updated successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to set boot timeout", ex);
                return false;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Adds a new boot menu entry.
        /// </summary>
        public async Task<bool> AddMenuEntryAsync(string bootConfigPath, string displayName, string loaderPath)
        {
            await _semaphore.WaitAsync();
            try
            {
                _logger.Info($"Adding menu entry: {displayName}");

                var config = await ReadBootConfigAsync(bootConfigPath);
                if (config == null)
                {
                    _logger.Error("Failed to read boot configuration");
                    return false;
                }

                var newEntry = new BootMenuEntry
                {
                    OrderIndex = config.MenuEntries.Count,
                    DisplayName = displayName,
                    LoaderPath = loaderPath,
                    Description = $"Boot option {config.MenuEntries.Count + 1}",
                    IsDefault = false
                };

                config.MenuEntries.Add(newEntry);

                var configContent = await SerializeBootConfigAsync(config);
                await File.WriteAllTextAsync(bootConfigPath, configContent);

                _logger.Info("Menu entry added successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to add menu entry", ex);
                return false;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Removes a boot menu entry.
        /// </summary>
        public async Task<bool> RemoveMenuEntryAsync(string bootConfigPath, int entryIndex)
        {
            await _semaphore.WaitAsync();
            try
            {
                _logger.Info($"Removing menu entry at index: {entryIndex}");

                var config = await ReadBootConfigAsync(bootConfigPath);
                if (config == null)
                {
                    return false;
                }

                if (entryIndex < 0 || entryIndex >= config.MenuEntries.Count)
                {
                    _logger.Error($"Invalid entry index: {entryIndex}");
                    return false;
                }

                config.MenuEntries.RemoveAt(entryIndex);

                // Re-order remaining entries
                for (int i = 0; i < config.MenuEntries.Count; i++)
                {
                    config.MenuEntries[i].OrderIndex = i;
                }

                var configContent = await SerializeBootConfigAsync(config);
                await File.WriteAllTextAsync(bootConfigPath, configContent);

                _logger.Info("Menu entry removed successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to remove menu entry", ex);
                return false;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Enables or disables graphical boot menu.
        /// </summary>
        public async Task<bool> SetGraphicalMenuAsync(string bootConfigPath, bool enabled)
        {
            await _semaphore.WaitAsync();
            try
            {
                _logger.Info($"Setting graphical menu to: {enabled}");

                var config = await ReadBootConfigAsync(bootConfigPath);
                if (config == null)
                {
                    return false;
                }

                config.EnableGraphicalMenu = enabled;

                var configContent = await SerializeBootConfigAsync(config);
                await File.WriteAllTextAsync(bootConfigPath, configContent);

                return true;
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to set graphical menu", ex);
                return false;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        // Private helper methods

        private async Task<string> SerializeBootConfigAsync(BootConfiguration config)
        {
            try
            {
                var lines = new List<string>
                {
                    "[BOOT_MENU_CONFIGURATION]",
                    $"DefaultBootOption={config.DefaultBootOption}",
                    $"BootTimeoutSeconds={config.BootTimeoutSeconds}",
                    $"EnableGraphicalMenu={config.EnableGraphicalMenu}",
                    $"EnableNetworkBoot={config.EnableNetworkBoot}",
                    "",
                    "[MENU_ENTRIES]"
                };

                foreach (var entry in config.MenuEntries)
                {
                    lines.Add($"Entry{entry.OrderIndex}DisplayName={entry.DisplayName}");
                    lines.Add($"Entry{entry.OrderIndex}LoaderPath={entry.LoaderPath}");
                    lines.Add($"Entry{entry.OrderIndex}Description={entry.Description}");
                    lines.Add($"Entry{entry.OrderIndex}IsDefault={entry.IsDefault}");
                    lines.Add("");
                }

                await Task.CompletedTask;
                return string.Join("\n", lines);
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to serialize boot configuration", ex);
                return null;
            }
        }

        private async Task<BootConfiguration> ReadBootConfigAsync(string bootConfigPath)
        {
            try
            {
                if (!File.Exists(bootConfigPath))
                {
                    _logger.Warning($"Boot config file not found: {bootConfigPath}");
                    return null;
                }

                var content = await File.ReadAllTextAsync(bootConfigPath);
                var config = new BootConfiguration { MenuEntries = new List<BootMenuEntry>() };

                // Parse configuration
                var lines = content.Split('\n');
                foreach (var line in lines)
                {
                    if (line.StartsWith("DefaultBootOption="))
                    {
                        config.DefaultBootOption = int.Parse(line.Split('=')[1]);
                    }
                    else if (line.StartsWith("BootTimeoutSeconds="))
                    {
                        config.BootTimeoutSeconds = int.Parse(line.Split('=')[1]);
                    }
                    else if (line.StartsWith("EnableGraphicalMenu="))
                    {
                        config.EnableGraphicalMenu = bool.Parse(line.Split('=')[1]);
                    }
                }

                return config;
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to read boot configuration", ex);
                return null;
            }
        }
    }
}
