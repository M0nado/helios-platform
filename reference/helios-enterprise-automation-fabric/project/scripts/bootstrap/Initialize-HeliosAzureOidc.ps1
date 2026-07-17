[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string]$Repository,
    [Parameter(Mandatory)] [ValidateSet('dev','stage','prod','emergency')] [string]$Environment,
    [Parameter(Mandatory)] [string]$SubscriptionId,
    [string]$Location = 'centralus',
    [string]$BootstrapResourceGroup = 'rg-helios-identity-bootstrap',
    [switch]$Apply,
    [switch]$GrantDeploymentRoles,
    [string]$Confirm = ''
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
foreach ($command in @('az','gh')) { if (-not (Get-Command $command -ErrorAction SilentlyContinue)) { throw "$command is required." } }
$environmentName = "helios-$Environment"
$deploymentEnvironment = if ($Environment -eq 'emergency') { 'prod' } else { $Environment }
$targetResourceGroup = "rg-helios-fabric-$deploymentEnvironment"
$identityName = "uami-helios-github-$Environment"
$subject = "repo:$Repository:environment:$environmentName"

[pscustomobject]@{
    Repository = $Repository
    GitHubEnvironment = $environmentName
    AzureSubscription = $SubscriptionId
    IdentityName = $identityName
    FederatedSubject = $subject
    TargetResourceGroup = $targetResourceGroup
    GrantDeploymentRoles = [bool]$GrantDeploymentRoles
} | Format-List
if (-not $Apply) { Write-Host 'Preview only. No Azure or GitHub state was changed.'; exit 0 }
if ($Confirm -ne 'CREATE HELIOS AZURE OIDC') { throw 'Apply requires -Confirm "CREATE HELIOS AZURE OIDC".' }

& az account set --subscription $SubscriptionId
if ($LASTEXITCODE -ne 0) { throw 'Unable to select the Azure subscription.' }
& az group create --name $BootstrapResourceGroup --location $Location --tags 'helios:system=enterprise-automation-fabric' 'helios:purpose=identity-bootstrap' | Out-Null
& az group create --name $targetResourceGroup --location $Location --tags 'helios:system=enterprise-automation-fabric' "helios:environment=$deploymentEnvironment" | Out-Null
$identity = & az identity create --resource-group $BootstrapResourceGroup --name $identityName --location $Location --output json | ConvertFrom-Json
$issuer = 'https://token.actions.githubusercontent.com'
$credentialName = "github-$Environment"
$existing = & az identity federated-credential list --resource-group $BootstrapResourceGroup --identity-name $identityName --query "[?name=='$credentialName'] | [0]" --output json | ConvertFrom-Json
if (-not $existing) {
    & az identity federated-credential create --resource-group $BootstrapResourceGroup --identity-name $identityName --name $credentialName --issuer $issuer --subject $subject --audiences 'api://AzureADTokenExchange' | Out-Null
}

$identity.clientId | & gh secret set AZURE_CLIENT_ID --repo $Repository --env $environmentName --body-file -
$identity.tenantId | & gh secret set AZURE_TENANT_ID --repo $Repository --env $environmentName --body-file -
$SubscriptionId | & gh secret set AZURE_SUBSCRIPTION_ID --repo $Repository --env $environmentName --body-file -

if ($GrantDeploymentRoles) {
    if ($Confirm -ne 'CREATE HELIOS AZURE OIDC') { throw 'Confirmation mismatch.' }
    Write-Warning 'Granting resource-group-scoped deployment roles. No subscription-wide Contributor grant is created.'
    $scope = (& az group show --name $targetResourceGroup --query id --output tsv)
    if (-not $scope) { throw "Unable to resolve target resource group $targetResourceGroup." }
    & az role assignment create --assignee-object-id $identity.principalId --assignee-principal-type ServicePrincipal --role Contributor --scope $scope | Out-Null
    & az role assignment create --assignee-object-id $identity.principalId --assignee-principal-type ServicePrincipal --role 'Role Based Access Control Administrator' --scope $scope | Out-Null
}
Write-Host 'OIDC federation created and public identity identifiers stored in the selected GitHub environment.'
Write-Host 'No client secret was created.'
