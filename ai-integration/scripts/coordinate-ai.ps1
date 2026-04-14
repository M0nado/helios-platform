<#
.SYNOPSIS
AI Coordination Orchestrator for HELIOS Platform

.DESCRIPTION
Coordinates ChatGPT and Codex recommendations, detects conflicts,
applies resolution logic, and generates unified recommendations.

.PARAMETER ChatGPTResponse
Response from ChatGPT analysis

.PARAMETER CodexResponse
Response from Codex generation

.PARAMETER ConflictResolution
Enable automatic conflict resolution (default: $true)

.PARAMETER GenerateReport
Generate coordination report (default: $false)

.EXAMPLE
$result = Invoke-AICoordination -ChatGPTResponse $gpt -CodexResponse $codex `
    -ConflictResolution $true -GenerateReport $true

.NOTES
Requires both ask-chatgpt.ps1 and ask-codex.ps1 to be loaded
AI-Generated: Yes
#>

function Invoke-AICoordination {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true)]
        [PSObject]$ChatGPTResponse,
        
        [Parameter(Mandatory=$true)]
        [PSObject]$CodexResponse,
        
        [Parameter(Mandatory=$false)]
        [bool]$ConflictResolution = $true,
        
        [Parameter(Mandatory=$false)]
        [bool]$GenerateReport = $false
    )
    
    # Detect conflicts
    $conflicts = Detect-AIConflicts -ChatGPT $ChatGPTResponse -Codex $CodexResponse
    
    # Resolve if enabled
    if ($ConflictResolution -and $conflicts.Count -gt 0) {
        $resolution = Resolve-AIConflicts -Conflicts $conflicts `
            -ChatGPT $ChatGPTResponse -Codex $CodexResponse
    } else {
        $resolution = $null
    }
    
    # Generate unified recommendation
    $unified = Generate-UnifiedRecommendation -ChatGPT $ChatGPTResponse `
        -Codex $CodexResponse -Resolution $resolution
    
    # Generate report if requested
    if ($GenerateReport) {
        $report = Generate-CoordinationReport -Conflicts $conflicts `
            -Resolution $resolution -Unified $unified
        Write-Host $report
    }
    
    # Log coordination event
    Log-Coordination -Conflicts $conflicts.Count -Resolution $resolution `
        -Unified $unified
    
    return @{
        Conflicts = $conflicts
        Resolution = $resolution
        Unified = $unified
        Timestamp = Get-Date
    }
}

<#
.SYNOPSIS
Detect conflicts between AI services
#>
function Detect-AIConflicts {
    param(
        [PSObject]$ChatGPT,
        [PSObject]$Codex
    )
    
    $conflicts = @()
    
    # Convert responses to strings for comparison
    $gptText = $ChatGPT | ConvertTo-Json -Compress
    $codexText = $Codex | ConvertTo-Json -Compress
    
    # Pattern matching for known conflict types
    $conflictPatterns = @{
        SecurityVsPerformance = @(
            @{ gpt = "granular|fine-grained|detailed"; codex = "consolidated|optimized|compact" },
            @{ severity = "Medium"; resolution = "Security wins" }
        )
        AuditVsEnforcement = @(
            @{ gpt = "audit.*week|monitoring"; codex = "enforce|deploy.*now" },
            @{ severity = "High"; resolution = "Risk assessment" }
        )
        ReadabilityVsOptimization = @(
            @{ gpt = "verbose|readable|clear"; codex = "compact|optimized|efficient" },
            @{ severity = "Low"; resolution = "Code review" }
        )
    }
    
    foreach ($pattern in $conflictPatterns.GetEnumerator()) {
        $gptMatch = $gptText -match $pattern.Value[0].gpt
        $codexMatch = $codexText -match $pattern.Value[0].codex
        
        if ($gptMatch -and $codexMatch) {
            $conflicts += @{
                Type = $pattern.Name
                Severity = $pattern.Value[1].severity
                Resolution = $pattern.Value[1].resolution
                GPTPosition = "Matches: $($pattern.Value[0].gpt)"
                CodexPosition = "Matches: $($pattern.Value[0].codex)"
                DetectedTime = Get-Date
            }
        }
    }
    
    return $conflicts
}

<#
.SYNOPSIS
Resolve detected conflicts
#>
function Resolve-AIConflicts {
    param(
        [array]$Conflicts,
        [PSObject]$ChatGPT,
        [PSObject]$Codex
    )
    
    $resolutions = @()
    
    foreach ($conflict in $Conflicts) {
        $decision = switch ($conflict.Type) {
            "SecurityVsPerformance" {
                @{
                    Decision = "ChatGPT"
                    Reasoning = "Security takes priority over performance"
                    ApplyGPT = $true
                    ApplyCodex = $false
                }
            }
            "AuditVsEnforcement" {
                @{
                    Decision = "RiskAssessment"
                    Reasoning = "Depends on organizational risk tolerance"
                    ApplyGPT = $null
                    ApplyCodex = $null
                    RequiresApproval = $true
                }
            }
            "ReadabilityVsOptimization" {
                @{
                    Decision = "CodeReview"
                    Reasoning = "Code review team decides based on standards"
                    ApplyGPT = $null
                    ApplyCodex = $null
                    RequiresApproval = $true
                }
            }
            default {
                @{
                    Decision = "Manual"
                    Reasoning = "Requires manual review"
                    RequiresApproval = $true
                }
            }
        }
        
        $decision.ConflictType = $conflict.Type
        $decision.Severity = $conflict.Severity
        $resolutions += $decision
    }
    
    return $resolutions
}

<#
.SYNOPSIS
Generate unified recommendation combining both AI services
#>
function Generate-UnifiedRecommendation {
    param(
        [PSObject]$ChatGPT,
        [PSObject]$Codex,
        [array]$Resolution
    )
    
    $unified = @{
        Source = "ChatGPT + Codex Coordination"
        GeneratedTime = Get-Date
        ApproachDescription = @"
This recommendation combines strategic planning from ChatGPT with 
code generation from GitHub Codex. Both have been evaluated for conflicts 
and integrated into a single coherent approach.
"@
        ChatGPTContribution = "Strategic planning, analysis, risk assessment"
        CodexContribution = "Code generation, implementation templates"
        ConflictsResolved = $Resolution.Count
        Status = if ($Resolution.Count -eq 0) { "Ready" } else { "Partial" }
    }
    
    if ($Resolution) {
        $unified.ApprovalRequired = $Resolution | Where-Object { $_.RequiresApproval } | Measure-Object | Select-Object -ExpandProperty Count
    }
    
    return $unified
}

<#
.SYNOPSIS
Generate coordination report
#>
function Generate-CoordinationReport {
    param(
        [array]$Conflicts,
        [array]$Resolution,
        [PSObject]$Unified
    )
    
    $report = @"
╔═══════════════════════════════════════════════════════════════════════════╗
║                    AI COORDINATION REPORT                                  ║
║                      $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')                                ║
╚═══════════════════════════════════════════════════════════════════════════╝

COORDINATION SUMMARY
───────────────────────────────────────────────────────────────────────────
- Conflicts Detected: $(if ($Conflicts) { $Conflicts.Count } else { "0" })
- Resolutions Applied: $(if ($Resolution) { $Resolution.Count } else { "0" })
- Status: $($Unified.Status)

CONFLICT ANALYSIS
───────────────────────────────────────────────────────────────────────────
$(@(
    if ($Conflicts) {
        foreach ($conflict in $Conflicts) {
            "• $($conflict.Type) [Severity: $($conflict.Severity)]"
            "  GPT Position: $($conflict.GPTPosition)"
            "  Codex Position: $($conflict.CodexPosition)"
            ""
        }
    } else {
        "• No conflicts detected between AI services"
    }
) -join "`n")

