namespace HELIOS.Analytics.FSharp.AIHub

open System

/// Signals shared by C# orchestration, Hermes/XCore agents, and learning reports.
type AIHubLearningSignals = {
    Novelty: float
    TestConfidence: float
    CostEfficiency: float
    Speed: float
    Quality: float
    ReusePotential: float
    RiskPenalty: float
}

/// Branch/module issue shape used by the learning engine to select Hermes/XCore specialties.
type AIHubIssueSignals = {
    Path: string
    Language: string
    FailingChecks: int
    ChangedLines: int
    HotPathPressure: float
    LearningValue: float
    SecurityRisk: float
}


/// Situation variables from self-learning notes that influence grading and routing.
type AIHubSelfLearningSituation = {
    BranchPressure: float
    MergeDistance: float
    ScaffoldGap: float
    EmptyFileRisk: float
    IncomingUniqueness: float
    LocalStability: float
    AgentConfidence: float
    EngineMismatch: float
    HotPathUrgency: float
    SecuritySensitivity: float
    GuiUserImpact: float
    DocsMemoryValue: float
    CloudReadiness: float
    VaultDependency: float
    TestCoverageGap: float
    NamingDrift: float
    ModuleBoneStrength: float
    SubmoduleJointFit: float
    CopyRoundValue: float
    DeleteReviewNeed: float
}

