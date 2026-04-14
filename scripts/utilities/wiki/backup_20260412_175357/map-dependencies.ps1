<#
.SYNOPSIS
    Maps and visualizes component dependencies

.DESCRIPTION
    Creates comprehensive dependency graphs and visualizations:
    - Generates DEPENDENCY_GRAPH.md with ASCII diagrams
    - Shows component relationships
    - Identifies circular dependencies
    - Generates DOT format for external visualization
    - Exports build configuration dependencies
    - Creates component interconnection map
    - Analyzes dependency depth and complexity

.PARAMETER OutputFormat
    'markdown', 'dot', 'json', 'all'

.PARAMETER IncludeOptional
    Include optional dependencies (default: $false)

.PARAMETER MaxDepth
    Maximum recursion depth for dependency chains (default: 5)

.PARAMETER OutputDir
    Directory for generated files

.PARAMETER DatabasePath
    SQLite database path

.EXAMPLE
    .\map-dependencies.ps1

.EXAMPLE
    .\map-dependencies.ps1 -OutputFormat dot -MaxDepth 3

.EXAMPLE
    .\map-dependencies.ps1 -OutputFormat all -IncludeOptional

.NOTES
    Location: scripts/utilities/wiki/map-dependencies.ps1
    Version: 1.0
    Outputs: DEPENDENCY_GRAPH.md, dependencies.dot, dependencies.json
#>

param(
    [ValidateSet('markdown', 'dot', 'json', 'all')]
    [string]$OutputFormat = 'all',
    [switch]$IncludeOptional,
    [int]$MaxDepth = 5,
    [string]$OutputDir = "C:\Users\ADMIN\helios-platform\docs",
    [string]$DatabasePath = "C:\Users\ADMIN\helios-platform\docs\wiki.db"
)

$rootDir = "C:\Users\ADMIN\helios-platform"
$graph = @{
    nodes = @()
    edges = @()
    metadata = @{}
}

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

function Get-FileDependencies {
    param([string]$DbPath)
    
    Write-Status "Loading dependencies from database..."
    
    $sql = @"
SELECT d.id, d.source_id, d.target_id, 
       src.name as source_name, src.path as source_path,
       tgt.name as target_name, tgt.path as target_path,
       d.dependency_type, d.is_circular, d.depth,
       d.description
FROM dependencies d
JOIN files src ON d.source_id = src.id
JOIN files tgt ON d.target_id = tgt.id
ORDER BY d.source_id, d.target_id;
"@
    
    return Execute-Query $sql $DbPath
}

function Get-BuildDependencies {
    param([string]$DbPath)
    
    Write-Status "Loading build dependencies..."
    
    $sql = @"
SELECT b.id, b.name as build_name, f.name as file_name, bc.inclusion_type
FROM builds b
JOIN build_components bc ON b.id = bc.build_id
JOIN files f ON bc.file_id = f.id
ORDER BY b.name, bc.order_index;
"@
    
    return Execute-Query $sql $DbPath
}

function Build-DependencyGraph {
    param([array]$Dependencies)
    
    Write-Status "Building dependency graph..."
    
    $nodes = @{}
    $edges = @()
    $circularChains = @()
    
    foreach ($dep in $Dependencies) {
        # Add nodes
        if (-not $nodes.ContainsKey($dep.source_id)) {
            $nodes[$dep.source_id] = @{
                id = $dep.source_id
                name = $dep.source_name
                path = $dep.source_path
                indegree = 0
                outdegree = 0
            }
        }
        
        if (-not $nodes.ContainsKey($dep.target_id)) {
            $nodes[$dep.target_id] = @{
                id = $dep.target_id
                name = $dep.target_name
                path = $dep.target_path
                indegree = 0
                outdegree = 0
            }
        }
        
        # Add edge
        $edges += @{
            source = $dep.source_id
            target = $dep.target_id
            type = $dep.dependency_type
            circular = $dep.is_circular
            depth = $dep.depth
            description = $dep.description
        }
        
        # Update degrees
        $nodes[$dep.source_id].outdegree++
        $nodes[$dep.target_id].indegree++
        
        # Track circular
        if ($dep.is_circular) {
            $circularChains += $dep
        }
    }
    
    return @{
        nodes = $nodes
        edges = $edges
        circular = $circularChains
    }
}

