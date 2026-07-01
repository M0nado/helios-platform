param(
    [Parameter(Mandatory=$true)][string]$ResourceGroupName,
    [string]$Location = "eastus"
)

az group create --name $ResourceGroupName --location $Location
pwsh infra/scripts/what-if.ps1 -ResourceGroupName $ResourceGroupName
