# HELIOS Platform Deployment Automation System

Complete deployment automation infrastructure for HELIOS Platform v1.0.0 across all distribution channels.

## Overview

This system automates the entire deployment lifecycle:
- 📦 **Distribution Package Creation** - Prepare all files and artifacts
- ✅ **Verification** - Validate package integrity and completeness
- 🚀 **NuGet Publishing** - Publish to NuGet.org
- 🏷️ **GitHub Releases** - Create releases with artifacts
- 🍫 **Package Managers** - Support for Chocolatey and Winget
- 🔍 **Verification** - Pre and post-deployment checks
- 🔄 **Rollback** - Emergency rollback procedures

## Quick Start

### 1. Prepare Distribution Package

```powershell
cd C:\Users\ADMIN\helios-platform

# Build and prepare distribution files
.\scripts\deployment\prepare-distribution.ps1 `
  -Version "1.0.0" `
  -OutputPath "dist" `
  -BuildConfiguration "Release"
```

### 2. Verify Distribution Package

```powershell
# Verify all files and checksums
.\scripts\deployment\verify-distribution.ps1 `
  -DistributionPath "dist" `
  -Version "1.0.0"
```

### 3. Publish to NuGet

```powershell
# Set your NuGet API key
$env:NUGET_API_KEY = "your-nuget-api-key"

# Publish package
.\scripts\deployment\publish-nuget.ps1 `
  -Version "1.0.0"
```

### 4. Create GitHub Release

```powershell
# Set GitHub token
$env:GITHUB_TOKEN = "your-github-token"

# Create release
.\scripts\deployment\create-release.ps1 `
  -Version "1.0.0" `
  -DistributionPath "dist"
```

## File Structure

```
helios-platform/
├── .github/workflows/
│   ├── publish-nuget.yml              # NuGet publishing workflow
│   ├── create-release.yml             # GitHub release workflow
│   └── publish-to-packagemanagers.yml # Package manager workflow
├── scripts/deployment/
│   ├── prepare-distribution.ps1       # Create distribution package
│   ├── verify-distribution.ps1        # Verify package integrity
│   ├── publish-nuget.ps1              # Publish to NuGet.org
│   └── create-release.ps1             # Create GitHub release
├── docs/
│   ├── DISTRIBUTION_GUIDE.md          # Distribution channel guide
│   ├── INSTALLATION_GUIDE.md          # Installation instructions
│   ├── DEPLOYMENT_CHECKLIST.md        # Pre-deployment checklist
│   └── DEPLOYMENT_VERIFICATION_ROLLBACK.md  # Verification & rollback
├── dist/
│   └── v1.0.0/
│       ├── executables/               # .exe files
│       ├── nuget/                     # .nupkg and .nuspec
│       ├── demos/                     # Demo applications
│       ├── documentation/             # Guides and docs
│       ├── installer/                 # Setup.exe
│       ├── checksums/                 # Verification hashes
│       └── CHECKSUMS.txt              # All file checksums
└── RELEASE_NOTES.md                   # v1.0.0 release notes
```

## GitHub Actions Workflows

### 1. publish-nuget.yml
**Trigger**: Git tag push (v1.0.0) or manual dispatch

**Actions**:
- Checkout code
- Setup .NET
- Build and test
- Prepare distribution
- Verify distribution
- Create NuGet package
- Publish to NuGet.org

```yaml
on:
  push:
    tags:
      - 'v*.*.*'
  workflow_dispatch:
    inputs:
      version: 'Release version'
      dry_run: 'Dry run mode'
```

### 2. create-release.yml
**Trigger**: Git tag push (v1.0.0) or manual dispatch

**Actions**:
- Checkout code
- Build projects
- Prepare distribution
- Verify distribution
- Create GitHub Release
- Upload artifacts

### 3. publish-to-packagemanagers.yml
**Trigger**: Release published

**Actions**:
- Create Chocolatey package
- Create Winget manifest
- Prepare for package manager submission

## Deployment Scripts

### prepare-distribution.ps1

Creates complete distribution package with all files.

