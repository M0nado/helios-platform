namespace HELIOS.Analytics.FSharp.Statistics

module Statistics =
    let mean values =
        match values with
        | [] -> 0.0
        | xs -> xs |> List.average
