param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]] $HeliosArguments
)

$ErrorActionPreference = "Stop"
$python = Get-Command python3 -ErrorAction SilentlyContinue
$pythonArguments = @()
if (-not $python) {
    $python = Get-Command python -ErrorAction SilentlyContinue
}

if (-not $python) {
    $python = Get-Command py -ErrorAction SilentlyContinue
    if ($python) {
        $pythonArguments = @("-3")
    }
}

if (-not $python) {
    throw "Python 3 is required. Install python3 or python, or install the py launcher with a Python 3 runtime."
}

& $python.Source @pythonArguments (Join-Path $PSScriptRoot "helios.py") @HeliosArguments
exit $LASTEXITCODE
