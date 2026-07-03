# HELIOS Native Performance / XCore Landing Zone

This module is the planned landing zone for C++/XCore performance backends. It keeps native acceleration separate from C# platform services and F# analytics until benchmarks justify specific native implementations.

## Intended ownership

- Native telemetry sampling
- XCore node integration
- SIMD/vectorized kernels
- GPU or hardware-assisted performance probes
- Interop surfaces consumed through shared C# contracts

## ABI v1

The first optional native ABI is declared in `include/helios_native_performance.h` and implemented in `src/helios_native_performance.cpp`.

Exported functions:

- `helios_native_abi_version()` returns the ABI version.
- `helios_vector_sum(const double* values, size_t length)` returns a safe sum and treats null/empty input as `0.0`.
- `helios_vector_mean(const double* values, size_t length)` returns a safe mean and treats null/empty input as `0.0`.

`HELIOSNativePerformanceBenchmark` is intentionally small: it verifies that the optional native library can compile, link, and execute before any C#/F#/Python caller is required to depend on it.

## Safety policy

1. Managed C# and F# implementations remain the default.
2. Native code must include benchmarks and tests before becoming required runtime code.
3. Interop boundaries should be documented and versioned.
4. Branch intelligence should rank native/XCore branches separately under `src/native`.

## Optional interop smoke tests

- C# sample: `samples/native-interop/HELIOS.NativeInterop.Sample.csproj`
- Python ctypes sample: `scripts/native/native_smoke.py`

Build the native library first:

```bash
cmake -S src/native/HELIOS.Native.Performance -B .build/native
cmake --build .build/native
```

Then run optional consumers with the platform-specific shared library on the library search path. The default Linux build emits `.build/native/libHELIOSNativePerformance.so`.
