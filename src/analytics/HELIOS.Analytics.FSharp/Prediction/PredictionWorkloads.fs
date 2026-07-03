namespace HELIOS.Analytics.FSharp.Prediction

open System
open System.Collections.Generic
open HELIOS.Analytics.FSharp.Models
open HELIOS.Analytics.FSharp.Statistics

/// Lightweight prediction algorithms for deterministic analytics scenarios.
module PredictionWorkloads =
    let private materialize values =
        let array = values |> Seq.toArray
        if array.Length = 0 then invalidArg (nameof values) "At least one value is required."
        array

    let private confidenceFromError error = 1.0 / (1.0 + max 0.0 error)

    /// Forecasts the next value using a moving average over the requested period.
    let movingAverageForecast period (values: seq<float>) : PredictionResult =
        if period <= 0 then invalidArg (nameof period) "Period must be positive."
        let materialized = materialize values
        let sample = materialized |> Array.rev |> Array.truncate period
        let predicted = MathWorkloads.mean sample
        let volatility = if sample.Length = 1 then 0.0 else MathWorkloads.standardDeviation sample
        { PredictedValue = predicted
          Confidence = confidenceFromError volatility
          Model = "moving-average"
          GeneratedAt = DateTimeOffset.UtcNow
          Diagnostics = Dictionary<string, float>(dict [ "sampleSize", float sample.Length; "volatility", volatility ]) }

    /// Forecasts the next value using exponential smoothing.
    let exponentialSmoothingForecast alpha (values: seq<float>) : PredictionResult =
        if alpha <= 0.0 || alpha > 1.0 then invalidArg (nameof alpha) "Alpha must be greater than 0 and less than or equal to 1."
        let materialized = materialize values
        let smoothed = materialized |> Array.tail |> Array.fold (fun forecast value -> alpha * value + (1.0 - alpha) * forecast) materialized[0]
        let residuals = materialized |> Array.map (fun value -> value - smoothed)
        let error = if residuals.Length = 1 then 0.0 else MathWorkloads.standardDeviation residuals
        { PredictedValue = smoothed
          Confidence = confidenceFromError error
          Model = "exponential-smoothing"
          GeneratedAt = DateTimeOffset.UtcNow
          Diagnostics = Dictionary<string, float>(dict [ "alpha", alpha; "error", error; "sampleSize", float materialized.Length ]) }

    /// Forecasts the next value using least-squares linear extrapolation.
    let linearForecast (values: seq<float>) : PredictionResult =
        let materialized = materialize values
        if materialized.Length = 1 then
            { PredictedValue = materialized[0]
              Confidence = 1.0
              Model = "linear-regression"
              GeneratedAt = DateTimeOffset.UtcNow
              Diagnostics = Dictionary<string, float>(dict [ "slope", 0.0; "sampleSize", 1.0; "rmse", 0.0 ]) }
        else
            let n = float materialized.Length
            let xMean = (n - 1.0) / 2.0
            let yMean = MathWorkloads.mean materialized
            let slope =
                let numerator = materialized |> Array.mapi (fun index value -> (float index - xMean) * (value - yMean)) |> Array.sum
                let denominator = materialized |> Array.mapi (fun index _ -> pown (float index - xMean) 2) |> Array.sum
                numerator / denominator
            let intercept = yMean - slope * xMean
            let fitted = materialized |> Array.mapi (fun index _ -> intercept + slope * float index)
            let rmse = MathWorkloads.rootMeanSquaredError materialized fitted
            let predicted = intercept + slope * n
            { PredictedValue = predicted
              Confidence = confidenceFromError rmse
              Model = "linear-regression"
              GeneratedAt = DateTimeOffset.UtcNow
              Diagnostics = Dictionary<string, float>(dict [ "slope", slope; "sampleSize", n; "rmse", rmse ]) }
