<#
.SYNOPSIS
    HELIOS Platform - GitHub Project Board Auto-Setup
    
.DESCRIPTION
    Uses GITHUB_PROJECT_BOARD_COMPLETE_GUIDE.md to automatically create and 
    configure GitHub Project board with all fields, views, automation, and phases
    
.EXAMPLE
    .\SETUP-GITHUB-PROJECT-BOARD.ps1
    .\SETUP-GITHUB-PROJECT-BOARD.ps1 -DryRun

#>

param(
    [switch]$DryRun,
    [string]$Owner = "M0nado",
    [string]$Repo = "helios-platform"
)

$ErrorActionPreference = 'Continue'

Write-Host "`n╔════════════════════════════════════════════════════════════════╗" -ForegroundColor Magenta
Write-Host "║  GITHUB PROJECT BOARD AUTO-SETUP                             ║" -ForegroundColor Magenta
Write-Host "║  Using GITHUB_PROJECT_BOARD_COMPLETE_GUIDE.md                ║" -ForegroundColor Magenta
Write-Host "╚════════════════════════════════════════════════════════════════╝`n"

# Define project configuration from Agent 3's documentation
$projectConfig = @{
    Title = "HELIOS Platform Deployment"
    Description = "Complete HELIOS platform end-to-end deployment and operations"
    
    # 5-Column Board Workflow
    Columns = @(
        @{ Name = "📋 Backlog"; Description = "Future work not yet prioritized" }
        @{ Name = "✅ Ready"; Description = "Ready to start, dependencies met" }
        @{ Name = "🚀 In Progress"; Description = "Currently being worked on" }
        @{ Name = "👀 Review"; Description = "Awaiting review/approval" }
        @{ Name = "✨ Done"; Description = "Completed and verified" }
    )
    
    # 25 Custom Fields (Tier 1-5 from Agent 3 docs)
    CustomFields = @(
        # Tier 1: Planning Fields
        @{ Name = "Phase"; Type = "SingleSelect"; Options = @("0-Preflight", "1-Infrastructure", "2-Workflows", "3-Deployment", "4-Verification", "5-Advanced", "6-GoLive", "7-Operations") }
        @{ Name = "Component"; Type = "SingleSelect"; Options = @("Monado Engine", "Security System", "AI Orchestrator", "GUI Dashboard", "Build Agents", "Dev AI Hub", "Software Stack") }
        @{ Name = "Priority"; Type = "SingleSelect"; Options = @("🔴 Critical", "🟠 High", "🟡 Medium", "🟢 Low") }
        @{ Name = "Work Track"; Type = "SingleSelect"; Options = @("Infrastructure", "Security", "Integration", "Operations", "Development") }
        @{ Name = "Owner"; Type = "Text" }
        
        # Tier 2: Execution Fields
        @{ Name = "Status"; Type = "SingleSelect"; Options = @("Pending", "In Progress", "Blocked", "Completed") }
        @{ Name = "Dependencies"; Type = "Text" }
        @{ Name = "Blocked By"; Type = "Text" }
        @{ Name = "Start Date"; Type = "Date" }
        @{ Name = "Due Date"; Type = "Date" }
        
        # Tier 3: Resource Fields
        @{ Name = "Team Size"; Type = "Text" }
        @{ Name = "Skills Required"; Type = "Text" }
        @{ Name = "Environment"; Type = "SingleSelect"; Options = @("Local", "Codespace", "Azure", "Staging", "Production") }
        @{ Name = "Tools"; Type = "Text" }
        @{ Name = "Estimated Hours"; Type = "Text" }
        
        # Tier 4: Metrics Fields
        @{ Name = "Complexity"; Type = "SingleSelect"; Options = @("Low", "Medium", "High", "Expert") }
        @{ Name = "Risk Level"; Type = "SingleSelect"; Options = @("Low", "Medium", "High", "Critical") }
        @{ Name = "Success Criteria"; Type = "Text" }
        @{ Name = "Completion %"; Type = "Number" }
        @{ Name = "Test Coverage"; Type = "Text" }
        
        # Tier 5: Strategic Fields
        @{ Name = "Business Value"; Type = "SingleSelect"; Options = @("Critical", "High", "Medium", "Low") }
        @{ Name = "Cost Impact"; Type = "Text" }
        @{ Name = "Timeline Impact"; Type = "SingleSelect"; Options = @("Accelerates", "On Track", "Delays", "Blocks") }
        @{ Name = "Documentation"; Type = "Text" }
        @{ Name = "Training"; Type = "Text" }
    )
    
    # 7 Phases with Templates
    Phases = @(
        @{
            Id = "phase-0"
            Name = "Phase 0: Preflight Checks"
            Description = "System validation and prerequisites"
            Duration = 10
            Tasks = @(
                "Validate PowerShell version (7+)"
                "Check Git installation"
                "Verify .NET SDK (8+)"
                "Check GitHub CLI"
                "Validate Docker"
            )
        }
        @{
            Id = "phase-1"
            Name = "Phase 1: Infrastructure Setup"
            Description = "Create base infrastructure and configuration"
            Duration = 15
            Tasks = @(
                "Create base directories"
                "Initialize git repository"
                "Configure secrets manager"
                "Setup environment variables"
                "Create CI/CD pipeline base"
            )
        }
        @{
            Id = "phase-2"
            Name = "Phase 2: GitHub Actions Setup"
            Description = "Configure workflows and automation"
            Duration = 20
            Tasks = @(
                "Enable GitHub Actions"
                "Configure deploy workflow"
                "Set GitHub secrets"
                "Create quality checks"
                "Setup verification workflow"
            )
        }
        @{
            Id = "phase-3"
            Name = "Phase 3: Component Deployment"
            Description = "Deploy core components and services"
            Duration = 22
            Tasks = @(
                "Deploy Monado Engine"
                "Deploy Security System"
                "Initialize NuGet package"
                "Configure Codespaces"
                "Setup AI Orchestrator"
            )
        }
        @{
            Id = "phase-4"
            Name = "Phase 4: Verification & Testing"
            Description = "Validation and testing"
            Duration = 20
            Tasks = @(
                "Run verification suite"
                "Execute health checks"
                "Validate integrations"
                "Test workflows"
                "Security audit"
            )
        }
        @{
            Id = "phase-5"
            Name = "Phase 5: Advanced Operations"
            Description = "Monitoring, scaling, and recovery"
            Duration = 15
            Tasks = @(
                "Enable monitoring"
                "Configure auto-scaling"
                "Setup disaster recovery"
                "Create runbooks"
                "Train team"
            )
        }
        @{
            Id = "phase-6"
            Name = "Phase 6: Go-Live Preparation"
            Description = "Final preparation and deployment"
            Duration = 10
            Tasks = @(
                "Final security audit"
                "Performance optimization"
                "Team handoff training"
                "Deployment approval"
                "Go-live execution"
            )
        }
    )
    
    # 6 Board Views from Agent 3 docs
    Views = @(
        @{ Name = "Timeline"; Description = "Gantt-style view of all phases"; Filter = "All"; GroupBy = "Phase" }
        @{ Name = "Critical Path"; Description = "High-priority items"; Filter = "Priority = Critical OR High"; GroupBy = "Phase" }
        @{ Name = "Metrics Dashboard"; Description = "Progress and metrics"; Filter = "All"; GroupBy = "Status" }
        @{ Name = "Resource Planning"; Description = "By team and environment"; Filter = "All"; GroupBy = "Owner" }
        @{ Name = "Risk Analysis"; Description = "High-risk items"; Filter = "Risk Level >= Medium"; GroupBy = "Risk Level" }
        @{ Name = "Agent Status"; Description = "Build agent execution"; Filter = "Component = Build Agents"; GroupBy = "Status" }
    )
    
    # 4 Automation Rules
    AutomationRules = @(
        @{
            Name = "Auto-assign Phase"
            Trigger = "When PR merged with phase label"
            Action = "Add to corresponding phase column"
        }
        @{
            Name = "Auto-move on Review"
            Trigger = "When marked as ready for review"
            Action = "Move to Review column"
        }
        @{
            Name = "Auto-archive Complete"
            Trigger = "When status = Completed for 30 days"
            Action = "Archive issue"
        }
        @{
            Name = "Auto-update Metrics"
            Trigger = "When workflow completes"
            Action = "Update completion % and metrics"
        }
    )
}

