#!/usr/bin/env pwsh
# 🚀 HELIOS Codespace Quick-Launch Helper
# Usage: ./codespace-launch.ps1

Write-Host ""
Write-Host "╔════════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║                                                                    ║" -ForegroundColor Cyan
Write-Host "║          🚀 HELIOS PLATFORM - CODESPACE QUICK LAUNCHER 🚀         ║" -ForegroundColor Cyan
Write-Host "║                                                                    ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

Write-Host "📋 CODESPACE STATUS CHECK" -ForegroundColor Yellow
Write-Host ""

# Check if GitHub CLI is available
try {
    $ghVersion = gh --version 2>&1
    Write-Host "✅ GitHub CLI: $($ghVersion.Split([Environment]::NewLine)[0])" -ForegroundColor Green
} catch {
    Write-Host "⚠️  GitHub CLI not available. Install from: https://cli.github.com" -ForegroundColor Yellow
}

Write-Host ""

# List existing codespaces
Write-Host "📚 Your Codespaces:" -ForegroundColor Cyan
Write-Host ""

try {
    $codespaces = gh codespace list 2>&1
    if ($codespaces -match "No codespaces") {
        Write-Host "  No active codespaces (they'll appear here after creation)" -ForegroundColor Gray
    } else {
        Write-Host $codespaces -ForegroundColor Gray
    }
} catch {
    Write-Host "  Could not list codespaces. Are you logged in?" -ForegroundColor Yellow
}

Write-Host ""

# Deployment menu
Write-Host "🎯 QUICK OPTIONS:" -ForegroundColor Yellow
Write-Host ""

Write-Host "  [1] Launch New Codespace (Browser)" -ForegroundColor Cyan
Write-Host "  [2] Connect via SSH" -ForegroundColor Cyan
Write-Host "  [3] Connect via VS Code Remote" -ForegroundColor Cyan
Write-Host "  [4] Show Codespace URL" -ForegroundColor Cyan
Write-Host "  [5] Delete a Codespace" -ForegroundColor Cyan
Write-Host "  [6] Show Setup Guide" -ForegroundColor Cyan
Write-Host "  [7] Validate HELIOS/Hermes Tooling (non-mutating)" -ForegroundColor Cyan
Write-Host "  [0] Exit" -ForegroundColor Gray
Write-Host ""

$choice = Read-Host "Choose an option [0-7]"

