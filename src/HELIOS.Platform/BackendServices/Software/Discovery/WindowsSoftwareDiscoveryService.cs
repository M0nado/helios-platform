using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using HELIOS.Platform.BackendServices.Software.Models;

namespace HELIOS.Platform.BackendServices.Software.Discovery
{
    /// <summary>
    /// Discovers installed software on Windows systems using multiple methods
    /// </summary>
    public class WindowsSoftwareDiscoveryService : ISoftwareDiscoveryService
    {
        private readonly Dictionary<string, SoftwarePackage> _knownPackages;

        public WindowsSoftwareDiscoveryService()
        {
            _knownPackages = new Dictionary<string, SoftwarePackage>();
            InitializeKnownPackages();
        }

        /// <summary>
        /// Discover all installed software on the system
        /// </summary>
        public async Task<List<SoftwarePackage>> DiscoverInstalledSoftwareAsync()
        {
            var discovered = new List<SoftwarePackage>();

            // Get from Windows Registry (HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall)
            discovered.AddRange(await GetFromRegistryAsync());

            // Get from Windows Package Manager (winget)
            discovered.AddRange(await GetFromWingetAsync());

            // Get from Chocolatey
            discovered.AddRange(await GetFromChocolateyAsync());

            // Get from Steam
            discovered.AddRange(await GetFromSteamAsync());

            // Merge with known packages and set installation status
            foreach (var package in discovered)
            {
                if (_knownPackages.ContainsKey(package.Name))
                {
                    var known = _knownPackages[package.Name];
                    package.Category = known.Category;
                    package.Tags = known.Tags;
                    package.InstallationMethods = known.InstallationMethods;
                    package.PreferredMethod = known.PreferredMethod;
                }
                package.IsInstalled = true;
                package.Status = SoftwareStatus.Installed;
            }

            return discovered.DistinctBy(p => p.Name).ToList();
        }

