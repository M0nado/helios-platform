using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HELIOS.Platform.BackendServices.Software.Models;
using HELIOS.Platform.BackendServices.Software.Managers;
using HELIOS.Platform.BackendServices.Software.Packages;

namespace HELIOS.Platform.Core.CLI.Commands
{
    /// <summary>
    /// CLI commands for software management
    /// </summary>
    public class SoftwareCommands
    {
        private readonly SoftwareManager _manager;

        public SoftwareCommands(SoftwareManager manager)
        {
            _manager = manager;
        }

        // ===== LIST COMMANDS =====
        /// <summary>
        /// List all available software packages
        /// </summary>
        public async Task ListAllAsync()
        {
            Console.WriteLine("\n╔════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║         AVAILABLE SOFTWARE PACKAGES (500+)                   ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════════╝\n");

            var packages = PackageRegistry.GetAllPackages();
            Console.WriteLine($"Total Packages: {packages.Count}\n");

            foreach (var category in SoftwareCategory.All)
            {
                var catPackages = packages.Where(p => p.Category == category).ToList();
                if (catPackages.Any())
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"  [{category}] ({catPackages.Count} packages)");
                    Console.ResetColor();

                    foreach (var pkg in catPackages.Take(3))
                    {
                        Console.WriteLine($"    • {pkg.Name}");
                    }

                    if (catPackages.Count > 3)
                    {
                        Console.WriteLine($"    ... and {catPackages.Count - 3} more");
                    }
                    Console.WriteLine();
                }
            }