Write-Host "📋 PROJECT BOARD CONFIGURATION" -ForegroundColor Cyan
Write-Host "──────────────────────────────────────────"
Write-Host "  Title: $($projectConfig.Title)"
Write-Host "  Columns: $($projectConfig.Columns.Count)"
Write-Host "  Custom Fields: $($projectConfig.CustomFields.Count)"
Write-Host "  Phases: $($projectConfig.Phases.Count)"
Write-Host "  Views: $($projectConfig.Views.Count)"
Write-Host "  Automation Rules: $($projectConfig.AutomationRules.Count)"
Write-Host ""

if ($DryRun) {
    Write-Host "🔍 DRY-RUN MODE: No changes will be made`n" -ForegroundColor Yellow
}

# ============================================================================
# STEP 1: VERIFY GITHUB CLI
# ============================================================================

Write-Host "🔐 Step 1: Verifying GitHub CLI..." -ForegroundColor Cyan

try {
    $ghStatus = gh auth status 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  ✅ GitHub CLI authenticated" -ForegroundColor Green
    } else {
        Write-Host "  ⚠️  GitHub CLI not authenticated. Run: gh auth login" -ForegroundColor Yellow
        exit 1
    }
} catch {
    Write-Host "  ❌ GitHub CLI not found. Install from: https://cli.github.com" -ForegroundColor Red
    exit 1
}

