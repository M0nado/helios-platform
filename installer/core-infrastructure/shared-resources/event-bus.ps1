#Requires -Version 5.1
<#
.SYNOPSIS
    HELIOS Event Bus
    Component communication through event-driven architecture

.DESCRIPTION
    Provides:
    - Event registration and management
    - Event emission with payloads
    - Event subscription with callbacks
    - Event filtering and prioritization
    - Event history tracking
    - Async event processing

.VERSION
    1.0.0

.AUTHOR
    HELIOS Infrastructure Team
#>

# Import common functions
$commonFunctionsPath = Join-Path $PSScriptRoot "common-functions.psm1"
if (Test-Path $commonFunctionsPath) {
    Import-Module $commonFunctionsPath -Force
}

# Module-level variables
$script:EventRegistry = @{}
$script:EventSubscriptions = @{}
$script:EventHistory = @()
$script:MaxHistorySize = 5000
$script:ProcessingQueue = @()
$script:EventFilters = @{}

# Event priority levels
$script:EventPriorities = @{
    "CRITICAL" = 1
    "HIGH" = 2
    "NORMAL" = 3
    "LOW" = 4
}

<#
.SYNOPSIS
    Register an event type

.PARAMETER EventName
    Name of the event

.PARAMETER Description
    Event description

.PARAMETER Priority
    Default priority (CRITICAL, HIGH, NORMAL, LOW)

.PARAMETER Schema
    Event payload schema (hashtable describing expected properties)
#>
function Register-Event {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true)]
        [string]$EventName,
        
        [Parameter(Mandatory=$false)]
        [string]$Description = "",
        
        [Parameter(Mandatory=$false)]
        [ValidateSet("CRITICAL", "HIGH", "NORMAL", "LOW")]
        [string]$Priority = "NORMAL",
        
        [Parameter(Mandatory=$false)]
        [hashtable]$Schema = @{}
    )
    
    try {
        if ($script:EventRegistry.ContainsKey($EventName)) {
            throw "Event already registered: $EventName"
        }
        
        $script:EventRegistry[$EventName] = @{
            EventName = $EventName
            Description = $Description
            Priority = $Priority
            Schema = $Schema
            Registered = Get-Date
            EmissionCount = 0
            SubscriberCount = 0
        }
        
        $script:EventSubscriptions[$EventName] = @()
        
        Log-Message -Message "Event registered: $EventName (Priority: $Priority)" -Component "EventBus" -Level "Success"
    }
    catch {
        Log-Error -Message "Failed to register event: $EventName" -Exception $_ -Component "EventBus"
        throw
    }
}

<#
.SYNOPSIS
    Unregister an event type

.PARAMETER EventName
    Name of the event
#>
function Unregister-Event {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true)]
        [string]$EventName
    )
    
    try {
        if (-not $script:EventRegistry.ContainsKey($EventName)) {
            throw "Event not found: $EventName"
        }
        
        $script:EventRegistry.Remove($EventName)
        $script:EventSubscriptions.Remove($EventName)
        
        Log-Message -Message "Event unregistered: $EventName" -Component "EventBus" -Level "Info"
    }
    catch {
        Log-Error -Message "Failed to unregister event: $EventName" -Exception $_ -Component "EventBus"
        throw
    }
}

<#
.SYNOPSIS
    Subscribe to an event

.PARAMETER EventName
    Name of the event to subscribe to

.PARAMETER Callback
    ScriptBlock to execute when event is emitted

.PARAMETER Component
    Component name for subscription tracking

.PARAMETER Filter
    Optional hashtable of filters (e.g., @{Status="Active"})

.PARAMETER Priority
    Execution priority relative to other subscribers (1-10, 1 is highest)
