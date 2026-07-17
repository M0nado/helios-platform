namespace HELIOS.Analytics.FSharp.Merge

module MergeScoring =
    type LanguageImpact = { Language: string; Score: int; RequiredChecks: string list; OwnerAgent: string }
    type MergeDecision = { Branch: string; Score: int; Impacts: LanguageImpact list; RecommendedParty: string; Blocked: bool }

    let private partyFor (impacts: LanguageImpact list) =
        let languages = impacts |> List.map (fun impact -> impact.Language) |> Set.ofList
        if languages.Contains "csharp" && languages.Contains "fsharp" && languages.Contains "cpp" then "full-merge-raid"
        elif languages.Contains "csharp" then "core-stability-party"
        elif languages.Contains "fsharp" then "analytics-party"
        elif languages.Contains "cpp" then "performance-party"
        elif languages.Contains "bicep" then "cloud-setup-party"
        else "aihub-party"

    let decide branch baseScore (impacts: LanguageImpact list) =
        let languageBoost = impacts |> List.sumBy (fun impact -> impact.Score)
        let total = baseScore + languageBoost
        { Branch = branch; Score = total; Impacts = impacts; RecommendedParty = partyFor impacts; Blocked = total < 0 }
