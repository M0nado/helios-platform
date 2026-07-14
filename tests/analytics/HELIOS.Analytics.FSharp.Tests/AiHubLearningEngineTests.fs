namespace HELIOS.Analytics.FSharp.Tests

open System
open HELIOS.Analytics.FSharp.AIHub
open Xunit

module AiHubLearningEngineTests =
    let private signals novelty testConfidence costEfficiency speed quality reusePotential riskPenalty =
        {
            Novelty = novelty
            TestConfidence = testConfidence
            CostEfficiency = costEfficiency
            Speed = speed
            Quality = quality
            ReusePotential = reusePotential
            RiskPenalty = riskPenalty
        }

    [<Fact>]
    let ``Rank orders candidates by their computed score`` () =
        let candidates =
            [
                "learning-only", signals 0.35 0.45 0.40 0.30 0.40 0.80 0.20
                "merge-ready", signals 0.90 0.95 0.80 0.85 0.95 0.80 0.05
                "rewrite", signals 0.10 0.10 0.20 0.15 0.10 0.10 0.90
            ]

        let ranked = AiHubLearningEngine.rank candidates
        let _, firstScore, _ = ranked[0]
        let _, secondScore, _ = ranked[1]
        let _, thirdScore, _ = ranked[2]

        Assert.Equal<string array>([| "merge-ready"; "learning-only"; "rewrite" |], ranked |> Array.map (fun (name, _, _) -> name))
        Assert.True(firstScore > secondScore)
        Assert.True(secondScore > thirdScore)

    [<Fact>]
    let ``Risk penalty lowers otherwise identical candidate score`` () =
        let lowRisk = signals 0.80 0.80 0.80 0.80 0.80 0.80 0.0
        let highRisk = { lowRisk with RiskPenalty = 1.0 }

        let lowRiskScore = AiHubLearningEngine.score lowRisk
        let highRiskScore = AiHubLearningEngine.score highRisk

        Assert.Equal(0.15, lowRiskScore - highRiskScore, 10)
        Assert.True(lowRiskScore > highRiskScore)

    [<Fact>]
    let ``Score handles non-finite inputs without emitting a non-finite rank`` () =
        let malformed =
            signals Double.NaN Double.PositiveInfinity Double.NegativeInfinity 0.5 0.5 0.5 Double.NaN

        let value = AiHubLearningEngine.score malformed

        Assert.True(Double.IsFinite(value))
        Assert.InRange(value, 0.0, 1.0)
        // NaN rewards contribute nothing and an unknown risk fails closed at full penalty.
        Assert.Equal(0.275, value, 10)

    [<Fact>]
    let ``Golden learning vector keeps its score and action stable`` () =
        let golden = signals 0.80 0.90 0.75 0.60 0.95 0.70 0.20

        let value = AiHubLearningEngine.score golden

        Assert.Equal(0.736, value, 10)
        Assert.Equal("test-and-merge", AiHubLearningEngine.classify value)

    [<Fact>]
    let ``Incomplete readiness stays report only`` () =
        let value = AiHubLearningEngine.finishReadinessScore 0.60 0.55 0.65 0.70 0.50

        Assert.Equal("ready-for-report-only-learning", AiHubLearningEngine.finishReadinessLabel value)
