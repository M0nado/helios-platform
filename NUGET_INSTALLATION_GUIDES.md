# HELIOS Platform - Installation & Usage Guides

## A) INSTALLATION VIA NUGET.ORG

### Method 1: NuGet Package Manager (Visual Studio)

**Steps:**

1. Open Visual Studio
2. Tools → NuGet Package Manager → Manage NuGet Packages for Solution
3. Browse tab
4. Search: `HELIOS.Platform`
5. Select the package
6. Version: 1.0.0 (or latest)
7. Select project to install into
8. Click "Install"
9. Accept license terms
10. Wait for installation to complete

**Visual Studio Output:**
```
Successfully installed 'HELIOS.Platform 1.0.0' to ProjectName
```

### Method 2: Package Manager Console

```powershell
# Install latest version
Install-Package HELIOS.Platform

# Install specific version
Install-Package HELIOS.Platform -Version 1.0.0

# Install to specific project
Install-Package HELIOS.Platform -ProjectName "MyProject"

# Update to latest
Update-Package HELIOS.Platform

# Remove package
Uninstall-Package HELIOS.Platform
```

**Output Indicators:**
```
Installing NuGet package HELIOS.Platform.1.0.0.
Added file 'packages.config' to project 'MyProject'.
Successfully added 'HELIOS.Platform 1.0.0' to MyProject.
```

### Method 3: .NET CLI (Recommended for Modern Projects)

```powershell
# Navigate to your project directory
cd "C:\MyProject"

# Add package to project
dotnet add package HELIOS.Platform

# Add specific version
dotnet add package HELIOS.Platform --version 1.0.0

# List installed packages
dotnet list package

# Update package
dotnet add package HELIOS.Platform --version 1.1.0

# Remove package
dotnet remove package HELIOS.Platform
```

**Output:**
```
Writing C:\MyProject\MyProject.csproj
info : Adding PackageReference for package 'HELIOS.Platform' into project 'C:\MyProject\MyProject.csproj'.
info : Restoring packages for C:\MyProject\MyProject.csproj...
info : Package 'HELIOS.Platform' is compatible with all the specified frameworks in the project 'C:\MyProject\MyProject.csproj'.
```

### Method 4: Edit .csproj Directly

**Manual Project File Edit:**

```xml
<ItemGroup>
  <PackageReference Include="HELIOS.Platform" Version="1.0.0" />
  
  <!-- Optional: Use floating version for auto-updates -->
  <!-- <PackageReference Include="HELIOS.Platform" Version="1.0.*" /> -->
  
  <!-- Optional: Use pre-release versions -->
  <!-- <PackageReference Include="HELIOS.Platform" Version="1.1.0-beta.1" /> -->
</ItemGroup>
```

Then run:
```powershell
dotnet restore
```

### Verification Steps

```powershell
# Check package installed
dotnet list package | grep HELIOS.Platform

# Check package information
dotnet package search HELIOS.Platform

# List package details
dotnet nuget list source
```

---

## B) INSTALLATION VIA GITHUB PACKAGES

### Prerequisites

1. GitHub account
2. Personal Access Token with `read:packages` permission
3. .NET project with nuget.config

### Step 1: Create GitHub Personal Access Token

```
GitHub → Settings → Developer settings → Personal access tokens → Fine-grained tokens
→ Generate new token
→ Token name: "NuGet Package Access"
→ Permissions: read:packages
→ Organization access: Select M0nado
→ Generate token
→ Copy the token value
```

### Step 2: Configure NuGet Source

#### Via Command Line

```powershell
# Add GitHub Packages source
dotnet nuget add source `
  --username <your-github-username> `
  --password <your-github-token> `
  --store-password-in-clear-text `
  --name github `
  https://nuget.pkg.github.com/M0nado/index.json
```

#### Via nuget.config File

Create or edit `nuget.config` in your project root:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
    <add key="github" value="https://nuget.pkg.github.com/M0nado/index.json" />
  </packageSources>
  <packageSourceCredentials>
    <github>
      <add key="Username" value="<your-github-username>" />
      <add key="ClearTextPassword" value="<your-github-token>" />
    </github>
  </packageSourceCredentials>
</configuration>
```

### Step 3: Install from GitHub Packages

