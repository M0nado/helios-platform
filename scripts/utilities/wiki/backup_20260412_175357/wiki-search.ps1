<#
.SYNOPSIS
    Query wiki database with flexible search and filtering

.DESCRIPTION
    Powerful search tool for the wiki database:
    - Search by keyword (full-text)
    - Filter by category, tag, complexity
    - Find cross-references and relationships
    - Identify orphaned files
    - Execute custom SQL queries
    - Export results in multiple formats

.PARAMETER Query
    Search keyword or SQL query

.PARAMETER SearchType
    'keyword', 'category', 'tag', 'complexity', 'build', 'orphaned', 'sql'

.PARAMETER Category
    Filter by category

.PARAMETER Complexity
    Filter by complexity (simple|moderate|complex|advanced)

.PARAMETER Format
    Output format (table|json|csv)

.PARAMETER DatabasePath
    SQLite database path

.EXAMPLE
    .\wiki-search.ps1 -Query "authentication" -SearchType keyword

.EXAMPLE
    .\wiki-search.ps1 -SearchType orphaned

.EXAMPLE
    .\wiki-search.ps1 -SearchType sql -Query "SELECT * FROM files WHERE complexity='advanced'"

.NOTES
    Location: scripts/utilities/wiki/wiki-search.ps1
    Version: 1.0
#>

