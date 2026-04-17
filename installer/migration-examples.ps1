# Complete Migration Example and Demonstration Script
# Shows how to use all migration tools together

. "$PSScriptRoot\migration-orchestrator.ps1"

# ============================================================================
# EXAMPLE 1: Simple Data Migration with Validation
# ============================================================================

function Invoke-SimpleMigration {
    Write-Host "`n=== Example 1: Simple Data Migration ===" -ForegroundColor Magenta

    # Create orchestrator
    $orchestrator = New-MigrationOrchestrator -Name "SimpleDataMigration" -FromVersion "v1" -ToVersion "v2"

    # Configure field mappings
    $orchestrator.AddFieldMapping("OldName", "Name", "string")
    $orchestrator.AddFieldMapping("OldStatus", "Status", "string")
    $orchestrator.AddFieldMapping("OldCount", "Count", "int")

    # Add validation rule
    $orchestrator.AddValidationRule("NameNotEmpty", {
        param($data)
        return @{
            Passed = -not [string]::IsNullOrEmpty($data.Name)
            Message = "Name field must not be empty"
        }
    })

    # Add transformation rule
    $orchestrator.AddTransformationRule("StatusNormalization", {
        param($source, $target)
        if ($target.Status) {
            $target.Status = $target.Status.ToUpper()
        }
        return $target
    })

    # Register upgrade script
    $upgradeScript = {
        param($data)
        # Add schema version marker
        $data['SchemaVersion'] = 'v2'
        $data['MigratedAt'] = (Get-Date).ToString()
        return $data
    }
    $orchestrator.RegisterUpgrade("v1", "v2", $upgradeScript)

    # Sample data
    $sampleData = @{
        OldName = "TestItem"
        OldStatus = "pending"
        OldCount = 42
    }

    # Execute migration
    $result = $orchestrator.ExecuteMigration($sampleData)
    
    Write-Host "Migration Result:" -ForegroundColor Green
    $result | ConvertTo-Json | Write-Host

    $orchestrator.Dispose()
}

# ============================================================================
# EXAMPLE 2: Dry-Run Mode Testing
# ============================================================================

function Invoke-DryRunMigration {
    Write-Host "`n=== Example 2: Dry-Run Mode Migration ===" -ForegroundColor Magenta

    $orchestrator = New-MigrationOrchestrator -Name "DryRunMigration" -FromVersion "v1.0" -ToVersion "v2.0"
    
    # Enable dry-run mode
    $orchestrator.EnableDryRun($true)

    Write-Host "Running migration in DRY-RUN mode (no changes will be persisted)" -ForegroundColor Yellow

    $testData = @{
        UserId = 123
        UserName = "john_doe"
        CreatedDate = (Get-Date).ToString()
    }

    $result = $orchestrator.ExecuteMigration($testData)

    Write-Host "Dry-run completed. No actual changes were made." -ForegroundColor Green
    Write-Host "Backup created: $($result.BackupPath)" -ForegroundColor Green

    $orchestrator.Dispose()
}

# ============================================================================
# EXAMPLE 3: Parallel Bulk Migration
# ============================================================================

function Invoke-ParallelBulkMigration {
    Write-Host "`n=== Example 3: Parallel Bulk Migration ===" -ForegroundColor Magenta

    $orchestrator = New-MigrationOrchestrator -Name "BulkDataMigration" -FromVersion "v1" -ToVersion "v2"
    
    # Configure transformer
    $orchestrator.AddFieldMapping("id", "Id", "int")
    $orchestrator.AddFieldMapping("name", "Name", "string")
    $orchestrator.AddFieldMapping("active", "IsActive", "bool")

    # Create bulk executor with parallel support
    $bulkExecutor = New-BulkMigrationExecutor -Orchestrator $orchestrator
    $bulkExecutor.EnableParallelExecution(4)

    # Generate sample records
    $records = [System.Collections.Generic.List[hashtable]]::new()
    for ($i = 0; $i -lt 100; $i++) {
        $records.Add(@{
            id = $i
            name = "Item_$i"
            active = ($i % 2) -eq 0
        })
    }

    Write-Host "Migrating $($records.Count) records in parallel..." -ForegroundColor Cyan

    $bulkResult = $bulkExecutor.ExecuteBulkMigration($records)

    Write-Host "Bulk Migration Result:" -ForegroundColor Green
    Write-Host "  Total Records: $($bulkResult.TotalRecords)"
    Write-Host "  Successful: $($bulkResult.SuccessfulRecords)"
    Write-Host "  Failed: $($bulkResult.FailedRecords)"
    Write-Host "  Duration: $($bulkResult.Duration) seconds"
    Write-Host "  Mode: $($bulkResult.ExecutionMode)"

    $bulkExecutor.Dispose()
    $orchestrator.Dispose()
}

