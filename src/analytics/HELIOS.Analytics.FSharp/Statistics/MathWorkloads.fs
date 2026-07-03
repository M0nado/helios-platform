namespace HELIOS.Analytics.FSharp.Statistics

open System
open System.Collections.Generic
open HELIOS.Analytics.FSharp.Models

/// Numerically stable mathematical operations for HELIOS analytics workloads.
module MathWorkloads =
    let private requireValues (values: seq<float>) =
        let materialized = values |> Seq.toArray
        if materialized.Length = 0 then invalidArg (nameof values) "At least one value is required."
        if materialized |> Array.exists (fun value -> Double.IsNaN(value) || Double.IsInfinity(value)) then
            invalidArg (nameof values) "Values must be finite numbers."
        materialized

    let private requireSameLength left right =
        if Array.length left <> Array.length right then invalidArg "values" "Sequences must have the same length."
        if Array.isEmpty left then invalidArg "values" "At least one paired value is required."

    /// Calculates an arithmetic mean.
    let mean (values: seq<float>) = requireValues values |> Array.average

    /// Calculates a weighted mean.
    let weightedMean (values: seq<float>) (weights: seq<float>) =
        let valueArray = requireValues values
        let weightArray = requireValues weights
        requireSameLength valueArray weightArray
        let totalWeight = Array.sum weightArray
        if totalWeight = 0.0 then invalidArg (nameof weights) "Total weight must be non-zero."
        Array.zip valueArray weightArray |> Array.sumBy (fun (value, weight) -> value * weight) |> fun total -> total / totalWeight

    /// Calculates a percentile using linear interpolation between closest ranks.
    let percentile percentileRank (values: seq<float>) =
        if percentileRank < 0.0 || percentileRank > 100.0 then invalidArg (nameof percentileRank) "Percentile must be between 0 and 100."
        let sorted = requireValues values |> Array.sort
        if sorted.Length = 1 then sorted[0]
        else
            let rank = (percentileRank / 100.0) * float (sorted.Length - 1)
            let lower = int (floor rank)
            let upper = int (ceil rank)
            if lower = upper then sorted[lower]
            else sorted[lower] + (rank - float lower) * (sorted[upper] - sorted[lower])

    /// Calculates a median value.
    let median (values: seq<float>) = percentile 50.0 values

    /// Calculates population variance.
    let variance (values: seq<float>) =
        let materialized = requireValues values
        let average = mean materialized
        materialized |> Array.averageBy (fun value -> pown (value - average) 2)

    /// Calculates population standard deviation.
    let standardDeviation values = values |> variance |> sqrt

    /// Calculates population covariance for paired sequences.
    let covariance (left: seq<float>) (right: seq<float>) =
        let x = requireValues left
        let y = requireValues right
        requireSameLength x y
        let xMean = mean x
        let yMean = mean y
        Array.zip x y |> Array.averageBy (fun (a, b) -> (a - xMean) * (b - yMean))

    /// Calculates Pearson correlation for paired sequences.
    let correlation (left: seq<float>) (right: seq<float>) =
        let x = requireValues left
        let y = requireValues right
        requireSameLength x y
        let denominator = standardDeviation x * standardDeviation y
        if denominator = 0.0 then 0.0 else covariance x y / denominator

    /// Calculates a normalized z-score for each value.
    let zScores (values: seq<float>) =
        let materialized = requireValues values
        let average = mean materialized
        let deviation = standardDeviation materialized
        if deviation = 0.0 then materialized |> Array.map (fun _ -> 0.0)
        else materialized |> Array.map (fun value -> (value - average) / deviation)

    /// Builds rolling windows of a fixed size.
    let rollingWindow windowSize (values: seq<float>) =
        if windowSize <= 0 then invalidArg (nameof windowSize) "Window size must be positive."
        let materialized = requireValues values
        if materialized.Length < windowSize then Array.empty
        else materialized |> Array.windowed windowSize

    /// Calculates root mean squared error between expected and actual values.
    let rootMeanSquaredError (expected: seq<float>) (actual: seq<float>) =
        let expectedArray = requireValues expected
        let actualArray = requireValues actual
        requireSameLength expectedArray actualArray
        Array.zip expectedArray actualArray
        |> Array.averageBy (fun (expectedValue, actualValue) -> pown (expectedValue - actualValue) 2)
        |> sqrt

    /// Builds a reusable statistical summary.
    let summarize (values: seq<float>) : StatisticalSummary =
        let materialized = requireValues values
        let percentiles =
            Dictionary<string, float>(dict [
                "P25", percentile 25.0 materialized
                "P50", percentile 50.0 materialized
                "P75", percentile 75.0 materialized
                "P95", percentile 95.0 materialized
                "P99", percentile 99.0 materialized ])
        { Count = materialized.Length
          Mean = mean materialized
          Median = median materialized
          Variance = variance materialized
          StandardDeviation = standardDeviation materialized
          Minimum = materialized |> Array.min
          Maximum = materialized |> Array.max
          Percentiles = percentiles }
