# Branch Intelligence Graphs

## Branch to module

```mermaid
graph TD
  work["work (65)"]
  work --> _devcontainer[".devcontainer"]
  work --> _github_workflows[".github/workflows"]
  work --> config["config"]
  work --> docs_integration["docs/integration"]
  work --> infra["infra"]
  work --> reports["reports"]
  work --> scripts["scripts"]
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
  Q1["extract-ideas 87"] --> M1[".github/workflows"]
  Q2["extract-ideas 85"] --> M2["scripts"]
  Q3["extract-ideas 77"] --> M3["docs/integration"]
  Q4["extract-ideas 69"] --> M4[".devcontainer"]
  Q5["extract-ideas 69"] --> M5["config"]
  Q6["extract-ideas 69"] --> M6["infra"]
  Q7["extract-ideas 69"] --> M7["reports"]
  Q8["idea-review 23"] --> M8[".github"]
  Q9["idea-review 23"] --> M9[".github"]
  Q10["idea-review 23"] --> M10[".github"]
  Q11["idea-review 23"] --> M11[".github"]
  Q12["idea-review 23"] --> M12[".github"]
  Q13["idea-review 23"] --> M13[".github"]
  Q14["idea-review 23"] --> M14[".github"]
  Q15["idea-review 23"] --> M15[".github"]
  Q16["idea-review 23"] --> M16[".nuget"]
  Q17["idea-review 23"] --> M17[".nuget"]
  Q18["idea-review 23"] --> M18[".nuget"]
  Q19["idea-review 23"] --> M19["ACCESSIBILITY_COMPLIANCE_REPORT.md"]
  Q20["idea-review 23"] --> M20["ACCESSIBILITY_COMPLIANCE_REPORT.md"]
  Q21["idea-review 23"] --> M21["ACTUAL_PROJECT_STATUS.md"]
  Q22["idea-review 23"] --> M22["ACTUAL_PROJECT_STATUS.md"]
  Q23["idea-review 23"] --> M23["ACTUAL_PROJECT_STATUS.md"]
  Q24["idea-review 23"] --> M24["ACTUAL_PROJECT_STATUS.md"]
  Q25["idea-review 23"] --> M25["AI_CODE_QUALITY_TRAINER_COMPLETION.md"]
```
