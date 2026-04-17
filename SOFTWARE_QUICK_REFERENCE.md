# Software Lifecycle Management - Quick Reference

## 🎯 Getting Started (5 minutes)

### Initialize the Software Manager
```csharp
using HELIOS.Platform.BackendServices.Software;
using HELIOS.Platform.BackendServices.Software.Discovery;
using HELIOS.Platform.BackendServices.Software.Installers;
using HELIOS.Platform.BackendServices.Software.Managers;

// Create services
var discoveryService = new WindowsSoftwareDiscoveryService();
var installerService = new SoftwareInstallerService();
var updateManager = new UpdateManager(installerService, discoveryService);

// Create main manager
var softwareManager = new SoftwareManager(
    discoveryService,
    installerService,
    updateManager
);
```

## 📦 Core Operations

### Scan Installed Software
```csharp
var installed = await softwareManager.ScanInstalledSoftwareAsync();
Console.WriteLine($"Found {installed.Count} installed packages");
```

### Search for Packages
```csharp
var results = await softwareManager.SearchAsync("python");
foreach (var pkg in results)
{
    Console.WriteLine($"{pkg.Name} - {pkg.Category}");
}
```

### Install a Package
```csharp
bool success = await softwareManager.InstallAsync("vscode");
if (success)
    Console.WriteLine("✓ Successfully installed Visual Studio Code");
```

### Bulk Install
```csharp
var packages = new List<string> { "python", "git", "nodejs" };
var result = await softwareManager.BulkInstallAsync(packages);
Console.WriteLine($"Installed {result.SuccessfulInstallations}/{result.TotalAttempted}");
```

### Check for Updates
```csharp
var updatable = await softwareManager.CheckForUpdatesAsync();
foreach (var pkg in updatable)
{
    Console.WriteLine($"{pkg.Name}: {pkg.CurrentVersion} → {pkg.LatestVersion}");
}
```

### Update All Packages
```csharp
bool success = await softwareManager.UpdateAllAsync();
```

### Health Check
```csharp
var health = await softwareManager.VerifyHealthAsync();
Console.WriteLine($"Installation Rate: {health.InstallationRate:F1}%");
Console.WriteLine($"Updates Available: {health.PackagesPendingUpdate}");
```

## 🛠️ Installation Methods

### Specify Installation Method
```csharp
// Use specific method
await softwareManager.InstallAsync("vscode", "chocolatey");
await softwareManager.InstallAsync("python", "official");
await softwareManager.InstallAsync("redis", "docker");
```

### Available Methods
- `winget` - Windows Package Manager
- `chocolatey` - Chocolatey
- `scoop` - Scoop
- `official` - Official installer
- `portable` - Portable version
- `docker` - Docker container
- `wsl` - WSL2 Linux
- `steam` - Steam platform
- `npm` - Node package manager
- `pip` - Python package manager
- `cargo` - Rust package manager

## 📋 Filter by Category

### Get Category Packages
```csharp
var devTools = softwareManager.GetPackagesByCategory("Development");
var games = softwareManager.GetPackagesByCategory("Gaming");
var security = softwareManager.GetPackagesByCategory("Security");
```

### Available Categories
- `Development` - IDEs, compilers, SDKs (50+)
- `Browsers` - Web browsers (10+)
- `Gaming` - Games and platforms (30+)
- `Communication` - Chat and video (15+)
- `Productivity` - Office and collaboration (20+)
- `Media` - Creative tools (35+)
- `Security` - Antivirus and VPN (20+)
- `SystemTools` - Utilities and managers (40+)
- `CloudServices` - Cloud platforms (12+)
- `Utilities` - Additional tools (35+)

## 🔍 Query Package Registry

### Get All Packages
```csharp
var allPackages = PackageRegistry.GetAllPackages();
Console.WriteLine($"Total: {allPackages.Count} packages");
```

### Get Specific Package
```csharp
var vscode = PackageRegistry.GetPackage("vscode");
Console.WriteLine($"{vscode.Name} by {vscode.Publisher}");
```

### Get Category
```csharp
var dev = PackageRegistry.GetByCategory("Development");
foreach (var pkg in dev.Take(5))
{
    Console.WriteLine($"  • {pkg.Name}");
}
```

## 📊 Data Models

### SoftwarePackage
```csharp
public class SoftwarePackage
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Category { get; set; }
    public string CurrentVersion { get; set; }
    public string LatestVersion { get; set; }
    public List<string> InstallationMethods { get; set; }
    public bool IsInstalled { get; set; }
    public bool AutoUpdate { get; set; }
    public string Status { get; set; }
    public List<string> Dependencies { get; set; }
}
```