```powershell
# Install package
dotnet add package HELIOS.Platform --source github

# Or restore if already in csproj
dotnet restore
```

### Step 4: Verify Installation

```powershell
# List sources
dotnet nuget list source

# Search for package
dotnet package search HELIOS.Platform --source github

# List installed packages
dotnet list package
```

### Troubleshooting GitHub Packages

**Issue: Authentication failed**
```
Solution: Verify token is valid and has read:packages permission
```

**Issue: Package not found**
```
Solution: Check username is correct (case-sensitive)
Correct: https://nuget.pkg.github.com/M0nado/index.json
```

**Issue: Credentials not working**
```
Solution: Clear credential cache
dotnet nuget remove source github
# Re-add with correct token
```

---

## C) USAGE EXAMPLES

### Example 1: Basic Usage

```csharp
using HELIOS.Platform;
using Microsoft.Extensions.Logging;

class Program
{
    static async Task Main()
    {
        // Create deployment instance
        var deployment = new HeliosDeployment();
        
        // Validate all components
        bool isValid = await deployment.ValidateAsync();
        
        if (!isValid)
        {
            Console.WriteLine("Validation failed");
            return;
        }
        
        // Deploy with Professional tier
        var result = await deployment.DeployAsync(DeploymentTier.Professional);
        
        if (result.Success)
        {
            Console.WriteLine("✓ Deployment completed successfully");
            Console.WriteLine($"Duration: {result.Duration.TotalSeconds:F1} seconds");
        }
        else
        {
            Console.WriteLine("✗ Deployment failed");
            foreach (var error in result.Errors)
            {
                Console.WriteLine($"  - {error}");
            }
        }
    }
}
```

### Example 2: Custom Configuration

```csharp
using HELIOS.Platform;

// Create custom phase configuration
var config = new PhaseConfig
{
    Tier = DeploymentTier.Enterprise,
    Phase = 2,  // Start at phase 2
    Components = new[] { "SecuritySystem", "AIOrchestrator" },
    Variables = new Dictionary<string, string>
    {
        { "Environment", "Production" },
        { "Region", "US-East-1" }
    },
    Timeout = TimeSpan.FromMinutes(30),
    ContinueOnError = false
};

// Deploy with configuration
var deployment = new HeliosDeployment();
var result = await deployment.DeployAsync(config);

// Monitor result
Console.WriteLine($"Phase: {result.Status.CurrentPhase}");
Console.WriteLine($"Progress: {result.Status.ProgressPercentage:F0}%");
```

### Example 3: Check Deployment Status

```csharp
using HELIOS.Platform;

var deployment = new HeliosDeployment();

// Get current status
var status = await deployment.GetStatusAsync();

Console.WriteLine($"Current Tier: {status.Tier}");
Console.WriteLine($"Current Phase: {status.CurrentPhase}");
Console.WriteLine($"Progress: {status.ProgressPercentage:F0}%");
Console.WriteLine($"State: {status.State}");

// Check individual components
foreach (var componentStatus in status.ComponentStatuses)
{
    var state = componentStatus.IsHealthy ? "✓ Healthy" : "✗ Unhealthy";
    Console.WriteLine($"{componentStatus.ComponentName}: {state}");
}

// Check for errors
if (status.Errors.Length > 0)
{
    Console.WriteLine("\nErrors:");
    foreach (var error in status.Errors)
    {
        Console.WriteLine($"  - {error}");
    }
}
```

### Example 4: Component Access

```csharp
using HELIOS.Platform;

var deployment = new HeliosDeployment();

// Access individual components
var monadoEngine = deployment.MonadoEngine;
var security = deployment.SecuritySystem;
var aiOrchestrator = deployment.AIOrchestrator;
var dashboard = deployment.GUIDashboard;
var buildAgents = deployment.BuildAgents;
var devHub = deployment.DevAIHub;
var stack = deployment.SoftwareStack;

// Check component health
if (monadoEngine.IsHealthy)
{
    var metrics = monadoEngine.GetMetrics();
    Console.WriteLine($"CPU Optimization: {metrics.CPUUsage}%");
    Console.WriteLine($"Memory Usage: {metrics.MemoryUsage}%");
}

// Check security status
if (security.IsCompliant)
{
    Console.WriteLine("✓ System is security compliant");
}
else
{
    Console.WriteLine("⚠ Security policy violations detected");
}
```

