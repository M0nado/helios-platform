# HELIOS Platform - NuGet Build Process

## A) LOCAL BUILD SETUP

### Step 1: Prerequisites

Ensure you have the following installed:

```powershell
# Check .NET SDK version
dotnet --version
# Required: 8.0 or later

# Check PowerShell version
$PSVersionTable.PSVersion
# Required: 7.0 or later

# Check Git
git --version
# Required: 2.30+
```

**Installation Links:**
- .NET SDK 8.0: https://dotnet.microsoft.com/download/dotnet/8.0
- PowerShell 7: https://github.com/PowerShell/PowerShell/releases
- Git: https://git-scm.com/download

### Step 2: Clone Repository

```powershell
# Clone the repository
git clone https://github.com/M0nado/helios-platform.git

# Navigate to project
cd helios-platform

# Verify structure
dir -Recurse -Depth 2 -Include "*.csproj", "*.sln"
```

**Expected Structure:**
```
helios-platform/
├── src/
│   └── HELIOS.Platform/
│       ├── HELIOS.Platform.csproj
│       ├── HeliosDeployment.cs
│       └── Components/
├── tests/
│   └── HELIOS.Platform.Tests/
│       └── HELIOS.Platform.Tests.csproj
├── .github/
│   └── workflows/
├── README.md
├── LICENSE.md
├── CHANGELOG.md
└── .gitignore
```

### Step 3: Restore Dependencies

```powershell
# Restore all NuGet packages
dotnet restore

# Verify successful restore
dotnet restore --verify-match-group dotnet --dryrun
```

**What This Does:**
- Downloads all NuGet dependencies
- Resolves dependency tree
- Caches packages locally
- Validates checksums

### Step 4: Build Solution

#### Build for Release (Default)

```powershell
# Build with Release configuration
dotnet build src/HELIOS.Platform/HELIOS.Platform.csproj `
  -c Release `
  -v normal

# Expected output:
# Build succeeded. 0 Warning(s)
# Time Elapsed: XX:XX:XX.XXX
```

#### Build for Debug

```powershell
# Build with Debug configuration
dotnet build src/HELIOS.Platform/HELIOS.Platform.csproj `
  -c Debug `
  -v normal
```

#### Build Specific Framework

```powershell
# Build for .NET 8.0 only
dotnet build src/HELIOS.Platform/HELIOS.Platform.csproj `
  -c Release `
  -f net8.0

# Build for .NET 7.0 only
dotnet build src/HELIOS.Platform/HELIOS.Platform.csproj `
  -c Release `
  -f net7.0

# Build for .NET 6.0 only
dotnet build src/HELIOS.Platform/HELIOS.Platform.csproj `
  -c Release `
  -f net6.0
```

#### Verify Build Output

```powershell
# List all generated files
dir "src/HELIOS.Platform/bin/Release/" -Recurse

# Check specific artifacts
$artifacts = @(
    "net8.0/HELIOS.Platform.dll",
    "net8.0/HELIOS.Platform.pdb",
    "net7.0/HELIOS.Platform.dll",
    "net6.0/HELIOS.Platform.dll"
)

foreach ($artifact in $artifacts) {
    $path = "src/HELIOS.Platform/bin/Release/$artifact"
    if (Test-Path $path) {
        Write-Host "✓ $artifact exists"
    } else {
        Write-Host "✗ $artifact MISSING"
    }
}
```

### Step 5: Run Tests

```powershell
# Run all tests
dotnet test tests/ -c Release -v normal

# Run with detailed output
dotnet test tests/ -c Release --logger "console;verbosity=detailed"

# Run specific test project
dotnet test tests/HELIOS.Platform.Tests/HELIOS.Platform.Tests.csproj

# Run with code coverage
dotnet test tests/ -c Release --collect:"XPlat Code Coverage"
```

**Expected Test Output:**
```
Test Run Successful.
Total tests: 50
     Passed: 50
     Failed: 0
```

**Coverage Targets:**
- Overall: > 80%
- Core components: > 90%
- Orchestration: > 85%

### Step 6: Create NuGet Package

```powershell
# Create release package
dotnet pack src/HELIOS.Platform/HELIOS.Platform.csproj `
  -c Release `
  -o ".\artifacts"

# Verify package created
dir ".\artifacts\*.nupkg"
```

**Output Location:** `artifacts/HELIOS.Platform.1.0.0.nupkg`

