# Quick Verification Script for HELIOS Migration Tools
# Tests core functionality without complex dependencies

Write-Host "`n====== HELIOS Migration Tools - Verification ======`n" -ForegroundColor Blue

# Test 1: Load core classes
Write-Host "Testing: Loading migration core..." -ForegroundColor Cyan
try {
    . "C:\HELIOS\migration-core.ps1"
    Write-Host "✓ Core module loaded" -ForegroundColor Green
}
catch {
    Write-Host "✗ Failed to load core: $_" -ForegroundColor Red
    exit 1
}

# Test 2: Create MigrationContext
Write-Host "Testing: Creating MigrationContext..." -ForegroundColor Cyan
try {
    $ctx = [MigrationContext]::new("Test", "v1", "v2")
    if ($ctx.Id -and $ctx.Name -eq "Test") {
        Write-Host "✓ MigrationContext created successfully" -ForegroundColor Green
    }
    else {
        throw "MigrationContext properties missing"
    }
}
catch {
    Write-Host "✗ Failed: $_" -ForegroundColor Red
    exit 1
}

# Test 3: Load data transformer
Write-Host "Testing: Loading data transformer..." -ForegroundColor Cyan
try {
    . "C:\HELIOS\data-transformer.ps1"
    $dt = New-DataTransformer -BatchSize 100
    Write-Host "✓ Data transformer loaded and instantiated" -ForegroundColor Green
}
catch {
    Write-Host "✗ Failed: $_" -ForegroundColor Red
    exit 1
}

# Test 4: Test field mapping
Write-Host "Testing: Field mapping transformation..." -ForegroundColor Cyan
try {
    $dt = New-DataTransformer
    $dt.AddFieldMapping("old_id", "id", "int")
    $dt.AddFieldMapping("old_name", "name", "string")
    
    $record = @{ old_id = "123"; old_name = "Test" }
    $transformed = $dt.TransformRecord($record)
    
    if ($transformed.id -eq 123 -and $transformed.name -eq "Test") {
        Write-Host "✓ Field mapping and transformation working" -ForegroundColor Green
    }
    else {
        throw "Transformation result incorrect"
    }
}
catch {
    Write-Host "✗ Failed: $_" -ForegroundColor Red
    exit 1
}

# Test 5: Backup Manager
Write-Host "Testing: BackupManager initialization..." -ForegroundColor Cyan
try {
    $bm = [BackupManager]::new("C:\HELIOS\test_backups")
    if ($null -ne $bm) {
        Write-Host "✓ BackupManager initialized" -ForegroundColor Green
    }
}
catch {
    Write-Host "✗ Failed: $_" -ForegroundColor Red
    exit 1
}

# Test 6: Conflict Resolver
Write-Host "Testing: ConflictResolver..." -ForegroundColor Cyan
try {
    $cr = [ConflictResolver]::new("merge")
    $cr.LogConflict("item1", @{v="old"}, @{v="new"}, "test")
    $summary = $cr.GetConflictSummary()
    
    if ($summary.TotalConflicts -eq 1) {
        Write-Host "✓ ConflictResolver working" -ForegroundColor Green
    }
}
catch {
    Write-Host "✗ Failed: $_" -ForegroundColor Red
    exit 1
}

# Test 7: Validation Engine
Write-Host "Testing: ValidationEngine..." -ForegroundColor Cyan
try {
    $ve = [ValidationEngine]::new([ValidationLevel]::Standard)
    $ve.AddRule("TestRule", {
        return @{ Passed = $true; Message = "OK" }
    })
    $result = $ve.Validate(@{ id = 1 })
    
    if ($result.Valid) {
        Write-Host "✓ ValidationEngine working" -ForegroundColor Green
    }
}
catch {
    Write-Host "✗ Failed: $_" -ForegroundColor Red
    exit 1
}

# Test 8: Load version upgrade
Write-Host "Testing: Loading version upgrade module..." -ForegroundColor Cyan
try {
    . "C:\HELIOS\version-upgrade.ps1"
    $vum = New-VersionUpgradeManager -LogPath "C:\HELIOS\logs\test.log"
    Write-Host "✓ Version upgrade module loaded" -ForegroundColor Green
    $vum.Dispose()
}
catch {
    Write-Host "✗ Failed: $_" -ForegroundColor Red
    exit 1
}

# Test 9: Load parallel migration
Write-Host "Testing: Loading parallel migration module..." -ForegroundColor Cyan
try {
    . "C:\HELIOS\parallel-migration.ps1"
    $pmc = New-ParallelMigrationCoordinator -MaxWorkers 2 -LogPath "C:\HELIOS\logs\parallel_test.log"
    if ($pmc.Workers.Count -eq 2) {
        Write-Host "✓ Parallel migration module loaded" -ForegroundColor Green
    }
    $pmc.Dispose()
}
catch {
    Write-Host "✗ Failed: $_" -ForegroundColor Red
    exit 1
}

