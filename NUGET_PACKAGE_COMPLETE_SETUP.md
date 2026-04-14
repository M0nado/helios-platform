# HELIOS Platform - Complete NuGet Package Setup

## A) PACKAGE OVERVIEW

### Basic Information
- **Package Name:** HELIOS.Platform
- **Current Version:** 1.0.0
- **License:** MIT
- **Repository:** https://github.com/M0nado/helios-platform
- **NuGet.org:** https://www.nuget.org/packages/HELIOS.Platform/
- **Status:** Ready for publishing

### Package Description
Complete Windows optimization ecosystem with 7 integrated components providing enterprise-grade deployment, automation, and management capabilities across .NET platforms.

### Target Frameworks
- .NET 8.0 (Latest)
- .NET 7.0 (LTS)
- .NET 6.0 (Previous LTS)

### Package Visibility
- Public: Yes
- Pre-release: No (1.0.0 is stable)
- Ownership: M0nado GitHub Organization

---

## B) PACKAGE CONTENTS

### Core Assembly
- **Main DLL:** HELIOS.Platform.dll
- **Entry Point:** HeliosDeployment class
- **Namespace:** HELIOS.Platform
- **Public API:** ~150+ public methods/properties

### Dependencies
```
Azure.Identity (>= 1.10.0)
├─ Required for: Azure authentication
├─ Security: Updated regularly
└─ Status: Critical

Azure.ResourceManager.Storage (>= 1.6.0)
├─ Required for: Cloud storage management
├─ Used by: AIOrchestrator component
└─ Status: Production-ready

Microsoft.Extensions.Logging (>= 8.0.0)
├─ Required for: Logging infrastructure
├─ Used by: All 7 components
└─ Status: Microsoft standard library

System.Diagnostics.EventLog (>= 4.7.0)
├─ Required for: Windows Event Log integration
├─ Used by: SecuritySystem component
└─ Status: System library
```

### Documentation Files
- README.md (package readme)
- LICENSE.md (MIT license)
- CHANGELOG.md (version history)
- API_REFERENCE.md (detailed API docs)
- DEPLOYMENT_GUIDE.md (setup instructions)

### Package Files Included
```
HELIOS.Platform.1.0.0.nupkg
├── HELIOS.Platform.dll
├── HELIOS.Platform.pdb (symbols)
├── README.md
├── LICENSE.md
├── CHANGELOG.md
├── .nuspec (package manifest)
└── [lib/net*/dependencies]
```

---

## C) PROJECT FILE SETUP (.csproj)

### Location
`src/HELIOS.Platform/HELIOS.Platform.csproj`

### Complete Project Configuration

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <!-- Framework Configuration -->
  <PropertyGroup>
    <TargetFrameworks>net8.0;net7.0;net6.0</TargetFrameworks>
    <RootNamespace>HELIOS.Platform</RootNamespace>
    <AssemblyName>HELIOS.Platform</AssemblyName>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <!-- Package Information -->
  <PropertyGroup>
    <PackageId>HELIOS.Platform</PackageId>
    <Version>1.0.0</Version>
    <Title>HELIOS Platform - Complete Windows Ecosystem</Title>
    <Authors>HELIOS Team</Authors>
    <Company>M0nado</Company>
    <Description>Complete Windows optimization ecosystem with 7 integrated components providing enterprise-grade deployment, automation, and management capabilities. Includes MonadoEngine, SecuritySystem, AIOrchestrator, GUIDashboard, BuildAgents, DevAIHub, and SoftwareStack.</Description>
    <PackageReadmeFile>README.md</PackageReadmeFile>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
    <PackageProjectUrl>https://github.com/M0nado/helios-platform</PackageProjectUrl>
    <RepositoryUrl>https://github.com/M0nado/helios-platform</RepositoryUrl>
    <RepositoryType>git</RepositoryType>
    <PackageTags>windows;optimization;automation;deployment;cloud;security;ecosystem</PackageTags>
    <RepositoryBranch>main</RepositoryBranch>
  </PropertyGroup>

  <!-- License & Documentation -->
  <ItemGroup>
    <None Include="../../README.md" Pack="true" PackagePath="\"/>
    <None Include="../../LICENSE.md" Pack="true" PackagePath="\"/>
    <None Include="../../CHANGELOG.md" Pack="true" PackagePath="\docs\"/>
  </ItemGroup>

  <!-- NuGet Dependencies -->
  <ItemGroup>
    <PackageReference Include="Azure.Identity" Version="1.10.0" />
    <PackageReference Include="Azure.ResourceManager.Storage" Version="1.6.0" />
    <PackageReference Include="Microsoft.Extensions.Logging" Version="8.0.0" />
    <PackageReference Include="System.Diagnostics.EventLog" Version="4.7.0" />
  </ItemGroup>

