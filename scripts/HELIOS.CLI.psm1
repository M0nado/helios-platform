#Requires -Version 5.0

<#
.SYNOPSIS
    HELIOS Platform CLI PowerShell Module
.DESCRIPTION
    Provides PowerShell cmdlets for HELIOS Platform management and automation
.VERSION
    1.0.0
.AUTHOR
    HELIOS Team
#>

$ModuleRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$CLIExecutable = Join-Path $ModuleRoot "helios-cli.exe"

# Initialize module variables
$HeliosCLICommands = @(
    "deploy", "config", "status", "health", "restart", 
    "scale", "backup", "restore", "list", "watch", "execute", "schedule"
)

<#
.SYNOPSIS
    Get HELIOS platform status
.EXAMPLE
    Get-HeliosStatus
#>
function Get-HeliosStatus {
    [CmdletBinding()]
    param()
    
    & $CLIExecutable status --json | ConvertFrom-Json
}

<#
.SYNOPSIS
    Get HELIOS platform health information
.EXAMPLE
    Get-HeliosHealth -Verbose
#>
function Get-HeliosHealth {
    [CmdletBinding()]
    param(
        [switch]$Verbose
    )
    
    $args = @("health")
    if ($Verbose) { $args += "--verbose" }
    $args += "--json"
    
    & $CLIExecutable @args | ConvertFrom-Json
}

<#
.SYNOPSIS
    Deploy a component or application
.PARAMETER Config
    Path to deployment configuration file
.EXAMPLE
    Invoke-HeliosDeploy -Config "app.json"
#>
function Invoke-HeliosDeploy {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true)]
        [string]$Config
    )
    
    & $CLIExecutable deploy --config $Config --json | ConvertFrom-Json
}

<#
.SYNOPSIS
    Get or set HELIOS configuration
.PARAMETER Action
    Action to perform: get, set, list
.PARAMETER Key
    Configuration key
.PARAMETER Value
    Configuration value
.EXAMPLE
    Get-HeliosConfig -Action get -Key "api.endpoint"
#>
function Invoke-HeliosConfig {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true)]
        [ValidateSet("get", "set", "list")]
        [string]$Action,
        
        [string]$Key,
        [string]$Value
    )
    
    $args = @("config", $Action)
    if ($Key) { $args += $Key }
    if ($Value) { $args += $Value }
    $args += "--json"
    
    & $CLIExecutable @args | ConvertFrom-Json
}

<#
.SYNOPSIS
    Restart HELIOS services
.PARAMETER Service
    Service to restart (default: all)
.EXAMPLE
    Restart-HeliosService -Service "api"
#>
function Restart-HeliosService {
    [CmdletBinding()]
    param(
        [string]$Service = "all"
    )
    
    & $CLIExecutable restart $Service --json | ConvertFrom-Json
}

<#
.SYNOPSIS
    Scale a HELIOS component
.PARAMETER Component
    Component to scale
.PARAMETER Instances
    Number of instances
.EXAMPLE
    Scale-HeliosComponent -Component "web" -Instances 5
#>
function Scale-HeliosComponent {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true)]
        [string]$Component,
        
        [Parameter(Mandatory=$true)]
        [int]$Instances
    )
    
    & $CLIExecutable scale $Component --instances $Instances --json | ConvertFrom-Json
}

<#
.SYNOPSIS
    Create a HELIOS backup
.PARAMETER Path
    Backup destination path
.EXAMPLE
    New-HeliosBackup -Path "C:\backups"
#>
function New-HeliosBackup {
    [CmdletBinding()]
    param(
        [string]$Path = "./backups"
    )
    
    & $CLIExecutable backup --path $Path --json | ConvertFrom-Json
}

<#
.SYNOPSIS
    Restore from a HELIOS backup
.PARAMETER BackupFile
    Backup file to restore from
.EXAMPLE
    Restore-HeliosBackup -BackupFile "backup.tar.gz"
#>
function Restore-HeliosBackup {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true)]
        [string]$BackupFile
    )
    
    & $CLIExecutable restore $BackupFile --json | ConvertFrom-Json
}

<#
.SYNOPSIS
    List HELIOS resources
.PARAMETER ResourceType
    Type of resource: services, components, etc.
.EXAMPLE
    Get-HeliosResource -ResourceType "services"
#>
function Get-HeliosResource {
    [CmdletBinding()]
    param(
        [string]$ResourceType = "services"
    )
    
    & $CLIExecutable list $ResourceType --json | ConvertFrom-Json
}

<#
.SYNOPSIS
    Execute a HELIOS script
.PARAMETER ScriptPath
    Path to script file
.EXAMPLE
    Invoke-HeliosScript -ScriptPath "deploy.ps1"
#>
function Invoke-HeliosScript {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true)]
        [string]$ScriptPath
    )
    
    & $CLIExecutable execute $ScriptPath --json | ConvertFrom-Json
}

<#
.SYNOPSIS
    Schedule a HELIOS task
.PARAMETER TaskName
    Name of the task
.PARAMETER Command
    Command to execute
.PARAMETER Schedule
    Schedule expression: daily, weekly, hourly
.EXAMPLE
    New-HeliosScheduledTask -TaskName "daily-backup" -Command "backup" -Schedule "daily"
#>
function New-HeliosScheduledTask {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true)]
        [string]$TaskName,
        
        [Parameter(Mandatory=$true)]
        [string]$Command,
        
        [ValidateSet("hourly", "daily", "weekly", "monthly")]
        [string]$Schedule = "daily"
    )
    
    & $CLIExecutable schedule $TaskName --command $Command --schedule $Schedule --json | ConvertFrom-Json
}

<#
.SYNOPSIS
    Get HELIOS command history
.PARAMETER Count
    Number of history entries to retrieve
.EXAMPLE
    Get-HeliosHistory -Count 20
#>
function Get-HeliosHistory {
    [CmdletBinding()]
    param(
        [int]$Count = 50
    )
    
    & $CLIExecutable history --count $Count --json | ConvertFrom-Json
}

<#
.SYNOPSIS
    Execute HELIOS CLI commands
.PARAMETER ArgumentList
    Command line arguments
.EXAMPLE
    Invoke-HeliosCLI -ArgumentList @("status", "--verbose")
#>
function Invoke-HeliosCLI {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true)]
        [string[]]$ArgumentList
    )
    
    & $CLIExecutable @ArgumentList
}

# Export all public functions
Export-ModuleMember -Function @(
    "Get-HeliosStatus",
    "Get-HeliosHealth",
    "Invoke-HeliosDeploy",
    "Invoke-HeliosConfig",
    "Restart-HeliosService",
    "Scale-HeliosComponent",
    "New-HeliosBackup",
    "Restore-HeliosBackup",
    "Get-HeliosResource",
    "Invoke-HeliosScript",
    "New-HeliosScheduledTask",
    "Get-HeliosHistory",
    "Invoke-HeliosCLI"
)

Write-Verbose "HELIOS CLI PowerShell Module loaded successfully"
