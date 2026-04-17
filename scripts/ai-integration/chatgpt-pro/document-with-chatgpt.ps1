#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Auto-generate documentation for build system using ChatGPT
    
.DESCRIPTION
    Generates comprehensive documentation for the HELIOS Platform build system,
    including build guides, best practices, troubleshooting, and API references.
    
.PARAMETER OutputFormat
    Output format: markdown, html, both
    
.PARAMETER DocumentType
    Type of documentation to generate: guide, reference, troubleshooting, all
    
.PARAMETER OutputDir
    Directory to save generated documentation
    
.PARAMETER DryRun
    If specified, simulates generation without API calls
    
.EXAMPLE
    .\document-with-chatgpt.ps1 -DocumentType all -OutputDir ./docs
    .\document-with-chatgpt.ps1 -DocumentType guide -OutputFormat markdown
    
.NOTES
    Author: HELIOS Platform
    Version: 1.0.0
    Requires: PowerShell 7.0+
#>

param(
    [ValidateSet("markdown", "html", "both")]
    [string]$OutputFormat = "markdown",
    
    [ValidateSet("guide", "reference", "troubleshooting", "all")]
    [string]$DocumentType = "all",
    
    [string]$OutputDir = "./docs",
    
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

$DOCUMENTATION_PROMPTS = @{
    guide = @"
Create a comprehensive Build & Deployment Guide for the HELIOS Platform:

**Table of Contents:**
1. **Getting Started**
   - System requirements
   - Installation and setup
   - Initial configuration
   - First build

2. **Build Concepts**
   - Build configuration
   - Platforms and targets
   - Build types (Debug, Release)
   - Parallel execution

3. **Running Builds**
   - Command-line usage
   - Build parameters
   - Performance tuning
   - Troubleshooting common issues

4. **Advanced Topics**
   - Custom build configurations
   - Performance optimization
   - Caching strategies
   - Cross-platform building

5. **Best Practices**
   - Build hygiene
   - Dependency management
   - Version control
   - CI/CD integration

Use clear examples and practical instructions. Format as Markdown.
"@

    reference = @"
Create a Technical Reference Guide for the HELIOS Platform build system:

**Sections:**
1. **Build Configuration Reference**
   - Configuration file format
   - All available options
   - Default values
   - Type specifications

2. **Command Reference**
   - Available commands
   - Parameters and options
   - Return values
   - Exit codes

3. **API Reference**
   - Build API endpoints
   - Request/response formats
   - Error handling
   - Rate limiting

4. **Environment Variables**
   - Configuration variables
   - Build options
   - Performance tuning
   - Security settings

5. **File Structure**
   - Directory layout
   - Configuration files
   - Output artifacts
   - Log locations

6. **Data Types and Formats**
   - Build configurations
   - Artifact specifications
   - Dependency formats
   - Version formats

Include code examples and JSON/XML samples where appropriate. Format as Markdown.
"@

    troubleshooting = @"
Create a Comprehensive Troubleshooting Guide for the HELIOS Platform:

**Sections:**
1. **Common Issues and Solutions**
   - Build failures and resolutions
   - Dependency resolution issues
   - Platform-specific problems
   - Performance issues

2. **Diagnostic Tools**
   - How to run diagnostics
   - Interpreting log files
   - Collecting debug information
   - Performance profiling

3. **Performance Troubleshooting**
   - Slow builds
   - Memory issues
   - Disk space problems
   - Network issues

4. **Platform-Specific Issues**
   - Windows-specific problems
   - Linux-specific problems
   - macOS-specific problems
   - Cross-platform compatibility

5. **Integration Issues**
   - Git integration
   - CI/CD pipeline
   - Container environments
   - Cloud deployment

6. **Advanced Debugging**
   - Enable debug logging
   - Verbose output interpretation
   - Trace file analysis
   - API debugging

Include symptoms, causes, solutions, and prevention for each issue. Format as Markdown.
"@
}

function Generate-Documentation {
    param(
        [string]$DocType,
        [bool]$DryRun = $false
    )
    
    if (-not $DOCUMENTATION_PROMPTS.ContainsKey($DocType)) {
        $DocType = "guide"
    }
    
    Write-LogEntry -Level "INFO" -Message "Generating documentation" -Context @{ documentType = $DocType }
    
    $prompt = $DOCUMENTATION_PROMPTS[$DocType]
    
    $request = @{
        model = Get-ConfigValue -Key "OPENAI_MODEL" -Default "gpt-4"
        messages = @(
            @{
                role = "system"
                content = "You are a technical documentation expert. Create clear, comprehensive, and well-structured documentation with practical examples."
            }
            @{
                role = "user"
                content = $prompt
            }
        )
        max_tokens = [int](Get-ConfigValue -Key "OPENAI_MAX_TOKENS" -Default "4096")
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
                   -Action "Generate-Documentation" `
                   -Status "success" `
                   -Duration $elapsed.TotalMilliseconds `
                   -InputTokens ($response.content.usage.prompt_tokens ?? 0) `
                   -OutputTokens ($response.content.usage.completion_tokens ?? 0) `
                   -DryRun $DryRun
        
        return $response
    }
    catch {
        Write-LogEntry -Level "ERROR" -Message "Documentation generation failed: $($_.Exception.Message)"
        throw $_
    }
}

function Save-Documentation {
    param(
        [string]$Content,
        [string]$DocType,
        [string]$Format
    )
    
    if (-not (Test-Path $OutputDir)) {
        New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
        Write-LogEntry -Level "INFO" -Message "Created output directory: $OutputDir"
    }
    
    $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
    $filename = "$DocType-guide_${timestamp}.$Format"
    $filepath = Join-Path $OutputDir $filename
    
    $header = @"
# HELIOS Platform - $($DocType -replace '^.', {$_.ToString().ToUpper()}) Guide
Generated: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")

---

"@
    
    $fullContent = $header + $Content
    Set-Content -Path $filepath -Value $fullContent -Encoding UTF8
    
    Write-LogEntry -Level "INFO" -Message "Documentation saved: $filename"
    Write-Host "✓ Saved: $filename" -ForegroundColor Green
    
    return $filepath
}

function Convert-ToHTML {
    param([string]$MarkdownContent)
    
    Write-LogEntry -Level "INFO" -Message "Converting documentation to HTML"
    
    # Simple Markdown to HTML conversion
    $html = $MarkdownContent | `
        ForEach-Object {
            $_ -replace '# (.*)', '<h1>$1</h1>' | `
            ForEach-Object { $_ -replace '## (.*)', '<h2>$1</h2>' } | `
            ForEach-Object { $_ -replace '### (.*)', '<h3>$1</h3>' } | `
            ForEach-Object { $_ -replace '\*\*(.*?)\*\*', '<strong>$1</strong>' } | `
            ForEach-Object { $_ -replace '\*(.*?)\*', '<em>$1</em>' } | `
            ForEach-Object { $_ -replace '`(.*?)`', '<code>$1</code>' }
        } | `
        Join-String -Separator "`n"
    
    $htmlDocument = @"
<!DOCTYPE html>
<html>
<head>
    <meta charset="UTF-8">
    <title>HELIOS Platform Documentation</title>
    <style>
        body { font-family: Arial, sans-serif; margin: 40px; line-height: 1.6; }
        h1, h2, h3 { color: #333; margin-top: 20px; }
        code { background: #f4f4f4; padding: 2px 6px; border-radius: 3px; }
        pre { background: #f4f4f4; padding: 10px; border-radius: 5px; overflow-x: auto; }
        strong { font-weight: bold; }
    </style>
</head>
<body>
    $html
</body>
</html>
"@
    
    return $htmlDocument
}

function Main {
    Write-Host "`n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
    Write-Host "  AI Documentation Generator" -ForegroundColor Cyan
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━`n" -ForegroundColor Cyan
    
    if ($DryRun) {
        Write-Host "[DRY RUN MODE]`n" -ForegroundColor Yellow
    }
    
    try {
        $docTypes = if ($DocumentType -eq "all") {
            @("guide", "reference", "troubleshooting")
        } else {
            @($DocumentType)
        }
        
        foreach ($docType in $docTypes) {
            Write-Host "Generating $docType documentation..." -ForegroundColor Magenta
            $documentation = Generate-Documentation -DocType $docType -DryRun $DryRun
            
            if ($documentation.success -or $documentation.isDryRun) {
                $content = if ($documentation.isDryRun) {
                    "# $($docType -replace '^.', {$_.ToString().ToUpper()}) Documentation`n`nThis is a placeholder for auto-generated documentation."
                } else {
                    $documentation.content.choices[0].message.content
                }
                
                if ($OutputFormat -in @("markdown", "both")) {
                    Save-Documentation -Content $content -DocType $docType -Format "md"
                }
                
                if ($OutputFormat -in @("html", "both")) {
                    $htmlContent = Convert-ToHTML -MarkdownContent $content
                    $filename = "$docType-guide_$(Get-Date -Format 'yyyyMMdd_HHmmss').html"
                    $filepath = Join-Path $OutputDir $filename
                    Set-Content -Path $filepath -Value $htmlContent -Encoding UTF8
                    Write-Host "✓ Saved: $filename" -ForegroundColor Green
                }
            }
        }
        
        Write-Host "`n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
        Write-Host "  Documentation Generation Complete ✓" -ForegroundColor Green
        Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━`n" -ForegroundColor Cyan
        Write-Host "Output directory: $OutputDir`n" -ForegroundColor Cyan
    }
    catch {
        Write-Host "`n✗ ERROR: $($_.Exception.Message)`n" -ForegroundColor Red
        exit 1
    }
}

Main
