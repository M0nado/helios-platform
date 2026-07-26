#!/usr/bin/env python3
"""Validate the HELIOS developer cockpit without authenticating or mutating services."""

from __future__ import annotations

import argparse
import json
import re
import shutil
import subprocess
import sys
from dataclasses import asdict, dataclass
from pathlib import Path
from typing import Any


ROOT = Path(__file__).resolve().parents[2]
LOCK_PATH = ROOT / "config/dev/toolchain-lock.json"
VERSION_PATTERN = re.compile(
    r"(?<!\d)v?(\d+(?:\.\d+){1,3}(?:-[0-9A-Za-z][0-9A-Za-z.-]*)?)"
)


@dataclass(frozen=True)
class Probe:
    command: tuple[str, ...]
    version_key: str
    match: str = "exact"
    minimum: bool = False


@dataclass
class Check:
    name: str
    status: str
    detail: str
    required: bool = True


PROBES: dict[str, Probe] = {
    "python": Probe(("python3", "--version"), "python", match="major-minor"),
    "dotnet": Probe(("dotnet", "--version"), "dotnet"),
    "node": Probe(("node", "--version"), "node"),
    "npm": Probe(("npm", "--version"), "npm"),
    "powershell": Probe(("pwsh", "--version"), "powershell"),
    "githubCli": Probe(("gh", "--version"), "githubCli"),
    "azureCli": Probe(("az", "version", "--output", "json"), "azureCli"),
    "bicep": Probe(("az", "bicep", "version"), "bicep"),
    "git": Probe(("git", "--version"), "git", minimum=True),
    "cmake": Probe(("cmake", "--version"), "cmake", minimum=True),
    "ninja": Probe(("ninja", "--version"), "ninja", minimum=True),
    "jq": Probe(("jq", "--version"), "jq", minimum=True),
    "shellcheck": Probe(
        ("shellcheck", "--version"),
        "shellcheck",
        minimum=True,
    ),
    "claudeCode": Probe(("claude", "--version"), "claudeCode"),
}


def read_json(path: Path) -> Any:
    return json.loads(path.read_text(encoding="utf-8"))


def extract_version(output: str) -> str | None:
    match = VERSION_PATTERN.search(output)
    return match.group(1) if match else None


def numeric_version(value: str) -> tuple[int, ...]:
    base = value.removeprefix("v").split("-", 1)[0]
    return tuple(int(part) for part in base.split("."))


def version_matches(
    actual: str,
    expected: str,
    *,
    match: str = "exact",
    minimum: bool = False,
) -> bool:
    actual_normalized = actual.removeprefix("v")
    expected_normalized = expected.removeprefix("v")
    if minimum:
        actual_parts = numeric_version(actual_normalized)
        expected_parts = numeric_version(expected_normalized)
        width = max(len(actual_parts), len(expected_parts))
        return actual_parts + (0,) * (width - len(actual_parts)) >= (
            expected_parts + (0,) * (width - len(expected_parts))
        )
    if match == "major-minor":
        return numeric_version(actual_normalized)[:2] == numeric_version(
            expected_normalized
        )[:2]
    return actual_normalized == expected_normalized


def add_check(
    checks: list[Check],
    name: str,
    condition: bool,
    detail: str,
    *,
    required: bool = True,
) -> None:
    checks.append(
        Check(
            name=name,
            status="pass" if condition else ("fail" if required else "warn"),
            detail=detail,
            required=required,
        )
    )


def nested_values(value: Any) -> list[str]:
    values: list[str] = []
    if isinstance(value, dict):
        for key, child in value.items():
            values.append(str(key))
            values.extend(nested_values(child))
    elif isinstance(value, list):
        for child in value:
            values.extend(nested_values(child))
    elif isinstance(value, str):
        values.append(value)
    return values


