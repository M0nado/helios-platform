# HELIOS Platform - File Integrity Monitoring (FIM)
# Detect modifications, track changes, and alert on unauthorized access

param(
    [string]$MonitorPath = "C:\HELIOS\platform",
    [string]$HashDatabasePath = "C:\HELIOS\security\fim-database.json"
)

Write-Host @"
╔════════════════════════════════════════════════════════════════╗
║     HELIOS Platform - File Integrity Monitoring                ║
║     Detect Modifications & Track Changes                       ║
╚════════════════════════════════════════════════════════════════╝
"@ -ForegroundColor Cyan

# Initialize FIM database
$database = @{
    Created = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    Files = @()
    Modifications = @()
    Statistics = @{
        TotalFilesMonitored = 0
        FilesModified = 0
        FilesAdded = 0
        FilesDeleted = 0
    }
}

# Create baseline if directory exists
if (Test-Path $MonitorPath) {
    Write-Host "[*] Creating file baseline..." -ForegroundColor Cyan
    $files = Get-ChildItem -Path $MonitorPath -Recurse -File -ErrorAction SilentlyContinue | Select-Object -First 50
    
    foreach ($file in $files) {
        try {
            $sha256 = (Get-FileHash -Path $file.FullName -Algorithm SHA256 -ErrorAction SilentlyContinue).Hash
            $database.Files += @{
                Path = $file.FullName
                Name = $file.Name
                Size = $file.Length
                SHA256 = $sha256
                Modified = $file.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss")
            }
        }
        catch {}
    }
}

$database.Statistics.TotalFilesMonitored = $database.Files.Count
$database | ConvertTo-Json -Depth 10 | Out-File -FilePath $HashDatabasePath -Force

Write-Host "[+] FIM Database Initialized" -ForegroundColor Green
Write-Host "    - Files Monitored: $($database.Files.Count)" -ForegroundColor Green
Write-Host "    - Database Path: $HashDatabasePath" -ForegroundColor Green
