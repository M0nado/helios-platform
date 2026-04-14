#requires -Version 5.1
<#
.SYNOPSIS
    Creates comprehensive dependency graphs and maps component relationships.

.DESCRIPTION
    Advanced dependency mapping tool that:
    - Creates dependency graph visualization
    - Shows component relationships
    - Detects circular dependencies
    - Color-codes by category
    - Generates DEPENDENCY_GRAPH.md
    - Exports to JSON and DOT format
    - Provides complexity analysis
    - Reports on depth and breadth
    - Generates visual ASCII art diagrams
    - Creates interactive graph data

.PARAMETER DatabasePath
    Path to wiki database

.PARAMETER OutputPath
    Path for graph output (default: docs/WIKI/graphs)

.PARAMETER Format
    Export format (dot, json, md, all)

.PARAMETER MaxDepth
    Maximum dependency depth to analyze

.PARAMETER ShowCircular
    Only show circular dependencies

.PARAMETER GenerateVisualization
    Generate ASCII visualization

.EXAMPLE
    .\map-dependencies.ps1
    .\map-dependencies.ps1 -Format all -GenerateVisualization
    .\map-dependencies.ps1 -ShowCircular -Verbose
#>

[CmdletBinding()]
param(
    [string]$DatabasePath = "C:\Users\ADMIN\helios-platform\docs\wiki.db",
    [string]$OutputPath = "C:\Users\ADMIN\helios-platform\docs\WIKI\graphs",
    [ValidateSet('dot', 'json', 'md', 'all')]$Format = 'all',
    [int]$MaxDepth = 5,
    [switch]$ShowCircular,
    [switch]$GenerateVisualization,
    [switch]$Verbose
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

# ============================================================================
# Global State
# ============================================================================

$script:dependencyMap = @{}
$script:circularDeps = @()
$script:categories = @{}
$script:statistics = @{
    TotalNodes = 0
    TotalEdges = 0
    MaxDepth = 0
    CircularCount = 0
    CategoryCount = 0
}

# ============================================================================
# Logging Functions
# ============================================================================

function Write-Log {
    param(
        [string]$Message,
        [string]$Level = 'INFO'
    )
    
    $colors = @{
        'INFO'    = 'Cyan'
        'WARN'    = 'Yellow'
        'ERROR'   = 'Red'
        'SUCCESS' = 'Green'
        'MAP'     = 'Magenta'
    }
    
    if ($Verbose -or $Level -in @('ERROR', 'SUCCESS')) {
        Write-Host "$Message" -ForegroundColor $colors[$Level]
    }
}

# ============================================================================
# Database Connection & Query
# ============================================================================

function New-DatabaseConnection {
    param([string]$DatabasePath)
    
    try {
        if (-not (Test-Path $DatabasePath)) {
            throw "Database not found: $DatabasePath"
        }
        
        $connection = New-Object System.Data.SQLite.SQLiteConnection
        $connection.ConnectionString = "Data Source=$DatabasePath;Version=3;Read Only=True;"
        $connection.Open()
        
        Write-Log "Connected to database" 'INFO'
        return $connection
    } catch {
        Write-Log "Failed to connect to database: $_" 'ERROR'
        throw
    }
}

function Invoke-DatabaseQuery {
    param(
        [System.Data.SQLite.SQLiteConnection]$Connection,
        [string]$Query
    )
    
    try {
        $command = $Connection.CreateCommand()
        $command.CommandText = $Query
        
        $adapter = New-Object System.Data.SQLite.SQLiteDataAdapter($command)
        $dataSet = New-Object System.Data.DataSet
        $adapter.Fill($dataSet) | Out-Null
        
        return $dataSet.Tables[0]
    } catch {
        Write-Log "Database query error: $_" 'ERROR'
        throw
    }
}

# ============================================================================
# Dependency Analysis Functions
# ============================================================================

function Load-Dependencies {
    param([System.Data.SQLite.SQLiteConnection]$Connection)
    
    Write-Log "Loading dependency data..." 'MAP'
    
    $sql = @"
SELECT
    d.dependency_id,
    d.dependent_file_id,
    d.required_file_id,
    d.dependency_type,
    d.depth,
    d.is_circular,
    f1.file_name as source_file,
    f1.file_type as source_type,
    f2.file_name as target_file,
    f2.file_type as target_type,
    c1.category_name as source_category,
    c2.category_name as target_category
FROM dependencies d
JOIN files f1 ON d.dependent_file_id = f1.file_id
JOIN files f2 ON d.required_file_id = f2.file_id
LEFT JOIN categories c1 ON f1.category_id = c1.category_id
LEFT JOIN categories c2 ON f2.category_id = c2.category_id
ORDER BY d.depth
"@
    
    $results = Invoke-DatabaseQuery -Connection $Connection -Query $sql
    
    Write-Log "Loaded $($results.Rows.Count) dependencies" 'SUCCESS'
    
    foreach ($row in $results.Rows) {
        $sourceId = $row['source_file']
        $targetId = $row['target_file']
        $depth = $row['depth']
        $circular = $row['is_circular']
        
        if (-not $script:dependencyMap.ContainsKey($sourceId)) {
            $script:dependencyMap[$sourceId] = @{
                Dependencies = @()
                ReverseDependencies = @()
                Type = $row['source_type']
                Category = $row['source_category']
                Depth = $depth
            }
        }
        
        $script:dependencyMap[$sourceId].Dependencies += @{
            Target = $targetId
            Type = $row['dependency_type']
            Circular = $circular
            Depth = $depth
        }
        
        if (-not $script:dependencyMap.ContainsKey($targetId)) {
            $script:dependencyMap[$targetId] = @{
                Dependencies = @()
                ReverseDependencies = @()
                Type = $row['target_type']
                Category = $row['target_category']
                Depth = $depth
            }
        }
        
        $script:dependencyMap[$targetId].ReverseDependencies += @{
            Source = $sourceId
            Type = $row['dependency_type']
        }
        
        if ($circular) {
            $script:circularDeps += @{
                Source = $sourceId
                Target = $targetId
                Type = $row['dependency_type']
            }
        }
        
        if ($depth -gt $script:statistics.MaxDepth) {
            $script:statistics.MaxDepth = $depth
        }
    }
    
    $script:statistics.TotalNodes = $script:dependencyMap.Count
    $script:statistics.CircularCount = $script:circularDeps.Count
}

function Load-Categories {
    param([System.Data.SQLite.SQLiteConnection]$Connection)
    
    Write-Log "Loading category data..." 'MAP'
    
    $sql = @"
SELECT
    category_id,
    category_name,
    icon,
    COUNT(f.file_id) as file_count
FROM categories c
LEFT JOIN files f ON c.category_id = f.category_id
GROUP BY c.category_id, c.category_name, c.icon
ORDER BY c.category_name
"@
    
    $results = Invoke-DatabaseQuery -Connection $Connection -Query $sql
    
    foreach ($row in $results.Rows) {
        $script:categories[$row['category_name']] = @{
            Icon = $row['icon']
            FileCount = $row['file_count']
        }
    }
    
    $script:statistics.CategoryCount = $script:categories.Count
    Write-Log "Loaded $($script:categories.Count) categories" 'SUCCESS'
}

# ============================================================================
# Graph Generation Functions
# ============================================================================

function Generate-DOTFormat {
    param([string]$OutputPath)
    
    Write-Log "Generating DOT format graph..." 'MAP'
    
    $dot = "digraph DependencyGraph {`r`n"
    $dot += "    rankdir=LR;`r`n"
    $dot += "    node [shape=box, fontname=Arial];`r`n"
    $dot += "    edge [fontname=Arial];`r`n`r`n"
    
    # Add nodes with categories and colors
    $colorMap = @{
        'Scripts' = '#FF6B6B'
        'Documentation' = '#4ECDC4'
        'Configurations' = '#45B7D1'
        'Builds' = '#FFA07A'
        'Components' = '#98D8C8'
        'Templates' = '#F7DC6F'
        'Tests' = '#BB8FCE'
        'Tools' = '#85C1E2'
        'Security' = '#D32F2F'
        'Optimization' = '#388E3C'
        'Integration' = '#1976D2'
        'Media' = '#7B1FA2'
    }
    
    foreach ($node in $script:dependencyMap.Keys) {
        $info = $script:dependencyMap[$node]
        $category = $info.Category ?? 'Unknown'
        $color = $colorMap[$category] ?? '#CCCCCC'
        
        $dot += "    `"$node`" [label=`"$node`", fillcolor=`"$color`", style=filled];`r`n"
    }
    
    $dot += "`r`n"
    
    # Add edges
    $edgeCount = 0
    foreach ($source in $script:dependencyMap.Keys) {
        foreach ($dep in $script:dependencyMap[$source].Dependencies) {
            $target = $dep.Target
            $type = $dep.Type
            $color = if ($dep.Circular) { 'red' } else { 'black' }
            
            $dot += "    `"$source`" -> `"$target`" [label=`"$type`", color=$color];`r`n"
            $edgeCount++
        }
    }
    
    $dot += "`r`n}`r`n"
    
    $dotFile = Join-Path $OutputPath "dependency-graph.dot"
    $dot | Out-File -FilePath $dotFile -Encoding UTF8
    
    $script:statistics.TotalEdges = $edgeCount
    Write-Log "Generated DOT format: $dotFile ($edgeCount edges)" 'SUCCESS'
}

function Generate-JSONFormat {
    param([string]$OutputPath)
    
    Write-Log "Generating JSON format graph..." 'MAP'
    
    $graphData = @{
        Timestamp = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'
        Statistics = $script:statistics
        Nodes = @()
        Edges = @()
        CircularDependencies = $script:circularDeps
    }
    
    # Build nodes array
    foreach ($nodeName in $script:dependencyMap.Keys) {
        $info = $script:dependencyMap[$nodeName]
        $graphData.Nodes += @{
            Id = $nodeName
            Type = $info.Type
            Category = $info.Category
            Depth = $info.Depth
            DependencyCount = $info.Dependencies.Count
            ReverseDependencyCount = $info.ReverseDependencies.Count
        }
    }
    
    # Build edges array
    foreach ($source in $script:dependencyMap.Keys) {
        foreach ($dep in $script:dependencyMap[$source].Dependencies) {
            $graphData.Edges += @{
                Source = $source
                Target = $dep.Target
                Type = $dep.Type
                Circular = $dep.Circular
            }
        }
    }
    
    $jsonFile = Join-Path $OutputPath "dependency-graph.json"
    $graphData | ConvertTo-Json -Depth 10 | Out-File -FilePath $jsonFile -Encoding UTF8
    
    Write-Log "Generated JSON format: $jsonFile" 'SUCCESS'
}

function Generate-MarkdownGraph {
    param([string]$OutputPath)
    
    Write-Log "Generating Markdown format graph..." 'MAP'
    
    $md = @"
# 🔗 Dependency Graph

**Generated:** $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')

## Overview

This document visualizes the dependencies between components in the Helios Platform.

## Statistics

| Metric | Value |
|--------|-------|
| **Total Components** | $($script:statistics.TotalNodes) |
| **Total Dependencies** | $($script:statistics.TotalEdges) |
| **Maximum Depth** | $($script:statistics.MaxDepth) |
| **Circular Dependencies** | $($script:statistics.CircularCount) |
| **Categories** | $($script:statistics.CategoryCount) |

## Category Distribution

"@
    
    foreach ($category in $script:categories.Keys | Sort-Object) {
        $info = $script:categories[$category]
        $md += "- $($info.Icon) **$category**: $($info.FileCount) files`r`n"
    }
    
    $md += @"
`r`n## Dependency Structure

"@
    
    if ($script:circularDeps.Count -gt 0) {
        $md += @"
### 🔴 Circular Dependencies ($($script:circularDeps.Count))

Circular dependencies that may need refactoring:

"@
        foreach ($circ in $script:circularDeps | Select-Object -First 20) {
            $md += "- \`$($circ.Source)\` ↔ \`$($circ.Target)\` ($($circ.Type))`r`n"
        }
    }
    
    $md += @"
`r`n### 📊 Dependency Tree

Top-level components and their dependencies:

"@
    
    $topLevel = $script:dependencyMap.GetEnumerator() |
                Where-Object { $_.Value.ReverseDependencies.Count -eq 0 } |
                Sort-Object { $_.Value.Dependencies.Count } -Descending |
                Select-Object -First 10
    
    foreach ($node in $topLevel) {
        $md += "- **$($node.Key)** ($($node.Value.Dependencies.Count) dependencies)`r`n"
        foreach ($dep in $node.Value.Dependencies | Select-Object -First 5) {
            $circular = if ($dep.Circular) { " 🔄" } else { "" }
            $md += "  - → $($dep.Target) [$($dep.Type)]$circular`r`n"
        }
        if ($node.Value.Dependencies.Count -gt 5) {
            $md += "  - ... and $($node.Value.Dependencies.Count - 5) more`r`n"
        }
    }
    
    $md += @"
`r`n## Export Formats

- [DOT Format](./dependency-graph.dot) - For use with Graphviz
- [JSON Format](./dependency-graph.json) - For programmatic access
- [Markdown](./DEPENDENCY_GRAPH.md) - This file

## Legend

| Symbol | Meaning |
|--------|---------|
| → | Depends On |
| ↔ | Circular Dependency |
| 🔄 | Marked as Circular |

---

**Last Updated:** $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
"@
    
    $mdFile = Join-Path $OutputPath "DEPENDENCY_GRAPH.md"
    $md | Out-File -FilePath $mdFile -Encoding UTF8
    
    Write-Log "Generated Markdown format: $mdFile" 'SUCCESS'
}

# ============================================================================
# Visualization Functions
# ============================================================================

function Generate-ASCIIVisualization {
    param([string]$OutputPath)
    
    Write-Log "Generating ASCII visualization..." 'MAP'
    
    $viz = @"
╔════════════════════════════════════════════════════════════════╗
║            HELIOS PLATFORM DEPENDENCY VISUALIZATION            ║
╚════════════════════════════════════════════════════════════════╝

Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')

STATISTICS:
──────────
Total Components:       $($script:statistics.TotalNodes)
Total Dependencies:     $($script:statistics.TotalEdges)
Maximum Depth:          $($script:statistics.MaxDepth)
Circular Dependencies:  $($script:statistics.CircularCount)

COMPONENT BREAKDOWN BY CATEGORY:
────────────────────────────────

"@
    
    foreach ($category in $script:categories.Keys | Sort-Object) {
        $info = $script:categories[$category]
        $viz += "$($info.Icon) $category ($($info.FileCount) files)`r`n"
    }
    
    $viz += @"
`r`n
TOP-LEVEL COMPONENTS (No Reverse Dependencies):
──────────────────────────────────────────────

"@
    
    $topLevel = $script:dependencyMap.GetEnumerator() |
                Where-Object { $_.Value.ReverseDependencies.Count -eq 0 } |
                Sort-Object { $_.Value.Dependencies.Count } -Descending |
                Select-Object -First 15
    
    foreach ($node in $topLevel) {
        $depCount = $node.Value.Dependencies.Count
        $bar = "─" * [math]::Min($depCount / 2, 40)
        $viz += "├─ $($node.Key) $bar $depCount`r`n"
    }
    
    $viz += @"
`r`n
HIGHEST COMPLEXITY COMPONENTS:
──────────────────────────────

"@
    
    $complex = $script:dependencyMap.GetEnumerator() |
               Sort-Object { $_.Value.Dependencies.Count } -Descending |
               Select-Object -First 10
    
    foreach ($node in $complex) {
        $viz += "$($node.Key): $($node.Value.Dependencies.Count) deps, $($node.Value.ReverseDependencies.Count) reverse`r`n"
    }
    
    if ($script:circularDeps.Count -gt 0) {
        $viz += @"
`r`n
CIRCULAR DEPENDENCIES (🔴 NEEDS ATTENTION):
────────────────────────────────────────────

"@
        
        foreach ($circ in $script:circularDeps | Select-Object -First 10) {
            $viz += "⚠️  $($circ.Source) ↔ $($circ.Target)`r`n"
        }
        
        if ($script:circularDeps.Count -gt 10) {
            $viz += "... and $($script:circularDeps.Count - 10) more circular dependencies`r`n"
        }
    }
    
    $viz += @"
`r`n
COMPLEXITY ANALYSIS:
────────────────────

Average Dependencies per Component:
    $(if ($script:statistics.TotalNodes -gt 0) { [math]::Round($script:statistics.TotalEdges / $script:statistics.TotalNodes, 2) } else { 0 })

Component with Most Dependencies:
    $(($script:dependencyMap.GetEnumerator() | Sort-Object { $_.Value.Dependencies.Count } -Descending | Select-Object -First 1).Key)

Component with Most Reverse Dependencies:
    $(($script:dependencyMap.GetEnumerator() | Sort-Object { $_.Value.ReverseDependencies.Count } -Descending | Select-Object -First 1).Key)

"@
    
    $vizFile = Join-Path $OutputPath "DEPENDENCY_VISUALIZATION.txt"
    $viz | Out-File -FilePath $vizFile -Encoding UTF8
    
    Write-Log "Generated ASCII visualization: $vizFile" 'SUCCESS'
}

# ============================================================================
# Main Execution
# ============================================================================

try {
    Write-Log "╔════════════════════════════════════════════╗" 'MAP'
    Write-Log "║     DEPENDENCY MAPPING & VISUALIZATION     ║" 'MAP'
    Write-Log "╚════════════════════════════════════════════╝" 'MAP'
    Write-Log "" 'INFO'
    
    # Create output directory
    New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
    
    # Connect to database
    $connection = New-DatabaseConnection -DatabasePath $DatabasePath
    
    # Load data
    Load-Dependencies -Connection $connection
    Load-Categories -Connection $connection
    
    $connection.Close()
    
    # Filter for circular if requested
    if ($ShowCircular -and $script:circularDeps.Count -eq 0) {
        Write-Log "No circular dependencies found." 'WARN'
        exit 0
    }
    
    # Generate outputs
    Write-Log "" 'INFO'
    Write-Log "Generating graph formats..." 'MAP'
    
    if ($Format -in @('dot', 'all')) {
        Generate-DOTFormat -OutputPath $OutputPath
    }
    
    if ($Format -in @('json', 'all')) {
        Generate-JSONFormat -OutputPath $OutputPath
    }
    
    if ($Format -in @('md', 'all')) {
        Generate-MarkdownGraph -OutputPath $OutputPath
    }
    
    if ($GenerateVisualization -or $Format -eq 'all') {
        Generate-ASCIIVisualization -OutputPath $OutputPath
    }
    
    # Summary
    Write-Log "" 'SUCCESS'
    Write-Log "════════════════════════════════════════════" 'SUCCESS'
    Write-Log "Dependency Mapping Complete" 'SUCCESS'
    Write-Log "════════════════════════════════════════════" 'SUCCESS'
    Write-Log "" 'INFO'
    Write-Log "Statistics:" 'INFO'
    Write-Log "  Components: $($script:statistics.TotalNodes)" 'INFO'
    Write-Log "  Dependencies: $($script:statistics.TotalEdges)" 'INFO'
    Write-Log "  Max Depth: $($script:statistics.MaxDepth)" 'INFO'
    Write-Log "  Circular: $($script:statistics.CircularCount)" 'INFO'
    Write-Log "" 'INFO'
    Write-Log "Output Location: $OutputPath" 'SUCCESS'
    
} catch {
    Write-Log "FATAL ERROR: $_" 'ERROR'
    Write-Log $_.Exception.StackTrace 'ERROR'
    exit 1
}
