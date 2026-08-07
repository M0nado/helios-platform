namespace HELIOS.Analytics.FSharp.Optimization

open System

type MonadoEnterpriseProfileId =
    | Core
    | Developer
    | Gamer
    | Studio
    | Personal
    | SysOps
    | AiServer
    | SysAdmin

[<CLIMutable>]
type MonadoEnterpriseTelemetry =
    {
        CpuUtilization: float
        GpuUtilization: float
        MemoryUtilization: float
        StorageLatencyMs: float
        NetworkLatencyMs: float
        ThermalPressure: float
        SecurityRisk: float
        VmMemoryPressure: float
        ModelLatencyMs: float
        AudioXruns: float
        FrameTimeMs: float
    }

[<CLIMutable>]
type MonadoEnterpriseRecommendation =
    {
        Profile: MonadoEnterpriseProfileId
        FitnessScore: float
        RequiresApproval: bool
        Actions: string array
    }

module private MonadoEnterpriseNumeric =
    let clamp01 (value: float) : float =
        if value < 0.0 then 0.0
        elif value > 1.0 then 1.0
        else value

    let normalizedPercent (value: float) : float =
        clamp01 (value / 100.0)

    let inverseNormalized (maximum: float) (value: float) : float =
        if maximum <= 0.0 then
            0.0
        else
            1.0 - clamp01 (value / maximum)

module private MonadoEnterpriseTelemetryValidation =
    let isFinite (telemetry: MonadoEnterpriseTelemetry) : bool =
        [|
            telemetry.CpuUtilization
            telemetry.GpuUtilization
            telemetry.MemoryUtilization
            telemetry.StorageLatencyMs
            telemetry.NetworkLatencyMs
            telemetry.ThermalPressure
            telemetry.SecurityRisk
            telemetry.VmMemoryPressure
            telemetry.ModelLatencyMs
            telemetry.AudioXruns
            telemetry.FrameTimeMs
        |]
        |> Array.forall Double.IsFinite

