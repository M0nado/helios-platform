@echo off
REM MonadoBlade Performance Benchmark Runner
REM Automated script to run benchmarks and generate comprehensive reports

setlocal enabledelayedexpansion

REM Configuration
set DOTNET_VERSION=8.0
set BUILD_CONFIG=Release
set SCRIPT_DIR=%~dp0
set PROJECT_DIR=%SCRIPT_DIR%
set RESULTS_DIR=%PROJECT_DIR%BenchmarkResults
for /f "tokens=2-4 delims=/ " %%a in ('date /t') do (set mydate=%%c-%%a-%%b)
for /f "tokens=1-2 delims=/:" %%a in ('time /t') do (set mytime=%%a-%%b)
set TIMESTAMP=%mydate%_%mytime%
set RESULTS_TIMESTAMP_DIR=%RESULTS_DIR%\%TIMESTAMP%

echo.
echo ╔════════════════════════════════════════════════════════════╗
echo ║     MonadoBlade Performance Benchmarking Suite             ║
echo ╚════════════════════════════════════════════════════════════╝
echo.

REM Check .NET is installed
echo Checking .NET installation...
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo ERROR: .NET SDK not found. Please install .NET %DOTNET_VERSION%
    exit /b 1
)

for /f "tokens=*" %%a in ('dotnet --version') do set DOTNET_ACTUAL_VERSION=%%a
echo [OK] .NET %DOTNET_ACTUAL_VERSION% found
echo.

REM Create results directory
echo Setting up results directory...
if not exist "%RESULTS_TIMESTAMP_DIR%" mkdir "%RESULTS_TIMESTAMP_DIR%"
echo [OK] Results will be saved to: %RESULTS_TIMESTAMP_DIR%
echo.

REM Build project
echo Building project...
cd /d "%PROJECT_DIR%"
dotnet build -c %BUILD_CONFIG% >nul 2>&1
if errorlevel 1 (
    echo ERROR: Build failed
    exit /b 1
)
echo [OK] Build successful
echo.

REM Run benchmarks
echo Running benchmarks...
echo Start time: %date% %time%
echo.

dotnet run -c %BUILD_CONFIG% --artifacts "%RESULTS_TIMESTAMP_DIR%" 2>&1

echo.
echo End time: %date% %time%
echo.

REM Generate summary
echo Generating summary...

if exist "%RESULTS_TIMESTAMP_DIR%\BENCHMARK_SUMMARY.txt" (
    echo [OK] Summary report generated
    echo.
    type "%RESULTS_TIMESTAMP_DIR%\BENCHMARK_SUMMARY.txt" | more +0
    echo.
)

REM List generated files
echo Generated files:
dir "%RESULTS_TIMESTAMP_DIR%" /b /s
echo.

REM Success message
echo ╔════════════════════════════════════════════════════════════╗
echo ║             Benchmarking Complete! [OK]                    ║
echo ╚════════════════════════════════════════════════════════════╝
echo.
echo Results saved to: %RESULTS_TIMESTAMP_DIR%
echo.
echo Next steps:
echo   1. Open BenchmarkReport.html in your browser
echo   2. Review performance metrics
echo   3. Check for regressions against baseline
echo.

endlocal
exit /b 0