def check_mcp_contract(
    checks: list[Check],
    path: Path,
    server_key: str,
    azure_mcp_version: str,
    *,
    expect_remote_devops: bool,
) -> None:
    payload = read_json(path)
    servers = payload.get(server_key, {})
    required_servers = {
        "helios-local-dev",
        "azure-mcp-readonly",
        "github",
        "linear",
        "slack",
    }
    if expect_remote_devops:
        required_servers.add("azure-devops-readonly")
    add_check(
        checks,
        f"{path.relative_to(ROOT)} server set",
        required_servers.issubset(servers),
        (
            "VS Code MCP includes local, Azure, read-only DevOps, GitHub, "
            "Linear, and Slack"
            if expect_remote_devops
            else "Claude MCP includes local, Azure, GitHub, Linear, and Slack"
        ),
    )

    azure_server = servers.get("azure-mcp-readonly", {})
    azure_args = azure_server.get("args", [])
    add_check(
        checks,
        f"{path.relative_to(ROOT)} Azure read-only",
        "--read-only" in azure_args
        and azure_server.get("command") == "azmcp"
        and bool(azure_mcp_version),
        "Azure MCP uses the integrity-locked local binary in read-only mode",
    )

    devops = servers.get("azure-devops-readonly", {})
    add_check(
        checks,
        (
            f"{path.relative_to(ROOT)} Azure DevOps read-only"
            if expect_remote_devops
            else f"{path.relative_to(ROOT)} Azure DevOps host compatibility"
        ),
        (
            str(devops.get("headers", {}).get("X-MCP-Readonly", "")).lower()
            == "true"
            if expect_remote_devops
            else "azure-devops-readonly" not in servers
        ),
        (
            "VS Code uses Microsoft's remote server with enforced read-only mode"
            if expect_remote_devops
            else "Claude omits the unsupported remote server and unsafe local write-capable fallback"
        ),
    )

    add_check(
        checks,
        f"{path.relative_to(ROOT)} Foundry write boundary",
        "foundry" not in servers,
        "hosted Foundry MCP is not auto-bound because the preview exposes write tools",
    )

    direct_microsoft_servers = {"teams", "sharepoint"}.intersection(servers)
    add_check(
        checks,
        f"{path.relative_to(ROOT)} governed Microsoft routes",
        not direct_microsoft_servers,
        "Teams and SharePoint are not assigned invented direct MCP endpoints",
    )

    flattened = "\n".join(nested_values(payload)).lower()
    forbidden = (
        "clientsecret",
        "personalaccesstoken",
        "bearer ",
        "api_key=",
        "apikey=",
    )
    add_check(
        checks,
        f"{path.relative_to(ROOT)} secretless configuration",
        not any(token in flattened for token in forbidden),
        "project MCP configuration contains no literal credential fields",
    )