### InstallationResult
```csharp
public class InstallationResult
{
    public int TotalAttempted { get; set; }
    public int SuccessfulInstallations { get; set; }
    public int FailedInstallations { get; set; }
    public List<InstallationError> Errors { get; set; }
    public TimeSpan Duration { get; set; }
    public bool IsSuccessful { get; set; }
}
```

### SoftwareHealthReport
```csharp
public class SoftwareHealthReport
{
    public int TotalPackages { get; set; }
    public int InstalledPackages { get; set; }
    public int FailedInstallations { get; set; }
    public int PackagesPendingUpdate { get; set; }
    public double InstallationRate { get; set; }
    public double UpdateRequiredRate { get; set; }
}
```

## 🎛️ Common Scenarios

### Setup Developer Workstation
```csharp
var packages = new List<string>
{
    "vscode",
    "git",
    "python",
    "nodejs",
    "dotnet",
    "docker-desktop"
};

var result = await softwareManager.BulkInstallAsync(packages);
Console.WriteLine($"Setup complete: {result.SuccessfulInstallations} installed");
```

### Update Critical Software
```csharp
var criticalPackages = new[] { "python", "nodejs", "git" };
foreach (var pkg in criticalPackages)
{
    await softwareManager.UpdateAsync(pkg);
}
```

### Find Missing Tools
```csharp
var devTools = softwareManager.GetPackagesByCategory("Development");
var installed = await softwareManager.ScanInstalledSoftwareAsync();
var missing = devTools.Where(dt => !installed.Any(i => i.Name == dt.Name));

foreach (var pkg in missing)
{
    Console.WriteLine($"Missing: {pkg.Name}");
}
```

### Verify System Health
```csharp
var health = await softwareManager.VerifyHealthAsync();

if (health.InstallationRate < 80)
    Console.WriteLine("⚠️  Less than 80% of software installed");

if (health.PackagesPendingUpdate > 10)
    Console.WriteLine("⚠️  Multiple updates available");
```

## ⚡ Performance Tips

- **Batch Operations**: Use `BulkInstallAsync()` instead of multiple single installs
- **Async All The Way**: Always use `await` for async methods
- **Cache Results**: Store results if making multiple queries
- **Parallel Where Possible**: Winget installs run in parallel
- **Check Off-Peak**: Schedule updates during low-usage hours

## 🐛 Debugging

### Enable Verbose Logging
```csharp
var logger = new ConsoleInstallationLogger();
logger.Log("Starting installation...", LogLevel.Info);
logger.Log("Error occurred", LogLevel.Error);
logger.Log("Installation successful", LogLevel.Success);
```

### Check Installation Status
```csharp
var registry = softwareManager.GetRegistry();
var failed = registry.Where(p => p.Status == "failed");
foreach (var pkg in failed)
{
    Console.WriteLine($"Failed: {pkg.Name} - {pkg.StatusMessage}");
}
```

### Verify Package Exists
```csharp
bool exists = await softwareManager.SearchAsync("nonexistent").
    Result.Any();
```

## 🔗 Integration Examples

### With CLI
```csharp
var commands = new SoftwareCommands(softwareManager);
await commands.ListAllAsync();
await commands.SearchAsync("python");
await commands.VerifyHealthAsync();
```

### With Configuration File
```yaml
packages:
  - name: "VSCode"
    method: "winget"
  - name: "Python"
    method: "official"
```

### With Scheduler
```csharp
var schedule = await updateManager.GetUpdateScheduleAsync();
schedule.AutoUpdateEnabled = true;
schedule.UpdateDay = DayOfWeek.Sunday;
schedule.UpdateTime = new TimeSpan(2, 0, 0);
```

## 📚 Additional Resources

- **Main Documentation**: `Software/README.md`
- **Configuration Templates**: `SOFTWARE_CONFIGURATION_TEMPLATES.md`
- **Test Examples**: `SoftwareAutomationTests.cs`
- **Implementation Details**: `SOFTWARE_LIFECYCLE_IMPLEMENTATION_COMPLETE.md`

## ✅ Checklist for Setup

- [ ] Create SoftwareManager instance
- [ ] Run initial scan: `ScanInstalledSoftwareAsync()`
- [ ] Search for needed packages: `SearchAsync(query)`
- [ ] Install: `InstallAsync()` or `BulkInstallAsync()`
- [ ] Check updates: `CheckForUpdatesAsync()`
- [ ] Verify health: `VerifyHealthAsync()`
- [ ] Monitor: `ShowRegistryAsync()`

---

**Quick Start Time**: ~5 minutes  
**Full Setup Time**: 30 minutes - 2 hours (depending on packages)  
**System Requirements**: Windows 10+ with internet access

**Status**: ✅ Production Ready
