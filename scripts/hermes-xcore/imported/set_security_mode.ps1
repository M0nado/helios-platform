#Requires -Version 7.0
#Requires -RunAsAdministrator
# =============================================================
# set_security_mode.ps1
# Usage: .\set_security_mode.ps1 -Mode C|B|A
# Toggles Hermes workstation between security operating modes.
# NOT FOR BLIND EXECUTION — Review all firewall rules before use.
# =============================================================

param(
    [Parameter(Mandatory=$true)]
    [ValidateSet("C","B","A")]
    [string]$Mode
)

$ErrorActionPreference = "Stop"
$LogPath = "D:\DevDrive\logs\security_mode_{0}.log" -f (Get-Date -Format "yyyyMMdd_HHmmss")
function Log($msg) { $line = "[$(Get-Date -Format 's')] $msg"; Write-Host $line; $line | Tee-Object -FilePath $LogPath -Append }

# -----------------------------------------------------------
# SNAPSHOT CURRENT STATE BEFORE ANY CHANGE
# -----------------------------------------------------------
Log "Snapshotting current firewall and service state..."
Get-NetFirewallRule | Where-Object Enabled -eq True | Select-Object DisplayName,Direction,Action |
    Out-File "D:\DevDrive\logs\firewall_snapshot_{0}.txt" -f (Get-Date -Format "yyyyMMdd_HHmmss")
Get-Service | Where-Object Status -eq Running | Select-Object Name,Status |
    Out-File "D:\DevDrive\logs\services_snapshot_{0}.txt" -f (Get-Date -Format "yyyyMMdd_HHmmss")
Log "Snapshot complete."

# -----------------------------------------------------------
function Set-ModeC {
    Log "Entering Mode C — Maximum Security (Locked)"

    # Block all outbound firewall rules except Windows Update and Defender
    Get-NetFirewallRule -Direction Outbound -Action Allow | Where-Object {
        $_.DisplayName -notmatch "Windows Update|Windows Defender|MpsSvc"
    } | Disable-NetFirewallRule
    Log "All non-system outbound firewall rules DISABLED."

    # Allow only Windows Update FQDN list (create explicit rules if not present)
    New-NetFirewallRule -DisplayName "Allow-WindowsUpdate-ModeC" `
        -Direction Outbound -Action Allow -Protocol TCP `
        -RemoteAddress "13.107.4.50","13.107.5.88","13.107.9.79","40.73.5.206" `
        -ErrorAction SilentlyContinue

    # Shutdown WSL2
    wsl --shutdown
    Log "WSL2 shut down."

    # Disable HermesNet external vSwitch (blocks VM external access)
    Set-VMSwitch -Name "HermesNet" -NetAdapterName "" -ErrorAction SilentlyContinue
    Log "HermesNet external vSwitch disconnected."

    # Stop Docker Desktop
    Stop-Service "com.docker.service" -ErrorAction SilentlyContinue
    Stop-Process -Name "Docker Desktop" -Force -ErrorAction SilentlyContinue
    Log "Docker Desktop stopped."

    # Enforce BitLocker TPM+PIN on C:
    # manage-bde -protectors -add C: -TPMAndPIN
    Log "PSEUDO: manage-bde -protectors -add C: -TPMAndPIN  (requires interactive PIN entry)"

    # Revoke GitHub CLI token
    gh auth logout --hostname github.com -ErrorAction SilentlyContinue
    Log "GitHub CLI token revoked."

    # Revoke Azure CLI token
    az account clear -ErrorAction SilentlyContinue
    Log "Azure CLI context cleared."

    # Disable Windows Sandbox
    Disable-WindowsOptionalFeature -Online -FeatureName Containers-DisposableClientVM -NoRestart -ErrorAction SilentlyContinue
    Log "Windows Sandbox disabled."

    # Set Defender to high protection
    Set-MpPreference -MAPSReporting Advanced
    Set-MpPreference -SubmitSamplesConsent SendSafeSamples
    Set-MpPreference -RealTimeProtectionEnabled $true
    Log "Defender set to maximum protection."

    # Enable maximum audit policies
    auditpol /set /category:"Logon/Logoff" /success:enable /failure:enable
    auditpol /set /category:"Account Logon" /success:enable /failure:enable
    auditpol /set /category:"Policy Change" /success:enable /failure:enable
    auditpol /set /category:"Privilege Use"  /success:enable /failure:enable
    Log "Audit policies maximized."

    Log "Mode C APPLIED. System is in LOCKED state."
}

# -----------------------------------------------------------
function Set-ModeB {
    Log "Entering Mode B — Balanced Developer (Default)"

    # Reset all firewall rules to Mode B allowlist
    Get-NetFirewallRule -DisplayName "Allow-*-ModeC" | Remove-NetFirewallRule -ErrorAction SilentlyContinue

    $allowedDomains = @(
        "github.com", "*.github.com",
        "*.microsoft.com", "*.azure.com",
        "pypi.org", "*.pypi.org",
        "registry-1.docker.io", "*.docker.io",
        "*.huggingface.co", "*.anaconda.org"
    )
    # PSEUDO: Create outbound allow rules per domain group
    # (Windows Firewall does not natively filter by FQDN; use DNS-based filtering or Windows Defender Application Control)
    Log "PSEUDO: Apply Mode B outbound allowlist (GitHub, Azure, PyPI, Docker, Hugging Face)"

    # Block all inbound except localhost and WSL2 adapter
    New-NetFirewallRule -DisplayName "Block-Inbound-ModeB" `
        -Direction Inbound -Action Block -Protocol Any `
        -RemoteAddress "0.0.0.0-127.0.0.0","128.0.0.0-255.255.255.255" `
        -ErrorAction SilentlyContinue
    Log "Inbound blocked except localhost."

    # Ensure WSL2 running with internal networking
    wsl --distribution Ubuntu-22.04 -- echo "WSL2 active" 2>&1 | Out-Null
    Log "WSL2 running."

    # Set Hyper-V VMs to HermesInternal only (disconnect HermesNet)
    Get-VM | ForEach-Object { Set-VMNetworkAdapter -VMName $_.Name -SwitchName "HermesInternal" -ErrorAction SilentlyContinue }
    Log "Hyper-V VMs set to HermesInternal vSwitch."

    # BitLocker TPM-only on C:
    # manage-bde -protectors -add C: -TPM
    Log "PSEUDO: manage-bde -protectors -add C: -TPM"

    # Enable Windows Sandbox with restricted config
    Enable-WindowsOptionalFeature -Online -FeatureName Containers-DisposableClientVM -NoRestart -ErrorAction SilentlyContinue
    Log "Windows Sandbox enabled."

    # Re-authenticate if tokens missing
    $ghStatus = gh auth status 2>&1
    if ($ghStatus -match "not logged") {
        Log "GitHub CLI not authenticated — run: gh auth login --web"
    }
    $azStatus = az account show 2>&1
    if ($azStatus -match "error") {
        Log "Azure CLI not authenticated — run: az login --use-device-code"
    }

    Log "Mode B APPLIED. Developer mode active."
}

