#Requires -Version 5.1
<#
.SYNOPSIS
    Audits and optionally restores the critical Windows environment variables
    required by HELIOS, PowerShell, DISM, SFC, WMI, networking, and PnP tools.

.DESCRIPTION
    Report-first by default. Use -Apply to write missing baseline values.
    Existing PATH entries are preserved; required Windows entries are appended.
    A JSON backup and audit report are written before any change.
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [switch]$Apply,
    [switch]$IncludeMachine,
    [string]$ReportDirectory = "$env:ProgramData\HELIOS\Reports\EnvironmentRepair"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Test-IsAdministrator {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = [Security.Principal.WindowsPrincipal]::new($identity)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Get-WindowsRoot {
    $candidate = [Environment]::GetEnvironmentVariable('SystemRoot', 'Machine')
    if ([string]::IsNullOrWhiteSpace($candidate)) { $candidate = $env:SystemRoot }
    if ([string]::IsNullOrWhiteSpace($candidate)) { $candidate = 'C:\Windows' }
    return $candidate.TrimEnd('\\')
}

function Split-PathValue([string]$Value) {
    if ([string]::IsNullOrWhiteSpace($Value)) { return @() }
    return @($Value.Split(';', [StringSplitOptions]::RemoveEmptyEntries) | ForEach-Object { $_.Trim() } | Where-Object { $_ })
}

function Merge-PathValue([string]$Existing, [string[]]$Required) {
    $seen = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    $result = [Collections.Generic.List[string]]::new()
    foreach ($entry in @(Split-PathValue $Existing) + $Required) {
        if ([string]::IsNullOrWhiteSpace($entry)) { continue }
        if ($seen.Add($entry)) { $result.Add($entry) }
    }
    return ($result -join ';')
}

$windowsRoot = Get-WindowsRoot
$system32 = Join-Path $windowsRoot 'System32'
$timestamp = Get-Date -Format 'yyyyMMdd_HHmmss'
New-Item -ItemType Directory -Path $ReportDirectory -Force | Out-Null

$requiredMachinePath = @(
    $system32,
    $windowsRoot,
    (Join-Path $system32 'Wbem'),
    (Join-Path $system32 'WindowsPowerShell\v1.0'),
    (Join-Path $system32 'OpenSSH')
)
$requiredUserPath = @('%USERPROFILE%\AppData\Local\Microsoft\WindowsApps')

$before = [ordered]@{
    timestamp = (Get-Date).ToUniversalTime().ToString('o')
    machine = [ordered]@{}
    user = [ordered]@{}
}
foreach ($name in @('Path','TEMP','TMP','SystemRoot','windir','ComSpec','PATHEXT')) {
    $before.machine[$name] = [Environment]::GetEnvironmentVariable($name, 'Machine')
    $before.user[$name] = [Environment]::GetEnvironmentVariable($name, 'User')
}
$backupPath = Join-Path $ReportDirectory "environment-before-$timestamp.json"
$before | ConvertTo-Json -Depth 5 | Set-Content -Path $backupPath -Encoding UTF8

$desired = [ordered]@{
    User = [ordered]@{
        TEMP = '%USERPROFILE%\AppData\Local\Temp'
        TMP = '%USERPROFILE%\AppData\Local\Temp'
        Path = Merge-PathValue $before.user.Path $requiredUserPath
    }
    Machine = [ordered]@{
        TEMP = "$windowsRoot\Temp"
        TMP = "$windowsRoot\Temp"
        SystemRoot = $windowsRoot
        windir = '%SystemRoot%'
        ComSpec = '%SystemRoot%\System32\cmd.exe'
        PATHEXT = '.COM;.EXE;.BAT;.CMD;.VBS;.VBE;.JS;.JSE;.WSF;.WSH;.MSC;.CPL'
        Path = Merge-PathValue $before.machine.Path $requiredMachinePath
    }
}

$changes = [Collections.Generic.List[object]]::new()
foreach ($scope in @('User','Machine')) {
    if ($scope -eq 'Machine' -and -not $IncludeMachine) { continue }
    foreach ($entry in $desired[$scope].GetEnumerator()) {
        $oldValue = [Environment]::GetEnvironmentVariable($entry.Key, $scope)
        if ($oldValue -ne $entry.Value) {
            $changes.Add([pscustomobject]@{ Scope=$scope; Name=$entry.Key; Old=$oldValue; New=$entry.Value })
        }
    }
}

if ($Apply) {
    if ($IncludeMachine -and -not (Test-IsAdministrator)) {
        throw 'Machine-level repair requires an elevated PowerShell session.'
    }
    foreach ($change in $changes) {
        if ($PSCmdlet.ShouldProcess("$($change.Scope):$($change.Name)", 'Restore environment variable')) {
            [Environment]::SetEnvironmentVariable($change.Name, $change.New, $change.Scope)
        }
    }
    $userTemp = [Environment]::ExpandEnvironmentVariables($desired.User.TEMP)
    New-Item -ItemType Directory -Path $userTemp -Force | Out-Null
    if ($IncludeMachine) { New-Item -ItemType Directory -Path $desired.Machine.TEMP -Force | Out-Null }

    $env:SystemRoot = $windowsRoot
    $env:windir = $windowsRoot
    $env:ComSpec = Join-Path $system32 'cmd.exe'
    $env:TEMP = $userTemp
    $env:TMP = $userTemp
    $env:Path = Merge-PathValue $env:Path $requiredMachinePath

    Add-Type -Namespace Native -Name EnvironmentBroadcast -MemberDefinition @'
[System.Runtime.InteropServices.DllImport("user32.dll", SetLastError=true, CharSet=System.Runtime.InteropServices.CharSet.Auto)]
public static extern System.IntPtr SendMessageTimeout(System.IntPtr hWnd, uint Msg, System.IntPtr wParam, string lParam, uint flags, uint timeout, out System.IntPtr result);
'@
    $result = [IntPtr]::Zero
    [void][Native.EnvironmentBroadcast]::SendMessageTimeout([IntPtr]0xffff, 0x001A, [IntPtr]::Zero, 'Environment', 2, 5000, [ref]$result)
}

$tools = @('sfc.exe','DISM.exe','winmgmt.exe','netsh.exe','pnputil.exe') | ForEach-Object {
    $path = Join-Path $system32 $_
    [pscustomobject]@{ Name=$_; Path=$path; Exists=(Test-Path -LiteralPath $path) }
}

$report = [ordered]@{
    timestamp = (Get-Date).ToUniversalTime().ToString('o')
    mode = if ($Apply) { 'apply' } else { 'audit' }
    includeMachine = [bool]$IncludeMachine
    backup = $backupPath
    windowsRoot = $windowsRoot
    plannedOrAppliedChanges = @($changes)
    tools = @($tools)
    rebootRecommended = [bool]($Apply -and $changes.Count -gt 0)
}
$reportPath = Join-Path $ReportDirectory "environment-repair-$timestamp.json"
$report | ConvertTo-Json -Depth 8 | Set-Content -Path $reportPath -Encoding UTF8
$report | ConvertTo-Json -Depth 8
Write-Host "Report: $reportPath" -ForegroundColor Green