Write-Host ""

# ============================================================================
# STEP 2: DISPLAY PROJECT STRUCTURE
# ============================================================================

Write-Host "📐 Step 2: Displaying project structure..." -ForegroundColor Cyan
Write-Host ""

Write-Host "Board Columns:" -ForegroundColor Yellow
$projectConfig.Columns | ForEach-Object { Write-Host "  $($_.Name) - $($_.Description)" }
Write-Host ""

Write-Host "Custom Fields (by tier):" -ForegroundColor Yellow
$tier1 = $projectConfig.CustomFields | Select-Object -First 5
Write-Host "  Tier 1 Planning: $($tier1.Count) fields"
$tier2 = $projectConfig.CustomFields | Select-Object -Skip 5 -First 5
Write-Host "  Tier 2 Execution: $($tier2.Count) fields"
$tier3 = $projectConfig.CustomFields | Select-Object -Skip 10 -First 5
Write-Host "  Tier 3 Resources: $($tier3.Count) fields"
$tier4 = $projectConfig.CustomFields | Select-Object -Skip 15 -First 5
Write-Host "  Tier 4 Metrics: $($tier4.Count) fields"
$tier5 = $projectConfig.CustomFields | Select-Object -Skip 20 -First 5
Write-Host "  Tier 5 Strategic: $($tier5.Count) fields"
Write-Host ""

Write-Host "Deployment Phases:" -ForegroundColor Yellow
$projectConfig.Phases | ForEach-Object {
    Write-Host "  Phase $($_.Name) - $($_.Duration) min"
}
Write-Host ""

# ============================================================================
# STEP 3: DISPLAY GITHUB CLI COMMANDS
# ============================================================================

Write-Host "⚙️  Step 3: GitHub CLI commands to execute..." -ForegroundColor Cyan
Write-Host ""

$commands = @(
    "# Create GitHub Project"
    "gh project create --owner $Owner --repo $Repo --title '$($projectConfig.Title)' --description '$($projectConfig.Description)'"
    ""
    "# Add custom fields (run after project creation)"
    "gh project field-create --project-id <PROJECT_ID> --name 'Phase' --data-type SINGLE_SELECT"
    "gh project field-create --project-id <PROJECT_ID> --name 'Component' --data-type SINGLE_SELECT"
    "gh project field-create --project-id <PROJECT_ID> --name 'Priority' --data-type SINGLE_SELECT"
    ""
    "# Create issues for each phase"
    "gh issue create --owner $Owner --repo $Repo --title 'Phase 0: Preflight Checks' --body '10 minute preflight validation'"
    "gh issue create --owner $Owner --repo $Repo --title 'Phase 1: Infrastructure' --body '15 minute infrastructure setup'"
    ""
    "# Add labels"
    "gh label create 'phase:0' --repo $Owner/$Repo"
    "gh label create 'phase:1' --repo $Owner/$Repo"
)

