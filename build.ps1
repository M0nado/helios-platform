param(
    [string]$Phase = "all",
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
Write-Host "HELIOS build entrypoint" -ForegroundColor Cyan
Write-Host "Phase: $Phase"
Write-Host "Configuration: $Configuration"

if (Get-Command dotnet -ErrorAction SilentlyContinue) {
    dotnet restore HELIOS.Platform.sln
    dotnet build HELIOS.Platform.sln --configuration $Configuration --no-restore
} else {
    Write-Warning "dotnet SDK is not installed; skipping local build."
}
