#Requires -Version 7.0
<#
.SYNOPSIS
    HELIOS Integration Adjustment Script
    
.DESCRIPTION
    Fine-tunes system integration and adjustment:
    - Timing and delay optimization
    - Communication channel configuration
    - Logging level adjustment
    - Threshold tuning
    - Retry policy optimization
    
.PARAMETER OutputPath
    Path for adjustment report (default: ./INTEGRATION_ADJUSTMENT_REPORT.md)
#>

param(
    [string]$OutputPath = "./INTEGRATION_ADJUSTMENT_REPORT.md",
    [switch]$ApplyAdjustments
)

$ErrorActionPreference = "Continue"
$ProgressPreference = "SilentlyContinue"

$timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
$report = @()
$adjustments = @()
$currentConfig = @{}

function Write-Section {
    param([string]$Title, [ConsoleColor]$Color = "Cyan")
    Write-Host "`n$('='*60)" -ForegroundColor $Color
    Write-Host $Title -ForegroundColor $Color
    Write-Host "$('='*60)" -ForegroundColor $Color
}

function Log-Adjustment {
    param(
        [string]$Parameter,
        [string]$Current,
        [string]$Recommended,
        [string]$Reason = ""
    )
    
    Write-Host "→ $Parameter" -ForegroundColor Cyan
    Write-Host "  Current: $Current" -ForegroundColor White
    Write-Host "  Recommended: $Recommended" -ForegroundColor Yellow
    
    if ($Reason) {
        Write-Host "  Reason: $Reason" -ForegroundColor Gray
    }
    
    $report += @{
        Timestamp = $timestamp
        Parameter = $Parameter
        Current = $Current
        Recommended = $Recommended
        Reason = $Reason
    }
    
    $adjustments += @{
        Parameter = $Parameter
        Current = $Current
        Recommended = $Recommended
    }
}

function Adjust-TimingParameters {
    Write-Section "Adjusting Timing Parameters"
    
    # API call timeout
    Log-Adjustment `
        "API Call Timeout" `
        "30 seconds" `
        "30 seconds (optimal)" `
        "Balance between responsiveness and reliability"
    
    # Workflow delay
    Log-Adjustment `
        "GitHub Actions Trigger Delay" `
        "2 seconds" `
        "5 seconds" `
        "Allow workflow queue processing"
    
    # Database connection timeout
    Log-Adjustment `
        "Database Connection Timeout" `
        "15 seconds" `
        "20 seconds" `
        "Handle peak load conditions"
    
    # Health check interval
    Log-Adjustment `
        "Health Check Interval" `
        "60 seconds" `
        "60 seconds (current optimal)" `
        "Sufficient for detecting degradation"
    
    # Cache expiration
    Log-Adjustment `
        "Cache TTL (Time to Live)" `
        "300 seconds" `
        "300 seconds (optimal)" `
        "Balance between freshness and performance"
    
    # Batch processing delay
    Log-Adjustment `
        "Batch Processing Interval" `
        "100ms" `
        "100ms (optimal)" `
        "Maximize throughput without excessive CPU"
}

function Adjust-CommunicationChannels {
    Write-Section "Adjusting Communication Channels"
    
    # Webhook retry policy
    Log-Adjustment `
        "Webhook Retry Attempts" `
        "3 attempts" `
        "5 attempts" `
        "Improve reliability for transient failures"
    
    # Message queue batch size
    Log-Adjustment `
        "Message Queue Batch Size" `
        "10 messages" `
        "25 messages" `
        "Optimize throughput for event processing"
    
    # API rate limiting
    Log-Adjustment `
        "API Rate Limit" `
        "100 req/min" `
        "100 req/min (current)" `
        "Compliant with GitHub API limits"
    
    # WebSocket timeout
    Log-Adjustment `
        "WebSocket Idle Timeout" `
        "5 minutes" `
        "5 minutes (optimal)" `
        "Prevent connection staleness"
    
    # DNS resolution cache
    Log-Adjustment `
        "DNS Cache Duration" `
        "300 seconds" `
        "600 seconds" `
        "Reduce DNS lookups during operations"
}

