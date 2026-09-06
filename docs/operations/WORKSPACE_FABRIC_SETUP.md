# HELIOS workspace setup connected to the control fabric

This change adds a workspace readiness view to the existing HELIOS web wizard and a
read-only report helper to the existing `helios-control-fabric` plugin. It does not
create another server, copy credentials between clients, or deploy the hosted site.

## Repository ownership and dependencies

- `M0nado/helios-platform`: canonical product/execution; this panel and fabric plugin.
- `Yolkster64/helios-platform`: active developer workbench; existing setup PR #135.
- Workbench #145: project MCP verification; #113: authentication; #136: Slack/Linear
  reliability, stacked on #135. They need their own current review/checks and landing.
- The reusable Claude `helios-operator` plugin remains the same implementation.
- `M0nado/Helios-Control-Center`: reviewed operator/promotion repository, not silently
  replaced. Its current main is a scaffold; this change uses the already implemented
  control application at `monado/helios-control` instead of adding another backend.

Work links: [setup #135](https://github.com/Yolkster64/helios-platform/pull/135),
[MCP #145](https://github.com/Yolkster64/helios-platform/pull/145),
[auth #113](https://github.com/Yolkster64/helios-platform/pull/113),
[connectors #136](https://github.com/Yolkster64/helios-platform/pull/136),
[canonical Codespaces issue #194](https://github.com/M0nado/helios-platform/issues/194).

## Open the panel

After this change is reviewed and the existing control app is built/served, open
`/wizard/setup/index.html` on that app's origin. The existing `/wizard/index.html` page links
there. This is an actual static page in the ASP.NET application's `wwwroot`, not a
claim that the separately hosted ChatGPT Site has been redeployed.

The supplied HELIOS Control Center description uses a separately paired local
`helios_bridge.py` on port 4318. Its source was not available in the inspected
repositories. This change does not replace that bridge, alter its origin policy, or
add a browser-to-shell executor. That panel can link to this setup route when its
owner deploys the existing control app. No localhost pairing key enters this report.

## Produce a fresh report in the trusted workbench

Use PowerShell 7 in a Codespace or the intended operator host. The strict option
requires PR #135's reviewed implementation, not the older script on workbench main.

```powershell
dotnet build HELIOS.sln -c Release
pwsh scripts/setup/setup-all.ps1
New-Item -ItemType Directory -Force .helios/setup | Out-Null
pwsh scripts/bootstrap/setup-everything.ps1 -RequireReady -Json |
    Set-Content -Encoding utf8 .helios/setup/readiness.json
git rev-parse HEAD
```

Inspect the command's exit code and report. Exit 2 means attention is required; do
not treat report generation as activation. Review before using `setup-all.ps1 -Fix`
to install missing AI CLIs. No login, -Apply, secret loading, inference, or training
command is automatically added by this feature. Building a checkout executes project
code: use trusted source or a credential-free disposable runner.

Choose `readiness.json` in the panel and enter the exact workspace repository/commit.
The page never uploads it, calls services, or stores it in local/session storage.
Raw summaries, owner-action commands and unknown fields are not rendered. Reports
older than 30 minutes or over one minute in the future cannot show ready.

## CLI / assistant bridge

The existing fabric plugin includes a Node.js 22+ helper:

```text
node plugins/helios-control-fabric/scripts/workspace-readiness.mjs --report /path/to/workbench/.helios/setup/readiness.json --source-sha FULL_40_CHARACTER_WORKSPACE_SHA
```

It defaults to `Yolkster64/helios-platform`; `--repository M0nado/helios-platform`
may be supplied when that is genuinely the report's source. It returns sanitized JSON
on stdout and reads no secrets, environment variables, auth caches or remote services.
The helper's exit codes are 0 (workspace checks ready), 2 (incomplete/stale), and 1
(invalid/unavailable). It does not write a report automatically.

`recordType=helios.workspace-readiness` is a local UI projection, **not** a deployment
approval or a normalized event sent to the broker. No collaboration event is emitted.
The supplied commit is operator metadata; the input hash is byte integrity, not origin
authentication. Never use this projection as an authorization gate.

The browser's `report.mjs` is a byte-identical distribution copy of plugin
`scripts/lib/workspace-report.mjs`. CI enforces parity. Both consumers enforce the
same allowlist, size, time, schema and readiness rules.

## Codex, Claude and the remaining services

Use the existing C# MCP server, Claude operator plugin and Codex readiness skill.
The setup panel links to the relevant current PRs rather than duplicating their
configuration or running unreviewed authentication code. Official Codex guidance
supports project MCP configuration and separate MCP OAuth authorization:
https://developers.openai.com/codex/mcp

GitHub/Codex/Claude sign-in, provider inference, Slack and Linear delivery, SharePoint
publication, Teams/Microsoft 365 Copilot consent, Agent 365 preparation, Azure/azd/
Foundry/Cloud Shell/DevOps context, and Hermes/XCore/AIHub runtime proof remain separate.
Every `liveVerification` field says `not-proven-by-report`; `fullSetupReady` and
`deploymentAuthorized` are always false. This does not mean those systems are known
to be offline. It means this report cannot prove their live state.

No new MCP remote tool, OAuth scope or secret is added. Hosted MCP clients continue
using the existing authorization flow, not an OpenAI API key as user identity:
https://developers.openai.com/apps-sdk/build/auth

## Supplied evidence imported unchanged

`docs/evidence/workspace-2026-09-05/` preserves all three supplied readiness artifacts:
original runbook, its checksum file, and original ZIP. `manifest.json` records exact
sizes and SHA-256 values. The ZIP's code snapshots remain archived reference evidence;
they are not promoted to runtime source. Earlier PR/test claims remain historical.

## Validation and rollout

```text
node --test plugins/helios-control-fabric/tests/workspace-readiness.test.mjs
```

The tests exercise the actual shared projection and CLI. Required hosted CI also
checks attachment integrity. No native C# code or Azure template changes are made.
Full ASP.NET serving and visual browser QA must still be checked in the normal app
build/review path. Publication is a draft PR, not a production-site update.

Component Version Check validates versioned manifests in this repository, not
private satellite checkouts. It validates the repository registry and pinned
gitlinks without fetching submodules. Checksum inventories under `docs/evidence/`
are excluded from component versioning; the workspace workflow verifies their
bytes and hashes separately. Missing versions in component manifests still fail.

Merge readiness requires successful checks on the current PR head, not historical
evidence. A skipped validation job caused by an upstream failure is a blocker.
Azure what-if and deployment jobs intentionally require an explicit workflow
dispatch and protected-environment approval; they remain skipped on pull requests.
Do not remove those gates to make a PR appear green. The legacy Node module-build
matrix has a limited discovery scope and is not proof that platform runtimes
built; the required polyglot workflow independently builds and tests them.

Rollback: revert this additive panel/helper change. There are no tenant, identity,
storage, webhook, service-connection, model or device changes to reverse.
