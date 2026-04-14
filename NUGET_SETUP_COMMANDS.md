# HELIOS Platform - NuGet Setup Quick Reference

One-page command reference for all common NuGet operations.

---

## LOCAL DEVELOPMENT

### Initial Setup
```powershell
# Clone repository
git clone https://github.com/M0nado/helios-platform.git
cd helios-platform

# Restore dependencies
dotnet restore

# Build for Release
dotnet build src/HELIOS.Platform/HELIOS.Platform.csproj -c Release

# Run tests
dotnet test tests/ -c Release

# Create package
dotnet pack src/HELIOS.Platform/HELIOS.Platform.csproj -c Release -o artifacts/

# List package contents
Get-ChildItem artifacts/*.nupkg -Recurse
```

### Daily Development
```powershell
# Build (quick)
dotnet build

# Build Release
dotnet build -c Release

# Run tests
dotnet test

# Run specific test
dotnet test tests/HELIOS.Platform.Tests/ComponentTests.cs

# Clean build
dotnet clean && dotnet build -c Release

# Full test suite with coverage
dotnet test -c Release --collect:"XPlat Code Coverage"
```

### Debugging
```powershell
# Build with debug symbols
dotnet build -c Debug

# Run with verbose logging
dotnet build -v diagnostic

# Show detailed test output
dotnet test --logger "console;verbosity=detailed"

# List all local packages
dotnet nuget locals all --list

# Clear cache
dotnet nuget locals cache --clear
```

---

## BUILDING & PACKAGING

### Build Commands
```powershell
# Build all frameworks Release
dotnet build src/HELIOS.Platform/HELIOS.Platform.csproj -c Release

# Build specific framework
dotnet build src/HELIOS.Platform/HELIOS.Platform.csproj -c Release -f net8.0

# Build all projects
dotnet build

# Rebuild (clean + build)
dotnet clean && dotnet build -c Release

# Build and output to specific folder
dotnet build -o C:\build-output
```

### Package Creation
```powershell
# Create package
dotnet pack src/HELIOS.Platform/HELIOS.Platform.csproj -c Release -o artifacts/

# Create with version override
dotnet pack src/HELIOS.Platform/HELIOS.Platform.csproj `
  -c Release `
  -o artifacts/ `
  /p:Version=1.0.1

# List created packages
dir artifacts/*.nupkg

# Verify package
$nupkg = "artifacts/HELIOS.Platform.1.0.0.nupkg"
Expand-Archive $nupkg temp-verify/
dir temp-verify/ -Recurse
```

### Version Management
```powershell
# Get current version from csproj
$csproj = [xml](Get-Content "src/HELIOS.Platform/HELIOS.Platform.csproj")
$csproj.Project.PropertyGroup.Version

# Update version
$csproj.Project.PropertyGroup.Version = "1.0.1"
$csproj.Save("src/HELIOS.Platform/HELIOS.Platform.csproj")
```

---

## PUBLISHING & DISTRIBUTION

### Publish to NuGet.org
```powershell
# Push package (requires API key)
$nupkg = "artifacts/HELIOS.Platform.1.0.0.nupkg"
dotnet nuget push $nupkg `
  --api-key $env:NUGET_API_KEY `
  --source https://api.nuget.org/v3/index.json

# Push with skip duplicate
dotnet nuget push $nupkg `
  --api-key $env:NUGET_API_KEY `
  --source https://api.nuget.org/v3/index.json `
  --skip-duplicate

# Verify published
# Visit: https://www.nuget.org/packages/HELIOS.Platform/
```

### Publish to GitHub Packages
```powershell
# Add GitHub source (first time only)
dotnet nuget add source `
  --username <your-username> `
  --password <your-token> `
  --store-password-in-clear-text `
  --name github `
  https://nuget.pkg.github.com/M0nado/index.json

# Push to GitHub
$nupkg = "artifacts/HELIOS.Platform.1.0.0.nupkg"
dotnet nuget push $nupkg `
  --source github `
  --skip-duplicate
```

### List Package Sources
```powershell
# Show all configured sources
dotnet nuget list source

# Add source
dotnet nuget add source -n "MySource" "https://api.nuget.org/v3/index.json"

# Remove source
dotnet nuget remove source "MySource"

# Update source
dotnet nuget update source "MySource" -s "https://new-url"

# Disable source
dotnet nuget disable source "MySource"

# Enable source
dotnet nuget enable source "MySource"
```

---

## INSTALLING PACKAGES

### Install Package
```powershell
# Install latest version
dotnet add package HELIOS.Platform

# Install specific version
dotnet add package HELIOS.Platform --version 1.0.0

# Install with prerelease
dotnet add package HELIOS.Platform --prerelease

# Install to specific project
dotnet add MyProject/MyProject.csproj package HELIOS.Platform

# Install from specific source
dotnet add package HELIOS.Platform --source github
```

### Manage Installed Packages
```powershell
# List all packages
dotnet list package

# List with versions
dotnet list package --include-transitive

# Check for outdated packages
dotnet list package --outdated

# Check for vulnerable packages
dotnet list package --vulnerable

# Update package
dotnet add package HELIOS.Platform --version 1.0.1

# Remove package
dotnet remove package HELIOS.Platform

# Update all to latest
dotnet upgrade --skip-verify
```

---

## TESTING

### Run Tests
```powershell
# Run all tests
dotnet test

# Run Release tests
dotnet test -c Release

# Run tests for specific project
dotnet test tests/HELIOS.Platform.Tests/HELIOS.Platform.Tests.csproj

# Run with detailed output
dotnet test -v d

# Run specific test class
dotnet test tests/ --filter ClassName

# Run specific test method
dotnet test tests/ --filter FullyQualifiedName~TestMethodName

# Run tests in parallel
dotnet test -p:ParallelizeTestCollections=true
```

### Test Coverage
```powershell
# Collect coverage data
dotnet test -c Release --collect:"XPlat Code Coverage"

# View coverage report
# Reports saved in: TestResults/

# Generate HTML report
# Requires: reportgenerator
reportgenerator -reports:TestResults/**/coverage.cobertura.xml `
  -reporttypes:Html `
  -targetdir:coverage-report

