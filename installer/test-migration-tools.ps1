# Comprehensive Test Suite for HELIOS Migration Tools

. "$PSScriptRoot\migration-orchestrator.ps1"

# ============================================================================
# TEST CONFIGURATION
# ============================================================================

$script:testResults = @()
$script:testsPassed = 0
$script:testsFailed = 0

function Test-MigrationTool {
    param(
        [string]$Name,
        [scriptblock]$TestBlock
    )

    $testResult = @{
        Name = $Name
        Passed = $false
        Error = $null
        Duration = 0
    }

    $startTime = Get-Date

    try {
        & $TestBlock
        $testResult.Passed = $true
        Write-Host "✓ PASS: $Name" -ForegroundColor Green
        $global:testsPassed++
    }
    catch {
        $testResult.Passed = $false
        $testResult.Error = $_.Exception.Message
        Write-Host "✗ FAIL: $Name" -ForegroundColor Red
        Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Yellow
        $global:testsFailed++
    }
    finally {
        $testResult.Duration = ((Get-Date) - $startTime).TotalMilliseconds
        $script:testResults += $testResult
    }
}

# ============================================================================
# CORE TESTS
# ============================================================================

Write-Host "`n====== HELIOS Migration Tools Test Suite ======`n" -ForegroundColor Blue

# Test 1: MigrationContext Creation
Test-MigrationTool "MigrationContext Creation" {
    $context = [MigrationContext]::new("TestMigration", "v1", "v2")
    
    if ($null -eq $context.Id) { throw "ID not generated" }
    if ($context.Name -ne "TestMigration") { throw "Name not set" }
    if ($context.SourceVersion -ne "v1") { throw "Source version not set" }
    if ($context.TargetVersion -ne "v2") { throw "Target version not set" }
    if ($context.State -ne [MigrationState]::NotStarted) { throw "State not initialized" }
}

# Test 2: Backup Manager
Test-MigrationTool "BackupManager Creation" {
    $backupManager = [BackupManager]::new("C:\HELIOS\test_backups", 30)
    
    if ($null -eq $backupManager) { throw "BackupManager not created" }
    if ($backupManager.BackupBasePath -ne "C:\HELIOS\test_backups") { throw "Path not set" }
    if ($backupManager.RetentionDays -ne 30) { throw "Retention not set" }
}

# Test 3: Data Transformer
Test-MigrationTool "DataTransformer Field Mapping" {
    $transformer = New-DataTransformer -BatchSize 50
    
    $transformer.AddFieldMapping("old_field", "new_field", "string")
    $transformer.AddFieldMapping("count", "total", "int")
    
    $record = @{ old_field = "test"; count = 42 }
    $transformed = $transformer.TransformRecord($record)
    
    if ($transformed -eq $null) { throw "Record not transformed" }
    if ($transformed.new_field -ne "test") { throw "String mapping failed" }
    if ($transformed.total -ne 42) { throw "Int conversion failed" }
}

# Test 4: Conflict Resolver
Test-MigrationTool "ConflictResolver Logging" {
    $resolver = [ConflictResolver]::new("merge")
    
    $resolver.LogConflict("item1", @{value = "old"}, @{value = "new"}, "Test conflict")
    
    $summary = $resolver.GetConflictSummary()
    if ($summary.TotalConflicts -ne 1) { throw "Conflict not logged" }
}

# Test 5: Validation Engine
Test-MigrationTool "ValidationEngine Rules" {
    $validator = [ValidationEngine]::new([ValidationLevel]::Standard)
    
    $validator.AddRule("TestRule", {
        param($data)
        return @{
            Passed = $data.id -gt 0
            Message = "ID must be positive"
        }
    })
    
    $result = $validator.Validate(@{ id = 1 })
    if (-not $result.Valid) { throw "Validation failed for valid data" }
    
    $result2 = $validator.Validate(@{ id = -1 })
    if ($result2.Valid) { throw "Validation passed for invalid data" }
}

