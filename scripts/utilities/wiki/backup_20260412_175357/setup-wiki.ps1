<#
.SYNOPSIS
    Initializes the Helios Platform wiki database and schema

.DESCRIPTION
    Creates SQLite database at docs/wiki.db with full schema including:
    - Files registry
    - Hierarchical categories
    - Cross-references with conflict detection
    - Team notes and metadata
    - Dependencies and builds
    - Code snippets registry
    
    Includes performance indexes and root category seeding.

.PARAMETER DatabasePath
    Path to SQLite database file (default: docs/wiki.db)

.PARAMETER ForceReset
    Delete and recreate database if it exists

.PARAMETER SeedCategories
    Populate with root categories (default: $true)

.EXAMPLE
    .\setup-wiki.ps1 -DatabasePath C:\path\to\wiki.db
    
.EXAMPLE
    .\setup-wiki.ps1 -ForceReset

.NOTES
    Requires: System.Data.SQLite or sqlite3.exe
    Location: scripts/utilities/wiki/setup-wiki.ps1
    Version: 1.0
#>

param(
    [string]$DatabasePath = "C:\Users\ADMIN\helios-platform\docs\wiki.db",
    [switch]$ForceReset,
    [switch]$SeedCategories = $true
)

# Import utilities
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$rootDir = Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $scriptDir))
$schemaFile = Join-Path $rootDir "docs\wiki.db.sql"

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

function Test-SqliteAvailable {
    try {
        $result = & sqlite3 ":memory:" "SELECT 1;" 2>&1
        return $LASTEXITCODE -eq 0
    }
    catch {
        return $false
    }
}

function Initialize-Database {
    param([string]$DbPath, [string]$SchemaPath)
    
    try {
        # Read schema
        if (-not (Test-Path $SchemaPath)) {
            throw "Schema file not found: $SchemaPath"
        }
        
        $schema = Get-Content -Path $SchemaPath -Raw
        
        # Remove database if force reset
        if ($ForceReset -and (Test-Path $DbPath)) {
            Remove-Item -Path $DbPath -Force
            Write-Status "Removed existing database" "Warning"
        }
        
        # Create parent directory if needed
        $dbDir = Split-Path -Parent $DbPath
        if (-not (Test-Path $dbDir)) {
            New-Item -ItemType Directory -Path $dbDir -Force | Out-Null
        }
        
        # Execute schema
        Write-Status "Initializing database schema..."
        $schema | & sqlite3 $DbPath 2>&1 | Out-Null
        
        if ($LASTEXITCODE -ne 0) {
            throw "SQLite error executing schema"
        }
        
        Write-Status "Database created successfully" "Success"
        return $true
    }
    catch {
        Write-Status "Error: $_" "Error"
        return $false
    }
}

function Seed-RootCategories {
    param([string]$DbPath)
    
    try {
        Write-Status "Seeding root categories..."
        
        $categories = @(
            @{ name = "Scripts"; level = 1; description = "Automation and utility scripts"; icon = "⚙️" },
            @{ name = "Configurations"; level = 1; description = "Configuration files and templates"; icon = "⚙️" },
            @{ name = "Documentation"; level = 1; description = "Project documentation"; icon = "📚" },
            @{ name = "Templates"; level = 1; description = "PowerShell and code templates"; icon = "📋" },
            @{ name = "Build"; level = 1; description = "Build configuration and targets"; icon = "🔨" }
        )
        
        foreach ($cat in $categories) {
            $sql = @"
INSERT OR IGNORE INTO categories (name, parent_id, level, description, icon, order_index)
VALUES ('$($cat.name)', NULL, $($cat.level), '$($cat.description)', '$($cat.icon)', 0);
"@
            $sql | & sqlite3 $DbPath 2>&1 | Out-Null
        }
        
        Write-Status "Root categories seeded" "Success"
        return $true
    }
    catch {
        Write-Status "Error seeding categories: $_" "Error"
        return $false
    }
}

function Get-DatabaseStats {
    param([string]$DbPath)
    
    $tables = & sqlite3 $DbPath "SELECT name FROM sqlite_master WHERE type='table';" 2>&1
    $indexes = & sqlite3 $DbPath "SELECT name FROM sqlite_master WHERE type='index';" 2>&1
    $views = & sqlite3 $DbPath "SELECT name FROM sqlite_master WHERE type='view';" 2>&1
    
    Write-Host "`n=== Database Statistics ===" -ForegroundColor Cyan
    Write-Host "Tables: $($tables.Count)" -ForegroundColor Green
    Write-Host "Indexes: $($indexes.Count)" -ForegroundColor Green
    Write-Host "Views: $($views.Count)" -ForegroundColor Green
    Write-Host "`n=== Tables ===" -ForegroundColor Cyan
    $tables | ForEach-Object { Write-Host "  - $_" }
    Write-Host "`n=== Indexes ===" -ForegroundColor Cyan
    $indexes | ForEach-Object { Write-Host "  - $_" }
    Write-Host "`n=== Views ===" -ForegroundColor Cyan
    $views | ForEach-Object { Write-Host "  - $_" }
}

# Main execution
Write-Host "`n╔════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║     HELIOS WIKI DATABASE SETUP         ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════╝`n" -ForegroundColor Cyan

Write-Status "Checking SQLite availability..."
if (-not (Test-SqliteAvailable)) {
    Write-Status "SQLite3 not found in PATH" "Error"
    exit 1
}
Write-Status "SQLite3 available" "Success"

Write-Status "Database Path: $DatabasePath"
Write-Status "Schema File: $SchemaFile"

# Initialize database
if (-not (Initialize-Database -DbPath $DatabasePath -SchemaPath $SchemaFile)) {
    exit 1
}

# Seed categories
if ($SeedCategories) {
    if (-not (Seed-RootCategories -DbPath $DatabasePath)) {
        Write-Status "Warning: Category seeding failed, but database is initialized" "Warning"
    }
}

# Display statistics
Get-DatabaseStats -DbPath $DatabasePath

Write-Status "Wiki database setup complete!" "Success"
Write-Host "`n✓ Database ready for population`n"