# ============================================================================
# EXAMPLE 4: Multi-Step Version Upgrade
# ============================================================================

function Invoke-MultiStepUpgrade {
    Write-Host "`n=== Example 4: Multi-Step Version Upgrade ===" -ForegroundColor Magenta

    $orchestrator = New-MigrationOrchestrator -Name "MultiStepUpgrade" -FromVersion "v1" -ToVersion "v3"

    # Register v1 -> v2
    $upgrade1to2 = {
        param($data)
        Write-Host "Upgrading v1 → v2" -ForegroundColor Yellow
        $data['MigrationVersion'] = 'v2'
        $data['FieldA'] = $data['field_a']
        Remove-Item $data -Name 'field_a'
        return $data
    }

    # Register v2 -> v3
    $upgrade2to3 = {
        param($data)
        Write-Host "Upgrading v2 → v3" -ForegroundColor Yellow
        $data['MigrationVersion'] = 'v3'
        $data['FieldB'] = $data['field_b']
        $data['MigratedAt'] = Get-Date
        Remove-Item $data -Name 'field_b'
        return $data
    }

    $orchestrator.RegisterUpgrade("v1", "v2", $upgrade1to2)
    $orchestrator.RegisterUpgrade("v2", "v3", $upgrade2to3)

    $testData = @{
        field_a = "ValueA"
        field_b = "ValueB"
        timestamp = Get-Date
    }

    $result = $orchestrator.ExecuteMigration($testData)

    Write-Host "Multi-step upgrade result: $($result.Success)" -ForegroundColor Green

    $orchestrator.Dispose()
}

# ============================================================================
# EXAMPLE 5: Incremental Phased Migration
# ============================================================================

function Invoke-IncrementalPhasedMigration {
    Write-Host "`n=== Example 5: Incremental Phased Migration ===" -ForegroundColor Magenta

    $orchestrator = New-MigrationOrchestrator -Name "PhasedMigration" -FromVersion "v1" -ToVersion "v2"
    $orchestrator.SetParallelWorkers(4)

    # Create incremental upgrade orchestrator
    $upgradeManager = New-VersionUpgradeManager -LogPath "C:\HELIOS\logs\incremental.log"

    $upgrade = {
        param($data)
        $data['MigrationPhase'] = 'incremental'
        return $data
    }

    $upgradeManager.RegisterUpgrade("v1", "v2", $upgrade)
    
    $incrementalOrchestrator = New-IncrementalUpgradeOrchestrator -UpgradeManager $upgradeManager

    # Define phases
    $incrementalOrchestrator.DefineMigrationPhase(1, "UserData", @("users"), {
        param($result)
        return @{ Valid = $result.Success }
    })

    $incrementalOrchestrator.DefineMigrationPhase(2, "TransactionData", @("transactions"), {
        param($result)
        return @{ Valid = $result.Success }
    })

    $incrementalOrchestrator.DefineMigrationPhase(3, "AnalyticsData", @("analytics"), {
        param($result)
        return @{ Valid = $result.Success }
    })

    $testData = @{
        users = @(@{ id = 1; name = "User1" })
        transactions = @(@{ id = 1; amount = 100 })
        analytics = @(@{ metric = "views"; count = 1000 })
    }

    Write-Host "Executing incremental 3-phase migration..." -ForegroundColor Cyan

    $phasedResult = $incrementalOrchestrator.ExecutePhased("v1", "v2", $testData, $false)

    Write-Host "Phases Completed: $($phasedResult.PhasesCompleted) / $($phasedResult.PhasesTotal)" -ForegroundColor Green
    Write-Host "Overall Success: $($phasedResult.Success)" -ForegroundColor Green
    Write-Host "Total Duration: $($phasedResult.Duration) seconds" -ForegroundColor Green

    $orchestrator.Dispose()
}

