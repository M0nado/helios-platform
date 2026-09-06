# AIHub Secure Runtime V1

## Purpose

The secure runtime is the supported local compatibility boundary for the older XTier/AIHub prototype. It preserves health, status, task queueing, and training proposals without preserving the prototype's public listener or unauthenticated write surface.

## Mandatory defaults

```text
listener                 loopback only
protected routes         bearer token required
request body             64 KiB default, 1 MiB hard maximum
request rate             60 requests/minute default
state writes             atomic temporary file + replace
execution                queue only
training                 proposal only
shell bridge             unavailable
cloud deployment         unavailable
privileged Windows work  unavailable
production               disabled
```

The token is supplied through `AIHUB_API_KEY` or a local path referenced by `AIHUB_API_KEY_FILE`. A token value must never be committed to the repository, printed in logs, returned through health/status endpoints, or published to collaboration systems.

## Endpoints

| Method | Route | Authentication | Behavior |
|---|---|---:|---|
| `GET` | `/health` | No | Minimal health and security posture |
| `GET` | `/api/health` | No | Minimal health and security posture |
| `GET` | `/api/status` | Yes | Queue count and fail-closed runtime mode |
| `GET` | `/api/tasks` | Yes | Bounded queue listing |
| `POST` | `/api/tasks` | Yes | Validates and persists a queued task |
| `POST` | `/api/train/trigger` | Yes | Persists a training proposal; does not run training |

There is deliberately no arbitrary command, shell, URL fetch, package installation, disk operation, security mutation, Azure deployment, RBAC, Key Vault write, tenant consent, or secret-readback endpoint.

## Local launch

```powershell
$env:AIHUB_API_KEY_FILE = "$env:LOCALAPPDATA\HELIOS\Secrets\aihub.local.token"
$env:AIHUB_CONTROL_HOST = '127.0.0.1'
$env:AIHUB_CONTROL_PORT = '8787'

python -m python.aihub.secure_runtime.server
```

Create the token outside the repository with a cryptographically secure generator and restrict its file ACL to the current user.

## Production relationship

This local runtime is not the public ChatGPT/Copilot MCP endpoint. The hosted integration path remains the typed HELIOS broker and MCP/plugin control plane, protected by Entra/OAuth, GitHub deployment environments, Azure workload identity, managed identity, and separate plan/deploy approvals.
