using System;
using System.Diagnostics;
using System.IO;

namespace Helios.Autosetup
{
    public static class HeliosAutosetup
    {
        private const string ManifestPath = "autosetup/manifest.yaml";
        private const string ScriptsDir = "scripts";
        private const string WorkflowsDir = ".github/workflows";

        public static void Main(string[] args)
        {
            Console.WriteLine("=== Helios C# Autosetup Orchestrator ===");

            CreateFolders();
            CreateManifest();
            CreateAutosetupScript();
            CreateWorkflow();

            Console.WriteLine("=== Files created. You can now run scripts/autosetup.sh or the GitHub workflow. ===");
        }

        private static void CreateFolders()
        {
            Directory.CreateDirectory("autosetup");
            Directory.CreateDirectory(ScriptsDir);
            Directory.CreateDirectory(WorkflowsDir);
        }

        private static void CreateManifest()
        {
            var manifest = """
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
""";
            File.WriteAllText(ManifestPath, manifest);
        }

        private static void CreateAutosetupScript()
        {
            var script = """
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

echo "=== Helios: now point ChatGPT/Codex at repos/ with the mega-prompt to generate glue and merges. ==="

echo "Autosetup run completed" > autosetup/summary.md
""";
            File.WriteAllText(Path.Combine(ScriptsDir, "autosetup.sh"), script);
        }

        private static void CreateWorkflow()
        {
            var workflow = """
name: Helios Hermes Autosetup

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
          sudo apt-get install -y yq curl
          curl -fsSL https://github.com/cli/cli/releases/latest/download/gh_2.54.0_linux_amd64.tar.gz \
            | tar xz --strip-components=1 -C /usr/local/bin

      - name: Run autosetup
        run: |
          chmod +x scripts/autosetup.sh
          ./scripts/autosetup.sh
""";
            File.WriteAllText(Path.Combine(WorkflowsDir, "autosetup.yml"), workflow);
        }
    }
}
