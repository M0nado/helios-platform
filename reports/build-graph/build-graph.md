# HELIOS Build Graph

| Node | Title | Command |
| --- | --- | --- |
| `hybrid-gap-analysis` | Hybrid integration gap analysis | `python3 scripts/analysis/hybrid_gap_analysis.py` |
| `repo-inventory` | Whole-project inventory | `python3 scripts/analysis/repo_inventory.py` |
| `python-static` | Python static compile checks | `python3 -m py_compile scripts/control/helios-control.py scripts/web/helios-web.py scripts/ai/enrich-ideas.py scripts/integrations/check-connections.py scripts/analysis/branch_intelligence.py scripts/graphs/generate_graphs.py scripts/github/update-wiki-from-reports.py` |
| `analytics-tests` | F# analytics tests | `dotnet test tests/analytics/HELIOS.Analytics.FSharp.Tests/HELIOS.Analytics.FSharp.Tests.fsproj --no-restore` |
| `native-configure` | Native/XCore CMake configure | `cmake -S src/native/HELIOS.Native.Performance -B .build/native` |
| `native-build` | Native/XCore CMake build | `cmake --build .build/native` |
| `branch-intelligence` | Branch intelligence reports | `python3 scripts/analysis/branch_intelligence.py` |
| `graphs` | Graph reports | `python3 scripts/graphs/generate_graphs.py` |
| `control-summary` | Control summary | `python3 scripts/control/helios-control.py` |
| `azure-bicep` | Azure Bicep build | `az bicep build --file infra/azure/main.bicep` |
| `dotnet-workloads` | .NET workload visibility for MAUI/Visual Studio readiness | `dotnet workload list` |
| `maui-readiness` | MAUI readiness placeholder | `dotnet --info` |
| `visual-studio-readiness` | Visual Studio developer machine readiness guidance | `python3 -c "print('Visual Studio/MAUI readiness is documented; install VS with .NET MAUI workload on Windows/macOS.')"` |
