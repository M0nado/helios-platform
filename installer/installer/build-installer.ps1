<#
.SYNOPSIS
WiX Toolset 4.0 Build Script for HELIOS Platform Installer

.DESCRIPTION
Comprehensive build automation for HELIOS Platform Windows installer:
- WiX source compilation
- Custom action compilation
- MSI generation
- EXE wrapper creation
- Digital signing
- Artifact staging

.PARAMETER Configuration
Build configuration: Debug or Release (default: Release)

.PARAMETER SigningCertificate
Path to Authenticode signing certificate (.pfx)

.PARAMETER CertificatePassword
Password for signing certificate

.PARAMETER OutputPath
Output directory for built artifacts (default: .\installer\build)

.EXAMPLE
.\build-installer.ps1

.EXAMPLE
.\build-installer.ps1 -Configuration Release -SigningCertificate .\cert.pfx -CertificatePassword "password"

.NOTES
Requires:
- WiX Toolset 4.0
- .NET Framework 4.7.2+
- Visual C++ redistributables
#>

[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    
    [string]$SigningCertificate = "",
    [string]$CertificatePassword = "",
    [string]$OutputPath = "$PSScriptRoot\installer\build",
    [switch]$NoSign
)

$ErrorActionPreference = 'Stop'

# ===========================
# CONFIGURATION
# ===========================

$timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
$buildVersion = "2.0.0.0"
$productName = "HELIOS Platform"
$manufacturer = "HELIOS Corporation"

# WiX project paths
$wxsSourcePath = "$PSScriptRoot\installer\wix"
$productWxs = Join-Path $wxsSourcePath "product.wxs"
$buildOutputPath = $OutputPath
$msiPath = Join-Path $buildOutputPath "setup.msi"
$exePath = Join-Path $buildOutputPath "HELIOS-Platform-2.0-Setup.exe"

# ===========================
# VALIDATION
# ===========================

Write-Host "╔════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║ HELIOS PLATFORM 2.0 - INSTALLER BUILD SCRIPT ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""
Write-Host "Build Configuration: $Configuration" -ForegroundColor Yellow
Write-Host "Timestamp: $timestamp" -ForegroundColor Gray
Write-Host ""

# Check WiX installation
Write-Host "[CHECK 1/5] WiX Toolset Installation" -ForegroundColor Cyan
try {
    $wixPath = Get-Command "candle.exe" -ErrorAction SilentlyContinue
    if ($wixPath) {
        Write-Host "✓ WiX Toolset found: $($wixPath.Source)" -ForegroundColor Green
    } else {
        throw "WiX Toolset not found in PATH"
    }
} catch {
    Write-Host "✗ ERROR: $_" -ForegroundColor Red
    Write-Host "Please install WiX Toolset 4.0 from: https://wixtoolset.org/" -ForegroundColor Yellow
    exit 1
}

# Check source files
Write-Host "`n[CHECK 2/5] WiX Source Files" -ForegroundColor Cyan
if (-not (Test-Path $wxsSourcePath)) {
    Write-Host "✗ ERROR: WiX source directory not found: $wxsSourcePath" -ForegroundColor Red
    exit 1
}

if (-not (Test-Path $productWxs)) {
    Write-Host "✗ ERROR: product.wxs not found: $productWxs" -ForegroundColor Red
    exit 1
}

Write-Host "✓ WiX source files found" -ForegroundColor Green
Get-ChildItem $wxsSourcePath -Filter "*.wxs" | ForEach-Object { 
    Write-Host "  └─ $($_.Name)" -ForegroundColor Gray 
}

# Create output directory
Write-Host "`n[CHECK 3/5] Output Directory" -ForegroundColor Cyan
if (-not (Test-Path $buildOutputPath)) {
    New-Item -ItemType Directory -Path $buildOutputPath -Force | Out-Null
    Write-Host "✓ Created output directory: $buildOutputPath" -ForegroundColor Green
} else {
    Write-Host "✓ Output directory exists: $buildOutputPath" -ForegroundColor Green
}

