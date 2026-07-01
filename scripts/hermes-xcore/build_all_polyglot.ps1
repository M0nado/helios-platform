#Requires -Version 7.0

param(
    [Parameter(Mandatory = $true)]
    [string]$SourcePath
)

$ErrorActionPreference = "Stop"
$PSNativeCommandUseErrorActionPreference = $true

Set-Location (Split-Path -Parent $PSScriptRoot | Split-Path -Parent)

python -m py_compile python\x-tier\polyglot\master_brief_compiler.py python\x-tier\polyglot\super_integrator.py python\x-tier\aihub_stack\winre_conversation_integrator.py
dotnet build src\PolyglotXTier\PolyglotXTier.csproj -nologo

$gpp = "C:\Users\thepa\tools\mingw64\bin\g++.exe"
if (Test-Path $gpp) {
    New-Item -ItemType Directory -Path artifacts\polyglot -Force | Out-Null
    & $gpp -std=c++17 -O2 -static -static-libstdc++ -static-libgcc cpp\x-tier\main.cpp cpp\x-tier\phase_runtime.cpp cpp\x-tier\secure_runtime_core.cpp -o artifacts\polyglot\xtier_brief_compiler.exe
}

python python\x-tier\polyglot\super_integrator.py --source $SourcePath --out-dir artifacts\polyglot

Write-Host "Polyglot build + integration complete."
