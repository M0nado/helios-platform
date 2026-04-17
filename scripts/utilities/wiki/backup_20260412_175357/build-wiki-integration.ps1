<#
.SYNOPSIS
    Build system integration utilities for wiki management

.DESCRIPTION
    Helpers for integrating wiki utilities with the Helios build system:
    - Get files for specific build configurations
    - Register new builds
    - Update build components
    - Query build-specific dependencies
    - Generate build documentation

.PARAMETER Action
    'list-builds', 'get-components', 'register-build', 'update-components', 'build-doc'

.PARAMETER BuildName
    Name of build configuration

.PARAMETER Components
    Array of component paths for registration

.PARAMETER DatabasePath
    SQLite database path

.EXAMPLE
    .\build-wiki-integration.ps1 -Action list-builds

.EXAMPLE
    .\build-wiki-integration.ps1 -Action register-build -BuildName "enterprise" -Components @('script1.ps1', 'script2.ps1')

.EXAMPLE
    .\build-wiki-integration.ps1 -Action get-components -BuildName "standard"

.NOTES
    Location: scripts/utilities/wiki/build-wiki-integration.ps1
    Version: 1.0
#>

param(
    [ValidateSet('list-builds', 'get-components', 'register-build', 'update-components', 'build-doc')]
    [string]$Action = 'list-builds',
    [string]$BuildName,
    [string[]]$Components,
    [string]$DatabasePath = "C:\Users\ADMIN\helios-platform\docs\wiki.db"
)

function Write-Status {
    param([string]$Message, [ValidateSet('Info', 'Success', 'Warning', 'Error')]$Type = 'Info')
    $colors = @{
        'Info'    = 'Cyan'
        'Success' = 'Green'
        'Warning' = 'Yellow'
        'Error'   = 'Red'
    }
    Write-Host "[$(Get-Date -Format 'HH:mm:ss')] $Message" -ForegroundColor $colors[$Type]
}

function Execute-Query {
    param([string]$Sql, [string]$DbPath)
    
    try {
        $result = & sqlite3 -json $DbPath $Sql 2>&1
        if ($LASTEXITCODE -eq 0) {
            return $result | ConvertFrom-Json -ErrorAction SilentlyContinue
        }
        return @()
    }
    catch {
        Write-Status "Query error: $_" "Error"
        return @()
    }
}

function List-Builds {
    param([string]$DbPath)
    
    Write-Status "Listing build configurations..."
    
    $sql = @"
SELECT b.id, b.name, b.description, b.environment,
       COUNT(bc.id) as component_count,
       GROUP_CONCAT(f.name, ', ') as components
FROM builds b
LEFT JOIN build_components bc ON b.id = bc.build_id
LEFT JOIN files f ON bc.file_id = f.id
GROUP BY b.id
ORDER BY b.name;
"@
    
    $builds = Execute-Query $sql $DbPath
    
    if ($builds) {
        Write-Host "`n=== Build Configurations ===" -ForegroundColor Cyan
        $builds | Format-Table -AutoSize | Out-String
    } else {
        Write-Status "No builds found" "Warning"
    }
}

function Get-BuildComponents {
    param([string]$BuildName, [string]$DbPath)
    
    Write-Status "Getting components for build: $BuildName"
    
    $sql = @"
SELECT f.name, f.path, f.complexity, f.status,
       bc.inclusion_type, bc.order_index
FROM builds b
JOIN build_components bc ON b.id = bc.build_id
JOIN files f ON bc.file_id = f.id
WHERE b.name = '$BuildName'
ORDER BY bc.order_index;
"@
    
    $components = Execute-Query $sql $DbPath
    
    if ($components) {
        Write-Host "`n=== Components in '$BuildName' ===" -ForegroundColor Cyan
        $components | Format-Table -AutoSize | Out-String
        Write-Host "Total: $($components.Count) components" -ForegroundColor Green
    } else {
        Write-Status "Build not found or no components" "Warning"
    }
}

function Register-Build {
    param([string]$BuildName, [string[]]$ComponentPaths, [string]$DbPath)
    
    Write-Status "Registering build: $BuildName"
    
    try {
        # Create build
        $sql = @"
INSERT OR REPLACE INTO builds (name, description, environment, modified_at)
VALUES ('$BuildName', 'Build configuration', 'production', datetime('now'));
"@
        
        $sql | & sqlite3 $DbPath 2>&1 | Out-Null
        
        # Get build ID
        $buildId = Execute-Query "SELECT id FROM builds WHERE name='$BuildName';" $DbPath
        
        if ($buildId) {
            $bid = $buildId.id
            Write-Status "Created build ID: $bid" "Success"
            
            # Add components
            $order = 0
            foreach ($component in $ComponentPaths) {
                $fileId = Execute-Query "SELECT id FROM files WHERE path='$component';" $DbPath
                
                if ($fileId.id) {
                    $sql = @"
INSERT OR REPLACE INTO build_components (build_id, file_id, order_index)
VALUES ($bid, $($fileId.id), $order);
"@
                    $sql | & sqlite3 $DbPath 2>&1 | Out-Null
                    Write-Status "  Added: $component" "Info"
                    $order++
                } else {
                    Write-Status "  Skipped: $component (not in database)" "Warning"
                }
            }
            
            Write-Status "Build registered with $order components" "Success"
        }
    }
    catch {
        Write-Status "Error: $_" "Error"
    }
}

