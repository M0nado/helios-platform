# Helios One-File Autosetup
# This script generates the entire autosetup system automatically.

Write-Host "=== Helios Autosetup Starting ==="

# Create folders
$folders = @(
    "scripts",
    ".github/workflows",
    "autosetup",
    "autosetup/modules",
    "autosetup/fleet",
    "autosetup/xcore",
    "autosetup/hermes",
    "autosetup/os"
)

foreach ($f in $folders) {
    New-Item -ItemType Directory -Force -Path $f | Out-Null
}

# Create manifest
@"
repos:
  - M0nado/helios-platform
  - M0nado/hermes-core
  - M0nado/xcore-agent
  - M0nado/hermes-fleet-public
  - M0nado/hermes-fleet-private
  - M0nado/usb-wizard
  - M0nado/os-setup

branches:
  clean: hermes-clean
  merge: hermes-merge
"@ | Set-Content "autosetup/manifest.yaml"

# Create autosetup script
@"
#!/usr/bin/env bash
set -e

BASE=repos
mkdir -p "$BASE"

# Clone + update everything
while read repo; do
  gh repo clone "$repo" "$BASE/$repo" -- -q || true
  git -C "$BASE/$repo" fetch --all --prune
done < <(yq '.repos[]' autosetup/manifest.yaml)

# Normalize branches
while read repo; do
  git -C "$BASE/$repo" checkout -B hermes-clean
  git -C "$BASE/$repo" merge origin/main --strategy-option theirs || true
done < <(yq '.repos[]' autosetup/manifest.yaml)

# Glue generation hook
echo "=== Call Codex/ChatGPT connector here ==="

# Commit + push
while read repo; do
  git -C "$BASE/$repo" add .
  git -C "$BASE/$repo" commit -m "Hermes autosetup glue" || true
  git -C "$BASE/$repo" push origin hermes-clean || true
done < <(yq '.repos[]' autosetup/manifest.yaml)
"@ | Set-Content "scripts/autosetup.sh"

# Create workflow
@"
name: Hermes Autosetup

on:
  workflow_dispatch:

jobs:
  autosetup:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Install GitHub CLI + yq
        run: |
          sudo apt-get update
          sudo apt-get install -y yq
          curl -fsSL https://github.com/cli/cli/releases/latest/download/gh_2.54.0_linux_amd64.tar.gz \
            | tar xz --strip-components=1 -C /usr/local/bin

      - name: Run autosetup
        run: |
          chmod +x scripts/autosetup.sh
          ./scripts/autosetup.sh
"@ | Set-Content ".github/workflows/autosetup.yml"

Write-Host "=== Helios Autosetup Complete ==="
