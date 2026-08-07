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
        [bool]$GenerateReport = $false,

        [Parameter(Mandatory=$false)]
        [bool]$MergeSimilarRecommendations = $true,

        [Parameter(Mandatory=$false)]
        [ValidateRange(0.1, 1.0)]
        [double]$SimilarityThreshold = 0.7
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

    $mergedRecommendations = @()
    if ($MergeSimilarRecommendations) {
        $mergedRecommendations = Merge-SimilarAIRecommendations `
            -ChatGPT $ChatGPTResponse `
            -Codex $CodexResponse `
            -SimilarityThreshold $SimilarityThreshold
    }

    $unified.MergedRecommendations = $mergedRecommendations.Count
    
    # Generate report if requested
    if ($GenerateReport) {
        $report = Generate-CoordinationReport -Conflicts $conflicts `
            -Resolution $resolution -Unified $unified `
            -MergedRecommendations $mergedRecommendations
        Write-Host $report
    }
    
    # Log coordination event
    Log-Coordination -Conflicts $conflicts.Count -Resolution $resolution `
        -Unified $unified
    
    return @{
        Conflicts = $conflicts
        Resolution = $resolution
        Unified = $unified
        MergedRecommendations = $mergedRecommendations
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
    $gptText = $ChatGPT | ConvertTo-Json -Depth 10 -Compress
    $codexText = $Codex | ConvertTo-Json -Depth 10 -Compress
    
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
Extract response text from heterogeneous AI payloads
#>
function Get-AIResponseText {
    param([PSObject]$Response)

    if ($null -eq $Response) {
        return ""
    }

    if ($Response -is [string]) {
        return $Response
    }

    if ($Response.PSObject.Properties.Name -contains "choices") {
        $choices = @($Response.choices)
        $firstChoice = if ($choices.Count -gt 0) { $choices[0] } else { $null }

        if ($null -ne $firstChoice) {
            if ($firstChoice.PSObject.Properties.Name -contains "message") {
                $message = $firstChoice.message
                if ($null -ne $message -and ($message.PSObject.Properties.Name -contains "content")) {
                    $content = $message.content

                    if ($content -is [string] -and -not [string]::IsNullOrWhiteSpace($content)) {
                        return [string]$content
                    }

                    if (($content -is [System.Collections.IEnumerable]) -and -not ($content -is [string])) {
                        $contentParts = @()
                        foreach ($part in $content) {
                            if ($part -and $part.type -eq "text" -and $part.text) {
                                $contentParts += [string]$part.text
                            }
                        }

                        if ($contentParts.Count -gt 0) {
                            return ($contentParts -join "`n")
                        }
                    }
                }

                # Message-shaped payloads with no text are metadata-only.
                return ""
            }

            if (($firstChoice.PSObject.Properties.Name -contains "text") -and
                -not [string]::IsNullOrWhiteSpace([string]$firstChoice.text)) {
                return [string]$firstChoice.text
            }

            return ""
        }
    }

    if ($Response.PSObject.Properties.Name -contains "content") {
        $content = $Response.content

        if ($content -is [string] -and -not [string]::IsNullOrWhiteSpace($content)) {
            return [string]$content
        }

        if (($content -is [System.Collections.IEnumerable]) -and -not ($content -is [string])) {
            $contentParts = @()
            foreach ($part in $content) {
                if ($part -and $part.type -eq "text" -and $part.text) {
                    $contentParts += [string]$part.text
                }
            }

            if ($contentParts.Count -gt 0) {
                return ($contentParts -join "`n")
            }
        }

        return ""
    }

    return ($Response | ConvertTo-Json -Depth 10 -Compress)
}

<#
.SYNOPSIS
Normalize recommendation text to support fuzzy similarity comparison
#>
function Normalize-RecommendationText {
    param([string]$Text)

    if (-not $Text) {
        return ""
    }

    $normalized = $Text.ToLowerInvariant()

    # Preserve language-significant punctuation by canonicalizing known tokens.
    $normalized = $normalized -replace "(?<!\w)c\+\+(?!\w)", " cpp "
    $normalized = $normalized -replace "(?<!\w)c#(?!\w)", " csharp "

    # Keep Unicode letters and numbers for non-Latin recommendations.
    $normalized = $normalized -replace "[^\p{L}\p{N}\s]", " "
    $normalized = $normalized -replace "\s+", " "

    return $normalized.Trim()
}

<#
.SYNOPSIS
Tokenize normalized recommendation text for similarity checks
#>
function Get-RecommendationTokens {
    param([string]$Text)

    if (-not $Text) {
        return @()
    }

    return $Text.Split(" ", [System.StringSplitOptions]::RemoveEmptyEntries) |
        Where-Object { $_.Length -gt 1 } |
        Select-Object -Unique
}

<#
.SYNOPSIS
Calculate Jaccard similarity between two recommendation texts
#>
function Get-TextSimilarity {
    param(
        [string]$LeftText,
        [string]$RightText
    )

    if (-not $LeftText -or -not $RightText) {
        return 0.0
    }

    if ($LeftText -eq $RightText) {
        return 1.0
    }

    $leftWords = Get-RecommendationTokens -Text $LeftText
    $rightWords = Get-RecommendationTokens -Text $RightText

    if ($leftWords.Count -eq 0 -or $rightWords.Count -eq 0) {
        return 0.0
    }

    $leftSet = [System.Collections.Generic.HashSet[string]]::new([string[]]$leftWords)
    $rightSet = [System.Collections.Generic.HashSet[string]]::new([string[]]$rightWords)
    $intersection = [System.Collections.Generic.HashSet[string]]::new($leftSet)
    $null = $intersection.IntersectWith($rightSet)
    $union = [System.Collections.Generic.HashSet[string]]::new($leftSet)
    $null = $union.UnionWith($rightSet)

    if ($union.Count -eq 0) {
        return 0.0
    }

    return [double]$intersection.Count / [double]$union.Count
}

<#
.SYNOPSIS
Detect if text looks like source code rather than recommendation prose
#>
function Test-CodeLikeRecommendationText {
    param([string]$Text)

    if ([string]::IsNullOrWhiteSpace($Text)) {
        return $false
    }

    $codePatterns = @(
        '(?m)^\s*```',
        '(?m)^\s*(using|namespace|class|interface|enum|record)\b',
        '(?m)^\s*(public|private|internal|protected)\s+',
        '(?m)^\s*(function|param\s*\(|foreach\s*\(|switch\s*\(|return\b)',
        '(?m)^\s*(import|from|def|const|let|var)\b',
        '(?m)^\s*#include\b'
    )

    foreach ($pattern in $codePatterns) {
        if ($Text -match $pattern) {
            return $true
        }
    }

    $lineCount = ([regex]::Matches($Text, "(`r`n|`n|`r)")).Count + 1
    $symbolCount = ([regex]::Matches($Text, "[\{\};]")).Count
    return ($lineCount -ge 6 -and $symbolCount -ge 8)
}

<#
.SYNOPSIS
Recursively extract recommendation-like text fields from structured payloads
#>
function Get-StructuredRecommendationStrings {
    param(
        [object]$Value,
        [string]$PropertyName = ""
    )

    if ($null -eq $Value) {
        return @()
    }

    $results = [System.Collections.Generic.List[string]]::new()

    if ($Value -is [string]) {
        $candidate = $Value.Trim()
        if ($candidate.Length -lt 12) {
            return @()
        }

        if ([string]::IsNullOrWhiteSpace($PropertyName) -or
            $PropertyName -match "(?i)(recommend|description|summary|guidance|action|advice|text|content|title|message|rationale|note)") {
            $results.Add($candidate)
        }

        return $results.ToArray()
    }

    if (($Value -is [System.Collections.IEnumerable]) -and -not ($Value -is [string])) {
        foreach ($entry in $Value) {
            $texts = Get-StructuredRecommendationStrings -Value $entry -PropertyName $PropertyName
            foreach ($text in $texts) {
                if (-not $results.Contains($text)) {
                    $results.Add($text)
                }
            }
        }

        return $results.ToArray()
    }

    if ($Value.PSObject) {
        foreach ($property in $Value.PSObject.Properties) {
            $texts = Get-StructuredRecommendationStrings -Value $property.Value -PropertyName $property.Name
            foreach ($text in $texts) {
                if (-not $results.Contains($text)) {
                    $results.Add($text)
                }
            }
        }
    }

    return $results.ToArray()
}

<#
.SYNOPSIS
Parse structured JSON payloads into recommendation candidates
#>
function Get-StructuredRecommendationCandidates {
    param(
        [string]$Text,
        [string]$Source
    )

    if ([string]::IsNullOrWhiteSpace($Text)) {
        return @()
    }

    $trimmed = $Text.Trim()
    if (-not ($trimmed.StartsWith("{") -or $trimmed.StartsWith("["))) {
        return @()
    }

    try {
        $parsed = $trimmed | ConvertFrom-Json -Depth 20 -ErrorAction Stop
    } catch {
        return @()
    }

    $structuredTexts = Get-StructuredRecommendationStrings -Value $parsed
    if (-not $structuredTexts -or $structuredTexts.Count -eq 0) {
        return @()
    }

    $candidates = [System.Collections.Generic.List[object]]::new()
    foreach ($candidateText in ($structuredTexts | Select-Object -Unique)) {
        $normalized = Normalize-RecommendationText -Text $candidateText
        if (-not $normalized) {
            continue
        }

        $candidates.Add([PSCustomObject]@{
            Source = $Source
            Text = $candidateText.Trim()
            Normalized = $normalized
        })
    }

    return $candidates
}

<#
.SYNOPSIS
Detect potentially contradictory recommendations with opposite polarity
#>
function Test-PolarityConflict {
    param(
        [string]$LeftText,
        [string]$RightText
    )

    if (-not $LeftText -or -not $RightText) {
        return $false
    }

    $negationTokenPattern = "\b(no|not|never|disable|disabled|avoid|forbid|forbidden|deny|denied|without|dont|cannot)\b"
    $leftHasNegation = ($LeftText -match $negationTokenPattern) -or ($LeftText -match "\bdo\s+not\b")
    $rightHasNegation = ($RightText -match $negationTokenPattern) -or ($RightText -match "\bdo\s+not\b")

    if ($leftHasNegation -eq $rightHasNegation) {
        return $false
    }

    $leftTokens = Get-RecommendationTokens -Text $LeftText
    $rightTokens = Get-RecommendationTokens -Text $RightText

    if ($leftTokens.Count -eq 0 -or $rightTokens.Count -eq 0) {
        return $false
    }

    $leftSet = [System.Collections.Generic.HashSet[string]]::new([string[]]$leftTokens)
    $rightSet = [System.Collections.Generic.HashSet[string]]::new([string[]]$rightTokens)
    $intersection = [System.Collections.Generic.HashSet[string]]::new($leftSet)
    $null = $intersection.IntersectWith($rightSet)

    $largestSet = [Math]::Max($leftSet.Count, $rightSet.Count)
    if ($largestSet -eq 0) {
        return $false
    }

    $overlap = [double]$intersection.Count / [double]$largestSet
    return $overlap -ge 0.6
}

<#
.SYNOPSIS
Extract candidate recommendation lines/sentences from AI response text
#>
function Get-RecommendationCandidates {
    param(
        [string]$Text,
        [string]$Source
    )

    if (-not $Text) {
        return @()
    }

    $structuredCandidates = Get-StructuredRecommendationCandidates -Text $Text -Source $Source
    if ($structuredCandidates.Count -gt 0) {
        return $structuredCandidates | Select-Object -Unique -Property Source, Text, Normalized
    }

    # Codex commonly returns source code, which should not be interpreted as prose guidance.
    if (Test-CodeLikeRecommendationText -Text $Text) {
        return @()
    }

    $candidates = [System.Collections.Generic.List[object]]::new()
    $rawLines = $Text -split "(`r`n|`n|`r)"

    foreach ($line in $rawLines) {
        $trimmed = $line.Trim()
        if (-not $trimmed) {
            continue
        }

        if ($trimmed -match "^(\-|\*|\d+\.)\s+") {
            $normalizedLine = $trimmed -replace "^(\-|\*|\d+\.)\s+", ""
            $candidates.Add([PSCustomObject]@{
                Source = $Source
                Text = $normalizedLine.Trim()
                Normalized = Normalize-RecommendationText -Text $normalizedLine
            })
        }
    }

    if ($candidates.Count -eq 0) {
        $sentences = $Text -split "(?<=[\.\!\?])\s+"
        foreach ($sentence in $sentences) {
            $trimmedSentence = $sentence.Trim()
            if ($trimmedSentence.Length -lt 25) {
                continue
            }

            $candidates.Add([PSCustomObject]@{
                Source = $Source
                Text = $trimmedSentence
                Normalized = Normalize-RecommendationText -Text $trimmedSentence
            })
        }
    }

    return $candidates | Where-Object { $_.Normalized } | Select-Object -Unique -Property Source, Text, Normalized
}

<#
.SYNOPSIS
Merge semantically similar recommendations across AI responses
#>
function Merge-SimilarAIRecommendations {
    param(
        [PSObject]$ChatGPT,
        [PSObject]$Codex,
        [double]$SimilarityThreshold = 0.7
    )

    $chatGPTText = Get-AIResponseText -Response $ChatGPT
    $codexText = Get-AIResponseText -Response $Codex

    $items = @()
    $items += Get-RecommendationCandidates -Text $chatGPTText -Source "ChatGPT"
    $items += Get-RecommendationCandidates -Text $codexText -Source "Codex"

    if (-not $items -or $items.Count -eq 0) {
        return @()
    }

    $clusters = [System.Collections.Generic.List[object]]::new()
    foreach ($item in $items) {
        if (-not $item.Normalized) {
            continue
        }

        $bestCluster = $null
        $bestScore = 0.0

        foreach ($cluster in $clusters) {
            $score = Get-TextSimilarity -LeftText $item.Normalized -RightText $cluster.Normalized
            if ($score -gt $bestScore) {
                $bestScore = $score
                $bestCluster = $cluster
            }
        }

        if ($bestCluster -and $bestScore -ge $SimilarityThreshold -and
            -not ($bestCluster.Sources -contains $item.Source) -and
            -not (Test-PolarityConflict -LeftText $item.Normalized -RightText $bestCluster.Normalized)) {
            if (-not ($bestCluster.Sources -contains $item.Source)) {
                $bestCluster.Sources += $item.Source
            }
            if (-not ($bestCluster.Variants -contains $item.Text)) {
                $bestCluster.Variants += $item.Text
            }
            $bestCluster.ScoreTotal += $bestScore
            $bestCluster.ScoreCount += 1
            $bestCluster.MergeCount += 1
            continue
        }

        $clusters.Add([PSCustomObject]@{
            Recommendation = $item.Text
            Normalized = $item.Normalized
            Sources = @($item.Source)
            Variants = @($item.Text)
            ScoreTotal = 0.0
            ScoreCount = 0
            MergeCount = 0
        })
    }

    return $clusters |
        Where-Object {
            ($_.Sources | Select-Object -Unique).Count -gt 1 -and $_.MergeCount -gt 0
        } |
        ForEach-Object {
            $similarity = if ($_.ScoreCount -gt 0) {
                [Math]::Round(($_.ScoreTotal / $_.ScoreCount), 2)
            } else {
                $null
            }

        [PSCustomObject]@{
            Recommendation = $_.Recommendation
            Sources = ($_.Sources | Select-Object -Unique) -join ", "
            VariantCount = $_.Variants.Count
            Similarity = $similarity
        }
    }
}

<#
.SYNOPSIS
Generate coordination report
#>
function Generate-CoordinationReport {
    param(
        [array]$Conflicts,
        [array]$Resolution,
        [PSObject]$Unified,
        [array]$MergedRecommendations
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
- Similar Recommendations Merged: $(if ($MergedRecommendations) { $MergedRecommendations.Count } else { "0" })
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

MERGED RECOMMENDATIONS
───────────────────────────────────────────────────────────────────────────
$(@(
    if ($MergedRecommendations) {
        foreach ($item in $MergedRecommendations) {
            "• $($item.Recommendation)"
            "  Sources: $($item.Sources)"
            "  Variants: $($item.VariantCount) Similarity: $($item.Similarity)"
            ""
        }
    } else {
        "• No merge candidates detected"
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

# Export functions only when loaded as a module.
if ($ExecutionContext.SessionState.Module) {
    Export-ModuleMember -Function @(
        'Invoke-AICoordination'
        'Detect-AIConflicts'
        'Get-AICoordinationStats'
    )
}