function Update-BuildComponents {
    param([string]$BuildName, [string[]]$ComponentPaths, [string]$DbPath)
    
    Write-Status "Updating components for build: $BuildName"
    
    try {
        # Get build ID
        $buildId = Execute-Query "SELECT id FROM builds WHERE name='$BuildName';" $DbPath
        
        if (-not $buildId.id) {
            Write-Status "Build not found" "Error"
            return
        }
        
        # Clear existing components
        $sql = "DELETE FROM build_components WHERE build_id=$($buildId.id);"
        $sql | & sqlite3 $DbPath 2>&1 | Out-Null
        Write-Status "Cleared existing components" "Info"
        
        # Add new components
        $order = 0
        foreach ($component in $ComponentPaths) {
            $fileId = Execute-Query "SELECT id FROM files WHERE path='$component';" $DbPath
            
            if ($fileId.id) {
                $sql = @"
INSERT INTO build_components (build_id, file_id, order_index)
VALUES ($($buildId.id), $($fileId.id), $order);
"@
                $sql | & sqlite3 $DbPath 2>&1 | Out-Null
                Write-Status "  Added: $component" "Info"
                $order++
            } else {
                Write-Status "  Skipped: $component (not in database)" "Warning"
            }
        }
        
        Write-Status "Build updated with $order components" "Success"
    }
    catch {
        Write-Status "Error: $_" "Error"
    }
}

function Generate-BuildDocumentation {
    param([string]$BuildName, [string]$DbPath, [string]$OutputDir = "C:\Users\ADMIN\helios-platform\docs")
    
    Write-Status "Generating documentation for build: $BuildName"
    
    try {
        $components = Execute-Query @"
SELECT f.name, f.path, f.purpose, f.complexity, f.status,
       bc.inclusion_type, bc.order_index
FROM builds b
JOIN build_components bc ON b.id = bc.build_id
JOIN files f ON bc.file_id = f.id
WHERE b.name = '$BuildName'
ORDER BY bc.order_index;
"@ $DbPath
        
        if (-not $components) {
            Write-Status "No components found for build" "Warning"
            return
        }
        
        $md = @"
# Build Configuration: $BuildName

**Generated:** $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')

## Overview
Components included in the **$BuildName** build configuration.

## Component Manifest

| # | Component | Path | Complexity | Inclusion | Status |
|---|-----------|------|-----------|-----------|--------|
$(
    $components | ForEach-Object {
        $idx = [array]::IndexOf($components, $_) + 1
        "| $idx | $($_.name) | ``$($_.path)`` | $($_.complexity) | $($_.inclusion_type) | $($_.status) |"
    } | Out-String
)

## Statistics
- **Total Components:** $($components.Count)
- **Complexity Distribution:**
$(
    $components | Group-Object -Property complexity | ForEach-Object {
        "  - $($_.Name): $($_.Count)"
    } | Out-String
)
- **Status Distribution:**
$(
    $components | Group-Object -Property status | ForEach-Object {
        "  - $($_.Name): $($_.Count)"
    } | Out-String
)

## Inclusion Types
- **Required:** Components that must be included
- **Optional:** Components that may be included
- **Enterprise:** Enterprise-only components
- **Internal:** Internal-only components

---
*Auto-generated build documentation*
"@
        
        $buildDocPath = Join-Path $OutputDir "BUILD_$($BuildName.ToUpper()).md"
        Set-Content -Path $buildDocPath -Value $md -Force
        Write-Status "Documentation saved: $buildDocPath" "Success"
    }
    catch {
        Write-Status "Error: $_" "Error"
    }
}

# Main execution
Write-Host "`n╔════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║    BUILD WIKI INTEGRATION              ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════╝`n" -ForegroundColor Cyan

if (-not (Test-Path $DatabasePath)) {
    Write-Status "Database not found: $DatabasePath" "Error"
    exit 1
}

switch ($Action) {
    'list-builds' {
        List-Builds $DatabasePath
    }
    'get-components' {
        if (-not $BuildName) {
            Write-Status "BuildName required" "Error"
            exit 1
        }
        Get-BuildComponents $BuildName $DatabasePath
    }
    'register-build' {
        if (-not $BuildName -or -not $Components) {
            Write-Status "BuildName and Components required" "Error"
            exit 1
        }
        Register-Build $BuildName $Components $DatabasePath
    }
    'update-components' {
        if (-not $BuildName -or -not $Components) {
            Write-Status "BuildName and Components required" "Error"
            exit 1
        }
        Update-BuildComponents $BuildName $Components $DatabasePath
    }
    'build-doc' {
        if (-not $BuildName) {
            Write-Status "BuildName required" "Error"
            exit 1
        }
        Generate-BuildDocumentation $BuildName $DatabasePath
    }
}

Write-Host ""
