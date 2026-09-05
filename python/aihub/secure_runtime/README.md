# HELIOS Secure AIHub compatibility runtime

This module replaces direct use of the historical Python control-server prototype for local compatibility clients.

## Security contract

- Binds only to `127.0.0.1`, `::1`, or `localhost`.
- Refuses `0.0.0.0`, public IPs, hostnames, and wildcard binds.
- Exposes `/api/health` without authentication and requires a bearer token for every other route.
- Uses constant-time token comparison.
- Limits request size, JSON depth, prompt size, and request rate.
- Writes queues and proposals through atomic replacement.
- Never executes queued tasks.
- Converts the legacy training trigger into an approval-required proposal.
- Records administrative requests without executing them.
- Does not read back secrets or deploy cloud resources.

Cloud ingress belongs to the governed C# Integration Broker, not this local compatibility process.

## Start as a standard user

```powershell
pwsh -NoProfile -File .\scripts\aihub\Start-HeliosSecureAIHub.ps1 -InitializeToken
```

Health check:

```powershell
pwsh -NoProfile -File .\scripts\aihub\Start-HeliosSecureAIHub.ps1 -HealthCheck
```

The generated token is stored under the current user's local application-data directory and is never printed.

## Direct Python start

```powershell
$env:AIHUB_API_TOKEN = '<locally generated token>'
python -m python.aihub.secure_runtime --config config/aihub/secure-runtime.v1.json
```

Do not place the token in GitHub, Slack, Linear, SharePoint, Google Drive, screenshots, logs, or chat.

## Compatibility routes

| Route | Behavior |
|---|---|
| `GET /api/health` | public, redacted liveness only |
| `GET /api/status` | authenticated runtime status |
| `GET /api/tasks` | authenticated queue inspection |
| `POST /api/tasks` | authenticated queue-only task creation |
| `POST /api/train/trigger` | authenticated training proposal; no execution |
| `GET /api/admin-requests` | authenticated proposal inspection |
| `POST /api/admin-requests` | authenticated pending-approval record; no execution |

The old `0.0.0.0` prototype must remain under an inert reference/legacy boundary until every caller has migrated.
