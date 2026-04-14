#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Refactor existing code using Codex AI
    
.DESCRIPTION
    Analyzes existing code and suggests improvements using Codex:
    - Performance optimization
    - Code quality enhancement
    - Pattern modernization
    - Complexity reduction
    - Security improvements
    
.PARAMETER FilePath
    Path to code file to refactor
    
.PARAMETER RefactorType
    Type of refactoring: performance, quality, modernize, simplify, security
    
.PARAMETER DryRun
    If specified, shows suggestions without applying changes
    
.EXAMPLE
    .\refactor-with-codex.ps1 -FilePath ./build-script.ps1
    .\refactor-with-codex.ps1 -FilePath ./build-script.ps1 -RefactorType performance
    
.NOTES
    Author: HELIOS Platform
    Version: 1.0.0
    Requires: PowerShell 7.0+
#>

param(
    [Parameter(Mandatory=$true)]
    [string]$FilePath,
    
    [ValidateSet("performance", "quality", "modernize", "simplify", "security", "all")]
    [string]$RefactorType = "all",
    
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

function Analyze-Code {
    param(
        [string]$FilePath,
        [string]$RefactorType,
        [bool]$DryRun = $false
    )
    
    if (-not (Test-Path $FilePath)) {
        throw "File not found: $FilePath"
    }
    
    $code = Get-Content $FilePath -Raw
    $lines = @($code -split "`n").Count
    
    Write-LogEntry -Level "INFO" -Message "Analyzing code for refactoring" -Context @{
        file = $FilePath
        lines = $lines
        refactorType = $RefactorType
    }
    
    $prompt = @"
Analyze and suggest refactoring improvements for this code:

File: $(Split-Path $FilePath -Leaf)
Lines: $lines
Type: $RefactorType

Code:
\`\`\`
$code
\`\`\`

For each refactoring opportunity:
1. **Current Pattern**: Describe current implementation
2. **Issues**: Identify problems (performance, security, maintainability)
3. **Suggested Improvement**: Show improved code
4. **Benefits**: Quantify improvements (performance, readability, maintainability)
5. **Implementation Complexity**: Rate as simple/moderate/complex
6. **Breaking Changes**: Identify any breaking changes
7. **Testing Considerations**: What needs testing

Focus on $RefactorType improvements. Provide prioritized recommendations.
"@
    
    $request = @{
        model = Get-ConfigValue -Key "CODEX_ENGINE" -Default "code-davinci-002"
        prompt = $prompt
        max_tokens = [int](Get-ConfigValue -Key "CODEX_MAX_TOKENS" -Default "2048")
        temperature = 0.3
    }
    
    try {
        $startTime = Get-Date
        $response = Invoke-OpenAIRequest -Endpoint "/chat/completions" `
                                        -Body @{
                                            model = "gpt-4"
                                            messages = @(
                                                @{ role = "system"; content = "You are a code refactoring expert." }
                                                @{ role = "user"; content = $prompt }
                                            )
                                            max_tokens = 3000
                                            temperature = 0.3
                                        } `
                                        -MaxRetries 3 `
                                        -DryRun $DryRun
        $elapsed = (Get-Date) - $startTime
        
        Log-APICall -Service "codex" `
                   -Action "Refactor-Analysis" `
                   -Status "success" `
                   -Duration $elapsed.TotalMilliseconds `
                   -DryRun $DryRun
        
        return $response
    }
    catch {
        Write-LogEntry -Level "ERROR" -Message "Refactoring analysis failed: $($_.Exception.Message)"
        throw $_
    }
}

function Format-RefactoringReport {
    param([object]$Response)
    
    if ($Response.isDryRun) {
        return @"
[Refactoring Analysis Report]

This analysis would contain:
- Performance optimization opportunities
- Code quality improvements
- Modernization suggestions
- Simplification opportunities
- Security vulnerability fixes

Each suggestion would include:
- Current pattern description
- Issues identified
- Improved code
- Estimated benefits
- Implementation complexity
- Testing considerations
"@
    }
    
    return $Response.content.choices[0].message.content
}

function Save-RefactoringReport {
    param(
        [string]$Report,
        [string]$SourceFile,
        [string]$RefactorType
    )
    
    $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
    $sourceFilename = Split-Path $SourceFile -Leaf
    $filename = "refactor_${sourceFilename}_${RefactorType}_${timestamp}.md"
    $filepath = Join-Path (Get-ConfigValue -Key "LOG_DIR" -Default "./logs") $filename
    
    $content = @"
# Refactoring Report
Source File: $sourceFilename
Refactor Type: $RefactorType
Generated: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")

---

$Report
"@
    
    Set-Content -Path $filepath -Value $content
    Write-LogEntry -Level "INFO" -Message "Refactoring report saved: $filename"
    return $filepath
}

function Main {
    Write-Host "`n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
    Write-Host "  Code Refactoring with Codex" -ForegroundColor Cyan
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━`n" -ForegroundColor Cyan
    
    if ($DryRun) {
        Write-Host "[DRY RUN MODE]`n" -ForegroundColor Yellow
    }
    
    try {
        Write-Host "Analyzing code for refactoring..." -ForegroundColor Magenta
        Write-Host "File: $FilePath" -ForegroundColor Gray
        Write-Host "Focus: $RefactorType`n" -ForegroundColor Gray
        
        $analysis = Analyze-Code -FilePath $FilePath -RefactorType $RefactorType -DryRun $DryRun
        $report = Format-RefactoringReport -Response $analysis
        
        Write-Host "[Refactoring Recommendations]`n" -ForegroundColor Green
        Write-Host $report
        
        $savedPath = Save-RefactoringReport -Report $report -SourceFile $FilePath -RefactorType $RefactorType
        Write-Host "`n✓ Report saved to: $savedPath" -ForegroundColor Green
        
        Write-Host "`n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
        Write-Host "  Analysis Complete ✓" -ForegroundColor Green
        Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━`n" -ForegroundColor Cyan
    }
    catch {
        Write-Host "`n✗ ERROR: $($_.Exception.Message)`n" -ForegroundColor Red
        exit 1
    }
}

Main