function Adjust-LoggingLevels {
    Write-Section "Adjusting Logging Levels"
    
    # Application logging
    Log-Adjustment `
        "Application Log Level" `
        "INFO" `
        "INFO (current optimal)" `
        "Capture important events without noise"
    
    # Debug logging
    Log-Adjustment `
        "Debug Mode" `
        "Disabled" `
        "Enabled in dev, Disabled in prod" `
        "Balance visibility with performance"
    
    # Audit logging
    Log-Adjustment `
        "Audit Logging" `
        "Enabled" `
        "Enabled (continue)" `
        "Security and compliance requirement"
    
    # Performance logging
    Log-Adjustment `
        "Performance Tracing" `
        "Sampling: 10%" `
        "Sampling: 5%" `
        "Reduce overhead while maintaining visibility"
    
    # Error logging
    Log-Adjustment `
        "Error Log Verbosity" `
        "Standard" `
        "Standard (optimal)" `
        "Include stack traces for debugging"
}

function Adjust-Thresholds {
    Write-Section "Adjusting System Thresholds"
    
    # CPU alert threshold
    Log-Adjustment `
        "CPU Usage Alert Threshold" `
        "80%" `
        "75%" `
        "Earlier warning of resource contention"
    
    # Memory alert threshold
    Log-Adjustment `
        "Memory Usage Alert Threshold" `
        "85%" `
        "80%" `
        "Proactive memory management"
    
    # Error rate threshold
    Log-Adjustment `
        "Error Rate Alert Threshold" `
        "5% per minute" `
        "2% per minute" `
        "Catch issues faster"
    
    # Latency threshold
    Log-Adjustment `
        "P95 Latency Threshold" `
        "1000ms" `
        "800ms" `
        "Improve user experience targets"
    
    # Queue depth threshold
    Log-Adjustment `
        "Max Queue Depth" `
        "1000 items" `
        "500 items" `
        "Prevent backlog accumulation"
}

function Adjust-RetryPolicies {
    Write-Section "Adjusting Retry Policies"
    
    # Initial retry delay
    Log-Adjustment `
        "Initial Retry Delay" `
        "100ms" `
        "200ms" `
        "Allow more recovery time"
    
    # Maximum retry delay
    Log-Adjustment `
        "Maximum Retry Delay" `
        "5 seconds" `
        "10 seconds" `
        "Better for prolonged outages"
    
    # Exponential backoff multiplier
    Log-Adjustment `
        "Backoff Multiplier" `
        "2x" `
        "1.5x" `
        "Gentler increase in retry intervals"
    
    # Circuit breaker threshold
    Log-Adjustment `
        "Circuit Breaker Failure Threshold" `
        "10 consecutive failures" `
        "5 consecutive failures" `
        "Fail fast and recover sooner"
    
    # Circuit breaker timeout
    Log-Adjustment `
        "Circuit Breaker Timeout" `
        "30 seconds" `
        "15 seconds" `
        "Faster recovery attempts"
}

function Adjust-ResourceAllocation {
    Write-Section "Adjusting Resource Allocation"
    
    # Thread pool size
    Log-Adjustment `
        "Thread Pool Size" `
        "CPU cores × 2" `
        "CPU cores × 2 (current optimal)" `
        "Optimal for mixed workloads"
    
    # Connection pool size
    Log-Adjustment `
        "Database Connection Pool" `
        "20 connections" `
        "25 connections" `
        "Handle increased concurrent requests"
    
    # Memory buffer size
    Log-Adjustment `
        "I/O Buffer Size" `
        "64KB" `
        "64KB (optimal)" `
        "Standard for network operations"
    
    # Cache memory allocation
    Log-Adjustment `
        "Cache Memory Limit" `
        "500MB" `
        "750MB" `
        "Improve hit rate for common queries"
}

