<#
.SYNOPSIS
    Validates HELIOS Platform board configuration
.DESCRIPTION
    Comprehensive validation of all board elements: fields, templates, automation, views
.PARAMETER GitHubToken
    GitHub Personal Access Token
.PARAMETER ProjectNumber
    Project number to validate
.PARAMETER OrganizationName
    Organization name
.PARAMETER GenerateReport
    Generate comprehensive validation report
.PARAMETER Verbose
    Detailed output
#>

param(
    [Parameter(Mandatory=$true)]
    [string]$GitHubToken,
    
    [Parameter(Mandatory=$true)]
    [int]$ProjectNumber,
    
    [Parameter(Mandatory=$true)]
    [string]$OrganizationName,
    
    [switch]$GenerateReport,
    [switch]$Verbose
)

$ErrorActionPreference = 'Stop'
$VerbosePreference = if ($Verbose) { 'Continue' } else { 'SilentlyContinue' }

$timestamp = Get-Date -Format 'yyyy-MM-dd_HH-mm-ss'
$logFile = "logs/validation_$timestamp.log"
$reportFile = "logs/validation-report_$timestamp.json"

if (-not (Test-Path 'logs')) { New-Item -ItemType Directory -Path 'logs' -Force | Out-Null }

function Write-Log {
    param([string]$Message, [string]$Level = 'INFO')
    $ts = Get-Date -Format 'HH:mm:ss'
    $entry = "[$ts] [$Level] $Message"
    Add-Content -Path $logFile -Value $entry
    if ($Verbose -or $Level -eq 'ERROR' -or $Level -eq 'SUCCESS') { Write-Host $entry }
}

function Validate-CustomFields {
    Write-Log 'Validating custom fields...'
    
    $validationResults = @{
        expectedFields = 25
        foundFields = 0
        fieldsByTier = @{ tier1 = 0; tier2 = 0; tier3 = 0; tier4 = 0; tier5 = 0 }
        missingFields = @()
        details = @()
    }
    
    $expectedFields = @(
        'Priority', 'Sprint', 'Effort', 'Component', 'DueDate',
        'AssignedTo', 'ProgressStatus', 'QAStatus', 'BlockedBy', 'TimeEstimate',
        'ReviewStatus', 'ReviewedBy', 'ApprovalRequired', 'RiskLevel', 'ComplianceCheck',
        'DeploymentEnvironment', 'DeploymentStatus', 'IntegrationPoints', 'DependsOn', 'DataMigration',
        'SuccessMetrics', 'UserImpact', 'PerformanceImpact', 'Documentation', 'ArchitectureDecision'
    )
    
    if (Test-Path '.fields') {
        $fieldFiles = Get-ChildItem '.fields' -Filter '*.json' -ErrorAction SilentlyContinue
        foreach ($file in $fieldFiles) {
            $field = Get-Content $file.FullName | ConvertFrom-Json
            $validationResults.foundFields++
            $validationResults.details += @{ name = $field.name; file = $file.Name; status = 'found' }
        }
    }
    
    foreach ($field in $expectedFields) {
        if ($validationResults.details.name -notcontains $field) {
            $validationResults.missingFields += $field
        }
    }
    
    $validationResults.isValid = ($validationResults.foundFields -ge 20)
    
    Write-Log "  Fields Found: $($validationResults.foundFields) / $($validationResults.expectedFields)" $(if ($validationResults.isValid) { 'SUCCESS' } else { 'ERROR' })
    if ($validationResults.missingFields.Count -gt 0) {
        Write-Log "  Missing Fields: $(($validationResults.missingFields) -join ', ')" 'ERROR'
    }
    
    return $validationResults
}

function Validate-Templates {
    Write-Log 'Validating phase templates...'
    
    $validationResults = @{
        expectedTemplates = 8
        foundTemplates = 0
        templates = @()
        missingTemplates = @()
        details = @()
    }
    
    $expectedPhases = @(
        'Phase 1', 'Phase 2', 'Phase 3', 'Phase 4', 
        'Phase 5', 'Phase 6', 'Phase 7', 'Phase 8'
    )
    
    if (Test-Path 'templates') {
        $templateFiles = Get-ChildItem 'templates' -Filter '*.json' -ErrorAction SilentlyContinue
        foreach ($file in $templateFiles) {
            $template = Get-Content $file.FullName | ConvertFrom-Json
            $validationResults.foundTemplates++
            $validationResults.details += @{
                name = $template.name
                phase = $template.phase
                file = $file.Name
                status = 'found'
            }
            $validationResults.templates += $template
        }
    }
    
    foreach ($phase in $expectedPhases) {
        if ($validationResults.templates.phase -notcontains $phase) {
            $validationResults.missingTemplates += $phase
        }
    }
    
    $validationResults.isValid = ($validationResults.foundTemplates -eq 8)
    
    Write-Log "  Templates Found: $($validationResults.foundTemplates) / $($validationResults.expectedTemplates)" $(if ($validationResults.isValid) { 'SUCCESS' } else { 'ERROR' })
    if ($validationResults.missingTemplates.Count -gt 0) {
        Write-Log "  Missing Phases: $(($validationResults.missingTemplates) -join ', ')" 'ERROR'
    }
    
    return $validationResults
}

