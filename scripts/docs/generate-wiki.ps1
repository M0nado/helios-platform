$config = "docs/wiki/wiki.config.yaml"
$output = "docs/wiki/generated"
New-Item -ItemType Directory -Force -Path $output | Out-Null
Copy-Item docs/status/current-status.md "$output/Current-Status.md" -Force
Copy-Item docs/architecture/MODULAR_ARCHITECTURE.md "$output/Modular-Architecture.md" -Force
Copy-Item docs/architecture/COMPONENT_MATRIX.md "$output/Component-Matrix.md" -Force
Write-Host "Generated wiki pages from $config into $output"