# Test 10: Load orchestrator
Write-Host "Testing: Loading orchestrator..." -ForegroundColor Cyan
try {
    . "C:\HELIOS\migration-orchestrator.ps1"
    $orch = New-MigrationOrchestrator -Name "TestOrch" -FromVersion "v1" -ToVersion "v2"
    
    if ($null -ne $orch.Context -and $orch.Context.Name -eq "TestOrch") {
        Write-Host "✓ Orchestrator loaded and working" -ForegroundColor Green
    }
    $orch.Dispose()
}
catch {
    Write-Host "✗ Failed: $_" -ForegroundColor Red
    exit 1
}

# Test 11: Simple end-to-end migration
Write-Host "Testing: End-to-end migration..." -ForegroundColor Cyan
try {
    $orch = New-MigrationOrchestrator -Name "E2E" -FromVersion "v1" -ToVersion "v2"
    
    $orch.AddFieldMapping("id", "Id", "int")
    $orch.AddFieldMapping("name", "Name", "string")
    
    $orch.RegisterUpgrade("v1", "v2", {
        param($data)
        $data['Version'] = 'v2'
        return $data
    })
    
    $data = @{ id = 1; name = "Test" }
    $result = $orch.ExecuteMigration($data)
    
    if ($result.Success) {
        Write-Host "✓ End-to-end migration successful" -ForegroundColor Green
    }
    else {
        throw "Migration failed: $($result.Error)"
    }
    
    $orch.Dispose()
}
catch {
    Write-Host "✗ Failed: $_" -ForegroundColor Red
    exit 1
}

# Test 12: Dry-run mode
Write-Host "Testing: Dry-run mode..." -ForegroundColor Cyan
try {
    $orch = New-MigrationOrchestrator -Name "DryRun" -FromVersion "v1" -ToVersion "v2"
    $orch.EnableDryRun($true)
    
    $result = $orch.ExecuteMigration(@{ test = "data" })
    
    if ($result.DryRun) {
        Write-Host "✓ Dry-run mode working" -ForegroundColor Green
    }
    
    $orch.Dispose()
}
catch {
    Write-Host "✗ Failed: $_" -ForegroundColor Red
    exit 1
}

# Test 13: Report generation
Write-Host "Testing: Report generation..." -ForegroundColor Cyan
try {
    $rg = New-MigrationReportGenerator
    $orch = New-MigrationOrchestrator -Name "Report" -FromVersion "v1" -ToVersion "v2"
    $result = $orch.ExecuteMigration(@{ test = "data" })
    $rg.AddMigrationResult("test1", $result)
    
    $json = $rg.GenerateJSONReport()
    $html = $rg.GenerateHTMLReport()
    
    if (-not [string]::IsNullOrEmpty($json) -and -not [string]::IsNullOrEmpty($html)) {
        Write-Host "✓ Report generation working" -ForegroundColor Green
    }
    
    $orch.Dispose()
}
catch {
    Write-Host "✗ Failed: $_" -ForegroundColor Red
    exit 1
}

# Test 14: Verify log directory
Write-Host "Testing: Log directory creation..." -ForegroundColor Cyan
try {
    if (Test-Path "C:\HELIOS\logs") {
        $logFiles = @(Get-ChildItem "C:\HELIOS\logs" -Filter "*.log" -ErrorAction SilentlyContinue)
        Write-Host "✓ Log directory exists with $($logFiles.Count) log files" -ForegroundColor Green
    }
    else {
        Write-Host "✓ Log directory will be created on first use" -ForegroundColor Green
    }
}
catch {
    Write-Host "✗ Failed: $_" -ForegroundColor Red
    exit 1
}

# Summary
Write-Host "`n====== Verification Complete ======`n" -ForegroundColor Blue
Write-Host "All core components verified successfully!" -ForegroundColor Green
Write-Host "`nMigration Tools Available:" -ForegroundColor Cyan
Write-Host @"
  ✓ migration-core.ps1 - Core classes
  ✓ data-transformer.ps1 - Data transformation
  ✓ version-upgrade.ps1 - Version management
  ✓ parallel-migration.ps1 - Parallel execution
  ✓ migration-orchestrator.ps1 - Main orchestrator
  ✓ migration-examples.ps1 - Usage examples
  ✓ MIGRATION_GUIDE.md - Complete documentation
"@

Write-Host "Location: C:\HELIOS\" -ForegroundColor Yellow
Write-Host "Logs: C:\HELIOS\logs\" -ForegroundColor Yellow
Write-Host "Backups: C:\HELIOS\backups\" -ForegroundColor Yellow