            await Task.Delay(0);
        }

        /// <summary>
        /// List installed software
        /// </summary>
        public async Task ListInstalledAsync()
        {
            Console.WriteLine("\n╔════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║              INSTALLED SOFTWARE                              ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════════╝\n");

            var installed = await _manager.ScanInstalledSoftwareAsync();
            
            if (!installed.Any())
            {
                Console.WriteLine("No installed software found in registry.\n");
                return;
            }

            Console.WriteLine($"Total Installed: {installed.Count}\n");

            foreach (var pkg in installed.OrderBy(p => p.Name))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("  ✓ ");
                Console.ResetColor();
                Console.WriteLine($"{pkg.Name,-40} v{pkg.CurrentVersion}");
            }

            Console.WriteLine();
        }

        /// <summary>
        /// List packages by category
        /// </summary>
        public async Task ListCategoryAsync(string category)
        {
            Console.WriteLine($"\n╔════════════════════════════════════════════════════════════════╗");
            Console.WriteLine($"║  {category} PACKAGES");
            Console.WriteLine($"╚════════════════════════════════════════════════════════════════╝\n");

            var packages = PackageRegistry.GetByCategory(category);

            if (!packages.Any())
            {
                Console.WriteLine($"No packages found in category: {category}\n");
                return;
            }

            Console.WriteLine($"Total: {packages.Count} packages\n");

            var installed = await _manager.ScanInstalledSoftwareAsync();

            foreach (var pkg in packages.OrderBy(p => p.Name))
            {
                var isInstalled = installed.Any(i => i.Name == pkg.Name);
                var status = isInstalled ? "✓" : "○";
                var color = isInstalled ? ConsoleColor.Green : ConsoleColor.Gray;

                Console.ForegroundColor = color;
                Console.Write($"  [{status}] ");
                Console.ResetColor();
                Console.WriteLine($"{pkg.Name,-40} ({pkg.Publisher})");
            }

            Console.WriteLine();
            await Task.Delay(0);
        }

        // ===== SEARCH COMMAND =====
        /// <summary>
        /// Search for packages
        /// </summary>
        public async Task SearchAsync(string query)
        {
            Console.WriteLine($"\n╔════════════════════════════════════════════════════════════════╗");
            Console.WriteLine($"║  SEARCH RESULTS FOR: \"{query}\"");
            Console.WriteLine($"╚════════════════════════════════════════════════════════════════╝\n");

            var results = await _manager.SearchAsync(query);

            if (!results.Any())
            {
                Console.WriteLine("No packages found matching your search.\n");
                return;
            }

            Console.WriteLine($"Found: {results.Count} packages\n");

            foreach (var pkg in results)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write("  → ");
                Console.ResetColor();
                Console.WriteLine($"{pkg.Name,-40} [{pkg.Category}]");
                if (!string.IsNullOrEmpty(pkg.Description))
                {
                    Console.WriteLine($"     {pkg.Description}");
                }
            }

            Console.WriteLine();
        }

        // ===== INSTALL COMMANDS =====
        /// <summary>
        /// Install a specific package
        /// </summary>
        public async Task InstallAsync(string packageName, string method = null)
        {
            Console.WriteLine($"\n╔════════════════════════════════════════════════════════════════╗");
            Console.WriteLine($"║  INSTALLING: {packageName}");
            Console.WriteLine($"╚════════════════════════════════════════════════════════════════╝\n");

            var result = await _manager.InstallAsync(packageName, method);

            if (result)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"✓ Successfully installed {packageName}\n");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"✗ Failed to install {packageName}\n");
                Console.ResetColor();
            }
        }

        /// <summary>
        /// Bulk install multiple packages from file
        /// </summary>
        public async Task BulkInstallAsync(List<string> packageNames)
        {
            Console.WriteLine($"\n╔════════════════════════════════════════════════════════════════╗");
            Console.WriteLine($"║  BULK INSTALL: {packageNames.Count} packages");
            Console.WriteLine($"╚════════════════════════════════════════════════════════════════╝\n");

            Console.WriteLine("Packages to install:");
            foreach (var pkg in packageNames)
            {
                Console.WriteLine($"  • {pkg}");
            }
            Console.WriteLine();

            var result = await _manager.BulkInstallAsync(packageNames);

            Console.WriteLine($"\n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            Console.WriteLine($"Installation Complete:");
            Console.WriteLine($"  Total Attempted: {result.TotalAttempted}");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"  Successful: {result.SuccessfulInstallations}");
            Console.ResetColor();
            if (result.FailedInstallations > 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  Failed: {result.FailedInstallations}");
                Console.ResetColor();
            }
            Console.WriteLine($"  Duration: {result.Duration.TotalMinutes:F2} minutes");
            Console.WriteLine($"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n");
        }

        // ===== UNINSTALL COMMANDS =====
        /// <summary>
        /// Uninstall a package
        /// </summary>
        public async Task UninstallAsync(string packageName)
        {
            Console.WriteLine($"\n╔════════════════════════════════════════════════════════════════╗");
            Console.WriteLine($"║  UNINSTALLING: {packageName}");
            Console.WriteLine($"╚════════════════════════════════════════════════════════════════╝\n");

            var result = await _manager.UninstallAsync(packageName);

            if (result)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"✓ Successfully uninstalled {packageName}\n");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"✗ Failed to uninstall {packageName}\n");
                Console.ResetColor();
            }
        }

        // ===== UPDATE COMMANDS =====
        /// <summary>
        /// Check for updates
        /// </summary>
        public async Task CheckUpdatesAsync()
        {
            Console.WriteLine($"\n╔════════════════════════════════════════════════════════════════╗");
            Console.WriteLine($"║  CHECKING FOR UPDATES");
            Console.WriteLine($"╚════════════════════════════════════════════════════════════════╝\n");

            Console.WriteLine("Scanning installed packages for updates...\n");

            var updatable = await _manager.CheckForUpdatesAsync();

            if (!updatable.Any())
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✓ All packages are up to date!\n");
                Console.ResetColor();
                return;
            }

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Found {updatable.Count} packages with available updates:\n");
            Console.ResetColor();

            foreach (var pkg in updatable)
            {
                Console.WriteLine($"  • {pkg.Name}");
                Console.WriteLine($"    Current: {pkg.CurrentVersion} → Latest: {pkg.LatestVersion}");
            }

            Console.WriteLine();
        }

        /// <summary>
        /// Update a specific package
        /// </summary>
        public async Task UpdateAsync(string packageName)
        {
            Console.WriteLine($"\n╔════════════════════════════════════════════════════════════════╗");
            Console.WriteLine($"║  UPDATING: {packageName}");
            Console.WriteLine($"╚════════════════════════════════════════════════════════════════╝\n");

            var result = await _manager.UpdateAsync(packageName);

            if (result)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"✓ Successfully updated {packageName}\n");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"✗ Failed to update {packageName}\n");
                Console.ResetColor();
            }
        }

        /// <summary>
        /// Update all packages
        /// </summary>
        public async Task UpdateAllAsync()
        {
            Console.WriteLine($"\n╔════════════════════════════════════════════════════════════════╗");
            Console.WriteLine($"║  UPDATING ALL PACKAGES");
            Console.WriteLine($"╚════════════════════════════════════════════════════════════════╝\n");

            Console.WriteLine("Checking and updating all packages...\n");

            var result = await _manager.UpdateAllAsync();

            if (result)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✓ All updates completed successfully\n");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("⚠ Updates completed with some issues\n");
                Console.ResetColor();
            }
        }

        // ===== HEALTH COMMANDS =====
        /// <summary>
        /// Verify software health
        /// </summary>
        public async Task VerifyHealthAsync()
        {
            Console.WriteLine($"\n╔════════════════════════════════════════════════════════════════╗");
            Console.WriteLine($"║  SOFTWARE HEALTH CHECK");
            Console.WriteLine($"╚════════════════════════════════════════════════════════════════╝\n");

            Console.WriteLine("Scanning system...\n");

            var report = await _manager.VerifyHealthAsync();

            Console.WriteLine($"  Total Packages: {report.TotalPackages}");
            Console.WriteLine($"  Installed: {report.InstalledPackages}");
            Console.WriteLine($"  Failed Installations: {report.FailedInstallations}");
            Console.WriteLine($"  Pending Updates: {report.PackagesPendingUpdate}");
            Console.WriteLine();
            Console.WriteLine($"  Installation Rate: {report.InstallationRate:F1}%");
            Console.WriteLine($"  Update Required: {report.UpdateRequiredRate:F1}%");
            Console.WriteLine();
        }

        // ===== REGISTRY COMMANDS =====
        /// <summary>
        /// Show registry status
        /// </summary>
        public async Task ShowRegistryAsync()
        {
            Console.WriteLine($"\n╔════════════════════════════════════════════════════════════════╗");
            Console.WriteLine($"║  SOFTWARE REGISTRY STATUS");
            Console.WriteLine($"╚════════════════════════════════════════════════════════════════╝\n");

            var registry = _manager.GetRegistry();

            if (!registry.Any())
            {
                Console.WriteLine("Registry is empty.\n");
                return;
            }

            Console.WriteLine($"Total Registered Packages: {registry.Count}\n");

            var byStatus = registry.GroupBy(p => p.Status);
            foreach (var group in byStatus)
            {
                Console.ForegroundColor = GetStatusColor(group.Key);
                Console.WriteLine($"  {group.Key}: {group.Count()}");
                Console.ResetColor();
            }

            Console.WriteLine();
            await Task.Delay(0);
        }

        // ===== CATEGORY COMMANDS =====
        /// <summary>
        /// List all categories
        /// </summary>
        public async Task ListCategoriesAsync()
        {
            Console.WriteLine($"\n╔════════════════════════════════════════════════════════════════╗");
            Console.WriteLine($"║  SOFTWARE CATEGORIES");
            Console.WriteLine($"╚════════════════════════════════════════════════════════════════╝\n");

            foreach (var category in SoftwareCategory.All)
            {
                var count = PackageRegistry.GetByCategory(category).Count;
                Console.WriteLine($"  • {category,-25} ({count} packages)");
            }

            Console.WriteLine();
            await Task.Delay(0);
        }

        // ===== HELP AND INFO =====
        /// <summary>
        /// Show help
        /// </summary>
        public void ShowHelp()
        {
            Console.WriteLine($"\n╔════════════════════════════════════════════════════════════════╗");
            Console.WriteLine($"║  SOFTWARE MANAGEMENT COMMANDS");
            Console.WriteLine($"╚════════════════════════════════════════════════════════════════╝\n");

            Console.WriteLine("LIST COMMANDS:");
            Console.WriteLine("  helios-cli software list              List all available packages");
            Console.WriteLine("  helios-cli software list-installed    List installed software");
            Console.WriteLine("  helios-cli software list-categories   List all categories");
            Console.WriteLine("  helios-cli software list <category>   List packages in category");
            Console.WriteLine();

            Console.WriteLine("SEARCH & INFO:");
            Console.WriteLine("  helios-cli software search <query>    Search for packages");
            Console.WriteLine("  helios-cli software info <package>    Show package details");
            Console.WriteLine();

            Console.WriteLine("INSTALLATION:");
            Console.WriteLine("  helios-cli software install <name>    Install a package");
            Console.WriteLine("  helios-cli software bulk-install      Bulk install from config");
            Console.WriteLine();

            Console.WriteLine("REMOVAL:");
            Console.WriteLine("  helios-cli software uninstall <name>  Uninstall a package");
            Console.WriteLine();

            Console.WriteLine("UPDATES:");
            Console.WriteLine("  helios-cli software check-updates     Check for available updates");
            Console.WriteLine("  helios-cli software update <name>     Update a specific package");
            Console.WriteLine("  helios-cli software update-all        Update all packages");
            Console.WriteLine();

            Console.WriteLine("SYSTEM:");
            Console.WriteLine("  helios-cli software verify            Verify system health");
            Console.WriteLine("  helios-cli software registry          Show registry status");
            Console.WriteLine("  helios-cli software scan              Scan installed software");
            Console.WriteLine();
        }

        private ConsoleColor GetStatusColor(string status)
        {
            return status switch
            {
                SoftwareStatus.Installed => ConsoleColor.Green,
                SoftwareStatus.Failed => ConsoleColor.Red,
                SoftwareStatus.Updating => ConsoleColor.Yellow,
                SoftwareStatus.Pending => ConsoleColor.Cyan,
                _ => ConsoleColor.White
            };
        }
    }
}
