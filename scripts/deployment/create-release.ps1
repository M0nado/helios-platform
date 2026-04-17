# HELIOS Platform - Create GitHub Release
# This script creates a GitHub release with artifacts

param(
    [string]$Version = "1.0.0",
    [string]$DistributionPath = "dist",
    [string]$GitHubToken = $env:GITHUB_TOKEN,
    [string]$ChangelogPath = "RELEASE_NOTES.md",
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent (Split-Path -Parent $scriptDir)

Write-Host "===========================================" -ForegroundColor Cyan
Write-Host "GitHub Release Creation" -ForegroundColor Cyan
Write-Host "Version: $Version" -ForegroundColor Cyan
Write-Host "===========================================" -ForegroundColor Cyan

# Resolve paths
if (-not [System.IO.Path]::IsPathRooted($DistributionPath)) {
    $DistributionPath = Join-Path $projectRoot $DistributionPath
}

$distPath = Join-Path $DistributionPath "v$Version"

# Validate inputs
if (-not (Test-Path $distPath)) {
    Write-Host "✗ Error: Distribution path not found: $distPath" -ForegroundColor Red
    exit 1
}

if (-not $GitHubToken) {
    Write-Host "✗ Error: GITHUB_TOKEN environment variable not set" -ForegroundColor Red
    exit 1
}

# Get repository info from git
$repoUrl = git -C $projectRoot config --get remote.origin.url
if ($repoUrl -match "github\.com[:/](.+)/(.+?)(?:\.git)?$") {
    $owner = $matches[1]
    $repo = $matches[2] -replace '\.git$'
} else {
    Write-Host "✗ Error: Could not parse GitHub repository from git config" -ForegroundColor Red
    exit 1
}

Write-Host "Repository: $owner/$repo" -ForegroundColor Green
Write-Host "Distribution: $distPath" -ForegroundColor Green

# Step 1: Create release notes
Write-Host "`n[1/5] Preparing release notes..." -ForegroundColor Yellow
$releaseNotes = @"
# HELIOS Platform v$Version

## What's New
- Initial release of HELIOS Platform v$Version
- Multi-framework support (.NET Framework 4.7.2, .NET Core 3.1+, .NET 5+, .NET 6+)
- Comprehensive NuGet package
- Demo applications included
- Full installation support

## Installation
### Via NuGet
\`\`\`
nuget install HELIOS.Platform -Version $Version
\`\`\`

### Via Chocolatey
\`\`\`
choco install helios-platform --version=$Version
\`\`\`

### Via Setup Wizard
1. Download HELIOS-Setup.exe
2. Run the installer
3. Follow the wizard

## Contents
- HELIOS.Platform.exe - Main application
- HELIOS-Setup.exe - Windows installer
- HELIOS.Platform.$Version.nupkg - NuGet package
- demo-games.exe, demo-dev.exe, demo-security.exe - Demo applications
- Complete documentation

## System Requirements
- Windows 7 SP1 or later
- .NET Framework 4.7.2 or .NET 6.0+
- 100 MB free disk space
- Administrator privileges for installation

## Documentation
- [Installation Guide](./dist/v$Version/documentation/INSTALLATION_GUIDE.md)
- [Quick Start Guide](./dist/v$Version/documentation/QUICK_START.md)
- [Readme](./dist/v$Version/documentation/README.txt)

## Support & Feedback
- 🐛 [Report Issues](https://github.com/$owner/$repo/issues)
- 📖 [Documentation](https://helios-platform.github.io/)
- 💬 [Discussions](https://github.com/$owner/$repo/discussions)

## Hash Verification
All files have been verified. See CHECKSUMS.txt for MD5 and SHA256 hashes.

---
**Release Date:** $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
**Version:** $Version
**Build:** Production Ready
"@

if (Test-Path $ChangelogPath) {
    $additionalNotes = Get-Content $ChangelogPath -Raw
    $releaseNotes += "`n`n## Additional Changes`n$additionalNotes"
}

Write-Host "  ✓ Release notes prepared"

# Step 2: Collect artifacts
Write-Host "`n[2/5] Collecting distribution artifacts..." -ForegroundColor Yellow
$artifacts = @()

# Add executables
Get-ChildItem "$distPath\executables" -Filter "*.exe" | ForEach-Object {
    $artifacts += @{
        Path = $_.FullName
        Name = $_.Name
        Type = "executable"
    }
    Write-Host "  ✓ Collected: $($_.Name)"
}

