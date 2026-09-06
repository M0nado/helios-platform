$ErrorActionPreference = 'Stop'
$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$shimGenerator = Join-Path $repositoryRoot 'scripts/setup/New-Python3Shim.ps1'
$temporaryDirectory = Join-Path ([IO.Path]::GetTempPath()) "helios-python-shim-$([Guid]::NewGuid().ToString('N'))"

try {
    New-Item -ItemType Directory -Force -Path $temporaryDirectory | Out-Null
    $argumentLog = Join-Path $temporaryDirectory 'arguments.txt'
    $fakeLauncher = Join-Path $temporaryDirectory 'py.cmd'
    Set-Content -Encoding Ascii -Path $fakeLauncher -Value @(
        '@echo off'
        "echo %* > `"$argumentLog`""
    )

    $shim = Join-Path $temporaryDirectory 'tools/python/python3.cmd'
    & $shimGenerator -InterpreterPath $fakeLauncher -OutputPath $shim -UsePythonLauncher
    & $shim 'script.py' '--value' 'two words'
    $arguments = (Get-Content -Raw $argumentLog).Trim()
    if ($arguments -ne '-3 script.py --value "two words"') {
        throw "py launcher shim forwarded unexpected arguments: $arguments"
    }

    $fakePython = Join-Path $temporaryDirectory 'python.cmd'
    Set-Content -Encoding Ascii -Path $fakePython -Value @(
        '@echo off'
        "echo %* > `"$argumentLog`""
    )
    & $shimGenerator -InterpreterPath $fakePython -OutputPath $shim
    & $shim '-m' 'module_name'
    $arguments = (Get-Content -Raw $argumentLog).Trim()
    if ($arguments -ne '-m module_name') {
        throw "python executable shim forwarded unexpected arguments: $arguments"
    }

    $devScript = Get-Content -Raw (Join-Path $repositoryRoot 'scripts/setup/helios-dev.sh')
    $pathPosition = $devScript.IndexOf('$TOOLS_DIR/python')
    $invocationPosition = $devScript.IndexOf('python3 scripts/build_graph')
    if ($pathPosition -lt 0 -or $invocationPosition -lt 0 -or $pathPosition -gt $invocationPosition) {
        throw 'helios-dev.sh must add the Python shim directory before invoking python3.'
    }
}
finally {
    Remove-Item -Recurse -Force -ErrorAction SilentlyContinue $temporaryDirectory
}
