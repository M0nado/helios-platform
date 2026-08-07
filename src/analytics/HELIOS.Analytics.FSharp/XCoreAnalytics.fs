namespace HELIOS.Analytics.FSharp

open System
open System.Collections.Generic
open HELIOS.Platform.Contracts.XCore9

/// Pure, deterministic XCore-9 ranking and evaluation functions.
type XCoreAnalytics() =
    let finite value = if Double.IsFinite value then value else 0.0
    let scoreFeature (feature: SanitizedRunFeature) =
        let bounded = Math.Clamp(finite feature.Value, 0.0, 1.0)
        match feature.Name with
        | "retry_rate"
        | "cost_ratio"
        | "duration_ratio" -> 1.0 - bounded
        | _ -> bounded

    interface IXCoreAnalytics with
        member _.CalibrateConfidence(rawConfidence, sampleCount) =
            if sampleCount < 0 then invalidArg "sampleCount" "Sample count must be non-negative."
            let bounded = Math.Clamp(finite rawConfidence, 0.0, 1.0)
            bounded * (float sampleCount / (float sampleCount + 20.0))
        member _.Rank(candidates) =
            candidates
            |> Seq.map (fun candidate ->
                let features = candidate.Features |> Seq.sortBy (fun feature -> feature.Name) |> Seq.toArray
                let score = if features.Length = 0 then 0.0 else features |> Array.averageBy scoreFeature
                let confidence = Math.Clamp(float features.Length / 8.0, 0.0, 1.0) * 0.9
                let anomaly = features |> Array.exists (fun f -> not (Double.IsFinite f.Value) || f.Value < 0.0 || f.Value > 1.0)
                RouteScore(candidate.RouteId, score, confidence, anomaly, Dictionary<string,double>(dict [ "featureCount", float features.Length ])))
            |> Seq.sortBy (fun score -> (-score.Score, score.RouteId))
            |> Seq.toArray
            :> IReadOnlyList<RouteScore>
        member _.DetectAnomalies(values, threshold) =
            if not (Double.IsFinite threshold) || threshold <= 0.0 then invalidArg "threshold" "Threshold must be finite and positive."
            let indexed = values |> Seq.mapi (fun index value -> index, value) |> Seq.toArray
            let nonFinite =
                indexed
                |> Array.choose (fun (index, value) -> if Double.IsFinite value then None else Some index)
            let finiteData =
                indexed
                |> Array.choose (fun (index, value) -> if Double.IsFinite value then Some (index, value) else None)
            if finiteData.Length < 2 then
                nonFinite |> Array.distinct |> Array.sort :> IReadOnlyList<int>
            else
                let maxAbs = finiteData |> Array.maxBy (fun (_, value) -> abs value) |> snd |> abs
                if maxAbs = 0.0 then
                    nonFinite |> Array.distinct |> Array.sort :> IReadOnlyList<int>
                else
                    let normalized = finiteData |> Array.map (fun (index, value) -> index, value / maxAbs)
                    let meanNormalized = normalized |> Array.averageBy snd
                    let centered =
                        normalized
                        |> Array.map (fun (index, value) -> index, value - meanNormalized)
                    let deviation = centered |> Array.averageBy (fun (_, value) -> value * value) |> sqrt
                    let statistical =
                        if deviation = 0.0 then Array.empty<int>
                        else
                            centered
                            |> Array.choose (fun (index, value) -> if abs value / deviation >= threshold then Some index else None)
                    Array.append nonFinite statistical |> Array.distinct |> Array.sort :> IReadOnlyList<int>
        member _.EvaluatePredictions(predicted, actual, confidence) =
            if predicted.Count = 0 || predicted.Count <> actual.Count || predicted.Count <> confidence.Count then invalidArg "predicted" "Prediction, actual, and confidence arrays must have equal non-zero lengths."
            if Seq.exists (fun value -> not (Double.IsFinite value)) predicted ||
               Seq.exists (fun value -> not (Double.IsFinite value)) actual ||
               Seq.exists (fun value -> not (Double.IsFinite value)) confidence then
                invalidArg "predicted" "Prediction, actual, and confidence values must be finite."
            if Seq.exists (fun value -> value < 0.0 || value > 1.0) confidence then
                invalidArg "confidence" "Confidence values must be normalized to [0,1]."
            let errors =
                Seq.map2 (fun p a ->
                    let error = p - a
                    if not (Double.IsFinite error) then invalidArg "predicted" "Prediction and actual differences must be finite."
                    error)
                    predicted
                    actual
                |> Seq.toArray
            let maxAbsError = errors |> Array.maxBy abs |> abs
            let mae =
                if maxAbsError = 0.0 then 0.0
                else
                    maxAbsError * (errors |> Array.averageBy (fun error -> abs(error / maxAbsError)))
            let rmse =
                if maxAbsError = 0.0 then 0.0
                else
                    let normalizedSquares = errors |> Array.averageBy (fun error -> let normalized = error / maxAbsError in normalized * normalized)
                    maxAbsError * sqrt normalizedSquares
            let calibration = Seq.map2 (fun error c -> abs(c - (if abs error <= 0.1 then 1.0 else 0.0))) errors confidence |> Seq.average
            PredictionEvaluation(mae, rmse, calibration)