**Package Contents:**
```powershell
# List contents of nupkg file
# (It's a ZIP file)
$nupkg = "artifacts/HELIOS.Platform.1.0.0.nupkg"

# Extract to temp and list
$temp = ".\temp_nupkg"
Expand-Archive $nupkg -DestinationPath $temp
dir -Path $temp -Recurse

# Check specifically for:
dir "$temp/lib/"        # Compiled assemblies
dir "$temp/*.md"        # Documentation
Test-Path "$temp/LICENSE"  # License file
```

### Step 7: Verify Package Locally

```powershell
# Test package locally
dotnet nuget locals cache --list

# Clear cache (optional, for fresh test)
dotnet nuget locals cache --clear

# Create test project
mkdir "test-consume"
cd "test-consume"
dotnet new console -n TestHELIOS

# Add local package source
dotnet nuget add source "../artifacts" -n local

# Install package from local source
cd TestHELIOS
dotnet add package HELIOS.Platform --version 1.0.0 --source local

# Verify package installed
dotnet package search HELIOS.Platform

# Verify dependencies resolved
dotnet list package
```

---

## B) GITHUB ACTIONS BUILD WORKFLOW

### Complete Workflow File

**Location:** `.github/workflows/nuget.yml`

```yaml
name: NuGet Build & Release

on:
  push:
    branches:
      - main
    tags:
      - 'v*.*.*'
  pull_request:
    branches:
      - main
  workflow_dispatch:
    inputs:
      publish_nuget:
        description: 'Publish to NuGet.org'
        required: false
        default: 'false'

jobs:
  build:
    runs-on: ${{ matrix.os }}
    strategy:
      matrix:
        os: [ubuntu-latest, windows-latest]
        dotnet-version: ['8.0', '7.0', '6.0']

    steps:
      - name: Checkout code
        uses: actions/checkout@v4
        with:
          fetch-depth: 0  # For version info

      - name: Setup .NET ${{ matrix.dotnet-version }}
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ matrix.dotnet-version }}

      - name: Restore dependencies
        run: dotnet restore

      - name: Build solution
        run: |
          dotnet build src/HELIOS.Platform/HELIOS.Platform.csproj `
            -c Release `
            -f net${{ matrix.dotnet-version }} `
            --no-restore

      - name: Run tests
        run: |
          dotnet test tests/ `
            -c Release `
            --no-build `
            --logger "trx;LogFileName=test-results.trx"

      - name: Upload test results
        if: always()
        uses: actions/upload-artifact@v3
        with:
          name: test-results-${{ matrix.os }}-${{ matrix.dotnet-version }}
          path: '**/test-results.trx'

  package:
    needs: build
    runs-on: windows-latest
    if: always()

    steps:
      - name: Checkout code
        uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0'

      - name: Get version
        id: version
        run: |
          $xml = [xml](Get-Content "src/HELIOS.Platform/HELIOS.Platform.csproj")
          $version = $xml.Project.PropertyGroup.Version
          echo "VERSION=$version" >> $env:GITHUB_OUTPUT
          echo "Package version: $version"

      - name: Restore dependencies
        run: dotnet restore

      - name: Create NuGet package
        run: |
          dotnet pack src/HELIOS.Platform/HELIOS.Platform.csproj `
            -c Release `
            -o artifacts/

      - name: Verify package
        run: |
          Get-ChildItem artifacts/*.nupkg | ForEach-Object {
            Write-Host "Package created: $($_.Name)"
            Write-Host "Size: $(($_.Length / 1MB).ToString('F2')) MB"
          }

      - name: Upload package artifact
        uses: actions/upload-artifact@v3
        with:
          name: nuget-package-${{ steps.version.outputs.VERSION }}
          path: artifacts/*.nupkg
          retention-days: 30

  publish-nuget:
    needs: package
    runs-on: windows-latest
    if: startsWith(github.ref, 'refs/tags/v')

    steps:
      - name: Download package
        uses: actions/download-artifact@v3
        with:
          path: download/

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0'

      - name: Get version from tag
        id: tag_version
        run: |
          $tag = "${{ github.ref }}"
          $version = $tag -replace 'refs/tags/v', ''
          echo "VERSION=$version" >> $env:GITHUB_OUTPUT
          Write-Host "Publishing version: $version"

      - name: Publish to NuGet.org
        run: |
          $nupkgs = Get-ChildItem download -Recurse -Filter "*.nupkg"
          foreach ($nupkg in $nupkgs) {
            Write-Host "Publishing: $($nupkg.FullName)"
            dotnet nuget push "$($nupkg.FullName)" `
              --api-key "${{ secrets.NUGET_API_KEY }}" `
              --source https://api.nuget.org/v3/index.json `
              --skip-duplicate
          }

      - name: Create GitHub Release
        uses: actions/create-release@v1
        env:
          GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
        with:
          tag_name: ${{ github.ref }}
          release_name: Release ${{ steps.tag_version.outputs.VERSION }}
          body: |
            # HELIOS.Platform v${{ steps.tag_version.outputs.VERSION }}
            
            See [CHANGELOG.md](CHANGELOG.md) for details.
          draft: false
          prerelease: ${{ contains(steps.tag_version.outputs.VERSION, 'alpha') || contains(steps.tag_version.outputs.VERSION, 'beta') }}

  publish-github:
    needs: package
    runs-on: windows-latest
    if: always()

    steps:
      - name: Download package
        uses: actions/download-artifact@v3
        with:
          path: download/

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0'

      - name: Configure GitHub package source
        run: |
          dotnet nuget add source `
            --username github-actions `
            --password ${{ secrets.GITHUB_TOKEN }} `
            --store-password-in-clear-text `
            --name github `
            https://nuget.pkg.github.com/M0nado/index.json

      - name: Publish to GitHub Packages
        run: |
          $nupkgs = Get-ChildItem download -Recurse -Filter "*.nupkg"
          foreach ($nupkg in $nupkgs) {
            Write-Host "Publishing to GitHub: $($nupkg.FullName)"
            dotnet nuget push "$($nupkg.FullName)" `
              --source github `
              --skip-duplicate
          }
```

