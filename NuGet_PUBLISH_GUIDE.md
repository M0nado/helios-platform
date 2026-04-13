# NuGet Package Publishing Guide - HELIOS Platform

## Overview

This guide provides comprehensive instructions for publishing the HELIOS Platform NuGet package to nuget.org.

---

## Prerequisites

### 1. Development Environment Setup

```bash
# Install .NET 8.0 SDK or later
# Download from: https://dotnet.microsoft.com/en-us/download

# Verify installation
dotnet --version
```

### 2. NuGet Account

- Create account at [nuget.org](https://www.nuget.org)
- Verify email address
- Enable Two-Factor Authentication (recommended)

### 3. API Key

- Navigate to Account Settings → API Keys on nuget.org
- Create new API key with:
  - Name: "HELIOS Platform"
  - Scope: Push new packages and package versions
  - Glob Pattern: `HELIOS.Platform*`
  - Expiration: 1 year (or as per your policy)

---

## Package Structure Verification

### Project File Configuration

**File:** `src/HELIOS.Platform/HELIOS.Platform.csproj`

Required fields present:
```xml
✓ PackageId: HELIOS.Platform
✓ Version: 1.0.0
✓ Authors: M0nado
✓ Description: Complete Windows Optimization Ecosystem...
✓ RepositoryUrl: https://github.com/M0nado/helios-platform
✓ RepositoryType: git
✓ License/PackageLicenseExpression: MIT
✓ ReadmeFile: README.md
✓ PackageTags: helios;platform;automation;optimization;security;ai;windows;deployment
✓ GeneratePackageOnBuild: true
```

### Required Files Included

- ✓ README.md (in repo root)
- ✓ LICENSE (MIT license file)
- ✓ src/HELIOS.Platform/HELIOS.Platform.csproj (with pack items)

### Core Deployment Class

**File:** `src/HELIOS.Platform/core/HeliosDeployment.cs`

Structure verified:
```
✓ Namespace: HELIOS.Platform.Core
✓ Main class: HeliosDeployment
✓ Version property: "1.0.0"
✓ Execute method: async Task<DeploymentResult>
✓ Supporting types:
  - DeploymentTier enum
  - DeploymentResult class
  - PhaseResult class
```

---

## Build and Package Creation

### Step 1: Clean Previous Builds

```bash
cd C:\helios-platform-repo
dotnet clean src/HELIOS.Platform/HELIOS.Platform.csproj -c Release
```

### Step 2: Restore Dependencies

```bash
dotnet restore src/HELIOS.Platform/HELIOS.Platform.csproj
```

**Expected Dependencies:**
- Microsoft.Extensions.DependencyInjection (8.0.0)
- Microsoft.Extensions.Configuration (8.0.0)
- Microsoft.Extensions.Logging (8.0.0)

### Step 3: Build Release Configuration

```bash
dotnet build src/HELIOS.Platform/HELIOS.Platform.csproj -c Release
```

**Verification:**
- No compilation errors
- No warnings (or acceptable warnings)
- Output: `src/HELIOS.Platform/bin/Release/`

### Step 4: Create NuGet Package

```bash
dotnet pack src/HELIOS.Platform/HELIOS.Platform.csproj -c Release
```

**Output Location:**
```
src/HELIOS.Platform/bin/Release/HELIOS.Platform.1.0.0.nupkg
```

### Step 5: Verify Package Contents

```bash
# Inspect package using nuget CLI
nuget list <path-to-nupkg> -Verbosity detailed

# Or extract and examine contents
# NuGet packages are ZIP files
```

**Expected Contents:**
```
HELIOS.Platform.1.0.0.nupkg
├── [Content_Types].xml
├── _rels/
├── package/
│   ├── services/metadata/core-properties/
│   └── HELIOS.Platform.nuspec
├── HELIOS.Platform/
│   └── lib/net8.0/
│       └── HELIOS.Platform.dll
├── README.md
└── LICENSE
```

---

## Package Validation

### Pre-Publication Checklist

```
□ Version number is semantic and incremented correctly
□ README.md displays properly on nuget.org
□ LICENSE file is accessible
□ No local assembly paths in dependencies
□ All dependencies use official NuGet packages
□ No pre-release suffix (unless intentional)
□ Assembly is strong-named (optional but recommended)
□ No deprecated API usage
□ No security vulnerabilities in dependencies
```

### Local Installation Test

```bash
# Create test project
dotnet new console -n TestHelios
cd TestHelios

# Add package reference (local)
dotnet add package HELIOS.Platform --version 1.0.0 --source C:\helios-platform-repo\src\HELIOS.Platform\bin\Release

# Verify compilation
dotnet build
```

---

## Publishing to NuGet.org

### Method 1: Using dotnet CLI (Recommended)

```bash
# Navigate to project directory
cd C:\helios-platform-repo

# Publish to NuGet.org
dotnet nuget push src/HELIOS.Platform/bin/Release/HELIOS.Platform.1.0.0.nupkg \
  --api-key <YOUR_NUGET_API_KEY> \
  --source https://api.nuget.org/v3/index.json

# Expected output:
# Pushing HELIOS.Platform 1.0.0 to 'https://www.nuget.org'...
# [Your package was pushed.]
```

### Method 2: Using NuGet CLI

```bash
# If dotnet is unavailable
nuget push HELIOS.Platform.1.0.0.nupkg <YOUR_NUGET_API_KEY> -Source https://www.nuget.org/api/v2/package

# Expected output:
# Pushing HELIOS.Platform 1.0.0 to the 'NuGet.org' feed (https://www.nuget.org)...
# Your package was pushed.
```

### Verification

After publishing:
1. Visit: https://www.nuget.org/packages/HELIOS.Platform
2. Verify package appears within 5-10 minutes
3. Check package page displays correctly:
   - Icon and metadata
   - README.md renders
   - Dependencies listed
   - Version history
4. Test installation from NuGet:

```bash
# In a new project
dotnet add package HELIOS.Platform --version 1.0.0
```

---

## Versioning Strategy

### Semantic Versioning

HELIOS Platform follows **Semantic Versioning (SemVer)**:

Format: `MAJOR.MINOR.PATCH[-PRERELEASE][+BUILD]`

#### Version Components

| Component | Increment When | Example Progression |
|-----------|---|---|
| MAJOR | Breaking changes to public API | 1.0.0 → 2.0.0 |
| MINOR | New features (backward compatible) | 1.0.0 → 1.1.0 |
| PATCH | Bug fixes only | 1.0.0 → 1.0.1 |
| PRERELEASE | Pre-release versions | 1.0.0-beta.1 |
| BUILD | Build metadata (not for versioning) | 1.0.0+20230115 |

#### Pre-Release Examples

```
1.0.0-alpha        # Alpha release
1.0.0-alpha.1      # Alpha iteration 1
1.0.0-beta         # Beta release
1.0.0-beta.2       # Beta iteration 2
1.0.0-rc.1         # Release candidate 1
```

#### HELIOS Platform Timeline

```
Current:  1.0.0     (Current stable)
Next:     1.1.0     (New features in Phase 4-5)
Future:   1.0.1     (Patch for critical fixes)
          2.0.0     (Major architecture changes)
```

### Update Process

1. **Update Version in csproj:**
   ```xml
   <Version>1.1.0</Version>
   ```

2. **Update Version in HeliosDeployment.cs:**
   ```csharp
   public string Version => "1.1.0";
   ```

3. **Update CHANGELOG.md:**
   ```markdown
   ## [1.1.0] - 2024-01-XX
   
   ### Added
   - New feature X
   - New feature Y
   
   ### Changed
   - Improved performance
   
   ### Fixed
   - Bug #123
   ```

4. **Commit and Tag:**
   ```bash
   git add .
   git commit -m "Release v1.1.0"
   git tag -a v1.1.0 -m "Version 1.1.0 release"
   git push origin main
   git push origin v1.1.0
   ```

5. **Build and Publish:**
   ```bash
   dotnet pack src/HELIOS.Platform/HELIOS.Platform.csproj -c Release
   dotnet nuget push src/HELIOS.Platform/bin/Release/HELIOS.Platform.1.1.0.nupkg \
     --api-key <API_KEY> \
     --source https://api.nuget.org/v3/index.json
   ```

---

## Security Best Practices

### API Key Management

```bash
# Store API key securely (Windows)
$apiKey = Read-Host -AsSecureString "Enter NuGet API Key"
$apiKey | ConvertFrom-SecureString | Out-File "$env:APPDATA\nuget_key.txt"

# Or use environment variable
[Environment]::SetEnvironmentVariable('NUGET_API_KEY', '<KEY>', 'User')
```

### Never Commit Secrets

```
# .gitignore
*.apikey
nuget.config (if contains credentials)
.env
.env.local
```

### Verify Package Integrity

```bash
# Check package signature (if using strong names)
sn -v HELIOS.Platform.dll

# Validate NuGet package
nuget verify -All HELIOS.Platform.1.0.0.nupkg
```

---

## Troubleshooting

### Common Issues

#### 1. "401 Unauthorized" Error

**Cause:** Invalid or expired API key

**Solution:**
```bash
# Verify API key
dotnet nuget update source nuget.org --username __USERNAME__ --password <API_KEY> --store-password-in-clear-text

# Test authentication
dotnet nuget push --help
```

#### 2. "409 Conflict" Error

**Cause:** Version already published

**Solution:**
- Increment version number
- Delete package from nuget.org (if published within 10 minutes)
- Unlist package if necessary

```bash
# Unlist package
dotnet nuget delete HELIOS.Platform 1.0.0 --api-key <KEY> --source https://api.nuget.org/v3/index.json
```

#### 3. "Package validation failed"

**Cause:** Issues with package contents

**Solution:**
```bash
# Verify package structure
# Re-inspect with:
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [System.IO.Compression.ZipFile]::OpenRead("HELIOS.Platform.1.0.0.nupkg")
$zip.Entries | ForEach-Object { $_.FullName }
$zip.Dispose()
```

#### 4. "Metadata issues"

**Common causes:**
- Missing README.md
- Invalid license expression
- Malformed repository URL

**Solution:**
- Verify csproj configuration
- Re-pack and validate
- Use NuGet Package Explorer tool

---

## Continuous Integration/Deployment

### GitHub Actions Setup

Create `.github/workflows/nuget-publish.yml`:

```yaml
name: Publish to NuGet

on:
  push:
    tags:
      - 'v*'

jobs:
  publish:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'
      
      - name: Restore dependencies
        run: dotnet restore
      
      - name: Build
        run: dotnet build -c Release
      
      - name: Pack
        run: dotnet pack src/HELIOS.Platform/HELIOS.Platform.csproj -c Release
      
      - name: Publish to NuGet
        run: dotnet nuget push src/HELIOS.Platform/bin/Release/*.nupkg \
          --api-key ${{ secrets.NUGET_API_KEY }} \
          --source https://api.nuget.org/v3/index.json \
          --skip-duplicate
```

### Setup Repository Secret

1. Go to repository Settings → Secrets and variables → Actions
2. Add new repository secret:
   - Name: `NUGET_API_KEY`
   - Value: Your NuGet.org API key
3. Commit and tag to trigger publishing

---

## Maintenance

### Monitor Package Health

- Check nuget.org dashboard for download statistics
- Monitor GitHub issues for user feedback
- Track dependency updates for security

### Staying Current

```bash
# Check for outdated dependencies
dotnet outdated src/HELIOS.Platform/HELIOS.Platform.csproj

# Update dependencies
dotnet add src/HELIOS.Platform/HELIOS.Platform.csproj package <PackageName> --version <NewVersion>
```

### Updating Existing Package

To release a new version:

1. Update version in csproj
2. Update version in source code
3. Build and test locally
4. Commit changes with version tag
5. Run publish workflow

---

## Resources

- [NuGet.org Publishing Guide](https://learn.microsoft.com/en-us/nuget/nuget-org/publish-a-package)
- [Semantic Versioning](https://semver.org/)
- [NuGet Package Management Best Practices](https://learn.microsoft.com/en-us/nuget/best-practices/create-nuget-packages)
- [MSBuild Package Properties Reference](https://learn.microsoft.com/en-us/dotnet/core/project-sdk/msbuild-props-desktop)

---

## Support

For issues:
1. Check [GitHub Issues](https://github.com/M0nado/helios-platform/issues)
2. Review [Official NuGet Docs](https://learn.microsoft.com/en-us/nuget/)
3. Contact package owner: M0nado

---

**Last Updated:** January 2024
**Package:** HELIOS.Platform 1.0.0
**Status:** Ready for Publication
