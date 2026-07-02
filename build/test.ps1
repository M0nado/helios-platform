#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Runs tests for the MonadoBlade solution.
.DESCRIPTION
    Executes all unit, integration, and optionally performance tests.
.PARAMETER TestType
    The type of tests to run: All, Unit, Integration, or Performance (default: All)
.PARAMETER Coverage
    If specified, generates code coverage report
.PARAMETER Verbose
    If specified, shows detailed test output
#>

param(
    [ValidateSet("All", "Unit", "Integration", "Performance")]
    [string]$TestType = "All",
    [switch]$Coverage,
    [switch]$Verbose
)

$ErrorActionPreference = "Stop"
$testLog = "test-$(Get-Date -Format 'yyyyMMdd-HHmmss').log"
$coverageDir = "./coverage"

Write-Host "🧪 MonadoBlade Test Script" -ForegroundColor Cyan
Write-Host "Test Type: $TestType"
Write-Host "Coverage: $Coverage"
Write-Host ""

function Run-Tests {
    param(
        [string]$ProjectPath,
        [string]$ProjectName
    )

    Write-Host "Running $ProjectName tests..." -ForegroundColor Yellow

    $testArgs = @($ProjectPath, "--configuration", "Debug", "--no-build", "--logger:console;verbosity=normal")

    if ($Coverage) {
        $testArgs += "/p:CollectCoverage=true"
        $testArgs += "/p:CoverageFormat=opencover"
        $testArgs += "/p:Exclude=`"[xunit.*]*`""
    }

    if ($Verbose) {
        $testArgs += "--verbosity", "normal"
    }

    dotnet test @testArgs 2>&1 | Tee-Object -FilePath $testLog -Append
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ $ProjectName failed!" -ForegroundColor Red
        return $false
    }
    
    Write-Host "✅ $ProjectName passed" -ForegroundColor Green
    return $true
}

# Build first
Write-Host "🏗️  Building solution..." -ForegroundColor Yellow
dotnet build MonadoBlade.sln --configuration Debug 2>&1 | Tee-Object -FilePath $testLog -Append
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Build failed!" -ForegroundColor Red
    exit 1
}

$allPassed = $true

if ($TestType -in "All", "Unit") {
    $allPassed = (Run-Tests "tests/MonadoBlade.Tests.Unit/MonadoBlade.Tests.Unit.csproj" "Unit Tests") -and $allPassed
}

if ($TestType -in "All", "Integration") {
    $allPassed = (Run-Tests "tests/MonadoBlade.Tests.Integration/MonadoBlade.Tests.Integration.csproj" "Integration Tests") -and $allPassed
}

if ($TestType -in "All", "Performance") {
    Write-Host "Running Performance tests..." -ForegroundColor Yellow
    dotnet test "tests/MonadoBlade.Tests.Performance/MonadoBlade.Tests.Performance.csproj" --configuration Debug --no-build 2>&1 | Tee-Object -FilePath $testLog -Append
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Performance Tests passed" -ForegroundColor Green
    } else {
        Write-Host "❌ Performance Tests failed!" -ForegroundColor Red
        $allPassed = $false
    }
}

Write-Host ""
if ($allPassed) {
    Write-Host "✅ All tests passed!" -ForegroundColor Green
} else {
    Write-Host "❌ Some tests failed!" -ForegroundColor Red
    Write-Host "📋 See $testLog for details" -ForegroundColor Gray
    exit 1
}

if ($Coverage) {
    Write-Host ""
    Write-Host "📊 Coverage reports available in $coverageDir" -ForegroundColor Cyan
}

Write-Host "📋 Log saved to: $testLog" -ForegroundColor Gray
