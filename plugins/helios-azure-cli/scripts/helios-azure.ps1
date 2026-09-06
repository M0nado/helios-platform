$ErrorActionPreference = "Stop"
$Python = Get-Command python3 -ErrorAction SilentlyContinue
if (-not $Python) {
    $Python = Get-Command python -ErrorAction SilentlyContinue
}
if (-not $Python) {
    throw "Python 3 is required to run the HELIOS Azure CLI plugin."
}
& $Python.Source "$PSScriptRoot/helios_azure.py" @args
exit $LASTEXITCODE