switch ($choice) {
    "1" {
        Write-Host ""
        Write-Host "🚀 Launching Codespace in browser..." -ForegroundColor Green
        Write-Host ""
        Write-Host "URL: https://github.com/codespaces/new?repo=M0nado/helios-platform" -ForegroundColor Yellow
        Write-Host ""
        Write-Host "⏱️  Wait 3-5 minutes for initialization (first time)" -ForegroundColor Gray
        Write-Host "⏱️  Or 30 seconds (if you have existing codespaces)" -ForegroundColor Gray
        Write-Host ""

        # Attempt to open in browser
        try {
            Start-Process "https://github.com/codespaces/new?repo=M0nado/helios-platform"
            Write-Host "✅ Browser opening..." -ForegroundColor Green
        } catch {
            Write-Host "⚠️  Could not open browser. Please visit URL above manually." -ForegroundColor Yellow
        }
    }

    "2" {
        Write-Host ""
        Write-Host "🔗 SSH Connection Instructions:" -ForegroundColor Green
        Write-Host ""
        Write-Host "  Step 1: List your codespaces:" -ForegroundColor Cyan
        Write-Host "    gh codespace list" -ForegroundColor Gray
        Write-Host ""
        Write-Host "  Step 2: Connect to codespace:" -ForegroundColor Cyan
        Write-Host "    gh codespace ssh -c <codespace-name>" -ForegroundColor Gray
        Write-Host ""
        Write-Host "  Example:" -ForegroundColor Gray
        Write-Host "    gh codespace ssh -c aged-space-gw4xgq7" -ForegroundColor Gray
        Write-Host ""
    }

    "3" {
        Write-Host ""
        Write-Host "📱 VS Code Remote Connection:" -ForegroundColor Green
        Write-Host ""
        Write-Host "  Step 1: Install 'GitHub Codespaces' extension in VS Code" -ForegroundColor Cyan
        Write-Host "  Step 2: Open VS Code" -ForegroundColor Cyan
        Write-Host "  Step 3: Click Remote Explorer icon (left sidebar)" -ForegroundColor Cyan
        Write-Host "  Step 4: Select 'GitHub Codespaces'" -ForegroundColor Cyan
        Write-Host "  Step 5: Choose your codespace from the list" -ForegroundColor Cyan
        Write-Host ""
    }

    "4" {
        Write-Host ""
        Write-Host "🌐 Codespace URLs:" -ForegroundColor Green
        Write-Host ""
        Write-Host "  Create New:" -ForegroundColor Cyan
        Write-Host "    https://github.com/codespaces/new?repo=M0nado/helios-platform" -ForegroundColor Yellow
        Write-Host ""
        Write-Host "  List Existing:" -ForegroundColor Cyan
        Write-Host "    https://github.com/codespaces" -ForegroundColor Yellow
        Write-Host ""
        Write-Host "  Repository:" -ForegroundColor Cyan
        Write-Host "    https://github.com/M0nado/helios-platform" -ForegroundColor Yellow
        Write-Host ""
    }

    "5" {
        Write-Host ""
        Write-Host "🗑️  Delete Codespace:" -ForegroundColor Yellow
        Write-Host ""

        try {
            $codespaces = gh codespace list --json name,repository --noheader 2>&1
            if ($codespaces -match "No codespaces") {
                Write-Host "  No codespaces to delete" -ForegroundColor Gray
            } else {
                Write-Host "  Available codespaces:" -ForegroundColor Gray
                gh codespace list
                Write-Host ""
                $name = Read-Host "Enter codespace name to delete"
                Write-Host ""
                Write-Host "⚠️  About to delete: $name" -ForegroundColor Yellow
                $confirm = Read-Host "Are you sure? (yes/no)"

                if ($confirm -eq "yes") {
                    gh codespace delete -c $name
                    Write-Host "✅ Codespace deleted" -ForegroundColor Green
                } else {
                    Write-Host "Cancelled" -ForegroundColor Gray
                }
            }
        } catch {
            Write-Host "Could not delete codespace" -ForegroundColor Red
        }
    }

    "6" {
        Write-Host ""
        Write-Host "📖 Opening Setup Guide..." -ForegroundColor Green
        Write-Host ""

        $guideFile = "./CODESPACE_SETUP_GUIDE.md"
        if (Test-Path $guideFile) {
            Get-Content $guideFile | more
        } else {
            Write-Host "Setup guide not found. Please read: https://github.com/M0nado/helios-platform/blob/main/CODESPACE_SETUP_GUIDE.md" -ForegroundColor Yellow
        }
    }


    "7" {
        Write-Host ""
        Write-Host "🧪 Running non-mutating setup validation..." -ForegroundColor Green
        Write-Host ""

        $validator = Join-Path $PSScriptRoot "Validate-Setup.ps1"
        if (Test-Path $validator) {
            & $validator
        } else {
            Write-Host "Validator not found at $validator" -ForegroundColor Red
        }
    }

    "0" {
        Write-Host ""
        Write-Host "👋 Goodbye!" -ForegroundColor Green
        Write-Host ""
        exit
    }

    default {
        Write-Host ""
        Write-Host "❌ Invalid option" -ForegroundColor Red
        Write-Host ""
    }
}

Write-Host ""
Write-Host "═════════════════════════════════════════════════════════════════════" -ForegroundColor Gray
Write-Host ""
Write-Host "📚 More Info:" -ForegroundColor Yellow
Write-Host "  • Docs: https://docs.github.com/en/codespaces" -ForegroundColor Gray
Write-Host "  • Guide: ./CODESPACE_SETUP_GUIDE.md" -ForegroundColor Gray
Write-Host "  • Repo: https://github.com/M0nado/helios-platform" -ForegroundColor Gray
Write-Host ""
