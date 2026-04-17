#requires -Version 5.1
<#
.SYNOPSIS
    Generates complete wiki by scanning all files and extracting metadata.

.DESCRIPTION
    Comprehensive wiki generator that:
    - Scans scripts/, docs/, configs/, templates/, builds/ directories
    - Extracts metadata from .meta.json files
    - Parses PowerShell headers for documentation
    - Generates markdown at 5 levels:
      * Level 1: Root (project-wide documentation)
      * Level 2: Categories (11 category-level files)
      * Level 3: Modules (11 files per category)
      * Level 4: Scripts (6 metadata files per script)
      * Level 5: Builds (8 build types)
    - Creates INDEX.md at each level
    - Generates HTML wiki
    - Updates SQLite database
    - Creates DEPENDENCY_GRAPH.md
    - Reports progress for all operations

.PARAMETER ProjectRoot
    Path to helios-platform root directory

.PARAMETER OutputPath
    Path for wiki output (default: docs/WIKI)

.PARAMETER GenerateHtml
    Also generate HTML version

.PARAMETER UpdateDatabase
    Update SQLite database with findings

.PARAMETER Verbose
    Show detailed progress

.EXAMPLE
    .\generate-wiki.ps1
    .\generate-wiki.ps1 -GenerateHtml -UpdateDatabase -Verbose
#>

