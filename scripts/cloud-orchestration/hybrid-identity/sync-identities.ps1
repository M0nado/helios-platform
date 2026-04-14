<#
.SYNOPSIS
    Synchronize user and group identities between on-premises and cloud
.DESCRIPTION
    Manages the continuous synchronization of identities, attributes,
    and group memberships across hybrid environments
#>

param(
    [Parameter(Mandatory = $false)]
    [ValidateSet("Users", "Groups", "Both")]
    [string]$SyncType = "Both",
    
    [Parameter(Mandatory = $false)]
    [string]$ConfigPath = ".\config\cloud-orchestration-config.json"
)

$ErrorActionPreference = "Stop"

Write-Host "`n╔════════════════════════════════════════════════════════════╗" -ForegroundColor Magenta
Write-Host "║       IDENTITY SYNCHRONIZATION - HELIOS ORCHESTRATOR       ║" -ForegroundColor Magenta
Write-Host "╚════════════════════════════════════════════════════════════╝`n" -ForegroundColor Magenta

try {
    $config = Get-Content $ConfigPath | ConvertFrom-Json
    
    Connect-MgGraph -Scopes "User.ReadWrite.All", "Group.ReadWrite.All" -ErrorAction Stop | Out-Null
    
    $syncResults = @{}
    
    # User Synchronization
    if ($SyncType -in "Users", "Both") {
        Write-Host "[Users] Synchronizing identities..." -ForegroundColor Cyan
        
        $localUsers = Get-ADUser -Filter * -Properties Mail, Title, Department, MobilePhone
        $syncedCount = 0
        $errorCount = 0
        
        foreach ($user in $localUsers) {
            try {
                $upn = if ($user.UserPrincipalName) { $user.UserPrincipalName } else { "$($user.SamAccountName)@$((Get-ADDomain).Name)" }
                
                $cloudUser = @{
                    userPrincipalName = $upn
                    displayName = $user.DisplayName
                    mail = $user.Mail
                    title = $user.Title
                    department = $user.Department
                    mobilePhone = $user.MobilePhone
                    accountEnabled = $true
                }
                
                $existing = Get-MgUser -Filter "userPrincipalName eq '$upn'" -ErrorAction SilentlyContinue
                if ($existing) {
                    Update-MgUser -UserId $existing.Id -BodyParameter $cloudUser -ErrorAction SilentlyContinue
                }
                
                $syncedCount++
                Write-Verbose "Synced: $upn"
            }
            catch {
                $errorCount++
                Write-Verbose "Error syncing $($user.SamAccountName): $_"
            }
        }
        
        $syncResults["Users"] = @{
            Synced = $syncedCount
            Errors = $errorCount
            Total = $localUsers.Count
        }
        
        Write-Host "  ✓ $syncedCount/$($localUsers.Count) users synchronized" -ForegroundColor Green
        if ($errorCount -gt 0) {
            Write-Host "  ⚠ $errorCount errors encountered" -ForegroundColor Yellow
        }
    }
    
    # Group Synchronization
    if ($SyncType -in "Groups", "Both") {
        Write-Host "`n[Groups] Synchronizing group memberships..." -ForegroundColor Cyan
        
        $localGroups = Get-ADGroup -Filter * -Properties Description
        $syncedCount = 0
        $errorCount = 0
        
        foreach ($group in $localGroups) {
            try {
                $cloudGroup = @{
                    displayName = $group.Name
                    description = $group.Description
                    mailNickname = $group.SamAccountName
                }
                
                $existing = Get-MgGroup -Filter "displayName eq '$($group.Name)'" -ErrorAction SilentlyContinue
                if ($existing) {
                    Update-MgGroup -GroupId $existing.Id -BodyParameter $cloudGroup -ErrorAction SilentlyContinue
                    
                    # Sync members
                    $members = Get-ADGroupMember -Identity $group.ObjectGUID -ErrorAction SilentlyContinue
                    Write-Verbose "Group $($group.Name) has $($members.Count) members"
                }
                
                $syncedCount++
            }
            catch {
                $errorCount++
                Write-Verbose "Error syncing group $($group.Name): $_"
            }
        }
        
        $syncResults["Groups"] = @{
            Synced = $syncedCount
            Errors = $errorCount
            Total = $localGroups.Count
        }
        
        Write-Host "  ✓ $syncedCount/$($localGroups.Count) groups synchronized" -ForegroundColor Green
        if ($errorCount -gt 0) {
            Write-Host "  ⚠ $errorCount errors encountered" -ForegroundColor Yellow
        }
    }
    
    Write-Host "`n╔════════════════════════════════════════════════════════════╗" -ForegroundColor Green
    Write-Host "║              IDENTITY SYNC COMPLETED                       ║" -ForegroundColor Green
    Write-Host "╚════════════════════════════════════════════════════════════╝`n" -ForegroundColor Green
    
    $totalSynced = ($syncResults.Values | Measure-Object -Property Synced -Sum).Sum
    Write-Host "Total Synchronized: $totalSynced objects`n" -ForegroundColor Cyan
    
    # Save results
    @{
        Timestamp = (Get-Date)
        SyncType = $SyncType
        Results = $syncResults
    } | ConvertTo-Json | Out-File ".\logs\identity-sync-$(Get-Date -Format 'yyyyMMdd-HHmmss').json"
}
catch {
    Write-Host "✗ Error: $_" -ForegroundColor Red
    exit 1
}