RESOLUTION DECISIONS
───────────────────────────────────────────────────────────────────────────
$(@(
    if ($Resolution) {
        foreach ($res in $Resolution) {
            "• $($res.ConflictType)"
            "  Decision: $($res.Decision)"
            "  Reasoning: $($res.Reasoning)"
            ""
        }
    } else {
        "• No resolutions needed"
    }
) -join "`n")

APPROVAL STATUS
───────────────────────────────────────────────────────────────────────────
Approvals Required: $(if ($Unified.ApprovalRequired) { $Unified.ApprovalRequired } else { "0" })
Recommendation Status: $($Unified.Status)

═════════════════════════════════════════════════════════════════════════════
"@
    
    return $report
}

<#
.SYNOPSIS
Log coordination event
#>
function Log-Coordination {
    param(
        [int]$ConflictCount,
        [array]$Resolution,
        [PSObject]$Unified
    )
    
    $logDir = "$env:LOCALAPPDATA\helios-ai-logs"
    if (-not (Test-Path $logDir)) {
        New-Item -ItemType Directory -Path $logDir -Force | Out-Null
    }
    
    $logFile = "$logDir\coordination-$(Get-Date -Format 'yyyyMMdd').log"
    
    $logEntry = @"
[$(Get-Date -Format 'yyyy-MM-ddTHH:mm:ssZ')] COORDINATION
- Conflicts detected: $ConflictCount
- Resolutions applied: $(if ($Resolution) { $Resolution.Count } else { 0 })
- Status: $($Unified.Status)
- Approvals required: $(if ($Unified.ApprovalRequired) { $Unified.ApprovalRequired } else { 0 })

"@
    
    Add-Content -Path $logFile -Value $logEntry -Encoding UTF8
}

<#
.SYNOPSIS
Get AI coordination statistics
#>
function Get-AICoordinationStats {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$false)]
        [int]$Days = 30
    )
    
    $logDir = "$env:LOCALAPPDATA\helios-ai-logs"
    if (-not (Test-Path $logDir)) {
        return "No logs found"
    }
    
    $coordLogs = Get-ChildItem "$logDir\coordination-*.log" -ErrorAction SilentlyContinue
    $stats = @{
        TotalCoordinations = 0
        TotalConflicts = 0
        ConflictsResolved = 0
        ApprovalsNeeded = 0
    }
    
    foreach ($log in $coordLogs) {
        $content = Get-Content $log
        $stats.TotalCoordinations += 1
        # Simple parsing - can be enhanced
    }
    
    return $stats
}

# Export functions
Export-ModuleMember -Function @(
    'Invoke-AICoordination'
    'Detect-AIConflicts'
    'Get-AICoordinationStats'
)
