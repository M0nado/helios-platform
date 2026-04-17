#=============================================================================
# HELIOS Platform - Optimization Scripts & Automation
# Purpose: Automated performance profiling, metrics collection, and reporting
# Version: 1.0
#=============================================================================

param(
    [ValidateSet('baseline', 'analyze', 'optimize', 'verify', 'report')]
    [string]$Profile = 'baseline',
    
    [switch]$Detailed,
    [switch]$GenerateReport
)

$ErrorActionPreference = "Stop"
$scriptsPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectPath = Join-Path $scriptsPath "../../src/HELIOS.Platform"

# ============================================================================
# METRICS DATA STRUCTURES
# ============================================================================

class PerformanceMetrics {
    [DateTime]$Timestamp = (Get-Date)
    [double]$BuildTimeSeconds
    [double]$TestTimeSeconds
    [double]$DeploymentTimeSeconds
    [double]$MemoryUsageMB
    [double]$CpuUtilizationPercent
    [int]$ArtifactSizeMB
    [double]$CacheHitRate
    [int]$ErrorCount
}

class OptimizationResult {
    [string]$Name
    [double]$BaselineSeconds
    [double]$OptimizedSeconds
    [double]$ImprovementPercent
    [string]$Status
    [datetime]$Timestamp
}

# ============================================================================
# LOGGING & OUTPUT
# ============================================================================

function Write-Header {
    param([string]$Text, [ConsoleColor]$Color = [ConsoleColor]::Cyan)
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor $Color
    Write-Host "  $Text" -ForegroundColor Green
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor $Color
}

function Write-Status {
    param([string]$Message, [ConsoleColor]$Status = 'Green')
    $symbol = if ($Status -eq 'Green') { '✓' } else { '✗' }
    Write-Host "  $symbol $Message" -ForegroundColor $Status
}

# ============================================================================
# BUILD PERFORMANCE MEASUREMENT
# ============================================================================

function Measure-BuildPerformance {
    Write-Header "Measuring Build Performance"
    
    $configurations = @('Debug', 'Release')
    $results = @()
    
    foreach ($config in $configurations) {
        Write-Host "`nBuilding with $config configuration..."
        
        $timer = [System.Diagnostics.Stopwatch]::StartNew()
        
        try {
            # Clean build
            dotnet clean --configuration $config | Out-Null
            
            # Full build
            dotnet build --configuration $config `
                        --verbosity quiet `
                        /p:EnforceCodeStyleInBuild=false | Out-Null
            
            $timer.Stop()
            
            $result = [PerformanceMetrics]@{
                BuildTimeSeconds = $timer.Elapsed.TotalSeconds
            }
            
            $results += $result
            
            Write-Status "Build completed in $([math]::Round($timer.Elapsed.TotalSeconds, 2)) seconds"
        }
        catch {
            Write-Status "Build failed: $_" -Status Red
        }
    }
    
    return $results
}

function Measure-IncrementalBuild {
    Write-Header "Measuring Incremental Build Performance"
    
    # Make a small change to trigger incremental build
    $testFile = Join-Path $projectPath "HeliosDeployment.cs"
    
    if (-not (Test-Path $testFile)) {
        Write-Status "Test file not found" -Status Red
        return $null
    }
    
    $timer = [System.Diagnostics.Stopwatch]::StartNew()
    
    try {
        dotnet build --configuration Release `
                    --no-restore `
                    --verbosity quiet | Out-Null
        
        $timer.Stop()
        
        Write-Status "Incremental build completed in $([math]::Round($timer.Elapsed.TotalSeconds, 2)) seconds"
        
        return $timer.Elapsed.TotalSeconds
    }
    catch {
        Write-Status "Incremental build failed: $_" -Status Red
        return $null
    }
}

# ============================================================================
# TEST PERFORMANCE MEASUREMENT
# ============================================================================