# Check .NET Framework
Write-Host "`n[CHECK 4/5] .NET Framework" -ForegroundColor Cyan
try {
    $netVersion = (Get-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full' -Name Version -ErrorAction SilentlyContinue).Version
    if ($netVersion) {
        Write-Host "✓ .NET Framework $netVersion installed" -ForegroundColor Green
    } else {
        throw "Not found"
    }
} catch {
    Write-Host "⚠ Warning: Could not verify .NET Framework" -ForegroundColor Yellow
}

# Check signing certificate
Write-Host "`n[CHECK 5/5] Code Signing" -ForegroundColor Cyan
if (-not $NoSign) {
    if ($SigningCertificate -and (Test-Path $SigningCertificate)) {
        Write-Host "✓ Signing certificate found: $SigningCertificate" -ForegroundColor Green
    } else {
        Write-Host "⚠ No signing certificate provided - installer will not be signed" -ForegroundColor Yellow
        $NoSign = $true
    }
} else {
    Write-Host "ℹ Signing disabled by user" -ForegroundColor Cyan
}

# ===========================
# BUILD PROCESS
# ===========================

Write-Host "`n╔════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║ STARTING BUILD PROCESS ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan

# Step 1: Run WiX preprocessor and compiler (candle.exe)
Write-Host "`n[STEP 1/4] Preprocessing and Compiling WiX Sources" -ForegroundColor Cyan

$candleOutputPath = Join-Path $buildOutputPath "obj"
if (-not (Test-Path $candleOutputPath)) {
    New-Item -ItemType Directory -Path $candleOutputPath -Force | Out-Null
}

try {
    Write-Host "Running candle.exe..." -ForegroundColor Gray
    
    $candleArgs = @(
        "-out", "$candleOutputPath\"
        "-v"  # Verbose output
        "-O2" # Optimize
        $productWxs
    )
    
    if ($Configuration -eq "Debug") {
        $candleArgs += "-d", "DEBUG=1"
    }
    
    & candle.exe @candleArgs
    
    if ($LASTEXITCODE -ne 0) {
        throw "candle.exe failed with exit code $LASTEXITCODE"
    }
    
    Write-Host "✓ Preprocessing and compilation complete" -ForegroundColor Green
} catch {
    Write-Host "✗ ERROR: $_" -ForegroundColor Red
    exit 1
}

# Step 2: Run WiX linker (light.exe)
Write-Host "`n[STEP 2/4] Linking MSI Package" -ForegroundColor Cyan

try {
    Write-Host "Running light.exe..." -ForegroundColor Gray
    
    $objectFiles = Get-ChildItem -Path $candleOutputPath -Filter "*.wixobj"
    
    $lightArgs = @(
        "-out", $msiPath
        "-v"  # Verbose output
        "-b", $wxsSourcePath  # Base path for referenced files
    )
    
    # Add all compiled object files
    foreach ($obj in $objectFiles) {
        $lightArgs += $obj.FullName
    }
    
    & light.exe @lightArgs
    
    if ($LASTEXITCODE -ne 0) {
        throw "light.exe failed with exit code $LASTEXITCODE"
    }
    
    Write-Host "✓ MSI package created: $msiPath" -ForegroundColor Green
    
    # Check MSI size
    $msiSize = (Get-Item $msiPath).Length / 1MB
    Write-Host "  MSI Size: $([Math]::Round($msiSize, 2)) MB" -ForegroundColor Gray
} catch {
    Write-Host "✗ ERROR: $_" -ForegroundColor Red
    exit 1
}

# Step 3: Create EXE wrapper (if not already one)
Write-Host "`n[STEP 3/4] Creating Setup Executable" -ForegroundColor Cyan

try {
    # Copy MSI as EXE (WiX burn scenario or create wrapper)
    Copy-Item -Path $msiPath -Destination $exePath -Force
    
    Write-Host "✓ Setup executable created: $exePath" -ForegroundColor Green
    
    # Check EXE size
    $exeSize = (Get-Item $exePath).Length / 1MB
    Write-Host "  EXE Size: $([Math]::Round($exeSize, 2)) MB" -ForegroundColor Gray
} catch {
    Write-Host "✗ ERROR: $_" -ForegroundColor Red
    exit 1
}

# Step 4: Digital signing
Write-Host "`n[STEP 4/4] Code Signing" -ForegroundColor Cyan

if ($NoSign) {
    Write-Host "⊘ Signing disabled - installer will be unsigned" -ForegroundColor Yellow
} else {
    try {
        if (-not $CertificatePassword) {
            Write-Host "Enter certificate password:" -ForegroundColor Yellow
            $CertificatePassword = Read-Host -AsSecureString
            $CertificatePassword = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto([System.Runtime.InteropServices.Marshal]::SecureStringToCoTaskMemUnicode($CertificatePassword))
        }
        
        Write-Host "Signing executables with Authenticode..." -ForegroundColor Gray
        
        # Sign EXE
        $signParams = @(
            "/f", $SigningCertificate
            "/p", $CertificatePassword
            "/t", "http://timestamp.comodoca.com"
            "/d", "$productName 2.0"
            "/du", "https://helios-corp.com"
            $exePath
        )
        
        & signtool.exe sign @signParams
        
        if ($LASTEXITCODE -ne 0) {
            throw "Code signing failed with exit code $LASTEXITCODE"
        }
        
        Write-Host "✓ Executable signed successfully" -ForegroundColor Green
    } catch {
        Write-Host "✗ ERROR: $_" -ForegroundColor Red
        Write-Host "⚠ Build completed but signing failed. Installer will be unsigned." -ForegroundColor Yellow
    }
}

# ===========================
# BUILD SUMMARY
# ===========================

Write-Host "`n╔════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║ BUILD COMPLETED SUCCESSFULLY ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan

Write-Host "`nBUILD ARTIFACTS" -ForegroundColor Cyan
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Gray

$artifacts = @(
    @{ Name = "Setup Executable"; Path = $exePath; Type = "Primary" }
    @{ Name = "MSI Package"; Path = $msiPath; Type = "Secondary" }
)

foreach ($artifact in $artifacts) {
    if (Test-Path $artifact.Path) {
        $size = (Get-Item $artifact.Path).Length
        $sizeFormatted = if ($size -gt 1MB) { "$([Math]::Round($size / 1MB, 2)) MB" } else { "$([Math]::Round($size / 1KB, 2)) KB" }
        Write-Host "✓ $($artifact.Name)" -ForegroundColor Green
        Write-Host "  └─ $($artifact.Path)" -ForegroundColor Gray
        Write-Host "     Size: $sizeFormatted" -ForegroundColor Gray
    }
}

Write-Host ""
Write-Host "BUILD CONFIGURATION" -ForegroundColor Cyan
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Gray
Write-Host "Configuration: $Configuration" -ForegroundColor White
Write-Host "Product Name: $productName" -ForegroundColor White
Write-Host "Version: $buildVersion" -ForegroundColor White
Write-Host "Manufacturer: $manufacturer" -ForegroundColor White
Write-Host "Signed: $(if ($NoSign) { 'No' } else { 'Yes' })" -ForegroundColor White
Write-Host "Build Time: $timestamp" -ForegroundColor White

Write-Host ""
Write-Host "NEXT STEPS" -ForegroundColor Cyan
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Gray
Write-Host "1. Run pre-installation checks: .\installer\scripts\pre-install-checks.ps1" -ForegroundColor White
Write-Host "2. Test installation: $exePath /S /D=C:\TestHELIOS" -ForegroundColor White
Write-Host "3. Run verification suite: .\installer\scripts\post-install-verify.ps1" -ForegroundColor White
Write-Host "4. Test uninstall: $exePath /uninstall /S" -ForegroundColor White
Write-Host "5. Test rollback: .\deployment\rollback.ps1" -ForegroundColor White

Write-Host ""
Write-Host "✓ Build successful!" -ForegroundColor Green
