#!/usr/bin/env python3
"""
X-Tier bootstrap runner (Python conversion of the Phase 1–3 pseudo bundle).

Default mode is dry-run for safety. Use --execute to run system-changing actions.
"""

from __future__ import annotations

import argparse
import json
import os
import subprocess
from datetime import datetime
from pathlib import Path
from typing import Iterable


class Bootstrap:
    def __init__(self, execute: bool, devdrive_root: Path) -> None:
        self.execute = execute
        self.devdrive_root = devdrive_root
        if self.execute:
            logs = self.devdrive_root / "logs"
        else:
            # Dry-run must work even when D:\DevDrive does not exist.
            logs = Path.cwd() / ".x-tier-logs"
        logs.mkdir(parents=True, exist_ok=True)
        self.log_file = logs / f"xtier_bootstrap_{datetime.now().strftime('%Y%m%d_%H%M%S')}.log"

    def log(self, message: str) -> None:
        line = f"[{datetime.now().isoformat(timespec='seconds')}] {message}"
        print(line)
        with self.log_file.open("a", encoding="utf-8") as log_stream:
            log_stream.write(line + "\n")

    def run(self, command: list[str], fatal: bool = True) -> None:
        cmd = " ".join(command)
        if not self.execute:
            self.log(f"DRY-RUN: {cmd}")
            return
        try:
            subprocess.run(command, check=True)
            self.log(f"OK: {cmd}")
        except Exception as exc:  # noqa: BLE001
            self.log(f"ERROR: {cmd} -> {exc}")
            if fatal:
                raise

    def mkdirs(self, paths: Iterable[Path]) -> None:
        for path in paths:
            if not self.execute:
                self.log(f"DRY-RUN: create directory {path}")
            else:
                path.mkdir(parents=True, exist_ok=True)
                self.log(f"Created: {path}")

    def write_file(self, path: Path, content: str) -> None:
        if not self.execute:
            self.log(f"DRY-RUN: write file {path}")
            return
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(content, encoding="utf-8")
        self.log(f"Wrote: {path}")

    def phase_1(self) -> None:
        self.log("=== PHASE 1: DevDrive layout, WSL2, Docker data-root ===")
        folders = [
            "ai-hub/models",
            "ai-hub/embeddings",
            "ai-hub/checkpoints",
            "ai-hub/venvs",
            "ai-hub/cuda",
            "hermes/nodes",
            "hermes/orchestrator",
            "hermes/configs",
            "hermes/logs",
            "data/raw",
            "data/processed",
            "data/features",
            "data/exports",
            "postgres/data",
            "qdrant/storage",
            "qdrant/config",
            "wsl2/distros",
            "wsl2/vhdx",
            "hyperv/vms",
            "hyperv/vhdx",
            "docker/data-root",
            "caches/pip",
            "caches/conda",
            "caches/npm",
            "caches/nuget",
            "caches/cargo",
            "caches/gradle",
            "caches/tmp",
            "caches/delivery",
            "sandbox",
            "repos",
            "scripts",
            "logs",
        ]
        self.mkdirs([self.devdrive_root / rel for rel in folders])

        env_vars = {
            "PIP_CACHE_DIR": str(self.devdrive_root / "caches/pip"),
            "CONDA_PKGS_DIRS": str(self.devdrive_root / "caches/conda/pkgs"),
            "npm_config_cache": str(self.devdrive_root / "caches/npm"),
            "NUGET_PACKAGES": str(self.devdrive_root / "caches/nuget"),
            "CARGO_HOME": str(self.devdrive_root / "caches/cargo"),
            "GRADLE_USER_HOME": str(self.devdrive_root / "caches/gradle"),
            "TEMP": str(self.devdrive_root / "caches/tmp"),
            "TMP": str(self.devdrive_root / "caches/tmp"),
        }
        for key, value in env_vars.items():
            if self.execute:
                os.environ[key] = value
            self.log(f"Set env target (machine/manual): {key}={value}")

        # WSL2 relocation plan commands (left as explicit command plan; run with --execute).
        self.run(["wsl", "--shutdown"], fatal=False)
        self.log("NOTE: distro export/import steps require per-distro review before execution.")

        wslconfig = (
            "[wsl2]\n"
            "memory=32GB\n"
            "processors=16\n"
            "swap=8GB\n"
            f"swapFile={str((self.devdrive_root / 'wsl2/vhdx/swap.vhdx')).replace('/', '\\\\')}\n"
            "localhostForwarding=true\n"
            "nestedVirtualization=true\n"
            "kernelCommandLine=quiet splash\n"
        )
        self.write_file(Path.home() / ".wslconfig", wslconfig)

        daemon_config = {"data-root": str(self.devdrive_root / "docker/data-root")}
        self.write_file(
            Path(r"C:\ProgramData\docker\config\daemon.json"),
            json.dumps(daemon_config, indent=2) + "\n",
        )

    def phase_2(self) -> None:
        self.log("=== PHASE 2: Core tools, SDKs, CUDA/Docker GPU checks ===")
        packages = [
            "Git.Git",
            "Microsoft.VisualStudioCode",
            "Microsoft.WindowsTerminal",
            "Python.Python.3.12",
            "OpenJS.NodeJS.LTS",
            "Rustlang.Rustup",
            "GoLang.Go",
            "Microsoft.AzureCLI",
            "GitHub.cli",
            "Docker.DockerDesktop",
            "Microsoft.PowerShell",
        ]
        for pkg in packages:
            self.run(
                [
                    "winget",
                    "install",
                    "--id",
                    pkg,
                    "--silent",
                    "--accept-package-agreements",
                    "--accept-source-agreements",
                ],
                fatal=False,
            )

        self.run(["nvidia-smi"], fatal=False)
        self.run(["cmd", "/c", "nvcc --version"], fatal=False)
        self.log("PSEUDO: validate WSL2 GPU via torch.cuda.is_available() inside distro.")

    def phase_3(self) -> None:
        self.log("=== PHASE 3: Hyper-V, Sandbox, Postgres, Qdrant, Hermes skeleton ===")
        self.run(
            [
                "powershell",
                "-NoProfile",
                "-Command",
                "Enable-WindowsOptionalFeature -Online -FeatureName Microsoft-Hyper-V-All -NoRestart",
            ],
            fatal=False,
        )
        self.run(
            [
                "powershell",
                "-NoProfile",
                "-Command",
                "Enable-WindowsOptionalFeature -Online -FeatureName Containers-DisposableClientVM -NoRestart",
            ],
            fatal=False,
        )

        qdrant_config = (
            "storage:\n"
            "  storage_path: /qdrant/storage\n"
            "service:\n"
            "  host: 127.0.0.1\n"
            "  http_port: 6333\n"
            "  grpc_port: 6334\n"
            "log_level: INFO\n"
        )
        self.write_file(self.devdrive_root / "qdrant/config/config.yaml", qdrant_config)

        orchestrator_py = (
            "import asyncio\n"
            "import logging\n"
            "from pathlib import Path\n\n"
            "logging.basicConfig(level=logging.INFO)\n"
            "logger = logging.getLogger('hermes.orchestrator')\n\n"
            "class HermesOrchestrator:\n"
            "    def __init__(self, config_path: str):\n"
            "        self.config_path = Path(config_path)\n"
            "        self.running = False\n\n"
            "    async def run_orchestration_loop(self):\n"
            "        self.running = True\n"
            "        while self.running:\n"
            "            await asyncio.sleep(1)\n\n"
            "if __name__ == '__main__':\n"
            "    orchestrator = HermesOrchestrator('D:/DevDrive/hermes/configs/hermes_global.yaml')\n"
            "    asyncio.run(orchestrator.run_orchestration_loop())\n"
        )
        self.write_file(self.devdrive_root / "hermes/orchestrator/orchestrator.py", orchestrator_py)

    def run_all(self) -> None:
        self.phase_1()
        self.phase_2()
        self.phase_3()
        self.log("X-Tier bootstrap conversion complete.")


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Python conversion of X-Tier Phase 1–3 bootstrap.")
    parser.add_argument("--execute", action="store_true", help="Actually execute commands (default is dry-run).")
    parser.add_argument(
        "--devdrive-root",
        default=r"D:\DevDrive",
        help="Target DevDrive root path (default: D:\\DevDrive).",
    )
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    bootstrap = Bootstrap(execute=args.execute, devdrive_root=Path(args.devdrive_root))
    bootstrap.run_all()
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
