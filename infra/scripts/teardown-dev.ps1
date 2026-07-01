param([Parameter(Mandatory=$true)][string]$ResourceGroupName)

Write-Host "Review resources in $ResourceGroupName before teardown."
az resource list --resource-group $ResourceGroupName --output table