function Validate-AutomationRules {
    Write-Log 'Validating automation rules...'
    
    $validationResults = @{
        expectedRules = 4
        foundRules = 0
        rules = @()
        details = @()
        validationsPassed = @()
        validationsFailed = @()
    }
    
    if (Test-Path '.automation') {
        $ruleFiles = Get-ChildItem '.automation' -Filter '*.json' -ErrorAction SilentlyContinue
        foreach ($file in $ruleFiles) {
            $rule = Get-Content $file.FullName | ConvertFrom-Json
            $validationResults.foundRules++
            
            # Validate rule structure
            $ruleValid = @{
                name = $rule.name
                id = $rule.id
                checks = @{}
            }
            
            # Check 1: Trigger valid
            $ruleValid.checks['triggerValid'] = $rule.trigger -in @('ItemAdded', 'FieldChanged', 'PullRequestMerged', 'IssueOpened')
            
            # Check 2: Action valid
            $ruleValid.checks['actionValid'] = $rule.action -in @('SetField', 'SendNotification', 'MultiAction', 'MoveColumn')
            
            # Check 3: Error handling valid
            $ruleValid.checks['errorHandlingValid'] = $rule.errorHandling -in @('Continue', 'Stop', 'Notify', 'Retry')
            
            # Check 4: Conditions count valid
            $ruleValid.checks['conditionsValid'] = ($rule.conditions.Count -le 5)
            
            $allChecksPass = $ruleValid.checks.Values | Where-Object { $_ -eq $true } | Measure-Object | Select-Object -ExpandProperty Count
            
            if ($allChecksPass -eq 4) {
                $validationResults.validationsPassed += $ruleValid.name
                $ruleValid.status = 'valid'
            }
            else {
                $validationResults.validationsFailed += $ruleValid.name
                $ruleValid.status = 'invalid'
            }
            
            $validationResults.details += $ruleValid
        }
    }
    
    $validationResults.isValid = ($validationResults.foundRules -eq 4) -and ($validationResults.validationsFailed.Count -eq 0)
    
    Write-Log "  Rules Found: $($validationResults.foundRules) / $($validationResults.expectedRules)" $(if ($validationResults.foundRules -eq 4) { 'SUCCESS' } else { 'ERROR' })
    Write-Log "  Rules Valid: $($validationResults.validationsPassed.Count), Invalid: $($validationResults.validationsFailed.Count)" $(if ($validationResults.validationsFailed.Count -eq 0) { 'SUCCESS' } else { 'ERROR' })
    
    return $validationResults
}

function Validate-Views {
    Write-Log 'Validating board views...'
    
    $validationResults = @{
        expectedViews = 6
        foundViews = 0
        views = @()
        details = @()
        validationsPassed = @()
        validationsFailed = @()
    }
    
    if (Test-Path '.views') {
        $viewFiles = Get-ChildItem '.views' -Filter '*.json' -ErrorAction SilentlyContinue
        foreach ($file in $viewFiles) {
            $view = Get-Content $file.FullName | ConvertFrom-Json
            $validationResults.foundViews++
            
            # Validate view structure
            $viewValid = @{
                name = $view.name
                id = $view.id
                checks = @{}
            }
            
            # Check 1: Layout valid
            $viewValid.checks['layoutValid'] = $view.layout -in @('table', 'board', 'roadmap')
            
            # Check 2: GroupBy specified
            $viewValid.checks['groupByValid'] = -not [string]::IsNullOrEmpty($view.groupBy)
            
            # Check 3: Sort order valid
            $viewValid.checks['sortValid'] = -not [string]::IsNullOrEmpty($view.sortBy.order)
            
            # Check 4: Filters structure valid
            $viewValid.checks['filtersValid'] = $view.filters -is [array]
            
            # Check 5: Field visibility configured
            $viewValid.checks['fieldsValid'] = $view.fieldVisibility.Count -gt 0
            
            $passedChecks = ($viewValid.checks.Values | Where-Object { $_ -eq $true } | Measure-Object | Select-Object -ExpandProperty Count)
            
            if ($passedChecks -ge 4) {
                $validationResults.validationsPassed += $viewValid.name
                $viewValid.status = 'valid'
            }
            else {
                $validationResults.validationsFailed += $viewValid.name
                $viewValid.status = 'invalid'
            }
            
            $validationResults.details += $viewValid
        }
    }
    
    $validationResults.isValid = ($validationResults.foundViews -eq 6) -and ($validationResults.validationsFailed.Count -eq 0)
    
    Write-Log "  Views Found: $($validationResults.foundViews) / $($validationResults.expectedViews)" $(if ($validationResults.foundViews -eq 6) { 'SUCCESS' } else { 'ERROR' })
    Write-Log "  Views Valid: $($validationResults.validationsPassed.Count), Invalid: $($validationResults.validationsFailed.Count)" $(if ($validationResults.validationsFailed.Count -eq 0) { 'SUCCESS' } else { 'ERROR' })
    
    return $validationResults
}