</Project>
```

### Build Configuration
- **Configuration:** Release
- **Platform Target:** Any CPU
- **Output Type:** Library (.dll)
- **Warnings as Errors:** Enabled
- **Documentation:** Generated
- **Optimization:** Enabled in Release mode

### Strong Name Signing (Optional)
```xml
<!-- Uncomment to enable strong name signing -->
<!-- 
<PropertyGroup>
  <SignAssembly>true</SignAssembly>
  <AssemblyOriginatorKeyFile>helios.snk</AssemblyOriginatorKeyFile>
</PropertyGroup>
-->
```

---

## D) CORE CLASSES & STRUCTURE

### Main Entry Point: HeliosDeployment.cs

**Location:** `src/HELIOS.Platform/HeliosDeployment.cs`

**Key Methods:**
```csharp
// Core orchestration methods
public async Task<bool> ValidateAsync()
  └─ Validates all 7 components and dependencies

public async Task<DeploymentResult> DeployAsync(DeploymentTier tier)
  └─ Executes deployment with specified tier
  └─ Returns: DeploymentResult with status and phase info

public async Task<DeploymentResult> DeployAsync(PhaseConfig config)
  └─ Deploys specific phases/components
  └─ Advanced control for fine-grained deployment

public async Task<DeploymentStatus> GetStatusAsync()
  └─ Returns current deployment status
  └─ Includes phase progress, component states

public async Task<bool> RollbackAsync(int toPhase)
  └─ Rolls back to previous deployment phase
  └─ Safe, reversible deployment

public async Task UndeployAsync()
  └─ Complete cleanup and undeployment
  └─ Removes all components
```

**Key Properties:**
```csharp
public MonadoEngine MonadoEngine { get; }
public SecuritySystem SecuritySystem { get; }
public AIOrchestrator AIOrchestrator { get; }
public GUIDashboard GUIDashboard { get; }
public BuildAgents BuildAgents { get; }
public DevAIHub DevAIHub { get; }
public SoftwareStack SoftwareStack { get; }

