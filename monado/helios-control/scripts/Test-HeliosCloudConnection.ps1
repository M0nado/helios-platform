#Requires -Version 7.0

<#
.SYNOPSIS
Performs read-only verification of a deployed Helios Azure connector.

.DESCRIPTION
Checks the anonymous health surface and verifies that the connector and MCP
routes reject unauthenticated requests. With -InteractiveAuth, the script uses
Azure CLI to obtain a user access token in memory and verifies the read-only
connector context, managed-identity resource inventory, OAuth metadata, and the
MCP initialize/initialized/tools lifecycle. Tokens and request headers are never
written to output.

.EXAMPLE
./scripts/Test-HeliosCloudConnection.ps1 `
  -ConnectorUrl https://helios.example.azurecontainerapps.io `
  -EntraClientId 00000000-0000-0000-0000-000000000000

.EXAMPLE
./scripts/Test-HeliosCloudConnection.ps1 `
  -ConnectorUrl https://helios.example.azurecontainerapps.io `
  -EntraClientId 00000000-0000-0000-0000-000000000000 `
  -TenantId 11111111-1111-1111-1111-111111111111 `
  -InteractiveAuth -Json
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [uri]$ConnectorUrl,

    [Parameter(Mandatory)]
    [guid]$EntraClientId,

    [string]$TenantId,

    [Alias('Authenticate', 'AcquireToken')]
    [switch]$InteractiveAuth,

    [switch]$Json,

    [ValidateRange(5, 300)]
    [int]$TimeoutSeconds = 30
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $false

$results = [System.Collections.Generic.List[object]]::new()
$accessToken = $null
$httpClient = $null

function Add-TestResult {
    param(
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][ValidateSet('passed', 'failed', 'skipped')][string]$Status,
        [AllowNull()][object]$StatusCode,
        [Parameter(Mandatory)][string]$Detail
    )

    $results.Add([pscustomobject][ordered]@{
        name       = $Name
        status     = $Status
        statusCode = $StatusCode
        detail     = $Detail
    })
}

function Get-SafeErrorDetail {
    param([Parameter(Mandatory)][System.Management.Automation.ErrorRecord]$ErrorRecord)

    # HTTP response bodies and authorization headers are intentionally excluded.
    $message = $ErrorRecord.Exception.Message
    if ([string]::IsNullOrWhiteSpace($message)) {
        return 'The operation failed without an error message.'
    }
    return $message
}

function Invoke-ConnectorRequest {
    param(
        [Parameter(Mandatory)][System.Net.Http.HttpMethod]$Method,
        [Parameter(Mandatory)][string]$Path,
        [AllowNull()][string]$Body,
        [AllowNull()][string]$BearerToken,
        [hashtable]$Headers = @{},
        [string[]]$Accept = @('application/json')
    )

    $requestUri = [uri]::new($script:baseUri, $Path.TrimStart('/'))
    $request = [System.Net.Http.HttpRequestMessage]::new($Method, $requestUri)
    $response = $null
    try {
        foreach ($mediaType in $Accept) {
            $request.Headers.Accept.ParseAdd($mediaType)
        }
        if (-not [string]::IsNullOrWhiteSpace($BearerToken)) {
            $request.Headers.Authorization = [System.Net.Http.Headers.AuthenticationHeaderValue]::new('Bearer', $BearerToken)
        }
        foreach ($entry in $Headers.GetEnumerator()) {
            [void]$request.Headers.TryAddWithoutValidation([string]$entry.Key, [string]$entry.Value)
        }
        if ($null -ne $Body) {
            $request.Content = [System.Net.Http.StringContent]::new($Body, [System.Text.Encoding]::UTF8, 'application/json')
        }

        $response = $httpClient.SendAsync($request).GetAwaiter().GetResult()
        $responseBody = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
        $responseHeaders = @{}
        foreach ($header in $response.Headers) {
            $responseHeaders[$header.Key] = $header.Value -join ','
        }
        foreach ($header in $response.Content.Headers) {
            $responseHeaders[$header.Key] = $header.Value -join ','
        }

        return [pscustomobject]@{
            StatusCode = [int]$response.StatusCode
            Body       = $responseBody
            Headers    = $responseHeaders
        }
    }
    finally {
        if ($null -ne $response) { $response.Dispose() }
        $request.Dispose()
    }
}