        /// <summary>
        /// Get software from Windows Registry
        /// </summary>
        private async Task<List<SoftwarePackage>> GetFromRegistryAsync()
        {
            var packages = new List<SoftwarePackage>();

            try
            {
                var result = await ExecuteCommandAsync(
                    @"powershell -Command ""Get-ItemProperty HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\* | Select-Object DisplayName, DisplayVersion | ConvertTo-Json""");

                if (result.Success)
                {
                    // Parse JSON result (simplified)
                    var lines = result.Output.Split('\n');
                    foreach (var line in lines)
                    {
                        if (line.Contains("DisplayName"))
                        {
                            var name = line.Split(':')[1]?.Trim().Trim('"', ',');
                            if (!string.IsNullOrEmpty(name))
                            {
                                packages.Add(new SoftwarePackage
                                {
                                    Id = Guid.NewGuid().ToString(),
                                    Name = name,
                                    Status = SoftwareStatus.Installed
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Registry discovery failed: {ex.Message}");
            }

            return packages;
        }

        /// <summary>
        /// Get software from Windows Package Manager (winget)
        /// </summary>
        private async Task<List<SoftwarePackage>> GetFromWingetAsync()
        {
            var packages = new List<SoftwarePackage>();

            try
            {
                var result = await ExecuteCommandAsync("winget list --accept-source-agreements");

                if (result.Success)
                {
                    var lines = result.Output.Split('\n');
                    foreach (var line in lines.Skip(1)) // Skip header
                    {
                        if (string.IsNullOrWhiteSpace(line))
                            continue;

                        var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 2)
                        {
                            packages.Add(new SoftwarePackage
                            {
                                Id = Guid.NewGuid().ToString(),
                                Name = parts[0],
                                CurrentVersion = parts.Length > 1 ? parts[1] : "Unknown",
                                InstallationMethods = new List<string> { InstallationMethod.Winget },
                                Status = SoftwareStatus.Installed
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Winget discovery failed: {ex.Message}");
            }

            return packages;
        }

        /// <summary>
        /// Get software from Chocolatey
        /// </summary>
        private async Task<List<SoftwarePackage>> GetFromChocolateyAsync()
        {
            var packages = new List<SoftwarePackage>();

            try
            {
                var result = await ExecuteCommandAsync("choco list -l");

                if (result.Success)
                {
                    var lines = result.Output.Split('\n');
                    foreach (var line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line) || line.StartsWith("Chocolatey"))
                            continue;

                        var parts = line.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 2)
                        {
                            packages.Add(new SoftwarePackage
                            {
                                Id = Guid.NewGuid().ToString(),
                                Name = parts[0],
                                CurrentVersion = parts[1],
                                InstallationMethods = new List<string> { InstallationMethod.Chocolatey },
                                Status = SoftwareStatus.Installed
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Chocolatey discovery failed: {ex.Message}");
            }

            return packages;
        }

        /// <summary>
        /// Get games from Steam
        /// </summary>
        private async Task<List<SoftwarePackage>> GetFromSteamAsync()
        {
            var packages = new List<SoftwarePackage>();

            try
            {
                // Steam game library location
                string steamPath = @"C:\Program Files (x86)\Steam\steamapps\common";

                if (System.IO.Directory.Exists(steamPath))
                {
                    var folders = System.IO.Directory.GetDirectories(steamPath);
                    foreach (var folder in folders)
                    {
                        var folderName = System.IO.Path.GetFileName(folder);
                        packages.Add(new SoftwarePackage
                        {
                            Id = Guid.NewGuid().ToString(),
                            Name = folderName,
                            Category = SoftwareCategory.Gaming,
                            InstallationMethods = new List<string> { InstallationMethod.Steam },
                            IsInstalled = true,
                            Status = SoftwareStatus.Installed
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Steam discovery failed: {ex.Message}");
            }

            return packages;
        }

        /// <summary>
        /// Check if a specific package is installed
        /// </summary>
        public async Task<bool> IsPackageInstalledAsync(string packageName)
        {
            var result = await ExecuteCommandAsync($"winget list {packageName}");
            return result.Success;
        }

        /// <summary>
        /// Get installed version of a package
        /// </summary>
        public async Task<string> GetInstalledVersionAsync(string packageName)
        {
            var result = await ExecuteCommandAsync($"winget show {packageName}");

            if (result.Success)
            {
                var lines = result.Output.Split('\n');
                foreach (var line in lines)
                {
                    if (line.Contains("Version"))
                    {
                        return line.Split(':')[1]?.Trim();
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Search for packages in known package database
        /// </summary>
        public async Task<List<SoftwarePackage>> SearchPackagesAsync(string query)
        {
            await Task.Delay(0); // Make it async

            return _knownPackages.Values
                .Where(p => p.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                           p.Description?.Contains(query, StringComparison.OrdinalIgnoreCase) == true ||
                           p.Tags.Any(t => t.Contains(query, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        /// <summary>
        /// Get package details
        /// </summary>
        public async Task<SoftwarePackage> GetPackageDetailsAsync(string packageId)
        {
            await Task.Delay(0); // Make it async

            return _knownPackages.Values.FirstOrDefault(p => p.Id == packageId || p.Name == packageId);
        }

        /// <summary>
        /// Execute a shell command
        /// </summary>
        private async Task<CommandResult> ExecuteCommandAsync(string command)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {command}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(psi))
                {
                    var output = await process.StandardOutput.ReadToEndAsync();
                    var error = await process.StandardError.ReadToEndAsync();

                    await Task.Run(() => process.WaitForExit(30000));

                    return new CommandResult
                    {
                        Success = process.ExitCode == 0,
                        Output = output,
                        Error = error,
                        ExitCode = process.ExitCode
                    };
                }
            }
            catch (Exception ex)
            {
                return new CommandResult
                {
                    Success = false,
                    Error = ex.Message,
                    ExitCode = -1
                };
            }
        }

        /// <summary>
        /// Initialize known packages database
        /// </summary>
        private void InitializeKnownPackages()
        {
            // Development Tools
            AddKnownPackage("vscode", "Visual Studio Code", SoftwareCategory.Development, InstallationMethod.Winget);
            AddKnownPackage("git", "Git", SoftwareCategory.Development, InstallationMethod.Winget);
            AddKnownPackage("python", "Python", SoftwareCategory.Development, InstallationMethod.Winget);
            AddKnownPackage("nodejs", "Node.js", SoftwareCategory.Development, InstallationMethod.Winget);
            AddKnownPackage("dotnet", ".NET SDK", SoftwareCategory.Development, InstallationMethod.Official);
            AddKnownPackage("docker", "Docker Desktop", SoftwareCategory.Development, InstallationMethod.Official);

            // Browsers
            AddKnownPackage("chrome", "Google Chrome", SoftwareCategory.Browsers, InstallationMethod.Official);
            AddKnownPackage("firefox", "Mozilla Firefox", SoftwareCategory.Browsers, InstallationMethod.Winget);
            AddKnownPackage("brave", "Brave Browser", SoftwareCategory.Browsers, InstallationMethod.Winget);
            AddKnownPackage("edge", "Microsoft Edge", SoftwareCategory.Browsers, InstallationMethod.Winget);

            // Communication
            AddKnownPackage("discord", "Discord", SoftwareCategory.Communication, InstallationMethod.Winget);
            AddKnownPackage("slack", "Slack", SoftwareCategory.Communication, InstallationMethod.Winget);
            AddKnownPackage("telegram", "Telegram", SoftwareCategory.Communication, InstallationMethod.Winget);
            AddKnownPackage("zoom", "Zoom", SoftwareCategory.Communication, InstallationMethod.Official);

            // Media & Streaming
            AddKnownPackage("vlc", "VLC Media Player", SoftwareCategory.Media, InstallationMethod.Winget);
            AddKnownPackage("obs", "OBS Studio", SoftwareCategory.Media, InstallationMethod.Winget);
            AddKnownPackage("audacity", "Audacity", SoftwareCategory.Media, InstallationMethod.Winget);
            AddKnownPackage("blender", "Blender", SoftwareCategory.Media, InstallationMethod.Official);

            // System Tools
            AddKnownPackage("7zip", "7-Zip", SoftwareCategory.SystemTools, InstallationMethod.Chocolatey);
            AddKnownPackage("autohotkey", "AutoHotkey", SoftwareCategory.SystemTools, InstallationMethod.Chocolatey);
            AddKnownPackage("processhacker", "Process Hacker", SoftwareCategory.SystemTools, InstallationMethod.Portable);

            // Security
            AddKnownPackage("malwarebytes", "Malwarebytes", SoftwareCategory.Security, InstallationMethod.Official);
            AddKnownPackage("windirstat", "WinDirStat", SoftwareCategory.SystemTools, InstallationMethod.Portable);
        }

        private void AddKnownPackage(string id, string name, string category, string preferredMethod)
        {
            _knownPackages[id] = new SoftwarePackage
            {
                Id = id,
                Name = name,
                Category = category,
                PreferredMethod = preferredMethod,
                InstallationMethods = new List<string> { preferredMethod },
                Status = SoftwareStatus.NotInstalled
            };
        }
    }

    /// <summary>
    /// Result from executing a command
    /// </summary>
    internal class CommandResult
    {
        public bool Success { get; set; }
        public string Output { get; set; }
        public string Error { get; set; }
        public int ExitCode { get; set; }
    }
}