function Generate-MarkdownGraph {
    param([hashtable]$Graph)
    
    Write-Status "Generating markdown graph..."
    
    $md = @"
# Dependency Graph

**Generated:** $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')

## Overview
Comprehensive dependency map of the Helios Platform components.

## Statistics
- **Total Components:** $($Graph.nodes.Count)
- **Dependencies:** $($Graph.edges.Count)
- **Circular Dependencies:** $($Graph.circular.Count)

## Component Hierarchy

### High-Degree Dependencies (Most Connected)
$(
    $Graph.nodes.Values | Sort-Object { $_.outdegree + $_.indegree } -Descending | 
    Select-Object -First 10 | ForEach-Object {
        $totalConnections = $_.indegree + $_.outdegree
        "- **$($_.name)** - $totalConnections connections (in: $($_.indegree), out: $($_.outdegree))"
    } | Out-String
)

### Dependency Chains

#### Root Dependencies (No Incoming)
$(
    $Graph.nodes.Values | Where-Object { $_.indegree -eq 0 } | ForEach-Object {
        "- $($_.name) → [$($_.outdegree) dependents]"
    } | Out-String
)

#### Leaf Dependencies (No Outgoing)
$(
    $Graph.nodes.Values | Where-Object { $_.outdegree -eq 0 } | ForEach-Object {
        "- [$($_.indegree) dependencies] → $($_.name)"
    } | Out-String
)

## Circular Dependencies
$(
    if ($Graph.circular.Count -gt 0) {
        "⚠️ **WARNING:** $($Graph.circular.Count) circular dependencies detected"
        "`n"
        $Graph.circular | ForEach-Object {
            "- $($_.source_name) ↔ $($_.target_name)"
        } | Out-String
    } else {
        "✓ No circular dependencies found"
    }
)

## Dependency Matrix

| Component | In-Degree | Out-Degree | Total | Status |
|-----------|-----------|------------|-------|--------|
$(
    $Graph.nodes.Values | Sort-Object { $_.outdegree } -Descending | ForEach-Object {
        $total = $_.indegree + $_.outdegree
        $status = if ($_.outdegree -eq 0) { "Leaf" } elseif ($_.indegree -eq 0) { "Root" } else { "Middle" }
        "| $($_.name) | $($_.indegree) | $($_.outdegree) | $total | $status |"
    } | Out-String
)

## Build Configuration Dependencies

$(
    $buildDeps = Get-BuildDependencies $DatabasePath
    if ($buildDeps) {
        "### Build Components"
        "`n"
        $buildDeps | Group-Object -Property build_name | ForEach-Object {
            "#### $($_.Name)"
            "`n"
            $_.Group | ForEach-Object {
                "- $($_.file_name) [$($_.inclusion_type)]"
            } | Out-String
        } | Out-String
    }
)

---
*Generated by Dependency Mapper v1.0*
"@
    
    return $md
}