function ConvertFrom-McpResponse {
    param(
        [Parameter(Mandatory)][string]$Body,
        [Parameter(Mandatory)][int]$ExpectedId
    )

    $candidatePayloads = [System.Collections.Generic.List[string]]::new()
    $trimmed = $Body.Trim()
    if ($trimmed.StartsWith('{')) {
        $candidatePayloads.Add($trimmed)
    }
    else {
        # Streamable HTTP servers may return a JSON-RPC response as SSE data.
        foreach ($line in ($Body -split "`r?`n")) {
            if ($line.StartsWith('data:', [System.StringComparison]::OrdinalIgnoreCase)) {
                $candidate = $line.Substring(5).Trim()
                if ($candidate.StartsWith('{')) { $candidatePayloads.Add($candidate) }
            }
        }
    }

    foreach ($candidate in $candidatePayloads) {
        try {
            $message = $candidate | ConvertFrom-Json -AsHashtable -Depth 50
            if ($message.ContainsKey('id') -and [int]$message.id -eq $ExpectedId) {
                return $message
            }
        }
        catch {
            continue
        }
    }
    throw "The MCP response did not contain valid JSON-RPC for id $ExpectedId."
}

function Get-AzureCliAccessToken {
    param(
        [Parameter(Mandatory)][guid]$ClientId,
        [AllowEmptyString()][string]$RequestedTenant
    )

    $az = Get-Command az -ErrorAction SilentlyContinue
    if ($null -eq $az) {
        throw 'Azure CLI (az) was not found on PATH.'
    }

    & $az.Source account show --output none --only-show-errors 2>$null
    if ($LASTEXITCODE -ne 0) {
        $loginArguments = @('login', '--use-device-code', '--output', 'none', '--only-show-errors')
        if (-not [string]::IsNullOrWhiteSpace($RequestedTenant)) {
            $loginArguments += @('--tenant', $RequestedTenant)
        }
        & $az.Source @loginArguments
        if ($LASTEXITCODE -ne 0) {
            throw 'Azure CLI interactive sign-in failed.'
        }
    }

    $tenantArguments = @()
    if (-not [string]::IsNullOrWhiteSpace($RequestedTenant)) {
        $tenantArguments = @('--tenant', $RequestedTenant)
    }

    $scope = "api://$ClientId/user_impersonation"
    $tokenArguments = @(
        'account', 'get-access-token',
        '--scope', $scope,
        '--query', 'accessToken',
        '--output', 'tsv',
        '--only-show-errors'
    ) + $tenantArguments
    $tokenOutput = & $az.Source @tokenArguments 2>$null

    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace(($tokenOutput -join ''))) {
        # Older Azure CLI/MSAL combinations use the application ID URI as a resource.
        $resource = "api://$ClientId"
        $tokenArguments = @(
            'account', 'get-access-token',
            '--resource', $resource,
            '--query', 'accessToken',
            '--output', 'tsv',
            '--only-show-errors'
        ) + $tenantArguments
        $tokenOutput = & $az.Source @tokenArguments 2>$null
    }

    if ($LASTEXITCODE -ne 0) {
        throw 'Azure CLI could not acquire a token for the connector application.'
    }

    $token = ($tokenOutput -join '').Trim()
    if ([string]::IsNullOrWhiteSpace($token)) {
        throw 'Azure CLI returned an empty access token.'
    }
    return $token
}

