# HELIOS Native Performance / XCore Landing Zone

This module is the planned landing zone for C++/XCore performance backends. It keeps native acceleration separate from C# platform services and F# analytics until benchmarks justify specific native implementations.

## Intended ownership

- Native telemetry sampling
- XCore node integration
- SIMD/vectorized kernels
- GPU or hardware-assisted performance probes
- Interop surfaces consumed through shared C# contracts

## Safety policy

1. Managed C# and F# implementations remain the default.
2. Native code must include benchmarks and tests before becoming required runtime code.
3. Interop boundaries should be documented and versioned.
4. Branch intelligence should rank native/XCore branches separately under `src/native`.