/// F# deep-learning/prediction scoring helpers for module and submodule placement.
module AiHubLearningEngine =
    let private clamp value = Math.Max(0.0, Math.Min(1.0, value))

    let score signals =
        (signals.Novelty * 0.18) +
        (signals.TestConfidence * 0.20) +
        (signals.CostEfficiency * 0.12) +
        (signals.Speed * 0.13) +
        (signals.Quality * 0.20) +
        (signals.ReusePotential * 0.12) -
        (signals.RiskPenalty * 0.15)
        |> clamp

    let classify score =
        if score >= 0.85 then "absorb-now"
        elif score >= 0.70 then "test-and-merge"
        elif score >= 0.50 then "keep-for-learning"
        else "prune-or-rewrite"

    let rank candidates =
        candidates
        |> Seq.map (fun (name, signals) -> name, score signals, classify (score signals))
        |> Seq.sortByDescending (fun (_, value, _) -> value)
        |> Seq.toArray

    let issuePriority issue =
        let failurePressure = float issue.FailingChecks / 8.0 |> clamp
        let churn = float issue.ChangedLines / 500.0 |> clamp
        (failurePressure * 0.26) +
        (issue.HotPathPressure * 0.20) +
        (issue.LearningValue * 0.22) +
        (issue.SecurityRisk * 0.20) +
        (churn * 0.12)
        |> clamp

    let selectSpecialty issue =
        let language = issue.Language.ToLowerInvariant()
        if language.Contains("c++") || issue.HotPathPressure > 0.72 then "xcore-native-performance"
        elif language.Contains("f#") || issue.LearningValue > 0.78 then "xcore-fsharp-learning"
        elif language.Contains("c#") then "hermes-csharp-orchestrator"
        elif language.Contains("yaml") || language.Contains("bicep") || language.Contains("json") then "hermes-control-plane"
        else "hermes-aihub-integration"

    let rankIssues issues =
        issues
        |> Seq.map (fun issue -> issue.Path, issuePriority issue, selectSpecialty issue)
        |> Seq.sortByDescending (fun (_, priority, _) -> priority)
        |> Seq.toArray

    let languageBalanceScore csharp cpp fsharp python =
        let total = csharp + cpp + fsharp + python
        if total <= 0.0 then 0.0 else
            let cppTarget = 0.32
            let fsharpTarget = 0.26
            let csharpTarget = 0.28
            let pythonTarget = 0.14
            let distance =
                Math.Abs((cpp / total) - cppTarget) +
                Math.Abs((fsharp / total) - fsharpTarget) +
                Math.Abs((csharp / total) - csharpTarget) +
                Math.Abs((python / total) - pythonTarget)
            clamp (1.0 - distance)

    let moduleFitScore security performance learning providerGlue =
        (security * 0.25) + (performance * 0.30) + (learning * 0.30) + (providerGlue * 0.15)
        |> clamp

    let absorptionScore novelty securityRisk performanceValue learningValue failingChecks changedLines =
        let failurePenalty = float failingChecks / 10.0 |> clamp
        let churnPenalty = float changedLines / 1200.0 |> clamp
        (novelty * 0.25) +
        (performanceValue * 0.20) +
        (learningValue * 0.30) -
        (securityRisk * 0.18) -
        (failurePenalty * 0.14) -
        (churnPenalty * 0.08)
        |> clamp

    let absorptionAction score =
        if score >= 0.82 then "absorb-and-test"
        elif score >= 0.64 then "isolate-and-specialize"
        elif score >= 0.42 then "record-abstract-idea"
        else "prune-or-rewrite"

    let finishReadinessScore absorptionScore branchTestScore dashboardFreshness liveFlagSafety vaultReadiness =
        (absorptionScore * 0.28) +
        (branchTestScore * 0.24) +
        (dashboardFreshness * 0.16) +
        (liveFlagSafety * 0.18) +
        (vaultReadiness * 0.14)
        |> clamp

    let finishReadinessLabel score =
        if score >= 0.88 then "ready-for-scoped-live-automation"
        elif score >= 0.70 then "ready-for-local-autofix-and-pr"
        elif score >= 0.52 then "ready-for-report-only-learning"
        else "needs-more-branch-absorption-data"

    let complexCodeGradeScore uniqueTokenRatio duplicateLineRatio complexityScore maintainabilityScore reuseScore securitySignal performanceSignal learningSignal =
        (uniqueTokenRatio * 0.16) -
        (duplicateLineRatio * 0.18) -
        (complexityScore * 0.10) +
        (maintainabilityScore * 0.18) +
        (reuseScore * 0.20) +
        (securitySignal * 0.08) +
        (performanceSignal * 0.10) +
        (learningSignal * 0.10)
        |> clamp

    let complexCodeGradeAction score duplicateLineRatio =
        if score >= 0.74 then "keep-unique-good-part"
        elif score >= 0.58 && duplicateLineRatio < 0.40 then "absorb-small-idea-through-fsharp-score"
        elif score < 0.42 && duplicateLineRatio >= 0.42 then "prune-or-rewrite-trash-after-csharp-review"
        else "review-with-fsharp-and-csharp"


    let situationScore situation =
        (situation.BranchPressure * 0.06) +
        ((1.0 - situation.MergeDistance) * 0.05) +
        ((1.0 - situation.ScaffoldGap) * 0.05) +
        ((1.0 - situation.EmptyFileRisk) * 0.04) +
        (situation.IncomingUniqueness * 0.07) +
        (situation.LocalStability * 0.08) +
        (situation.AgentConfidence * 0.08) +
        ((1.0 - situation.EngineMismatch) * 0.05) +
        (situation.HotPathUrgency * 0.05) +
        ((1.0 - situation.SecuritySensitivity) * 0.04) +
        (situation.GuiUserImpact * 0.05) +
        (situation.DocsMemoryValue * 0.04) +
        (situation.CloudReadiness * 0.04) +
        ((1.0 - situation.VaultDependency) * 0.03) +
        ((1.0 - situation.TestCoverageGap) * 0.05) +
        ((1.0 - situation.NamingDrift) * 0.04) +
        (situation.ModuleBoneStrength * 0.07) +
        (situation.SubmoduleJointFit * 0.07) +
        (situation.CopyRoundValue * 0.05) +
        ((1.0 - situation.DeleteReviewNeed) * 0.04)
        |> clamp

    let selfLearningGradeBoost noteHit thoughtTierHit connectionIdea compositePart situation =
        let memory =
            (float noteHit * 0.08) +
            (float thoughtTierHit * 0.025) +
            (float connectionIdea * 0.05) +
            (float compositePart * 0.06)
            |> clamp
        ((situationScore situation) * 0.55) + (memory * 0.45)
        |> clamp

    let selfLearningAction situation =
        let score = situationScore situation
        if situation.DeleteReviewNeed > 0.74 then "hold-for-delete-review"
        elif situation.EngineMismatch > 0.68 then "reroute-engine-before-merge"
        elif situation.ScaffoldGap > 0.62 || situation.EmptyFileRisk > 0.62 then "write-scaffold-note-first"
        elif score >= 0.78 then "promote-learning-connection"
        else "keep-learning-note"
