# HELIOS deployment-planning agent

This service turns a bounded repository manifest into an evidence-linked implementation plan using
the OpenAI Agents SDK. It is intentionally **plan-only**. Its tools summarize caller-supplied
non-secret manifests and evaluate proposed actions against deterministic policy; they do not call
GitHub, Azure, SharePoint, Slack, Linear, Microsoft Graph, or a USB device.

## Safety boundary

Read-only analysis and build validation may be planned automatically. Draft PR creation and status
notifications require a recorded human approval. Merge, branch deletion, repository archive,
deployment, RBAC, Graph writes, and USB writes require the controls in `policy.py`. Secret readback,
secret echo, self-approval, branch-protection bypass, and direct production apply are denied.
A verdict of `allow_plan` never means `execute`.

## Local run

Use Python 3.12 or later. Keep the real key only in the repository-root `.env.local`, which must stay
untracked.

```bash
python -m venv .venv
. .venv/bin/activate
python -m pip install -e '.[dev]'
set -a
. ../../.env.local
set +a
uvicorn helios_deployment_agent.main:app --reload --port 8080
```

Run deterministic checks without an API request:

```bash
python -m unittest discover -s tests -v
python evals/run_local.py
```

After those pass, run the optional live smoke:

```bash
python evals/run_local.py --live
```

## Azure connection contract

GitHub Actions authenticates to Azure with workload identity federation. Configure only the
non-secret `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, and `AZURE_SUBSCRIPTION_ID` repository variables;
do not create a long-lived Azure client secret. The deployed Container App uses managed identity.
Store `OPENAI_API_KEY` in Azure Key Vault and expose it to the container through a Key Vault secret
reference—never through source, workflow output, issue text, or a prompt.

The production path is Front Door/WAF to private ingress (optionally through API Management) to an
internal Container Apps environment. Azure Monitor receives correlated, redacted telemetry. A Dev
Tunnel is permitted only for local development and must never be the production endpoint.

Before any deployment workflow is enabled:

1. create a GitHub OIDC federated credential scoped to the exact repository and protected
   environment;
2. assign the smallest Azure role at the narrowest resource-group scope;
3. require the `HELIOS CI / required` context and environment approval;
4. run Bicep lint/validate/what-if and retain the deployment evidence;
5. deploy to staging, probe `/health`, canary, monitor, and preserve the last known-good revision.

## API

- `GET /health` reports service state and whether OpenAI configuration is present, never its value.
- `POST /plan` accepts `objective`, `manifest`, and boolean `controls`, and returns plan text plus
  `execution_mode: plan-only`.

The caller must omit secrets and credentials from the manifest. Sensitive-looking manifest keys are
rejected by the inspection tool.