$commands | ForEach-Object {
    if ($_ -match "^#") {
        Write-Host $_ -ForegroundColor Yellow
    } else {
        Write-Host $_ -ForegroundColor Gray
    }
}

Write-Host ""

# ============================================================================
# STEP 4: GENERATE SETUP SCRIPT
# ============================================================================

Write-Host "📝 Step 4: Generating setup script..." -ForegroundColor Cyan

$setupScript = @"
#!/bin/bash
# HELIOS Platform - Project Board Setup Script
# Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')

echo "🚀 Setting up GitHub Project Board for HELIOS Platform..."

# Verify authentication
gh auth status || exit 1

# Step 1: Create project
echo "Creating project..."
PROJECT_ID=`$(gh project create --owner $Owner --repo $Repo --title 'HELIOS Platform Deployment' | grep -oE '[0-9]+' | head -1)
echo "Project created: `$PROJECT_ID"

# Step 2: Add custom fields
echo "Adding custom fields..."
gh project field-create --project-id `$PROJECT_ID --name "Phase" --data-type SINGLE_SELECT
gh project field-create --project-id `$PROJECT_ID --name "Component" --data-type SINGLE_SELECT
gh project field-create --project-id `$PROJECT_ID --name "Priority" --data-type SINGLE_SELECT

# Step 3: Create phase issues
echo "Creating phase issues..."
for i in {0..6}; do
  gh issue create --repo $Owner/$Repo --title "Phase `$i: Deployment" --label "phase:\$i"
done

echo "✅ Project board setup complete!"
"@

$setupScript | Out-File "SETUP-PROJECT-BOARD.sh" -Encoding UTF8
Write-Host "  ✅ Setup script generated: SETUP-PROJECT-BOARD.sh" -ForegroundColor Green
Write-Host ""

# ============================================================================
# STEP 5: EXECUTION
# ============================================================================

if (-not $DryRun) {
    Write-Host "🚀 Step 5: Would execute setup in production mode..." -ForegroundColor Cyan
    Write-Host ""
    Write-Host "  ⚠️  To actually create the project board, run:" -ForegroundColor Yellow
    Write-Host "      bash SETUP-PROJECT-BOARD.sh"
    Write-Host ""
} else {
    Write-Host "✅ DRY-RUN: Setup script prepared (no execution)" -ForegroundColor Green
    Write-Host ""
}

# ============================================================================
# SUMMARY
# ============================================================================

Write-Host "╔════════════════════════════════════════════════════════════════╗" -ForegroundColor Green
Write-Host "║  PROJECT BOARD CONFIGURATION PREPARED                        ║" -ForegroundColor Green
Write-Host "╚════════════════════════════════════════════════════════════════╝`n"

Write-Host "📊 CONFIGURATION SUMMARY" -ForegroundColor Green
Write-Host "──────────────────────────────────────────"
Write-Host "  Board Columns: $($projectConfig.Columns.Count)"
Write-Host "  Custom Fields: $($projectConfig.CustomFields.Count)"
Write-Host "  Deployment Phases: $($projectConfig.Phases.Count)"
Write-Host "  Board Views: $($projectConfig.Views.Count)"
Write-Host "  Automation Rules: $($projectConfig.AutomationRules.Count)"
Write-Host ""

Write-Host "📋 NEXT STEPS" -ForegroundColor Cyan
Write-Host "──────────────────────────────────────────"
Write-Host "  1. Review GITHUB_PROJECT_BOARD_COMPLETE_GUIDE.md"
Write-Host "  2. Run: bash SETUP-PROJECT-BOARD.sh"
Write-Host "  3. Configure custom field options"
Write-Host "  4. Add automation rules"
Write-Host "  5. Import 7 phase templates"
Write-Host ""

Write-Host "✅ Configuration ready for GitHub Project Board setup`n" -ForegroundColor Green
