namespace HELIOS.Analytics.FSharp

open System
open System.Threading
open System.Threading.Tasks
open HELIOS.Analytics.FSharp.Models
open HELIOS.Analytics.FSharp.Prediction
open HELIOS.Analytics.FSharp.Statistics
open HELIOS.Platform.Contracts.Analytics

/// C#-friendly facade exposing public HELIOS analytics APIs.
type AnalyticsEngine() =
    member _.Summarize(values: seq<float>) = MathWorkloads.summarize values
    member _.Percentile(values: seq<float>, percentileRank: float) = MathWorkloads.percentile percentileRank values
    member _.WeightedMean(values: seq<float>, weights: seq<float>) = MathWorkloads.weightedMean values weights
    member _.Correlation(left: seq<float>, right: seq<float>) = MathWorkloads.correlation left right
    member _.RollingWindows(values: seq<float>, windowSize: int) = MathWorkloads.rollingWindow windowSize values
    member _.DetectAnomalies(values: seq<float>, threshold: float) = AnalyticsWorkloads.anomalies threshold values
    member _.NormalizeMinMax(values: seq<float>) = AnalyticsWorkloads.normalizeMinMax values
    member _.BuildWindow(name: string, startedAt: DateTimeOffset, endedAt: DateTimeOffset, points: seq<#IMetricPoint>) =
        AnalyticsWorkloads.window name startedAt endedAt points
    member _.MovingAverageForecast(values: seq<float>, period: int) = PredictionWorkloads.movingAverageForecast period values
    member _.LinearForecast(values: seq<float>) = PredictionWorkloads.linearForecast values
    member _.ExponentialSmoothingForecast(values: seq<float>, alpha: float) = PredictionWorkloads.exponentialSmoothingForecast alpha values
    member _.ParallelMap(values: seq<'T>, degreeOfParallelism: int, work: Func<'T, 'U>) =
        ParallelWorkloads.map degreeOfParallelism work.Invoke values
    member _.ParallelBatches(values: seq<'T>, batchSize: int) = ParallelWorkloads.batches batchSize values
    member _.ParallelMapAsync(values: seq<'T>, degreeOfParallelism: int, work: Func<'T, CancellationToken, Task<'U>>, cancellationToken: CancellationToken) =
        ParallelWorkloads.mapAsync degreeOfParallelism cancellationToken (fun item token -> work.Invoke(item, token)) values
    interface IAnalyticsEngine with
        member this.Summarize(values) = this.Summarize(values) :> IStatisticalSummary
        member this.DetectAnomalies(values, threshold) = this.DetectAnomalies(values, threshold) |> Array.map (fun score -> score :> IAnomalyScore) :> System.Collections.Generic.IReadOnlyList<IAnomalyScore>
        member this.BuildWindow(name, startedAt, endedAt, points) = this.BuildWindow(name, startedAt, endedAt, points) :> IAnalyticsWindow
        member this.MovingAverageForecast(values, period) = this.MovingAverageForecast(values, period) :> IPredictionResult
        member this.LinearForecast(values) = this.LinearForecast(values) :> IPredictionResult
