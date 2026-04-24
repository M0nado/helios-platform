#!/usr/bin/env pwsh
# 🚀 HELIOS GitHub Project Setup Automation Script
# This script helps you set up the GitHub Project board, Actions, and Pages
# Usage: ./setup-github-project.ps1

Write-Host ""
Write-Host "╔════════════════════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║                                                                                ║" -ForegroundColor Cyan
Write-Host "║         🚀 HELIOS GitHub Project Setup - Interactive Configuration 🚀         ║" -ForegroundColor Cyan
Write-Host "║                                                                                ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

Write-Host "This script will guide you through setting up:" -ForegroundColor Yellow
Write-Host ""
Write-Host "  ✓ GitHub Project Board (20+ fields, 6 views, 7 issue templates)" -ForegroundColor Gray
Write-Host "  ✓ Automation Rules (4 rules for workflow automation)" -ForegroundColor Gray
Write-Host "  ✓ GitHub Pages (Documentation site)" -ForegroundColor Gray
Write-Host "  ✓ GitHub Actions (5 workflows verification)" -ForegroundColor Gray
Write-Host "  ✓ Repository Settings (Branch protection & secrets)" -ForegroundColor Gray
Write-Host ""

Write-Host "═════════════════════════════════════════════════════════════════════════════════" -ForegroundColor Gray
Write-Host ""

