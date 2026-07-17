[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$Root
)

$errors = [System.Collections.Generic.List[string]]::new()
Get-ChildItem -LiteralPath $Root -Recurse -File -Filter '*.ps1' |
    Where-Object { $_.FullName -notmatch '[\\/](?:\.git|artifacts|bin|obj)[\\/]' } |
    ForEach-Object {
        $tokens = $null
        $parseErrors = $null
        [void][System.Management.Automation.Language.Parser]::ParseFile(
            $_.FullName,
            [ref]$tokens,
            [ref]$parseErrors
        )
        foreach ($parseError in $parseErrors) {
            $errors.Add("$($_.FullName):$($parseError.Extent.StartLineNumber): $($parseError.Message)")
        }
    }

if ($errors.Count -gt 0) {
    $errors | ForEach-Object { Write-Error $_ }
    exit 1
}

Write-Host 'All PowerShell files parsed successfully.'
