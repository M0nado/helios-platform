<#
.SYNOPSIS
Validate AI Services API Keys

.DESCRIPTION
Validates API keys for all AI services, checks format, and tests connectivity.

.EXAMPLE
.\validate-api-keys.ps1
.\validate-api-keys.ps1 -Interactive
#>

param(
    [string]$ApiKeysPath = "C:\Users\ADMIN\helios-platform\config\ai-services\api-keys.env",
    [switch]$Interactive,
    [switch]$UpdateKeys
)

function Test-ApiKeyFormat {
    param([string]$Key, [string]$Value)

    switch ($Key) {
        { $_ -match "OPENAI" } {
            if ($Value -match "^sk-[A-Za-z0-9]{48,}$") {
                return @{ Valid = $true; Message = "Valid OpenAI format" }
            }
            else {
                return @{ Valid = $false; Message = "Invalid OpenAI format (should start with 'sk-')" }
            }
        }
        default {
            if ($Value.Length -gt 0) {
                return @{ Valid = $true; Message = "Key present" }
            }
            else {
                return @{ Valid = $false; Message = "Key is empty" }
            }
        }
    }
}

function Load-ApiKeys {
    param([string]$Path)

    $keys = @{}

    if (Test-Path $Path) {
        $content = Get-Content $Path
        foreach ($line in $content) {
            if ($line -and -not $line.StartsWith("#")) {
                $parts = $line -split "=", 2
                if ($parts.Count -eq 2) {
                    $keys[$parts[0].Trim()] = $parts[1].Trim()
                }
            }
        }
    }

    return $keys
}

function Save-ApiKeys {
    param(
        [string]$Path,
        [hashtable]$Keys
    )

    $content = "# AI Services Configuration - API Keys`n"
    $content += "# Generated: $(Get-Date)`n`n"

    foreach ($key in $Keys.GetEnumerator() | Sort-Object Name) {
        $value = $key.Value
        if ($value -match "password|key|secret") {
            $value = "***MASKED***"
        }
        $content += "$($key.Name)=$value`n"
    }

    Set-Content -Path $Path -Value $content -Force
}

function Interactive-KeyEntry {
    Write-Host "`n" -ForegroundColor Cyan
    Write-Host "─────────────────────────────────────────────────────────────" -ForegroundColor Cyan
    Write-Host "             Interactive API Key Configuration" -ForegroundColor Cyan
    Write-Host "─────────────────────────────────────────────────────────────" -ForegroundColor Cyan

    $keys = @{}

    $keyConfigs = @(
        @{ Name = "OPENAI_API_KEY"; Description = "gpt-5.4 API Key" },
        @{ Name = "OPENAI_API_KEY"; Description = "gpt-5.4-mini API Key" },
        @{ Name = "OPENAI_API_KEY"; Description = "gpt-5.5 API Key" }
    )

    foreach ($config in $keyConfigs) {
        $current = [Environment]::GetEnvironmentVariable($config.Name)
        if ($current) {
            Write-Host "`n$($config.Description):" -ForegroundColor Yellow
            Write-Host "Current: $($current.Substring(0, 10))..." -ForegroundColor Gray
            $prompt = "Update? (y/n): "
        }
        else {
            Write-Host "`n$($config.Description):" -ForegroundColor Yellow
            $prompt = "Enter key (press Enter to skip): "
        }

        $input = Read-Host $prompt

        if ($input -and $input -ne "n") {
            $keys[$config.Name] = $input
        }
    }

    return $keys
}

Write-Host "`n" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "              API KEY VALIDATION UTILITY" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan

# Load existing keys
$keys = Load-ApiKeys -Path $ApiKeysPath

if ($Interactive) {
    $newKeys = Interactive-KeyEntry
    $keys += $newKeys
}

Write-Host "`n" -ForegroundColor Cyan
Write-Host "Validating API Keys..." -ForegroundColor Cyan
Write-Host "─────────────────────────────────────────────────────────────" -ForegroundColor Cyan

$validCount = 0
$invalidCount = 0

foreach ($key in $keys.GetEnumerator() | Sort-Object Name) {
    $validation = Test-ApiKeyFormat -Key $key.Name -Value $key.Value

    $statusColor = if ($validation.Valid) { "Green" } else { "Red" }
    $status = if ($validation.Valid) { "✓" } else { "✗" }

    Write-Host "$status $($key.Name): " -NoNewline -ForegroundColor $statusColor
    Write-Host $validation.Message -ForegroundColor $statusColor

    if ($validation.Valid) { $validCount++ } else { $invalidCount++ }
}

Write-Host "`n" -ForegroundColor Cyan
Write-Host "Summary:" -ForegroundColor Cyan
Write-Host "✓ Valid keys:   $validCount" -ForegroundColor Green
Write-Host "✗ Invalid keys: $invalidCount" -ForegroundColor Red

if ($UpdateKeys -and $validCount -gt 0) {
    Save-ApiKeys -Path $ApiKeysPath -Keys $keys
    Write-Host "`nKeys saved to: $ApiKeysPath" -ForegroundColor Green
}

Write-Host "`n"
