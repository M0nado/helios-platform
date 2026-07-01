param(
    [Parameter(Mandatory=$true)][string]$ResourceGroupName,
    [string]$TemplateFile = "infra/main.bicep",
    [string]$ParametersFile = "infra/parameters/dev.bicepparam"
)

az deployment group what-if --resource-group $ResourceGroupName --template-file $TemplateFile --parameters $ParametersFile
