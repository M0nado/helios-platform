# HELIOS workspace readiness — September 5, 2026

This record distinguishes workbench configuration from account authorization and live cloud deployment. It is not a production approval.

## What changed in GitHub

Continued [Yolkster64/helios-platform PR #135](https://github.com/Yolkster64/helios-platform/pull/135), without opening a duplicate PR or issue.

- Inspected workbench main: `c21ec24f41e61941c7f335b9835aba965c8f298b`.
- Reconciliation commit: `19e42cb6d151a734b73d02a3a535704a8b432df4`.
- New setup/documentation head: `cd4958cfddd5c205377226bcb3d5b3f6f06fbdbb`.
- PR #135 was conflicted and is now reported mergeable; it remains draft/unmerged.
- Preserved the exact already-merged deterministic fleet outcome implementation, blob `2711a4a9210cfa51e2360780a88e76a5fa0b2ceb`, instead of retaining the PR's temporary null-ID compatibility edit.
- Added [Codex workspace readiness skill](https://github.com/Yolkster64/helios-platform/blob/cd4958cfddd5c205377226bcb3d5b3f6f06fbdbb/.agents/skills/helios-workspace-ready/SKILL.md).
- Added [verified setup and source-of-truth decisions](https://github.com/Yolkster64/helios-platform/blob/cd4958cfddd5c205377226bcb3d5b3f6f06fbdbb/docs/CANONICAL_DECISIONS.md).

PR #135 already contains the .NET 10 Codespace correction, target-framework-derived CLI path, required failure propagation, and `setup-everything.ps1 -RequireReady`. Its changes are not on main merely because this readiness checkpoint exists.

## Actual source-of-truth distinction

The live workbench README identifies `Yolkster64/helios-platform` as the active development home. The existing owner-scoped SharePoint authority has a July 28 cutoff and still identifies `M0nado/helios-platform` as enterprise execution authority. This record does not silently transfer cloud permissions, promotion authority, or federation subjects.

PR #113 remains the separate unmerged authentication/REST lane. The July instructions that restart #176/#177 are historical, not the current workbench bootstrap path.

## Start in the Codespace, not on the Windows workstation

Use an existing Codespace. For review, select `codex/setup-readiness-evidence`; after its required checks and review/merge, new Codespaces can use corrected main. A new Codespace can consume account quota/billing; none was created by this checkpoint.

The intended cloud setup executes the repository's existing scripts rather than a new installer:

```bash
git status --short
git rev-parse HEAD
dotnet --version
pwsh scripts/setup/setup-all.ps1
```

Only when you approve installing the missing AI CLIs:

```bash
pwsh scripts/setup/setup-all.ps1 -Fix
```

Then verify the trusted checkout with provider credentials absent:

```bash
dotnet build HELIOS.sln -c Release
dotnet test tests/HELIOS.AIHub.Tests -c Release --no-build
python3 -m pytest src/ai/python/tests -q
python3 plugins/helios-operator/tests/validate_contract.py
dotnet run --project src/ai/HELIOS.AIHub.Cli -c Release --no-build -- status
pwsh scripts/bootstrap/setup-everything.ps1 -RequireReady -Json
```

The strict option belongs to PR #135. The active SDK contract is .NET 10, not the older .NET 8 prerequisites paragraph. The Linux portable build does not validate the Windows-only WinUI solution or any physical USB/driver operation.

## Codex and Claude

Use Codex's ChatGPT sign-in for this keyless developer workflow; do not create another provider key just to inspect, build, or test the project.

```bash
codex login status
# Only when login is needed; finish browser authorization yourself.
codex login --device-auth
codex mcp list
```

Check existing registrations before adding one. A missing HELIOS registration can be added from the repository root after building:

```bash
codex mcp add helios --env HELIOS_REPO_ROOT="$PWD" -- \
  dotnet run --project "$PWD/src/mcp/HELIOS.Mcp" -c Release --no-build
```

Claude uses the existing root `.mcp.json` plus the merged operator plugin:

```bash
claude --plugin-dir ./plugins/helios-operator
```

In Claude, use `/mcp` and `/helios-operator:operate-helios`. No second HELIOS MCP server should be added by the plugin. Start with status/routing/operator-next-step reads. Foundry agent creation, provider calls, training, and cloud changes are not keyless health tests.

Use separate branches/worktrees for concurrent Codex and Claude coding. Share approved task packets, commits, test evidence, and sanitized decisions; do not share credentials, private memory, or hidden conversations. The `.helios/operator/` state is local to the checkout, not automatically synchronized across computers.

Official client references: [Codex authentication](https://developers.openai.com/codex/auth), [Codex MCP](https://developers.openai.com/codex/mcp), [Claude plugins](https://code.claude.com/docs/en/plugins).

## Collaboration connections

ChatGPT connector access and client/runtime OAuth are separate.

| System | This checkpoint proved | Not established by that proof |
| --- | --- | --- |
| GitHub | Repository reads and existing-branch writes succeeded | Merge, deployment, organization administration |
| Slack | `#helios-control-plane` resolved, ID `C0BHWDBHG1W` | Claude/Codex OAuth, installed workflow workers |
| Linear | Existing JOH-35 readable and checkpoint comment saved | Native GitHub/Slack integrations or webhooks activated |
| SharePoint | Owner-scoped `Helios/Governance` readable | Dedicated `/sites/helios` site; it still returns 404 |
| Codex/Claude | Current source configuration and client setup verified | A running signed-in client inside this chat runtime |
| Azure/Foundry/azd | Existing source contracts inspected | Live login, tenant/subscription context, what-if, provisioning |
| Teams/M365/DevOps | Historical targets retained | New consent, package publication, service connection or pipeline writes |

For a missing Linear connection, [Linear's documented MCP setup](https://linear.app/docs/mcp) is:

```bash
codex mcp add linear --url https://mcp.linear.app/mcp
codex mcp login linear
claude mcp add --transport http --scope local linear https://mcp.linear.app/mcp
```

Finish Claude OAuth through `/mcp`. Reuse equivalent registrations instead of overwriting them. Slack and Microsoft systems also require the selected client's authorized OAuth flow; ChatGPT tokens must not be copied into the Codespace.

## Validation actually executed

- Four existing isolated bootstrap tests: passed.
- Four additional focused fleet-identity checks: passed.
- Bash syntax: passed.
- Retrieved bootstrap, test, and fleet source files: Git blob hashes match GitHub.

These tests use inert commands and temporary fixture data, not real .NET installation, provider inference, fleet training, or cloud services.

The previous PR head had eleven returned PR workflow runs, all successful. The new head requires its own CI. Its results must be read from GitHub, not inherited from that earlier head.

This chat runtime lacks `gh`, `az`, `azd`, .NET, PowerShell, Docker, Codex, and Claude executables. Outbound DNS to GitHub fails. Full live CLI setup, Windows builds, MCP startup, account login, cloud what-if, and end-to-end provider calls were therefore not executed here.

## Remaining gates

Review current-head CI for #135 and #113 independently. Complete browser sign-in in the actual Codespace/client. Verify one real read for each connection, and a separately approved write with a receipt where needed. Configure Azure identities/OIDC for the actual repository and environment, review a development what-if, then obtain a distinct deployment approval.

No duplicate PR/issue, merge, force-push, new credential, tenant consent, role change, Azure deployment, provider inference, Windows security mutation, or physical USB operation was performed by this checkpoint.
