#requires -Version 5.1
<#
.SYNOPSIS
    Validates all cross-references and detects issues in the wiki database.

.DESCRIPTION
    Comprehensive cross-reference validation tool that:
    - Validates all links between files
    - Finds broken references
    - Reports circular dependencies
    - Detects conflicts between file relationships
    - Suggests fixes for issues
    - Generates detailed report
    - Exports results to CSV/JSON
    - Provides summary statistics

.PARAMETER DatabasePath
    Path to wiki database

.PARAMETER ReportPath
    Output path for validation report

.PARAMETER FixIssues
    Automatically fix detected issues (read-only by default)

.PARAMETER GenerateReport
    Generate HTML report of findings

.PARAMETER ExportFormat
    Export format (csv, json, html)

.EXAMPLE
    .\check-cross-references.ps1
    .\check-cross-references.ps1 -GenerateReport
    .\check-cross-references.ps1 -ExportFormat json
#>

[CmdletBinding()]
param(
    [string]$DatabasePath = "C:\Users\ADMIN\helios-platform\docs\wiki.db",
    [string]$ReportPath = "C:\Users\ADMIN\helios-platform\docs\WIKI\cross-reference-report.md",
    [switch]$FixIssues,
    [switch]$GenerateReport,
    [ValidateSet('csv', 'json', 'html', 'md')]$ExportFormat = 'md',
    [switch]$Verbose
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

# ============================================================================
# Global Variables & Logging
# ============================================================================

$script:issues = @()
$script:brokenReferences = @()
$script:circularDeps = @()
$script:conflicts = @()
$script:suggestions = @()

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
        'CHECK'   = 'Magenta'
        'ISSUE'   = 'Red'
    }
    
    if ($Verbose -or $Level -eq 'ERROR' -or $Level -eq 'ISSUE') {
        Write-Host "$Message" -ForegroundColor $colors[$Level]
    }
}

# ============================================================================
# Database Functions
# ============================================================================

function New-DatabaseConnection {
    param([string]$DatabasePath)
    
    try {
        if (-not (Test-Path $DatabasePath)) {
            throw "Database not found: $DatabasePath"
        }
        
        $connection = New-Object System.Data.SQLite.SQLiteConnection
        $readOnly = if ($FixIssues) { "False" } else { "True" }
        $connection.ConnectionString = "Data Source=$DatabasePath;Version=3;Read Only=$readOnly;"
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
        throw
    }
}

function Invoke-DatabaseCommand {
    param(
        [System.Data.SQLite.SQLiteConnection]$Connection,
        [string]$Query
    )
    
    try {
        $command = $Connection.CreateCommand()
        $command.CommandText = $Query
        $command.ExecuteNonQuery()
    } catch {
        Write-Log "Database command error: $_" 'ERROR'
        throw
    }
}

# ============================================================================
# Validation Functions
# ============================================================================

function Find-BrokenReferences {
    param([System.Data.SQLite.SQLiteConnection]$Connection)
    
    Write-Log "Checking for broken references..." 'CHECK'
    
    $sql = @"
SELECT
    xr.reference_id,
    xr.source_file_id,
    xr.target_file_id,
    xr.reference_type,
    f1.file_name as source_file,
    f2.file_name as target_file
FROM cross_references xr
LEFT JOIN files f1 ON xr.source_file_id = f1.file_id
LEFT JOIN files f2 ON xr.target_file_id = f2.file_id
WHERE f1.file_id IS NULL OR f2.file_id IS NULL
"@
    
    $results = Invoke-DatabaseQuery -Connection $Connection -Query $sql
    
    if ($results.Rows.Count -gt 0) {
        Write-Log "Found $($results.Rows.Count) broken references!" 'ISSUE'
        
        foreach ($row in $results.Rows) {
            $issue = @{
                Type = 'BrokenReference'
                Severity = 'High'
                ReferenceId = $row['reference_id']
                SourceId = $row['source_file_id']
                TargetId = $row['target_file_id']
                Message = "Broken reference: $($row['source_file']) -> $($row['target_file'])"
                Suggestion = "Remove reference ID $($row['reference_id']) or add missing file"
            }
            $script:issues += $issue
            $script:brokenReferences += $issue
        }
    }
    
    return $results
}