module MonadoEnterpriseProfilePolicy =
    let private safetyFloor (telemetry: MonadoEnterpriseTelemetry) : float =
        let thermal = MonadoEnterpriseNumeric.normalizedPercent telemetry.ThermalPressure
        let risk = MonadoEnterpriseNumeric.normalizedPercent telemetry.SecurityRisk
        1.0 - ((thermal * 0.35) + (risk * 0.65))

    let score (profile: MonadoEnterpriseProfileId) (telemetry: MonadoEnterpriseTelemetry) : float =
        if not (MonadoEnterpriseTelemetryValidation.isFinite telemetry) then
            0.0
        else
            let cpuHeadroom = 1.0 - MonadoEnterpriseNumeric.normalizedPercent telemetry.CpuUtilization
            let gpuLoad = MonadoEnterpriseNumeric.normalizedPercent telemetry.GpuUtilization
            let memoryHeadroom = 1.0 - MonadoEnterpriseNumeric.normalizedPercent telemetry.MemoryUtilization
            let storage = MonadoEnterpriseNumeric.inverseNormalized 25.0 telemetry.StorageLatencyMs
            let network = MonadoEnterpriseNumeric.inverseNormalized 150.0 telemetry.NetworkLatencyMs
            let thermal = 1.0 - MonadoEnterpriseNumeric.normalizedPercent telemetry.ThermalPressure
            let risk = 1.0 - MonadoEnterpriseNumeric.normalizedPercent telemetry.SecurityRisk
            let vm = 1.0 - MonadoEnterpriseNumeric.normalizedPercent telemetry.VmMemoryPressure
            let model = MonadoEnterpriseNumeric.inverseNormalized 250.0 telemetry.ModelLatencyMs
            let audio = MonadoEnterpriseNumeric.inverseNormalized 10.0 telemetry.AudioXruns
            let frame = MonadoEnterpriseNumeric.inverseNormalized 33.4 telemetry.FrameTimeMs

            let profileScore =
                match profile with
                | Core -> (cpuHeadroom * 0.30) + (memoryHeadroom * 0.25) + (risk * 0.25) + (thermal * 0.20)
                | Developer -> (cpuHeadroom * 0.20) + (memoryHeadroom * 0.25) + (storage * 0.30) + (model * 0.25)
                | Gamer -> (frame * 0.40) + (network * 0.20) + (gpuLoad * 0.20) + (thermal * 0.20)
                | Studio -> (audio * 0.45) + (storage * 0.25) + (memoryHeadroom * 0.15) + (thermal * 0.15)
                | Personal -> (cpuHeadroom * 0.20) + (memoryHeadroom * 0.20) + (thermal * 0.25) + (risk * 0.35)
                | SysOps -> (network * 0.25) + (storage * 0.20) + (vm * 0.20) + (risk * 0.35)
                | AiServer -> (cpuHeadroom * 0.20) + (memoryHeadroom * 0.25) + (vm * 0.25) + (risk * 0.30)
                | SysAdmin -> (risk * 0.60) + (thermal * 0.20) + (memoryHeadroom * 0.20)

            MonadoEnterpriseNumeric.clamp01 ((profileScore * 0.80) + (safetyFloor telemetry * 0.20))

    let recommend
        (profile: MonadoEnterpriseProfileId)
        (telemetry: MonadoEnterpriseTelemetry)
        : MonadoEnterpriseRecommendation =
        if not (MonadoEnterpriseTelemetryValidation.isFinite telemetry) then
            {
                Profile = profile
                FitnessScore = 0.0
                RequiresApproval = true
                Actions = [| "require-operator-telemetry-review" |]
            }
        else
            let fitness = score profile telemetry
            let actions = ResizeArray<string>()
            let mutable requiresApproval = false

            if telemetry.SecurityRisk >= 60.0 then
                actions.Add("isolate-risky-workload")
                requiresApproval <- true

            if telemetry.ThermalPressure >= 80.0 then
                actions.Add("reduce-background-compute")

            if telemetry.MemoryUtilization >= 88.0 || telemetry.VmMemoryPressure >= 85.0 then
                actions.Add("reduce-vm-and-model-memory-pressure")

            match profile with
            | Developer when telemetry.StorageLatencyMs >= 15.0 -> actions.Add("review-devdrive-vhdx-placement")
            | SysOps when telemetry.NetworkLatencyMs >= 100.0 -> actions.Add("inspect-network-and-service-path")
            | Gamer when telemetry.FrameTimeMs >= 20.0 -> actions.Add("throttle-nonessential-background-workers")
            | Studio when telemetry.AudioXruns > 0.0 -> actions.Add("protect-audio-realtime-scheduling")
            | AiServer when telemetry.VmMemoryPressure >= 75.0 -> actions.Add("rebalance-worker-capacity")
            | SysAdmin ->
                actions.Add("recommend-only")
                requiresApproval <- true
            | _ -> ()

            if actions.Count = 0 then
                actions.Add("maintain-current-policy")

            {
                Profile = profile
                FitnessScore = Math.Round(fitness, 4)
                RequiresApproval = requiresApproval
                Actions = actions.ToArray()
            }

[<AbstractClass; Sealed>]
type MonadoEnterpriseProfileScoringV2 private () =
    static member Score(profile: MonadoEnterpriseProfileId, telemetry: MonadoEnterpriseTelemetry) =
        ArgumentNullException.ThrowIfNull(telemetry)
        MonadoEnterpriseProfilePolicy.score profile telemetry

    static member Recommend(profile: MonadoEnterpriseProfileId, telemetry: MonadoEnterpriseTelemetry) =
        ArgumentNullException.ThrowIfNull(telemetry)
        MonadoEnterpriseProfilePolicy.recommend profile telemetry
