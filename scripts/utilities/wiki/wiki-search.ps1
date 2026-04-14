#requires -Version 5.1
<#
.SYNOPSIS
    Query and search the wiki database with full-text search capabilities.

.DESCRIPTION
    Comprehensive wiki search tool that queries SQLite database to:
    - Search by keyword, tag, category, complexity, build
    - Find cross-references between files
    - Find orphaned files (no references)
    - Find files used in specific builds
    - Support SQL-like queries
    - Format results in multiple output styles
    - Export results to CSV/JSON
    - Show dependency information

.PARAMETER Query
    Search query string (keyword-based)

.PARAMETER Category
    Filter by category

.PARAMETER Tag
    Filter by tag

.PARAMETER Complexity
    Filter by complexity (simple, moderate, complex)

.PARAMETER Build
    Find files in specific build

.PARAMETER Type
    Filter by file type

.PARAMETER ShowOrphaned
    Show files with no references

.PARAMETER ShowDependencies
    Show dependency information

.PARAMETER ExportPath
    Export results to CSV or JSON file

.PARAMETER DatabasePath
    Path to wiki database

.EXAMPLE
    .\wiki-search.ps1 -Query "optimization"
    .\wiki-search.ps1 -Category "Scripts"
    .\wiki-search.ps1 -ShowOrphaned
    .\wiki-search.ps1 -Query "security" -ExportPath results.csv
#>

[CmdletBinding()]
param(
    [string]$Query = '',
    [string]$Category = '',
    [string]$Tag = '',
    [ValidateSet('simple', 'moderate', 'complex', '')]$Complexity = '',
    [string]$Build = '',
    [string]$Type = '',
    [switch]$ShowOrphaned,
    [switch]$ShowDependencies,
    [string]$ExportPath = '',
    [string]$DatabasePath = "C:\Users\ADMIN\helios-platform\docs\wiki.db"
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

# ============================================================================
# Logging & Output Functions
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
        'RESULT'  = 'Magenta'
        'DATA'    = 'White'
    }
    
    Write-Host "$Message" -ForegroundColor $colors[$Level]
}

function Format-Table-Results {
    param(
        [array]$Results,
        [array]$Properties = @('file_id', 'file_name', 'category_name', 'complexity')
    )
    
    if ($Results.Count -eq 0) {
        Write-Log "No results found." 'WARN'
        return
    }
    
    Write-Log "Found $($Results.Count) result(s):" 'SUCCESS'
    Write-Log "" 'DATA'
    
    $Results | Format-Table -Property $Properties -AutoSize
}

function Export-Results {
    param(
        [array]$Results,
        [string]$ExportPath,
        [string]$Format = 'csv'
    )
    
    if (-not $Results -or $Results.Count -eq 0) {
        Write-Log "No results to export." 'WARN'
        return
    }
    
    try {
        if ($Format -eq 'csv') {
            $Results | Export-Csv -Path $ExportPath -NoTypeInformation -Encoding UTF8
            Write-Log "Results exported to CSV: $ExportPath" 'SUCCESS'
        } elseif ($Format -eq 'json') {
            $Results | ConvertTo-Json | Out-File -Path $ExportPath -Encoding UTF8
            Write-Log "Results exported to JSON: $ExportPath" 'SUCCESS'
        }
    } catch {
        Write-Log "Error exporting results: $_" 'ERROR'
    }
}

# ============================================================================
# Database Connection & Query Functions
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
        
        Write-Log "Connected to database: $DatabasePath" 'INFO'
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
        Write-Log "Query: $Query" 'WARN'
        throw
    }
}

# ============================================================================
# Search Functions
# ============================================================================