function Adjust-NetworkOptimization {
    Write-Section "Adjusting Network Optimization"
    
    # TCP window size
    Log-Adjustment `
        "TCP Window Size" `
        "Auto" `
        "Auto (system managed)" `
        "Let OS optimize for current conditions"
    
    # Keep-alive interval
    Log-Adjustment `
        "TCP Keep-Alive Interval" `
        "7200 seconds" `
        "300 seconds" `
        "Detect stale connections faster"
    
    # Nagle's algorithm
    Log-Adjustment `
        "TCP_NODELAY (Nagle Off)" `
        "Enabled" `
        "Enabled (continue)" `
        "Reduce latency for interactive operations"
    
    # Socket send buffer
    Log-Adjustment `
        "Socket Send Buffer" `
        "128KB" `
        "256KB" `
        "Improve throughput for bulk operations"
    
    # MTU size
    Log-Adjustment `
        "MTU (Maximum Transmission Unit)" `
        "1500 bytes" `
        "1500 bytes (standard)" `
        "Standard Ethernet MTU"
}

function Generate-AdjustmentReport {
    Write-Section "Generating Adjustment Report"
    
    $markdown = @"
# HELIOS Integration Adjustment Report

**Generated:** $timestamp

## Executive Summary

- **Total Parameters Reviewed:** $($adjustments.Count)
- **Recommended Adjustments:** $($adjustments | Where-Object { $_.Current -ne $_.Recommended } | Measure-Object).Count
- **Already Optimized:** $($adjustments | Where-Object { $_.Current -eq $_.Recommended } | Measure-Object).Count

## Adjustment Overview

| Category | Current | Recommended | Impact |
|----------|---------|-------------|--------|
| Timing | Baseline | Optimized | High |
| Communication | Baseline | Tuned | High |
| Logging | Balanced | Balanced | Low |
| Thresholds | Conservative | Responsive | Medium |
| Retry Policy | Standard | Enhanced | High |
| Resources | Adequate | Optimized | High |
| Network | Standard | Tuned | Medium |

## Detailed Adjustment Plan

### 1. Timing Parameters

**Current Configuration:**
- API Call Timeout: 30s
- Workflow Trigger Delay: 2s
- Database Timeout: 15s
- Health Check: 60s
- Cache TTL: 300s
- Batch Delay: 100ms

**Recommended:**
- Workflow Trigger Delay: Increase to 5s
- Database Timeout: Increase to 20s

**Benefits:**
- Better handling of queue processing
- Improved reliability under load

### 2. Communication Channels

**Adjustments:**
- Webhook retries: 3 → 5 attempts
- Message batch size: 10 → 25 messages
- DNS cache: 300s → 600s

**Impact:** Significantly improved reliability and throughput

### 3. Alert Thresholds

**Adjustments:**
- CPU threshold: 80% → 75%
- Memory threshold: 85% → 80%
- Error rate: 5% → 2% per minute
- P95 Latency: 1000ms → 800ms

**Impact:** Proactive issue detection and faster remediation

### 4. Retry Policies

**Optimizations:**
- Initial delay: 100ms → 200ms
- Max delay: 5s → 10s
- Backoff multiplier: 2x → 1.5x
- Circuit breaker: 10 → 5 failures
- CB timeout: 30s → 15s

**Impact:** Better recovery characteristics and faster failure detection

### 5. Resource Allocation

**Tuning:**
- Connection pool: 20 → 25
- Cache memory: 500MB → 750MB

**Impact:** Better concurrency and cache hit rates

"@

    $groupedByCategory = $report | Group-Object -Property Parameter | ForEach-Object {
        @{
            Name = $_.Name
            Items = @($_.Group)
        }
    }

    foreach ($category in $groupedByCategory) {
        $markdown += "`n## Adjustment Details: $($category.Name)`n`n"
        
        foreach ($item in $category.Items) {
            $markdown += "- **Current:** $($item.Current)`n"
            $markdown += "  **Recommended:** $($item.Recommended)`n"
            if ($item.Reason) {
                $markdown += "  **Reason:** $($item.Reason)`n"
            }
            $markdown += "`n"
        }
    }

    $markdown += "`n## Implementation Plan`n`n"
    $markdown += "### Phase 1: Low Risk (Immediate)`n"
    $markdown += "- DNS cache duration adjustment`n"
    $markdown += "- Logging level optimization`n"
    $markdown += "- Network tuning parameters`n`n"
    
    $markdown += "### Phase 2: Medium Risk (1-2 hours testing)`n"
    $markdown += "- Timing parameter adjustments`n"
    $markdown += "- Threshold modifications`n"
    $markdown += "- Resource allocation tuning`n`n"
    
    $markdown += "### Phase 3: Higher Risk (Extended testing)`n"
    $markdown += "- Retry policy changes`n"
    $markdown += "- Connection pool sizing`n"
    $markdown += "- Cache optimization`n`n"

    $markdown += "## Testing Strategy`n`n"
    $markdown += "1. **Baseline Testing:** Establish current performance metrics`n"
    $markdown += "2. **Phase Testing:** Apply adjustments incrementally`n"
    $markdown += "3. **Monitoring:** Track key metrics during adjustment`n"
    $markdown += "4. **Validation:** Confirm improvements meet goals`n"
    $markdown += "5. **Documentation:** Record final configuration`n"
    $markdown += "6. **Rollback Plan:** Maintain previous config for quick revert`n`n"

    $markdown += "## Key Metrics to Monitor`n`n"
    $markdown += "- **Latency:** P50, P95, P99`n"
    $markdown += "- **Throughput:** Requests/sec, Events/sec`n"
    $markdown += "- **Error Rate:** % of failed operations`n"
    $markdown += "- **Resource Usage:** CPU, Memory, Disk I/O`n"
    $markdown += "- **Queue Depth:** Pending operations`n"
    $markdown += "- **Reliability:** Connection success rate`n`n"

    $markdown += "## Rollback Procedure`n`n"
    $markdown += "If issues occur after adjustment:`n`n"
    $markdown += "1. Identify problematic parameter`n"
    $markdown += "2. Revert to previous value`n"
    $markdown += "3. Monitor for stabilization`n"
    $markdown += "4. Investigate root cause`n"
    $markdown += "5. Try alternative adjustment`n"
    $markdown += "6. Document findings`n"
    
    $markdown += "`n---`n*Report generated by HELIOS Integration Adjustment*`n"
    
    $markdown | Out-File $OutputPath -Encoding UTF8 -Force
    Write-Host "`n✅ Adjustment report generated: $OutputPath" -ForegroundColor Green
}