def check_contract(root: Path = ROOT) -> list[Check]:
    checks: list[Check] = []
    lock_path = root / "config/dev/toolchain-lock.json"
    required_files = (
        ".devcontainer/Dockerfile",
        ".devcontainer/README.md",
        ".devcontainer/codespaces-secrets.env.example",
        ".devcontainer/devcontainer.json",
        ".devcontainer/docker-compose.yml",
        ".devcontainer/onCreateCommand.sh",
        ".devcontainer/package.json",
        ".devcontainer/package-lock.json",
        ".github/workflows/helios-dev-cockpit.yml",
        ".vscode/extensions.json",
        ".vscode/launch.json",
        ".vscode/mcp.json",
        ".vscode/settings.json",
        ".vscode/tasks.json",
        ".mcp.json",
        "CLAUDE.md",
        "config/dev/toolchain-lock.json",
        "docs/guides/HELIOS_DEVELOPER_COCKPIT.md",
        "global.json",
        "plugins/helios-control-fabric/assets/runner-topology.json",
        "plugins/helios-control-fabric/skills/helios-dev-cockpit/SKILL.md",
        "scripts/dev/bootstrap-cockpit.sh",
        "scripts/dev/devsetup.sh",
        "scripts/dev/portable-validate.sh",
        "verify-setup.sh",
    )
    for relative in required_files:
        add_check(
            checks,
            f"file {relative}",
            (root / relative).is_file(),
            "required cockpit contract file exists",
        )

    if not lock_path.is_file():
        return checks

    lock = read_json(lock_path)
    versions = lock.get("versions", {})
    minimum_versions = lock.get("minimumVersions", {})
    add_check(
        checks,
        "toolchain schema",
        lock.get("schemaVersion") == 1
        and lock.get("policy") == "pinned-read-plan-pr-first",
        "toolchain lock is schema v1 and plan-first",
    )
    add_check(
        checks,
        "toolchain version keys",
        all(
            key in versions
            for key in (
                "dotnet",
                "python",
                "node",
                "npm",
                "powershell",
                "githubCli",
                "azureCli",
                "bicep",
                "devcontainerCli",
                "claudeCode",
                "azureMcp",
                "m365AgentsToolkit",
            )
        )
        and all(
            key in minimum_versions
            for key in ("git", "cmake", "ninja", "jq", "shellcheck")
        ),
        "required exact and minimum version pins are present",
    )
    boundaries = lock.get("boundaries", {})
    add_check(
        checks,
        "cold mutation boundaries",
        bool(boundaries) and all(value is False for value in boundaries.values()),
        "all deployment, identity, runner, device, and merge mutations remain disabled",
    )

    global_json_path = root / "global.json"
    if global_json_path.is_file():
        dotnet_sdk = read_json(global_json_path).get("sdk", {})
        add_check(
            checks,
            ".NET SDK pin",
            dotnet_sdk.get("version") == versions.get("dotnet")
            and dotnet_sdk.get("rollForward") == "latestPatch"
            and dotnet_sdk.get("allowPrerelease") is False,
            "global.json matches the lock and stays on the .NET 8 servicing band",
        )

    devcontainer_path = root / ".devcontainer/devcontainer.json"
    if devcontainer_path.is_file():
        devcontainer = read_json(devcontainer_path)
        features = devcontainer.get("features", {})
        expected_features = {
            "ghcr.io/devcontainers/features/node:2": {
                "version": versions.get("node"),
                "npmVersion": versions.get("npm"),
            },
            "ghcr.io/devcontainers/features/python:1": {
                "version": versions.get("python"),
            },
            "ghcr.io/devcontainers/features/powershell:2": {
                "version": versions.get("powershell"),
            },
            "ghcr.io/devcontainers/features/azure-cli:1": {
                "version": versions.get("azureCli"),
                "bicepVersion": f"v{versions.get('bicep')}",
                "installBicep": True,
            },
            "ghcr.io/devcontainers/features/github-cli:1": {
                "version": versions.get("githubCli"),
            },
        }
        feature_match = all(
            all(features.get(feature, {}).get(key) == value for key, value in options.items())
            for feature, options in expected_features.items()
        )
        add_check(
            checks,
            "devcontainer feature pins",
            feature_match,
            "devcontainer features match the toolchain lock",
        )
        add_check(
            checks,
            "devcontainer safe defaults",
            devcontainer.get("remoteUser") == "vscode"
            and bool(devcontainer.get("portsAttributes"))
            and all(
                value.get("visibility") == "private"
                for value in devcontainer.get("portsAttributes", {}).values()
            )
            and "privileged" not in devcontainer,
            "non-root user, private forwarded ports, and no privileged mode",
        )

    dockerfile_path = root / ".devcontainer/Dockerfile"
    if dockerfile_path.is_file():
        dockerfile = dockerfile_path.read_text(encoding="utf-8")
        add_check(
            checks,
            "Dockerfile base and user",
            "mcr.microsoft.com/devcontainers/dotnet:1-8.0-noble" in dockerfile
            and dockerfile.rstrip().endswith("USER vscode"),
            "supported .NET 8 Noble base returns to non-root vscode user",
        )
        add_check(
            checks,
            "Dockerfile supply-chain boundary",
            "curl " not in dockerfile
            and "wget " not in dockerfile
            and "npm install -g" not in dockerfile
            and "Install-Module" not in dockerfile,
            "Dockerfile performs no pipe-to-shell or unpinned global package install",
        )

    compose_path = root / ".devcontainer/docker-compose.yml"
    if compose_path.is_file():
        compose = compose_path.read_text(encoding="utf-8")
        add_check(
            checks,
            "Compose isolation",
            "/var/run/docker.sock" not in compose
            and "SYS_ADMIN" not in compose
            and "privileged:" not in compose
            and "POSTGRES_PASSWORD" not in compose
            and '127.0.0.1:5080:5080' in compose,
            "fallback compose has no host Docker socket, extra capability, or embedded password",
        )

    setup_scripts = (
        root / ".devcontainer/onCreateCommand.sh",
        root / "scripts/dev/devsetup.sh",
    )
    if all(path.is_file() for path in setup_scripts):
        setup_text = "\n".join(
            path.read_text(encoding="utf-8") for path in setup_scripts
        )
        add_check(
            checks,
            "setup scripts are side-effect bounded",
            "bootstrap-cockpit.sh" in setup_text
            and "cat > .env" not in setup_text
            and ".git/hooks" not in setup_text,
            "setup delegates to the locked bootstrap and does not create env files or Git hooks",
        )

    local_bootstrap_path = root / "scripts/setup/bootstrap-local-tools.sh"
    if local_bootstrap_path.is_file():
        local_bootstrap = local_bootstrap_path.read_text(encoding="utf-8")
        add_check(
            checks,
            "local bootstrap supply-chain boundary",
            "raw.githubusercontent.com/dotnet/install-scripts/${DOTNET_INSTALL_COMMIT}"
            in local_bootstrap
            and "DOTNET_INSTALL_SHA256=" in local_bootstrap
            and local_bootstrap.count("sha256sum --check") >= 2
            and "https://dot.net/v1/dotnet-install.sh" not in local_bootstrap
            and "pip\" install --disable-pip-version-check --upgrade" not in local_bootstrap,
            "fallback host installers are version-pinned and checksum-verified",
        )

    package_path = root / ".devcontainer/package.json"
    if package_path.is_file():
        package = read_json(package_path)
        add_check(
            checks,
            "node package pins",
            package.get("dependencies") == lock.get("nodePackages"),
            "automatically installed Claude Code, Azure MCP, and devcontainer CLI match the lock",
        )
        package_lock_path = root / ".devcontainer/package-lock.json"
        if package_lock_path.is_file():
            package_lock = read_json(package_lock_path)
            locked_root = (
                package_lock.get("packages", {})
                .get("", {})
                .get("dependencies", {})
            )
            add_check(
                checks,
                "npm integrity lock",
                package_lock.get("lockfileVersion") == 3
                and locked_root == lock.get("nodePackages"),
                "npm lockfile v3 exactly matches the declared node packages",
            )
    quarantine = lock.get("quarantinedTools", {}).get(
        "@microsoft/m365agentstoolkit-cli",
        {},
    )
    add_check(
        checks,
        "M365 CLI quarantine",
        quarantine.get("automaticInstall") is False
        and quarantine.get("version") == versions.get("m365AgentsToolkit")
        and "GHSA-xcpc-8h2w-3j85" in quarantine.get("advisories", []),
        "vulnerable M365 CLI dependency graph is recorded but not auto-installed",
    )

    extensions_path = root / ".vscode/extensions.json"
    if extensions_path.is_file():
        extensions = set(read_json(extensions_path).get("recommendations", []))
        locked_extensions = set(lock.get("vscodeExtensions", []))
        add_check(
            checks,
            "VS Code extension policy",
            extensions == locked_extensions,
            "root extension recommendations exactly match the lock",
        )
        if devcontainer_path.is_file():
            dev_extensions = set(
                read_json(devcontainer_path)
                .get("customizations", {})
                .get("vscode", {})
                .get("extensions", [])
            )
            add_check(
                checks,
                "devcontainer extension policy",
                dev_extensions == locked_extensions,
                "Codespaces and local VS Code recommend the same extensions",
            )

    launch_path = root / ".vscode/launch.json"
    if launch_path.is_file():
        launch = read_json(launch_path)
        configurations = launch.get("configurations", [])
        add_check(
            checks,
            "Microsoft Edge launch profiles",
            len(configurations) >= 2
            and all(item.get("type") == "msedge" for item in configurations)
            and any("/wizard/" in item.get("url", "") for item in configurations),
            "Edge profiles cover the setup wizard and MCP app",
        )

    task_path = root / ".vscode/tasks.json"
    if task_path.is_file():
        tasks = read_json(task_path).get("tasks", [])
        local_task = next(
            (
                task
                for task in tasks
                if task.get("label") == "HELIOS: start local connector"
            ),
            {},
        )
        environment = local_task.get("options", {}).get("env", {})
        add_check(
            checks,
            "local connector task boundary",
            environment.get("HELIOS_EXECUTION_MODE") == "dry-run"
            and environment.get("HELIOS_CLOUD_RUNTIME_ONLY") == "false"
            and environment.get("HELIOS_LOCAL_RUNTIME_ALLOWED") == "true",
            "local task is explicitly dry-run and local-only",
        )

    check_mcp_contract(
        checks,
        root / ".mcp.json",
        "mcpServers",
        str(versions.get("azureMcp")),
        expect_remote_devops=False,
    )
    check_mcp_contract(
        checks,
        root / ".vscode/mcp.json",
        "servers",
        str(versions.get("azureMcp")),
        expect_remote_devops=True,
    )

    runner_path = root / "plugins/helios-control-fabric/assets/runner-topology.json"
    if runner_path.is_file():
        topology = read_json(runner_path)
        add_check(
            checks,
            "runner topology schema",
            topology.get("schemaVersion") == 2,
            "runner topology uses the cockpit-aware schema",
        )
        add_check(
            checks,
            "runner isolation",
            topology.get("selfHosted", {}).get("enabled") is False
            and topology.get("deviceLab", {}).get("enabled") is False,
            "self-hosted and physical device lanes remain disabled",
        )
        add_check(
            checks,
            "Windows artifact evidence",
            topology.get("windowsDesktop", {}).get("artifactEvidence", {}).get(
                "sha256Manifest"
            )
            == "SHA256SUMS.txt",
            "Windows-hosted output requires a SHA-256 manifest",
        )

    plugin_mcp_path = root / "plugins/helios-control-fabric/.mcp.json"
    if plugin_mcp_path.is_file():
        plugin_mcp_text = plugin_mcp_path.read_text(encoding="utf-8")
        add_check(
            checks,
            "plugin Azure MCP pin",
            f"@azure/mcp@{versions.get('azureMcp')}" in plugin_mcp_text
            and "--read-only" in plugin_mcp_text,
            "plugin MCP pin matches the cockpit lock",
        )

    workflow_path = root / ".github/workflows/helios-dev-cockpit.yml"
    if workflow_path.is_file():
        workflow = workflow_path.read_text(encoding="utf-8")
        add_check(
            checks,
            "cockpit workflow is read-only",
            "permissions:\n  contents: read" in workflow
            and "id-token: write" not in workflow
            and "deployment" not in workflow.lower(),
            "workflow can read source and build the container but cannot request cloud identity",
        )
        add_check(
            checks,
            "devcontainer CLI pin",
            workflow.count(".devcontainer/node_modules/.bin/devcontainer") == 2
            and "@devcontainers/cli@" not in workflow
            and str(versions.get("devcontainerCli"))
            == str(lock.get("nodePackages", {}).get("@devcontainers/cli")),
            "clean bootstrap and exec use the integrity-locked devcontainer CLI",
        )

    plugin_manifest_path = (
        root / "plugins/helios-control-fabric/.codex-plugin/plugin.json"
    )
    app_manifest_path = root / "monado/helios-control/appPackage/manifest.json"
    runtime_path = (
        root / "monado/helios-control/src/Helios.Connect.Api/Program.cs"
    )
    if (
        plugin_manifest_path.is_file()
        and app_manifest_path.is_file()
        and runtime_path.is_file()
    ):
        plugin_version = str(read_json(plugin_manifest_path).get("version", ""))
        app_version = str(read_json(app_manifest_path).get("version", ""))
        runtime = runtime_path.read_text(encoding="utf-8")
        add_check(
            checks,
            "control-fabric version alignment",
            plugin_version == "0.7.0"
            and app_version == plugin_version
            and f'Version = "{plugin_version}"' in runtime,
            "plugin, Microsoft package, and MCP runtime use one version",
        )

    return checks


