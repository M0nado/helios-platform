param(
    [switch]$OpenOnly,
    [string]$BrowserPath = ""
)
$ErrorActionPreference = 'Stop'
$Root = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$ProfileDir = if ($env:HELIOS_BROWSER_PROFILE_DIR) { $env:HELIOS_BROWSER_PROFILE_DIR } else { Join-Path $Root '.helios\browser-profile' }
$PrivateDir = if ($env:HELIOS_PRIVATE_DIR) { $env:HELIOS_PRIVATE_DIR } else { Join-Path $Root '.helios\private' }
New-Item -ItemType Directory -Force -Path $ProfileDir, $PrivateDir | Out-Null

@'
# HELIOS private local data

This folder is intentionally ignored by git. Put manual exports, temporary downloaded reports, and browser/app scratch data here. Do not copy these files into commits unless they are sanitized durable docs/config.
'@ | Set-Content -Path (Join-Path $PrivateDir 'README.md') -Encoding UTF8

function Find-Browser {
    if ($BrowserPath -and (Test-Path $BrowserPath)) { return $BrowserPath }
    $candidates = @(
        "$env:ProgramFiles\Microsoft\Edge\Application\msedge.exe",
        "${env:ProgramFiles(x86)}\Microsoft\Edge\Application\msedge.exe",
        "$env:ProgramFiles\Google\Chrome\Application\chrome.exe"
    )
    foreach ($candidate in $candidates) {
        if ($candidate -and (Test-Path $candidate)) { return $candidate }
    }
    return $null
}

function Open-AppWindow([string]$Name, [string]$Url, [string]$Browser) {
    Write-Host "Opening ${Name}: $Url" -ForegroundColor Cyan
    if ($Browser) {
        Start-Process -FilePath $Browser -ArgumentList @("--user-data-dir=$ProfileDir", "--app=$Url") | Out-Null
    } else {
        Start-Process $Url | Out-Null
    }
}

if (-not $OpenOnly) {
    Push-Location $Root
    python3 scripts/integrations/app_automation.py
    python3 scripts/dashboard/generate-gui.py
    Pop-Location
}

$browser = Find-Browser
Open-AppWindow 'HELIOS GUI' 'http://127.0.0.1:8787/' $browser
Open-AppWindow 'Microsoft 365 Copilot' $(if ($env:COPILOT_WEB_URL) { $env:COPILOT_WEB_URL } else { 'https://copilot.microsoft.com' }) $browser
Open-AppWindow 'ChatGPT' 'https://chatgpt.com' $browser
Open-AppWindow 'Claude' 'https://claude.ai' $browser
Open-AppWindow 'GitHub' 'https://github.com' $browser
Open-AppWindow 'Azure Cloud Shell' 'https://shell.azure.com' $browser
Open-AppWindow 'Slack' 'https://app.slack.com' $browser
Open-AppWindow 'Linear' 'https://linear.app' $browser

Write-Host "Local HELIOS app shell launched." -ForegroundColor Green
Write-Host "Browser profile/scratch: $ProfileDir"
Write-Host "Private exports: $PrivateDir"
Write-Host "Nothing in .helios/ is committed to git. Use official sign-in pages; this script does not read cookies or credentials."
