namespace HELIOS.Analytics.FSharp.Statistics

open System
open HELIOS.Analytics.FSharp.Models
open HELIOS.Platform.Contracts.Analytics

/// Higher-level analytics routines built on the core math workloads.
module AnalyticsWorkloads =
    let private slope (points: float[]) =
        if points.Length < 2 then 0.0
        else
            let n = float points.Length
            let xMean = (n - 1.0) / 2.0
            let yMean = MathWorkloads.mean points
            let numerator = points |> Array.mapi (fun index value -> (float index - xMean) * (value - yMean)) |> Array.sum
            let denominator = points |> Array.mapi (fun index _ -> pown (float index - xMean) 2) |> Array.sum
            if denominator = 0.0 then 0.0 else numerator / denominator

    /// Classifies the direction and significance of a trend slope.
    let classifyTrend tolerance trend =
        if abs trend <= tolerance then "stable"
        elif trend > 0.0 then "increasing"
        else "decreasing"

    /// Calculates a bounded 0-100 health score from anomaly density and volatility.
    let healthScore anomalyThreshold (values: seq<float>) =
        let materialized = values |> Seq.toArray
        if materialized.Length = 0 then invalidArg (nameof values) "At least one value is required."
        let anomalyCount = MathWorkloads.zScores materialized |> Array.filter (fun score -> abs score >= anomalyThreshold) |> Array.length
        let anomalyPenalty = 100.0 * float anomalyCount / float materialized.Length
        let volatilityPenalty = min 50.0 (MathWorkloads.standardDeviation materialized)
        max 0.0 (100.0 - anomalyPenalty - volatilityPenalty)

    /// Aggregates a set of metric points into a named analytics window.
    let window (name: string) (startedAt: DateTimeOffset) (endedAt: DateTimeOffset) (points: seq<#IMetricPoint>) =
        let values = points |> Seq.map _.Value |> Seq.toArray
        let trend = slope values
        { Name = name
          StartedAt = startedAt
          EndedAt = endedAt
          Summary = MathWorkloads.summarize values
          Trend = trend
          TrendClassification = classifyTrend 0.001 trend
          HealthScore = healthScore 2.0 values }

    /// Detects values with an absolute z-score greater than or equal to a threshold.
    let anomalies threshold (values: seq<float>) =
        if threshold <= 0.0 then invalidArg (nameof threshold) "Threshold must be positive."
        let materialized = values |> Seq.toArray
        let scores = MathWorkloads.zScores materialized
        Array.zip materialized scores
        |> Array.filter (fun (_, score) -> abs score >= threshold)
        |> Array.map (fun (value, score) -> { Value = value; Score = score })

    /// Normalizes values into the 0-1 range.
    let normalizeMinMax (values: seq<float>) =
        let materialized = values |> Seq.toArray
        let summary = MathWorkloads.summarize materialized
        let range = summary.Maximum - summary.Minimum
        if range = 0.0 then materialized |> Array.map (fun _ -> 0.0)
        else materialized |> Array.map (fun value -> (value - summary.Minimum) / range)
