#requires -Version 7.0
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$package = Join-Path $root 'appPackage'

foreach ($name in @('color', 'outline')) {
    $source = Join-Path $package "$name.png.b64"
    $target = Join-Path $package "$name.png"
    $encoded = (Get-Content -LiteralPath $source -Raw).Trim()
    [IO.File]::WriteAllBytes($target, [Convert]::FromBase64String($encoded))
}

function Get-PngDimensions([string]$Path) {
    $bytes = [IO.File]::ReadAllBytes($Path)
    $signature = [byte[]](137, 80, 78, 71, 13, 10, 26, 10)
    $validSignature = $bytes.Length -ge 24
    for ($index = 0; $validSignature -and $index -lt $signature.Length; $index++) {
        $validSignature = $bytes[$index] -eq $signature[$index]
    }
    if (-not $validSignature) {
        throw "$Path is not a valid PNG file."
    }
    $width = ($bytes[16] -shl 24) -bor ($bytes[17] -shl 16) -bor ($bytes[18] -shl 8) -bor $bytes[19]
    $height = ($bytes[20] -shl 24) -bor ($bytes[21] -shl 16) -bor ($bytes[22] -shl 8) -bor $bytes[23]
    [pscustomobject]@{ Width = $width; Height = $height }
}

$color = Get-PngDimensions (Join-Path $package 'color.png')
$outline = Get-PngDimensions (Join-Path $package 'outline.png')
if ($color.Width -ne 192 -or $color.Height -ne 192) { throw 'The color icon must be 192x192.' }
if ($outline.Width -ne 32 -or $outline.Height -ne 32) { throw 'The outline icon must be 32x32.' }

Write-Host 'Materialized and verified the Teams package icons.'