# Test 6: Progress Tracker
Test-MigrationTool "MigrationProgressTracker" {
    $logPath = "C:\HELIOS\logs\test_progress.log"
    $tracker = [MigrationProgressTracker]::new($logPath)
    
    $tracker.Log("test-id", "Test log message")
    $tracker.UpdateProgress("test-id", 50, 100)
    
    $info = $tracker.GetTrackerInfo("test-id")
    if ($null -eq $info) { throw "Progress not tracked" }
    
    $tracker.Dispose()
}

# Test 7: Version Upgrade Manager
Test-MigrationTool "VersionUpgradeManager Path Finding" {
    $upgradeManager = New-VersionUpgradeManager -LogPath "C:\HELIOS\logs\test_upgrade.log"
    
    $script1 = { param($data) $data['v'] = 'v2'; return $data }
    $script2 = { param($data) $data['v'] = 'v3'; return $data }
    
    $upgradeManager.RegisterUpgrade("v1", "v2", $script1)
    $upgradeManager.RegisterUpgrade("v2", "v3", $script2)
    
    $path = $upgradeManager.GetUpgradePath("v1", "v3", $false)
    if ($null -eq $path -or $path.Count -ne 3) { throw "Path not found correctly" }
    
    $upgradeManager.Dispose()
}

# Test 8: Schema Transformer
Test-MigrationTool "SchemaTransformer Schema Definition" {
    $schemaTransformer = New-SchemaTransformer
    
    $schema = @{
        Fields = @(
            @{ Name = "id"; Type = "int"; Required = $true }
            @{ Name = "name"; Type = "string"; Required = $true; Default = "Unknown" }
        )
    }
    
    $schemaTransformer.DefineSchema("UserSchema", $schema)
    
    if (-not $schemaTransformer.SchemaDefinitions.ContainsKey("UserSchema")) {
        throw "Schema not defined"
    }
}

# Test 9: Parallel Coordinator
Test-MigrationTool "ParallelMigrationCoordinator Initialization" {
    $coordinator = New-ParallelMigrationCoordinator -MaxWorkers 4 -LogPath "C:\HELIOS\logs\test_parallel.log"
    
    if ($coordinator.Workers.Count -ne 4) { throw "Workers not initialized" }
    if ($null -eq $coordinator.GlobalResults) { throw "Results not initialized" }
    
    $coordinator.Dispose()
}

# Test 10: Migration Orchestrator
Test-MigrationTool "MigrationOrchestrator Creation" {
    $orchestrator = New-MigrationOrchestrator -Name "Test" -FromVersion "v1" -ToVersion "v2"
    
    if ($null -eq $orchestrator.Context) { throw "Context not created" }
    if ($orchestrator.Context.Name -ne "Test") { throw "Name not set" }
    
    $orchestrator.Dispose()
}

# ============================================================================
# FUNCTIONAL TESTS
# ============================================================================

# Test 11: Simple Data Transformation
Test-MigrationTool "End-to-End Simple Transformation" {
    $orchestrator = New-MigrationOrchestrator -Name "E2E_Simple" -FromVersion "v1" -ToVersion "v2"
    
    $orchestrator.AddFieldMapping("id", "Id", "int")
    $orchestrator.AddFieldMapping("name", "Name", "string")
    
    $orchestrator.RegisterUpgrade("v1", "v2", {
        param($data)
        $data['Version'] = 'v2'
        return $data
    })
    
    $testData = @{ id = 1; name = "Test" }
    $result = $orchestrator.ExecuteMigration($testData)
    
    if (-not $result.Success) { throw $result.Error }
    if ($result.SourceVersion -ne "v1") { throw "Source version not recorded" }
    
    $orchestrator.Dispose()
}

