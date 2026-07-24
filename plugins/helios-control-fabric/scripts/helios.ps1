param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]] $HeliosArguments
)

$ErrorActionPreference = "Stop"
$python = Get-Command python3 -ErrorAction SilentlyContinue
if (-not $python) {
    $python = Get-Command python -ErrorAction Stop
}

& $python.Source (Join-Path $PSScriptRoot "helios.py") @HeliosArguments
exit $LASTEXITCODE
