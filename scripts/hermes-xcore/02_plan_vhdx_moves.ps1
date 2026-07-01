#Requires -Version 7.0
# Planning-only script for WSL2/Hyper-V VHDX relocation (no execution).

$ErrorActionPreference = "Stop"

$plan = @(
    "1) Run: wsl --shutdown",
    "2) Export distros: wsl --export <distro> D:\DevDrive\wsl2\<distro>.tar",
    "3) Unregister/import distros to D:\DevDrive\wsl2\distros\<distro>",
    "4) Set .wslconfig swapFile=D:\\DevDrive\\wsl2\\vhdx\\swap.vhdx",
    "5) Move Hyper-V VM storage to D:\DevDrive\hyperv\vms and D:\DevDrive\hyperv\vhdx",
    "6) Verify integrity and keep rollback backups"
)

$plan | ForEach-Object { Write-Host $_ }
Write-Host "Planning output only (no changes applied)."

