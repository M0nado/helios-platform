<#
.SYNOPSIS
    Creates 8 phase templates for HELIOS Platform board
.DESCRIPTION
    Sets up comprehensive phase templates with fields, defaults, and acceptance criteria
.PARAMETER GitHubToken
    GitHub Personal Access Token
.PARAMETER ProjectNumber
    Project number to configure
.PARAMETER OrganizationName
    Organization name
.PARAMETER DryRun
    Preview mode
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
    
    [switch]$DryRun,
    [switch]$Verbose
)

$ErrorActionPreference = 'Stop'
$VerbosePreference = if ($Verbose) { 'Continue' } else { 'SilentlyContinue' }

$timestamp = Get-Date -Format 'yyyy-MM-dd_HH-mm-ss'
$logFile = "logs/templates-setup_$timestamp.log"
$reportFile = "logs/templates-report_$timestamp.json"

if (-not (Test-Path 'logs')) { New-Item -ItemType Directory -Path 'logs' -Force | Out-Null }

function Write-Log {
    param([string]$Message, [string]$Level = 'INFO')
    $ts = Get-Date -Format 'HH:mm:ss'
    $entry = "[$ts] [$Level] $Message"
    Add-Content -Path $logFile -Value $entry
    if ($Verbose -or $Level -eq 'ERROR' -or $Level -eq 'SUCCESS') { Write-Host $entry }
}

# 8 Phase Templates Definition
$templates = @(
    @{
        name = 'Requirements & Planning'
        phase = 'Phase 1'
        description = 'Initial requirements gathering and planning'
        fields = @{
            Priority = 'Medium'
            Sprint = 'Backlog'
            Effort = 'M'
        }
        acceptanceCriteria = @(
            'Requirements document completed',
            'Stakeholder sign-off obtained',
            'Technical feasibility assessed',
            'Resource allocation confirmed'
        )
        successMetrics = @(
            'Requirements completeness score > 90%',
            'Stakeholder satisfaction > 85%',
            'Risk identification rate > 95%'
        )
        duration = '5-10 days'
    },
    @{
        name = 'Design & Architecture'
        phase = 'Phase 2'
        description = 'System design and architecture planning'
        fields = @{
            Priority = 'High'
            Sprint = 'Sprint 1'
            Effort = 'L'
            ReviewStatus = 'Not Reviewed'
            ArchitectureDecision = 'Required'
        }
        acceptanceCriteria = @(
            'Architecture diagram created',
            'Design review completed',
            'Tech stack validated',
            'Database schema defined'
        )
        successMetrics = @(
            'Design review approval rate 100%',
            'Architecture complexity score acceptable',
            'Schema efficiency rating > 4/5'
        )
        duration = '7-14 days'
    },
    @{
        name = 'Development'
        phase = 'Phase 3'
        description = 'Implementation phase'
        fields = @{
            Priority = 'High'
            ProgressStatus = 'Not Started'
            Component = 'API'
            AssignedTo = 'Development Team'
            TimeEstimate = '40-60'
        }
        acceptanceCriteria = @(
            'Code written and reviewed',
            'Unit tests achieve > 80% coverage',
            'Code style guide compliance',
            'Performance benchmarks met'
        )
        successMetrics = @(
            'Code coverage > 85%',
            'Build success rate 100%',
            'Zero critical bugs',
            'Performance improvement > 10%'
        )
        duration = '10-20 days'
    },
    @{
        name = 'Testing & QA'
        phase = 'Phase 4'
        description = 'Quality assurance and testing'
        fields = @{
            QAStatus = 'Pending QA'
            ComplianceCheck = 'Pending'
            RiskLevel = 'Medium'
            ApprovalRequired = 'Yes'
        }
        acceptanceCriteria = @(
            'All test cases executed',
            'Bug tracking completed',
            'Security scan passed',
            'Performance tested in staging'
        )
        successMetrics = @(
            'Test pass rate > 95%',
            'Zero critical/high bugs',
            'Security score > 90',
            'Response time < threshold'
        )
        duration = '5-10 days'
    },
    @{
        name = 'Code Review & Integration'
        phase = 'Phase 5'
        description = 'Peer review and system integration'
        fields = @{
            ReviewStatus = 'In Review'
            ReviewedBy = 'Code Review Team'
            ApprovalRequired = 'Yes'
            DeploymentEnvironment = 'Development'
        }
        acceptanceCriteria = @(
            'Peer review completed',
            'Integration tests pass',
            'No conflicts in merge',
            'Documentation updated'
        )
        successMetrics = @(
            'Review feedback incorporated 100%',
            'Integration test pass rate 100%',
            'Zero merge conflicts',
            'Documentation completeness 100%'
        )
        duration = '3-5 days'
    },
    @{
        name = 'Pre-Production Staging'
        phase = 'Phase 6'
        description = 'Staging environment deployment and validation'
        fields = @{
            DeploymentEnvironment = 'Staging'
            DeploymentStatus = 'Deployed to Staging'
            DataMigration = 'In Progress'
            DocumentationStatus = 'Complete'
        }
        acceptanceCriteria = @(
            'Deployed to staging successfully',
            'Data migration validated',
            'User acceptance testing scheduled',
            'Rollback plan documented'
        )
        successMetrics = @(
            'Staging stability score > 98%',
            'UAT readiness 100%',
            'Performance parity with prod spec',
            'Security validation complete'
        )
        duration = '3-5 days'
    },
    @{
        name = 'Production Deployment'
        phase = 'Phase 7'
        description = 'Production release and rollout'
        fields = @{
            DeploymentEnvironment = 'Production'
            DeploymentStatus = 'Deployed to Prod'
            ApprovalRequired = 'Yes'
            RiskLevel = 'Critical'
        }
        acceptanceCriteria = @(
            'Production deployment successful',
            'Monitoring and alerting active',
            'Post-deployment smoke tests pass',
            'Team communication completed'
        )
        successMetrics = @(
            'Deployment success 100%',
            'Zero production incidents P1',
            'User adoption rate > 90%',
            'System performance maintained'
        )
        duration = '1-2 days'
    },
    @{
        name = 'Post-Launch & Monitoring'
        phase = 'Phase 8'
        description = 'Post-deployment monitoring and support'
        fields = @{
            DeploymentStatus = 'Deployed to Prod'
            UserImpact = 'High'
            PerformanceImpact = 'Positive'
            SuccessMetrics = 'Required'
        }
        acceptanceCriteria = @(
            '24/7 monitoring active',
            'User feedback collected',
            'Performance metrics within SLA',
            'Issue resolution time < 1 hour'
        )
        successMetrics = @(
            'System uptime 99.9%+',
            'User satisfaction > 90%',
            'Support ticket volume normal',
            'All success metrics achieved'
        )
        duration = 'Ongoing (14+ days minimum)'
    }
)

