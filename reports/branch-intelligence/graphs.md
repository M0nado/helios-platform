# Branch Intelligence Graphs

## Branch to module

```mermaid
graph TD
  work["work (74)"]
  work --> _devcontainer[".devcontainer"]
  work --> _github_workflows[".github/workflows"]
  work --> config["config"]
  work --> docs["docs"]
  work --> docs_integration["docs/integration"]
  work --> infra["infra"]
  work --> reports["reports"]
  work --> root["root"]
  work --> scripts["scripts"]
  work --> src_analytics["src/analytics"]
  work --> src_core["src/core"]
  work --> src_native["src/native"]
  work --> status_site["status-site"]
  work --> tests["tests"]
```

## Idea categories

```mermaid
graph LR
  fsharp_analytics["fsharp-analytics"] --> _github[".github"]
  fsharp_analytics["fsharp-analytics"] --> _github[".github"]
  xcore["xcore"] --> _github[".github"]
  hermes["hermes"] --> _github[".github"]
  fsharp_analytics["fsharp-analytics"] --> _github[".github"]
  fsharp_analytics["fsharp-analytics"] --> _github[".github"]
  fsharp_analytics["fsharp-analytics"] --> _github[".github"]
  fsharp_analytics["fsharp-analytics"] --> _github[".github"]
  hermes["hermes"] --> _nuget[".nuget"]
  hermes["hermes"] --> _nuget[".nuget"]
  fsharp_analytics["fsharp-analytics"] --> _nuget[".nuget"]
  xcore["xcore"] --> ACCESSIBILITY_COMPLIANCE_REPORT_md["ACCESSIBILITY_COMPLIANCE_REPORT.md"]
  xcore["xcore"] --> ACCESSIBILITY_COMPLIANCE_REPORT_md["ACCESSIBILITY_COMPLIANCE_REPORT.md"]
  fsharp_analytics["fsharp-analytics"] --> ACTUAL_PROJECT_STATUS_md["ACTUAL_PROJECT_STATUS.md"]
  fsharp_analytics["fsharp-analytics"] --> ACTUAL_PROJECT_STATUS_md["ACTUAL_PROJECT_STATUS.md"]
  fsharp_analytics["fsharp-analytics"] --> ACTUAL_PROJECT_STATUS_md["ACTUAL_PROJECT_STATUS.md"]
  fsharp_analytics["fsharp-analytics"] --> ACTUAL_PROJECT_STATUS_md["ACTUAL_PROJECT_STATUS.md"]
  fsharp_analytics["fsharp-analytics"] --> AI_CODE_QUALITY_TRAINER_COMPLETION_md["AI_CODE_QUALITY_TRAINER_COMPLETION.md"]
  hermes["hermes"] --> AI_TRAINING_FLEET_md["AI_TRAINING_FLEET.md"]
  hermes["hermes"] --> AI_TRAINING_FLEET_md["AI_TRAINING_FLEET.md"]
  hermes["hermes"] --> AI_TRAINING_FLEET_md["AI_TRAINING_FLEET.md"]
  hermes["hermes"] --> AI_TRAINING_FLEET_md["AI_TRAINING_FLEET.md"]
  hermes["hermes"] --> AI_TRAINING_FLEET_md["AI_TRAINING_FLEET.md"]
  hermes["hermes"] --> AI_TRAINING_FLEET_md["AI_TRAINING_FLEET.md"]
  hermes["hermes"] --> AI_TRAINING_FLEET_md["AI_TRAINING_FLEET.md"]
```

## Agent queue

```mermaid
graph TD
  Q1["compare-selectively 104"] --> M1["src/core"]
  Q2["compare-selectively 102"] --> M2["src/analytics"]
  Q3["compare-selectively 98"] --> M3["tests"]
  Q4["compare-selectively 96"] --> M4[".github/workflows"]
  Q5["compare-selectively 94"] --> M5["scripts"]
  Q6["compare-selectively 94"] --> M6["src/native"]
  Q7["compare-selectively 86"] --> M7["docs/integration"]
  Q8["compare-selectively 79"] --> M8["docs"]
  Q9["compare-selectively 78"] --> M9[".devcontainer"]
  Q10["compare-selectively 78"] --> M10["root"]
  Q11["compare-selectively 78"] --> M11["config"]
  Q12["compare-selectively 78"] --> M12["infra"]
  Q13["compare-selectively 78"] --> M13["reports"]
  Q14["compare-selectively 78"] --> M14["status-site"]
  Q15["idea-review 23"] --> M15[".github"]
  Q16["idea-review 23"] --> M16[".github"]
  Q17["idea-review 23"] --> M17[".github"]
  Q18["idea-review 23"] --> M18[".github"]
  Q19["idea-review 23"] --> M19[".github"]
  Q20["idea-review 23"] --> M20[".github"]
  Q21["idea-review 23"] --> M21[".github"]
  Q22["idea-review 23"] --> M22[".github"]
  Q23["idea-review 23"] --> M23[".nuget"]
  Q24["idea-review 23"] --> M24[".nuget"]
  Q25["idea-review 23"] --> M25[".nuget"]
```