# Open report
Start-Process coverage-report/index.html
```

---

## GIT & RELEASES

### Git Operations
```powershell
# Clone repository
git clone https://github.com/M0nado/helios-platform.git

# Check status
git status

# Add changes
git add .

# Commit changes
git commit -m "Commit message"

# Push to remote
git push origin main

# Create tag
git tag -a v1.0.0 -m "Release v1.0.0"

# Push tag
git push origin v1.0.0

# List tags
git tag -l

# View tag info
git show v1.0.0
```

### Release Workflow
```powershell
# 1. Update version in csproj
$csproj = [xml](Get-Content "src/HELIOS.Platform/HELIOS.Platform.csproj")
$csproj.Project.PropertyGroup.Version = "1.0.1"
$csproj.Save("src/HELIOS.Platform/HELIOS.Platform.csproj")

# 2. Update CHANGELOG.md
# Edit manually...

# 3. Commit
git add -A
git commit -m "Release v1.0.1

- Bug fixes
- Performance improvements

Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>"

# 4. Create tag
git tag -a v1.0.1 -m "HELIOS.Platform v1.0.1 release"

# 5. Push
git push origin main
git push origin v1.0.1

# GitHub Actions workflow runs automatically!
# Monitor at: https://github.com/M0nado/helios-platform/actions
```

---

## NUGET API KEYS

### Get API Key
```powershell
# Visit: https://www.nuget.org/account/apikeys
# Create new key with:
# - Name: GitHub Actions HELIOS
# - Scope: Push new packages
# - Pattern: HELIOS.Platform*
# - Expiration: 90 days
```

### Store API Key
```powershell
# Via GitHub CLI
gh secret set NUGET_API_KEY --body "your-api-key"

# Via GitHub Web UI
# Repository → Settings → Secrets → New repository secret
# Name: NUGET_API_KEY
# Value: [paste key]

# Local (for testing - DON'T COMMIT)
$env:NUGET_API_KEY = "your-api-key"
```

### Verify API Key
```powershell
# Test API key validity
# This will fail with 401 if key is invalid
dotnet nuget push dummy.nupkg `
  --api-key $env:NUGET_API_KEY `
  --source https://api.nuget.org/v3/index.json

# Check if environment variable set
if ($env:NUGET_API_KEY) {
    Write-Host "✓ API key configured"
} else {
    Write-Host "✗ API key NOT configured"
}
```

---

## TROUBLESHOOTING

### Clear Caches
```powershell
# Clear all NuGet caches
dotnet nuget locals cache --clear

# Clear HTTP cache
dotnet nuget locals http-cache --clear

# Clear temp cache
dotnet nuget locals temp --clear

# List all cache locations
dotnet nuget locals all --list
```

### Package Issues
```powershell
# Package not found
# 1. Check sources
dotnet nuget list source

# 2. Add missing source
dotnet nuget add source -n "nuget" "https://api.nuget.org/v3/index.json"

# 3. Clear cache
dotnet nuget locals cache --clear

# 4. Restore
dotnet restore
```

### Build Issues
```powershell
# Clean everything
dotnet clean

# Remove obj/bin folders
Remove-Item -Recurse obj/, bin/

# Restore and rebuild
dotnet restore
dotnet build -c Release

# Verbose output
dotnet build -v diagnostic

# Check for compilation errors
dotnet build | Select-String "error"
```

### Version Conflicts
```powershell
# Check dependency tree
dotnet list package --include-transitive

# Find all Azure.Identity versions
dotnet list package --include-transitive | Select-String "Azure.Identity"

# Update problematic package
dotnet add package Azure.Identity --version 1.10.0

# Remove and reinstall
dotnet remove package Azure.Identity
dotnet add package Azure.Identity
```

---

## QUICK CHECKS

### Pre-Release Verification
```powershell
# 1. Tests pass?
dotnet test -c Release
$testsPassed = $LASTEXITCODE -eq 0

