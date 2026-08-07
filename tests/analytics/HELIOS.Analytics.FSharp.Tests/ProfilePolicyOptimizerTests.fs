namespace HELIOS.Analytics.FSharp.Tests

open HELIOS.Analytics.FSharp.Optimization
open HELIOS.Platform.Contracts.Monadoblade
open Xunit

module ProfilePolicyOptimizerTests =
    let private telemetry =
        {
            CpuUtilization = 58.0
            GpuUtilization = 44.0
            MemoryUtilization = 61.0
            StorageLatencyMs = 9.0
            NetworkLatencyMs = 36.0
            ThermalPressure = 25.0
            SecurityRisk = 8.0
            VmMemoryPressure = 31.0
            ModelLatencyMs = 90.0
            AudioXruns = 0.0
            FrameTimeMs = 12.5
        }

    [<Fact>]
    let ``Score remains bounded for every Monadoblade profile`` () =
        let profiles =
            [|
                MonadobladeProfileId.Developer
                MonadobladeProfileId.SysAdmin
                MonadobladeProfileId.SysOps
                MonadobladeProfileId.Gamer
                MonadobladeProfileId.Studio
                MonadobladeProfileId.Personal
                MonadobladeProfileId.ServerBackground
            |]

        for profile in profiles do
            let score = ProfilePolicyOptimizer.Score(profile, telemetry)
            Assert.InRange(score, 0.0, 1.0)

    [<Fact>]
    let ``SysAdmin recommendations always require approval`` () =
        let recommendation = ProfilePolicyOptimizer.Recommend(MonadobladeProfileId.SysAdmin, telemetry)
        Assert.True(recommendation.RequiresApproval)
        Assert.Contains("recommend-only", recommendation.Actions)

    let private enterpriseTelemetry : MonadoEnterpriseTelemetry =
        {
            CpuUtilization = 58.0
            GpuUtilization = 44.0
            MemoryUtilization = 61.0
            StorageLatencyMs = 9.0
            NetworkLatencyMs = 36.0
            ThermalPressure = 25.0
            SecurityRisk = 8.0
            VmMemoryPressure = 31.0
            ModelLatencyMs = 90.0
            AudioXruns = 0.0
            FrameTimeMs = 12.5
        }

    [<Fact>]
    let ``Enterprise v2 scorer remains bounded for all permanent profiles`` () =
        let profiles : HELIOS.Analytics.FSharp.Optimization.MonadoEnterpriseProfileId array =
            [|
                HELIOS.Analytics.FSharp.Optimization.MonadoEnterpriseProfileId.Core
                HELIOS.Analytics.FSharp.Optimization.MonadoEnterpriseProfileId.Developer
                HELIOS.Analytics.FSharp.Optimization.MonadoEnterpriseProfileId.Gamer
                HELIOS.Analytics.FSharp.Optimization.MonadoEnterpriseProfileId.Studio
                HELIOS.Analytics.FSharp.Optimization.MonadoEnterpriseProfileId.Personal
                HELIOS.Analytics.FSharp.Optimization.MonadoEnterpriseProfileId.SysOps
                HELIOS.Analytics.FSharp.Optimization.MonadoEnterpriseProfileId.AiServer
                HELIOS.Analytics.FSharp.Optimization.MonadoEnterpriseProfileId.SysAdmin
            |]

        for profile in profiles do
            let score = MonadoEnterpriseProfileScoringV2.Score(profile, enterpriseTelemetry)
            Assert.InRange(score, 0.0, 1.0)
