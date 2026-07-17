namespace HELIOS.Analytics.FSharp.Optimization

open System
open HELIOS.Platform.Contracts.Monadoblade

[<CLIMutable>]
type ProfileTelemetry =
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
type ProfileOptimizationRecommendation =
    {
        Profile: MonadobladeProfileId
        FitnessScore: float
        Confidence: float
        Actions: string array
        Reasons: string array
        RequiresApproval: bool
    }

module private Numeric =
    let clamp (minimum: float) (maximum: float) (value: float) : float =
        if value < minimum then minimum
        elif value > maximum then maximum
        else value

    let normalizedPercent (value: float) : float =
        clamp 0.0 1.0 (value / 100.0)

    let inverseNormalized (maximum: float) (value: float) : float =
        if maximum <= 0.0 then
            0.0
        else
            1.0 - clamp 0.0 1.0 (value / maximum)

module ProfilePolicy =
    let private commonSafety (telemetry: ProfileTelemetry) : float =
        let thermal = Numeric.normalizedPercent telemetry.ThermalPressure
        let risk = Numeric.normalizedPercent telemetry.SecurityRisk
        1.0 - ((thermal * 0.35) + (risk * 0.65))

    let private developer (telemetry: ProfileTelemetry) : float =
        let cpuHeadroom = 1.0 - Numeric.normalizedPercent telemetry.CpuUtilization
        let memoryHeadroom = 1.0 - Numeric.normalizedPercent telemetry.MemoryUtilization
        let storage = Numeric.inverseNormalized 20.0 telemetry.StorageLatencyMs
        let model = Numeric.inverseNormalized 250.0 telemetry.ModelLatencyMs
        (cpuHeadroom * 0.25) + (memoryHeadroom * 0.25) + (storage * 0.25) + (model * 0.25)

    let private sysAdmin (telemetry: ProfileTelemetry) : float =
        let risk = 1.0 - Numeric.normalizedPercent telemetry.SecurityRisk
        let thermal = 1.0 - Numeric.normalizedPercent telemetry.ThermalPressure
        let memory = 1.0 - Numeric.normalizedPercent telemetry.MemoryUtilization
        (risk * 0.60) + (thermal * 0.20) + (memory * 0.20)

    let private sysOps (telemetry: ProfileTelemetry) : float =
        let network = Numeric.inverseNormalized 150.0 telemetry.NetworkLatencyMs
        let storage = Numeric.inverseNormalized 30.0 telemetry.StorageLatencyMs
        let vm = 1.0 - Numeric.normalizedPercent telemetry.VmMemoryPressure
        let risk = 1.0 - Numeric.normalizedPercent telemetry.SecurityRisk
        (network * 0.25) + (storage * 0.20) + (vm * 0.20) + (risk * 0.35)

    let private gamer (telemetry: ProfileTelemetry) : float =
        let frame = Numeric.inverseNormalized 33.4 telemetry.FrameTimeMs
        let network = Numeric.inverseNormalized 100.0 telemetry.NetworkLatencyMs
        let gpu = Numeric.normalizedPercent telemetry.GpuUtilization
        let thermal = 1.0 - Numeric.normalizedPercent telemetry.ThermalPressure
        (frame * 0.40) + (network * 0.20) + (gpu * 0.20) + (thermal * 0.20)

    let private studio (telemetry: ProfileTelemetry) : float =
        let audio = Numeric.inverseNormalized 10.0 telemetry.AudioXruns
        let storage = Numeric.inverseNormalized 15.0 telemetry.StorageLatencyMs
        let memory = 1.0 - Numeric.normalizedPercent telemetry.MemoryUtilization
        let thermal = 1.0 - Numeric.normalizedPercent telemetry.ThermalPressure
        (audio * 0.45) + (storage * 0.25) + (memory * 0.15) + (thermal * 0.15)

    let private personal (telemetry: ProfileTelemetry) : float =
        let cpu = 1.0 - Numeric.normalizedPercent telemetry.CpuUtilization
        let memory = 1.0 - Numeric.normalizedPercent telemetry.MemoryUtilization
        let thermal = 1.0 - Numeric.normalizedPercent telemetry.ThermalPressure
        let risk = 1.0 - Numeric.normalizedPercent telemetry.SecurityRisk
        (cpu * 0.20) + (memory * 0.20) + (thermal * 0.25) + (risk * 0.35)

    let private server (telemetry: ProfileTelemetry) : float =
        let cpu = 1.0 - Numeric.normalizedPercent telemetry.CpuUtilization
        let memory = 1.0 - Numeric.normalizedPercent telemetry.MemoryUtilization
        let vm = 1.0 - Numeric.normalizedPercent telemetry.VmMemoryPressure
        let network = Numeric.inverseNormalized 150.0 telemetry.NetworkLatencyMs
        let risk = 1.0 - Numeric.normalizedPercent telemetry.SecurityRisk
        (cpu * 0.15) + (memory * 0.20) + (vm * 0.20) + (network * 0.15) + (risk * 0.30)

    let score (profile: MonadobladeProfileId) (telemetry: ProfileTelemetry) : float =
        let profileFitness =
            match profile with
            | MonadobladeProfileId.Developer -> developer telemetry
            | MonadobladeProfileId.SysAdmin -> sysAdmin telemetry
            | MonadobladeProfileId.SysOps -> sysOps telemetry
            | MonadobladeProfileId.Gamer -> gamer telemetry
            | MonadobladeProfileId.Studio -> studio telemetry
            | MonadobladeProfileId.Personal -> personal telemetry
            | MonadobladeProfileId.ServerBackground -> server telemetry
            | _ -> 0.0

        Numeric.clamp 0.0 1.0 ((profileFitness * 0.80) + (commonSafety telemetry * 0.20))

    let recommend
        (profile: MonadobladeProfileId)
        (telemetry: ProfileTelemetry)
        : ProfileOptimizationRecommendation =
        let fitness = score profile telemetry
        let actions = ResizeArray<string>()
        let reasons = ResizeArray<string>()
        let mutable requiresApproval = false

        if telemetry.SecurityRisk >= 60.0 then
            actions.Add("isolate-risky-workload")
            reasons.Add("Security risk exceeded the automatic optimization threshold.")
            requiresApproval <- true

        if telemetry.ThermalPressure >= 80.0 then
            actions.Add("reduce-background-compute")
            reasons.Add("Thermal pressure is high.")

        if telemetry.MemoryUtilization >= 88.0 || telemetry.VmMemoryPressure >= 85.0 then
            actions.Add("reduce-vm-and-model-memory-pressure")
            reasons.Add("Memory pressure threatens profile stability.")

        match profile with
        | MonadobladeProfileId.Developer when telemetry.StorageLatencyMs >= 15.0 ->
            actions.Add("review-devdrive-cache-placement")
            reasons.Add("Developer storage latency is above the preferred build threshold.")
        | MonadobladeProfileId.SysOps when telemetry.NetworkLatencyMs >= 100.0 ->
            actions.Add("inspect-network-and-service-path")
            reasons.Add("SysOps network latency is elevated.")
        | MonadobladeProfileId.Gamer when telemetry.FrameTimeMs >= 20.0 ->
            actions.Add("throttle-nonessential-background-workers")
            reasons.Add("Frame-time stability is below the Gamer target.")
        | MonadobladeProfileId.Studio when telemetry.AudioXruns > 0.0 ->
            actions.Add("protect-audio-realtime-scheduling")
            reasons.Add("Audio x-runs were detected.")
        | MonadobladeProfileId.ServerBackground when telemetry.VmMemoryPressure >= 75.0 ->
            actions.Add("rebalance-worker-capacity")
            reasons.Add("Server/background VM memory pressure is elevated.")
        | MonadobladeProfileId.SysAdmin ->
            actions.Add("recommend-only")
            reasons.Add("SysAdmin optimization never applies autonomous writes.")
            requiresApproval <- true
        | _ -> ()

        if actions.Count = 0 then
            actions.Add("maintain-current-policy")
            reasons.Add("No threshold requires a policy change.")

        {
            Profile = profile
            FitnessScore = Math.Round(fitness, 4)
            Confidence =
                Math.Round(
                    Numeric.clamp 0.50 0.98 (0.55 + (Math.Abs(fitness - 0.5) * 0.7)),
                    4
                )
            Actions = actions.ToArray()
            Reasons = reasons.ToArray()
            RequiresApproval = requiresApproval
        }

/// C#-friendly facade for profile policy scoring and recommendations.
[<AbstractClass; Sealed>]
type ProfilePolicyOptimizer private () =
    static member Score(profile: MonadobladeProfileId, telemetry: ProfileTelemetry) =
        ArgumentNullException.ThrowIfNull(telemetry)
        ProfilePolicy.score profile telemetry

    static member Recommend(profile: MonadobladeProfileId, telemetry: ProfileTelemetry) =
        ArgumentNullException.ThrowIfNull(telemetry)
        ProfilePolicy.recommend profile telemetry
