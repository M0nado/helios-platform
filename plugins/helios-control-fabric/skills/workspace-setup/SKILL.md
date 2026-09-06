---
name: workspace-setup
description: Connect the existing HELIOS developer setup to the control-fabric readiness panel using bounded local reports, without granting or implying cloud authority.
---

# Workspace setup in the HELIOS control fabric

Use the existing Yolkster64/helios-platform setup work (PR #135), MCP verification
(PR #145), authentication work (PR #113), and Slack/Linear work (PR #136). Do not
replace these with a new installer. M0nado/helios-platform remains canonical execution
authority. Do not reuse an OIDC subject from another repository.

The existing fabric web wizard now links to `/wizard/setup/index.html`. It accepts
an operator-selected native JSON report from `setup-everything.ps1 -RequireReady -Json`.
The report remains in browser memory, is never uploaded, and expires after 30 minutes.
The browser does not run shell commands. Use the intended paired machine or trusted
Codespace for account sign-in, builds and local checks.

## Workflow

1. Confirm the trusted checkout and full `git rev-parse HEAD` before testing code.
2. Review the relevant PR and current checks. Do not merge or retarget another PR.
3. Reuse `scripts/setup/setup-all.ps1` to inspect missing tools. Install with `-Fix`
   only when the user requested installation; do not run login or secret loaders
   merely to inventory tools.
4. Run `scripts/bootstrap/setup-everything.ps1 -RequireReady -Json` without `-Apply`
   on the trusted workbench. This is report-only, not a cloud deployment.
5. Save the report under the ignored `.helios/setup/` directory, never in Git.
6. Load it in the setup panel with its repository and full commit SHA, or project it
   with this plugin's `scripts/workspace-readiness.mjs` using Node.js 22+:

```text
node plugins/helios-control-fabric/scripts/workspace-readiness.mjs --report <LOCAL_REPORT> --source-sha <FULL_WORKSPACE_COMMIT>
```

The helper reads a bounded regular local file; it never executes commands or calls
services. Exit 0 means imported workspace checks are ready, 2 means incomplete/stale,
and 1 means invalid/unavailable input. Even exit 0 always has `fullSetupReady=false`
and `deploymentAuthorized=false`.

`workspaceChecksReady` is an unattested claim derived from the input, not evidence
of an actual live provider. The SHA-256 identifies the bytes; it is not a signature.
Do not convert input summaries or owner-action strings into executable commands.
The projection discards them and reports only counts and allowlisted step statuses.

Use the existing Claude operator and one C# MCP registration per client. Share
sanitized task packets and commits with Codex; keep independent branches/worktrees.
Client OAuth, provider API access, connector delivery, and Azure deployment are
separate checks. Never capture or forward credentials, browser-pairing keys, or
raw private memory. Do not launch Hermes or XCore training during readiness checks.

Follow `docs/operations/WORKSPACE_FABRIC_SETUP.md` for the operator path. Preserve
archived evidence as historical; no successful archived check certifies a new SHA.
