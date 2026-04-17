<#
.SYNOPSIS
GitHub Codex API integration for HELIOS Platform

.DESCRIPTION
Generates code using GitHub Codex API based on specifications.
Includes safety checks, validation, and error handling.

.PARAMETER Spec
Code specification describing what to generate

.PARAMETER Language
Programming language: powershell (default), python, javascript

.PARAMETER AddSafetyChecks
Include safety checks and validation (default: $true)

.PARAMETER IncludeTests
Include test cases (default: $false)

.PARAMETER ValidateBeforeReturn
Validate generated code before returning (default: $true)

.PARAMETER IncludeErrorHandling
Add error handling to generated code (default: $true)

.EXAMPLE
$code = Invoke-Codex -Spec "Generate AppLocker rule script" `
    -Language "powershell" -AddSafetyChecks $true

.NOTES
Requires GITHUB_COPILOT_API_KEY or GitHub CLI authentication
AI-Generated: Yes
#>

function Invoke-Codex {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true)]
        [string]$Spec,
        
        [Parameter(Mandatory=$false)]
        [ValidateSet("powershell", "python", "javascript")]
        [string]$Language = "powershell",
        
        [Parameter(Mandatory=$false)]
        [bool]$AddSafetyChecks = $true,
        
        [Parameter(Mandatory=$false)]
        [bool]$IncludeTests = $false,
        
        [Parameter(Mandatory=$false)]
        [bool]$ValidateBeforeReturn = $true,
        
        [Parameter(Mandatory=$false)]
        [bool]$IncludeErrorHandling = $true
    )
    
    # Build comprehensive prompt
    $prompt = Build-CodexPrompt -Spec $Spec -Language $Language `
        -AddSafety $AddSafetyChecks -AddErrorHandling $IncludeErrorHandling
    
    # Call Codex API
    try {
        $startTime = [DateTime]::Now
        
        # Use GitHub CLI if available, otherwise use API
        if ((Get-Command gh -ErrorAction SilentlyContinue) -and -not $env:GITHUB_COPILOT_API_KEY) {
            $code = Invoke-CodexViaGitHub -Prompt $prompt
        } else {
            $code = Invoke-CodexViaAPI -Prompt $prompt
        }
        
        $duration = ([DateTime]::Now - $startTime).TotalSeconds
        
        # Add AI header
        $code = Add-AIHeader -Code $code -Source "GitHub Codex" -Language $Language
        
        # Validate if requested
        if ($ValidateBeforeReturn) {
            Test-GeneratedCode -Code $code -Language $Language
        }
        
        # Add tests if requested
        if ($IncludeTests) {
            $tests = Generate-Tests -Code $code -Language $Language
            $code = "$code`n`n$tests"
        }
        
        # Log the generation
        Log-CodexGeneration -Spec $Spec -Language $Language `
            -CodeLength $code.Length -Duration $duration
        
        return $code
    }
    catch {
        Write-Error "Codex generation error: $_"
        throw
    }
}

<#
.SYNOPSIS
Build comprehensive Codex prompt with safety and error handling
#>
function Build-CodexPrompt {
    param(
        [string]$Spec,
        [string]$Language,
        [bool]$AddSafety,
        [bool]$AddErrorHandling
    )
    
    $prompt = "Generate $Language code for:`n$Spec`n`n"
    
    if ($AddSafety) {
        $prompt += "SAFETY REQUIREMENTS:`n"
        $prompt += "- Validate all inputs`n"
        $prompt += "- No hardcoded credentials`n"
        $prompt += "- Handle errors gracefully`n"
        $prompt += "- Include logging`n`n"
    }
    
    if ($AddErrorHandling) {
        $prompt += "ERROR HANDLING:`n"
        $prompt += "- Use try-catch blocks`n"
        $prompt += "- Provide meaningful error messages`n"
        $prompt += "- Log all errors`n`n"
    }
    
    $prompt += "REQUIREMENTS:`n"
    $prompt += "- Clear variable names`n"
    $prompt += "- Comprehensive comments`n"
    $prompt += "- Professional quality code`n"
    $prompt += "- Follow language best practices`n"
    
    return $prompt
}

<#
.SYNOPSIS
Invoke Codex via GitHub CLI
#>
function Invoke-CodexViaGitHub {
    param([string]$Prompt)
    
    # Use GitHub CLI copilot command if available
    try {
        $code = gh copilot suggest -t shell "$Prompt" 2>$null
        return $code
    }
    catch {
        Write-Verbose "GitHub CLI copilot not available, using API"
        return Invoke-CodexViaAPI -Prompt $Prompt
    }
}