---

## C) CONTINUOUS INTEGRATION SETUP

### GitHub Actions Configuration

#### 1. Enable Actions for Repository
```
Repository → Settings → Actions → General
→ Enable "Allow all actions and reusable workflows"
```

#### 2. Set Required Secrets
```
Repository → Settings → Secrets and variables → Actions
```

**Secrets to Configure:**

| Name | Value | Source |
|------|-------|--------|
| NUGET_API_KEY | [API key] | https://www.nuget.org/account/apikeys |
| GITHUB_TOKEN | Auto-provided | GitHub automatically provides |

#### 3. Branch Protection Rules
```
Repository → Settings → Branches → Add rule
→ Branch name pattern: main
→ Require status checks to pass before merging
→ Require pull request reviews
→ Require code review from code owners
```

### CI Pipeline Flow

```
Push to branch
    ↓
Trigger build workflow
    ├─ Checkout code
    ├─ Setup .NET SDKs
    ├─ Restore packages
    ├─ Build (all frameworks)
    ├─ Run tests
    ├─ Upload test results
    └─ Create package (if success)
        ↓
    [On PR] Stop here
    [On main] Continue...
    ├─ Publish to GitHub Packages
    └─ [If tagged] Publish to NuGet.org
        ↓
    Create GitHub Release
```

### Auto-Build on Events

#### Push to Main
```
Automatically:
- Build all frameworks
- Run all tests
- Create NuGet package
- Publish to GitHub Packages
- Store artifacts for 30 days
```

#### Pull Request
```
Automatically:
- Build all frameworks
- Run all tests
- Report status on PR
- Block merge if tests fail
```

#### Tag Creation (Release)
```
When tag matches v*.*.*:
- Build and test
- Create NuGet package
- Publish to NuGet.org
- Create GitHub Release
- Archive artifacts indefinitely
```

#### Manual Trigger
```
Via workflow_dispatch input:
- Manual publish option
- Custom trigger without code push
```

---

## D) PACKAGE PUBLISHING

### Publishing to NuGet.org

#### Prerequisites
1. Create account on https://www.nuget.org
2. Create API key
3. Store API key in GitHub Secrets

#### API Key Generation

```
NuGet.org → Sign In → Settings → API Keys
→ Create New
→ Name: "GitHub Actions HELIOS"
→ Select Scopes: Push new packages and package versions
→ Glob Pattern: HELIOS.Platform*
→ Expiration: 90 days (recommended)
```

#### Store in GitHub Secrets

```powershell
# Via GitHub CLI
gh secret set NUGET_API_KEY --body "your-api-key-here"

# Or via web UI:
# Repository → Settings → Secrets → New repository secret
# Name: NUGET_API_KEY
# Value: [paste your key]
```

#### Publishing Command

