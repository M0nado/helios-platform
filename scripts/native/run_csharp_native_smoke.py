#!/usr/bin/env python3
from __future__ import annotations

import os
import platform
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT))
from scripts.common.tool_resolver import path_with_repo_tools
BUILD_DIR = ROOT / ".build" / "native"
PROJECT = ROOT / "samples" / "native-interop" / "HELIOS.NativeInterop.Sample.csproj"


def native_env() -> dict[str, str]:
    env = os.environ.copy()
    env["PATH"] = path_with_repo_tools(env.get("PATH"))
    system = platform.system().lower()
    if system == "windows":
        key = "PATH"
    elif system == "darwin":
        key = "DYLD_LIBRARY_PATH"
    else:
        key = "LD_LIBRARY_PATH"
    current = env.get(key, "")
    env[key] = os.pathsep.join([str(BUILD_DIR), current]) if current else str(BUILD_DIR)
    return env


def run(command: list[str], env: dict[str, str]) -> None:
    print("+ " + " ".join(command), flush=True)
    subprocess.run(command, cwd=ROOT, env=env, check=True)


def main() -> int:
    env = native_env()
    run(["dotnet", "build", str(PROJECT)], env)
    run(["dotnet", "run", "--project", str(PROJECT), "--no-build"], env)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
