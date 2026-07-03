#!/usr/bin/env python3
"""Build and run the C# native interop smoke sample with native paths set."""
from __future__ import annotations

import os
import platform
import subprocess
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
NATIVE_BUILD_DIR = ROOT / ".build" / "native"
PROJECT = ROOT / "samples" / "native-interop" / "HELIOS.NativeInterop.Sample.csproj"


def prepend_env_path(env: dict[str, str], name: str, path: Path) -> None:
    """Prepend a directory to a platform-specific path-like environment variable."""
    current = env.get(name)
    value = str(path)
    env[name] = value if not current else os.pathsep.join([value, current])


def configure_native_search_path(env: dict[str, str]) -> None:
    """Configure the native library search path for the current operating system."""
    system = platform.system()
    if system == "Windows":
        prepend_env_path(env, "PATH", NATIVE_BUILD_DIR)
    elif system == "Linux":
        prepend_env_path(env, "LD_LIBRARY_PATH", NATIVE_BUILD_DIR)
    elif system == "Darwin":
        prepend_env_path(env, "DYLD_LIBRARY_PATH", NATIVE_BUILD_DIR)
    else:
        raise SystemExit(f"Unsupported platform for native smoke test: {system}")


def run(command: list[str], env: dict[str, str]) -> None:
    print(f"$ {' '.join(command)}")
    subprocess.run(command, cwd=ROOT, env=env, check=True)


def main() -> None:
    env = os.environ.copy()
    configure_native_search_path(env)
    run(["dotnet", "build", str(PROJECT.relative_to(ROOT))], env)
    run(["dotnet", "run", "--project", str(PROJECT.relative_to(ROOT)), "--no-build"], env)


if __name__ == "__main__":
    main()
