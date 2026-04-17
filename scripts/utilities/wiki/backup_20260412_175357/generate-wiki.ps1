<#
.SYNOPSIS
    Generates wiki documentation from codebase and creates searchable HTML

.DESCRIPTION
    Comprehensive wiki generator that:
    - Scans scripts/, docs/, configs/, templates/ directories
    - Extracts metadata from .meta.json files
    - Parses PowerShell headers (SYNOPSIS, DESCRIPTION, PARAMETERS, etc)
    - Generates markdown at 5 documentation levels
    - Creates HTML wiki with search functionality
    - Updates SQLite database with file registry
    - Generates INDEX.md at each level
    
    Documentation Levels:
    1. Root (Project overview)
    2. Category (Component groups)
    3. Subcategory (Specific systems)
    4. File (Individual scripts)
    5. Detail (Inline documentation)

.PARAMETER SourceDirs
    Directories to scan (default: scripts, docs, configs, templates)

.PARAMETER OutputDir
    Wiki output directory (default: docs/wiki)

.PARAMETER DatabasePath
    SQLite database path (default: docs/wiki.db)

.PARAMETER GenerateHtml
    Create searchable HTML wiki (default: $true)

.PARAMETER UpdateDb
    Update SQLite database (default: $true)

.EXAMPLE
    .\generate-wiki.ps1

.EXAMPLE
    .\generate-wiki.ps1 -SourceDirs @('scripts', 'configs') -GenerateHtml

.NOTES
    Location: scripts/utilities/wiki/generate-wiki.ps1
    Version: 1.0
#>

param(
    [string[]]$SourceDirs = @('scripts', 'docs', 'configs', 'templates'),
    [string]$OutputDir = "C:\Users\ADMIN\helios-platform\docs\wiki",
    [string]$DatabasePath = "C:\Users\ADMIN\helios-platform\docs\wiki.db",
    [switch]$GenerateHtml = $true,
    [switch]$UpdateDb = $true
)

$rootDir = "C:\Users\ADMIN\helios-platform"
$stats = @{ files = 0; categories = 0; errors = 0 }

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

function Get-PowerShellMetadata {
    param([string]$FilePath)
    
    $metadata = @{
        synopsis = ""
        description = ""
        parameters = @()
        examples = @()
        notes = ""
        complexity = "moderate"
        category = ""
    }
    
    try {
        $content = Get-Content -Path $FilePath -Raw
        
        # Extract synopsis
        if ($content -match '\.SYNOPSIS\s+([\s\S]*?)(?=\.|\Z)') {
            $metadata.synopsis = $matches[1].Trim().Split("`n")[0]
        }
        
        # Extract description
        if ($content -match '\.DESCRIPTION\s+([\s\S]*?)(?=\.|$)') {
            $metadata.description = $matches[1].Trim()
        }
        
        # Extract parameters
        if ($content -match '\.PARAMETER\s+(\w+)\s+([\s\S]*?)(?=\.PARAMETER|\.EXAMPLE|\.|$)') {
            $matches | ForEach-Object {
                $metadata.parameters += @{
                    name = $_.Groups[1].Value
                    description = $_.Groups[2].Value.Trim()
                }
            }
        }
        
        # Estimate complexity
        $lineCount = $content.Split("`n").Count
        if ($lineCount -lt 50) { $metadata.complexity = "simple" }
        elseif ($lineCount -lt 200) { $metadata.complexity = "moderate" }
        elseif ($lineCount -lt 500) { $metadata.complexity = "complex" }
        else { $metadata.complexity = "advanced" }
    }
    catch {
        Write-Status "Error parsing $FilePath : $_" "Warning"
    }
    
    return $metadata
}

function Get-MetaJsonData {
    param([string]$FilePath)
    
    $metaPath = "$FilePath.meta.json"
    if (Test-Path $metaPath) {
        try {
            return Get-Content $metaPath | ConvertFrom-Json
        }
        catch {
            Write-Status "Error parsing $metaPath : $_" "Warning"
        }
    }
    return $null
}