def run_probe(
    tool_id: str,
    probe: Probe,
    lock: dict[str, Any],
    *,
    required: bool,
) -> Check:
    executable = shutil.which(probe.command[0])
    if executable is None:
        return Check(
            name=f"tool {tool_id}",
            status="fail" if required else "warn",
            detail=f"{probe.command[0]} is not on PATH",
            required=required,
        )

    command = (executable, *probe.command[1:])
    try:
        process = subprocess.run(
            command,
            check=False,
            capture_output=True,
            text=True,
            timeout=20,
        )
    except (OSError, subprocess.TimeoutExpired) as error:
        return Check(
            name=f"tool {tool_id}",
            status="fail" if required else "warn",
            detail=f"version probe failed: {type(error).__name__}",
            required=required,
        )

    output = "\n".join(part for part in (process.stdout, process.stderr) if part)
    actual = extract_version(output)
    expected_source = (
        lock.get("minimumVersions", {})
        if probe.minimum
        else lock.get("versions", {})
    )
    expected = str(expected_source.get(probe.version_key, ""))
    healthy = (
        process.returncode == 0
        and actual is not None
        and bool(expected)
        and version_matches(
            actual,
            expected,
            match=probe.match,
            minimum=probe.minimum,
        )
    )
    relation = "minimum" if probe.minimum else probe.match
    return Check(
        name=f"tool {tool_id}",
        status="pass" if healthy else ("fail" if required else "warn"),
        detail=(
            f"actual={actual or 'unknown'} expected={expected or 'missing'} "
            f"match={relation} exit={process.returncode}"
        ),
        required=required,
    )


