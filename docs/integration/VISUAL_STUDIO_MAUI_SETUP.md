# Visual Studio and .NET MAUI Setup

HELIOS is structured so C#, F#, C++, Python, Azure, and dashboard tooling can be developed from one command center while still supporting Visual Studio and .NET MAUI on developer machines.

## Recommended Windows setup

1. Install Visual Studio 2022 or newer.
2. Enable workloads:
   - .NET desktop development
   - Desktop development with C++
   - .NET Multi-platform App UI development
   - Azure development
   - Python development
3. Install .NET 8 SDK or newer.
4. Run:

```powershell
dotnet workload list
dotnet workload install maui
```

## Repository commands

```bash
./helios.sh setup
./helios.sh build
PATH="/tmp/dotnet:$PATH" dotnet test tests/analytics/HELIOS.Analytics.FSharp.Tests/HELIOS.Analytics.FSharp.Tests.fsproj --no-restore
cmake -S src/native/HELIOS.Native.Performance -B .build/native
```

## Language map

- C#: contracts and future app connective tissue under `src/core/`.
- F#: analytics/math/prediction under `src/analytics/`.
- C++/XCore: native performance landing zone under `src/native/`.
- Python: automation, control-plane, branch intelligence, and dashboard generation under `scripts/`.
- Azure/Bicep: infrastructure under `infra/azure/`.
