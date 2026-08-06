---
name: openai-plugin-bridge
description: Build and govern HELIOS OpenAI Agents SDK, Codex task, ChatGPT App, and MCP tool connections with secure key references and approval-gated writes.
---

# OpenAI Agent and Plugin Bridge

## Trigger

Use for OpenAI provider setup, Agents SDK orchestration, Codex task packaging, tracing/evaluation, ChatGPT App tooling, MCP servers, or custom plugin-style connections.

## Configuration contract

Use environment or Azure Key Vault references only:

```text
OPENAI_API_KEY
OPENAI_BASE_URL
OPENAI_PROJECT_ID
OPENAI_ORG_ID
```

The governed key name is `HELIOS Codex`. Never print, commit, log, echo, or return the key.

## OpenAI Provider Agent

The provider agent may:

- select an approved model for a declared task class;
- call explicitly registered tools;
- hand off to bounded specialist agents;
- emit traces, evaluation data, and usage telemetry;
- package GitHub or Linear work for Codex;
- return normalized results to Hermes and AIHub.

The provider agent may not:

- expose or retrieve the API key value;
- invent a production write tool;
- run arbitrary shell commands;
- bypass GitHub environment approval;
- grant OAuth or Azure permissions;
- publish private memory or user data.

## Agent definition requirements

Every agent must declare:

- stable ID and owner;
- model policy;
- instructions and task boundary;
- allowed tools;
- denied operations;
- handoff targets;
- required approvals;
- input and output schemas;
- trace/evaluation policy;
- secret references;
- timeout, retry, and idempotency behavior.

## Tool design

Prefer narrow domain tools such as:

- `azure_plan_what_if`
- `github_package_codex_task`
- `linear_sync_issue`
- `teams_post_ops_event`
- `sharepoint_publish_evidence`
- `slack_post_incident`
- `hermes_register_worker`
- `aihub_health_check`

Avoid generic tools such as unrestricted `http_request`, `run_shell`, or `execute_sql`.

Write-capable tools must:

1. describe the external effect;
2. validate the environment;
3. accept a correlation ID and idempotency key;
4. expose a dry-run/preview where possible;
5. return an evidence URL;
6. declare approval requirements;
7. reject secret-bearing payloads.

## ChatGPT App and MCP connection

A custom HELIOS connection consists of:

- an MCP server exposing declared tools/resources;
- optional widget UI for review and approvals;
- OAuth or managed-identity configuration;
- tool annotations distinguishing read and write operations;
- a connector registry entry;
- health and negative tests;
- production publishing review.

The MCP server should broker existing HELIOS services rather than duplicating Azure, Graph, GitHub, Linear, Slack, Teams, or SharePoint authorization logic.

## Codex task packaging

A Codex-ready task must include:

- objective;
- source issue and branch;
- files to inspect;
- architecture constraints;
- prohibited changes;
- acceptance criteria;
- validation commands;
- required evidence;
- rollback considerations.

## Tracing and evaluation

Record:

- correlation ID;
- agent and tool IDs;
- handoffs;
- latency and token/usage metrics;
- success/failure category;
- validation outcome;
- evidence links.

Do not record raw secrets or unnecessary user content.

## Production gate

Production OpenAI tool writes require:

- reviewed registry change;
- secure key reference available;
- connector health check;
- approval-capable UI or protected workflow environment;
- successful negative tests proving denied operations remain denied.