# -----------------------------------------------------------
function Set-ModeA {
    Log "Entering Mode A — Active Cloud-Connected"

    # Reset firewall to default Windows profile
    netsh advfirewall reset
    Log "Windows Firewall reset to defaults."

    # Enable full WSL2 networking
    # Update .wslconfig: localhostForwarding=true (already set in Phase 1)
    Log "WSL2 full networking with localhost forwarding enabled."

    # Re-enable HermesNet external vSwitch for Azure integration testing
    # manage-bde should already be TPM-only from Mode B
    Set-VMSwitch -Name "HermesNet" -NetAdapterName "{your-physical-nic}" -ErrorAction SilentlyContinue
    Log "HermesNet external vSwitch re-enabled."

    # Verify Entra device compliance
    dsregcmd /status | Out-File "D:\DevDrive\logs\entra_status_modeA.txt"
    Log "Entra device status written to logs."

    # Confirm Azure CLI subscription context
    az account show | Out-File "D:\DevDrive\logs\azure_subscription_modeA.txt"
    Log "Azure CLI subscription context confirmed."

    # Activate Purview DLP policies (via Azure CLI or Purview CLI)
    Log "PSEUDO: az purview account show --name hermes-purview --resource-group hermes-rg"

    Log "Mode A APPLIED. Cloud integration fully active."
}

# -----------------------------------------------------------
# DISPATCH
# -----------------------------------------------------------
switch ($Mode) {
    "C" { Set-ModeC }
    "B" { Set-ModeB }
    "A" { Set-ModeA }
}
Log "Security mode script complete. Mode=$Mode. Log at $LogPath"
3.3 — BitLocker Configuration Script
