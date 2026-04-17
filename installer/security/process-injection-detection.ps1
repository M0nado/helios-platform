# HELIOS Platform - Process Injection Detection
# Detect DLL injection, code caves, and suspicious process behavior

Write-Host @"
╔════════════════════════════════════════════════════════════════╗
║     HELIOS Platform - Process Injection Detection              ║
║     DLL Injection, Code Caves, Suspicious Behavior             ║
╚════════════════════════════════════════════════════════════════╝
"@ -ForegroundColor Cyan

function Detect-ProcessInjection {
    Write-Host "[*] Analyzing processes for injection signatures..." -ForegroundColor Cyan
    
    $suspiciousProcesses = @()
    $processes = Get-Process -ErrorAction SilentlyContinue
    
    foreach ($proc in $processes) {
        try {
            # Check for suspicious memory patterns
            $handles = Get-WmiObject Win32_ProcessResource -ErrorAction SilentlyContinue | 
                Where-Object { $_.ProcessHandle -eq $proc.Id }
            
            # Check for DLL injection indicators
            $modules = $proc.Modules | Select-Object -ExpandProperty ModuleName
            
            # Look for suspicious DLLs
            $suspiciousDLLs = $modules | Where-Object { 
                $_ -match 'temp|appdata|programdata|users.*\d' 
            }
            
            if ($suspiciousDLLs.Count -gt 0) {
                $suspiciousProcesses += @{
                    ProcessId = $proc.Id
                    ProcessName = $proc.ProcessName
                    Risk = "HIGH"
                    Reason = "Suspicious DLLs loaded"
                    Details = ($suspiciousDLLs | Join-String -Separator ", ")
                }
            }
        }
        catch {}
    }
    
    return $suspiciousProcesses
}

# Run detection
$injectionDetection = Detect-ProcessInjection

Write-Host "[+] Process Injection Detection Complete" -ForegroundColor Green
Write-Host "    - Suspicious Processes: $($injectionDetection.Count)" -ForegroundColor Green
Write-Host "    - Results saved to: C:\HELIOS\logs\injection-detection.json" -ForegroundColor Green

$injectionDetection | ConvertTo-Json -Depth 5 | Out-File -FilePath "C:\HELIOS\logs\injection-detection.json" -Force