# Test 12: Dry-Run Mode
Test-MigrationTool "Dry-Run Mode" {
    $orchestrator = New-MigrationOrchestrator -Name "DryRun_Test" -FromVersion "v1" -ToVersion "v2"
    $orchestrator.EnableDryRun($true)
    
    if (-not $orchestrator.DryRun) { throw "Dry-run not enabled" }
    if (-not $orchestrator.Context.DryRun) { throw "Context dry-run not set" }
    
    $result = $orchestrator.ExecuteMigration(@{ test = "data" })
    
    if ($result.DryRun -ne $true) { throw "Dry-run not recorded in result" }
    
    $orchestrator.Dispose()
}

# Test 13: Multi-Version Upgrade Path
Test-MigrationTool "Multi-Step Version Upgrade" {
    $orchestrator = New-MigrationOrchestrator -Name "MultiVersion" -FromVersion "v1" -ToVersion "v3"
    
    $orchestrator.RegisterUpgrade("v1", "v2", {
        param($data)
        $data['Step'] = 'Step1'
        return $data
    })
    
    $orchestrator.RegisterUpgrade("v2", "v3", {
        param($data)
        $data['Step'] = 'Step2'
        return $data
    })
    
    $result = $orchestrator.ExecuteMigration(@{ initial = "data" })
    
    if (-not $result.Success) { throw "Multi-version upgrade failed" }
    if ($result.ExecutedUpgrades.Count -lt 2) { throw "Not all upgrades executed" }
    
    $orchestrator.Dispose()
}

# Test 14: Bulk Migration
Test-MigrationTool "Bulk Migration Sequential" {
    $orchestrator = New-MigrationOrchestrator -Name "Bulk_Test" -FromVersion "v1" -ToVersion "v2"
    
    $orchestrator.AddFieldMapping("id", "Id", "int")
    $orchestrator.AddFieldMapping("data", "Data", "string")
    
    $bulk = New-BulkMigrationExecutor -Orchestrator $orchestrator
    
    $records = [System.Collections.Generic.List[hashtable]]::new()
    for ($i = 0; $i -lt 10; $i++) {
        $records.Add(@{ id = $i; data = "Item_$i" })
    }
    
    $result = $bulk.ExecuteBulkMigration($records)
    
    if ($result.TotalRecords -ne 10) { throw "Record count mismatch" }
    if ($result.ExecutionMode -ne "Sequential") { throw "Wrong execution mode" }
    
    $bulk.Dispose()
    $orchestrator.Dispose()
}

# Test 15: Parallel Bulk Migration
Test-MigrationTool "Bulk Migration Parallel" {
    $orchestrator = New-MigrationOrchestrator -Name "Parallel_Bulk" -FromVersion "v1" -ToVersion "v2"
    
    $orchestrator.AddFieldMapping("id", "Id", "int")
    $orchestrator.AddFieldMapping("value", "Value", "string")
    
    $bulk = New-BulkMigrationExecutor -Orchestrator $orchestrator
    $bulk.EnableParallelExecution(2)
    
    $records = [System.Collections.Generic.List[hashtable]]::new()
    for ($i = 0; $i -lt 20; $i++) {
        $records.Add(@{ id = $i; value = "Val_$i" })
    }
    
    $result = $bulk.ExecuteBulkMigration($records)
    
    if ($result.ExecutionMode -ne "Parallel") { throw "Wrong execution mode" }
    if ($result.ProcessedRecords -eq 0) { throw "No records processed" }
    
    $bulk.Dispose()
    $orchestrator.Dispose()
}

# Test 16: Type Conversion
Test-MigrationTool "Type Conversions" {
    $transformer = New-DataTransformer
    
    $transformer.AddFieldMapping("str_field", "string_val", "string")
    $transformer.AddFieldMapping("int_field", "int_val", "int")
    $transformer.AddFieldMapping("bool_field", "bool_val", "bool")
    
    $record = @{
        str_field = 123
        int_field = "456"
        bool_field = 1
    }
    
    $result = $transformer.TransformRecord($record)
    
    if ($result.string_val -ne "123") { throw "String conversion failed" }
    if ($result.int_val -ne 456) { throw "Int conversion failed" }
    if ($result.bool_val -ne $true) { throw "Bool conversion failed" }
}