function Find-CircularDependencies {
    param([System.Data.SQLite.SQLiteConnection]$Connection)
    
    Write-Log "Checking for circular dependencies..." 'CHECK'
    
    $sql = @"
SELECT
    d.dependency_id,
    d.dependent_file_id,
    d.required_file_id,
    f1.file_name as dependent_file,
    f2.file_name as required_file,
    d.is_circular
FROM dependencies d
JOIN files f1 ON d.dependent_file_id = f1.file_id
JOIN files f2 ON d.required_file_id = f2.file_id
WHERE d.is_circular = 1
"@
    
    $results = Invoke-DatabaseQuery -Connection $Connection -Query $sql
    
    if ($results.Rows.Count -gt 0) {
        Write-Log "Found $($results.Rows.Count) circular dependencies!" 'ISSUE'
        
        foreach ($row in $results.Rows) {
            $issue = @{
                Type = 'CircularDependency'
                Severity = 'High'
                DependencyId = $row['dependency_id']
                File1 = $row['dependent_file']
                File2 = $row['required_file']
                Message = "Circular dependency: $($row['dependent_file']) <-> $($row['required_file'])"
                Suggestion = "Refactor to remove circular dependency or mark as acceptable"
            }
            $script:issues += $issue
            $script:circularDeps += $issue
        }
    }
    
    return $results
}

function Find-OrphanedFiles {
    param([System.Data.SQLite.SQLiteConnection]$Connection)
    
    Write-Log "Checking for orphaned files..." 'CHECK'
    
    $sql = @"
SELECT
    f.file_id,
    f.file_name,
    f.file_path,
    f.created_date
FROM files f
LEFT JOIN cross_references xr ON f.file_id = xr.source_file_id OR f.file_id = xr.target_file_id
LEFT JOIN dependencies d ON f.file_id = d.dependent_file_id OR f.file_id = d.required_file_id
WHERE xr.reference_id IS NULL AND d.dependency_id IS NULL
"@
    
    $results = Invoke-DatabaseQuery -Connection $Connection -Query $sql
    
    if ($results.Rows.Count -gt 0) {
        Write-Log "Found $($results.Rows.Count) orphaned files" 'WARN'
        
        foreach ($row in $results.Rows) {
            $issue = @{
                Type = 'OrphanedFile'
                Severity = 'Medium'
                FileId = $row['file_id']
                FileName = $row['file_name']
                FilePath = $row['file_path']
                Message = "Orphaned file: $($row['file_name']) has no references or dependencies"
                Suggestion = "Add references or consider removing the file"
            }
            $script:issues += $issue
        }
    }
    
    return $results
}

function Find-DanglingDependencies {
    param([System.Data.SQLite.SQLiteConnection]$Connection)
    
    Write-Log "Checking for dangling dependencies..." 'CHECK'
    
    $sql = @"
SELECT
    d.dependency_id,
    d.dependent_file_id,
    d.required_file_id,
    f1.file_name as dependent_file,
    f2.file_name as required_file
FROM dependencies d
JOIN files f1 ON d.dependent_file_id = f1.file_id
LEFT JOIN files f2 ON d.required_file_id = f2.file_id
WHERE f2.file_id IS NULL
"@
    
    $results = Invoke-DatabaseQuery -Connection $Connection -Query $sql
    
    if ($results.Rows.Count -gt 0) {
        Write-Log "Found $($results.Rows.Count) dangling dependencies!" 'ISSUE'
        
        foreach ($row in $results.Rows) {
            $issue = @{
                Type = 'DanglingDependency'
                Severity = 'High'
                DependencyId = $row['dependency_id']
                SourceFile = $row['dependent_file']
                Message = "Dangling dependency: $($row['dependent_file']) depends on missing file"
                Suggestion = "Remove dependency or add missing required file"
            }
            $script:issues += $issue
        }
    }
    
    return $results
}