function Generate-DotGraph {
    param([hashtable]$Graph)
    
    Write-Status "Generating DOT format graph..."
    
    $dot = @"
digraph DependencyGraph {
    rankdir=LR;
    concentrate=true;
    nodesep=0.5;
    ranksep=1.0;
    
    // Node definitions
    node [shape=box, style=filled, fillcolor=lightblue];
    
$(
    $Graph.nodes.Values | ForEach-Object {
        $fillColor = if ($_.outdegree -eq 0) { "lightgreen" } 
                    elseif ($_.indegree -eq 0) { "lightyellow" } 
                    else { "lightblue" }
        "`"$($_.id)_$($_.name.Replace(' ', '_'))`" [label=`"$($_.name)\n(in:$($_.indegree) out:$($_.outdegree))`", fillcolor=$fillColor];"
    } | Out-String
)
    
    // Edge definitions
    edge [fontsize=9];
    
$(
    $Graph.edges | ForEach-Object {
        $style = if ($_.circular) { "style=dashed,color=red" } else { "" }
        "`"$($_.source)_*`" -> `"$($_.target)_*`" [label=`"$($_.type)`", $style];"
    } | Out-String
)
    
    // Legend
    {
        rank=same;
        Legend [shape=none, margin=0, label=<
            <TABLE BORDER="1" CELLBORDER="1" CELLSPACING="0">
                <TR><TD COLSPAN="2">Legend</TD></TR>
                <TR><TD BGCOLOR="lightyellow">Root Nodes</TD><TD>No incoming deps</TD></TR>
                <TR><TD BGCOLOR="lightgreen">Leaf Nodes</TD><TD>No outgoing deps</TD></TR>
                <TR><TD BGCOLOR="lightblue">Middle Nodes</TD><TD>Both in &amp; out</TD></TR>
                <TR><TD STYLE="dashed">Circular</TD><TD>Circular dependency</TD></TR>
            </TABLE>
        >];
    }
}
"@
    
    return $dot
}

function Generate-JsonGraph {
    param([hashtable]$Graph)
    
    Write-Status "Generating JSON graph..."
    
    $json = @{
        metadata = @{
            generated = Get-Date -Format 'o'
            version = '1.0'
            stats = @{
                nodes = $Graph.nodes.Count
                edges = $Graph.edges.Count
                circular = $Graph.circular.Count
            }
        }
        nodes = $Graph.nodes.Values
        edges = $Graph.edges
        circular = $Graph.circular
    }
    
    return $json | ConvertTo-Json -Depth 10
}

function Save-Graphs {
    param([hashtable]$Graph)
    
    Write-Status "Saving generated files..."
    
    if (-not (Test-Path $OutputDir)) {
        New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
    }
    
    switch ($OutputFormat) {
        'markdown' {
            $mdPath = Join-Path $OutputDir "DEPENDENCY_GRAPH.md"
            $md = Generate-MarkdownGraph $Graph
            Set-Content -Path $mdPath -Value $md -Force
            Write-Status "Saved: $mdPath" "Success"
        }
        'dot' {
            $dotPath = Join-Path $OutputDir "dependencies.dot"
            $dot = Generate-DotGraph $Graph
            Set-Content -Path $dotPath -Value $dot -Force
            Write-Status "Saved: $dotPath" "Success"
        }
        'json' {
            $jsonPath = Join-Path $OutputDir "dependencies.json"
            $json = Generate-JsonGraph $Graph
            Set-Content -Path $jsonPath -Value $json -Force
            Write-Status "Saved: $jsonPath" "Success"
        }
        'all' {
            Save-Graphs @Graph | Where-Object { $_ }
        }
    }
}

# Main execution
Write-Host "`n╔════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║      DEPENDENCY MAPPER                 ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════╝`n" -ForegroundColor Cyan

if (-not (Test-Path $DatabasePath)) {
    Write-Status "Database not found: $DatabasePath" "Error"
    exit 1
}

# Load dependencies
$dependencies = Get-FileDependencies $DatabasePath

if (-not $dependencies -or $dependencies.Count -eq 0) {
    Write-Status "No dependencies found in database" "Warning"
} else {
    # Build graph
    $graphData = Build-DependencyGraph $dependencies
    
    # Generate outputs
    Save-Graphs $graphData
    
    # Display summary
    Write-Host "`n=== Dependency Summary ===" -ForegroundColor Cyan
    Write-Host "Components: $($graphData.nodes.Count)" -ForegroundColor Green
    Write-Host "Dependencies: $($graphData.edges.Count)" -ForegroundColor Green
    Write-Host "Circular: $($graphData.circular.Count)" -ForegroundColor $(if ($graphData.circular.Count -gt 0) { 'Yellow' } else { 'Green' })
    
    Write-Status "Dependency mapping complete!" "Success"
}

Write-Host ""
