#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Ask ChatGPT-4 for build optimization suggestions
    
.DESCRIPTION
    Queries GPT-4 for specific build optimization recommendations including:
    - Performance improvements
    - Caching strategies
    - Parallelization opportunities
    - Dependency optimization
    - Cross-platform compatibility
    
.PARAMETER FocusArea
    Specific area to analyze (performance, caching, dependencies, all)
    
.PARAMETER DryRun
    If specified, simulates operations without making actual API calls
    
.PARAMETER SaveResults
    If specified, saves recommendations to file
    
.EXAMPLE
    .\get-ai-suggestions.ps1 -FocusArea performance
    .\get-ai-suggestions.ps1 -FocusArea all -SaveResults
    
.NOTES
    Author: HELIOS Platform
    Version: 1.0.0
    Requires: PowerShell 7.0+
#>

param(
    [ValidateSet("performance", "caching", "dependencies", "parallelization", "security", "all")]
    [string]$FocusArea = "all",
    
    [switch]$DryRun,
    [switch]$SaveResults,
    [switch]$Verbose
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$apiClientPath = Split-Path $PSScriptRoot -Parent | Join-Path -ChildPath "config\api-clients.ps1"
. $apiClientPath

if ($Verbose) {
    [Environment]::SetEnvironmentVariable("DEBUG_MODE", "true")
}

$OPTIMIZATION_PROMPTS = @{
    performance = @"
Analyze the HELIOS Platform build pipeline and suggest performance optimizations:

1. **Build Time Reduction**: Identify bottlenecks and parallelization opportunities
2. **Compilation Optimization**: Suggest compiler flags and incremental build strategies
3. **Artifact Caching**: Recommend caching layers and strategies
4. **Dependency Resolution**: Optimize NuGet package resolution and restore times
5. **Incremental Builds**: How to maximize incremental build effectiveness
6. **Resource Allocation**: CPU, memory, and disk optimization

Provide specific, actionable recommendations with estimated impact (time/resource savings).
"@

    caching = @"
Design an optimal caching strategy for the HELIOS Platform builds:

1. **Multi-Level Caching**: Local, CI/CD, and distributed caching layers
2. **Cache Key Strategy**: Effective cache invalidation patterns
3. **NuGet Package Caching**: Repository and package-level optimization
4. **Docker Layer Caching**: Efficient Dockerfile construction
5. **Build Artifact Caching**: What to cache and retention policies
6. **Cache Warming**: Pre-population strategies for CI/CD pipelines

Include implementation patterns and configuration recommendations.
"@

    dependencies = @"
Optimize dependency management for the HELIOS Platform:

1. **Dependency Analysis**: Review current technology stack for:
   - Outdated packages requiring updates
   - Unused or redundant dependencies
   - Potential security vulnerabilities
   - Compatibility issues between versions
2. **Transitive Dependency Review**: Analyze cascading dependencies
3. **Version Strategy**: Recommend pinning vs. flexible versioning
4. **Alternative Solutions**: Suggest modern alternatives to legacy dependencies
5. **Update Strategy**: Plan for regular security and feature updates

Provide prioritized recommendations with risk assessment.
"@

    parallelization = @"
Optimize parallel build execution for HELIOS Platform:

1. **Task Dependency Analysis**: Identify parallelizable build tasks
2. **Job Scheduling**: Optimal job distribution across available cores
3. **Resource Contention**: Prevent bottlenecks in parallel execution
4. **Test Parallelization**: Running tests in parallel without race conditions
5. **Artifact Staging**: Parallel artifact building and packaging
6. **CI/CD Pipeline Parallelization**: Multiple platform builds simultaneously

Include specific configurations for 4, 8, and 16 core systems.
"@

    security = @"
Security hardening for HELIOS Platform builds:

1. **Dependency Security**: Check for known vulnerabilities in build chain
2. **Build Environment**: Secure build agent configuration
3. **Artifact Signing**: Code signing and artifact verification
4. **Access Control**: Build artifact access and distribution
5. **Secret Management**: Secure handling of API keys and credentials
6. **Supply Chain Security**: Prevent tampering in build pipeline
7. **Container Security**: Secure base images and layer scanning

Provide security best practices and implementation guidance.
"@
}

function Get-OptimizationSuggestions {
    param(
        [string]$Area,
        [bool]$DryRun = $false
    )
    
    if (-not $OPTIMIZATION_PROMPTS.ContainsKey($Area)) {
        $Area = "all"
    }
    
    $prompt = if ($Area -eq "all") {
        @"
Provide comprehensive build optimization recommendations for the HELIOS Platform covering:
- Performance improvements (build time, resource usage)
- Caching strategies (multi-level caching)
- Dependency optimization (security, version strategy)
- Parallelization opportunities
- Security hardening

Prioritize recommendations by impact and implementation complexity.
"@
    } else {
        $OPTIMIZATION_PROMPTS[$Area]
    }
    
    Write-LogEntry -Level "INFO" -Message "Requesting optimization suggestions" -Context @{ focusArea = $Area }
    
    $request = @{
        model = Get-ConfigValue -Key "OPENAI_MODEL" -Default "gpt-4"
        messages = @(
            @{
                role = "system"
                content = "You are a build optimization expert for the HELIOS Platform. Provide specific, actionable recommendations with implementation details and estimated impact."
            }
            @{
                role = "user"
                content = $prompt
            }
        )
        max_tokens = [int](Get-ConfigValue -Key "OPENAI_MAX_TOKENS" -Default "4096")
        temperature = 0.5
    }
    
    try {
        $startTime = Get-Date
        $response = Invoke-OpenAIRequest -Endpoint "/chat/completions" `
                                        -Body $request `
                                        -MaxRetries 3 `
                                        -DryRun $DryRun
        $elapsed = (Get-Date) - $startTime
        
        Log-APICall -Service "chatgpt-pro" `
                   -Action "Get-Suggestions" `
                   -Status "success" `
                   -Duration $elapsed.TotalMilliseconds `
                   -InputTokens ($response.content.usage.prompt_tokens ?? 0) `
                   -OutputTokens ($response.content.usage.completion_tokens ?? 0) `
                   -DryRun $DryRun `
                   -Details @{ focusArea = $Area }
        
        return $response
    }
    catch {
        Write-LogEntry -Level "ERROR" -Message "Failed to get suggestions: $($_.Exception.Message)"
        throw $_
    }
}

function Format-Suggestions {
    param($Response)
    
    if ($Response.isDryRun) {
        return @"
[DRY RUN] Build Optimization Suggestions

This section would contain detailed optimization recommendations including:
- Performance improvements with estimated time savings
- Caching strategies for multi-platform builds
- Dependency management recommendations
- Parallelization strategies
- Security hardening guidelines
"@
    }
    
    return $Response.content.choices[0].message.content
}

function Save-Suggestions {
    param(
        [string]$Content,
        [string]$FocusArea
    )
    
    $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
    $filename = "suggestions_${FocusArea}_${timestamp}.md"
    $filepath = Join-Path (Get-ConfigValue -Key "LOG_DIR" -Default "./logs") $filename
    
    $output = @"
# HELIOS Platform Build Optimization Suggestions
Generated: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
Focus Area: $FocusArea

$Content
"@
    
    Set-Content -Path $filepath -Value $output
    Write-LogEntry -Level "INFO" -Message "Suggestions saved to $filename"
    
    return $filepath
}

function Main {
    Write-Host "`n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
    Write-Host "  AI Build Optimization Suggestions" -ForegroundColor Cyan
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━`n" -ForegroundColor Cyan
    
    if ($DryRun) {
        Write-Host "[DRY RUN MODE]`n" -ForegroundColor Yellow
    }
    
    try {
        Write-Host "Requesting AI optimization suggestions for focus area: $FocusArea`n" -ForegroundColor Magenta
        
        $suggestions = Get-OptimizationSuggestions -Area $FocusArea -DryRun $DryRun
        $formatted = Format-Suggestions -Response $suggestions
        
        Write-Host "`n[Build Optimization Recommendations]`n" -ForegroundColor Green
        Write-Host $formatted
        
        if ($SaveResults) {
            $savedPath = Save-Suggestions -Content $formatted -FocusArea $FocusArea
            Write-Host "`n✓ Suggestions saved to: $savedPath" -ForegroundColor Green
        }
        
        Write-Host "`n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
        Write-Host "  Complete ✓" -ForegroundColor Green
        Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━`n" -ForegroundColor Cyan
    }
    catch {
        Write-Host "`n✗ ERROR: $($_.Exception.Message)`n" -ForegroundColor Red
        exit 1
    }
}

Main