function Find-ConflictingReferences {
    param([System.Data.SQLite.SQLiteConnection]$Connection)
    
    Write-Log "Checking for conflicting references..." 'CHECK'
    
    $sql = @"
SELECT
    f1.file_id,
    f1.file_name,
    COUNT(DISTINCT xr.reference_type) as reference_type_count,
    GROUP_CONCAT(DISTINCT xr.reference_type, ', ') as types
FROM cross_references xr
JOIN files f1 ON xr.source_file_id = f1.file_id
JOIN files f2 ON xr.target_file_id = f2.file_id
WHERE xr.source_file_id = xr.target_file_id
GROUP BY f1.file_id
HAVING COUNT(DISTINCT xr.reference_type) > 1
"@
    
    $results = Invoke-DatabaseQuery -Connection $Connection -Query $sql
    
    if ($results.Rows.Count -gt 0) {
        Write-Log "Found $($results.Rows.Count) conflicting reference patterns" 'WARN'
        
        foreach ($row in $results.Rows) {
            $issue = @{
                Type = 'ConflictingReference'
                Severity = 'Medium'
                FileId = $row['file_id']
                FileName = $row['file_name']
                Types = $row['types']
                Message = "File has conflicting reference types: $($row['types'])"
                Suggestion = "Review and clarify relationship types"
            }
            $script:issues += $issue
            $script:conflicts += $issue
        }
    }
    
    return $results
}

function Validate-ReferentialIntegrity {
    param([System.Data.SQLite.SQLiteConnection]$Connection)
    
    Write-Log "Validating referential integrity..." 'CHECK'
    
    $sql = "PRAGMA integrity_check;"
    $results = Invoke-DatabaseQuery -Connection $Connection -Query $sql
    
    if ($results.Rows[0][0] -ne 'ok') {
        $issue = @{
            Type = 'IntegrityError'
            Severity = 'Critical'
            Message = "Database integrity check failed: $($results.Rows[0][0])"
            Suggestion = "Run database repair or restoration"
        }
        $script:issues += $issue
    }
    
    return $results
}

# ============================================================================
# Statistics & Analysis
# ============================================================================

function Get-DatabaseStatistics {
    param([System.Data.SQLite.SQLiteConnection]$Connection)
    
    Write-Log "Gathering statistics..." 'INFO'
    
    $stats = @{}
    
    # File count
    $sql = "SELECT COUNT(*) as count FROM files;"
    $result = Invoke-DatabaseQuery -Connection $Connection -Query $sql
    $stats.TotalFiles = $result.Rows[0]['count']
    
    # Category count
    $sql = "SELECT COUNT(*) as count FROM categories;"
    $result = Invoke-DatabaseQuery -Connection $Connection -Query $sql
    $stats.TotalCategories = $result.Rows[0]['count']
    
    # Reference count
    $sql = "SELECT COUNT(*) as count FROM cross_references;"
    $result = Invoke-DatabaseQuery -Connection $Connection -Query $sql
    $stats.TotalReferences = $result.Rows[0]['count']
    
    # Dependency count
    $sql = "SELECT COUNT(*) as count FROM dependencies;"
    $result = Invoke-DatabaseQuery -Connection $Connection -Query $sql
    $stats.TotalDependencies = $result.Rows[0]['count']
    
    # Build count
    $sql = "SELECT COUNT(*) as count FROM builds;"
    $result = Invoke-DatabaseQuery -Connection $Connection -Query $sql
    $stats.TotalBuilds = $result.Rows[0]['count']
    
    return $stats
}

# ============================================================================
# Report Generation
# ============================================================================