function Measure-TestPerformance {
    Write-Header "Measuring Test Performance"
    
    $timer = [System.Diagnostics.Stopwatch]::StartNew()
    
    try {
        $output = dotnet test --configuration Release `
                             --no-build `
                             --logger "console;verbosity=minimal" 2>&1
        
        $timer.Stop()
        
        # Parse test results
        $passedMatch = $output | Select-String "(\d+) passed"
        $failedMatch = $output | Select-String "(\d+) failed"
        
        $passed = if ($passedMatch) { [int]$passedMatch.Matches.Groups[1].Value } else { 0 }
        $failed = if ($failedMatch) { [int]$failedMatch.Matches.Groups[1].Value } else { 0 }
        
        Write-Status "Tests completed: $passed passed, $failed failed"
        Write-Status "Total test time: $([math]::Round($timer.Elapsed.TotalSeconds, 2)) seconds"
        
        return @{
            TimeSeconds = $timer.Elapsed.TotalSeconds
            Passed = $passed
            Failed = $failed
            PassRate = if (($passed + $failed) -gt 0) { $passed / ($passed + $failed) * 100 } else { 0 }
        }
    }
    catch {
        Write-Status "Test execution failed: $_" -Status Red
        return $null
    }
}

# ============================================================================
# RESOURCE MONITORING
# ============================================================================

function Get-CpuUtilization {
    try {
        $cpuMetrics = Get-Counter '\Processor(_Total)\% Processor Time' `
            -SampleInterval 1 `
            -MaxSamples 10 | 
            Select-Object -ExpandProperty CounterSamples | 
            Measure-Object -Property CookedValue -Average
        
        return [math]::Round($cpuMetrics.Average, 2)
    }
    catch {
        Write-Status "Failed to get CPU metrics: $_" -Status Red
        return 0
    }
}

function Get-MemoryUtilization {
    try {
        $totalMemory = [math]::Round((Get-CimInstance Win32_ComputerSystem).TotalPhysicalMemory / 1MB, 2)
        $freeMemory = [math]::Round((Get-CimInstance Win32_OperatingSystem).FreePhysicalMemory, 2)
        $usedMemory = $totalMemory - $freeMemory
        $utilization = ($usedMemory / $totalMemory) * 100
        
        return [math]::Round($utilization, 2)
    }
    catch {
        Write-Status "Failed to get memory metrics: $_" -Status Red
        return 0
    }
}

function Get-DiskIOMetrics {
    try {
        $diskMetrics = Get-Counter '\PhysicalDisk(_Total)\Disk Read Bytes/sec' `
            -SampleInterval 1 `
            -MaxSamples 5 | 
            Select-Object -ExpandProperty CounterSamples | 
            Measure-Object -Property CookedValue -Average
        
        return [math]::Round($diskMetrics.Average / 1MB, 2)  # MB/sec
    }
    catch {
        Write-Status "Failed to get disk metrics: $_" -Status Red
        return 0
    }
}

function Measure-SystemResources {
    Write-Header "Measuring System Resources"
    
    $cpu = Get-CpuUtilization
    $memory = Get-MemoryUtilization
    $diskIO = Get-DiskIOMetrics
    
    Write-Status "CPU Utilization: $cpu%"
    Write-Status "Memory Utilization: $memory%"
    Write-Status "Disk I/O: $diskIO MB/sec"
    
    return @{
        CPU = $cpu
        Memory = $memory
        DiskIO = $diskIO
    }
}

# ============================================================================
# ARTIFACT SIZE ANALYSIS
# ============================================================================

function Measure-ArtifactSize {
    Write-Header "Measuring Artifact Sizes"
    
    $buildDir = Join-Path $projectPath "bin/Release"
    
    if (Test-Path $buildDir) {
        $totalSize = (Get-ChildItem -Path $buildDir -Recurse | Measure-Object -Sum -Property Length).Sum
        $totalSizeMB = [math]::Round($totalSize / 1MB, 2)
        
        Write-Status "Total build artifacts: $totalSizeMB MB"
        
        # List largest files
        $largestFiles = Get-ChildItem -Path $buildDir -Recurse -File | 
            Sort-Object -Property Length -Descending | 
            Select-Object -First 5
        
        if ($Detailed) {
            Write-Host "`nLargest files:"
            $largestFiles | ForEach-Object {
                $sizeMB = [math]::Round($_.Length / 1MB, 2)
                Write-Host "  $($_.Name): $sizeMB MB"
            }
        }
        
        return $totalSizeMB
    }
    else {
        Write-Status "Build directory not found" -Status Red
        return 0
    }
}

# ============================================================================
# CACHE EFFECTIVENESS
# ============================================================================