```powershell
# One-time publish
dotnet nuget push bin/Release/HELIOS.Platform.1.0.0.nupkg `
  --api-key $env:NUGET_API_KEY `
  --source https://api.nuget.org/v3/index.json

# With error handling
$nupkg = "bin/Release/HELIOS.Platform.1.0.0.nupkg"
if (Test-Path $nupkg) {
    dotnet nuget push $nupkg `
      --api-key $env:NUGET_API_KEY `
      --source https://api.nuget.org/v3/index.json `
      --skip-duplicate
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ Published to NuGet.org"
    } else {
        Write-Host "✗ Publish failed"
        exit 1
    }
} else {
    Write-Host "✗ Package file not found"
    exit 1
}
```

#### Verify on NuGet.org

```
https://www.nuget.org/packages/HELIOS.Platform/1.0.0/
```

Check:
- [ ] Package listed publicly
- [ ] All frameworks shown (net8.0, net7.0, net6.0)
- [ ] Dependencies listed correctly
- [ ] README displayed
- [ ] License shown as MIT

### Publishing to GitHub Packages

#### Configuration

```xml
<!-- In .csproj or nuget.config -->
<PackageSource>
  <Add Key="github" 
       Value="https://nuget.pkg.github.com/M0nado/index.json" />
</PackageSource>
```

#### Authentication

```powershell
# Using GitHub CLI (recommended)
dotnet nuget add source `
  --username github-actions `
  --password $env:GITHUB_TOKEN `
  --store-password-in-clear-text `
  --name github `
  https://nuget.pkg.github.com/M0nado/index.json

# Using authentication file
# Create/edit ~/.config/NuGet/NuGet.Config
```

#### Publishing Command

```powershell
# To GitHub Packages
dotnet nuget push bin/Release/HELIOS.Platform.1.0.0.nupkg `
  --source github

# Workflow does this automatically
# No additional steps needed in GitHub Actions
```

#### Access Control

```
Repository → Settings → Packages and data
→ Package settings → Manage access
→ Allow organization members to access
```

---

## E) VERSION MANAGEMENT

### Automatic Version Updates

#### Option 1: Manual Versioning (Recommended)

```powershell
# 1. Update version in csproj
[xml]$csproj = Get-Content "src/HELIOS.Platform/HELIOS.Platform.csproj"
$csproj.Project.PropertyGroup.Version = "1.0.1"
$csproj.Save("src/HELIOS.Platform/HELIOS.Platform.csproj")

# 2. Update CHANGELOG.md
$changelog = @"
## [1.0.1] - 2024-04-14

### Fixed
- Bug fix description

## [1.0.0] - 2024-04-13
- Initial release
"@
Set-Content CHANGELOG.md $changelog

# 3. Commit changes
git add -A
git commit -m "Release v1.0.1

- Bug fixes
- Improved error handling

Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>"

# 4. Tag release
git tag -a v1.0.1 -m "HELIOS.Platform v1.0.1 release"

# 5. Push to GitHub
git push origin main
git push origin v1.0.1

# This triggers the workflow to build and publish
```

#### Option 2: Automated Versioning (nbgv)

```powershell
# Install NerdBank GitVersioning
dotnet tool install -g nbgv

# Initialize versioning
nbgv cloud -r https://github.com/M0nado/helios-platform

