param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]] $HeliosArguments
)

$ErrorActionPreference = "Stop"
$scriptPath = Join-Path $PSScriptRoot "helios.py"

$python = Get-Command python3 -ErrorAction SilentlyContinue
if ($python) {
    & $python.Source $scriptPath @HeliosArguments
    exit $LASTEXITCODE
}

$python = Get-Command python -ErrorAction SilentlyContinue
if ($python) {
    & $python.Source $scriptPath @HeliosArguments
    exit $LASTEXITCODE
}

$pythonLauncher = Get-Command py -ErrorAction SilentlyContinue
if ($pythonLauncher) {
    & $pythonLauncher.Source -3 $scriptPath @HeliosArguments
    exit $LASTEXITCODE
}

throw 'Python 3 was not found. Install Python 3 or ensure python3, python, or py is on PATH.'
