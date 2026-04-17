<#
.SYNOPSIS
    Pattern Extraction Engine for HELIOS Platform
    
.DESCRIPTION
    Extracts patterns from all component interactions across the platform.
    Identifies recurring patterns, sequences, and relationships.
    
.NOTES
    Version: 1.0
    Requires: Learning Database Connection
#>

param(
    [string]$DataSource = "C:\HELIOS\analytics\learning-database\component-interactions.db",
    [int]$BatchSize = 1000,
    [int]$PatternMinFrequency = 5
)

# Import required modules
$ErrorActionPreference = "Stop"

class PatternExtractor {
    [string]$DatabasePath
    [int]$BatchSize
    [int]$MinFrequency
    [hashtable]$ExtractedPatterns
    [array]$ComponentInteractions
    
    PatternExtractor([string]$dbPath, [int]$batchSize, [int]$minFreq) {
        $this.DatabasePath = $dbPath
        $this.BatchSize = $batchSize
        $this.MinFrequency = $minFreq
        $this.ExtractedPatterns = @{}
        $this.ComponentInteractions = @()
    }
    
    [void] LoadInteractions() {
        Write-Host "Loading component interactions from database..." -ForegroundColor Cyan
        
        # Simulate loading from SQLite database
        $interactions = @(
            @{
                id = 1
                source = "web-interface"
                target = "api-gateway"
                frequency = 234
                latency = 45
                timestamp = (Get-Date).AddMinutes(-5)
            },
            @{
                id = 2
                source = "api-gateway"
                target = "auth-service"
                frequency = 189
                latency = 32
                timestamp = (Get-Date).AddMinutes(-4)
            },
            @{
                id = 3
                source = "auth-service"
                target = "cache-layer"
                frequency = 456
                latency = 8
                timestamp = (Get-Date).AddMinutes(-3)
            },
            @{
                id = 4
                source = "api-gateway"
                target = "data-processor"
                frequency = 123
                latency = 78
                timestamp = (Get-Date).AddMinutes(-2)
            },
            @{
                id = 5
                source = "data-processor"
                target = "database"
                frequency = 98
                latency = 156
                timestamp = (Get-Date).AddMinutes(-1)
            }
        )
        
        $this.ComponentInteractions = $interactions
        Write-Host "✓ Loaded $($this.ComponentInteractions.Count) interactions" -ForegroundColor Green
    }
    
    [void] ExtractSequencePatterns() {
        Write-Host "Extracting sequence patterns..." -ForegroundColor Cyan
        
        $sequences = @{}
        
        foreach ($interaction in $this.ComponentInteractions) {
            $sequence = "$($interaction.source) -> $($interaction.target)"
            
            if ($sequences.ContainsKey($sequence)) {
                $sequences[$sequence] += $interaction.frequency
            } else {
                $sequences[$sequence] = $interaction.frequency
            }
        }
        
        # Filter by minimum frequency
        $frequentSequences = $sequences | Where-Object { $_.Value -ge $this.MinFrequency }
        
        $this.ExtractedPatterns["sequences"] = $frequentSequences
        Write-Host "✓ Found $($frequentSequences.Count) sequence patterns" -ForegroundColor Green
    }
    
    [void] ExtractConcurrencyPatterns() {
        Write-Host "Extracting concurrency patterns..." -ForegroundColor Cyan
        
        $concurrencyMap = @{}
        
        # Group by timestamp proximity (within 1 second)
        $timeGroups = $this.ComponentInteractions | Group-Object {
            [Math]::Floor(($_.timestamp.Ticks) / 10000000)
        }
        
        foreach ($group in $timeGroups) {
            if ($group.Count -gt 1) {
                $pattern = ($group.Group | ForEach-Object { "$($_.source):$($_.target)" }) -join ", "
                
                if ($concurrencyMap.ContainsKey($pattern)) {
                    $concurrencyMap[$pattern] += $group.Count
                } else {
                    $concurrencyMap[$pattern] = $group.Count
                }
            }
        }
        
        $this.ExtractedPatterns["concurrency"] = $concurrencyMap
        Write-Host "✓ Found $($concurrencyMap.Count) concurrency patterns" -ForegroundColor Green
    }
    
    [void] ExtractPerformancePatterns() {
        Write-Host "Extracting performance patterns..." -ForegroundColor Cyan
        
        $perfPatterns = @{}
        
        # Group interactions by latency ranges
        $latencyRanges = @{
            "Fast (0-50ms)" = $this.ComponentInteractions | Where-Object { $_.latency -le 50 }
            "Medium (50-200ms)" = $this.ComponentInteractions | Where-Object { $_.latency -gt 50 -and $_.latency -le 200 }
            "Slow (200ms+)" = $this.ComponentInteractions | Where-Object { $_.latency -gt 200 }
        }
        
        foreach ($range in $latencyRanges.GetEnumerator()) {
            if ($range.Value.Count -gt 0) {
                $perfPatterns[$range.Key] = @{
                    count = $range.Value.Count
                    avgLatency = ($range.Value | Measure-Object -Property latency -Average).Average
                    components = ($range.Value | Select-Object -ExpandProperty source -Unique)
                }
            }
        }
        
        $this.ExtractedPatterns["performance"] = $perfPatterns
        Write-Host "✓ Found performance patterns across latency ranges" -ForegroundColor Green
    }
    
    [void] ExtractDependencyPatterns() {
        Write-Host "Extracting dependency patterns..." -ForegroundColor Cyan
        
        $dependencies = @{}
        
        foreach ($interaction in $this.ComponentInteractions) {
            if (-not $dependencies.ContainsKey($interaction.source)) {
                $dependencies[$interaction.source] = @()
            }
            
            if ($interaction.target -notin $dependencies[$interaction.source]) {
                $dependencies[$interaction.source] += $interaction.target
            }
        }
        
        $this.ExtractedPatterns["dependencies"] = $dependencies
        Write-Host "✓ Mapped $($dependencies.Count) dependency chains" -ForegroundColor Green
    }
    
    [hashtable] GetExtractedPatterns() {
        return $this.ExtractedPatterns
    }
    
    [void] PrintPatternSummary() {
        Write-Host "`n=== EXTRACTED PATTERNS SUMMARY ===" -ForegroundColor Yellow
        
        foreach ($patternType in $this.ExtractedPatterns.Keys) {
            Write-Host "`n$patternType Patterns:" -ForegroundColor Cyan
            
            $patterns = $this.ExtractedPatterns[$patternType]
            
            if ($patterns -is [hashtable]) {
                $patterns.GetEnumerator() | ForEach-Object {
                    if ($_.Value -is [int]) {
                        Write-Host "  • $($_.Key): $($_.Value)" -ForegroundColor White
                    } else {
                        Write-Host "  • $($_.Key): $(($_.Value | ConvertTo-Json -Compress))" -ForegroundColor White
                    }
                }
            }
        }
    }
}

# Main execution
Write-Host "HELIOS Platform - Pattern Extraction Engine" -ForegroundColor Yellow
Write-Host "============================================" -ForegroundColor Yellow

$extractor = [PatternExtractor]::new($DataSource, $BatchSize, $PatternMinFrequency)

$extractor.LoadInteractions()
$extractor.ExtractSequencePatterns()
$extractor.ExtractConcurrencyPatterns()
$extractor.ExtractPerformancePatterns()
$extractor.ExtractDependencyPatterns()

$extractor.PrintPatternSummary()

# Return results for piping
$extractor.GetExtractedPatterns() | ConvertTo-Json | Write-Output
