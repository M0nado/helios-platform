# Whole-Project Next Steps

Use these as the next major work packages after the current command-center foundation.

## 1. Normalize all entry points

Move every safe script into one of these command-center categories: setup, status, github, azure, ai, branches, build, codex, recommendations, actions, dashboard.

## 2. Add drift reports

Create GitHub and Azure drift reports comparing actual inventory to desired-state manifests before any apply mode exists.

## 3. Add provider router

Unify OpenAI, Azure OpenAI, Claude, Codex, Microsoft 365 Copilot, and Slack readiness behind a provider manifest with opt-in execution.

## 4. Add hybrid app hosting

Extend Azure Bicep with optional modules for app hosting, private endpoints, self-hosted runners, and observability dashboards.

## 5. Add Visual Studio solution organization

Create solution filters or documented solution views for platform core, analytics, native/XCore, security, installer, tests, and future MAUI UI.

## 6. Connect F# analytics to scoring

Move branch/module ranking calculations from Python-only heuristics toward reusable analytics outputs from `HELIOS.Analytics.FSharp`.

## 7. Build language-specific CI lanes

Separate CI checks for C#, F#, C++, Python, Bicep, PowerShell, docs, and generated dashboard artifacts.

## 8. Reduce generated report bloat

Keep full JSON as artifacts where possible and commit only compact examples or summaries when the repo is ready for stricter hygiene.