### Example 5: Logging Integration

```csharp
using HELIOS.Platform;
using Microsoft.Extensions.Logging;

// Setup logging
var logFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Debug);
});

var logger = logFactory.CreateLogger<Program>();

var deployment = new HeliosDeployment();

try
{
    logger.LogInformation("Starting HELIOS deployment...");
    
    var result = await deployment.DeployAsync(DeploymentTier.Ultimate);
    
    if (result.Success)
    {
        logger.LogInformation("Deployment succeeded in {Duration}ms", 
            result.Duration.TotalMilliseconds);
    }
    else
    {
        logger.LogError("Deployment failed with {ErrorCount} errors", 
            result.Errors.Length);
    }
}
catch (Exception ex)
{
    logger.LogCritical(ex, "Deployment exception occurred");
}
```

### Example 6: Deployment Rollback

```csharp
using HELIOS.Platform;

var deployment = new HeliosDeployment();

// Start deployment
var result = await deployment.DeployAsync(DeploymentTier.Ultimate);

if (!result.Success)
{
    Console.WriteLine("Deployment failed, rolling back...");
    
    // Rollback to previous phase
    bool rollbackSuccess = await deployment.RollbackAsync(3);
    
    if (rollbackSuccess)
    {
        Console.WriteLine("✓ Successfully rolled back to phase 3");
    }
    else
    {
        Console.WriteLine("✗ Rollback failed");
    }
}
```

### Example 7: Complete Enterprise Setup

```csharp
using HELIOS.Platform;
using Microsoft.Extensions.Logging;

class EnterpriseSetup
{
    static async Task Main()
    {
        // Setup logging
        var logFactory = LoggerFactory.Create(builder =>
            builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        
        var logger = logFactory.CreateLogger<EnterpriseSetup>();
        
        logger.LogInformation("=== HELIOS Enterprise Deployment ===\n");
        
        var deployment = new HeliosDeployment();
        
        // Step 1: Validate
        logger.LogInformation("Step 1: Validating configuration...");
        if (!await deployment.ValidateAsync())
        {
            logger.LogError("Validation failed");
            return;
        }
        logger.LogInformation("✓ Validation passed\n");
        
        // Step 2: Deploy
        logger.LogInformation("Step 2: Deploying Enterprise tier...");
        var result = await deployment.DeployAsync(DeploymentTier.Enterprise);
        
        if (!result.Success)
        {
            logger.LogError("Deployment failed: {Errors}", 
                string.Join(", ", result.Errors));
            return;
        }
        logger.LogInformation("✓ Deployment completed in {Duration:F1}s\n", 
            result.Duration.TotalSeconds);
        
        // Step 3: Verify
        logger.LogInformation("Step 3: Verifying components...");
        var status = await deployment.GetStatusAsync();
        
        var allHealthy = status.ComponentStatuses.All(c => c.IsHealthy);
        if (allHealthy)
        {
            logger.LogInformation("✓ All components are healthy");
        }
        else
        {
            logger.LogWarning("⚠ Some components need attention");
        }
        
        // Step 4: Summary
        logger.LogInformation("\n=== Deployment Summary ===");
        logger.LogInformation("Tier: {Tier}", status.Tier);
        logger.LogInformation("Phase: {Phase} of 7", status.CurrentPhase);
        logger.LogInformation("Progress: {Progress:F0}%", status.ProgressPercentage);
        
        logger.LogInformation("\n=== Component Status ===");
        foreach (var component in status.ComponentStatuses)
        {
            var icon = component.IsHealthy ? "✓" : "✗";
            logger.LogInformation("{Icon} {Name}: {Version}", 
                icon, component.ComponentName, component.Version);
        }
    }
}
```

---

## D) DEPENDENCY MANAGEMENT

### What Gets Installed

When you install HELIOS.Platform, the following packages are automatically installed:

```
HELIOS.Platform
├── Azure.Identity (1.10.0)
│   ├── Azure.Core (1.35.0)
│   ├── System.Collections.Immutable
│   ├── System.Memory.Data
│   ├── System.Security.Cryptography.Cng
│   └── System.Text.Json
├── Azure.ResourceManager.Storage (1.6.0)
│   ├── Azure.Identity (1.10.0) ✓ Same version
│   ├── Azure.ResourceManager (1.8.0)
│   └── Azure.Core (1.35.0)
├── Microsoft.Extensions.Logging (8.0.0)
│   └── Microsoft.Extensions.Abstractions (8.0.0)
└── System.Diagnostics.EventLog (4.7.0)
    └── System.Runtime

Total: ~15 NuGet packages
Total Size: ~3-5 MB (depending on .NET version)
```

### Checking Installed Versions

```powershell
# List all packages with versions
dotnet list package

# List packages including transitive dependencies
dotnet list package --include-transitive

# Show detailed package information
dotnet package search HELIOS.Platform --exact

# Check for obsolete packages
dotnet list package --outdated
```

### Handling Version Conflicts

**Scenario 1: Another package requires different Azure.Identity version**

```
Conflict: 
- HELIOS.Platform requires Azure.Identity >= 1.10.0
- OtherPackage requires Azure.Identity >= 1.8.0

Resolution: NuGet automatically uses 1.10.0 (highest)
```

**Resolution Steps:**
```powershell
# Option 1: Let NuGet resolve (automatic)
dotnet restore

# Option 2: Specify exact version
dotnet add package Azure.Identity --version 1.10.0

# Option 3: Check compatibility
dotnet list package --include-transitive | grep Azure.Identity
```

**Scenario 2: Conflicting .NET Framework requirements**

```
Error: Package requires .NET 8.0+
Your project: Targets .NET 6.0

Resolution: Either upgrade project or use compatible version
```

**Steps:**
```powershell
# Check target framework
dotnet --info

# Update project to .NET 8.0
# Edit .csproj:
# <TargetFramework>net8.0</TargetFramework>

# Or use compatible package version
# Check: https://www.nuget.org/packages/HELIOS.Platform/
```

### Updating Dependencies

```powershell
# Update all packages to latest
dotnet list package --outdated
dotnet add package Azure.Identity --version 1.11.0  # if newer available
dotnet add package Azure.ResourceManager.Storage --version 1.7.0

# Or use floating versions (auto-updates within range)
# Edit .csproj:
# <PackageReference Include="Azure.Identity" Version="1.10.*" />

# Then restore
dotnet restore

# Check updates
dotnet list package --outdated
```

### Breaking Changes in Dependencies

**Azure.Identity 1.x → 2.x (hypothetical breaking change)**

```
Breaking Change: Changed authentication method signatures

Update required:
1. Read breaking changes: 
   https://github.com/Azure/azure-sdk-for-net/blob/main/CHANGELOG.md

2. Update code to use new API

3. Test thoroughly

4. Update version in csproj:
   <PackageReference Include="Azure.Identity" Version="2.0.0" />

5. Run tests:
   dotnet test
```

### Safe Update Process

```powershell
# 1. Check current versions
dotnet list package

# 2. Create backup branch
git checkout -b feature/dependency-updates

# 3. Check for outdated packages
dotnet list package --outdated

# 4. Read changelog of each update
# Example: https://github.com/Azure/azure-sdk-for-net/releases

# 5. Update one package at a time
dotnet add package Azure.Identity --version 1.10.1

# 6. Build and test
dotnet build
dotnet test

# 7. Commit
git add -A
git commit -m "Update Azure.Identity to 1.10.1

- Security fixes
- Bug improvements"

# 8. Repeat for other packages

# 9. Create pull request for review
git push origin feature/dependency-updates
# Create PR on GitHub
```

---

## E) TROUBLESHOOTING

### Common Issues and Solutions

#### Issue 1: Package Not Found

**Error:**
```
error NU1101: Unable to find package HELIOS.Platform
```

**Solutions:**
```powershell
# 1. Verify package is published
curl https://api.nuget.org/v3-flatcontainer/helios.platform/index.json

# 2. Check NuGet source is configured
dotnet nuget list source

# 3. Clear cache and restore
dotnet nuget locals cache --clear
dotnet restore

# 4. Explicitly specify version
dotnet add package HELIOS.Platform --version 1.0.0

# 5. Check package is public
# Visit: https://www.nuget.org/packages/HELIOS.Platform/
```

