# ============================================================================
# Git Secrets Configuration & Pre-commit Hook Setup (PowerShell)
# ============================================================================
# Purpose: Prevent accidental credential exposure in git commits
# Usage: .\setup-git-secrets.ps1
# ============================================================================

param(
    [string]$RepoPath = (Get-Location).Path,
    [switch]$ScanHistory = $false
)

Write-Host "Git Secrets Setup for Monado Blade Repository" -ForegroundColor Cyan
Write-Host "Repository: $RepoPath" -ForegroundColor Yellow

# ============================================================================
# 1. Setup Pre-commit Hook
# ============================================================================

Write-Host "`n[1/4] Creating pre-commit hook..." -ForegroundColor Green

$hookPath = "$RepoPath\.git\hooks\pre-commit"

$preCommitContent = @'
# Pre-commit hook - Check for secrets before committing
Write-Host "Checking for secrets in staged changes..."

$secretPatterns = @(
    'password'
    'api_key'
    'apikey'
    'secret'
    'token'
    'credential'
    'aws_'
    'private_key'
    'BEGIN PRIVATE KEY'
)

$diff = & git diff --cached 2>$null
$foundSecrets = $false

foreach ($pattern in $secretPatterns) {
    if ($diff -match $pattern) {
        Write-Host "WARNING: Potential secret found matching: $pattern" -ForegroundColor Red
        $foundSecrets = $true
    }
}

if ($foundSecrets) {
    Write-Host "COMMIT BLOCKED: Potential secrets detected!" -ForegroundColor Red
    Write-Host "Review your changes before committing" -ForegroundColor Yellow
    exit 1
}

Write-Host "OK: No obvious secrets detected" -ForegroundColor Green
exit 0
'@

New-Item -Path ($hookPath | Split-Path) -ItemType Directory -Force -ErrorAction SilentlyContinue | Out-Null
Set-Content -Path $hookPath -Value $preCommitContent -Encoding UTF8
Write-Host "   OK: Pre-commit hook created" -ForegroundColor Green

# ============================================================================
# 2. Configure Git Attributes
# ============================================================================

Write-Host "`n[2/4] Setting up .gitattributes..." -ForegroundColor Green

$gitAttributesPath = "$RepoPath\.gitattributes"
$gitAttributesContent = @'
# Git Attributes Configuration
# Prevent certain files from being tracked/diffed

# Environment files - never diff
*.env diff=secret
*.key diff=secret
*.pem diff=secret
*.p12 diff=secret
*.pfx diff=secret

# Config files with credentials
appsettings.*.json diff=secret
config/*.json diff=secret
secrets/** diff=secret

# Binary files
*.dll binary
*.exe binary
*.pdb binary
*.so binary
*.dylib binary
'@

if (-not (Test-Path $gitAttributesPath)) {
    Set-Content -Path $gitAttributesPath -Value $gitAttributesContent -Encoding UTF8
    Write-Host "   OK: .gitattributes created" -ForegroundColor Green
} else {
    Write-Host "   INFO: .gitattributes already exists" -ForegroundColor Yellow
}

# ============================================================================
# 3. Git Configuration
# ============================================================================

Write-Host "`n[3/4] Configuring git settings..." -ForegroundColor Green

& git config --local core.safecrlf true
& git config --local core.hooksPath .git/hooks
& git config --local diff.secret.text false

Write-Host "   OK: Git configuration updated" -ForegroundColor Green

# ============================================================================
# 4. Verify and Scan History (Optional)
# ============================================================================

Write-Host "`n[4/4] Security scan..." -ForegroundColor Green

if ($ScanHistory) {
    Write-Host "   Scanning git history for exposed secrets..." -ForegroundColor Yellow
    
    $suspiciousPatterns = @(
        'password'
        'api_key'
        'secret'
        'token'
        'credential'
        'aws_'
    )
    
    $foundIssues = $false
    foreach ($pattern in $suspiciousPatterns) {
        $matches = & git log -p -S $pattern --all 2>$null | Measure-Object -Line
        if ($matches.Lines -gt 0) {
            Write-Host "   WARNING: Found '$pattern' in history (review manually)" -ForegroundColor Yellow
            $foundIssues = $true
        }
    }
    
    if (-not $foundIssues) {
        Write-Host "   OK: No obvious secrets found in history" -ForegroundColor Green
    }
} else {
    Write-Host "   INFO: Run with -ScanHistory flag to scan git history" -ForegroundColor Cyan
}

# ============================================================================
# Summary
# ============================================================================

Write-Host "`nGit Secrets Protection Setup Complete" -ForegroundColor Green
Write-Host ""
Write-Host "Configuration completed:" -ForegroundColor Green
Write-Host "  OK: Pre-commit hook installed"
Write-Host "  OK: .gitattributes configured"
Write-Host "  OK: Git configuration updated"
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. Review .gitignore to ensure sensitive files are excluded"
Write-Host "  2. Test: git commit should now check for secrets"
Write-Host "  3. Ensure no real secrets are in template files"
Write-Host ""