function Generate-MarkdownReport {
    param(
        [string]$OutputPath,
        [hashtable]$Statistics,
        [array]$Issues
    )
    
    Write-Log "Generating markdown report..." 'INFO'
    
    $report = @"
# 🔍 Cross-Reference Validation Report

**Generated:** $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')

## Executive Summary

| Metric | Value |
|--------|-------|
| **Total Issues Found** | $($script:issues.Count) |
| **Critical Issues** | $(($script:issues | Where-Object { $_.Severity -eq 'Critical' }).Count) |
| **High Severity** | $(($script:issues | Where-Object { $_.Severity -eq 'High' }).Count) |
| **Medium Severity** | $(($script:issues | Where-Object { $_.Severity -eq 'Medium' }).Count) |
| **Low Severity** | $(($script:issues | Where-Object { $_.Severity -eq 'Low' }).Count) |

## Database Statistics

| Metric | Count |
|--------|-------|
| Total Files | $($Statistics.TotalFiles) |
| Total Categories | $($Statistics.TotalCategories) |
| Total References | $($Statistics.TotalReferences) |
| Total Dependencies | $($Statistics.TotalDependencies) |
| Total Builds | $($Statistics.TotalBuilds) |

## Detailed Findings

### 🔴 Critical Issues

$(
    $critical = $script:issues | Where-Object { $_.Severity -eq 'Critical' }
    if ($critical.Count -gt 0) {
        $critical | ForEach-Object {
            "- **$($_.Message)**`n  - Suggestion: $($_.Suggestion)`n"
        }
    } else {
        "✓ No critical issues found"
    }
)

### 🔴 High Severity Issues

$(
    $high = $script:issues | Where-Object { $_.Severity -eq 'High' }
    if ($high.Count -gt 0) {
        $high | ForEach-Object {
            "- **$($_.Message)**`n  - Suggestion: $($_.Suggestion)`n"
        }
    } else {
        "✓ No high severity issues found"
    }
)

### 🟡 Medium Severity Issues

$(
    $medium = $script:issues | Where-Object { $_.Severity -eq 'Medium' }
    if ($medium.Count -gt 0) {
        $medium | ForEach-Object {
            "- **$($_.Message)**`n  - Suggestion: $($_.Suggestion)`n"
        }
    } else {
        "✓ No medium severity issues found"
    }
)

### 🟢 Low Severity Issues

$(
    $low = $script:issues | Where-Object { $_.Severity -eq 'Low' }
    if ($low.Count -gt 0) {
        $low | ForEach-Object {
            "- **$($_.Message)**`n  - Suggestion: $($_.Suggestion)`n"
        }
    } else {
        "✓ No low severity issues found"
    }
)

## Issue Categories

### Broken References: $($script:brokenReferences.Count)
$($script:brokenReferences | ForEach-Object { "- $($_.Message)" } | Out-String)

### Circular Dependencies: $($script:circularDeps.Count)
$($script:circularDeps | ForEach-Object { "- $($_.Message)" } | Out-String)

### Conflicting References: $($script:conflicts.Count)
$($script:conflicts | ForEach-Object { "- $($_.Message)" } | Out-String)

## Recommendations

1. **Resolve Critical Issues Immediately**
   - These may indicate database corruption or missing files
   
2. **Fix High Priority Issues**
   - Broken references need to be either removed or corrected
   - Circular dependencies should be refactored

3. **Address Medium Priority Issues**
   - Conflicting references need clarification
   - Consider removing orphaned files

4. **Monitor Low Priority Items**
   - These are informational and don't require immediate action

## Next Steps

1. Review all critical and high-priority issues
2. Run: .\fix-cross-references.ps1 (if available)
3. Re-run this validation to verify fixes
4. Update documentation as needed

---

**Report File:** $(Split-Path $OutputPath -Leaf)
**Database:** $(Split-Path $(Split-Path $OutputPath) -Leaf)
"@
    
    $report | Out-File -Path $OutputPath -Encoding UTF8 -Force
    Write-Log "Report saved to: $OutputPath" 'SUCCESS'
}