<#
.SYNOPSIS
Invoke Codex via OpenAI API (same as ChatGPT)
#>
function Invoke-CodexViaAPI {
    param([string]$Prompt)
    
    if (-not $env:OPENAI_API_KEY) {
        throw "OPENAI_API_KEY not set. Cannot call Codex API."
    }
    
    $headers = @{
        "Authorization" = "Bearer $($env:OPENAI_API_KEY)"
        "Content-Type" = "application/json"
    }
    
    $body = @{
        model = "code-davinci-002"
        prompt = $Prompt
        max_tokens = 4000
        temperature = 0.3
    } | ConvertTo-Json
    
    $response = Invoke-RestMethod `
        -Uri "https://api.openai.com/v1/completions" `
        -Method Post `
        -Headers $headers `
        -Body $body `
        -ErrorAction Stop
    
    return $response.choices[0].text.Trim()
}

<#
.SYNOPSIS
Add AI-generated code header
#>
function Add-AIHeader {
    param(
        [string]$Code,
        [string]$Source,
        [string]$Language
    )
    
    $header = switch ($Language) {
        "powershell" {
            @"
<#
.AI_GENERATED_HEADER
    Source: $Source
    Generated: $(Get-Date -Format 'o')
    Status: REQUIRES_REVIEW
    Language: PowerShell
#>

"@
        }
        "python" {
            @"
# AI-Generated Code
# Source: $Source
# Generated: $(Get-Date -Format 'o')
# Status: REQUIRES_REVIEW

"@
        }
        "javascript" {
            @"
// AI-Generated Code
// Source: $Source
// Generated: $(Get-Date -Format 'o')
// Status: REQUIRES_REVIEW

"@
        }
    }
    
    return "$header`n$Code"
}

<#
.SYNOPSIS
Validate generated code for basic security and syntax
#>
function Test-GeneratedCode {
    param(
        [string]$Code,
        [string]$Language
    )
    
    # Check for obvious security issues
    $dangerPatterns = @(
        "password|api.?key|secret|credential"
        "cmd\.exe|powershell.*-nop|bypass"
        "Remove-Item.*recurse|del.*\/s"
    )
    
    foreach ($pattern in $dangerPatterns) {
        if ($Code -match $pattern) {
            Write-Warning "Generated code contains potentially dangerous pattern: $pattern"
        }
    }
    
    # Language-specific validation
    if ($Language -eq "powershell") {
        # Try to parse as PowerShell to catch syntax errors
        try {
            [System.Management.Automation.PSParser]::Tokenize($Code, [ref]$null) | Out-Null
            Write-Verbose "PowerShell code validation passed"
        }
        catch {
            Write-Warning "PowerShell syntax validation warning: $_"
        }
    }
}

<#
.SYNOPSIS
Generate tests for code
#>
function Generate-Tests {
    param(
        [string]$Code,
        [string]$Language
    )
    
    $testTemplate = switch ($Language) {
        "powershell" {
            @"
# Generated test cases
Describe "Generated Code Tests" {
    It "Should execute without errors" {
        { $Code } | Should -Not -Throw
    }
}
"@
        }
        default {
            "# Tests should be added for this code"
        }
    }
    
    return $testTemplate
}

<#
.SYNOPSIS
Log Codex generation event
#>
function Log-CodexGeneration {
    param(
        [string]$Spec,
        [string]$Language,
        [int]$CodeLength,
        [double]$Duration
    )
    
    $logDir = "$env:LOCALAPPDATA\helios-ai-logs"
    if (-not (Test-Path $logDir)) {
        New-Item -ItemType Directory -Path $logDir -Force | Out-Null
    }
    
    $logFile = "$logDir\codex-$(Get-Date -Format 'yyyyMMdd').log"
    
    $logEntry = @"
[$(Get-Date -Format 'yyyy-MM-ddTHH:mm:ssZ')] CODEX_GENERATION
- Language: $Language
- Spec length: $($Spec.Length) chars
- Generated length: $CodeLength chars
- Duration: $($Duration.ToString('F2'))s
- Status: Success

"@
    
    Add-Content -Path $logFile -Value $logEntry -Encoding UTF8
}

# Export function
Export-ModuleMember -Function Invoke-Codex