function Measure-CacheEffectiveness {
    Write-Header "Measuring Cache Effectiveness"
    
    # First build (cold cache)
    Write-Host "Running cold cache build..."
    $timer1 = [System.Diagnostics.Stopwatch]::StartNew()
    
    dotnet clean | Out-Null
    dotnet build --configuration Release --verbosity quiet | Out-Null
    
    $timer1.Stop()
    $coldBuildTime = $timer1.Elapsed.TotalSeconds
    
    # Second build (warm cache)
    Write-Host "Running warm cache build..."
    $timer2 = [System.Diagnostics.Stopwatch]::StartNew()
    
    dotnet build --configuration Release --verbosity quiet --no-restore | Out-Null
    
    $timer2.Stop()
    $warmBuildTime = $timer2.Elapsed.TotalSeconds
    
    $improvement = (($coldBuildTime - $warmBuildTime) / $coldBuildTime) * 100
    
    Write-Status "Cold cache build: $([math]::Round($coldBuildTime, 2))s"
    Write-Status "Warm cache build: $([math]::Round($warmBuildTime, 2))s"
    Write-Status "Cache improvement: $([math]::Round($improvement, 1))%"
    
    return @{
        ColdTime = $coldBuildTime
        WarmTime = $warmBuildTime
        Improvement = $improvement
        CacheHitRate = 100 - $improvement
    }
}

# ============================================================================
# COMPREHENSIVE BASELINE COLLECTION
# ============================================================================

function Invoke-BaselineCollection {
    Write-Header "HELIOS Platform - Baseline Performance Collection" Green
    
    $baseline = @{}
    
    # Build metrics
    Write-Host "`n[1/5] Build Performance..."
    $baseline.Build = Measure-BuildPerformance
    
    # Incremental build
    Write-Host "`n[2/5] Incremental Build..."
    $baseline.IncrementalBuild = Measure-IncrementalBuild
    
    # Test metrics
    Write-Host "`n[3/5] Test Performance..."
    $baseline.Tests = Measure-TestPerformance
    
    # Resource metrics
    Write-Host "`n[4/5] System Resources..."
    $baseline.Resources = Measure-SystemResources
    
    # Artifact analysis
    Write-Host "`n[5/5] Artifact Analysis..."
    $baseline.ArtifactSize = Measure-ArtifactSize
    
    # Cache effectiveness
    Write-Host "`n[6/6] Cache Effectiveness..."
    $baseline.Cache = Measure-CacheEffectiveness
    
    return $baseline
}

# ============================================================================
# OPTIMIZATION RECOMMENDATIONS
# ============================================================================

function Get-OptimizationRecommendations {
    param([hashtable]$Baseline)
    
    Write-Header "Optimization Recommendations"
    
    $recommendations = @()
    
    # Build time recommendations
    if ($Baseline.Build[0].BuildTimeSeconds -gt 600) {
        $recommendations += "Enable parallel compilation in project file"
        $recommendations += "Implement NuGet package caching"
        $recommendations += "Use assembly trimming to reduce size"
    }
    
    # Test time recommendations
    if ($Baseline.Tests.TimeSeconds -gt 300) {
        $recommendations += "Parallelize unit tests"
        $recommendations += "Separate unit and integration tests"
        $recommendations += "Skip E2E tests in CI pipeline"
    }
    
    # Resource recommendations
    if ($Baseline.Resources.CPU -lt 50) {
        $recommendations += "Enable more parallel compilation"
        $recommendations += "Increase parallelization in tests"
    }
    
    if ($Baseline.Resources.Memory -gt 80) {
        $recommendations += "Configure garbage collection tuning"
        $recommendations += "Implement memory pooling"
    }
    
    # Artifact recommendations
    if ($Baseline.ArtifactSize -gt 100) {
        $recommendations += "Enable assembly trimming"
        $recommendations += "Use Ready-to-Run compilation"
        $recommendations += "Compress artifacts for distribution"
    }
    
    # Cache recommendations
    if ($Baseline.Cache.Improvement -lt 20) {
        $recommendations += "Setup GitHub Actions caching"
        $recommendations += "Configure NuGet cache"
        $recommendations += "Use build output caching"
    }
    
    if ($recommendations.Count -eq 0) {
        Write-Status "No major recommendations - system is well-optimized"
    }
    else {
        Write-Host "`nRecommended Actions:"
        $recommendations | ForEach-Object { Write-Host "  • $_" -ForegroundColor Yellow }
    }
    
    return $recommendations
}

# ============================================================================
# REPORT GENERATION
# ============================================================================

