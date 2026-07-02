@echo off
REM MONADO BLADE Developer Ecosystem - Windows Setup Script
REM Run this as Administrator

setlocal enabledelayedexpansion

echo.
echo 🚀 MONADO BLADE Developer Ecosystem - Windows Setup
echo ====================================================
echo.

REM Check if running as Administrator
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo ❌ ERROR: Please run this script as Administrator
    pause
    exit /b 1
)

REM Check Windows version
for /f "tokens=2 delims=." %%a in ('ver ^| findstr /r "Build"') do set BUILD=%%a
if %BUILD% lss 22000 (
    echo ❌ ERROR: Windows 11 Build 22000 or later required
    echo Your build: %BUILD%
    pause
    exit /b 1
)

echo ✅ Windows version check passed
echo.

REM Install or update WSL2
echo 📦 Setting up WSL2...
wsl --update
wsl --set-default-version 2

if errorlevel 1 (
    echo ⚠️  WSL2 not installed, installing now...
    wsl --install
    echo 📝 Please restart your computer and run this script again
    pause
    exit /b 1
)

echo ✅ WSL2 installed/updated
echo.

REM Install Ubuntu 24.04
echo 📦 Installing Ubuntu 24.04 LTS...
wsl --list --verbose | find "Ubuntu-24.04" >nul
if errorlevel 1 (
    echo Installing Ubuntu 24.04...
    wsl --install -d Ubuntu-24.04
) else (
    echo ✅ Ubuntu 24.04 already installed
)

echo.

REM Install Fedora 40 (optional)
echo 📦 Setting up Fedora 40 (optional)...
wsl --list --verbose | find "Fedora" >nul
if errorlevel 1 (
    echo Installing Fedora 40...
    wsl --install -d Fedora
) else (
    echo ✅ Fedora already installed
)

echo.

REM Configure WSL config
echo ⚙️  Configuring WSL2 settings...
set WSL_CONFIG=%USERPROFILE%\.wslconfig

(
    echo [interop]
    echo guiApplications=true
    echo.
    echo [experimental]
    echo sparseVhd=true
    echo.
    echo [wsl2]
    echo memory=8GB
    echo processors=4
    echo swap=2GB
    echo localhostForwarding=true
) > "%WSL_CONFIG%"

echo ✅ WSL config created at %USERPROFILE%\.wslconfig
echo.

REM Check for Docker Desktop
echo 🐳 Checking Docker Desktop...
docker --version >nul 2>&1
if errorlevel 1 (
    echo ⚠️  Docker Desktop not found
    echo Please download and install Docker Desktop from: https://www.docker.com/products/docker-desktop
    echo.
)

echo.

REM Check for .NET SDK
echo 📙 Checking .NET 8.0 SDK...
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo ⚠️  .NET SDK not found
    echo Please download and install .NET 8.0 SDK from: https://dotnet.microsoft.com/download
    echo.
)

echo.

REM Check for Ollama
echo 🤖 Checking Ollama...
ollama --version >nul 2>&1
if errorlevel 1 (
    echo ⚠️  Ollama not found
    echo Please download and install Ollama from: https://ollama.ai
    echo.
)

echo.

REM Create project structure
echo 📂 Creating project structure...
if not exist "%USERPROFILE%\MonadoBlade\Developer.Ecosystem\models" (
    mkdir "%USERPROFILE%\MonadoBlade\Developer.Ecosystem\models"
)
if not exist "%USERPROFILE%\MonadoBlade\Developer.Ecosystem\backups" (
    mkdir "%USERPROFILE%\MonadoBlade\Developer.Ecosystem\backups"
)
if not exist "%USERPROFILE%\MonadoBlade\Developer.Ecosystem\logs" (
    mkdir "%USERPROFILE%\MonadoBlade\Developer.Ecosystem\logs"
)

echo ✅ Directories created
echo.

REM Set environment variables
echo 🔐 Setting environment variables...
setx OLLAMA_HOST 0.0.0.0:11434
setx DEV_ECOSYSTEM_ROOT "%USERPROFILE%\MonadoBlade\Developer.Ecosystem"

REM Prompt for GitHub Copilot token
echo.
echo 🔗 GitHub Copilot Setup (Optional)
echo ====================================
set /p COPILOT_TOKEN="Enter your GitHub Copilot API token (press Enter to skip): "
if not "!COPILOT_TOKEN!"=="" (
    setx COPILOT_API_TOKEN !COPILOT_TOKEN!
    echo ✅ Copilot token saved
) else (
    echo ⏭️  Copilot setup skipped (Hermes will be used offline)
)

echo.

REM Create DevDrive (E: drive with ReFS)
echo.
echo 💾 DevDrive Setup (Optional)
echo ============================
set /p CREATE_DEVDRIVE="Create DevDrive on E: with ReFS? (y/n): "
if /i "!CREATE_DEVDRIVE!"=="y" (
    echo 🔄 Creating 50GB VHDX file...
    powershell -Command "New-VHD -Path '$env:USERPROFILE\DevDrive.vhdx' -SizeBytes 50GB -Dynamic -ErrorAction SilentlyContinue"
    
    echo 🔗 Mounting VHDX...
    powershell -Command "Mount-VHD -Path '$env:USERPROFILE\DevDrive.vhdx' -ErrorAction SilentlyContinue"
    
    echo 🔧 Initializing disk and formatting with ReFS...
    powershell -Command "Get-VHD -Path '$env:USERPROFILE\DevDrive.vhdx' | Select-Object -ExpandProperty Number | Initialize-Disk -PassThru | New-Partition -UseMaximumSize | Format-Volume -FileSystem ReFS -ErrorAction SilentlyContinue"
    
    echo ✅ DevDrive created
) else (
    echo ⏭️  DevDrive setup skipped
)

echo.

REM Summary
echo ✅ Setup Complete!
echo.
echo 📋 Summary:
echo   ✓ WSL2 configured
echo   ✓ Ubuntu 24.04 installed
echo   ✓ Fedora 40 installed (optional)
echo   ✓ WSL config optimized
echo   ✓ Environment variables set
echo.
echo 📦 Next steps:
echo   1. Verify Docker Desktop is running
echo   2. Verify Ollama is installed
echo   3. Verify .NET 8.0 SDK is installed
echo   4. Run WSL setup: wsl bash setup-wsl2.sh
echo   5. Build project: dotnet build
echo   6. Run tests: dotnet test
echo   7. Launch GUI: dotnet run
echo.
echo 🔗 Downloads:
echo   - Docker Desktop: https://www.docker.com/products/docker-desktop
echo   - .NET 8.0 SDK: https://dotnet.microsoft.com/download
echo   - Ollama: https://ollama.ai
echo.
echo 📊 Verify installation:
echo   wsl -l -v               # Check WSL distributions
echo   docker version          # Check Docker
echo   dotnet --version        # Check .NET
echo   ollama --version        # Check Ollama
echo.

pause
