#!/usr/bin/env python3
"""Optional ctypes smoke test for HELIOS Native/XCore ABI."""
from __future__ import annotations

import argparse
import ctypes
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
DEFAULT_LIBRARY = ROOT / ".build" / "native" / "libHELIOSNativePerformance.so"


def main() -> int:
    parser = argparse.ArgumentParser(description="Run a ctypes smoke test against the HELIOS native library.")
    parser.add_argument("library", nargs="?", default=str(DEFAULT_LIBRARY), help="Path to built HELIOSNativePerformance shared library.")
    args = parser.parse_args()

    library = ctypes.CDLL(args.library)
    library.helios_native_abi_version.restype = ctypes.c_int
    library.helios_vector_sum.argtypes = [ctypes.POINTER(ctypes.c_double), ctypes.c_size_t]
    library.helios_vector_sum.restype = ctypes.c_double
    library.helios_vector_mean.argtypes = [ctypes.POINTER(ctypes.c_double), ctypes.c_size_t]
    library.helios_vector_mean.restype = ctypes.c_double

    values = (ctypes.c_double * 4)(1.0, 2.0, 3.0, 4.0)
    print(f"abi={library.helios_native_abi_version()}")
    print(f"sum={library.helios_vector_sum(values, 4)}")
    print(f"mean={library.helios_vector_mean(values, 4)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
