#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Initialize ChatGPT Pro connection and send build configurations
    
.DESCRIPTION
    Establishes connection with OpenAI ChatGPT Pro API and sends HELIOS platform
    build configurations for context. Validates connection and logs all operations.
    
.PARAMETER DryRun
    If specified, simulates operations without making actual API calls
    
.PARAMETER Verbose
    Enable verbose logging output
    
.EXAMPLE
    .\sync-with-chatgpt.ps1 -DryRun
    .\sync-with-chatgpt.ps1 -Verbose
    
.NOTES
    Author: HELIOS Platform
    Version: 1.0.0
    Requires: PowerShell 7.0+
#>

param(
    [switch]$DryRun,
    [switch]$Verbose
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$apiClientPath = Split-Path $PSScriptRoot -Parent | Join-Path -ChildPath "config\api-clients.ps1"
if (-not (Test-Path $apiClientPath)) {
    throw "API client not found at $apiClientPath"
}
. $apiClientPath

if ($Verbose) {
    [Environment]::SetEnvironmentVariable("DEBUG_MODE", "true")
}

$HELIOS_ARCHITECTURE = @{
    version = "1.0.0"
    components = @("build-pipeline", "artifact-manager", "deployment-controller", "monitoring-system", "security-layer", "api-gateway")
    buildTargets = @("windows-x64", "linux-x64", "macos-arm64")
    technologies = @{
        buildSystem = "MSBuild"
        caching = "NuGet"
        testing = "xUnit"
        containerization = "Docker"
        orchestration = "Kubernetes"
    }
}

$SYSTEM_PROMPT = @"
You are an AI assistant specialized in build optimization and software architecture analysis for the HELIOS platform.

HELIOS Architecture Overview:
- Version: $($HELIOS_ARCHITECTURE.version)
- Components: $($HELIOS_ARCHITECTURE.components -join ", ")
- Build Targets: $($HELIOS_ARCHITECTURE.buildTargets -join ", ")
- Technology Stack: $($HELIOS_ARCHITECTURE.technologies.keys -join ", ")

Your responsibilities:
1. Analyze build configurations for optimization opportunities
2. Suggest best practices for cross-platform builds
3. Recommend performance improvements
4. Identify potential security issues
5. Provide guidance on dependency management
"@

function Test-APIConnection {
    param([bool]$DryRun = $false)
    
    Write-LogEntry -Level "INFO" -Message "Testing OpenAI API connection" -Context @{ dryRun = $DryRun }
    
    try {
        $testRequest = @{
            model = Get-ConfigValue -Key "OPENAI_MODEL" -Default "gpt-4"
            messages = @(@{ role = "system"; content = "Connection test." })
            max_tokens = 10
            temperature = 0
        }
        
        $response = Invoke-OpenAIRequest -Endpoint "/chat/completions" -Body $testRequest -DryRun $DryRun
        
        if ($response.success -or $response.isDryRun) {
            Write-LogEntry -Level "INFO" -Message "API connection verified"
            return $true
        }
        return $false
    }
    catch {
        Write-LogEntry -Level "ERROR" -Message "Connection test error: $($_.Exception.Message)"
        return $false
    }
}

function Load-BuildConfiguration {
    $buildConfigPath = Get-ConfigValue -Key "BUILD_CONFIG_PATH" -Default "./build-config.json"
    
    if (Test-Path $buildConfigPath) {
        return Get-Content $buildConfigPath | ConvertFrom-Json
    }
    
    return @{
        projectName = "HELIOS Platform"
        version = "1.0.0"
        buildConfiguration = "Release"
        platforms = @("x64", "ARM64")
        buildTargets = $HELIOS_ARCHITECTURE.buildTargets
        parallelJobs = 4
        cacheEnabled = $true
        artifactRetention = 30
    }
}

function Initialize-ChatGPTContext {
    param([object]$BuildConfig, [bool]$DryRun = $false)
    
    Write-LogEntry -Level "INFO" -Message "Initializing ChatGPT context"
    
    $contextMessage = @"
HELIOS Platform Build Configuration:
- Project: $($BuildConfig.projectName)
- Version: $($BuildConfig.version)
- Platforms: $($BuildConfig.platforms -join ", ")
- Parallel Jobs: $($BuildConfig.parallelJobs)

Ready to optimize your builds. What needs analysis?
"@
    
    $initRequest = @{
        model = Get-ConfigValue -Key "OPENAI_MODEL" -Default "gpt-4"
        messages = @(
            @{ role = "system"; content = $SYSTEM_PROMPT }
            @{ role = "user"; content = $contextMessage }
        )
        max_tokens = [int](Get-ConfigValue -Key "OPENAI_MAX_TOKENS" -Default "2048")
        temperature = [float](Get-ConfigValue -Key "OPENAI_TEMPERATURE" -Default "0.7")
    }
    
    $startTime = Get-Date
    $response = Invoke-OpenAIRequest -Endpoint "/chat/completions" -Body $initRequest -MaxRetries 3 -DryRun $DryRun
    $elapsed = (Get-Date) - $startTime
    
    Log-APICall -Service "chatgpt-pro" -Action "Initialize-Context" -Status "success" `
               -Duration $elapsed.TotalMilliseconds -DryRun $DryRun
    
    if ($response.success -or $response.isDryRun) {
        Write-Host "`n[ChatGPT Response]`n" -ForegroundColor Cyan
        Write-Host ($response.content.choices[0].message.content ?? "DRY RUN RESPONSE")
    }
    
    return $response
}

function Send-BuildConfigForAnalysis {
    param([object]$BuildConfig, [bool]$DryRun = $false)
    
    Write-LogEntry -Level "INFO" -Message "Analyzing build configuration"
    
    $analysisPrompt = @"
Analyze our HELIOS build configuration and provide:
1. **Optimization Opportunities**: Improve build speed and resource usage
2. **Best Practices**: Industry standard recommendations
3. **Dependency Analysis**: Check for vulnerabilities
4. **Parallelization**: Optimize for $($BuildConfig.parallelJobs) parallel jobs
5. **Performance Metrics**: Key metrics to track

Current Configuration:
$(ConvertTo-Json $BuildConfig -Depth 5)
"@
    
    $analysisRequest = @{
        model = Get-ConfigValue -Key "OPENAI_MODEL" -Default "gpt-4"
        messages = @(
            @{ role = "system"; content = $SYSTEM_PROMPT }
            @{ role = "user"; content = $analysisPrompt }
        )
        max_tokens = [int](Get-ConfigValue -Key "OPENAI_MAX_TOKENS" -Default "4096")
        temperature = [float](Get-ConfigValue -Key "OPENAI_TEMPERATURE" -Default "0.7")
    }
    
    $startTime = Get-Date
    $response = Invoke-OpenAIRequest -Endpoint "/chat/completions" -Body $analysisRequest -MaxRetries 3 -DryRun $DryRun
    $elapsed = (Get-Date) - $startTime
    
    Log-APICall -Service "chatgpt-pro" -Action "Analyze-BuildConfig" -Status "success" `
               -Duration $elapsed.TotalMilliseconds -DryRun $DryRun
    
    if ($response.success -or $response.isDryRun) {
        Write-Host "`n[Build Configuration Analysis]`n" -ForegroundColor Green
        Write-Host ($response.content.choices[0].message.content ?? "DRY RUN RESPONSE")
    }
    
    return $response
}

function Main {
    Write-Host "`n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
    Write-Host "  ChatGPT Pro Synchronization - HELIOS Platform" -ForegroundColor Cyan
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━`n" -ForegroundColor Cyan
    
    if ($DryRun) {
        Write-Host "[DRY RUN MODE] No actual API calls will be made`n" -ForegroundColor Yellow
    }
    
    try {
        Write-Host "Phase 1: Testing API Connection..." -ForegroundColor Magenta
        if (-not (Test-APIConnection -DryRun $DryRun)) {
            throw "Failed to establish connection"
        }
        Write-Host "✓ API connection verified`n" -ForegroundColor Green
        
        Write-Host "Phase 2: Loading Build Configuration..." -ForegroundColor Magenta
        $buildConfig = Load-BuildConfiguration
        Write-Host "✓ Build configuration loaded`n" -ForegroundColor Green
        
        Write-Host "Phase 3: Initializing ChatGPT Context..." -ForegroundColor Magenta
        Initialize-ChatGPTContext -BuildConfig $buildConfig -DryRun $DryRun
        
        Write-Host "Phase 4: Analyzing Build Configuration..." -ForegroundColor Magenta
        Send-BuildConfigForAnalysis -BuildConfig $buildConfig -DryRun $DryRun
        
        Write-Host "`n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
        Write-Host "  Synchronization Complete ✓" -ForegroundColor Green
        Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━`n" -ForegroundColor Cyan
    }
    catch {
        Write-Host "`n✗ ERROR: $($_.Exception.Message)`n" -ForegroundColor Red
        Write-LogEntry -Level "ERROR" -Message "Script failed: $($_.Exception.Message)"
        exit 1
    }
}

Main
