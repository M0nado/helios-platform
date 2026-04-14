#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Validate builds against AI-recommended best practices
    
.DESCRIPTION
    Analyzes current build configuration and validates it against
    industry best practices and AI recommendations from previous analyses.
    
.PARAMETER ConfigPath
    Path to build configuration file to validate
    
.PARAMETER CheckAreas
    Comma-separated list of areas to validate (performance, security, compatibility, dependencies, all)
    
.PARAMETER StrictMode
    If specified, fails on any warning (not just errors)
    
.PARAMETER DryRun
    If specified, simulates validation without changes
    
.EXAMPLE
    .\validate-with-ai.ps1 -ConfigPath ./build-config.json
    .\validate-with-ai.ps1 -CheckAreas "security,performance" -StrictMode
    
.NOTES
    Author: HELIOS Platform
    Version: 1.0.0
    Requires: PowerShell 7.0+
#>

param(
    [string]$ConfigPath = "./build-config.json",
    [ValidateSet("performance", "security", "compatibility", "dependencies", "caching", "all")]
    [string]$CheckAreas = "all",
    [switch]$StrictMode,
    [switch]$DryRun,
    [switch]$Verbose
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$apiClientPath = Split-Path $PSScriptRoot -Parent | Join-Path -ChildPath "config\api-clients.ps1"
. $apiClientPath

if ($Verbose) {
    [Environment]::SetEnvironmentVariable("DEBUG_MODE", "true")
}

$script:ValidationResults = @{
    passed = 0
    warnings = 0
    errors = 0
    items = @()
}

function Test-PerformanceConfig {
    param([object]$Config)
    
    Write-LogEntry -Level "INFO" -Message "Validating performance configuration"
    
    $checks = @(
        @{
            name = "Parallel Jobs Configuration"
            rule = { $Config.parallelJobs -ge 2 -and $Config.parallelJobs -le 128 }
            message = "Parallel jobs should be between 2 and 128 (current: $($Config.parallelJobs))"
            severity = "error"
        }
        @{
            name = "Cache Enabled"
            rule = { $Config.cacheEnabled -eq $true }
            message = "Caching should be enabled for optimal performance"
            severity = "warning"
        }
        @{
            name = "Build Configuration"
            rule = { $Config.buildConfiguration -in @("Release", "Debug", "ReleaseOptimized") }
            message = "Build configuration should be Release or ReleaseOptimized (current: $($Config.buildConfiguration))"
            severity = "error"
        }
        @{
            name = "Artifact Retention"
            rule = { $Config.artifactRetention -ge 7 -and $Config.artifactRetention -le 365 }
            message = "Artifact retention should be between 7 and 365 days (current: $($Config.artifactRetention))"
            severity = "warning"
        }
    )
    
    foreach ($check in $checks) {
        $result = & $check.rule
        Add-ValidationResult -Name $check.name -Result $result -Message $check.message -Severity $check.severity
    }
}

function Test-SecurityConfig {
    param([object]$Config)
    
    Write-LogEntry -Level "INFO" -Message "Validating security configuration"
    
    $checks = @(
        @{
            name = "Build Targets Specified"
            rule = { $Config.buildTargets.Count -gt 0 }
            message = "At least one build target must be specified"
            severity = "error"
        }
        @{
            name = "Platforms Specified"
            rule = { $Config.platforms.Count -gt 0 }
            message = "At least one platform must be specified"
            severity = "error"
        }
    )
    
    foreach ($check in $checks) {
        $result = & $check.rule
        Add-ValidationResult -Name $check.name -Result $result -Message $check.message -Severity $check.severity
    }
}

function Test-DependencyConfig {
    param([object]$Config)
    
    Write-LogEntry -Level "INFO" -Message "Validating dependency configuration"
    
    if ($Config.dependencies) {
        $checks = @(
            @{
                name = "Dependencies Array"
                rule = { $Config.dependencies -is [array] -or $Config.dependencies -is [System.Collections.ArrayList] }
                message = "Dependencies should be an array"
                severity = "warning"
            }
        )
        
        foreach ($check in $checks) {
            $result = & $check.rule
            Add-ValidationResult -Name $check.name -Result $result -Message $check.message -Severity $check.severity
        }
    }
}

function Test-CompatibilityConfig {
    param([object]$Config)
    
    Write-LogEntry -Level "INFO" -Message "Validating platform compatibility"
    
    $validPlatforms = @("x86", "x64", "ARM64", "ARM")
    $validTargets = @("windows-x64", "linux-x64", "macos-arm64", "macos-x64")
    
    $checks = @(
        @{
            name = "Platform Validity"
            rule = { ($Config.platforms | Where-Object { $_ -notin $validPlatforms }).Count -eq 0 }
            message = "Invalid platforms. Valid: $($validPlatforms -join ', ')"
            severity = "error"
        }
        @{
            name = "Target Validity"
            rule = { ($Config.buildTargets | Where-Object { $_ -notin $validTargets }).Count -eq 0 }
            message = "Invalid build targets. Valid: $($validTargets -join ', ')"
            severity = "error"
        }
    )
    
    foreach ($check in $checks) {
        $result = & $check.rule
        Add-ValidationResult -Name $check.name -Result $result -Message $check.message -Severity $check.severity
    }
}

function Test-CachingConfig {
    param([object]$Config)
    
    Write-LogEntry -Level "INFO" -Message "Validating caching configuration"
    
    if ($Config.cacheEnabled) {
        $checks = @(
            @{
                name = "Cache Strategy Defined"
                rule = { $Config.cacheStrategy -in @("aggressive", "balanced", "conservative") -or -not $Config.cacheStrategy }
                message = "Cache strategy should be aggressive, balanced, or conservative"
                severity = "warning"
            }
            @{
                name = "Cache TTL Valid"
                rule = { -not $Config.cacheTTL -or ($Config.cacheTTL -gt 0 -and $Config.cacheTTL -le 86400) }
                message = "Cache TTL should be between 0 and 86400 seconds"
                severity = "warning"
            }
        )
        
        foreach ($check in $checks) {
            $result = & $check.rule
            Add-ValidationResult -Name $check.name -Result $result -Message $check.message -Severity $check.severity
        }
    }
}

function Add-ValidationResult {
    param(
        [string]$Name,
        [bool]$Result,
        [string]$Message,
        [ValidateSet("error", "warning", "info")]
        [string]$Severity = "info"
    )
    
    $item = @{
        name = $Name
        result = $Result
        message = $Message
        severity = $Severity
        timestamp = Get-Date
    }
    
    $script:ValidationResults.items += $item
    
    if ($Result) {
        $script:ValidationResults.passed++
        $status = "✓ PASS"
        $color = "Green"
    } else {
        if ($Severity -eq "error") {
            $script:ValidationResults.errors++
            $status = "✗ ERROR"
            $color = "Red"
        } else {
            $script:ValidationResults.warnings++
            $status = "⚠ WARNING"
            $color = "Yellow"
        }
    }
    
    Write-Host "$status : $Name" -ForegroundColor $color
    Write-Host "       $Message`n"
}

function Invoke-AIValidation {
    param([object]$Config, [bool]$DryRun = $false)
    
    Write-LogEntry -Level "INFO" -Message "Running AI validation analysis"
    
    $configJson = ConvertTo-Json $Config -Depth 10
    
    $prompt = @"
Validate the following HELIOS Platform build configuration against best practices:

$configJson

Provide a detailed validation report including:
1. Configuration correctness assessment
2. Performance implications
3. Security considerations
4. Recommended improvements
5. Risk assessment
6. Priority actions

Format your response as a structured validation report with sections.
"@
    
    $request = @{
        model = Get-ConfigValue -Key "OPENAI_MODEL" -Default "gpt-4"
        messages = @(
            @{
                role = "system"
                content = "You are a build configuration validator. Provide detailed, actionable validation feedback with clear recommendations."
            }
            @{
                role = "user"
                content = $prompt
            }
        )
        max_tokens = [int](Get-ConfigValue -Key "OPENAI_MAX_TOKENS" -Default "2048")
        temperature = 0.3
    }
    
    try {
        $startTime = Get-Date
        $response = Invoke-OpenAIRequest -Endpoint "/chat/completions" `
                                        -Body $request `
                                        -MaxRetries 3 `
                                        -DryRun $DryRun
        $elapsed = (Get-Date) - $startTime
        
        Log-APICall -Service "chatgpt-pro" `
                   -Action "Validate-Config" `
                   -Status "success" `
                   -Duration $elapsed.TotalMilliseconds `
                   -DryRun $DryRun
        
        return $response
    }
    catch {
        Write-LogEntry -Level "ERROR" -Message "AI validation failed: $($_.Exception.Message)"
        throw $_
    }
}

function Main {
    Write-Host "`n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
    Write-Host "  AI Build Configuration Validator" -ForegroundColor Cyan
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━`n" -ForegroundColor Cyan
    
    try {
        if (-not (Test-Path $ConfigPath)) {
            throw "Configuration file not found: $ConfigPath"
        }
        
        Write-Host "Loading configuration from: $ConfigPath`n" -ForegroundColor Magenta
        $config = Get-Content $ConfigPath | ConvertFrom-Json
        
        # Run validation checks based on area selection
        if ($CheckAreas -in @("all", "performance")) {
            Write-Host "Validating Performance Configuration..." -ForegroundColor Magenta
            Test-PerformanceConfig -Config $config
        }
        
        if ($CheckAreas -in @("all", "security")) {
            Write-Host "Validating Security Configuration..." -ForegroundColor Magenta
            Test-SecurityConfig -Config $config
        }
        
        if ($CheckAreas -in @("all", "compatibility")) {
            Write-Host "Validating Platform Compatibility..." -ForegroundColor Magenta
            Test-CompatibilityConfig -Config $config
        }
        
        if ($CheckAreas -in @("all", "dependencies")) {
            Write-Host "Validating Dependency Configuration..." -ForegroundColor Magenta
            Test-DependencyConfig -Config $config
        }
        
        if ($CheckAreas -in @("all", "caching")) {
            Write-Host "Validating Caching Configuration..." -ForegroundColor Magenta
            Test-CachingConfig -Config $config
        }
        
        # Run AI validation
        Write-Host "Running AI Validation..." -ForegroundColor Magenta
        $aiValidation = Invoke-AIValidation -Config $config -DryRun $DryRun
        
        # Summary
        Write-Host "`n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
        Write-Host "  Validation Summary" -ForegroundColor Cyan
        Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━`n" -ForegroundColor Cyan
        
        Write-Host "Passed:  $($script:ValidationResults.passed)" -ForegroundColor Green
        Write-Host "Warnings: $($script:ValidationResults.warnings)" -ForegroundColor Yellow
        Write-Host "Errors:   $($script:ValidationResults.errors)`n" -ForegroundColor Red
        
        if ($aiValidation.success -or $aiValidation.isDryRun) {
            Write-Host "[AI Validation Report]`n" -ForegroundColor Green
            Write-Host ($aiValidation.content.choices[0].message.content ?? "DRY RUN VALIDATION")
        }
        
        if ($script:ValidationResults.errors -gt 0 -or ($StrictMode -and $script:ValidationResults.warnings -gt 0)) {
            Write-Host "`n✗ Validation failed" -ForegroundColor Red
            exit 1
        } else {
            Write-Host "`n✓ Validation passed" -ForegroundColor Green
        }
    }
    catch {
        Write-Host "`n✗ ERROR: $($_.Exception.Message)`n" -ForegroundColor Red
        exit 1
    }
}

Main