try {
    if (-not $ConnectorUrl.IsAbsoluteUri -or $ConnectorUrl.Scheme -ne 'https') {
        throw 'ConnectorUrl must be an absolute HTTPS URL.'
    }
    if (-not [string]::IsNullOrWhiteSpace($ConnectorUrl.Query) -or -not [string]::IsNullOrWhiteSpace($ConnectorUrl.Fragment)) {
        throw 'ConnectorUrl must not include a query string or fragment.'
    }

    $script:baseUri = [uri]($ConnectorUrl.AbsoluteUri.TrimEnd('/') + '/')
    Add-TestResult -Name 'transport.https' -Status passed -StatusCode $null -Detail 'Connector base URL uses HTTPS.'

    $handler = [System.Net.Http.HttpClientHandler]::new()
    $handler.AllowAutoRedirect = $false
    $httpClient = [System.Net.Http.HttpClient]::new($handler, $true)
    $httpClient.Timeout = [timespan]::FromSeconds($TimeoutSeconds)

    foreach ($endpoint in @('/health', '/health/live', '/health/ready')) {
        try {
            $response = Invoke-ConnectorRequest -Method ([System.Net.Http.HttpMethod]::Get) -Path $endpoint -Body $null -BearerToken $null
            $passed = $response.StatusCode -eq 200
            Add-TestResult -Name "anonymous$endpoint" -Status $(if ($passed) { 'passed' } else { 'failed' }) -StatusCode $response.StatusCode -Detail $(if ($passed) { 'Anonymous health endpoint is available.' } else { 'Expected HTTP 200.' })
        }
        catch {
            Add-TestResult -Name "anonymous$endpoint" -Status failed -StatusCode $null -Detail (Get-SafeErrorDetail $_)
        }
    }

    try {
        $response = Invoke-ConnectorRequest -Method ([System.Net.Http.HttpMethod]::Get) -Path '/.well-known/oauth-protected-resource/mcp' -Body $null -BearerToken $null
        $validMetadata = $false
        if ($response.StatusCode -eq 200) {
            try {
                $metadata = $response.Body | ConvertFrom-Json -AsHashtable -Depth 20
                $expectedResource = $script:baseUri.AbsoluteUri.TrimEnd('/') + '/mcp'
                $expectedScope = "api://$EntraClientId/user_impersonation"
                $authorizationServers = @($metadata.authorization_servers)
                $validMetadata =
                    $metadata.resource -eq $expectedResource -and
                    @($metadata.scopes_supported) -contains $expectedScope -and
                    $authorizationServers.Count -ge 1 -and
                    @($authorizationServers | Where-Object { $_ -match '^https://login\.microsoftonline\.com/[^/]+/v2\.0$' }).Count -ge 1
            }
            catch { $validMetadata = $false }
        }
        $passed = $response.StatusCode -eq 200 -and $validMetadata
        Add-TestResult -Name 'oauth.protected-resource-metadata' -Status $(if ($passed) { 'passed' } else { 'failed' }) -StatusCode $response.StatusCode -Detail $(if ($passed) { 'RFC 9728 metadata advertises the exact MCP resource, Entra issuer, and delegated scope.' } else { 'Expected public RFC 9728 metadata for the exact connector MCP URL and user_impersonation scope.' })
    }
    catch {
        Add-TestResult -Name 'oauth.protected-resource-metadata' -Status failed -StatusCode $null -Detail (Get-SafeErrorDetail $_)
    }

    try {
        $response = Invoke-ConnectorRequest -Method ([System.Net.Http.HttpMethod]::Get) -Path '/connector/context' -Body $null -BearerToken $null
        $passed = $response.StatusCode -eq 401
        Add-TestResult -Name 'anonymous.connector-context-denied' -Status $(if ($passed) { 'passed' } else { 'failed' }) -StatusCode $response.StatusCode -Detail $(if ($passed) { 'Protected connector context rejects anonymous requests.' } else { 'Expected HTTP 401 with redirects disabled.' })
    }
    catch {
        Add-TestResult -Name 'anonymous.connector-context-denied' -Status failed -StatusCode $null -Detail (Get-SafeErrorDetail $_)
    }

    try {
        $forgedHeaders = @{ 'X-MS-CLIENT-PRINCIPAL-ID' = 'forged-untrusted-principal' }
        $response = Invoke-ConnectorRequest -Method ([System.Net.Http.HttpMethod]::Get) -Path '/connector/context' -Body $null -BearerToken $null -Headers $forgedHeaders
        $passed = $response.StatusCode -eq 401
        Add-TestResult -Name 'anonymous.forged-easy-auth-header-denied' -Status $(if ($passed) { 'passed' } else { 'failed' }) -StatusCode $response.StatusCode -Detail $(if ($passed) { 'The Azure ingress rejected a forged Easy Auth principal header without a bearer token.' } else { 'Expected HTTP 401; a client-supplied X-MS-CLIENT-PRINCIPAL-ID must never cross the Easy Auth trust boundary.' })
    }
    catch {
        Add-TestResult -Name 'anonymous.forged-easy-auth-header-denied' -Status failed -StatusCode $null -Detail (Get-SafeErrorDetail $_)
    }

    try {
        $anonymousMcpBody = @{ jsonrpc = '2.0'; id = 900; method = 'tools/list'; params = @{} } | ConvertTo-Json -Compress -Depth 10
        $response = Invoke-ConnectorRequest -Method ([System.Net.Http.HttpMethod]::Post) -Path '/mcp' -Body $anonymousMcpBody -BearerToken $null -Accept @('application/json', 'text/event-stream')
        $challenge = if ($response.Headers.ContainsKey('WWW-Authenticate')) { [string]$response.Headers['WWW-Authenticate'] } else { '' }
        $passed = $response.StatusCode -eq 401 -and $challenge -match 'resource_metadata='
        Add-TestResult -Name 'anonymous.mcp-denied' -Status $(if ($passed) { 'passed' } else { 'failed' }) -StatusCode $response.StatusCode -Detail $(if ($passed) { 'Protected MCP endpoint rejects anonymous requests and advertises OAuth resource metadata.' } else { 'Expected HTTP 401 with an RFC 9728 resource_metadata challenge and redirects disabled.' })
    }
    catch {
        Add-TestResult -Name 'anonymous.mcp-denied' -Status failed -StatusCode $null -Detail (Get-SafeErrorDetail $_)
    }

    if ($InteractiveAuth) {
        try {
            $accessToken = Get-AzureCliAccessToken -ClientId $EntraClientId -RequestedTenant $TenantId
            Add-TestResult -Name 'azure-cli.access-token' -Status passed -StatusCode $null -Detail 'Azure CLI acquired a connector token in memory.'
        }
        catch {
            Add-TestResult -Name 'azure-cli.access-token' -Status failed -StatusCode $null -Detail (Get-SafeErrorDetail $_)
        }

        if (-not [string]::IsNullOrWhiteSpace($accessToken)) {
            try {
                $response = Invoke-ConnectorRequest -Method ([System.Net.Http.HttpMethod]::Get) -Path '/connector/context' -Body $null -BearerToken $accessToken
                $validJson = $false
                $readOnlyContext = $false
                if ($response.StatusCode -eq 200) {
                    try {
                        $contextPayload = $response.Body | ConvertFrom-Json -AsHashtable -Depth 20
                        $validJson = $true
                        $readOnlyContext = $contextPayload.ContainsKey('access') -and $contextPayload.access -eq 'read-only-resource-group'
                    }
                    catch { $validJson = $false }
                }
                $passed = $response.StatusCode -eq 200 -and $validJson -and $readOnlyContext
                Add-TestResult -Name 'authenticated.connector-context' -Status $(if ($passed) { 'passed' } else { 'failed' }) -StatusCode $response.StatusCode -Detail $(if ($passed) { 'Authenticated context reports read-only-resource-group access.' } else { 'Expected HTTP 200 JSON with read-only-resource-group access.' })
            }
            catch {
                Add-TestResult -Name 'authenticated.connector-context' -Status failed -StatusCode $null -Detail (Get-SafeErrorDetail $_)
            }

            try {
                $response = Invoke-ConnectorRequest -Method ([System.Net.Http.HttpMethod]::Get) -Path '/connector/resources' -Body $null -BearerToken $accessToken
                $validResourceList = $false
                if ($response.StatusCode -eq 200) {
                    try {
                        $resourceDocument = [System.Text.Json.JsonDocument]::Parse($response.Body)
                        try {
                            $validResourceList = $resourceDocument.RootElement.ValueKind -eq [System.Text.Json.JsonValueKind]::Array
                        }
                        finally {
                            $resourceDocument.Dispose()
                        }
                    }
                    catch { $validResourceList = $false }
                }
                $passed = $response.StatusCode -eq 200 -and $validResourceList
                Add-TestResult -Name 'authenticated.connector-resources' -Status $(if ($passed) { 'passed' } else { 'failed' }) -StatusCode $response.StatusCode -Detail $(if ($passed) { 'REST resource inventory succeeded through the connector managed identity.' } else { 'Expected a JSON array from the read-only Azure resource inventory; check managed identity and Reader RBAC.' })
            }
            catch {
                Add-TestResult -Name 'authenticated.connector-resources' -Status failed -StatusCode $null -Detail (Get-SafeErrorDetail $_)
            }

            try {
                $originProbeBody = @{ jsonrpc = '2.0'; id = 999; method = 'tools/list'; params = @{} } | ConvertTo-Json -Compress -Depth 10
                $originProbeHeaders = @{
                    'Origin' = 'https://dns-rebinding-probe.invalid'
                    'MCP-Protocol-Version' = '2025-06-18'
                }
                $response = Invoke-ConnectorRequest -Method ([System.Net.Http.HttpMethod]::Post) -Path '/mcp' -Body $originProbeBody -BearerToken $accessToken -Headers $originProbeHeaders -Accept @('application/json', 'text/event-stream')
                $passed = $response.StatusCode -eq 403
                Add-TestResult -Name 'authenticated.mcp-origin-rejected' -Status $(if ($passed) { 'passed' } else { 'failed' }) -StatusCode $response.StatusCode -Detail $(if ($passed) { 'MCP rejects an authenticated request with an unapproved Origin.' } else { 'Expected HTTP 403 for an unapproved authenticated Origin.' })
            }
            catch {
                Add-TestResult -Name 'authenticated.mcp-origin-rejected' -Status failed -StatusCode $null -Detail (Get-SafeErrorDetail $_)
            }

            $mcpSessionId = $null
            $negotiatedProtocolVersion = '2025-06-18'
            $mcpInitializePassed = $false
            $mcpInitializedPassed = $false
            try {
                $initializeBody = @{
                    jsonrpc = '2.0'
                    id = 1001
                    method = 'initialize'
                    params = @{
                        protocolVersion = '2025-06-18'
                        capabilities = @{}
                        clientInfo = @{ name = 'helios-cloud-verifier'; version = '1.0.0' }
                    }
                } | ConvertTo-Json -Compress -Depth 20
                $response = Invoke-ConnectorRequest -Method ([System.Net.Http.HttpMethod]::Post) -Path '/mcp' -Body $initializeBody -BearerToken $accessToken -Accept @('application/json', 'text/event-stream')
                $message = if ($response.StatusCode -eq 200) { ConvertFrom-McpResponse -Body $response.Body -ExpectedId 1001 } else { $null }
                $passed = $response.StatusCode -eq 200 -and $null -ne $message -and $message.jsonrpc -eq '2.0' -and $message.ContainsKey('result') -and -not $message.ContainsKey('error')
                if ($passed -and $message.result.ContainsKey('protocolVersion')) {
                    $negotiatedProtocolVersion = [string]$message.result.protocolVersion
                }
                if ($response.Headers.ContainsKey('Mcp-Session-Id')) {
                    $mcpSessionId = [string]$response.Headers['Mcp-Session-Id']
                }
                $mcpInitializePassed = $passed -and ($negotiatedProtocolVersion -in @('2025-03-26', '2025-06-18', '2025-11-25'))
                Add-TestResult -Name 'authenticated.mcp-initialize' -Status $(if ($mcpInitializePassed) { 'passed' } else { 'failed' }) -StatusCode $response.StatusCode -Detail $(if ($mcpInitializePassed) { 'MCP initialize returned valid JSON-RPC and a supported protocol version.' } else { 'Expected a successful JSON-RPC initialize result with a supported protocol version.' })
            }
            catch {
                Add-TestResult -Name 'authenticated.mcp-initialize' -Status failed -StatusCode $null -Detail (Get-SafeErrorDetail $_)
            }

            if ($mcpInitializePassed) {
                try {
                    $requestHeaders = @{ 'MCP-Protocol-Version' = $negotiatedProtocolVersion }
                    if (-not [string]::IsNullOrWhiteSpace($mcpSessionId)) {
                        $requestHeaders['Mcp-Session-Id'] = $mcpSessionId
                    }
                    $initializedBody = @{ jsonrpc = '2.0'; method = 'notifications/initialized'; params = @{} } | ConvertTo-Json -Compress -Depth 10
                    $response = Invoke-ConnectorRequest -Method ([System.Net.Http.HttpMethod]::Post) -Path '/mcp' -Body $initializedBody -BearerToken $accessToken -Headers $requestHeaders -Accept @('application/json', 'text/event-stream')
                    $mcpInitializedPassed = $response.StatusCode -eq 202 -and [string]::IsNullOrWhiteSpace($response.Body)
                    Add-TestResult -Name 'authenticated.mcp-initialized-notification' -Status $(if ($mcpInitializedPassed) { 'passed' } else { 'failed' }) -StatusCode $response.StatusCode -Detail $(if ($mcpInitializedPassed) { 'MCP initialized notification received an empty HTTP 202 acknowledgement.' } else { 'Expected notifications/initialized to return HTTP 202 with no response body.' })
                }
                catch {
                    Add-TestResult -Name 'authenticated.mcp-initialized-notification' -Status failed -StatusCode $null -Detail (Get-SafeErrorDetail $_)
                }
            }
            else {
                Add-TestResult -Name 'authenticated.mcp-initialized-notification' -Status skipped -StatusCode $null -Detail 'Skipped because MCP initialize failed.'
            }

            if ($mcpInitializedPassed) {
                try {
                    $requestHeaders = @{ 'MCP-Protocol-Version' = $negotiatedProtocolVersion }
                    if (-not [string]::IsNullOrWhiteSpace($mcpSessionId)) {
                        $requestHeaders['Mcp-Session-Id'] = $mcpSessionId
                    }
                    $toolsBody = @{ jsonrpc = '2.0'; id = 1002; method = 'tools/list'; params = @{} } | ConvertTo-Json -Compress -Depth 10
                    $response = Invoke-ConnectorRequest -Method ([System.Net.Http.HttpMethod]::Post) -Path '/mcp' -Body $toolsBody -BearerToken $accessToken -Headers $requestHeaders -Accept @('application/json', 'text/event-stream')
                    $message = if ($response.StatusCode -eq 200) { ConvertFrom-McpResponse -Body $response.Body -ExpectedId 1002 } else { $null }
                    $tools = if ($null -ne $message -and $message.ContainsKey('result') -and $message.result.ContainsKey('tools')) { @($message.result.tools) } else { @() }
                    $toolNames = @($tools | ForEach-Object { [string]$_.name })
                    $expectedToolNames = @('azure_get_context', 'azure_list_resources', 'azure_list_foundry_resources')
                    $toolDifference = @(Compare-Object -ReferenceObject $expectedToolNames -DifferenceObject $toolNames)
                    $passed = $response.StatusCode -eq 200 -and $null -ne $message -and $message.jsonrpc -eq '2.0' -and -not $message.ContainsKey('error') -and $toolDifference.Count -eq 0
                    Add-TestResult -Name 'authenticated.mcp-tools-list' -Status $(if ($passed) { 'passed' } else { 'failed' }) -StatusCode $response.StatusCode -Detail $(if ($passed) { 'MCP exposes exactly the three approved read-only Azure inventory tools.' } else { 'MCP tool inventory differs from the approved read-only set.' })
                }
                catch {
                    Add-TestResult -Name 'authenticated.mcp-tools-list' -Status failed -StatusCode $null -Detail (Get-SafeErrorDetail $_)
                }
            }
            else {
                Add-TestResult -Name 'authenticated.mcp-tools-list' -Status skipped -StatusCode $null -Detail 'Skipped because the MCP initialization lifecycle did not complete.'
            }


            if ($mcpInitializedPassed) {
                try {
                    $requestHeaders = @{ 'MCP-Protocol-Version' = $negotiatedProtocolVersion }
                    if (-not [string]::IsNullOrWhiteSpace($mcpSessionId)) {
                        $requestHeaders['Mcp-Session-Id'] = $mcpSessionId
                    }
                    $toolCallBody = @{
                        jsonrpc = '2.0'
                        id = 1003
                        method = 'tools/call'
                        params = @{
                            name = 'azure_list_resources'
                            arguments = @{}
                        }
                    } | ConvertTo-Json -Compress -Depth 20
                    $response = Invoke-ConnectorRequest -Method ([System.Net.Http.HttpMethod]::Post) -Path '/mcp' -Body $toolCallBody -BearerToken $accessToken -Headers $requestHeaders -Accept @('application/json', 'text/event-stream')
                    $message = if ($response.StatusCode -eq 200) { ConvertFrom-McpResponse -Body $response.Body -ExpectedId 1003 } else { $null }
                    $passed =
                        $response.StatusCode -eq 200 -and
                        $null -ne $message -and
                        $message.ContainsKey('result') -and
                        $message.result.ContainsKey('isError') -and
                        -not [bool]$message.result.isError -and
                        @($message.result.content).Count -ge 1
                    Add-TestResult -Name 'authenticated.mcp-tools-call-resources' -Status $(if ($passed) { 'passed' } else { 'failed' }) -StatusCode $response.StatusCode -Detail $(if ($passed) { 'MCP azure_list_resources completed through the managed identity and Reader RBAC path.' } else { 'Expected a successful read-only MCP tool result; check managed identity and Reader RBAC.' })
                }
                catch {
                    Add-TestResult -Name 'authenticated.mcp-tools-call-resources' -Status failed -StatusCode $null -Detail (Get-SafeErrorDetail $_)
                }
            }
            else {
                Add-TestResult -Name 'authenticated.mcp-tools-call-resources' -Status skipped -StatusCode $null -Detail 'Skipped because the MCP initialization lifecycle did not complete.'
            }
        }
        else {
            foreach ($name in @('authenticated.connector-context', 'authenticated.connector-resources', 'authenticated.mcp-origin-rejected', 'authenticated.mcp-initialize', 'authenticated.mcp-initialized-notification', 'authenticated.mcp-tools-list', 'authenticated.mcp-tools-call-resources')) {
                Add-TestResult -Name $name -Status skipped -StatusCode $null -Detail 'Skipped because Azure CLI token acquisition failed.'
            }
        }
    }
    else {
        Add-TestResult -Name 'azure-cli.access-token' -Status skipped -StatusCode $null -Detail 'Use -InteractiveAuth to run authenticated checks.'
        foreach ($name in @('authenticated.connector-context', 'authenticated.connector-resources', 'authenticated.mcp-origin-rejected', 'authenticated.mcp-initialize', 'authenticated.mcp-initialized-notification', 'authenticated.mcp-tools-list', 'authenticated.mcp-tools-call-resources')) {
            Add-TestResult -Name $name -Status skipped -StatusCode $null -Detail 'Authenticated verification was not requested.'
        }
    }
}
catch {
    Add-TestResult -Name 'configuration' -Status failed -StatusCode $null -Detail (Get-SafeErrorDetail $_)
}
finally {
    # Drop references to credentials before producing any output.
    $accessToken = $null
    if ($null -ne $httpClient) { $httpClient.Dispose() }
}

$failedCount = @($results | Where-Object status -eq 'failed').Count
$summary = [pscustomobject][ordered]@{
    schemaVersion          = '1.0'
    checkedAtUtc           = [DateTimeOffset]::UtcNow.ToString('o')
    connectorUrl           = $ConnectorUrl.GetLeftPart([System.UriPartial]::Path).TrimEnd('/')
    authenticatedRequested = [bool]$InteractiveAuth
    passed                 = $failedCount -eq 0
    failedCount            = $failedCount
    tests                  = @($results)
}

if ($Json) {
    $summary | ConvertTo-Json -Depth 20
}
else {
    $results | Format-Table -Property @('name', 'status', 'statusCode', 'detail') -AutoSize
    if ($summary.passed) {
        Write-Host 'Helios cloud connection verification passed.' -ForegroundColor Green
    }
    else {
        Write-Error "Helios cloud connection verification failed ($failedCount check(s))." -ErrorAction Continue
    }
}

if (-not $summary.passed) {
    exit 1
}