function New-OptimizationReport {
    param([hashtable]$Baseline)
    
    $reportPath = Join-Path $scriptsPath "optimization_report_$(Get-Date -Format 'yyyyMMdd_HHmmss').txt"
    
    $report = @"
╔═══════════════════════════════════════════════════════════════════════════╗
║         HELIOS Platform - Performance Optimization Report                 ║
║                        Generated: $(Get-Date)                    ║
╚═══════════════════════════════════════════════════════════════════════════╝

═══════════════════════════════════════════════════════════════════════════
BUILD PERFORMANCE
═══════════════════════════════════════════════════════════════════════════

  Debug Build Time:          $([math]::Round($Baseline.Build[0].BuildTimeSeconds, 2))s
  Release Build Time:        $([math]::Round($Baseline.Build[1].BuildTimeSeconds, 2))s
  Incremental Build Time:    $([math]::Round($Baseline.IncrementalBuild, 2))s
  
═══════════════════════════════════════════════════════════════════════════
TEST PERFORMANCE
═══════════════════════════════════════════════════════════════════════════

  Total Test Time:           $([math]::Round($Baseline.Tests.TimeSeconds, 2))s
  Tests Passed:              $($Baseline.Tests.Passed)
  Tests Failed:              $($Baseline.Tests.Failed)
  Pass Rate:                 $([math]::Round($Baseline.Tests.PassRate, 1))%

═══════════════════════════════════════════════════════════════════════════
SYSTEM RESOURCES
═══════════════════════════════════════════════════════════════════════════

  CPU Utilization:           $($Baseline.Resources.CPU)%
  Memory Utilization:        $($Baseline.Resources.Memory)%
  Disk I/O:                  $($Baseline.Resources.DiskIO) MB/sec

═══════════════════════════════════════════════════════════════════════════
ARTIFACTS & CACHING
═══════════════════════════════════════════════════════════════════════════

  Total Artifact Size:       $($Baseline.ArtifactSize) MB
  Cold Cache Build:          $([math]::Round($Baseline.Cache.ColdTime, 2))s
  Warm Cache Build:          $([math]::Round($Baseline.Cache.WarmTime, 2))s
  Cache Effectiveness:       $([math]::Round($Baseline.Cache.Improvement, 1))%

═══════════════════════════════════════════════════════════════════════════
OPTIMIZATION TARGETS
═══════════════════════════════════════════════════════════════════════════

  Target Build Time:         ≤ 10 minutes (Improvement: 45-50%)
  Target Test Time:          ≤ 4 minutes (Improvement: 45-50%)
  Target Cache Hit Rate:     ≥ 85% (Improvement: +60%)
  Target Artifact Size:      ≤ 50 MB (Improvement: 40-50%)

═══════════════════════════════════════════════════════════════════════════
NEXT STEPS
═══════════════════════════════════════════════════════════════════════════

1. Implement GitHub Actions caching (2-3 hours)
2. Enable parallel compilation (1-2 hours)
3. Setup assembly trimming (2-3 hours)
4. Parallelize test execution (2-3 hours)
5. Monitor and measure improvements

Expected Timeline: 2-3 weeks for 50% improvement

═══════════════════════════════════════════════════════════════════════════
"@
    
    $report | Out-File -FilePath $reportPath -Encoding UTF8
    Write-Status "Report saved to: $reportPath"
    
    return $reportPath
}

# ============================================================================
# MAIN EXECUTION
# ============================================================================

function Main {
    switch ($Profile) {
        'baseline' {
            $baseline = Invoke-BaselineCollection
            Get-OptimizationRecommendations -Baseline $baseline
            
            if ($GenerateReport) {
                New-OptimizationReport -Baseline $baseline
            }
        }
        
        'analyze' {
            Write-Header "Analyzing Current Performance"
            $baseline = Invoke-BaselineCollection
            Get-OptimizationRecommendations -Baseline $baseline
        }
        
        'optimize' {
            Write-Header "Optimization Implementation (TODO)"
            Write-Status "Run optimization procedures from docs/optimization/"
        }
        
        'verify' {
            Write-Header "Verifying Optimization Results"
            $baseline = Invoke-BaselineCollection
            Write-Status "Compare with baseline metrics"
        }
        
        'report' {
            Write-Header "Generating Optimization Report"
            $baseline = Invoke-BaselineCollection
            New-OptimizationReport -Baseline $baseline
        }
    }
}

# Execute main function
Main

Write-Host "`n✓ Optimization analysis completed" -ForegroundColor Green