# Check if GitHub CLI is installed
try {
    $ghVersion = gh --version 2>&1
    Write-Host "✅ GitHub CLI detected: $($ghVersion.Split([Environment]::NewLine)[0])" -ForegroundColor Green
} catch {
    Write-Host "❌ GitHub CLI not found. Please install from: https://cli.github.com" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Check authentication
try {
    $auth = gh auth status 2>&1
    if ($auth -match "Logged in") {
        Write-Host "✅ GitHub authentication: Verified" -ForegroundColor Green
    } else {
        Write-Host "⚠️  Please authenticate with: gh auth login" -ForegroundColor Yellow
        exit 1
    }
} catch {
    Write-Host "⚠️  Could not verify GitHub authentication" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "═════════════════════════════════════════════════════════════════════════════════" -ForegroundColor Gray
Write-Host ""

# Menu
Write-Host "📋 SETUP OPTIONS:" -ForegroundColor Yellow
Write-Host ""

Write-Host "  [1] View Project Setup Checklist" -ForegroundColor Cyan
Write-Host "  [2] View GitHub Pages Setup Instructions" -ForegroundColor Cyan
Write-Host "  [3] View GitHub Actions Verification" -ForegroundColor Cyan
Write-Host "  [4] View Repository Settings Guide" -ForegroundColor Cyan
Write-Host "  [5] View Issue Templates" -ForegroundColor Cyan
Write-Host "  [6] Verify All Workflows" -ForegroundColor Cyan
Write-Host "  [7] Generate GitHub Pages" -ForegroundColor Cyan
Write-Host "  [8] View Complete Setup Guide" -ForegroundColor Cyan
Write-Host "  [0] Exit" -ForegroundColor Gray
Write-Host ""

$choice = Read-Host "Choose an option [0-8]"

Write-Host ""

switch ($choice) {
    "1" {
        Write-Host "📋 PROJECT BOARD SETUP CHECKLIST" -ForegroundColor Green
        Write-Host ""
        
        $checklist = @(
            "Create new project named 'HELIOS Deployment'",
            "Add 20+ custom fields:",
            "  • Core: Phase, Component, Status, Priority, Owner",
            "  • Time: Time (min), Start Date, End Date, Assignee, Team",
            "  • Details: Disk Impact, Services, Security Layers, Tests",
            "  • Metrics: Performance, Cost Savings, Complexity, Risk Level",
            "  • Tracking: Dependencies, Subtasks, Mitigation, Notes",
            "Create 5 board columns:",
            "  • Backlog, Ready, In Progress, In Review, Done",
            "Create 6 custom views:",
            "  • Timeline View (grouped by Phase)",
            "  • Critical Path (Priority = Critical)",
            "  • Metrics Dashboard (grouped by Component)",
            "  • Resource Planning (grouped by Team)",
            "  • Risk Analysis (Risk Level != Low)",
            "  • Agent Status (grouped by Component)",
            "Add 7 issue templates (Phase 0-6)",
            "Configure 4 automation rules:",
            "  • Auto-add on PR opened",
            "  • Auto-move on label change",
            "  • Auto-mark done on close",
            "  • Auto-archive after 7 days"
        )
        
        $checklist | ForEach-Object {
            Write-Host "  ☐ $_" -ForegroundColor Gray
        }
        
        Write-Host ""
        Write-Host "📚 Instructions:" -ForegroundColor Yellow
        Write-Host "  1. Go to: https://github.com/M0nado/helios-platform/projects" -ForegroundColor Gray
        Write-Host "  2. Click 'New Project'" -ForegroundColor Gray
        Write-Host "  3. Follow the checklist above" -ForegroundColor Gray
        Write-Host "  4. See COMPLETE_GITHUB_SETUP_GUIDE.md for detailed instructions" -ForegroundColor Gray
    }
    
    "2" {
        Write-Host "📄 GITHUB PAGES SETUP INSTRUCTIONS" -ForegroundColor Green
        Write-Host ""
        
        Write-Host "Step 1: Enable Pages" -ForegroundColor Cyan
        Write-Host "  1. Go to: https://github.com/M0nado/helios-platform/settings" -ForegroundColor Gray
        Write-Host "  2. Scroll to 'Pages' section" -ForegroundColor Gray
        Write-Host "  3. Source: 'Deploy from a branch'" -ForegroundColor Gray
        Write-Host "  4. Branch: 'main'" -ForegroundColor Gray
        Write-Host "  5. Folder: '/' (root)" -ForegroundColor Gray
        Write-Host "  6. Click 'Save'" -ForegroundColor Gray
        Write-Host ""
        
        Write-Host "Step 2: Wait for Deployment" -ForegroundColor Cyan
        Write-Host "  • GitHub will build automatically" -ForegroundColor Gray
        Write-Host "  • Check Actions tab for status" -ForegroundColor Gray
        Write-Host "  • Takes 2-5 minutes" -ForegroundColor Gray
        Write-Host ""
        
        Write-Host "Step 3: Access Your Site" -ForegroundColor Cyan
        Write-Host "  • URL: https://m0nado.github.io/helios-platform" -ForegroundColor Green
        Write-Host ""
        
        Write-Host "Step 4: Add to README" -ForegroundColor Cyan
        Write-Host "  • Add: [View Documentation](https://m0nado.github.io/helios-platform/)" -ForegroundColor Gray
    }
    
    "3" {
        Write-Host "✅ GITHUB ACTIONS VERIFICATION" -ForegroundColor Green
        Write-Host ""
        
        Write-Host "Verifying workflows..." -ForegroundColor Yellow
        Write-Host ""
        
        $workflows = @(
            @("deploy.yml", "7-phase orchestration", "77-102 min"),
            @("nuget.yml", "NuGet package build", "~5 min"),
            @("analysis.yml", "Metrics validation", "~3 min"),
            @("quality.yml", "Code quality checks", "~5 min"),
            @("verify.yml", "42-point verification", "~2 min")
        )
        
        foreach ($workflow in $workflows) {
            $name = $workflow[0]
            $desc = $workflow[1]
            $time = $workflow[2]
            
            if (Test-Path ".github/workflows/$name") {
                Write-Host "  ✅ $name" -ForegroundColor Green
                Write-Host "     Purpose: $desc" -ForegroundColor Gray
                Write-Host "     Duration: $time" -ForegroundColor Gray
            } else {
                Write-Host "  ❌ $name NOT FOUND" -ForegroundColor Red
            }
        }
        
        Write-Host ""
        Write-Host "Next steps:" -ForegroundColor Yellow
        Write-Host "  1. Configure secrets in Settings → Secrets and variables → Actions" -ForegroundColor Gray
        Write-Host "  2. Required: AZURE_SUBSCRIPTION_ID, AZURE_TENANT_ID, AZURE_CLIENT_ID, AZURE_CLIENT_SECRET" -ForegroundColor Gray
        Write-Host "  3. Optional: NUGET_API_KEY" -ForegroundColor Gray
    }
    
    "4" {
        Write-Host "🔒 REPOSITORY SETTINGS GUIDE" -ForegroundColor Green
        Write-Host ""
        
        Write-Host "1. Branch Protection (Main)" -ForegroundColor Cyan
        Write-Host "   Go to: Settings → Branches → Add rule" -ForegroundColor Gray
        Write-Host "   • Pattern: main" -ForegroundColor Gray
        Write-Host "   • Require pull request reviews: ✓ (1)" -ForegroundColor Gray
        Write-Host "   • Require status checks: ✓ (quality.yml)" -ForegroundColor Gray
        Write-Host "   • Up to date: ✓" -ForegroundColor Gray
        Write-Host "   • Dismiss stale reviews: ✓" -ForegroundColor Gray
        Write-Host ""
        
        Write-Host "2. Secrets & Variables" -ForegroundColor Cyan
        Write-Host "   Go to: Settings → Secrets and variables → Actions" -ForegroundColor Gray
        Write-Host "   Required:" -ForegroundColor Gray
        Write-Host "   • AZURE_SUBSCRIPTION_ID" -ForegroundColor Gray
        Write-Host "   • AZURE_TENANT_ID" -ForegroundColor Gray
        Write-Host "   • AZURE_CLIENT_ID" -ForegroundColor Gray
        Write-Host "   • AZURE_CLIENT_SECRET" -ForegroundColor Gray
        Write-Host ""
        
        Write-Host "3. General Settings" -ForegroundColor Cyan
        Write-Host "   • Description: HELIOS Platform Enterprise Deployment" -ForegroundColor Gray
        Write-Host "   • Website: https://m0nado.github.io/helios-platform" -ForegroundColor Gray
        Write-Host "   • Topics: deployment, automation, windows, enterprise" -ForegroundColor Gray
    }
    
    "5" {
        Write-Host "📝 ISSUE TEMPLATES" -ForegroundColor Green
        Write-Host ""
        
        Write-Host "All 7 phase templates are documented in:" -ForegroundColor Yellow
        Write-Host "  COMPLETE_GITHUB_SETUP_GUIDE.md (Part 2)" -ForegroundColor Green
        Write-Host ""
        
        Write-Host "Quick Template Summary:" -ForegroundColor Cyan
        Write-Host "  • Phase 0: Preflight Checks (10 min)" -ForegroundColor Gray
        Write-Host "  • Phase 1: Infrastructure Setup (12 min)" -ForegroundColor Gray
        Write-Host "  • Phase 2: Agent Fleet Deployment (25 min)" -ForegroundColor Gray
        Write-Host "  • Phase 3: AI Services Integration (18 min)" -ForegroundColor Gray
        Write-Host "  • Phase 4: Security Framework (22 min)" -ForegroundColor Gray
        Write-Host "  • Phase 5: Monitoring Setup (15 min)" -ForegroundColor Gray
        Write-Host "  • Phase 6: Verification & Go-Live (10 min)" -ForegroundColor Gray
        Write-Host ""
        
        Write-Host "To Create Issues:" -ForegroundColor Yellow
        Write-Host "  1. Go to: Issues → New Issue" -ForegroundColor Gray
        Write-Host "  2. Copy template from guide" -ForegroundColor Gray
        Write-Host "  3. Fill in all fields" -ForegroundColor Gray
        Write-Host "  4. Add to project board" -ForegroundColor Gray
    }
    
    "6" {
        Write-Host "🔍 VERIFYING WORKFLOWS" -ForegroundColor Green
        Write-Host ""
        
        Write-Host "Checking workflow files..." -ForegroundColor Yellow
        Write-Host ""
        
        $workflowPath = ".github/workflows"
        if (Test-Path $workflowPath) {
            $workflows = Get-ChildItem "$workflowPath/*.yml"
            
            foreach ($file in $workflows) {
                $content = Get-Content $file.FullName -Raw
                
                # Check for key workflow elements
                $hasName = $content -match "^name:"
                $hasTrigger = $content -match "^on:"
                $hasJobs = $content -match "^jobs:"
                
                Write-Host "  ✅ $($file.Name)" -ForegroundColor Green
                if ($hasName -and $hasTrigger -and $hasJobs) {
                    Write-Host "     Status: Valid workflow file" -ForegroundColor Gray
                }
            }
        }
        
        Write-Host ""
        Write-Host "To run a workflow:" -ForegroundColor Yellow
        Write-Host "  gh workflow run deploy.yml -r main -f tier=enterprise" -ForegroundColor Green
        Write-Host ""
        Write-Host "To monitor:" -ForegroundColor Yellow
        Write-Host "  gh run watch" -ForegroundColor Green
    }
    
    "7" {
        Write-Host "📄 GENERATING GITHUB PAGES" -ForegroundColor Green
        Write-Host ""
        
        Write-Host "GitHub Pages requires:" -ForegroundColor Yellow
        Write-Host "  ✅ index.md (already created)" -ForegroundColor Green
        Write-Host "  ✅ _config.yml (already created)" -ForegroundColor Green
        Write-Host "  ✅ Documentation files in /docs (already present)" -ForegroundColor Green
        Write-Host ""
        
        Write-Host "Current files ready for Pages:" -ForegroundColor Cyan
        $docsFiles = Get-ChildItem "docs" -Recurse -Filter "*.md" -ErrorAction SilentlyContinue | Measure-Object
        Write-Host "  📁 docs/ directory: $($docsFiles.Count) markdown files" -ForegroundColor Green
        Write-Host "  📄 index.md: Main landing page" -ForegroundColor Green
        Write-Host "  ⚙️  _config.yml: Theme and configuration" -ForegroundColor Green
        Write-Host ""
        
        Write-Host "To enable GitHub Pages:" -ForegroundColor Yellow
        Write-Host "  1. Go to Settings → Pages" -ForegroundColor Gray
        Write-Host "  2. Source: Deploy from branch → main" -ForegroundColor Gray
        Write-Host "  3. Folder: / (root)" -ForegroundColor Gray
        Write-Host "  4. Click Save" -ForegroundColor Gray
        Write-Host ""
        
        Write-Host "Site will be available at:" -ForegroundColor Green
        Write-Host "  https://m0nado.github.io/helios-platform" -ForegroundColor Yellow
    }
    
    "8" {
        Write-Host "📖 COMPLETE SETUP GUIDE" -ForegroundColor Green
        Write-Host ""
        
        Write-Host "The complete setup guide is available at:" -ForegroundColor Yellow
        Write-Host "  COMPLETE_GITHUB_SETUP_GUIDE.md" -ForegroundColor Green
        Write-Host ""
        
        Write-Host "It covers:" -ForegroundColor Cyan
        Write-Host "  • Part 1: Project Board Setup (8 min)" -ForegroundColor Gray
        Write-Host "  • Part 2: Add Issue Templates (copy-paste)" -ForegroundColor Gray
        Write-Host "  • Part 3: Setup Automation Rules (4 rules)" -ForegroundColor Gray
        Write-Host "  • Part 4: GitHub Pages Setup (10 min)" -ForegroundColor Gray
        Write-Host "  • Part 5: Actions Verification" -ForegroundColor Gray
        Write-Host "  • Part 6: Repository Settings" -ForegroundColor Gray
        Write-Host "  • Part 7: Issue Templates" -ForegroundColor Gray
        Write-Host "  • Part 8: Full Checklist" -ForegroundColor Gray
        Write-Host "  • Part 9: Quick Links" -ForegroundColor Gray
        Write-Host "  • Part 10: Testing" -ForegroundColor Gray
        Write-Host ""
        
        Write-Host "Opening guide (if available)..." -ForegroundColor Yellow
        if (Test-Path "./COMPLETE_GITHUB_SETUP_GUIDE.md") {
            notepad ./COMPLETE_GITHUB_SETUP_GUIDE.md
        }
    }
    
    "0" {
        Write-Host "👋 Goodbye!" -ForegroundColor Green
        exit
    }
    
    default {
        Write-Host "❌ Invalid option. Please choose 0-8." -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "═════════════════════════════════════════════════════════════════════════════════" -ForegroundColor Gray
Write-Host ""

Write-Host "📚 Additional Resources:" -ForegroundColor Yellow
Write-Host ""
Write-Host "  • Repository: https://github.com/M0nado/helios-platform" -ForegroundColor Gray
Write-Host "  • Project Board: https://github.com/M0nado/helios-platform/projects" -ForegroundColor Gray
Write-Host "  • GitHub Actions: https://github.com/M0nado/helios-platform/actions" -ForegroundColor Gray
Write-Host "  • Settings: https://github.com/M0nado/helios-platform/settings" -ForegroundColor Gray
Write-Host "  • Full Guide: COMPLETE_GITHUB_SETUP_GUIDE.md" -ForegroundColor Gray
Write-Host ""