def check_node_packages(root: Path, lock: dict[str, Any]) -> list[Check]:
    checks: list[Check] = []
    for package_name, expected in lock.get("nodePackages", {}).items():
        package_path = (
            root
            / ".devcontainer/node_modules"
            / Path(*package_name.split("/"))
            / "package.json"
        )
        if not package_path.is_file():
            add_check(
                checks,
                f"node package {package_name}",
                False,
                "package is not installed in .devcontainer/node_modules",
            )
            continue
        actual = str(read_json(package_path).get("version", ""))
        add_check(
            checks,
            f"node package {package_name}",
            actual == expected,
            f"actual={actual or 'unknown'} expected={expected}",
        )
    return checks


def build_report(profile: str, root: Path = ROOT) -> dict[str, Any]:
    checks = check_contract(root)
    lock = read_json(root / "config/dev/toolchain-lock.json")
    required_tools = set(
        lock.get("requiredProfiles", {}).get(profile, [])
    )
    if profile != "contract":
        for tool_id in required_tools:
            probe = PROBES.get(tool_id)
            if probe is None:
                checks.append(
                    Check(
                        name=f"tool {tool_id}",
                        status="fail",
                        detail="required profile tool has no safe probe",
                    )
                )
                continue
            checks.append(
                run_probe(
                    tool_id,
                    probe,
                    lock,
                    required=True,
                )
            )
        checks.extend(check_node_packages(root, lock))

    failed = [check for check in checks if check.status == "fail"]
    warned = [check for check in checks if check.status == "warn"]
    return {
        "schemaVersion": 1,
        "profile": profile,
        "status": "failed" if failed else "ready",
        "cloudAuthenticationChecked": False,
        "mutationsPerformed": 0,
        "summary": {
            "passed": sum(check.status == "pass" for check in checks),
            "failed": len(failed),
            "warnings": len(warned),
        },
        "checks": [asdict(check) for check in checks],
    }