# Test 17: Validation Rule Chain
Test-MigrationTool "Multiple Validation Rules" {
    $validator = [ValidationEngine]::new([ValidationLevel]::Strict)
    
    $validator.AddRule("Rule1", {
        param($data)
        return @{ Passed = $data.id -gt 0; Message = "ID required" }
    })
    
    $validator.AddRule("Rule2", {
        param($data)
        return @{ Passed = -not [string]::IsNullOrEmpty($data.name); Message = "Name required" }
    })
    
    $validData = @{ id = 1; name = "Test" }
    $result1 = $validator.Validate($validData)
    if (-not $result1.Valid) { throw "Valid data failed validation" }
    
    $invalidData = @{ id = 0; name = "Test" }
    $result2 = $validator.Validate($invalidData)
    if ($result2.Valid) { throw "Invalid data passed validation" }
}

# Test 18: Transformation Rules
Test-MigrationTool "Custom Transformation Rules" {
    $transformer = New-DataTransformer
    
    $transformer.AddTransformationRule("Uppercase", {
        param($source, $target)
        foreach ($key in $target.Keys) {
            if ($target[$key] -is [string]) {
                $target[$key] = $target[$key].ToUpper()
            }
        }
        return $target
    })
    
    # This would require the source to have fields for mapping
    $record = @{ field = "lowercase" }
    $result = $transformer.TransformRecord($record)
    
    if ($null -eq $result) { throw "Transformation rule not applied" }
}

# Test 19: Report Generation
Test-MigrationTool "Report Generation" {
    $reportGen = New-MigrationReportGenerator
    
    $orch = New-MigrationOrchestrator -Name "Report_Test" -FromVersion "v1" -ToVersion "v2"
    $result = $orch.ExecuteMigration(@{ test = "data" })
    $reportGen.AddMigrationResult("test1", $result)
    
    $json = $reportGen.GenerateJSONReport()
    if ([string]::IsNullOrEmpty($json)) { throw "JSON report empty" }
    
    $html = $reportGen.GenerateHTMLReport()
    if ([string]::IsNullOrEmpty($html)) { throw "HTML report empty" }
    if (-not $html.Contains("<html>")) { throw "HTML report not valid" }
    
    $orch.Dispose()
}

# Test 20: Work Distribution Engine
Test-MigrationTool "WorkDistributionEngine" {
    $engine = New-WorkDistributionEngine -Workers 3 -MaxQueueSize 100
    
    $worker = $engine.DistributeWork(@{ id = 1 }, "leastLoaded")
    if ($worker -lt 0) { throw "Work not distributed" }
    
    $stats = $engine.GetDistributionStats()
    if ($stats.TotalWorkers -ne 3) { throw "Worker count mismatch" }
}

# ============================================================================
# RESULTS
# ============================================================================

Write-Host "`n====== Test Results ======`n" -ForegroundColor Blue

Write-Host "Passed: $testsPassed" -ForegroundColor Green
Write-Host "Failed: $testsFailed" -ForegroundColor $(if ($testsFailed -eq 0) { "Green" } else { "Red" })
Write-Host "Total:  $($testsPassed + $testsFailed)" -ForegroundColor Cyan

if ($testsFailed -eq 0) {
    Write-Host "`n✓ ALL TESTS PASSED!" -ForegroundColor Green -BackgroundColor White
}
else {
    Write-Host "`n✗ SOME TESTS FAILED" -ForegroundColor Red -BackgroundColor White
    Write-Host "`nFailed Tests:" -ForegroundColor Red
    $script:testResults | Where-Object { -not $_.Passed } | ForEach-Object {
        Write-Host "  - $($_.Name): $($_.Error)" -ForegroundColor Yellow
    }
}

# Summary report
Write-Host "`n====== Test Execution Summary ======`n" -ForegroundColor Blue
$script:testResults | Format-Table -Property Name, Passed, Duration -AutoSize

Write-Host "`nTest suite completed at $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Cyan