param(
    [string]$Query,
    [ValidateSet('keyword', 'category', 'tag', 'complexity', 'build', 'orphaned', 'sql', 'conflicts', 'dependencies')]
    [string]$SearchType = 'keyword',
    [string]$Category,
    [ValidateSet('simple', 'moderate', 'complex', 'advanced')]
    [string]$Complexity,
    [ValidateSet('table', 'json', 'csv')]
    [string]$Format = 'table',
    [string]$DatabasePath = "C:\Users\ADMIN\helios-platform\docs\wiki.db",
    [switch]$ShowDetails
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

function Test-DatabaseExists {
    if (-not (Test-Path $DatabasePath)) {
        Write-Status "Database not found: $DatabasePath" "Error"
        return $false
    }
    return $true
}

function Execute-Query {
    param([string]$Sql, [string]$DbPath)
    
    try {
        $result = & sqlite3 -json $DbPath $Sql 2>&1
        if ($LASTEXITCODE -eq 0) {
            return $result | ConvertFrom-Json -ErrorAction SilentlyContinue
        }
        return $null
    }
    catch {
        Write-Status "Query error: $_" "Error"
        return $null
    }
}

function Search-Keyword {
    param([string]$Keyword, [string]$DbPath)
    
    Write-Status "Searching for keyword: $Keyword"
    
    $sql = @"
SELECT f.id, f.name, f.category_id, f.complexity, f.purpose, f.path
FROM files f
WHERE f.name LIKE '%$Keyword%' 
   OR f.purpose LIKE '%$Keyword%'
ORDER BY f.complexity, f.name;
"@
    
    return Execute-Query $sql $DbPath
}

function Search-Category {
    param([string]$CategoryName, [string]$DbPath)
    
    Write-Status "Searching in category: $CategoryName"
    
    $sql = @"
SELECT f.id, f.name, f.file_type, f.complexity, f.purpose
FROM files f
JOIN categories c ON f.category_id = c.id
WHERE c.name = '$CategoryName'
ORDER BY f.name;
"@
    
    return Execute-Query $sql $DbPath
}

function Search-Complexity {
    param([string]$ComplexityLevel, [string]$DbPath)
    
    Write-Status "Filtering by complexity: $ComplexityLevel"
    
    $sql = @"
SELECT f.id, f.name, f.category_id, f.complexity, f.purpose, COUNT(xr.id) as references
FROM files f
LEFT JOIN cross_references xr ON f.id = xr.source_file_id
WHERE f.complexity = '$ComplexityLevel'
GROUP BY f.id
ORDER BY f.name;
"@
    
    return Execute-Query $sql $DbPath
}

function Find-Orphaned {
    param([string]$DbPath)
    
    Write-Status "Finding orphaned files..."
    
    $sql = @"
SELECT f.id, f.name, f.path, f.complexity, f.status
FROM files f
WHERE f.id NOT IN (SELECT source_file_id FROM cross_references)
  AND f.id NOT IN (SELECT target_file_id FROM cross_references)
  AND f.status = 'active'
ORDER BY f.complexity DESC, f.name;
"@
    
    return Execute-Query $sql $DbPath
}

function Find-Conflicts {
    param([string]$DbPath)
    
    Write-Status "Finding conflicting references..."
    
    $sql = @"
SELECT xr.id, src.name as source, tgt.name as target, xr.conflict_notes, xr.reference_type
FROM cross_references xr
JOIN files src ON xr.source_file_id = src.id
LEFT JOIN files tgt ON xr.target_file_id = tgt.id
WHERE xr.conflict_potential = 1
ORDER BY xr.source_file_id, xr.target_file_id;
"@
    
    return Execute-Query $sql $DbPath
}

function Find-Dependencies {
    param([string]$FilePath, [string]$DbPath)
    
    Write-Status "Finding dependencies for: $FilePath"
    
    $sql = @"
WITH RECURSIVE dep_chain AS (
    SELECT d.*, f.name, f.path, 1 as depth
    FROM dependencies d
    JOIN files f ON d.target_id = f.id
    WHERE d.source_id = (SELECT id FROM files WHERE path = '$FilePath')
    
    UNION ALL
    
    SELECT d.*, f.name, f.path, dc.depth + 1
    FROM dependencies d
    JOIN files f ON d.target_id = f.id
    JOIN dep_chain dc ON d.source_id = dc.source_id
    WHERE dc.depth < 5
)
SELECT DISTINCT * FROM dep_chain
ORDER BY depth, name;
"@
    
    return Execute-Query $sql $DbPath
}

function Format-Output {
    param([array]$Results, [ValidateSet('table', 'json', 'csv')]$OutputFormat)
    
    if (-not $Results) {
        Write-Status "No results found" "Warning"
        return
    }
    
    switch ($OutputFormat) {
        'table' {
            $Results | Format-Table -AutoSize | Out-String
        }
        'json' {
            $Results | ConvertTo-Json
        }
        'csv' {
            $Results | ConvertTo-Csv -NoTypeInformation
        }
    }
}

function Show-Statistics {
    param([string]$DbPath)
    
    Write-Status "Calculating statistics..."
    
    $stats = @{
        'Total Files' = (Execute-Query "SELECT COUNT(*) as count FROM files;" $DbPath).count
        'Active Files' = (Execute-Query "SELECT COUNT(*) as count FROM files WHERE status='active';" $DbPath).count
        'Categories' = (Execute-Query "SELECT COUNT(*) as count FROM categories;" $DbPath).count
        'Cross-References' = (Execute-Query "SELECT COUNT(*) as count FROM cross_references;" $DbPath).count
        'Conflicts' = (Execute-Query "SELECT COUNT(*) as count FROM cross_references WHERE conflict_potential=1;" $DbPath).count
    }
    
    Write-Host "`n=== Database Statistics ===" -ForegroundColor Cyan
    $stats.GetEnumerator() | ForEach-Object {
        Write-Host "$($_.Key): $($_.Value.count)" -ForegroundColor Green
    }
}

# Main execution
Write-Host "`n╔════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║          WIKI SEARCH UTILITY           ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════╝`n" -ForegroundColor Cyan

if (-not (Test-DatabaseExists)) {
    exit 1
}

$results = $null

switch ($SearchType) {
    'keyword' {
        if (-not $Query) { Write-Status "Query required for keyword search" "Error"; exit 1 }
        $results = Search-Keyword $Query $DatabasePath
    }
    'category' {
        if (-not $Category -and -not $Query) { Write-Status "Category required" "Error"; exit 1 }
        $results = Search-Category ($Category ?? $Query) $DatabasePath
    }
    'complexity' {
        if (-not $Complexity -and -not $Query) { Write-Status "Complexity level required" "Error"; exit 1 }
        $results = Search-Complexity ($Complexity ?? $Query) $DatabasePath
    }
    'orphaned' {
        $results = Find-Orphaned $DatabasePath
    }
    'conflicts' {
        $results = Find-Conflicts $DatabasePath
    }
    'dependencies' {
        if (-not $Query) { Write-Status "File path required" "Error"; exit 1 }
        $results = Find-Dependencies $Query $DatabasePath
    }
    'sql' {
        if (-not $Query) { Write-Status "SQL query required" "Error"; exit 1 }
        Write-Status "Executing custom query..."
        $results = Execute-Query $Query $DatabasePath
    }
}

if ($results) {
    Write-Host (Format-Output $results $Format)
    Write-Status "$($results.Count) result(s) found" "Success"
}

Show-Statistics $DatabasePath
Write-Host ""
