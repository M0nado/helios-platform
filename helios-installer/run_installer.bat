@echo off
:: HELIOS Quick Start USB Installer - Windows Launcher
:: Requires Administrator privileges

title HELIOS Installer

echo.
echo  ============================================================
echo   HELIOS Quick Start USB Installer - Windows Launcher
echo  ============================================================
echo.

:: Check for Administrator privileges
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo  [ERROR] This installer must be run as Administrator.
    echo  Right-click run_installer.bat and choose "Run as administrator".
    echo.
    pause
    exit /b 1
)

:: Verify Python 3.10+ is available
python --version >nul 2>&1
if %errorlevel% neq 0 (
    echo  [ERROR] Python not found. Install Python 3.10+ from https://python.org
    echo.
    pause
    exit /b 1
)

echo  Installing Python dependencies ...
python -m pip install --upgrade pip --quiet
python -m pip install -r "%~dp0requirements.txt" --quiet
if %errorlevel% neq 0 (
    echo  [WARNING] Some dependencies failed to install. Continuing anyway.
)

echo  Launching HELIOS installer ...
echo.
python "%~dp0main.py" %*

echo.
echo  Installer finished. Press any key to exit.
pause >nul