function Generate-TemplateDocument {
    param([array]$Templates)
    
    $doc = @"
# HELIOS Platform - 8 Phase Templates

Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')

## Phase Overview

$(foreach ($t in $Templates) {
@"
### $($t.name)
- **Phase**: $($t.phase)
- **Duration**: $($t.duration)
- **Description**: $($t.description)

#### Acceptance Criteria
$(foreach ($ac in $t.acceptanceCriteria) { "- $ac`n" })

#### Success Metrics
$(foreach ($sm in $t.successMetrics) { "- $sm`n" })

---
"@
})

## Template Variables
All templates support the following field substitutions:
- {{TEAM_MEMBER}} - Assigned team member
- {{TARGET_DATE}} - Due date
- {{SPRINT_NAME}} - Sprint identifier
- {{ENVIRONMENT}} - Deployment environment

"@
    
    $doc | Set-Content -Path "logs/template-documentation_$timestamp.md"
    Write-Log "Template documentation generated" 'SUCCESS'
}

function Create-Template {
    param([hashtable]$Template)
    
    Write-Log "Creating template: $($Template.name)"
    
    if ($DryRun) {
        Write-Log "  [DRY RUN] Would create: $($Template.name)" 'INFO'
        return @{ name = $Template.name; status = 'dry-run' }
    }
    
    # In real implementation, would save template to version control
    $templatePath = "templates/phase_$($Template.phase.Replace(' ', '_')).json"
    
    $templateObj = @{
        name = $Template.name
        phase = $Template.phase
        description = $Template.description
        fields = $Template.fields
        acceptanceCriteria = $Template.acceptanceCriteria
        successMetrics = $Template.successMetrics
        duration = $Template.duration
        createdAt = Get-Date -Format 'o'
    }
    
    try {
        if (-not (Test-Path 'templates')) { New-Item -ItemType Directory -Path 'templates' -Force | Out-Null }
        $templateObj | ConvertTo-Json -Depth 10 | Set-Content -Path $templatePath
        Write-Log "  Template saved to: $templatePath" 'SUCCESS'
        return @{ name = $Template.name; status = 'created'; path = $templatePath }
    }
    catch {
        Write-Log "  Failed to create template: $_" 'ERROR'
        return @{ name = $Template.name; status = 'failed'; error = $_ }
    }
}

try {
    Write-Log '=== Starting Phase Templates Setup ===' 'INFO'
    
    if ($DryRun) {
        Write-Log 'DRY RUN MODE - No changes applied' 'INFO'
    }
    
    $createdTemplates = @()
    
    Write-Log "Creating $($templates.Count) phase templates..."
    
    $templates | ForEach-Object {
        $result = Create-Template -Template $_
        $createdTemplates += $result
    }
    
    Generate-TemplateDocument -Templates $templates
    
    $report = @{
        timestamp = $timestamp
        totalTemplates = $templates.Count
        created = ($createdTemplates | Where-Object { $_.status -eq 'created' }).Count
        failed = ($createdTemplates | Where-Object { $_.status -eq 'failed' }).Count
        templates = $createdTemplates
    }
    
    $report | ConvertTo-Json -Depth 10 | Set-Content -Path $reportFile
    
    Write-Log '=== Phase Templates Setup Complete ===' 'SUCCESS'
    Write-Log "Created: $($report.created), Failed: $($report.failed)" 'INFO'
    
    $report | ConvertTo-Json -Depth 10
}
catch {
    Write-Log "Script failed: $_" 'ERROR'
    exit 1
}
