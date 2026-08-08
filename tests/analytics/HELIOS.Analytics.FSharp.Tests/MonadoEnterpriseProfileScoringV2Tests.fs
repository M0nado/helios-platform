namespace HELIOS.Analytics.FSharp.Tests

open System
open HELIOS.Analytics.FSharp.Optimization
open Xunit

module MonadoEnterpriseProfileScoringV2Tests =
    let private telemetry profileRisk vmPressure audioXruns frameTime networkLatency storageLatency : MonadoEnterpriseTelemetry =
        {
            CpuUtilization = 62.0
            GpuUtilization = 55.0
            MemoryUtilization = 70.0
            StorageLatencyMs = storageLatency
            NetworkLatencyMs = networkLatency
            ThermalPressure = 45.0
            SecurityRisk = profileRisk
            VmMemoryPressure = vmPressure
            ModelLatencyMs = 95.0
            AudioXruns = audioXruns
            FrameTimeMs = frameTime
        }

    [<Fact>]
    let ``All permanent profile scores are bounded`` () =
        let signal = telemetry 20.0 35.0 0.0 16.0 35.0 9.0
        let profiles =
            [|
                MonadoEnterpriseProfileId.Core
                MonadoEnterpriseProfileId.Developer
                MonadoEnterpriseProfileId.Gamer
                MonadoEnterpriseProfileId.Studio
                MonadoEnterpriseProfileId.Personal
                MonadoEnterpriseProfileId.SysOps
                MonadoEnterpriseProfileId.AiServer
                MonadoEnterpriseProfileId.SysAdmin
            |]

        for profile in profiles do
            let score = MonadoEnterpriseProfileScoringV2.Score(profile, signal)
            Assert.InRange(score, 0.0, 1.0)

    [<Fact>]
    let ``SysAdmin recommendations stay approval-gated`` () =
        let signal = telemetry 15.0 20.0 0.0 16.0 30.0 8.0
        let recommendation = MonadoEnterpriseProfileScoringV2.Recommend(MonadoEnterpriseProfileId.SysAdmin, signal)

        Assert.True(recommendation.RequiresApproval)
        Assert.Contains("recommend-only", recommendation.Actions)

    [<Fact>]
    let ``AiServer recommendation rebalances worker capacity under VM pressure`` () =
        let signal = telemetry 12.0 80.0 0.0 16.0 22.0 7.0
        let recommendation = MonadoEnterpriseProfileScoringV2.Recommend(MonadoEnterpriseProfileId.AiServer, signal)

        Assert.Contains("rebalance-worker-capacity", recommendation.Actions)

    [<Fact>]
    let ``Core recommendation remains stable for low-risk baseline`` () =
        let signal = telemetry 5.0 20.0 0.0 15.0 18.0 6.0
        let recommendation = MonadoEnterpriseProfileScoringV2.Recommend(MonadoEnterpriseProfileId.Core, signal)

        Assert.Contains("maintain-current-policy", recommendation.Actions)
        Assert.False(recommendation.RequiresApproval)

    [<Fact>]
    let ``Non-finite telemetry fails closed`` () =
        let signal = telemetry Double.NaN 20.0 0.0 15.0 18.0 6.0
        let score = MonadoEnterpriseProfileScoringV2.Score(MonadoEnterpriseProfileId.Core, signal)
        let recommendation = MonadoEnterpriseProfileScoringV2.Recommend(MonadoEnterpriseProfileId.Core, signal)

        Assert.Equal(0.0, score)
        Assert.True(recommendation.RequiresApproval)
        Assert.Contains("require-operator-telemetry-review", recommendation.Actions)

    [<Fact>]
    let ``Negative telemetry values fail closed`` () =
        let signal = telemetry -1.0 20.0 0.0 15.0 18.0 6.0
        let score = MonadoEnterpriseProfileScoringV2.Score(MonadoEnterpriseProfileId.Core, signal)
        let recommendation = MonadoEnterpriseProfileScoringV2.Recommend(MonadoEnterpriseProfileId.Core, signal)

        Assert.Equal(0.0, score)
        Assert.True(recommendation.RequiresApproval)
        Assert.Contains("require-operator-telemetry-review", recommendation.Actions)