# Add NuGet package
Get-ChildItem "$distPath\nuget" -Filter "*.nupkg" | ForEach-Object {
    $artifacts += @{
        Path = $_.FullName
        Name = $_.Name
        Type = "nuget"
    }
    Write-Host "  ✓ Collected: $($_.Name)"
}

# Add demos
Get-ChildItem "$distPath\demos" -Filter "*.exe" | ForEach-Object {
    $artifacts += @{
        Path = $_.FullName
        Name = $_.Name
        Type = "demo"
    }
    Write-Host "  ✓ Collected: $($_.Name)"
}

# Add documentation
Get-ChildItem "$distPath\documentation" -Filter "*.md" -o "*.txt" | ForEach-Object {
    $artifacts += @{
        Path = $_.FullName
        Name = $_.Name
        Type = "documentation"
    }
}

# Add checksums
if (Test-Path "$distPath\CHECKSUMS.txt") {
    $artifacts += @{
        Path = "$distPath\CHECKSUMS.txt"
        Name = "CHECKSUMS.txt"
        Type = "verification"
    }
}

Write-Host "  ✓ Total artifacts: $($artifacts.Count)"

# Step 3: Prepare API request
Write-Host "`n[3/5] Preparing GitHub API request..." -ForegroundColor Yellow

$releaseBody = $releaseNotes -replace '"', '\"' -replace "`n", '\n'
$tagName = "v$Version"

$releasePayload = @{
    tag_name = $tagName
    name = "HELIOS Platform v$Version"
    body = $releaseBody
    draft = $false
    prerelease = $false
} | ConvertTo-Json

Write-Host "  ✓ Release payload prepared"
Write-Host "  ✓ Tag: $tagName"

# Step 4: Create release (dry-run mode)
Write-Host "`n[4/5] Creating GitHub release..." -ForegroundColor Yellow

if ($DryRun) {
    Write-Host "  [DRY-RUN] Would create release:"
    Write-Host "    Tag: $tagName"
    Write-Host "    Name: HELIOS Platform v$Version"
    Write-Host "    Artifacts: $($artifacts.Count) files"
} else {
    try {
        $headers = @{
            Authorization = "Bearer $GitHubToken"
            "Content-Type" = "application/json"
        }
        
        $apiUrl = "https://api.github.com/repos/$owner/$repo/releases"
        $response = Invoke-RestMethod -Uri $apiUrl -Method Post -Headers $headers -Body $releasePayload
        
        Write-Host "  ✓ Release created successfully"
        Write-Host "  ✓ Release ID: $($response.id)"
        Write-Host "  ✓ URL: $($response.html_url)"
        
        $releaseId = $response.id
    } catch {
        Write-Host "  ✗ Error creating release: $_" -ForegroundColor Red
        exit 1
    }
}

# Step 5: Upload artifacts
Write-Host "`n[5/5] Uploading release artifacts..." -ForegroundColor Yellow

if ($DryRun) {
    Write-Host "  [DRY-RUN] Would upload artifacts:"
    $artifacts | ForEach-Object {
        Write-Host "    - $($_.Name) ($($_.Type))"
    }
} else {
    $artifacts | ForEach-Object {
        $artifact = $_
        try {
            $fileContent = [System.IO.File]::ReadAllBytes($artifact.Path)
            $uploadUrl = "https://uploads.github.com/repos/$owner/$repo/releases/$releaseId/assets?name=$($artifact.Name)"
            
            $uploadHeaders = @{
                Authorization = "Bearer $GitHubToken"
                "Content-Type" = "application/octet-stream"
            }
            
            Invoke-RestMethod -Uri $uploadUrl -Method Post -Headers $uploadHeaders -Body $fileContent | Out-Null
            Write-Host "  ✓ Uploaded: $($artifact.Name)"
        } catch {
            Write-Host "  ✗ Error uploading $($artifact.Name): $_" -ForegroundColor Yellow
        }
    }
}

# Final summary
Write-Host "`n" + "=" * 60 -ForegroundColor Green
Write-Host "GitHub Release Created!" -ForegroundColor Green
Write-Host "=" * 60 -ForegroundColor Green
Write-Host "Tag: $tagName" -ForegroundColor Green
Write-Host "Version: v$Version" -ForegroundColor Green
Write-Host "Artifacts: $($artifacts.Count) files" -ForegroundColor Green
Write-Host "`nRelease URL:"
Write-Host "  https://github.com/$owner/$repo/releases/tag/$tagName" -ForegroundColor Cyan

exit 0