#>
function Subscribe-Event {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true)]
        [string]$EventName,
        
        [Parameter(Mandatory=$true)]
        [scriptblock]$Callback,
        
        [Parameter(Mandatory=$false)]
        [string]$Component = "Unknown",
        
        [Parameter(Mandatory=$false)]
        [hashtable]$Filter = @{},
        
        [Parameter(Mandatory=$false)]
        [ValidateRange(1, 10)]
        [int]$Priority = 5
    )
    
    try {
        if (-not $script:EventRegistry.ContainsKey($EventName)) {
            throw "Event not registered: $EventName"
        }
        
        $subscriptionId = "sub_$(Get-Random -Minimum 100000 -Maximum 999999)_$(Get-Date -Format 'yyyyMMddHHmmss')"
        
        $subscription = @{
            SubscriptionId = $subscriptionId
            EventName = $EventName
            Component = $Component
            Callback = $Callback
            Filter = $Filter
            Priority = $Priority
            Subscribed = Get-Date
            CallCount = 0
            LastExecuted = $null
            LastError = $null
        }
        
        $script:EventSubscriptions[$EventName] += $subscription
        $script:EventRegistry[$EventName].SubscriberCount++
        
        Log-Message -Message "Subscribed to event: $EventName from $Component (Sub: $subscriptionId)" -Component "EventBus" -Level "Success"
        
        return $subscriptionId
    }
    catch {
        Log-Error -Message "Failed to subscribe to event: $EventName" -Exception $_ -Component "EventBus"
        throw
    }
}

<#
.SYNOPSIS
    Unsubscribe from an event

.PARAMETER EventName
    Name of the event

.PARAMETER SubscriptionId
    The subscription ID returned from Subscribe-Event
#>
function Unsubscribe-Event {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true)]
        [string]$EventName,
        
        [Parameter(Mandatory=$true)]
        [string]$SubscriptionId
    )
    
    try {
        if (-not $script:EventSubscriptions.ContainsKey($EventName)) {
            throw "Event not found: $EventName"
        }
        
        $subscription = $script:EventSubscriptions[$EventName] | 
            Where-Object { $_.SubscriptionId -eq $SubscriptionId }
        
        if (-not $subscription) {
            throw "Subscription not found: $SubscriptionId"
        }
        
        $script:EventSubscriptions[$EventName] = $script:EventSubscriptions[$EventName] | 
            Where-Object { $_.SubscriptionId -ne $SubscriptionId }
        
        $script:EventRegistry[$EventName].SubscriberCount--
        
        Log-Message -Message "Unsubscribed from event: $EventName (Sub: $SubscriptionId)" -Component "EventBus" -Level "Info"
    }
    catch {
        Log-Error -Message "Failed to unsubscribe from event: $EventName" -Exception $_ -Component "EventBus"
        throw
    }
}

<#
.SYNOPSIS
    Emit an event to all subscribers

.PARAMETER EventName
    Name of the event

.PARAMETER Payload
    Event payload (hashtable)

.PARAMETER Component
    Component emitting the event

.PARAMETER Priority
    Event priority for execution order

.PARAMETER Async
    Whether to process subscriptions asynchronously
#>
function Emit-Event {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true)]
        [string]$EventName,
        
        [Parameter(Mandatory=$false)]
        [hashtable]$Payload = @{},
        
        [Parameter(Mandatory=$false)]
        [string]$Component = "Unknown",
        
        [Parameter(Mandatory=$false)]
        [ValidateSet("CRITICAL", "HIGH", "NORMAL", "LOW")]
        [string]$Priority = "NORMAL",
        
        [Parameter(Mandatory=$false)]
        [bool]$Async = $false
    )
    
    try {
        if (-not $script:EventRegistry.ContainsKey($EventName)) {
            throw "Event not registered: $EventName"
        }
        
        $eventId = "evt_$(Get-Random -Minimum 100000 -Maximum 999999)_$(Get-Date -Format 'yyyyMMddHHmmss')"
        
        $event = @{
            EventId = $eventId
            EventName = $EventName
            Component = $Component
            Priority = $Priority
            Payload = $Payload
            Timestamp = Get-Date
            SubscriberCount = 0
            ProcessedCount = 0
        }
        
        # Add to history
        $script:EventHistory += $event
        
        # Trim history if too large
        if ($script:EventHistory.Count -gt $script:MaxHistorySize) {
            $script:EventHistory = $script:EventHistory[-$script:MaxHistorySize..-1]
        }
        
        $script:EventRegistry[$EventName].EmissionCount++
        
        Log-Message -Message "Event emitted: $EventName (ID: $eventId, Priority: $Priority)" -Component "EventBus" -Level "Debug"
        
        if ($Async) {
            # Queue for async processing
            $script:ProcessingQueue += @{
                Event = $event
                Subscriptions = $script:EventSubscriptions[$EventName].Clone()
            }
        }
        else {
            # Synchronous processing
            Process-EventSubscriptions -Event $event
        }
        
        return $eventId
    }
    catch {
        Log-Error -Message "Failed to emit event: $EventName" -Exception $_ -Component "EventBus"
        throw
    }
}