# Version is computed from git tags
# Tags like v1.0.0 auto-increment to v1.0.1, etc.
```

### Changelog Management

#### Changelog Format (Keep a CHANGELOG)

```markdown
# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com),
and this project adheres to [Semantic Versioning](https://semver.org).

## [Unreleased]

### Added
- New feature description

### Changed
- Change description

### Fixed
- Bug fix description

### Deprecated
- Deprecated feature description

### Removed
- Removed feature description

### Security
- Security fix description

## [1.0.0] - 2024-04-13

### Added
- Initial release with 7 components
- Support for .NET 6.0, 7.0, 8.0
- 3 deployment tiers (Professional, Enterprise, Ultimate)
- 7-phase deployment process

## [0.9.0] - 2024-03-01

### Added
- Beta release for testing
```

#### Update Workflow

```powershell
# Before creating release tag:

# 1. Review changes since last release
git log v1.0.0..HEAD --oneline

# 2. Add unreleased changes to [Unreleased] section
# Edit CHANGELOG.md manually

# 3. Move [Unreleased] to [1.0.1] with date
# Change: ## [Unreleased] → ## [1.0.1] - 2024-04-14

# 4. Add back empty [Unreleased] section

# 5. Commit
git add CHANGELOG.md
git commit -m "Update CHANGELOG for v1.0.1"

# 6. Tag and push
git tag v1.0.1
git push origin main v1.0.1
```

---

## F) BUILD VERIFICATION

### Local Package Testing

```powershell
# Create test directory
$testDir = ".\test-helios-install"
mkdir $testDir
cd $testDir

# Create test project
dotnet new console -n TestApp

# Add local package source
dotnet nuget add source `
  --name local `
  "C:\Users\ADMIN\helios-platform\artifacts"

cd TestApp

# Install package
dotnet add package HELIOS.Platform --version 1.0.0

# Verify in csproj
(Get-Content TestApp.csproj) -match 'HELIOS.Platform' | Write-Host

# Create test code
@'
using HELIOS.Platform;

class Program
{
    static async Task Main()
    {
        var deployment = new HeliosDeployment();
        await deployment.ValidateAsync();
        Console.WriteLine("✓ HELIOS.Platform loaded successfully");
    }
}
'@ | Set-Content Program.cs

# Build test project
dotnet build

# Run test
dotnet run
```

### Dependency Verification

```powershell
# List all dependencies
dotnet list package

# Expected output:
# Top-level Package      Resolved
# Azure.Identity         1.10.0
# Azure.ResourceManager.Storage  1.6.0
# Microsoft.Extensions.Logging   8.0.0
# System.Diagnostics.EventLog    4.7.0

# Check for vulnerabilities
dotnet list package --vulnerable

# Should output: No vulnerabilities detected
```

### Assembly Version Verification

```powershell
# Get assembly info
$dll = "src/HELIOS.Platform/bin/Release/net8.0/HELIOS.Platform.dll"
$assembly = [System.Reflection.Assembly]::LoadFrom($dll)

Write-Host "Assembly Name: $($assembly.GetName().Name)"
Write-Host "Version: $($assembly.GetName().Version)"
Write-Host "Strong Name: $(if ($assembly.GetName().GetPublicKey().Length -gt 0) { 'Yes' } else { 'No' })"

# Verify class exists
$type = $assembly.GetType("HELIOS.Platform.HeliosDeployment")
if ($null -ne $type) {
    Write-Host "✓ HeliosDeployment class found"
    
    # List public methods
    $type.GetMethods([System.Reflection.BindingFlags]::Public -bor [System.Reflection.BindingFlags]::DeclaredOnly) | 
        ForEach-Object { Write-Host "  - $($_.Name)" }
} else {
    Write-Host "✗ HeliosDeployment class NOT found"
}
```

### License Compliance

```powershell
# Verify MIT license in package
$temp = ".\temp_verify"
$nupkg = "artifacts/HELIOS.Platform.1.0.0.nupkg"

Expand-Archive $nupkg -DestinationPath $temp

# Check files
$requiredFiles = @(
    "LICENSE",
    "README.md",
    "CHANGELOG.md"
)

foreach ($file in $requiredFiles) {
    $path = "$temp/$file"
    if (Test-Path $path) {
        Write-Host "✓ $file included"
    } else {
        Write-Host "✗ $file MISSING"
    }
}

# Verify license content
Get-Content "$temp/LICENSE" | Select-Object -First 5
```

### Framework Support Verification

```powershell
# Check all frameworks in package
$temp = ".\temp_verify"
Get-ChildItem "$temp/lib" -Directory | ForEach-Object {
    $framework = $_.Name
    $dlls = Get-ChildItem $_.FullName -Filter "*.dll"
    Write-Host "✓ $framework: $($dlls.Count) assembly files"
}

# Expected:
# ✓ net6.0: 1 assembly files
# ✓ net7.0: 1 assembly files
# ✓ net8.0: 1 assembly files
```

---

## Complete Build & Release Checklist

```powershell
# Run this comprehensive verification

$checks = @{
    "Git repository configured" = { git config --get remote.origin.url -match "M0nado/helios" }
    "All tests passing" = { (dotnet test tests/ -c Release | Select-String "Test Run Successful") -ne $null }
    "Package builds" = { Test-Path "artifacts/HELIOS.Platform.*.nupkg" }
    "Assembly compiles" = { Test-Path "src/HELIOS.Platform/bin/Release/net8.0/HELIOS.Platform.dll" }
    "License file present" = { Test-Path "LICENSE.md" }
    "README present" = { Test-Path "README.md" }
    "CHANGELOG updated" = { (Get-Content "CHANGELOG.md" | Select-String "\[1\.0\.0\]") -ne $null }
    "NuGet API key configured" = { $env:NUGET_API_KEY -ne $null }
    "GitHub token available" = { $env:GITHUB_TOKEN -ne $null }
}

Write-Host "Build & Release Verification`n"
foreach ($check in $checks.GetEnumerator()) {
    $result = & $check.Value
    $status = if ($result) { "✓" } else { "✗" }
    Write-Host "$status $($check.Name)"
}
```

---

**Last Updated:** April 13, 2024
**Repository:** https://github.com/M0nado/helios-platform
**Build Status:** Ready for publication
