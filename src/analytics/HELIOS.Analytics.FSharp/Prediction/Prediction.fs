namespace HELIOS.Analytics.FSharp.Prediction

open HELIOS.Analytics.FSharp.Models
open HELIOS.Analytics.FSharp.Statistics

module Prediction =
    let score (input: PredictionInput) =
        let average = Statistics.mean input.Values
        { Score = average; Confidence = if input.Values.IsEmpty then 0.0 else 0.75 }