<#
.SYNOPSIS
    Process event subscriptions

.PARAMETER Event
    The event object

.PARAMETER Component
    Optional: filter to specific component
#>
function Process-EventSubscriptions {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true)]
        [object]$Event,
        
        [Parameter(Mandatory=$false)]
        [string]$Component = $null
    )
    
    try {
        $subscriptions = $script:EventSubscriptions[$Event.EventName]
        
        # Sort by priority
        $subscriptions = $subscriptions | Sort-Object -Property Priority
        
        foreach ($subscription in $subscriptions) {
            # Check filter
            if (-not (Test-EventFilter -Event $Event -Filter $subscription.Filter)) {
                continue
            }
            
            # Check component filter
            if ($Component -and $subscription.Component -ne $Component) {
                continue
            }
            
            try {
                $eventData = @{
                    EventId = $Event.EventId
                    EventName = $Event.EventName
                    Component = $Event.Component
                    Priority = $Event.Priority
                    Payload = $Event.Payload
                    Timestamp = $Event.Timestamp
                    SubscriptionId = $subscription.SubscriptionId
                }
                
                & $subscription.Callback $eventData
                
                $subscription.CallCount++
                $subscription.LastExecuted = Get-Date
                $Event.ProcessedCount++
                
                Log-Message -Message "Event processed by $($subscription.Component): $($Event.EventName)" -Component "EventBus" -Level "Debug"
            }
            catch {
                $subscription.LastError = $_.Message
                Log-Error -Message "Subscription handler failed for $($Event.EventName)" -Exception $_ -Component "EventBus"
            }
        }
    }
    catch {
        Log-Error -Message "Failed to process event subscriptions" -Exception $_ -Component "EventBus"
        throw
    }
}

<#
.SYNOPSIS
    Test if an event matches a filter

.PARAMETER Event
    The event to test

.PARAMETER Filter
    The filter hashtable
#>
function Test-EventFilter {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true)]
        [object]$Event,
        
        [Parameter(Mandatory=$true)]
        [hashtable]$Filter
    )
    
    if ($Filter.Count -eq 0) {
        return $true
    }
    
    foreach ($key in $Filter.Keys) {
        if ($Event.Payload[$key] -ne $Filter[$key]) {
            return $false
        }
    }
    
    return $true
}

<#
.SYNOPSIS
    Process queued async events

.PARAMETER MaxProcessCount
    Maximum number of events to process
#>
function Process-AsyncEvents {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$false)]
        [int]$MaxProcessCount = 10
    )
    
    try {
        $processCount = 0
        
        while ($script:ProcessingQueue.Count -gt 0 -and $processCount -lt $MaxProcessCount) {
            $item = $script:ProcessingQueue[0]
            $script:ProcessingQueue = $script:ProcessingQueue[1..-1]
            
            Process-EventSubscriptions -Event $item.Event
            
            $processCount++
        }
        
        Log-Message -Message "Async events processed: $processCount (Remaining: $($script:ProcessingQueue.Count))" -Component "EventBus" -Level "Debug"
    }
    catch {
        Log-Error -Message "Failed to process async events" -Exception $_ -Component "EventBus"
    }
}

<#
.SYNOPSIS
    Get event history

.PARAMETER EventName
    Optional: filter by event name

.PARAMETER Component
    Optional: filter by component

.PARAMETER Hours
    Look back this many hours
