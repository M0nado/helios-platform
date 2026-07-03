using HELIOS.Platform.Contracts.Automation;

namespace HELIOS.Platform.Orchestration;

public sealed class FinalGateOrchestrator
{
    public IReadOnlyList<FinalGateStep> RequiredLanguageOwnedSteps() =>
    [
        new("csharp-contracts-build", "core", "csharp", "csharp-center", "required", "dotnet build src/core/HELIOS.Platform.Contracts/HELIOS.Platform.Contracts.csproj --configuration Release"),
        new("fsharp-analytics-tests", "analytics", "fsharp", "fsharp-oracle", "required", "dotnet test tests/analytics/HELIOS.Analytics.FSharp.Tests/HELIOS.Analytics.FSharp.Tests.fsproj --configuration Release"),
        new("native-cpp-build", "native", "cpp", "cpp-accelerator", "required", "cmake -S src/native/HELIOS.Native.Performance -B build/native-performance && cmake --build build/native-performance --config Release"),
        new("language-aware-merge-score", "github", "mixed", "github-admiral", "required", "python3 scripts/github/language_aware_score.py")
    ];
}