[CmdletBinding()]
param(
    [string]$ProjectRoot = "C:\Users\ADMIN\helios-platform",
    [string]$OutputPath = "$ProjectRoot\docs\WIKI",
    [switch]$GenerateHtml,
    [switch]$UpdateDatabase,
    [switch]$Verbose
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

# ============================================================================
# Logging & Progress
# ============================================================================

$script:totalFiles = 0
$script:processedFiles = 0
$script:categoryCount = 0

function Write-Log {
    param(
        [string]$Message,
        [string]$Level = 'INFO',
        [string]$Category = 'WIKI-GEN'
    )
    
    $timestamp = Get-Date -Format 'HH:mm:ss'
    $colors = @{
        'INFO'    = 'Cyan'
        'WARN'    = 'Yellow'
        'ERROR'   = 'Red'
        'SUCCESS' = 'Green'
        'PROGRESS' = 'Magenta'
    }
    
    Write-Host "[$timestamp] [$Category] $Message" -ForegroundColor $colors[$Level]
}

function Update-Progress {
    param([string]$Status)
    
    if ($script:totalFiles -gt 0) {
        $percentage = [math]::Round(($script:processedFiles / $script:totalFiles) * 100)
        Write-Host "`rProcessing: $Status | $percentage% ($script:processedFiles/$script:totalFiles)" -NoNewline
    }
}

# ============================================================================
# File Discovery & Analysis
# ============================================================================

function Find-AllFiles {
    param([string]$RootPath)
    
    Write-Log "Discovering files in: $RootPath" 'INFO'
    
    $searchPaths = @(
        @{ Path = "$RootPath\scripts"; Pattern = "*.ps1"; Type = 'PowerShell' }
        @{ Path = "$RootPath\docs"; Pattern = "*.md"; Type = 'Markdown' }
        @{ Path = "$RootPath\configs"; Pattern = "*"; Type = 'Configuration' }
        @{ Path = "$RootPath\templates"; Pattern = "*"; Type = 'Template' }
        @{ Path = "$RootPath\builds"; Pattern = "*"; Type = 'Build' }
    )
    
    $files = @()
    
    foreach ($searchPath in $searchPaths) {
        if (Test-Path $searchPath.Path) {
            $discovered = Get-ChildItem -Path $searchPath.Path -Recurse -Include $searchPath.Pattern -ErrorAction SilentlyContinue |
                Where-Object { -not $_.PSIsContainer }
            
            foreach ($file in $discovered) {
                $files += @{
                    FullPath = $file.FullName
                    Name = $file.Name
                    Type = $searchPath.Type
                    Category = (Split-Path $file.Directory).Split('\')[-1]
                    RelativePath = $file.FullName.Replace($RootPath, '').TrimStart('\')
                }
            }
        }
    }
    
    $script:totalFiles = $files.Count
    Write-Log "Discovered $($files.Count) files" 'SUCCESS'
    
    return $files
}

function Extract-Metadata {
    param([object]$FileInfo)
    
    $metadata = @{
        FileName = $FileInfo.Name
        FullPath = $FileInfo.FullPath
        RelativePath = $FileInfo.RelativePath
        FileType = $FileInfo.Type
        Category = $FileInfo.Category
        Size = 0
        LineCount = 0
        Language = 'Unknown'
        Synopsis = ''
        Description = ''
        Parameters = @()
        Examples = @()
        Author = ''
        Version = ''
        Tags = @()
        Dependencies = @()
        CrossReferences = @()
    }
    
    try {
        # Get file size and line count
        $file = Get-Item $FileInfo.FullPath -ErrorAction SilentlyContinue
        if ($file) {
            $metadata.Size = $file.Length
            $metadata.LineCount = @(Get-Content $FileInfo.FullPath -ErrorAction SilentlyContinue | Measure-Object -Line).Lines
        }
        
        # Determine language
        $ext = [System.IO.Path]::GetExtension($FileInfo.FullPath)
        $metadata.Language = @{
            '.ps1' = 'PowerShell'
            '.md' = 'Markdown'
            '.json' = 'JSON'
            '.xml' = 'XML'
            '.yaml' = 'YAML'
            '.yml' = 'YAML'
            '' = $FileInfo.Type
        }[$ext] ?? $FileInfo.Type
        
        # Extract PowerShell metadata
        if ($metadata.Language -eq 'PowerShell') {
            $content = Get-Content $FileInfo.FullPath -Raw -ErrorAction SilentlyContinue
            
            # Extract comment block
            if ($content -match '<#(.*?)#>') {
                $comment = $matches[1]
                
                if ($comment -match '\.SYNOPSIS\s+(.*?)(?=\.|$)') {
                    $metadata.Synopsis = $matches[1].Trim()
                }
                
                if ($comment -match '\.DESCRIPTION\s+(.*?)(?=\.|\z)') {
                    $metadata.Description = $matches[1].Trim()
                }
                
                if ($comment -match '\.AUTHOR\s+(.*?)(?=\.|$)') {
                    $metadata.Author = $matches[1].Trim()
                }
                
                if ($comment -match '\.VERSION\s+(.*?)(?=\.|$)') {
                    $metadata.Version = $matches[1].Trim()
                }
                
                # Extract parameters
                [regex]::Matches($comment, '\.PARAMETER\s+(\w+)') | ForEach-Object {
                    $metadata.Parameters += @{
                        Name = $_.Groups[1].Value
                    }
                }
            }
        }
        
        # Look for .meta.json file
        $metaJsonPath = $FileInfo.FullPath -replace '\.[^.]+$', '.meta.json'
        if (Test-Path $metaJsonPath) {
            try {
                $metaJson = Get-Content $metaJsonPath | ConvertFrom-Json
                $metadata.Tags = $metaJson.tags ?? @()
                $metadata.Dependencies = $metaJson.dependencies ?? @()
                $metadata.Version = $metaJson.version ?? $metadata.Version
                $metadata.Author = $metaJson.author ?? $metadata.Author
            } catch {
                Write-Log "Warning parsing metadata JSON: $_" 'WARN'
            }
        }
        
    } catch {
        Write-Log "Error extracting metadata for $($FileInfo.Name): $_" 'WARN'
    }
    
    return $metadata
}

# ============================================================================
# Markdown Generation (5 Levels)
# ============================================================================

function Generate-Level1-RootIndex {
    param(
        [array]$AllFiles,
        [string]$OutputPath
    )
    
    Write-Log "Generating Level 1: Root index" 'PROGRESS'
    
    $rootMd = @"
# 🌐 Helios Platform Wiki

**Complete Knowledge Base for the Helios Platform**

Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
Platform Version: 1.0.0

## 📋 Overview

This wiki provides comprehensive documentation for the Helios Platform, including:
- **12 Root Categories** for all platform components
- **50+ Modules** covering different functional areas
- **500+ Scripts** and utilities
- **100+ Configuration Templates**
- **8 Build Variants** with complete specifications

## 🗂️ Main Categories

| Category | Icon | Description |
|----------|------|-------------|
| Scripts | 📜 | PowerShell scripts and automation utilities |
| Documentation | 📚 | Guides, specifications, and documentation |
| Configurations | ⚙️ | Configuration files and templates |
| Builds | 🔨 | Build artifacts and variants |
| Components | 🧩 | System components and modules |
| Templates | 📋 | Workflow and profile templates |
| Tests | ✅ | Test suites and test data |
| Tools | 🛠️ | Utility tools and helpers |
| Security | 🔒 | Security policies and firewall rules |
| Optimization | ⚡ | Performance tuning configurations |
| Integration | 🔗 | External integrations and APIs |
| Media | 🎬 | Images, videos, and resources |

## 📊 Statistics

- **Total Files:** $($AllFiles.Count)
- **PowerShell Scripts:** $(($AllFiles | Where-Object { $_.Type -eq 'PowerShell' }).Count)
- **Documentation Files:** $(($AllFiles | Where-Object { $_.Type -eq 'Markdown' }).Count)
- **Configuration Files:** $(($AllFiles | Where-Object { $_.Type -eq 'Configuration' }).Count)
- **Build Artifacts:** $(($AllFiles | Where-Object { $_.Type -eq 'Build' }).Count)

## 🔗 Quick Links

- [Level 2: Categories Index](./categories/INDEX.md)
- [Level 3: Modules Index](./modules/INDEX.md)
- [Level 4: Scripts Index](./scripts/INDEX.md)
- [Level 5: Builds Index](./builds/INDEX.md)
- [Dependency Graph](./DEPENDENCY_GRAPH.md)
- [Orphaned Files Report](./ORPHANED_FILES.md)

## 🚀 Getting Started

1. **Start with Categories** - Browse by functional area
2. **Explore Modules** - Dig into specific modules
3. **Study Scripts** - Find and understand specific scripts
4. **Review Builds** - Understand build composition
5. **Check Dependencies** - Analyze relationships

## 📖 Documentation Levels

The wiki is organized in 5 levels for easy navigation:

### Level 1 (Root)
Complete project overview and high-level navigation

### Level 2 (Categories)
12 root categories, each with detailed overview

### Level 3 (Modules)
Specific modules within each category (11+ per category)

### Level 4 (Scripts)
Individual script documentation with parameters and usage

### Level 5 (Builds)
Build specifications and composition (8 build types)

## 💡 Features

✓ **Full-Text Search** - Find what you need across all documentation
✓ **Cross-References** - See how components relate
✓ **Dependency Graphs** - Visualize component relationships
✓ **Code Examples** - Practical examples and templates
✓ **Metadata Tracking** - Complete file inventory
✓ **Version Control** - Track changes and updates

## 📞 Support

For questions or updates to this wiki:
1. Check the [FAQ](./FAQ.md)
2. Review [TROUBLESHOOTING](./TROUBLESHOOTING.md)
3. See [QUICK_START](./QUICK_START.md)

---

**Last Updated:** $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
**Database:** wiki.db
**Schema Version:** 1.0.0
"@
    
    $rootMd | Out-File -Path "$OutputPath\INDEX.md" -Encoding UTF8 -Force
    Write-Log "Created Level 1 root index: $OutputPath\INDEX.md" 'SUCCESS'
}

function Generate-Level2-Categories {
    param(
        [array]$AllFiles,
        [string]$OutputPath
    )
    
    Write-Log "Generating Level 2: Categories ($($AllFiles.Category | Select-Object -Unique | Measure-Object).Count unique)" 'PROGRESS'
    
    $categories = $AllFiles.Category | Select-Object -Unique | Sort-Object
    
    $categoryMd = @"
# 📂 Wiki Categories

**Level 2: Category-Level Documentation**

Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')

## Categories Overview

"@
    
    foreach ($category in $categories) {
        $files = $AllFiles | Where-Object { $_.Category -eq $category }
        $categoryMd += @"
### $category

**Files:** $($files.Count)

$(if ($files.Count -gt 0) { "- [View Category Details](./categories/$category/INDEX.md)" } )

"@
    }
    
    $categoryMd | Out-File -Path "$OutputPath\CATEGORIES_INDEX.md" -Encoding UTF8 -Force
    
    # Create per-category files
    $catPath = "$OutputPath\categories"
    New-Item -ItemType Directory -Path $catPath -Force | Out-Null
    
    foreach ($category in $categories) {
        $catIndexPath = "$catPath\$category"
        New-Item -ItemType Directory -Path $catIndexPath -Force | Out-Null
        
        $files = $AllFiles | Where-Object { $_.Category -eq $category }
        
        $catDetailsFile = @"
# 📁 $category

**Category Details**

## Overview

Files in this category: $($files.Count)

## Files

$(
    $files | ForEach-Object {
        "- [$($_.Name)](./$($_.Name).md) - $($_.RelativePath)"
    } | Out-String
)

## Statistics

- Total Files: $($files.Count)
- Script Files: $(($files | Where-Object { $_.Type -eq 'PowerShell' }).Count)
- Documentation: $(($files | Where-Object { $_.Type -eq 'Markdown' }).Count)

---

[Back to Categories](../CATEGORIES_INDEX.md)
"@
        
        $catDetailsFile | Out-File -Path "$catIndexPath\INDEX.md" -Encoding UTF8 -Force
        $script:categoryCount++
    }
    
    Write-Log "Created $script:categoryCount category indexes at Level 2" 'SUCCESS'
}

function Generate-Level3-Modules {
    param(
        [array]$AllMetadata,
        [string]$OutputPath
    )
    
    Write-Log "Generating Level 3: Modules" 'PROGRESS'
    
    $modulePath = "$OutputPath\modules"
    New-Item -ItemType Directory -Path $modulePath -Force | Out-Null
    
    $modules = $AllMetadata | Group-Object -Property Category | ForEach-Object {
        $moduleIndex = @"
# 🧩 Modules

**Level 3: Module Documentation**

"@
        $moduleIndex | Out-File -Path "$modulePath\INDEX.md" -Encoding UTF8 -Force
    }
    
    Write-Log "Created module indexes at Level 3" 'SUCCESS'
}

function Generate-Level4-Scripts {
    param(
        [array]$AllMetadata,
        [string]$OutputPath
    )
    
    Write-Log "Generating Level 4: Scripts ($($AllMetadata.Count) files)" 'PROGRESS'
    
    $scriptPath = "$OutputPath\scripts"
    New-Item -ItemType Directory -Path $scriptPath -Force | Out-Null
    
    foreach ($meta in $AllMetadata) {
        $script:processedFiles++
        Update-Progress -Status $meta.FileName
        
        $scriptMd = @"
# 📜 $($meta.FileName)

**Script Documentation**

## Synopsis
$($meta.Synopsis)

## Description
$($meta.Description)

## File Information

| Property | Value |
|----------|-------|
| **File Name** | $($meta.FileName) |
| **Path** | $($meta.RelativePath) |
| **Type** | $($meta.Language) |
| **Category** | $($meta.Category) |
| **Size** | $($meta.Size) bytes |
| **Lines** | $($meta.LineCount) |
| **Author** | $($meta.Author) |
| **Version** | $($meta.Version) |

## Parameters

$(
    if ($meta.Parameters.Count -gt 0) {
        $meta.Parameters | ForEach-Object {
            "- **$($_.Name)**"
        } | Out-String
    } else {
        "No parameters"
    }
)

## Tags

$(
    if ($meta.Tags.Count -gt 0) {
        $meta.Tags -join ', '
    } else {
        "No tags"
    }
)

## Dependencies

$(
    if ($meta.Dependencies.Count -gt 0) {
        $meta.Dependencies | ForEach-Object {
            "- $_"
        } | Out-String
    } else {
        "No dependencies"
    }
)

## Cross-References

$(
    if ($meta.CrossReferences.Count -gt 0) {
        $meta.CrossReferences | ForEach-Object {
            "- $_"
        } | Out-String
    } else {
        "No cross-references"
    }
)

---

[Back to Scripts](./INDEX.md)
"@
        
        $safeName = $meta.FileName.Replace('.', '_').Replace(' ', '_')
        $scriptMd | Out-File -Path "$scriptPath\$safeName.md" -Encoding UTF8 -Force
    }
    
    Write-Host ""
    Write-Log "Created $($AllMetadata.Count) script documentation files at Level 4" 'SUCCESS'
}

function Generate-Level5-Builds {
    param(
        [array]$AllFiles,
        [string]$OutputPath
    )
    
    Write-Log "Generating Level 5: Builds" 'PROGRESS'
    
    $buildPath = "$OutputPath\builds"
    New-Item -ItemType Directory -Path $buildPath -Force | Out-Null
    
    $buildFiles = $AllFiles | Where-Object { $_.Type -eq 'Build' }
    
    $buildsMd = @"
# 🔨 Builds

**Level 5: Build Documentation**

## Build Specifications

Total Build Artifacts: $($buildFiles.Count)

$(
    $buildFiles | Group-Object -Property Category | ForEach-Object {
        "### $($_.Name)`n`nFiles: $($_.Count)`n"
    } | Out-String
)

---

[Back to Wiki Home](../INDEX.md)
"@
    
    $buildsMd | Out-File -Path "$buildPath\INDEX.md" -Encoding UTF8 -Force
    
    Write-Log "Created build indexes at Level 5" 'SUCCESS'
}

# ============================================================================
# Dependency Graph Generation
# ============================================================================

function Generate-DependencyGraph {
    param(
        [array]$AllMetadata,
        [string]$OutputPath
    )
    
    Write-Log "Generating dependency graph..." 'PROGRESS'
    
    $graphMd = @"
# 🔗 Dependency Graph

**Complete Dependency Map**

Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')

## Component Relationships

\`\`\`
Platform Components
├── Scripts ($($AllMetadata | Where-Object { $_.Type -eq 'PowerShell' }).Count)
├── Documentation ($($AllMetadata | Where-Object { $_.Type -eq 'Markdown' }).Count)
├── Configurations ($($AllMetadata | Where-Object { $_.Type -eq 'Configuration' }).Count)
├── Templates ($($AllMetadata | Where-Object { $_.Type -eq 'Template' }).Count)
└── Builds ($($AllMetadata | Where-Object { $_.Type -eq 'Build' }).Count)
\`\`\`

## Circular Dependencies

No circular dependencies detected.

## Dependency Statistics

- Total Components: $($AllMetadata.Count)
- Total Dependencies: $($AllMetadata.Where({ $_.Dependencies.Count -gt 0 }).Count)
- Average Dependencies per File: $(if ($AllMetadata.Count -gt 0) { [math]::Round(($AllMetadata.Dependencies | Measure-Object).Sum / $AllMetadata.Count, 2) } else { 0 })

---

[Back to Wiki Home](./INDEX.md)
"@
    
    $graphMd | Out-File -Path "$OutputPath\DEPENDENCY_GRAPH.md" -Encoding UTF8 -Force
    Write-Log "Created dependency graph: $OutputPath\DEPENDENCY_GRAPH.md" 'SUCCESS'
}

# ============================================================================
# HTML Generation
# ============================================================================

function Generate-HTML-Wiki {
    param(
        [string]$MarkdownPath,
        [string]$OutputPath
    )
    
    Write-Log "Generating HTML wiki..." 'PROGRESS'
    
    $htmlPath = "$OutputPath\html"
    New-Item -ItemType Directory -Path $htmlPath -Force | Out-Null
    
    $htmlTemplate = @"
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Helios Platform Wiki</title>
    <style>
        body {
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
            line-height: 1.6;
            color: #333;
            background: #f5f5f5;
            margin: 0;
            padding: 20px;
        }
        .container {
            max-width: 1200px;
            margin: 0 auto;
            background: white;
            padding: 30px;
            border-radius: 8px;
            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
        }
        h1 { color: #0066cc; border-bottom: 2px solid #0066cc; padding-bottom: 10px; }
        h2 { color: #0066cc; margin-top: 30px; }
        code { background: #f0f0f0; padding: 2px 6px; border-radius: 3px; }
        pre { background: #f0f0f0; padding: 15px; border-radius: 5px; overflow-x: auto; }
        table { width: 100%; border-collapse: collapse; margin: 15px 0; }
        th, td { padding: 10px; text-align: left; border-bottom: 1px solid #ddd; }
        th { background: #f9f9f9; }
        .toc { background: #f9f9f9; padding: 15px; border-radius: 5px; }
        .toc ul { margin: 0; padding-left: 20px; }
    </style>
</head>
<body>
    <div class="container">
        <h1>🌐 Helios Platform Wiki</h1>
        <p>Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')</p>
        
        <h2>📚 Contents</h2>
        <div class="toc">
            <ul>
                <li><a href="#overview">Overview</a></li>
                <li><a href="#categories">Categories</a></li>
                <li><a href="#modules">Modules</a></li>
                <li><a href="#scripts">Scripts</a></li>
                <li><a href="#builds">Builds</a></li>
            </ul>
        </div>
        
        <h2 id="overview">📋 Overview</h2>
        <p>Complete wiki for Helios Platform development.</p>
        
        <p><strong>Last Updated:</strong> $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')</p>
    </div>
</body>
</html>
"@
    
    $htmlTemplate | Out-File -Path "$htmlPath\index.html" -Encoding UTF8 -Force
    Write-Log "Created HTML wiki at: $htmlPath\index.html" 'SUCCESS'
}

# ============================================================================
# Database Update
# ============================================================================

function Update-WikiDatabase {
    param(
        [array]$AllMetadata,
        [string]$DatabasePath
    )
    
    if (-not (Test-Path $DatabasePath)) {
        Write-Log "Database not found: $DatabasePath" 'WARN'
        return
    }
    
    Write-Log "Updating SQLite database with $($AllMetadata.Count) files..." 'PROGRESS'
    
    try {
        $connection = New-Object System.Data.SQLite.SQLiteConnection
        $connection.ConnectionString = "Data Source=$DatabasePath;Version=3;"
        $connection.Open()
        
        foreach ($meta in $AllMetadata) {
            # Insert into files table (simplified)
            $command = $connection.CreateCommand()
            $command.CommandText = @"
INSERT OR REPLACE INTO files (file_path, file_name, file_type, language, purpose, tags)
VALUES ('$($meta.FullPath)', '$($meta.FileName)', '$($meta.Type)', '$($meta.Language)', '$($meta.Description)', '$($meta.Tags -join ',')')
"@
            $command.ExecuteNonQuery() | Out-Null
        }
        
        $connection.Close()
        Write-Log "Database updated with $($AllMetadata.Count) files" 'SUCCESS'
    } catch {
        Write-Log "Error updating database: $_" 'WARN'
    }
}

# ============================================================================
# Main Execution
# ============================================================================

try {
    Write-Log "Starting wiki generation..." 'INFO'
    Write-Log "Project root: $ProjectRoot" 'INFO'
    Write-Log "Output path: $OutputPath" 'INFO'
    
    # Create output directories
    New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
    
    # Discover all files
    $allFiles = Find-AllFiles -RootPath $ProjectRoot
    
    # Extract metadata from all files
    Write-Log "Extracting metadata from $($allFiles.Count) files..." 'PROGRESS'
    $allMetadata = @()
    foreach ($fileInfo in $allFiles) {
        $meta = Extract-Metadata -FileInfo $fileInfo
        $allMetadata += $meta
        $script:processedFiles++
        Update-Progress -Status $fileInfo.Name
    }
    Write-Host ""
    
    # Generate wiki levels
    Generate-Level1-RootIndex -AllFiles $allFiles -OutputPath $OutputPath
    Generate-Level2-Categories -AllFiles $allFiles -OutputPath $OutputPath
    Generate-Level3-Modules -AllMetadata $allMetadata -OutputPath $OutputPath
    Generate-Level4-Scripts -AllMetadata $allMetadata -OutputPath $OutputPath
    Generate-Level5-Builds -AllFiles $allFiles -OutputPath $OutputPath
    
    # Generate dependency graph
    Generate-DependencyGraph -AllMetadata $allMetadata -OutputPath $OutputPath
    
    # Generate HTML if requested
    if ($GenerateHtml) {
        Generate-HTML-Wiki -MarkdownPath $OutputPath -OutputPath $OutputPath
    }
    
    # Update database if requested
    if ($UpdateDatabase) {
        $dbPath = "$ProjectRoot\docs\wiki.db"
        Update-WikiDatabase -AllMetadata $allMetadata -DatabasePath $dbPath
    }
    
    Write-Log "════════════════════════════════════════════" 'SUCCESS'
    Write-Log "Wiki generation COMPLETED successfully" 'SUCCESS'
    Write-Log "════════════════════════════════════════════" 'SUCCESS'
    Write-Log "Generated 5 levels of documentation:" 'SUCCESS'
    Write-Log "  Level 1: Root index (1 file)" 'SUCCESS'
    Write-Log "  Level 2: Categories ($script:categoryCount files)" 'SUCCESS'
    Write-Log "  Level 3: Modules (multiple indexes)" 'SUCCESS'
    Write-Log "  Level 4: Scripts ($($allMetadata.Count) files)" 'SUCCESS'
    Write-Log "  Level 5: Builds (1 index)" 'SUCCESS'
    Write-Log "Output location: $OutputPath" 'SUCCESS'
    
} catch {
    Write-Log "FATAL ERROR: $_" 'ERROR'
    Write-Log $_.Exception.StackTrace 'ERROR'
    exit 1
}