function Generate-ValidationReport {
    param(
        [hashtable]$FieldsValidation,
        [hashtable]$TemplatesValidation,
        [hashtable]$RulesValidation,
        [hashtable]$ViewsValidation
    )
    
    $report = @{
        timestamp = $timestamp
        projectNumber = $ProjectNumber
        organization = $OrganizationName
        validations = @{
            fields = $FieldsValidation
            templates = $TemplatesValidation
            rules = $RulesValidation
            views = $ViewsValidation
        }
        summary = @{
            allValid = ($FieldsValidation.isValid -and $TemplatesValidation.isValid -and $RulesValidation.isValid -and $ViewsValidation.isValid)
            fieldsValid = $FieldsValidation.isValid
            templatesValid = $TemplatesValidation.isValid
            rulesValid = $RulesValidation.isValid
            viewsValid = $ViewsValidation.isValid
        }
        completionPercentage = @{
            fields = [math]::Round(($FieldsValidation.foundFields / $FieldsValidation.expectedFields) * 100, 2)
            templates = [math]::Round(($TemplatesValidation.foundTemplates / $TemplatesValidation.expectedTemplates) * 100, 2)
            rules = [math]::Round(($RulesValidation.foundRules / $RulesValidation.expectedRules) * 100, 2)
            views = [math]::Round(($ViewsValidation.foundViews / $ViewsValidation.expectedViews) * 100, 2)
        }
    }
    
    if ($GenerateReport) {
        $report | ConvertTo-Json -Depth 10 | Set-Content -Path $reportFile
        Write-Log "Report saved: $reportFile" 'SUCCESS'
    }
    
    return $report
}

function Display-ValidationSummary {
    param([hashtable]$Report)
    
    Write-Host "`n" -ForegroundColor Cyan
    Write-Host "╔═══════════════════════════════════════════════════════╗" -ForegroundColor Cyan
    Write-Host "║          BOARD VALIDATION SUMMARY                     ║" -ForegroundColor Cyan
    Write-Host "╚═══════════════════════════════════════════════════════╝" -ForegroundColor Cyan
    
    Write-Host "`nValidation Results:" -ForegroundColor Green
    Write-Host "  Custom Fields: " -NoNewline
    Write-Host "$($Report.validations.fields.foundFields)/$($Report.validations.fields.expectedFields) ($($Report.completionPercentage.fields)%)" `
        -ForegroundColor $(if ($Report.validations.fields.isValid) { 'Green' } else { 'Red' })
    
    Write-Host "  Phase Templates: " -NoNewline
    Write-Host "$($Report.validations.templates.foundTemplates)/$($Report.validations.templates.expectedTemplates) ($($Report.completionPercentage.templates)%)" `
        -ForegroundColor $(if ($Report.validations.templates.isValid) { 'Green' } else { 'Red' })
    
    Write-Host "  Automation Rules: " -NoNewline
    Write-Host "$($Report.validations.rules.foundRules)/$($Report.validations.rules.expectedRules) ($($Report.completionPercentage.rules)%)" `
        -ForegroundColor $(if ($Report.validations.rules.isValid) { 'Green' } else { 'Red' })
    
    Write-Host "  Board Views: " -NoNewline
    Write-Host "$($Report.validations.views.foundViews)/$($Report.validations.views.expectedViews) ($($Report.completionPercentage.views)%)" `
        -ForegroundColor $(if ($Report.validations.views.isValid) { 'Green' } else { 'Red' })
    
    Write-Host "`nOverall Status: " -NoNewline
    if ($Report.summary.allValid) {
        Write-Host "✓ VALID" -ForegroundColor Green
    }
    else {
        Write-Host "✗ INVALID" -ForegroundColor Red
    }
    
    Write-Host "`n"
}

try {
    Write-Log '╔═══════════════════════════════════════════════════════╗'
    Write-Log '║         BOARD VALIDATION SCRIPT                      ║'
    Write-Log '╚═══════════════════════════════════════════════════════╝'
    
    Write-Log "Validating board configuration..."
    
    $fieldsVal = Validate-CustomFields
    $templatesVal = Validate-Templates
    $rulesVal = Validate-AutomationRules
    $viewsVal = Validate-Views
    
    $report = Generate-ValidationReport -FieldsValidation $fieldsVal -TemplatesValidation $templatesVal `
        -RulesValidation $rulesVal -ViewsValidation $viewsVal
    
    Display-ValidationSummary -Report $report
    
    Write-Log 'Validation complete' 'SUCCESS'
    
    $report | ConvertTo-Json -Depth 10
}
catch {
    Write-Log "Validation failed: $_" 'ERROR'
    exit 1
}