function Search-ByKeyword {
    param(
        [System.Data.SQLite.SQLiteConnection]$Connection,
        [string]$Query
    )
    
    Write-Log "Searching for: '$Query'" 'INFO'
    
    $sql = @"
SELECT 
    f.file_id,
    f.file_name,
    f.file_path,
    f.file_type,
    c.category_name,
    f.complexity,
    f.language,
    f.line_count,
    f.purpose
FROM files f
LEFT JOIN categories c ON f.category_id = c.category_id
WHERE 
    f.file_name LIKE '%$Query%' OR
    f.purpose LIKE '%$Query%' OR
    f.tags LIKE '%$Query%' OR
    f.file_type LIKE '%$Query%' OR
    c.category_name LIKE '%$Query%'
ORDER BY f.modified_date DESC
LIMIT 100
"@
    
    Invoke-DatabaseQuery -Connection $Connection -Query $sql
}

function Search-ByCategory {
    param(
        [System.Data.SQLite.SQLiteConnection]$Connection,
        [string]$Category
    )
    
    Write-Log "Searching in category: '$Category'" 'INFO'
    
    $sql = @"
SELECT 
    f.file_id,
    f.file_name,
    f.file_path,
    f.file_type,
    c.category_name,
    f.complexity,
    f.language,
    f.line_count,
    f.purpose
FROM files f
LEFT JOIN categories c ON f.category_id = c.category_id
WHERE c.category_name LIKE '%$Category%'
ORDER BY f.file_name
"@
    
    Invoke-DatabaseQuery -Connection $Connection -Query $sql
}

function Search-ByTag {
    param(
        [System.Data.SQLite.SQLiteConnection]$Connection,
        [string]$Tag
    )
    
    Write-Log "Searching for tag: '$Tag'" 'INFO'
    
    $sql = @"
SELECT 
    f.file_id,
    f.file_name,
    f.file_path,
    f.tags,
    c.category_name,
    f.complexity
FROM files f
LEFT JOIN categories c ON f.category_id = c.category_id
WHERE f.tags LIKE '%$Tag%'
ORDER BY f.file_name
"@
    
    Invoke-DatabaseQuery -Connection $Connection -Query $sql
}

function Search-ByComplexity {
    param(
        [System.Data.SQLite.SQLiteConnection]$Connection,
        [string]$Complexity
    )
    
    Write-Log "Searching for complexity: '$Complexity'" 'INFO'
    
    $sql = @"
SELECT 
    f.file_id,
    f.file_name,
    f.file_path,
    f.complexity,
    f.language,
    f.line_count,
    c.category_name
FROM files f
LEFT JOIN categories c ON f.category_id = c.category_id
WHERE f.complexity = '$Complexity'
ORDER BY f.file_name
"@
    
    Invoke-DatabaseQuery -Connection $Connection -Query $sql
}

function Search-ByBuild {
    param(
        [System.Data.SQLite.SQLiteConnection]$Connection,
        [string]$BuildName
    )
    
    Write-Log "Searching for files in build: '$BuildName'" 'INFO'
    
    $sql = @"
SELECT DISTINCT
    f.file_id,
    f.file_name,
    f.file_path,
    f.file_type,
    b.build_name,
    f.complexity
FROM build_files bf
JOIN files f ON bf.file_id = f.file_id
JOIN builds b ON bf.build_id = b.build_id
WHERE b.build_name LIKE '%$BuildName%'
ORDER BY f.file_name
"@
    
    Invoke-DatabaseQuery -Connection $Connection -Query $sql
}

function Search-OrphanedFiles {
    param(
        [System.Data.SQLite.SQLiteConnection]$Connection
    )
    
    Write-Log "Searching for orphaned files (no references)..." 'INFO'
    
    # First, try to use the view if it exists
    $sql = @"
SELECT
    f.file_id,
    f.file_name,
    f.file_path,
    f.file_type,
    f.created_date,
    c.category_name,
    f.line_count
FROM files f
LEFT JOIN categories c ON f.category_id = c.category_id
LEFT JOIN cross_references xr ON f.file_id = xr.source_file_id OR f.file_id = xr.target_file_id
LEFT JOIN dependencies d ON f.file_id = d.dependent_file_id OR f.file_id = d.required_file_id
WHERE xr.reference_id IS NULL AND d.dependency_id IS NULL
ORDER BY f.created_date DESC
"@
    
    Invoke-DatabaseQuery -Connection $Connection -Query $sql
}