```powershell
.\scripts\deployment\prepare-distribution.ps1 `
  -Version "1.0.0" `
  -OutputPath "dist" `
  -BuildConfiguration "Release"
```

**Creates**:
- executables/ - All .exe files
- nuget/ - NuGet package and .nuspec
- demos/ - Demo applications
- documentation/ - Guides and README
- installer/ - Setup.exe
- checksums/ - CHECKSUMS.txt
- CHECKSUMS.txt - All file hashes

**Output**:
```
✓ Created: dist/v1.0.0/executables/
✓ Created: dist/v1.0.0/nuget/
✓ Created: dist/v1.0.0/demos/
✓ Created: dist/v1.0.0/documentation/
✓ Created: dist/v1.0.0/installer/
✓ Created: dist/v1.0.0/checksums/
✓ Generated checksums...
Distribution Package Prepared Successfully!
```

### verify-distribution.ps1

Validates all distribution files and integrity.

```powershell
.\scripts\deployment\verify-distribution.ps1 `
  -DistributionPath "dist" `
  -Version "1.0.0"
```

**Checks**:
- ✓ Directory structure
- ✓ Executable files
- ✓ NuGet package
- ✓ Demo applications
- ✓ Documentation
- ✓ File integrity (checksums)
- ✓ File accessibility
- ✓ File sizes

**Output**:
```
[CHECK 1] Directory Structure
  [✓ PASS] Directory: executables
  [✓ PASS] Directory: nuget
  ...
[CHECK 2] Executable Files
  [✓ PASS] Executables present
  ...
Verification Summary
Passed: 24
Failed: 0
✓ All checks passed! Distribution package is ready.
```

### publish-nuget.ps1

Publishes NuGet package to NuGet.org.

```powershell
$env:NUGET_API_KEY = "your-api-key"
.\scripts\deployment\publish-nuget.ps1 `
  -Version "1.0.0"
```

**Steps**:
1. Validates NuSpec file
2. Creates NuGet package
3. Verifies API key
4. Pushes to NuGet.org

**Output**:
```
[1/4] Validating NuSpec file...
[2/4] Creating NuGet package...
[3/4] Validating NuGet API key...
[4/4] Publishing to NuGet.org...
✓ Package published successfully!
```

### create-release.ps1

Creates GitHub Release with all artifacts.

```powershell
$env:GITHUB_TOKEN = "your-token"
.\scripts\deployment\create-release.ps1 `
  -Version "1.0.0" `
  -DistributionPath "dist"
```

**Steps**:
1. Prepares release notes
2. Collects artifacts
3. Creates GitHub API request
4. Creates release
5. Uploads artifacts

**Output**:
```
[1/5] Preparing release notes...
[2/5] Collecting distribution artifacts...
[3/5] Preparing GitHub API request...
[4/5] Creating GitHub release...
[5/5] Uploading release artifacts...
✓ GitHub Release Created!
```

## Configuration

### Required Secrets (GitHub)

Add to: Settings → Secrets and variables → Actions

```
NUGET_API_KEY        - NuGet.org API key
GITHUB_TOKEN         - GitHub personal access token
CHOCO_API_KEY        - Chocolatey API key (optional)
```

### Environment Variables

```powershell
# NuGet publishing
$env:NUGET_API_KEY = "oy2a..."

# GitHub release creation
$env:GITHUB_TOKEN = "ghp_..."

# Package manager publishing
$env:CHOCO_API_KEY = "..."
```

## Distribution Channels

### 1. NuGet.org
- **Package**: HELIOS.Platform
- **Install**: `nuget install HELIOS.Platform -Version 1.0.0`
- **URL**: https://www.nuget.org/packages/HELIOS.Platform/

### 2. GitHub Releases
- **URL**: https://github.com/HELIOS-Platform/helios-platform/releases
- **Tag**: v1.0.0
- **Artifacts**: All .exe, .nupkg, demos, docs

### 3. Chocolatey
- **Package**: helios-platform
- **Install**: `choco install helios-platform`
- **URL**: https://community.chocolatey.org/packages/helios-platform/