def print_human(report: dict[str, Any]) -> None:
    print(f"HELIOS developer cockpit ({report['profile']})")
    for check in report["checks"]:
        marker = {
            "pass": "OK",
            "fail": "FAIL",
            "warn": "WARN",
        }[check["status"]]
        print(f"  [{marker:4}] {check['name']}: {check['detail']}")
    summary = report["summary"]
    print(
        "  "
        f"passed={summary['passed']} failed={summary['failed']} "
        f"warnings={summary['warnings']}"
    )
    print("  Cloud authentication: not checked")
    print("  Mutations performed: 0")


def parse_args(argv: list[str] | None = None) -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description=(
            "Validate the pinned HELIOS cockpit without authenticating or "
            "mutating GitHub, Azure, Microsoft 365, or collaboration systems."
        )
    )
    parser.add_argument(
        "--profile",
        choices=("contract", "devcontainer"),
        default="contract",
    )
    parser.add_argument("--json", action="store_true")
    return parser.parse_args(argv)


def main(argv: list[str] | None = None) -> int:
    args = parse_args(argv)
    report = build_report(args.profile)
    if args.json:
        print(json.dumps(report, indent=2, sort_keys=True))
    else:
        print_human(report)
    return 0 if report["status"] == "ready" else 1


if __name__ == "__main__":
    sys.exit(main())