function Search-CrossReferences {
    param(
        [System.Data.SQLite.SQLiteConnection]$Connection,
        [int]$FileId
    )
    
    Write-Log "Searching for cross-references for file ID: $FileId" 'INFO'
    
    $sql = @"
SELECT
    xr.reference_id,
    f1.file_name as source_file,
    f2.file_name as target_file,
    xr.reference_type,
    xr.line_number,
    xr.context
FROM cross_references xr
JOIN files f1 ON xr.source_file_id = f1.file_id
JOIN files f2 ON xr.target_file_id = f2.file_id
WHERE xr.source_file_id = $FileId OR xr.target_file_id = $FileId
ORDER BY xr.reference_type
"@
    
    Invoke-DatabaseQuery -Connection $Connection -Query $sql
}

function Search-Dependencies {
    param(
        [System.Data.SQLite.SQLiteConnection]$Connection,
        [int]$FileId
    )
    
    Write-Log "Searching for dependencies for file ID: $FileId" 'INFO'
    
    $sql = @"
SELECT
    d.dependency_id,
    f1.file_name as dependent_file,
    f2.file_name as required_file,
    d.dependency_type,
    d.depth,
    d.is_circular
FROM dependencies d
JOIN files f1 ON d.dependent_file_id = f1.file_id
JOIN files f2 ON d.required_file_id = f2.file_id
WHERE d.dependent_file_id = $FileId OR d.required_file_id = $FileId
ORDER BY d.depth DESC, d.dependency_type
"@
    
    Invoke-DatabaseQuery -Connection $Connection -Query $sql
}

# ============================================================================
# Statistics Functions
# ============================================================================

function Get-SearchStatistics {
    param(
        [System.Data.SQLite.SQLiteConnection]$Connection,
        [array]$Results
    )
    
    if (-not $Results -or $Results.Count -eq 0) {
        return
    }
    
    Write-Log "" 'DATA'
    Write-Log "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" 'DATA'
    Write-Log "Search Statistics:" 'RESULT'
    Write-Log "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" 'DATA'
    
    Write-Log "Total Results: $($Results.Count)" 'DATA'
    
    # Count by file type if column exists
    if ($Results[0].PSObject.Properties.Name -contains 'file_type') {
        $typeStats = $Results | Group-Object -Property file_type | Select-Object Name, Count
        Write-Log "" 'DATA'
        Write-Log "By File Type:" 'DATA'
        $typeStats | ForEach-Object {
            Write-Log "  $($_.Name): $($_.Count)" 'DATA'
        }
    }
    
    # Count by category if column exists
    if ($Results[0].PSObject.Properties.Name -contains 'category_name') {
        $catStats = $Results | Where-Object { $_.category_name } | Group-Object -Property category_name | Select-Object Name, Count
        if ($catStats.Count -gt 0) {
            Write-Log "" 'DATA'
            Write-Log "By Category:" 'DATA'
            $catStats | ForEach-Object {
                Write-Log "  $($_.Name): $($_.Count)" 'DATA'
            }
        }
    }
    
    # Count by complexity if column exists
    if ($Results[0].PSObject.Properties.Name -contains 'complexity') {
        $complexityStats = $Results | Where-Object { $_.complexity } | Group-Object -Property complexity | Select-Object Name, Count
        if ($complexityStats.Count -gt 0) {
            Write-Log "" 'DATA'
            Write-Log "By Complexity:" 'DATA'
            $complexityStats | ForEach-Object {
                Write-Log "  $($_.Name): $($_.Count)" 'DATA'
            }
        }
    }
    
    Write-Log "" 'DATA'
}

