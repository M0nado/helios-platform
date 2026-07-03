namespace HELIOS.Analytics.FSharp.Learning

module LearningSummary =
    type LearningEvent = { Language: string; Success: bool; Cost: float; RuntimeMilliseconds: int }
    type LanguageSummary = { Language: string; Successes: int; Failures: int; AverageRuntimeMilliseconds: float; TotalCost: float }

    let summarize (events: LearningEvent list) =
        events
        |> List.groupBy _.Language
        |> List.map (fun (language, items) ->
            let successes = items |> List.filter _.Success |> List.length
            let failures = items.Length - successes
            let averageRuntime = if items.IsEmpty then 0.0 else items |> List.averageBy (fun e -> float e.RuntimeMilliseconds)
            let totalCost = items |> List.sumBy _.Cost
            { Language = language; Successes = successes; Failures = failures; AverageRuntimeMilliseconds = averageRuntime; TotalCost = totalCost })

    let nextBestLanguage summaries =
        summaries
        |> List.sortByDescending (fun s -> (s.Successes - s.Failures, -s.AverageRuntimeMilliseconds))
        |> List.tryHead