# ============================================================================
# EXAMPLE 6: Rollback on Failure
# ============================================================================

function Invoke-RollbackScenario {
    Write-Host "`n=== Example 6: Rollback on Failure ===" -ForegroundColor Magenta

    $orchestrator = New-MigrationOrchestrator -Name "RollbackTest" -FromVersion "v1" -ToVersion "v2"

    # Add failing validation rule
    $orchestrator.AddValidationRule("AlwaysFail", {
        param($data)
        return @{
            Passed = $false
            Message = "Intentional failure to trigger rollback"
        }
    })

    $testData = @{ id = 1; name = "Test" }

    Write-Host "Attempting migration with failing validation (should trigger rollback)..." -ForegroundColor Yellow

    $result = $orchestrator.ExecuteMigration($testData)

    if (-not $result.Success) {
        Write-Host "Migration failed as expected: $($result.Error)" -ForegroundColor Yellow
        Write-Host "Rollback was triggered automatically." -ForegroundColor Green
    }

    $orchestrator.Dispose()
}

# ============================================================================
# EXAMPLE 7: Report Generation
# ============================================================================

function Invoke-GenerateReport {
    Write-Host "`n=== Example 7: Migration Report Generation ===" -ForegroundColor Magenta

    $reportGen = New-MigrationReportGenerator

    # Run multiple migrations and collect results
    for ($i = 1; $i -le 3; $i++) {
        $orch = New-MigrationOrchestrator -Name "Migration_$i" -FromVersion "v$i" -ToVersion "v$($i+1)"
        $result = $orch.ExecuteMigration(@{ id = $i; data = "test_$i" })
        $reportGen.AddMigrationResult($i, $result)
        $orch.Dispose()
    }

    # Generate reports
    $jsonReport = $reportGen.GenerateJSONReport()
    $htmlReport = $reportGen.GenerateHTMLReport()

    Write-Host "JSON Report (first 500 chars):" -ForegroundColor Green
    Write-Host $jsonReport.Substring(0, [Math]::Min(500, $jsonReport.Length))

    Write-Host "`nHTML Report generated successfully" -ForegroundColor Green
}

# ============================================================================
# MAIN EXECUTION
# ============================================================================

Write-Host "HELIOS Platform - Migration Tools Demonstration" -ForegroundColor Blue -BackgroundColor White
Write-Host "================================================" -ForegroundColor Blue

# Ensure log directory exists
$logDir = "C:\HELIOS\logs"
if (-not (Test-Path $logDir)) {
    New-Item -ItemType Directory -Path $logDir -Force | Out-Null
}

# Run all examples
try {
    Invoke-SimpleMigration
    Invoke-DryRunMigration
    Invoke-ParallelBulkMigration
    Invoke-MultiStepUpgrade
    Invoke-IncrementalPhasedMigration
    Invoke-RollbackScenario
    Invoke-GenerateReport

    Write-Host "`n================================================" -ForegroundColor Blue
    Write-Host "All migration examples completed successfully!" -ForegroundColor Green
    Write-Host "Check logs at: $logDir" -ForegroundColor Green
}
catch {
    Write-Error "Error during migration examples: $_"
}

# ============================================================================
# HELPER SUMMARY
# ============================================================================

Write-Host "`n=== Migration Tools Available ===" -ForegroundColor Cyan
Write-Host @"
1. MigrationOrchestrator - Main migration controller
2. BackupManager - Automatic backup and restore
3. ConflictResolver - Detect and resolve data conflicts
4. ValidationEngine - Pre/post migration validation
5. DataTransformer - Transform data between formats
6. VersionUpgradeManager - Multi-step version upgrades
7. IncrementalUpgradeOrchestrator - Phased migrations
8. ParallelMigrationCoordinator - Parallel execution
9. DistributedMigrationExecutor - Distributed processing
10. BulkMigrationExecutor - Bulk data migration
11. MigrationReportGenerator - Generate reports

Features:
✓ Backup and rollback
✓ Conflict detection and resolution
✓ Pre/post validation
✓ Dry-run mode
✓ Parallel execution
✓ Incremental phased migration
✓ Progress tracking
✓ Multi-version upgrade paths
✓ Report generation
✓ Error handling and logging
"@