# 2. Builds successfully?
dotnet build -c Release -v m
$buildSucceeded = $LASTEXITCODE -eq 0

# 3. Package created?
dotnet pack src/HELIOS.Platform/HELIOS.Platform.csproj -c Release
$packageExists = Test-Path "bin/Release/*.nupkg"

# 4. Display status
@"
Tests: $(if ($testsPassed) { '✓' } else { '✗' })
Build: $(if ($buildSucceeded) { '✓' } else { '✗' })
Package: $(if ($packageExists) { '✓' } else { '✗' })
"@

# Summary
if ($testsPassed -and $buildSucceeded -and $packageExists) {
    Write-Host "✓ Ready for release!"
} else {
    Write-Host "✗ Issues found, fix before release"
    exit 1
}
```

### Health Check Script
```powershell
# Comprehensive health check
function Test-HeliosHealth {
    Write-Host "HELIOS Platform Health Check`n"
    
    # 1. Git status
    $git = git status --short
    Write-Host "Git status: $(if ([string]::IsNullOrEmpty($git)) { '✓ Clean' } else { '✗ Uncommitted changes' })"
    
    # 2. Tests
    Write-Host "Running tests..."
    dotnet test -c Release --no-build -q
    Write-Host "Tests: $(if ($LASTEXITCODE -eq 0) { '✓ Pass' } else { '✗ Failed' })"
    
    # 3. Build
    Write-Host "Building..."
    dotnet build -c Release -q
    Write-Host "Build: $(if ($LASTEXITCODE -eq 0) { '✓ Success' } else { '✗ Failed' })"
    
    # 4. Package
    Write-Host "Packaging..."
    dotnet pack src/HELIOS.Platform/HELIOS.Platform.csproj -c Release -q
    $pkgCount = @(Get-ChildItem bin/Release/*.nupkg -ErrorAction SilentlyContinue).Count
    Write-Host "Package: $(if ($pkgCount -gt 0) { "✓ Created ($pkgCount)" } else { '✗ Not found' })"
    
    # 5. Dependencies
    Write-Host "Checking dependencies..."
    $vuln = dotnet list package --vulnerable
    Write-Host "Vulnerable: $(if ($LASTEXITCODE -eq 0) { '✓ None' } else { '✗ Found' })"
}

# Run health check
Test-HeliosHealth
```

---

## ENVIRONMENT SETUP (One-time)

```powershell
# Install .NET 8.0 SDK
# https://dotnet.microsoft.com/download/dotnet/8.0

# Verify installation
dotnet --version
dotnet --list-sdks
dotnet --list-runtimes

# Install PowerShell 7+
# https://github.com/PowerShell/PowerShell/releases

# Install Git
# https://git-scm.com/download/win

# Install GitHub CLI (optional)
# https://cli.github.com/

# Add NuGet source (one-time)
dotnet nuget add source -n "nuget" "https://api.nuget.org/v3/index.json"

# Store API key (securely)
# Go to https://www.nuget.org/account/apikeys
# Create new key for GitHub Actions
# Add to GitHub repository secrets
```

---

## COMMON WORKFLOWS

### Feature Development
```powershell
# 1. Create feature branch
git checkout -b feature/new-feature

# 2. Make changes
# ... edit code ...

# 3. Run tests
dotnet test -c Release

# 4. Commit
git add .
git commit -m "Add new feature"

# 5. Push and create PR
git push origin feature/new-feature
# Create PR on GitHub

# 6. Merge after approval
# GitHub merges to main
# Workflow runs automatically
```

### Bug Fix
```powershell
# 1. Create fix branch
git checkout -b fix/bug-description

# 2. Fix bug
# ... edit code ...

# 3. Add test case
# ... create test ...

# 4. Verify fix
dotnet test -c Release

# 5. Commit
git commit -m "Fix: bug description"

# 6. Push and create PR
git push origin fix/bug-description

# 7. After merge
# Workflow releases patch automatically
```

### Release
```powershell
# 1. Update version
[xml]$csproj = Get-Content "src/HELIOS.Platform/HELIOS.Platform.csproj"
$csproj.Project.PropertyGroup.Version = "1.0.1"
$csproj.Save("src/HELIOS.Platform/HELIOS.Platform.csproj")

# 2. Update CHANGELOG.md
# Edit manually...

# 3. Commit
git commit -m "Release v1.0.1" -A

# 4. Create tag
git tag v1.0.1

# 5. Push
git push origin main v1.0.1

# 6. Monitor GitHub Actions
# Workflow builds, tests, and publishes
```

---

## REFERENCE LINKS

- Repository: https://github.com/M0nado/helios-platform
- NuGet Package: https://www.nuget.org/packages/HELIOS.Platform/
- Package API: https://api.nuget.org/v3/index.json
- Documentation: https://github.com/M0nado/helios-platform/wiki
- Issues: https://github.com/M0nado/helios-platform/issues

---

**Last Updated:** April 13, 2024
**Version:** 1.0.0
**Repository:** https://github.com/M0nado/helios-platform
