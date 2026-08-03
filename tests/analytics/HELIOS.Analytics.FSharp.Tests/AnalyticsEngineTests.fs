namespace HELIOS.Analytics.FSharp.Tests

open System
open System.Collections.Generic
open System.Threading
open System.Threading.Tasks
open HELIOS.Analytics.FSharp
open HELIOS.Analytics.FSharp.Models
open HELIOS.Platform.Contracts.Analytics
open Xunit

module AnalyticsEngineTests =
    [<Fact>]
    let ``Summarize returns central tendency spread and percentiles`` () =
        let engine = AnalyticsEngine()
        let summary = engine.Summarize([ 1.0; 2.0; 3.0; 4.0 ])

        Assert.Equal(4, summary.Count)
        Assert.Equal(2.5, summary.Mean, 6)
        Assert.Equal(2.5, summary.Median, 6)
        Assert.Equal(1.25, summary.Variance, 6)
        Assert.Equal(3.85, summary.Percentiles["P95"], 6)

    [<Fact>]
    let ``Math helpers expose weighted mean correlation and rolling windows`` () =
        let engine = AnalyticsEngine()

        Assert.Equal(25.0, engine.WeightedMean([ 10.0; 20.0; 30.0 ], [ 1.0; 1.0; 4.0 ]), 5)
        Assert.Equal(1.0, engine.Correlation([ 1.0; 2.0; 3.0 ], [ 2.0; 4.0; 6.0 ]), 6)
        Assert.Equal(2, engine.RollingWindows([ 1.0; 2.0; 3.0 ], 2).Length)

    [<Fact>]
    let ``Moving average and exponential smoothing forecasts include diagnostics`` () =
        let engine = AnalyticsEngine()
        let movingAverage = engine.MovingAverageForecast([ 10.0; 20.0; 30.0; 40.0 ], 2)
        let smoothed = engine.ExponentialSmoothingForecast([ 10.0; 20.0; 30.0 ], 0.5)

        Assert.Equal("moving-average", movingAverage.Model)
        Assert.Equal(35.0, movingAverage.PredictedValue, 6)
        Assert.True(movingAverage.Diagnostics.ContainsKey("volatility"))
        Assert.Equal("exponential-smoothing", smoothed.Model)
        Assert.True(smoothed.Confidence > 0.0)

    [<Fact>]
    let ``Linear forecast projects next trend with rmse`` () =
        let engine = AnalyticsEngine()
        let forecast = engine.LinearForecast([ 2.0; 4.0; 6.0 ])

        Assert.Equal(8.0, forecast.PredictedValue, 6)
        Assert.Equal(0.0, forecast.Diagnostics["rmse"], 6)

    [<Fact>]
    let ``BuildWindow computes trend classification and health over contract points`` () =
        let engine = AnalyticsEngine()
        let startedAt = DateTimeOffset.UtcNow
        let points : IMetricPoint list =
            [ 1.0; 2.0; 3.0 ]
            |> List.mapi (fun index value ->
                { Timestamp = startedAt.AddMinutes(float index)
                  Name = "cpu"
                  Value = value
                  Tags = Dictionary<string, string>() } :> IMetricPoint)

        let window = engine.BuildWindow("cpu", startedAt, startedAt.AddMinutes(3.0), points)

        Assert.Equal("cpu", window.Name)
        Assert.Equal(3, window.Summary.Count)
        Assert.Equal("increasing", window.TrendClassification)
        Assert.True(window.HealthScore > 0.0)

    [<Fact>]
    let ``Anomaly detection validates thresholds and returns zscore outliers`` () =
        let engine = AnalyticsEngine()
        let anomalies = engine.DetectAnomalies([ 10.0; 10.0; 10.0; 100.0 ], 1.5)

        let anomaly = Assert.Single(anomalies)
        Assert.Equal(100.0, anomaly.Value, 6)
        Assert.Throws<ArgumentException>(fun () -> engine.DetectAnomalies([ 1.0 ], 0.0) |> ignore) |> ignore

    [<Fact>]
    let ``Parallel helpers preserve order batch work and support async cancellation token`` () = task {
        let engine = AnalyticsEngine()
        let values = engine.ParallelMap([ 1; 2; 3 ], 2, Func<int, int>(fun value -> value * value))
        let batches = engine.ParallelBatches([ 1; 2; 3; 4; 5 ], 2)
        let! asyncValues =
            engine.ParallelMapAsync(
                [ 1; 2; 3 ],
                2,
                Func<int, CancellationToken, Task<int>>(fun value token -> Task.FromResult(value + 10)),
                CancellationToken.None)

        Assert.Equal<int array>([| 1; 4; 9 |], values)
        Assert.Equal(3, batches.Length)
        Assert.Equal<int array>([| 11; 12; 13 |], asyncValues)
    }
