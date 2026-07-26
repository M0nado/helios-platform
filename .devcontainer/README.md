# HELIOS developer cockpit container

This directory defines the governed portable development lane for
`M0nado/helios-platform`.

## Supported path

Open the repository in GitHub Codespaces or run **Dev Containers: Reopen in
Container** from VS Code. The active configuration is `devcontainer.json`; it
builds `Dockerfile`, installs declared feature channels and exact SDK/CLI
versions, installs the locked npm
tools from `package-lock.json`, and runs the developer doctor.

```bash
python3 scripts/dev/helios_dev_doctor.py --profile devcontainer
bash scripts/dev/portable-validate.sh
```

The container includes .NET 8, Python 3.12, Node 24 LTS, PowerShell 7.4,
GitHub CLI, Azure CLI, Bicep, CMake, Ninja, jq, Claude Code, Azure MCP, and the
compiler required by the C++20 lane.

The Microsoft 365 Agents Toolkit VS Code extension is recommended, but its CLI
is not automatically installed. Version `1.1.12` currently carries unresolved
high-severity transitive npm advisories and remains quarantined until Microsoft
publishes a clean dependency graph.

## Security boundary

- The container runs as non-root user `vscode`.
- Forwarded ports are private and do not auto-open.
- Azure MCP is read-only. Azure DevOps is hard read-only in the VS Code remote
  profile and intentionally omitted from Claude's project profile.
- No host Docker socket, `SYS_ADMIN`, tenant consent, RBAC change, cloud
  deployment, self-hosted runner, Windows boot mutation, or device write is
  configured.
- Authentication remains interactive and provider-specific. No credential is
  copied into this directory.

## Compatibility files

`docker-compose.yml` is a minimal non-privileged local fallback. It does not
start PostgreSQL or mount the host Docker socket. `onCreateCommand.sh` delegates
to the same locked bootstrap as Codespaces. `init-db.sh` is retained only for
historical database experiments and is not referenced by the cockpit.

The authoritative versions and policy are in
`../config/dev/toolchain-lock.json`. Full usage, Edge debugging, MCP routes, and
runner separation are documented in
`../docs/guides/HELIOS_DEVELOPER_COCKPIT.md`.
