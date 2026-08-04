namespace HELIOS.Analytics.FSharp

open System
open System.Collections.Generic
open HELIOS.Platform.Contracts.XCore9

/// Pure, deterministic XCore-9 ranking and evaluation functions.
type XCoreAnalytics() =
    let finite value = if Double.IsFinite value then value else 0.0
    interface IXCoreAnalytics with
        member _.CalibrateConfidence(rawConfidence, sampleCount) =
            let bounded = Math.Clamp(finite rawConfidence, 0.0, 1.0)
            bounded * (float sampleCount / (float sampleCount + 20.0))
        member _.Rank(candidates) =
            candidates
            |> Seq.map (fun candidate ->
                let features = candidate.Features |> Seq.sortBy (fun feature -> feature.Name) |> Seq.toArray
                let score = if features.Length = 0 then 0.0 else features |> Array.averageBy (fun f -> Math.Clamp(finite f.Value, 0.0, 1.0))
                let confidence = Math.Clamp(float features.Length / 8.0, 0.0, 1.0) * 0.9
                let anomaly = features |> Array.exists (fun f -> not (Double.IsFinite f.Value) || f.Value < 0.0 || f.Value > 1.0)
                RouteScore(candidate.RouteId, score, confidence, anomaly, Dictionary<string,double>(dict [ "featureCount", float features.Length ])))
            |> Seq.sortBy (fun score -> (-score.Score, score.RouteId))
            |> Seq.toArray
        member _.DetectAnomalies(values, threshold) =
            if threshold <= 0.0 then invalidArg "threshold" "Threshold must be positive."
            let data = values |> Seq.map finite |> Seq.toArray
            if data.Length < 2 then Array.empty else
                let mean = Array.average data
                let deviation = data |> Array.averageBy (fun value -> pown (value - mean) 2) |> sqrt
                if deviation = 0.0 then Array.empty else data |> Array.indexed |> Array.choose (fun (index, value) -> if abs(value - mean) / deviation >= threshold then Some index else None)
        member _.EvaluatePredictions(predicted, actual, confidence) =
            if predicted.Count = 0 || predicted.Count <> actual.Count || predicted.Count <> confidence.Count then invalidArg "predicted" "Prediction, actual, and confidence arrays must have equal non-zero lengths."
            let errors = Seq.map2 (fun p a -> finite p - finite a) predicted actual |> Seq.toArray
            let mae = errors |> Array.averageBy abs
            let rmse = errors |> Array.averageBy (fun error -> error * error) |> sqrt
            let calibration = Seq.map2 (fun error c -> abs(Math.Clamp(finite c, 0.0, 1.0) - (if abs error <= 0.1 then 1.0 else 0.0))) errors confidence |> Seq.average
            PredictionEvaluation(mae, rmse, calibration)
