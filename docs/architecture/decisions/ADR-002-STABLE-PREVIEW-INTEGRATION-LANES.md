# ADR-002: Stable and preview integration lanes

- **Status:** Accepted
- **Date:** 2026-08-09
- **Owners:** HELIOS platform architecture and Guardian

## Context

HELIOS integrates .NET, Azure AI Foundry, Microsoft Agent Framework, OpenAI,
Anthropic, LangChain, GitHub, Microsoft Graph, Fabric, Purview, and Copilot
Studio. Their release cadences and preview terms differ, so an accidental
preview transitive dependency can make production behavior unreproducible.

## Decision

The stable lane is the only artifact-producing lane. It uses the pinned .NET
SDK, repository NuGet version properties and lock files, `uv.lock`, and npm's
`package-lock.json`. Its compatibility manifest records supported target
frameworks, API versions, authentication mechanisms, and a deprecation-review
obligation for every integration. CI rejects prerelease markers and unsupported
frameworks in stable dependency inputs.

Preview evaluation is non-publishing and isolated in the container under
`eng/integration-lanes/preview`, or an equivalent disposable virtual
environment. Preview credentials use development identities and Key Vault;
preview output is never copied into release staging.

The .NET 11, C# preview, F# preview, WinUI 3, C++23 renderer, and Python 3.14
probes are compatibility experiments rather than product upgrades. Their jobs
compile from the preview tree, never upload artifacts, and assert that release
staging was not created. Provider experiments use a separate hashed Python
graph so Microsoft Agent Framework, Foundry, OpenAI, Anthropic, LangChain,
Microsoft Graph, Copilot Studio, and Purview transitive previews cannot enter
the stable application lock files.

## Promotion gate

A preview can move to stable only through a new accepted ADR that records the
owner, use case, support status, API/auth changes, dependency diff, security and
rollback review, and deprecation plan. The change must update every applicable
lock file and stable manifest entry. The lane validator, normalized event
contract test, affected builds, vulnerability review, and a clean release
artifact inspection must pass before approval.

Release workflows run the artifact inspector after distribution verification
and before publishing. The inspector rejects preview-lane paths and prerelease-
named files, including entries nested in ZIP, NuGet, and tar archives. Stable CI
also performs locked restores, npm vulnerability auditing, NuGet deprecation
reporting, and executable .NET contract tests.

## Consequences

Experiments may move quickly without changing product dependencies. Promotion
is intentionally slower and reviewable. API retirement notices are reviewed at
least quarterly and before each release; a notice creates a tracked migration
issue with an owner and deadline.

Developer tool upgrades are separate from tenant or repository provisioning.
The Windows bootstrap is audit-only by default and requires an explicit package
manager mode plus PowerShell confirmation. GitHub forks, Azure DevOps projects,
Graph consent, SharePoint/Purview policy changes, and Azure deployments remain
approval-gated external operations.