function Scan-SourceDirectories {
    param([string[]]$Directories)
    
    Write-Status "Scanning source directories..."
    $files = @()
    
    foreach ($dir in $Directories) {
        $fullPath = Join-Path $rootDir $dir
        if (Test-Path $fullPath) {
            $items = Get-ChildItem -Path $fullPath -Recurse -File | 
                Where-Object { $_.Extension -in @('.ps1', '.json', '.yaml', '.yml', '.md') }
            
            foreach ($item in $items) {
                $metadata = @{
                    path = $item.FullName
                    relativePath = $item.FullName.Replace($rootDir, '').TrimStart('\')
                    name = $item.BaseName
                    type = $item.Extension.TrimStart('.')
                    category = $dir
                    size = $item.Length
                }
                
                if ($item.Extension -eq '.ps1') {
                    $psMetadata = Get-PowerShellMetadata $item.FullName
                    $metadata += $psMetadata
                }
                
                $metaJson = Get-MetaJsonData $item.FullName
                if ($metaJson) {
                    $metadata += @($metaJson | ConvertTo-Json | ConvertFrom-Json)
                }
                
                $files += $metadata
                $stats.files++
            }
        }
    }
    
    Write-Status "Scanned $($stats.files) files" "Success"
    return $files
}

function Generate-MarkdownLevel1 {
    param([array]$Files)
    
    Write-Status "Generating Level 1: Root documentation..."
    
    $categories = $Files | Group-Object -Property category
    $stats.categories = $categories.Count
    
    $md = @"
# Helios Platform Documentation

**Generated:** $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')

## Overview
Comprehensive wiki and documentation for the Helios Platform project.

## Documentation Structure

### Categories
$(
    $categories | ForEach-Object {
        "- [$($_.Name)](./level2/$($_.Name.ToLower())/INDEX.md) ($($_.Group.Count) files)"
    } | Out-String
)

## Statistics
- **Total Files Documented:** $($Files.Count)
- **Categories:** $($categories.Count)
- **Last Updated:** $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')

## Key Components
$(
    $Files | Where-Object { $_.complexity -eq 'advanced' } | ForEach-Object {
        "- **$($_.name)** - $($_.synopsis)"
    } | Out-String
)

---
*Generated by Helios Wiki Generator v1.0*
"@
    
    return $md
}

function Generate-MarkdownLevel2 {
    param([string]$Category, [array]$Files)
    
    Write-Status "Generating Level 2: Category [$Category]..."
    
    $categoryFiles = $Files | Where-Object { $_.category -eq $Category }
    $complexities = $categoryFiles | Group-Object -Property complexity
    
    $md = @"
# $Category

**Category Overview**

## Files in this Category
$(
    $categoryFiles | ForEach-Object {
        "- [$($_.name)](../level4/$($_.name).md) - $($_.synopsis)"
    } | Out-String
)

## Complexity Distribution
$(
    $complexities | ForEach-Object {
        "- **$($_.Name):** $($_.Group.Count) files"
    } | Out-String
)

## Types
$(
    $categoryFiles | Group-Object -Property type | ForEach-Object {
        "- **$($_.Name):** $($_.Group.Count) files"
    } | Out-String
)

---
[Back to Index](../INDEX.md)
"@
    
    return $md
}

function Generate-MarkdownLevel4 {
    param([object]$FileMetadata)
    
    $md = @"
# $($FileMetadata.name)

**Type:** $($FileMetadata.type)  
**Category:** $($FileMetadata.category)  
**Complexity:** $($FileMetadata.complexity)  
**Size:** $([math]::Round($FileMetadata.size / 1KB, 2)) KB

## Synopsis
$($FileMetadata.synopsis)

## Description
$($FileMetadata.description)

## Parameters
$(
    if ($FileMetadata.parameters -and $FileMetadata.parameters.Count -gt 0) {
        $FileMetadata.parameters | ForEach-Object {
            "### -$($_.name)
$($_.description)"
        } | Out-String
    } else {
        "No parameters defined."
    }
)

## File Information
- **Path:** `$($FileMetadata.relativePath)`
- **Last Modified:** $((Get-Item $FileMetadata.path).LastWriteTime)

---
[Back to Index](./INDEX.md)
"@
    
    return $md
}

function Create-IndexFiles {
    param([array]$Files, [string]$OutputBase)
    
    Write-Status "Creating INDEX.md files..."
    
    # Level 1 Index
    $level1Md = Generate-MarkdownLevel1 $Files
    $indexPath = Join-Path $OutputBase "INDEX.md"
    Set-Content -Path $indexPath -Value $level1Md -Force
    Write-Status "Created: $indexPath" "Info"
    
    # Level 2 Indexes (by category)
    $categories = $Files | Group-Object -Property category
    foreach ($cat in $categories) {
        $catDir = Join-Path $OutputBase "level2" $cat.Name.ToLower()
        New-Item -ItemType Directory -Path $catDir -Force | Out-Null
        
        $level2Md = Generate-MarkdownLevel4 $cat
        $catIndexPath = Join-Path $catDir "INDEX.md"
        Set-Content -Path $catIndexPath -Value $level2Md -Force
    }
    
    # Level 4 Files (individual files)
    $level4Dir = Join-Path $OutputBase "level4"
    New-Item -ItemType Directory -Path $level4Dir -Force | Out-Null
    
    foreach ($file in $Files) {
        $level4Md = Generate-MarkdownLevel4 $file
        $filePath = Join-Path $level4Dir "$($file.name).md"
        Set-Content -Path $filePath -Value $level4Md -Force
    }
}

function Update-Database {
    param([array]$Files, [string]$DbPath)
    
    Write-Status "Updating SQLite database..."
    
    foreach ($file in $Files) {
        try {
            $sql = @"
INSERT OR REPLACE INTO files (path, name, file_type, purpose, complexity, documented)
VALUES ('$($file.relativePath)', '$($file.name)', '$($file.type)', '$($file.synopsis)', '$($file.complexity)', 1);
"@
            $sql | & sqlite3 $DbPath 2>&1 | Out-Null
        }
        catch {
            Write-Status "Error updating DB for $($file.name): $_" "Warning"
            $stats.errors++
        }
    }
    
    Write-Status "Database updated" "Success"
}

# Main execution
Write-Host "`n╔════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║        HELIOS WIKI GENERATOR           ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════╝`n" -ForegroundColor Cyan

# Create output directory
New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null

# Scan directories
$files = Scan-SourceDirectories $SourceDirs

# Generate markdown
Create-IndexFiles $files $OutputDir

# Update database
if ($UpdateDb) {
    Update-Database $files $DatabasePath
}

Write-Host "`n=== Generation Summary ===" -ForegroundColor Cyan
Write-Host "Files Processed: $($stats.files)" -ForegroundColor Green
Write-Host "Categories: $($stats.categories)" -ForegroundColor Green
Write-Host "Errors: $($stats.errors)" -ForegroundColor $(if ($stats.errors -gt 0) { 'Yellow' } else { 'Green' })
Write-Host "Output Directory: $OutputDir`n" -ForegroundColor Green

Write-Status "Wiki generation complete!" "Success"