#>
function Get-EventHistory {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$false)]
        [string]$EventName = $null,
        
        [Parameter(Mandatory=$false)]
        [string]$Component = $null,
        
        [Parameter(Mandatory=$false)]
        [int]$Hours = 1
    )
    
    try {
        $cutoffTime = (Get-Date).AddHours(-$Hours)
        
        $history = $script:EventHistory | Where-Object {
            $_.Timestamp -gt $cutoffTime -and
            ($null -eq $EventName -or $_.EventName -eq $EventName) -and
            ($null -eq $Component -or $_.Component -eq $Component)
        }
        
        return $history
    }
    catch {
        Log-Error -Message "Failed to retrieve event history" -Exception $_ -Component "EventBus"
        throw
    }
}

<#
.SYNOPSIS
    Get information about an event

.PARAMETER EventName
    Name of the event
#>
function Get-EventInfo {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true)]
        [string]$EventName
    )
    
    try {
        if (-not $script:EventRegistry.ContainsKey($EventName)) {
            throw "Event not found: $EventName"
        }
        
        $eventInfo = $script:EventRegistry[$EventName]
        $subscriptions = $script:EventSubscriptions[$EventName]
        
        return @{
            EventName = $EventName
            Description = $eventInfo.Description
            Priority = $eventInfo.Priority
            Schema = $eventInfo.Schema
            Registered = $eventInfo.Registered
            EmissionCount = $eventInfo.EmissionCount
            SubscriberCount = $eventInfo.SubscriberCount
            Subscribers = @($subscriptions | Select-Object -Property SubscriptionId, Component, Priority)
        }
    }
    catch {
        Log-Error -Message "Failed to get event info: $EventName" -Exception $_ -Component "EventBus"
        throw
    }
}

<#
.SYNOPSIS
    Get all registered events
#>
function Get-RegisteredEvents {
    [CmdletBinding()]
    param()
    
    try {
        $events = @()
        
        foreach ($eventName in $script:EventRegistry.Keys) {
            $eventInfo = $script:EventRegistry[$eventName]
            $events += @{
                EventName = $eventName
                Description = $eventInfo.Description
                Priority = $eventInfo.Priority
                SubscriberCount = $eventInfo.SubscriberCount
                EmissionCount = $eventInfo.EmissionCount
            }
        }
        
        return $events | Sort-Object -Property EventName
    }
    catch {
        Log-Error -Message "Failed to get registered events" -Exception $_ -Component "EventBus"
        throw
    }
}

<#
.SYNOPSIS
    Get event bus statistics
#>
function Get-EventBusStats {
    [CmdletBinding()]
    param()
    
    try {
        $totalSubscriptions = 0
        $totalEmissions = 0
        
        foreach ($event in $script:EventRegistry.Values) {
            $totalSubscriptions += $event.SubscriberCount
            $totalEmissions += $event.EmissionCount
        }
        
        return @{
            RegisteredEvents = $script:EventRegistry.Count
            TotalSubscriptions = $totalSubscriptions
            TotalEmissions = $totalEmissions
            HistorySize = $script:EventHistory.Count
            QueuedAsyncEvents = $script:ProcessingQueue.Count
        }
    }
    catch {
        Log-Error -Message "Failed to get event bus statistics" -Exception $_ -Component "EventBus"
        throw
    }
}

<#
.SYNOPSIS
    Clear event history

.PARAMETER EventName
    Optional: clear only for specific event
#>
function Clear-EventHistory {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$false)]
        [string]$EventName = $null
    )
    
    try {
        if ($EventName) {
            $script:EventHistory = $script:EventHistory | Where-Object { $_.EventName -ne $EventName }
            Log-Message -Message "Event history cleared for: $EventName" -Component "EventBus" -Level "Info"
        }
        else {
            $script:EventHistory = @()
            Log-Message -Message "Event history cleared completely" -Component "EventBus" -Level "Info"
        }
    }
    catch {
        Log-Error -Message "Failed to clear event history" -Exception $_ -Component "EventBus"
        throw
    }
}

# Export public functions
Export-ModuleMember -Function @(
    'Register-Event',
    'Unregister-Event',
    'Subscribe-Event',
    'Unsubscribe-Event',
    'Emit-Event',
    'Process-AsyncEvents',
    'Get-EventHistory',
    'Get-EventInfo',
    'Get-RegisteredEvents',
    'Get-EventBusStats',
    'Clear-EventHistory'
)