# ============================================================================
# Main Execution
# ============================================================================

try {
    Write-Log "╔════════════════════════════════════════════╗" 'RESULT'
    Write-Log "║        HELIOS PLATFORM WIKI SEARCH         ║" 'RESULT'
    Write-Log "╚════════════════════════════════════════════╝" 'RESULT'
    Write-Log "" 'DATA'
    
    # Validate inputs
    $hasSearchCriteria = -not [string]::IsNullOrWhiteSpace($Query) -or
                        -not [string]::IsNullOrWhiteSpace($Category) -or
                        -not [string]::IsNullOrWhiteSpace($Tag) -or
                        -not [string]::IsNullOrWhiteSpace($Build) -or
                        -not [string]::IsNullOrWhiteSpace($Complexity) -or
                        $ShowOrphaned
    
    if (-not $hasSearchCriteria) {
        Write-Log "Please specify at least one search criterion:" 'WARN'
        Write-Log "  -Query          : Keyword search" 'DATA'
        Write-Log "  -Category       : Filter by category" 'DATA'
        Write-Log "  -Tag            : Filter by tag" 'DATA'
        Write-Log "  -Complexity     : Filter by complexity (simple/moderate/complex)" 'DATA'
        Write-Log "  -Build          : Find files in build" 'DATA'
        Write-Log "  -ShowOrphaned   : Show files with no references" 'DATA'
        exit 1
    }
    
    # Connect to database
    $connection = New-DatabaseConnection -DatabasePath $DatabasePath
    
    # Perform searches
    $results = $null
    
    if ($ShowOrphaned) {
        $results = Search-OrphanedFiles -Connection $connection
    } elseif (-not [string]::IsNullOrWhiteSpace($Query)) {
        $results = Search-ByKeyword -Connection $connection -Query $Query
    } elseif (-not [string]::IsNullOrWhiteSpace($Category)) {
        $results = Search-ByCategory -Connection $connection -Category $Category
    } elseif (-not [string]::IsNullOrWhiteSpace($Tag)) {
        $results = Search-ByTag -Connection $connection -Tag $Tag
    } elseif (-not [string]::IsNullOrWhiteSpace($Complexity)) {
        $results = Search-ByComplexity -Connection $connection -Complexity $Complexity
    } elseif (-not [string]::IsNullOrWhiteSpace($Build)) {
        $results = Search-ByBuild -Connection $connection -BuildName $Build
    }
    
    # Display results
    if ($results -and $results.Rows.Count -gt 0) {
        Write-Log "" 'DATA'
        Format-Table-Results -Results $results
        Get-SearchStatistics -Connection $connection -Results $results
        
        # Export if requested
        if (-not [string]::IsNullOrWhiteSpace($ExportPath)) {
            $format = if ($ExportPath.EndsWith('.json')) { 'json' } else { 'csv' }
            Export-Results -Results $results -ExportPath $ExportPath -Format $format
        }
    } else {
        Write-Log "No results found." 'WARN'
    }
    
    # Show dependencies if requested and results exist
    if ($ShowDependencies -and $results -and $results.Rows.Count -gt 0) {
        $firstFileId = $results.Rows[0]['file_id']
        if ($firstFileId) {
            Write-Log "" 'DATA'
            Write-Log "Showing dependencies for first result:" 'INFO'
            $deps = Search-Dependencies -Connection $connection -FileId $firstFileId
            if ($deps -and $deps.Rows.Count -gt 0) {
                $deps | Format-Table
            }
        }
    }
    
    $connection.Close()
    
    Write-Log "" 'DATA'
    Write-Log "Search completed successfully." 'SUCCESS'
    
} catch {
    Write-Log "FATAL ERROR: $_" 'ERROR'
    Write-Log $_.Exception.StackTrace 'WARN'
    exit 1
}
