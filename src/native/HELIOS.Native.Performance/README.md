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

## Monadoblade living environment

`monadoblade_environment_renderer.hpp` defines the portable scene planner used behind a WinUI `SwapChainPanel`. It converts frame time, GPU and memory pressure, battery, thermals, occlusion, reduced-motion settings, local time, weather, wind, pointer movement, and profile energy into a fixed rendering budget.

The design uses four parallax horizon cards, instanced grass ribbons, a fixed particle pool, and low-resolution fog. Covered or minimized surfaces suspend immediately. The matching HLSL compute shader performs one bounded particle-update dispatch; audio and Chroma remain optional consumers and cannot block the shell.
