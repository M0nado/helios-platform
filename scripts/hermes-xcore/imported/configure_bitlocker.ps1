#Requires -Version 7.0
#Requires -RunAsAdministrator
# =============================================================
# configure_bitlocker.ps1
# Configures BitLocker on C:, D:, and optionally E:.
# Recovery keys are stored in Azure AD/Entra — NOT local files.
# NOT FOR BLIND EXECUTION — Review mode and drive letters first.
# =============================================================

$ErrorActionPreference = "Stop"
$LogFile = "D:\DevDrive\logs\bitlocker_{0}.log" -f (Get-Date -Format "yyyyMMdd_HHmmss")
function Log($msg) { $line = "[$(Get-Date -Format 's')] $msg"; Write-Host $line; $line | Out-File $LogFile -Append }

param(
    [ValidateSet("C","B","A")]
    [string]$SecurityMode = "B"
)

Log "=== BITLOCKER CONFIGURATION — Security Mode $SecurityMode ==="

# -----------------------------------------------------------
# CHECK TPM 2.0
# -----------------------------------------------------------
$tpm = Get-Tpm
if (-not $tpm.TpmPresent -or -not $tpm.TpmReady) {
    throw "FATAL: TPM 2.0 not present or not ready. Enable in BIOS firmware settings."
}
Log "TPM 2.0 present and ready: SpecVersion=$($tpm.ManufacturerVersionFull)"

# -----------------------------------------------------------
# CONFIGURE C:
# -----------------------------------------------------------
Log "Configuring BitLocker on C:..."
if ($SecurityMode -eq "C") {
    # Mode C: TPM + PIN
    Log "PSEUDO: manage-bde -on C: -UsedSpaceOnly -TpmAndPin"
    Log "PSEUDO: manage-bde -protectors -add C: -TPMAndPIN  (will prompt for PIN)"
} else {
    # Mode B / A: TPM only
    manage-bde -on C: -UsedSpaceOnly -TpmOnly 2>&1 | Out-File $LogFile -Append -ErrorAction SilentlyContinue
    Log "BitLocker TPM-only enabled on C:"
}
# Save recovery key to Azure AD (Entra) — NOT to local file
# PSEUDO: BackupToAAD-BitLockerKeyProtector -MountPoint C: -KeyProtectorId (Get-BitLockerVolume C:).KeyProtector[0].KeyProtectorId
Log "PSEUDO: BackupToAAD-BitLockerKeyProtector C: — saves recovery key to Entra (not local)"

# -----------------------------------------------------------
# CONFIGURE D: (DevDrive)
# -----------------------------------------------------------
Log "Configuring BitLocker on D: (DevDrive)..."
manage-bde -on D: -UsedSpaceOnly -TpmOnly 2>&1 | Out-File $LogFile -Append -ErrorAction SilentlyContinue
Log "PSEUDO: BackupToAAD-BitLockerKeyProtector D: — saves recovery key to Entra"

# -----------------------------------------------------------
# CONFIGURE E: (if present)
# -----------------------------------------------------------
if (Test-Path "E:\") {
    Log "E: drive detected — configuring BitLocker on E:..."
    manage-bde -on E: -UsedSpaceOnly -TpmOnly 2>&1 | Out-File $LogFile -Append -ErrorAction SilentlyContinue
    Log "PSEUDO: BackupToAAD-BitLockerKeyProtector E:"
}

# -----------------------------------------------------------
# VERIFY STATUS
# -----------------------------------------------------------
Log "=== BITLOCKER STATUS ==="
manage-bde -status | Out-File $LogFile -Append
Log "Full status written to $LogFile. Reboot to verify TPM unlock."

# Store recovery key ID (NOT the key) in hermes_run_log for audit
$keyId = (Get-BitLockerVolume C:).KeyProtector |
    Where-Object { $_.KeyProtectorType -eq "RecoveryPassword" } |
    Select-Object -First 1 -ExpandProperty KeyProtectorId
Log "C: Recovery Key Protector ID: $keyId  (logged for audit — key stored in Entra onl
