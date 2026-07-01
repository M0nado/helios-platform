namespace HELIOS.Analytics.FSharp.Models

type PredictionInput = { Values: float list }
type PredictionResult = { Score: float; Confidence: float }