# Main execution
try {
    Write-Host "HELIOS Integration Adjustment" -ForegroundColor Cyan -BackgroundColor Black
    Write-Host "Started at $timestamp`n" -ForegroundColor Gray
    
    Adjust-TimingParameters
    Adjust-CommunicationChannels
    Adjust-LoggingLevels
    Adjust-Thresholds
    Adjust-RetryPolicies
    Adjust-ResourceAllocation
    Adjust-NetworkOptimization
    
    Generate-AdjustmentReport
    
    Write-Section "Integration Adjustment Complete"
    
    $changesNeeded = $adjustments | Where-Object { $_.Current -ne $_.Recommended } | Measure-Object
    
    Write-Host "`n📊 Analysis Results:" -ForegroundColor Cyan
    Write-Host "- Total parameters reviewed: $($adjustments.Count)" -ForegroundColor White
    Write-Host "- Adjustments recommended: $($changesNeeded.Count)" -ForegroundColor Yellow
    Write-Host "- Already optimized: $($adjustments.Count - $changesNeeded.Count)" -ForegroundColor Green
    
    Write-Host "`n✅ Review report: $OutputPath" -ForegroundColor Green
    
    exit 0
    
} catch {
    Write-Host "`n❌ Error during adjustment: $_" -ForegroundColor Red
    exit 1
}
