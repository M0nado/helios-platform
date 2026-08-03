namespace HELIOS.Analytics.FSharp.Models

open System
open System.Collections.Generic
open HELIOS.Platform.Contracts.Analytics

/// Single numeric observation captured from a telemetry stream or business metric.
[<CLIMutable>]
type MetricPoint =
    { Timestamp: DateTimeOffset
      Name: string
      Value: float
      Tags: IReadOnlyDictionary<string, string> }
    interface IMetricPoint with
        member this.Timestamp = this.Timestamp
        member this.Name = this.Name
        member this.Value = this.Value
        member this.Tags = this.Tags

/// Compact statistical summary for a numeric workload.
[<CLIMutable>]
type StatisticalSummary =
    { Count: int
      Mean: float
      Median: float
      Variance: float
      StandardDeviation: float
      Minimum: float
      Maximum: float
      Percentiles: IReadOnlyDictionary<string, float> }
    interface IStatisticalSummary with
        member this.Count = this.Count
        member this.Mean = this.Mean
        member this.Median = this.Median
        member this.Variance = this.Variance
        member this.StandardDeviation = this.StandardDeviation
        member this.Minimum = this.Minimum
        member this.Maximum = this.Maximum
        member this.Percentiles = this.Percentiles


/// Value with its normalized anomaly score.
[<CLIMutable>]
type AnomalyScore =
    { Value: float
      Score: float }
    interface IAnomalyScore with
        member this.Value = this.Value
        member this.Score = this.Score

/// Prediction result with confidence and model diagnostic metadata.
[<CLIMutable>]
type PredictionResult =
    { PredictedValue: float
      Confidence: float
      Model: string
      GeneratedAt: DateTimeOffset
      Diagnostics: IReadOnlyDictionary<string, float> }
    interface IPredictionResult with
        member this.PredictedValue = this.PredictedValue
        member this.Confidence = this.Confidence
        member this.Model = this.Model
        member this.GeneratedAt = this.GeneratedAt
        member this.Diagnostics = this.Diagnostics

/// Windowed analytics aggregate suitable for C# contracts and UI projection layers.
[<CLIMutable>]
type AnalyticsWindow =
    { Name: string
      StartedAt: DateTimeOffset
      EndedAt: DateTimeOffset
      Summary: StatisticalSummary
      Trend: float
      TrendClassification: string
      HealthScore: float }
    interface IAnalyticsWindow with
        member this.Name = this.Name
        member this.StartedAt = this.StartedAt
        member this.EndedAt = this.EndedAt
        member this.Summary = this.Summary :> IStatisticalSummary
        member this.Trend = this.Trend
        member this.TrendClassification = this.TrendClassification
        member this.HealthScore = this.HealthScore
