#!/usr/bin/env python3
"""Optional ctypes smoke test for HELIOS Native/XCore ABI."""
from __future__ import annotations

import argparse
import ctypes
import platform
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
BUILD_DIR = ROOT / ".build" / "native"


def candidate_libraries() -> list[Path]:
    system = platform.system().lower()
    names = {
        "windows": ["HELIOSNativePerformance.dll"],
        "darwin": ["libHELIOSNativePerformance.dylib"],
    }.get(system, ["libHELIOSNativePerformance.so"])
    candidates = [BUILD_DIR / name for name in names]
    for name in names:
        candidates.extend(BUILD_DIR.rglob(name) if BUILD_DIR.exists() else [])
    return candidates


def discover_library(override: str | None) -> Path:
    if override:
        path = Path(override)
        if path.exists():
            return path
        raise FileNotFoundError(f"Native library not found: {path}")
    for candidate in candidate_libraries():
        if candidate.exists():
            return candidate
    raise FileNotFoundError(
        "Native library not found. Run: cmake -S src/native/HELIOS.Native.Performance "
        "-B .build/native && cmake --build .build/native"
    )


def main() -> int:
    parser = argparse.ArgumentParser(description="Run a ctypes smoke test against the HELIOS native library.")
    parser.add_argument("library", nargs="?", help="Path to built HELIOSNativePerformance shared library.")
    args = parser.parse_args()

    library_path = discover_library(args.library)
    library = ctypes.CDLL(str(library_path))
    library.helios_native_abi_version.restype = ctypes.c_int
    library.helios_vector_sum.argtypes = [ctypes.POINTER(ctypes.c_double), ctypes.c_size_t]
    library.helios_vector_sum.restype = ctypes.c_double
    library.helios_vector_mean.argtypes = [ctypes.POINTER(ctypes.c_double), ctypes.c_size_t]
    library.helios_vector_mean.restype = ctypes.c_double

    values = (ctypes.c_double * 4)(1.0, 2.0, 3.0, 4.0)
    print(f"library={library_path}")
    print(f"abi={library.helios_native_abi_version()}")
    print(f"sum={library.helios_vector_sum(values, 4)}")
    print(f"mean={library.helios_vector_mean(values, 4)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