public DeploymentStatus CurrentStatus { get; }
public DeploymentTier CurrentTier { get; }
public int CurrentPhase { get; }
```

### Component Classes (7 Total)

#### 1. MonadoEngine
**Purpose:** Core optimization and performance engine
```csharp
namespace HELIOS.Platform.Components
{
  public class MonadoEngine
  {
    public async Task InitializeAsync();
    public async Task OptimizeAsync();
    public async Task MonitorPerformanceAsync();
    public PerformanceMetrics GetMetrics();
    public bool IsHealthy { get; }
  }
}
```

#### 2. SecuritySystem
**Purpose:** Comprehensive Windows security management
```csharp
namespace HELIOS.Platform.Components
{
  public class SecuritySystem
  {
    public async Task InitializeAsync();
    public async Task AnalyzeThreatLandscapeAsync();
    public async Task ApplySecurityPoliciesAsync();
    public SecurityStatus GetSecurityStatus();
    public IEnumerable<SecurityEvent> GetSecurityEvents();
    public bool IsCompliant { get; }
  }
}
```

#### 3. AIOrchestrator
**Purpose:** AI-driven automation and orchestration
```csharp
namespace HELIOS.Platform.Components
{
  public class AIOrchestrator
  {
    public async Task InitializeAsync();
    public async Task OrchestrationAsync(DeploymentTier tier);
    public async Task<string> QueryAsync(string query);
    public AIModelStatus GetModelStatus();
    public bool IsModelReady { get; }
  }
}
```

#### 4. GUIDashboard
**Purpose:** Dashboard and monitoring interface
```csharp
namespace HELIOS.Platform.Components
{
  public class GUIDashboard
  {
    public async Task InitializeAsync();
    public async Task RenderDashboardAsync();
    public void UpdateMetrics(DeploymentMetrics metrics);
    public DashboardStatus GetStatus();
    public bool IsHealthy { get; }
  }
}
```

#### 5. BuildAgents
**Purpose:** Automated CI/CD and build management
```csharp
namespace HELIOS.Platform.Components
{
  public class BuildAgents
  {
    public async Task InitializeAsync();
    public async Task DeployAgentsAsync();
    public async Task<BuildResult> ExecuteBuildAsync(string buildConfig);
    public BuildStatus GetStatus();
    public IEnumerable<BuildAgent> GetAgents();
    public bool IsHealthy { get; }
  }
}
```

#### 6. DevAIHub
**Purpose:** Developer AI assistance and collaboration
```csharp
namespace HELIOS.Platform.Components
{
  public class DevAIHub
  {
    public async Task InitializeAsync();
    public async Task<string> GetRecommendationAsync(string context);
    public async Task<CodeAnalysisResult> AnalyzeCodeAsync(string code);
    public HubStatus GetStatus();
    public bool IsHealthy { get; }
  }
}
```

#### 7. SoftwareStack
**Purpose:** Integrated software and framework management
```csharp
namespace HELIOS.Platform.Components
{
  public class SoftwareStack
  {
    public async Task InitializeAsync();
    public async Task InstallComponentsAsync();
    public async Task UpdateComponentsAsync();
    public StackStatus GetStatus();
    public IEnumerable<InstalledComponent> GetComponents();
    public bool IsHealthy { get; }
  }
}
```

### Deployment Models

#### DeploymentTier Enum
```csharp
public enum DeploymentTier
{
    Professional = 1,   // Components: Monado, Security, Dashboard
    Enterprise = 2,     // Professional + AI, BuildAgents, DevAIHub
    Ultimate = 3        // Enterprise + SoftwareStack + advanced features
}
```

#### PhaseConfig
```csharp
public class PhaseConfig
{
    public int Phase { get; set; }                    // 0-7
    public DeploymentTier Tier { get; set; }
    public string[] Components { get; set; }          // Component names
    public Dictionary<string, string> Variables { get; set; }
    public TimeSpan Timeout { get; set; }
    public bool ContinueOnError { get; set; }
}
```

#### ComponentConfig
```csharp
public class ComponentConfig
{
    public string Name { get; set; }
    public bool Enabled { get; set; }
    public string Version { get; set; }
    public Dictionary<string, object> Settings { get; set; }
    public ComponentDependency[] Dependencies { get; set; }
}
```

#### DeploymentStatus
```csharp
public class DeploymentStatus
{
    public int CurrentPhase { get; set; }
    public DeploymentTier Tier { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? CompletionTime { get; set; }
    public double ProgressPercentage { get; set; }
    public DeploymentState State { get; set; }
    public ComponentStatus[] ComponentStatuses { get; set; }
    public string[] Errors { get; set; }
    public string[] Warnings { get; set; }
}
```

#### DeploymentResult
```csharp
public class DeploymentResult
{
    public bool Success { get; set; }
    public DeploymentStatus Status { get; set; }
    public TimeSpan Duration { get; set; }
    public string[] CreatedResources { get; set; }
    public string[] Errors { get; set; }
}
```

### Deployment Phases (0-7)

| Phase | Name | Components | Purpose |
|-------|------|-----------|---------|
| 0 | Validation | All | Pre-deployment checks |
| 1 | Foundation | Monado | Core engine initialization |
| 2 | Security | Security | Windows security hardening |
| 3 | Dashboard | GUI | Monitoring setup |
| 4 | Build | BuildAgents | CI/CD pipeline creation |
| 5 | Intelligence | AI + DevHub | AI orchestration |
| 6 | Stack | SoftwareStack | Framework integration |
| 7 | Finalization | All | Verification and optimization |

---

## E) BUILD CONFIGURATION

### Release Build Settings

```xml
<PropertyGroup Condition="'$(Configuration)|$(Platform)'=='Release|AnyCPU'">
  <DebugType>embedded</DebugType>
  <DebugSymbols>true</DebugSymbols>
  <Optimize>true</Optimize>
  <WarningLevel>4</WarningLevel>
  <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
  <DocumentationFile>bin\Release\HELIOS.Platform.xml</DocumentationFile>
  <Deterministic>true</Deterministic>
</PropertyGroup>
```

### Debug Build Settings

```xml
<PropertyGroup Condition="'$(Configuration)|$(Platform)'=='Debug|AnyCPU'">
  <DebugType>full</DebugType>
  <DebugSymbols>true</DebugSymbols>
  <Optimize>false</Optimize>
  <DefineConstants>DEBUG;TRACE</DefineConstants>
  <WarningLevel>4</WarningLevel>
</PropertyGroup>
```

### Target Platforms
- **Primary:** Any CPU (64-bit optimization)
- **Alternate:** x64 for performance-critical scenarios
- **ARM64:** Supported for future Windows on ARM

### Versioning Strategy

**Current:** 1.0.0 (Semantic Versioning)

**Format:** MAJOR.MINOR.PATCH[-PRERELEASE][+BUILD]

**Rules:**
- MAJOR (1.x.x): Breaking API changes, major feature additions
- MINOR (x.1.x): New features, backward compatible
- PATCH (x.x.1): Bug fixes, security patches
- Pre-release: alpha.1, beta.2, rc.1 (for testing)
- Build metadata: +build.20240413

**Examples:**
- 1.0.0 → 1.0.1 (patch fix)
- 1.0.0 → 1.1.0 (new feature)
- 1.0.0 → 2.0.0 (breaking change)
- 1.0.0-alpha.1 (alpha testing)
- 1.0.0-beta.1+build.202404 (beta with build info)

---

## F) DEPENDENCIES MANAGEMENT

### Dependency Matrix

| Package | Version | Size | Purpose | Security Level |
|---------|---------|------|---------|-----------------|
| Azure.Identity | 1.10.0 | 250 KB | Azure authentication | High |
| Azure.ResourceManager.Storage | 1.6.0 | 450 KB | Cloud storage API | High |
| Microsoft.Extensions.Logging | 8.0.0 | 180 KB | Logging framework | Medium |
| System.Diagnostics.EventLog | 4.7.0 | 100 KB | Event log integration | Medium |

### Dependency Details

#### 1. Azure.Identity (>= 1.10.0)
- **Purpose:** Secure authentication to Azure services
- **Used By:** AIOrchestrator (cloud model access), SecuritySystem (policy sync)
- **Size Impact:** ~250 KB
- **Security:** Regular security updates from Microsoft
- **Update Policy:** Update monthly to latest stable
- **Breaking Changes:** Documented in Azure SDK changelog

#### 2. Azure.ResourceManager.Storage (>= 1.6.0)
- **Purpose:** Manage Azure Storage resources
- **Used By:** AIOrchestrator (model storage), BuildAgents (artifact storage)
- **Size Impact:** ~450 KB
- **Security:** Annual security audits
- **Update Policy:** Update quarterly
- **Compatibility:** Requires Azure.Identity for authentication

#### 3. Microsoft.Extensions.Logging (>= 8.0.0)
- **Purpose:** Standard .NET logging infrastructure
- **Used By:** ALL 7 components for structured logging
- **Size Impact:** ~180 KB
- **Security:** Microsoft-maintained, battle-tested
- **Update Policy:** Update with .NET major versions
- **Integration:** Works with ILogger, Console, EventLog

#### 4. System.Diagnostics.EventLog (>= 4.7.0)
- **Purpose:** Windows Event Log integration
- **Used By:** SecuritySystem (threat logging), MonadoEngine (performance tracking)
- **Size Impact:** ~100 KB
- **Security:** System library, Windows-native
- **Update Policy:** Update with Windows patches
- **Windows-Only:** Requires Windows with Event Log service

### Dependency Tree
```
HELIOS.Platform
├── Azure.Identity (1.10.0+)
│   ├── Azure.Core (1.x)
│   ├── System.Collections.Immutable
│   └── System.Security.Cryptography
├── Azure.ResourceManager.Storage (1.6.0+)
│   ├── Azure.Identity (1.10.0+)
│   ├── Azure.ResourceManager (1.x)
│   └── Azure.Core (1.x)
├── Microsoft.Extensions.Logging (8.0.0+)
│   ├── Microsoft.Extensions.Abstractions (8.x)
│   └── System.Runtime
└── System.Diagnostics.EventLog (4.7.0+)
    └── System.Runtime
```

### Security Considerations

**Dependency Vulnerabilities:**
1. Monitor NuGet.org security advisories
2. Use `dotnet list package --vulnerable` to check
3. Enable Dependabot in GitHub for auto-updates
4. Require security updates within 7 days

**Version Pinning:**
- Minimum versions specified (>= 1.0.0)
- Floating versions for security patches
- No upper bounds to allow automatic updates

**Authentication Security:**
- Azure.Identity handles token caching
- Never store credentials in code
- Use managed identities in Azure
- Local: Use device code or interactive login

### Update Strategy

**Monthly Review:**
```powershell
dotnet outdated
dotnet list package --outdated
```

**Update Process:**
1. Test in development environment
2. Run full test suite
3. Check release notes for breaking changes
4. Update in csproj file
5. Run: `dotnet restore`
6. Verify build succeeds
7. Commit with detailed message

**Breaking Change Handling:**
- Major version updates require code review
- Test all 7 components thoroughly
- Update documentation
- Consider pre-release period

---

## G) NUGET PACKAGE METADATA

### Package Identity
- **ID:** HELIOS.Platform
- **Title:** HELIOS Platform - Complete Windows Ecosystem
- **Version:** 1.0.0 (stable release)
- **Package Type:** Library (compiled assembly)

### Complete Description (500 chars)
```
Complete Windows optimization ecosystem with 7 integrated components 
providing enterprise-grade deployment, automation, and management 
capabilities. Includes MonadoEngine (core optimization), SecuritySystem 
(Windows hardening), AIOrchestrator (intelligent automation), GUIDashboard 
(real-time monitoring), BuildAgents (CI/CD pipeline), DevAIHub (developer 
assistance), and SoftwareStack (framework integration). Supports 3 deployment 
tiers (Professional/Enterprise/Ultimate) and 7 deployment phases for 
flexible, enterprise-grade systems management.
```

### Metadata

| Field | Value |
|-------|-------|
| **Authors** | HELIOS Team |
| **Owners** | M0nado |
| **License** | MIT (SPDX identifier) |
| **Repository** | https://github.com/M0nado/helios-platform |
| **Repository Type** | git |
| **Project URL** | https://github.com/M0nado/helios-platform |
| **Icon URL** | (Optional) |
| **Language** | en-US |
| **Requires License Acceptance** | false |
| **Unlock This Package** | false |

### Tags (Searchable)
```
windows
optimization
automation
deployment
cloud
security
enterprise
ecosystem
management
monitoring
ai
cicd
build
developer-tools
```

### Package Requirements
- **.NET 8.0+** (Tier 1)
- **.NET 7.0+** (Tier 2)
- **.NET 6.0+** (Tier 3)
- **Windows 10/11/Server 2019+**
- **PowerShell 5.1+** (for some components)

### Release Notes Template

```markdown
# HELIOS.Platform 1.0.0 - Initial Release

## Overview
First production release of the complete HELIOS Platform ecosystem.

## Components Included
- ✅ MonadoEngine (Performance optimization)
- ✅ SecuritySystem (Windows hardening)
- ✅ AIOrchestrator (Intelligent automation)
- ✅ GUIDashboard (Real-time monitoring)
- ✅ BuildAgents (CI/CD pipeline)
- ✅ DevAIHub (Developer assistance)
- ✅ SoftwareStack (Framework integration)

## Deployment Tiers
- Professional: Core components (Monado, Security, Dashboard)
- Enterprise: Professional + AI components
- Ultimate: Enterprise + Advanced features + SoftwareStack

## Key Features
- 7-phase deployment process
- 3 deployment tiers for different use cases
- Azure cloud integration
- Windows Event Log integration
- Comprehensive logging via Microsoft.Extensions.Logging

## Breaking Changes
None - this is the first release.

## Known Issues
None reported.

## Support
- Documentation: https://github.com/M0nado/helios-platform/wiki
- Issues: https://github.com/M0nado/helios-platform/issues
- Discussions: https://github.com/M0nado/helios-platform/discussions

## Installation
```powershell
dotnet add package HELIOS.Platform
```
```

---

## H) VERSIONING STRATEGY

### Semantic Versioning (SemVer 2.0.0)

**Format:** `MAJOR.MINOR.PATCH[-PRERELEASE][+BUILD]`

### Current Version
- **1.0.0** - Initial stable release
- **Major:** 1 (API stability)
- **Minor:** 0 (Initial feature set)
- **Patch:** 0 (No bug fixes yet)

### Version Progression Plan

**Release Timeline:**

| Version | Type | Timeline | Focus |
|---------|------|----------|-------|
| 1.0.0 | Stable | Apr 2024 | Initial release, 7 components |
| 1.0.1 | Patch | Apr 2024 | Bug fixes from early feedback |
| 1.1.0 | Minor | Jun 2024 | New features, backward compatible |
| 1.2.0 | Minor | Sep 2024 | Component improvements |
| 2.0.0 | Major | Q1 2025 | Architecture redesign, API changes |

### Breaking Change Policy

**MAJOR Version Changes (e.g., 1.0.0 → 2.0.0):**
- Require explicit version bump
- Documented migration guide required
- 6-month deprecation notice before release
- Pre-release versions (2.0.0-alpha.1, 2.0.0-beta.1)
- Minimum 3 alpha/beta releases before stable

**MINOR Version Changes (e.g., 1.0.0 → 1.1.0):**
- Backward compatible only
- New public APIs allowed
- No removed APIs
- No changed signatures
- Opt-in new features

**PATCH Version Changes (e.g., 1.0.0 → 1.0.1):**
- Bug fixes only
- No new APIs
- No new features
- Critical security fixes
- Performance improvements allowed

### Pre-release Versioning

**Format:** `MAJOR.MINOR.PATCH-PRERELEASE[.N]`

**Types:**
- **alpha.1, alpha.2:** Early development, frequent changes
- **beta.1, beta.2:** Feature complete, stability focus
- **rc.1, rc.2:** Release candidate, final testing

**Example Sequence:**
```
1.0.0-alpha.1 → 1.0.0-alpha.2 → 1.0.0-beta.1 → 1.0.0-rc.1 → 1.0.0
```

### Build Metadata

**Format:** `MAJOR.MINOR.PATCH+BUILD`

**Examples:**
- `1.0.0+build.20240413` - Metadata only
- `1.0.0-rc.1+build.20240410.1234` - RC with build info

**Git Tag Format:**
```
v1.0.0                    # Stable release
v1.0.0-alpha.1            # Alpha release
v1.0.0-beta.1             # Beta release
v1.0.0-rc.1               # Release candidate
```

### Version Management Tools

**Check Current Version:**
```powershell
[xml]$csproj = Get-Content "src/HELIOS.Platform/HELIOS.Platform.csproj"
$csproj.Project.PropertyGroup.Version
```

**Update Version:**
```powershell
# In csproj
<Version>1.0.1</Version>

# Or via CLI
dotnet nuget locals all --clear
# Then update csproj and commit
```

**Tag Release:**
```powershell
git tag v1.0.0 -m "HELIOS.Platform 1.0.0 release"
git push origin v1.0.0
```

---

## Complete Checklist for Package Publishing

- [ ] Version number verified in csproj
- [ ] CHANGELOG.md updated with release notes
- [ ] README.md complete and accurate
- [ ] LICENSE.md present (MIT)
- [ ] All tests passing
- [ ] Code reviewed and approved
- [ ] Dependencies verified and updated
- [ ] Documentation generated
- [ ] Package built locally and verified
- [ ] Symbols (.pdb) included
- [ ] NuGet API key configured
- [ ] GitHub release created
- [ ] Package published to NuGet.org
- [ ] Package published to GitHub Packages
- [ ] Documentation published
- [ ] Release announced

---

## Quick Reference

**Local Build:**
```powershell
dotnet build src/HELIOS.Platform/HELIOS.Platform.csproj -c Release
```

**Create Package:**
```powershell
dotnet pack src/HELIOS.Platform/HELIOS.Platform.csproj -c Release
```

**Publish to NuGet.org:**
```powershell
dotnet nuget push bin/Release/HELIOS.Platform.1.0.0.nupkg `
  --api-key $env:NUGET_API_KEY `
  --source https://api.nuget.org/v3/index.json
```

**Install Package:**
```powershell
dotnet add package HELIOS.Platform
```

---

**Last Updated:** April 13, 2024
**Maintainer:** HELIOS Team
**Repository:** https://github.com/M0nado/helios-platform