#### Issue 2: Version Conflict

**Error:**
```
error NU1107: Version conflict detected for 'Azure.Identity'
- Project requires Azure.Identity >= 1.11.0
- But package HELIOS.Platform requires Azure.Identity >= 1.10.0
```

**Solutions:**
```powershell
# 1. Update HELIOS.Platform to newer version
dotnet add package HELIOS.Platform --version 1.1.0

# 2. Or downgrade other package
dotnet add package OtherPackage --version 1.0.0

# 3. Check compatibility
# https://www.nuget.org/packages/HELIOS.Platform/1.0.0/

# 4. Use floating version if compatible
# Edit .csproj to allow range:
# <PackageReference Include="Azure.Identity" Version="[1.10.0,2.0.0)" />
```

#### Issue 3: Authentication Failed (GitHub Packages)

**Error:**
```
error NU1403: The service index URL 'https://nuget.pkg.github.com/M0nado/index.json'
does not have a valid HTTP response code. Response Code: Unauthorized (401).
```

**Solutions:**
```powershell
# 1. Verify token is valid
# GitHub → Settings → Personal access tokens → Verify token

# 2. Check token has correct permissions
# Token should have: read:packages scope

# 3. Re-authenticate
dotnet nuget remove source github
dotnet nuget add source `
  --username <username> `
  --password <new-token> `
  --store-password-in-clear-text `
  --name github `
  https://nuget.pkg.github.com/M0nado/index.json

# 4. Test connectivity
curl -u <username>:<token> https://nuget.pkg.github.com/M0nado/index.json
```

#### Issue 4: Framework Incompatibility

**Error:**
```
error NU1202: Package HELIOS.Platform is not compatible with 
net5.0 (.NETFramework 5.0). Package supports: net6.0 / net7.0 / net8.0
```

**Solutions:**
```powershell
# 1. Upgrade project target framework
# Edit .csproj:
# <TargetFramework>net8.0</TargetFramework>
# Or <TargetFrameworks>net6.0;net7.0;net8.0</TargetFrameworks>

# 2. Update .NET SDK
dotnet --version
# If older than 6.0, update from https://dotnet.microsoft.com/download

# 3. Downgrade HELIOS.Platform if needed
# Check available versions:
dotnet package search HELIOS.Platform
```

#### Issue 5: Restore Timeout

**Error:**
```
The server did not respond within the configured timeout period.
```

**Solutions:**
```powershell
# 1. Increase timeout (in minutes)
dotnet restore --source https://api.nuget.org/v3/index.json `
  --packages ./packages `
  --no-cache

# 2. Use direct package file
# Download: https://www.nuget.org/api/v2/package/HELIOS.Platform/1.0.0
# Place in local folder
dotnet add source --name local-packages ./local-packages

# 3. Check internet connection
Test-NetConnection api.nuget.org -Port 443

# 4. Clear cache
dotnet nuget locals cache --clear
dotnet nuget locals temp --clear
```

---

## F) SUPPORT & DOCUMENTATION

### Getting Help

**Official Resources:**
- GitHub Repository: https://github.com/M0nado/helios-platform
- Issue Tracker: https://github.com/M0nado/helios-platform/issues
- Discussions: https://github.com/M0nado/helios-platform/discussions
- Package Page: https://www.nuget.org/packages/HELIOS.Platform/

**Community Support:**
- Stack Overflow: Tag `helios-platform`
- GitHub Discussions: https://github.com/M0nado/helios-platform/discussions

### Reporting Issues

```markdown
# Title: [Issue Type] Brief Description

## Description
Clear description of the problem

## Steps to Reproduce
1. Install HELIOS.Platform
2. Call method X with parameter Y
3. Observe error

## Expected Behavior
Should work correctly

## Actual Behavior
Error occurs with message: [exact error]

## Environment
- OS: Windows 11 / Windows Server 2022
- .NET Version: 8.0.100
- HELIOS.Platform Version: 1.0.0
- Other packages: [list]

## Code Sample
[Minimal reproducible code]

## Screenshots
[If applicable]
```

---

**Last Updated:** April 13, 2024
**Package:** HELIOS.Platform 1.0.0
**Repository:** https://github.com/M0nado/helios-platform