### 4. Winget
- **Package**: HELIOS.Platform
- **Install**: `winget install HELIOS.Platform`
- **Manifests**: Submitted to microsoft/winget-pkgs

### 5. Direct Downloads
- **Files**: Hosted on GitHub Releases
- **Format**: HELIOS-Setup.exe, HELIOS.Platform.exe, demos

## Deployment Process

### Phase 1: Preparation
1. Update version numbers
2. Update CHANGELOG.md
3. Create RELEASE_NOTES.md
4. Commit and create git tag

### Phase 2: Automated Publishing
1. GitHub Actions triggered by tag
2. Builds and tests code
3. Creates distribution package
4. Verifies all files
5. Publishes to NuGet.org
6. Creates GitHub Release
7. Prepares package managers

### Phase 3: Manual Steps
1. Submit to Chocolatey (auto-approve in ~10-30 min)
2. Submit PR to microsoft/winget-pkgs
3. Monitor for issues
4. Announce release

### Phase 4: Verification
1. Test installation from each channel
2. Verify version numbers
3. Check documentation
4. Monitor downloads

### Phase 5: Rollback (if needed)
1. Unlist from NuGet.org
2. Mark GitHub Release retracted
3. Notify users
4. Create hotfix (v1.0.1)
5. Re-release

## Troubleshooting

### NuGet Publishing Fails
```powershell
# Verify API key
Write-Host $env:NUGET_API_KEY

# Check NuSpec file
$xml = [xml](Get-Content "dist\v1.0.0\nuget\HELIOS.Platform.nuspec")
$xml.package.metadata

# Try with verbose output
nuget push ... -Verbosity detailed
```

### GitHub Release Issues
```powershell
# Verify token
Write-Host $env:GITHUB_TOKEN

# Check repository
git config --get remote.origin.url

# Verify tag exists
git tag | grep v1.0.0
```

### Distribution Verification Fails
```powershell
# Run detailed verification
.\scripts\deployment\verify-distribution.ps1 -Version "1.0.0"

# Check specific files
Get-ChildItem dist\v1.0.0 -Recurse | Where-Object Length -lt 100

# Verify checksums
(Get-FileHash "dist\v1.0.0\executables\HELIOS.exe" -Algorithm SHA256).Hash
```

## Dry-Run Mode

Test deployment without publishing:

```powershell
# Prepare distribution (always safe)
.\scripts\deployment\prepare-distribution.ps1 -Version "1.0.0"

# Verify distribution (always safe)
.\scripts\deployment\verify-distribution.ps1 -Version "1.0.0"

# Dry-run NuGet publish
.\scripts\deployment\publish-nuget.ps1 -Version "1.0.0" -DryRun

# Dry-run GitHub release
.\scripts\deployment\create-release.ps1 -Version "1.0.0" -DryRun
```

## Best Practices

1. **Always Run Verification**
   ```powershell
   .\scripts\deployment\verify-distribution.ps1
   ```

2. **Use Semantic Versioning**
   - Major.Minor.Patch (1.0.0)
   - Follow conventions

3. **Tag Before Publishing**
   ```powershell
   git tag -a v1.0.0 -m "HELIOS Platform v1.0.0"
   git push origin v1.0.0
   ```

4. **Test in Stages**
   - Local NuGet: `nuget install -OutputDirectory test`
   - Clean VM installation
   - All supported Windows versions

5. **Document Changes**
   - Update CHANGELOG.md
   - Create RELEASE_NOTES.md
   - Update installation guides

## Deployment Checklist

See `docs/DEPLOYMENT_CHECKLIST.md` for complete pre-deployment checklist.

## Support

- **Issues**: https://github.com/HELIOS-Platform/helios-platform/issues
- **Discussions**: https://github.com/HELIOS-Platform/helios-platform/discussions
- **Email**: support@helios-platform.org
- **Documentation**: https://helios-platform.github.io/

## License

MIT License - See LICENSE file

---

**Version**: 1.0.0  
**Status**: Production Ready  
**Last Updated**: 2024