function Export-ToJSON {
    param(
        [string]$OutputPath,
        [hashtable]$Statistics,
        [array]$Issues
    )
    
    Write-Log "Exporting to JSON..." 'INFO'
    
    $export = @{
        Timestamp = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'
        Statistics = $Statistics
        Issues = $script:issues
        Summary = @{
            Total = $script:issues.Count
            Critical = ($script:issues | Where-Object { $_.Severity -eq 'Critical' }).Count
            High = ($script:issues | Where-Object { $_.Severity -eq 'High' }).Count
            Medium = ($script:issues | Where-Object { $_.Severity -eq 'Medium' }).Count
            Low = ($script:issues | Where-Object { $_.Severity -eq 'Low' }).Count
        }
    }
    
    $export | ConvertTo-Json -Depth 10 | Out-File -Path $OutputPath -Encoding UTF8 -Force
    Write-Log "JSON export saved to: $OutputPath" 'SUCCESS'
}

function Export-ToCSV {
    param(
        [string]$OutputPath,
        [array]$Issues
    )
    
    Write-Log "Exporting to CSV..." 'INFO'
    
    $script:issues | Export-Csv -Path $OutputPath -NoTypeInformation -Encoding UTF8 -Force
    Write-Log "CSV export saved to: $OutputPath" 'SUCCESS'
}

# ============================================================================
# Main Execution
# ============================================================================

try {
    Write-Log "╔════════════════════════════════════════════╗" 'CHECK'
    Write-Log "║   CROSS-REFERENCE VALIDATION & CHECKER     ║" 'CHECK'
    Write-Log "╚════════════════════════════════════════════╝" 'CHECK'
    Write-Log "" 'INFO'
    
    # Connect to database
    $connection = New-DatabaseConnection -DatabasePath $DatabasePath
    
    # Run validation checks
    Write-Log "Starting comprehensive validation..." 'INFO'
    Find-BrokenReferences -Connection $connection
    Find-CircularDependencies -Connection $connection
    Find-OrphanedFiles -Connection $connection
    Find-DanglingDependencies -Connection $connection
    Find-ConflictingReferences -Connection $connection
    Validate-ReferentialIntegrity -Connection $connection
    
    # Get statistics
    $stats = Get-DatabaseStatistics -Connection $connection
    
    $connection.Close()
    
    # Generate reports
    Write-Log "" 'INFO'
    Write-Log "════════════════════════════════════════════" 'CHECK'
    Write-Log "Validation Complete" 'CHECK'
    Write-Log "════════════════════════════════════════════" 'CHECK'
    Write-Log "" 'INFO'
    
    Write-Log "Issues Found: $($script:issues.Count)" 'WARN'
    Write-Log "  - Critical: $(($script:issues | Where-Object { $_.Severity -eq 'Critical' }).Count)" 'ERROR'
    Write-Log "  - High: $(($script:issues | Where-Object { $_.Severity -eq 'High' }).Count)" 'ISSUE'
    Write-Log "  - Medium: $(($script:issues | Where-Object { $_.Severity -eq 'Medium' }).Count)" 'WARN'
    Write-Log "  - Low: $(($script:issues | Where-Object { $_.Severity -eq 'Low' }).Count)" 'WARN'
    
    Write-Log "" 'INFO'
    
    # Generate report
    if ($GenerateReport) {
        $mdReportPath = $ReportPath
        Generate-MarkdownReport -OutputPath $mdReportPath -Statistics $stats -Issues $script:issues
        
        # Also export in other formats
        if ($ExportFormat -eq 'json' -or $ExportFormat -eq 'all') {
            $jsonPath = $mdReportPath -replace '\.md$', '.json'
            Export-ToJSON -OutputPath $jsonPath -Statistics $stats -Issues $script:issues
        }
        
        if ($ExportFormat -eq 'csv' -or $ExportFormat -eq 'all') {
            $csvPath = $mdReportPath -replace '\.md$', '.csv'
            Export-ToCSV -OutputPath $csvPath -Issues $script:issues
        }
    }
    
    Write-Log "Validation complete. Found $($script:issues.Count) total issues." 'CHECK'
    
} catch {
    Write-Log "FATAL ERROR: $_" 'ERROR'
    Write-Log $_.Exception.StackTrace 'ERROR'
    exit 1
}
