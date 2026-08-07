#!/usr/bin/env python3
"""Plan-first HELIOS setup helper.

Commands are read-only and deterministic by default. Optional `--auto-local`
enables local runtime artifact generation and bounded autoscaling contracts for
fleet workflows. Cloud, repository, and collaboration writes remain in reviewed
protected-environment workflows.
"""

from __future__ import annotations

import argparse
import json
import os
import shutil
import subprocess
import sys
from datetime import datetime, timezone
from pathlib import Path
from typing import Any, Callable


ROOT = Path(__file__).resolve().parents[1]
REPO_ROOT = ROOT.parents[1]
ASSETS = ROOT / "assets"
MONADO_CONFIG = REPO_ROOT / "monado" / "helios-control" / "config"
LOCAL_AUTOMATION_ROOT = MONADO_CONFIG.parent / "runtime" / "fleet" / "automation"
TARGETS_FILE = ASSETS / "connections.json"

TOOLS: tuple[tuple[str, tuple[str, ...], bool], ...] = (
    ("az", ("version",), True),
    ("gh", ("--version",), True),
    ("dotnet", ("--version",), True),
    ("pwsh", ("--version",), True),
    ("node", ("--version",), True),
    ("npm", ("--version",), True),
    ("azd", ("version",), False),
    ("docker", ("--version",), False),
    ("jq", ("--version",), False),
    ("pac", ("--version",), False),
    ("atk", ("--version",), False),
)

ENVIRONMENT_KEYS = (
    "HELIOS_AZURE_CONNECTOR_URL",
    "AZURE_DEVOPS_ORGANIZATION",
    "AZURE_TENANT_ID",
    "AZURE_SUBSCRIPTION_ID",
    "AZURE_RESOURCE_GROUP",
    "AZURE_CLIENT_ID",
)

SUPPORTED_ENVIRONMENTS = ("azure-dev", "azure-test", "azure-prod")
SUPPORTED_LANGUAGES = ("csharp", "fsharp", "cpp", "python")
AIHUB_MATRIX_FILE = "aihub-learning-matrix.json"
GITHUB_API_VERSION = "2026-03-10"
GitHubApiReader = Callable[[str], dict[str, Any]]


def read_json_file(path: Path) -> dict[str, Any]:
    payload = json.loads(path.read_text(encoding="utf-8"))
    if not isinstance(payload, dict):
        raise RuntimeError(f"Expected a JSON object in {path}.")
    return payload


def read_asset(name: str) -> dict[str, Any]:
    return read_json_file(ASSETS / name)


def read_monado_config(name: str) -> dict[str, Any]:
    return read_json_file(MONADO_CONFIG / name)


def read_targets() -> dict[str, Any]:
    return read_asset("connections.json")


def require_mapping(value: Any, field: str) -> dict[str, Any]:
    if not isinstance(value, dict):
        raise RuntimeError(f"Expected '{field}' to be a JSON object.")
    return value


def require_list(value: Any, field: str) -> list[Any]:
    if not isinstance(value, list):
        raise RuntimeError(f"Expected '{field}' to be a JSON array.")
    return value


def require_string(value: Any, field: str) -> str:
    text = str(value or "").strip()
    if not text:
        raise RuntimeError(f"Expected '{field}' to be a non-empty string.")
    return text


def find_named_item(
    items: list[Any],
    field: str,
    expected_value: str,
    collection_name: str,
) -> dict[str, Any]:
    for item in items:
        if isinstance(item, dict) and item.get(field) == expected_value:
            return item
    raise RuntimeError(
        f"Expected '{collection_name}' to include {field}='{expected_value}'."
    )


def utc_timestamp() -> str:
    return datetime.now(timezone.utc).replace(microsecond=0).isoformat().replace(
        "+00:00",
        "Z",
    )


def to_repo_relative(path: Path) -> str:
    try:
        return str(path.relative_to(REPO_ROOT))
    except ValueError:
        return str(path)


def write_json_file(path: Path, payload: dict[str, Any]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(
        json.dumps(payload, indent=2, sort_keys=True) + "\n",
        encoding="utf-8",
    )


def required_non_negative_int(value: Any, field: str) -> int:
    if isinstance(value, bool):
        raise RuntimeError(f"Expected '{field}' to be a non-negative integer.")
    try:
        parsed = int(value)
    except (TypeError, ValueError) as error:
        raise RuntimeError(f"Expected '{field}' to be a non-negative integer.") from error
    if parsed < 0:
        raise RuntimeError(f"Expected '{field}' to be a non-negative integer.")
    return parsed


def first_line(value: str) -> str:
    return value.strip().splitlines()[0] if value.strip() else "available"


def check_tool(command: str, arguments: tuple[str, ...], required: bool) -> dict[str, Any]:
    path = shutil.which(command)
    result: dict[str, Any] = {
        "command": command,
        "required": required,
        "available": path is not None,
    }
    if path is None:
        return result
    try:
        process = subprocess.run(
            [path, *arguments],
            check=False,
            capture_output=True,
            text=True,
            timeout=8,
        )
        result["version"] = first_line(process.stdout or process.stderr)
        result["healthy"] = process.returncode == 0
    except (OSError, subprocess.TimeoutExpired):
        result["healthy"] = False
        result["version"] = "version check failed"
    return result


def doctor() -> dict[str, Any]:
    tools = [check_tool(*entry) for entry in TOOLS]
    environment = {
        key: {
            "configured": bool(os.environ.get(key)),
            "secret": False,
        }
        for key in ENVIRONMENT_KEYS
    }
    return {
        "mode": "read-only",
        "tools": tools,
        "environment": environment,
        "requiredToolsReady": all(
            tool["available"] and tool.get("healthy", False)
            for tool in tools
            if tool["required"]
        ),
        "cloudAuthenticated": "not-checked",
        "azureDeployed": False,
    }


def validate_environment(environment: str) -> None:
    if environment not in SUPPORTED_ENVIRONMENTS:
        raise ValueError("environment must be azure-dev, azure-test, or azure-prod")


def validate_language(language: str) -> None:
    if language not in SUPPORTED_LANGUAGES:
        raise ValueError(
            "language must be csharp, fsharp, cpp, or python"
        )


def run_gh_api(endpoint: str) -> dict[str, Any]:
    gh = shutil.which("gh")
    if gh is None:
        raise RuntimeError(
            "GitHub CLI is required to resolve the effective OIDC subject. "
            "Install gh and authenticate with `gh auth login`."
        )
    try:
        process = subprocess.run(
            [
                gh,
                "api",
                "--method",
                "GET",
                "--header",
                f"X-GitHub-Api-Version: {GITHUB_API_VERSION}",
                endpoint,
            ],
            check=False,
            capture_output=True,
            text=True,
            timeout=15,
        )
    except (OSError, subprocess.TimeoutExpired) as error:
        raise RuntimeError(
            f"GitHub API read could not complete for {endpoint}."
        ) from error
    if process.returncode != 0:
        detail = first_line(process.stderr or process.stdout)
        raise RuntimeError(f"GitHub API read failed for {endpoint}: {detail}")
    try:
        payload = json.loads(process.stdout)
    except json.JSONDecodeError as error:
        raise RuntimeError(
            f"GitHub API returned invalid JSON for {endpoint}."
        ) from error
    if not isinstance(payload, dict):
        raise RuntimeError(
            f"GitHub API returned an unexpected payload for {endpoint}."
        )
    return payload


def required_text(value: Any, field: str) -> str:
    text = str(value or "").strip()
    if not text:
        raise RuntimeError(
            f"GitHub did not return {field}; refusing to guess the OIDC subject."
        )
    return text


def resolve_github_oidc_trust(
    repository: str,
    environment: str,
    api_reader: GitHubApiReader | None = None,
) -> dict[str, Any]:
    validate_environment(environment)
    if repository.count("/") != 1:
        raise ValueError("repository must use owner/name format")

    reader = api_reader or run_gh_api
    repository_info = reader(f"repos/{repository}")
    oidc_policy = reader(f"repos/{repository}/actions/oidc/customization/sub")

    if oidc_policy.get("use_default") is not True:
        raise RuntimeError(
            f"Repository '{repository}' uses a customized OIDC subject template. "
            "HELIOS will not infer or overwrite it."
        )
    if "use_immutable_subject" not in oidc_policy:
        raise RuntimeError(
            "GitHub did not return use_immutable_subject; refusing to guess "
            "the OIDC subject format."
        )

    owner = repository_info.get("owner")
    if not isinstance(owner, dict):
        raise RuntimeError(
            "GitHub did not return canonical repository-owner metadata; "
            "refusing to guess the OIDC subject."
        )
    canonical_owner = required_text(owner.get("login"), "the canonical owner name")
    owner_id = required_text(owner.get("id"), "the immutable owner ID")
    canonical_repository = required_text(
        repository_info.get("name"),
        "the canonical repository name",
    )
    repository_id = required_text(
        repository_info.get("id"),
        "the immutable repository ID",
    )

    use_immutable_subject = bool(oidc_policy["use_immutable_subject"])
    if use_immutable_subject:
        repository_segment = (
            f"{canonical_owner}@{owner_id}/"
            f"{canonical_repository}@{repository_id}"
        )
    else:
        repository_segment = f"{canonical_owner}/{canonical_repository}"

    return {
        "subject": f"repo:{repository_segment}:environment:{environment}",
        "canonicalRepository": f"{canonical_owner}/{canonical_repository}",
        "repositoryId": repository_id,
        "ownerId": owner_id,
        "useImmutableSubject": use_immutable_subject,
        "policyEndpoint": (
            f"repos/{repository}/actions/oidc/customization/sub"
        ),
        "apiVersion": GITHUB_API_VERSION,
    }


def release_plan(environment: str) -> dict[str, Any]:
    validate_environment(environment)
    return {
        "environment": environment,
        "executionMode": "plan-only",
        "automatic": [
            "toolchain diagnosis",
            "identity and resource inventory",
            "Bicep compilation",
            "Azure what-if evidence generation",
            "test and security validation",
            "task branch and draft pull request",
            "dry-run connector receipts",
        ],
        "administratorGates": [
            "tenant authentication and consent",
            "least-privilege RBAC assignment",
            "immutable image publication",
            "what-if approval",
            "Azure deployment approval",
            "connector activation",
            "Microsoft 365 Copilot and Teams publication",
        ],
        "commands": [
            "pwsh ./monado/helios-control/scripts/Connect-HeliosAzureInteractive.ps1",
            "python plugins/helios-control-fabric/scripts/helios.py doctor --json",
            "python plugins/helios-control-fabric/scripts/helios.py setup-all --environment "
            + environment
            + " --json",
            "python plugins/helios-control-fabric/scripts/helios.py oidc --environment "
            + environment
            + " --json",
            "python plugins/helios-control-fabric/scripts/helios.py edge --environment "
            + environment
            + " --json",
            "python plugins/helios-control-fabric/scripts/helios.py integration --json",
            "python plugins/helios-control-fabric/scripts/helios.py fleet --json",
            "python plugins/helios-control-fabric/scripts/helios.py hermes --json",
            "python plugins/helios-control-fabric/scripts/helios.py xcore9 --json",
            "python plugins/helios-control-fabric/scripts/helios.py aihub --json",
            "python plugins/helios-control-fabric/scripts/helios.py code-engine --json",
            "python plugins/helios-control-fabric/scripts/helios.py benchmark --json",
            "GitHub Actions: HELIOS Azure → what-if",
            "GitHub protected environment: " + environment + " → deployment approval",
        ],
        "federationSubjectResolution": (
            "Run the authenticated `oidc` command; HELIOS resolves GitHub's "
            "effective default/immutable subject policy and fails closed."
        ),
    }


def oidc_contract(
    environment: str,
    api_reader: GitHubApiReader | None = None,
) -> dict[str, Any]:
    validate_environment(environment)
    contract = read_asset("oidc.json")
    resolution = resolve_github_oidc_trust(
        contract["repository"],
        environment,
        api_reader=api_reader,
    )
    return {
        **contract,
        "environment": environment,
        "selectedSubject": resolution["subject"],
        "resolvedRepository": resolution["canonicalRepository"],
        "repositoryId": resolution["repositoryId"],
        "ownerId": resolution["ownerId"],
        "useImmutableSubject": resolution["useImmutableSubject"],
        "resolutionEvidence": {
            "policyEndpoint": resolution["policyEndpoint"],
            "apiVersion": resolution["apiVersion"],
        },
        "configuredVariables": {
            key: bool(os.environ.get(key))
            for key in contract["requiredVariables"]
        },
        "executionMode": "plan-only",
    }


def devops_sync_plan() -> dict[str, Any]:
    plan = read_asset("devops-sync.json")
    return {
        **plan,
        "organizationConfigured": bool(os.environ.get("AZURE_DEVOPS_ORGANIZATION")),
        "executionMode": "plan-only",
    }


def runner_plan() -> dict[str, Any]:
    return {
        **read_asset("runner-topology.json"),
        "executionMode": "plan-only",
    }


def edge_plan(environment: str) -> dict[str, Any]:
    validate_environment(environment)
    return {
        **read_asset("edge-runtime.json"),
        "environment": environment,
        "executionMode": "plan-only",
    }


def integration_plan() -> dict[str, Any]:
    plan = read_monado_config("integrations.json")
    destinations = require_mapping(plan.get("destinations"), "integrations.destinations")
    routing = require_list(plan.get("routing"), "integrations.routing")
    if any(not isinstance(route, dict) for route in routing):
        raise RuntimeError("Expected every integrations routing entry to be a JSON object.")
    destination_states: dict[str, dict[str, Any]] = {}
    for destination, payload in destinations.items():
        if not isinstance(payload, dict):
            raise RuntimeError(
                f"Expected integrations.destinations['{destination}'] to be a JSON object."
            )
        destination_states[str(destination)] = {
            "enabled": bool(payload.get("enabled")),
            "bindingState": payload.get("bindingState", "unspecified"),
        }
    hermes_events = [
        str(route["event"])
        for route in routing
        if isinstance(route.get("event"), str) and route["event"].startswith("hermes.")
    ]
    return {
        "schemaVersion": plan.get("schemaVersion", 1),
        "integrationExecutionMode": plan.get("executionMode", "unknown"),
        "runtimeBindings": plan.get("runtimeBindings", "unknown"),
        "routingState": plan.get("routingState", "unknown"),
        "destinations": destination_states,
        "routes": {
            "total": len(routing),
            "enabled": sum(
                1
                for route in routing
                if bool(route.get("enabled"))
            ),
            "hermesEvents": hermes_events,
        },
        "executionMode": "plan-only",
    }


def fleet_plan() -> dict[str, Any]:
    fleet = read_monado_config("agent-fleet.json")
    providers = require_list(fleet.get("providers"), "agent-fleet.providers")
    agents = require_list(fleet.get("agents"), "agent-fleet.agents")
    limits = require_mapping(fleet.get("limits"), "agent-fleet.limits")
    workflow = require_list(fleet.get("workflow"), "agent-fleet.workflow")
    forbidden = require_list(fleet.get("forbidden"), "agent-fleet.forbidden")
    if any(not isinstance(provider, dict) for provider in providers):
        raise RuntimeError("Expected every agent-fleet provider entry to be a JSON object.")
    if any(not isinstance(agent, dict) for agent in agents):
        raise RuntimeError("Expected every agent-fleet agent entry to be a JSON object.")
    matrix = aihub_plan()
    parallelism = require_mapping(
        matrix.get("crossModelParallelism"),
        "aihub.crossModelParallelism",
    )
    return {
        "schemaVersion": fleet.get("schemaVersion", 1),
        "name": require_string(fleet.get("name"), "agent-fleet.name"),
        "fleetExecutionMode": fleet.get("executionMode", "unknown"),
        "automaticProviderRuns": bool(fleet.get("automaticProviderRuns")),
        "limits": limits,
        "providers": [
            {
                "id": require_string(provider.get("id"), "agent-fleet.providers[].id"),
                "host": require_string(provider.get("host"), "agent-fleet.providers[].host"),
                "role": require_string(provider.get("role"), "agent-fleet.providers[].role"),
                "enabledWhenConfigured": bool(provider.get("enabledWhenConfigured")),
            }
            for provider in providers
        ],
        "agents": [
            {
                "id": require_string(agent.get("id"), "agent-fleet.agents[].id"),
                "permission": require_string(
                    agent.get("permission"),
                    "agent-fleet.agents[].permission",
                ),
                "outputs": require_list(
                    agent.get("outputs"),
                    "agent-fleet.agents[].outputs",
                ),
            }
            for agent in agents
        ],
        "workflow": workflow,
        "languageCoverage": list(SUPPORTED_LANGUAGES),
        "crossModelParallelism": parallelism,
        "forbidden": forbidden,
        "commands": {
            "plan": (
                "pwsh ./monado/helios-control/scripts/Start-HeliosLocalFleet.ps1 "
                "-Mode Plan"
            ),
            "status": (
                "pwsh ./monado/helios-control/scripts/Start-HeliosLocalFleet.ps1 "
                "-Mode Status"
            ),
        },
        "executionMode": "plan-only",
    }


def hermes_plan() -> dict[str, Any]:
    agents_contract = read_monado_config("microsoft-agents.json")
    agents = require_list(agents_contract.get("agents"), "microsoft-agents.agents")
    orchestrator = find_named_item(
        agents,
        "id",
        "hermes-orchestrator",
        "microsoft-agents.agents",
    )
    learning = require_mapping(
        agents_contract.get("learningPolicy"),
        "microsoft-agents.learningPolicy",
    )
    matrix = aihub_plan()
    provider_engines = require_list(matrix.get("providerEngines"), "aihub.providerEngines")
    xcore_profiles = require_list(matrix.get("xcoreProfiles"), "aihub.xcoreProfiles")
    parallelism = require_mapping(
        matrix.get("crossModelParallelism"),
        "aihub.crossModelParallelism",
    )
    if any(not isinstance(item, dict) for item in provider_engines):
        raise RuntimeError("Expected every AIHub provider engine entry to be a JSON object.")
    return {
        "schemaVersion": agents_contract.get("schemaVersion", 1),
        "runtime": require_string(
            agents_contract.get("runtime"),
            "microsoft-agents.runtime",
        ),
        "runtimeState": require_string(
            agents_contract.get("runtimeState"),
            "microsoft-agents.runtimeState",
        ),
        "distributionState": require_string(
            agents_contract.get("distributionState"),
            "microsoft-agents.distributionState",
        ),
        "orchestrator": {
            "id": require_string(orchestrator.get("id"), "hermes-orchestrator.id"),
            "type": require_string(orchestrator.get("type"), "hermes-orchestrator.type"),
            "deploymentState": require_string(
                orchestrator.get("deploymentState"),
                "hermes-orchestrator.deploymentState",
            ),
            "permissionTier": orchestrator.get("permissionTier"),
            "writes": require_string(
                orchestrator.get("writes"),
                "hermes-orchestrator.writes",
            ),
            "purpose": require_string(
                orchestrator.get("purpose"),
                "hermes-orchestrator.purpose",
            ),
        },
        "learningPolicy": {
            "promotion": require_string(
                learning.get("promotion"),
                "microsoft-agents.learningPolicy.promotion",
            ),
            "automaticProductionMutation": bool(
                learning.get("automaticProductionMutation")
            ),
            "rollbackRequired": bool(learning.get("rollbackRequired")),
            "rawCopilotConversationsAsTrainingData": bool(
                learning.get("rawCopilotConversationsAsTrainingData")
            ),
        },
        "providerEngines": [
            require_string(item.get("id"), "aihub.providerEngines[].id")
            for item in provider_engines
        ],
        "xcoreProfiles": xcore_profiles,
        "crossModelParallelism": parallelism,
        "executionMode": "plan-only",
    }


def xcore9_plan() -> dict[str, Any]:
    topology = runner_plan()
    self_hosted = require_mapping(topology.get("selfHosted"), "runner-topology.selfHosted")
    groups = require_list(self_hosted.get("groups"), "runner-topology.selfHosted.groups")
    xcore_group = find_named_item(
        groups,
        "name",
        "hermes-xcore9-local",
        "runner-topology.selfHosted.groups",
    )
    agents_contract = read_monado_config("microsoft-agents.json")
    agents = require_list(agents_contract.get("agents"), "microsoft-agents.agents")
    teacher = find_named_item(agents, "id", "xcore-teacher", "microsoft-agents.agents")
    evaluator = find_named_item(
        agents,
        "id",
        "xcore-evaluator",
        "microsoft-agents.agents",
    )
    matrix = aihub_plan()
    xcore_profiles = require_list(matrix.get("xcoreProfiles"), "aihub.xcoreProfiles")
    if any(not isinstance(profile, dict) for profile in xcore_profiles):
        raise RuntimeError("Expected every AIHub XCore profile entry to be a JSON object.")
    xcore9_profiles = [
        profile
        for profile in xcore_profiles
        if profile.get("runnerType") == "xcore9"
    ]
    if not xcore9_profiles:
        raise RuntimeError("Expected at least one xcore9 profile in the AIHub matrix.")
    state = require_mapping(read_targets().get("state"), "connections.state")
    return {
        "schemaVersion": 1,
        "state": state.get("xcore", "unknown"),
        "runnerGroup": {
            "name": require_string(xcore_group.get("name"), "xcore9.runnerGroup.name"),
            "fleet": require_string(xcore_group.get("fleet"), "xcore9.runnerGroup.fleet"),
            "labels": require_list(xcore_group.get("labels"), "xcore9.runnerGroup.labels"),
            "runners": require_list(
                xcore_group.get("runners"),
                "xcore9.runnerGroup.runners",
            ),
            "activationMode": require_string(
                self_hosted.get("activationMode"),
                "runner-topology.selfHosted.activationMode",
            ),
        },
        "teacher": {
            "id": require_string(teacher.get("id"), "xcore-teacher.id"),
            "type": require_string(teacher.get("type"), "xcore-teacher.type"),
            "deploymentState": require_string(
                teacher.get("deploymentState"),
                "xcore-teacher.deploymentState",
            ),
            "permissionTier": teacher.get("permissionTier"),
            "writes": require_string(teacher.get("writes"), "xcore-teacher.writes"),
        },
        "evaluator": {
            "id": require_string(evaluator.get("id"), "xcore-evaluator.id"),
            "type": require_string(evaluator.get("type"), "xcore-evaluator.type"),
            "deploymentState": require_string(
                evaluator.get("deploymentState"),
                "xcore-evaluator.deploymentState",
            ),
            "permissionTier": evaluator.get("permissionTier"),
            "writes": require_string(
                evaluator.get("writes"),
                "xcore-evaluator.writes",
            ),
        },
        "xcoreProfiles": xcore9_profiles,
        "executionMode": "plan-only",
    }


def aihub_plan(language: str | None = None) -> dict[str, Any]:
    matrix = read_monado_config(AIHUB_MATRIX_FILE)
    providers = require_list(matrix.get("providerEngines"), "aihub.providerEngines")
    playbooks = require_mapping(matrix.get("languagePlaybooks"), "aihub.languagePlaybooks")
    xcore_profiles = require_list(matrix.get("xcoreProfiles"), "aihub.xcoreProfiles")
    parallelism = require_mapping(
        matrix.get("crossModelParallelism"),
        "aihub.crossModelParallelism",
    )
    benchmark_learning = require_mapping(
        matrix.get("benchmarkLearning"),
        "aihub.benchmarkLearning",
    )
    safety = require_mapping(matrix.get("safety"), "aihub.safety")
    if any(not isinstance(provider, dict) for provider in providers):
        raise RuntimeError("Expected every AIHub provider engine entry to be a JSON object.")
    if any(not isinstance(profile, dict) for profile in xcore_profiles):
        raise RuntimeError("Expected every AIHub XCore profile entry to be a JSON object.")

    for required_language in SUPPORTED_LANGUAGES:
        require_mapping(
            playbooks.get(required_language),
            f"aihub.languagePlaybooks.{required_language}",
        )

    payload = {
        "schemaVersion": matrix.get("schemaVersion", 1),
        "contract": "aihub-learning-matrix",
        "name": require_string(matrix.get("name"), "aihub.name"),
        "state": require_string(matrix.get("state"), "aihub.state"),
        "internetPolicy": require_mapping(matrix.get("internetPolicy"), "aihub.internetPolicy"),
        "providerEngines": providers,
        "languageCoverage": list(SUPPORTED_LANGUAGES),
        "languagePlaybooks": playbooks,
        "xcoreProfiles": xcore_profiles,
        "crossModelParallelism": parallelism,
        "benchmarkLearning": benchmark_learning,
        "safety": safety,
        "executionMode": "plan-only",
    }
    if language is not None:
        validate_language(language)
        payload["selectedLanguage"] = language
        payload["playbook"] = require_mapping(
            playbooks.get(language),
            f"aihub.languagePlaybooks.{language}",
        )
        payload.pop("languagePlaybooks", None)
    return payload


def code_engine_plan() -> dict[str, Any]:
    fleet = fleet_plan()
    local_fleet_providers = require_list(fleet.get("providers"), "fleet.providers")
    workflow = require_list(fleet.get("workflow"), "fleet.workflow")
    matrix = aihub_plan()
    provider_catalog = require_list(matrix.get("providerEngines"), "aihub.providerEngines")
    playbooks = require_mapping(matrix.get("languagePlaybooks"), "aihub.languagePlaybooks")
    parallelism = require_mapping(
        matrix.get("crossModelParallelism"),
        "aihub.crossModelParallelism",
    )
    if any(not isinstance(engine, dict) for engine in provider_catalog):
        raise RuntimeError("Expected every AIHub provider engine entry to be a JSON object.")
    return {
        "schemaVersion": 1,
        "model": "provider-neutral-mcp-boundary",
        "engines": provider_catalog,
        "localFleetEngines": local_fleet_providers,
        "languageCoverage": list(SUPPORTED_LANGUAGES),
        "languagePlaybooks": playbooks,
        "crossModelParallelism": parallelism,
        "defaultWorkflow": workflow,
        "forbidden": require_list(fleet.get("forbidden"), "fleet.forbidden"),
        "executionMode": "plan-only",
    }


def benchmarking_plan() -> dict[str, Any]:
    agents_contract = read_monado_config("microsoft-agents.json")
    agents = require_list(agents_contract.get("agents"), "microsoft-agents.agents")
    evaluator = find_named_item(
        agents,
        "id",
        "xcore-evaluator",
        "microsoft-agents.agents",
    )
    learning = require_mapping(
        agents_contract.get("learningPolicy"),
        "microsoft-agents.learningPolicy",
    )
    matrix = aihub_plan()
    benchmark_learning = require_mapping(
        matrix.get("benchmarkLearning"),
        "aihub.benchmarkLearning",
    )
    fleet = fleet_plan()
    limits = require_mapping(fleet.get("limits"), "fleet.limits")
    benchmark_metrics = require_list(
        benchmark_learning.get("metrics"),
        "aihub.benchmarkLearning.metrics",
    )
    benchmark_gates = require_list(
        benchmark_learning.get("gates"),
        "aihub.benchmarkLearning.gates",
    )
    language_coverage = require_list(
        benchmark_learning.get("languageCoverage"),
        "aihub.benchmarkLearning.languageCoverage",
    )
    return {
        "schemaVersion": 1,
        "status": require_string(matrix.get("state"), "aihub.state"),
        "owner": require_string(
            benchmark_learning.get("owner"),
            "aihub.benchmarkLearning.owner",
        ),
        "evaluator": {
            "id": require_string(evaluator.get("id"), "xcore-evaluator.id"),
            "purpose": require_string(evaluator.get("purpose"), "xcore-evaluator.purpose"),
            "writes": require_string(evaluator.get("writes"), "xcore-evaluator.writes"),
        },
        "metrics": benchmark_metrics,
        "stores": {
            "candidateStore": learning.get("candidateStore"),
            "vectorStore": learning.get("vectorStore"),
            "evidenceStore": learning.get("evidenceStore"),
            "cloudStoresImplemented": bool(learning.get("cloudStoresImplemented")),
            "evidenceRetention": benchmark_learning.get("evidenceRetention"),
        },
        "gates": {
            "promotion": benchmark_gates,
            "automaticProductionMutation": bool(
                learning.get("automaticProductionMutation")
            ),
            "rollbackRequired": bool(learning.get("rollbackRequired")),
        },
        "languageCoverage": language_coverage,
        "limits": {
            "maxTaskMinutes": limits.get("maxTaskMinutes"),
            "maxParallelAgents": limits.get("maxParallelAgents"),
            "maxParallelCloudAgents": limits.get("maxParallelCloudAgents"),
        },
        "executionMode": "plan-only",
    }


def build_local_autoscaling_contract(environment: str) -> dict[str, Any]:
    validate_environment(environment)
    fleet = fleet_plan()
    xcore9 = xcore9_plan()
    aihub = aihub_plan()
    limits = require_mapping(fleet.get("limits"), "fleet.limits")
    providers = require_list(fleet.get("providers"), "fleet.providers")
    agents = require_list(fleet.get("agents"), "fleet.agents")
    xcore_runners = require_list(
        xcore9.get("runnerGroup", {}).get("runners"),
        "xcore9.runnerGroup.runners",
    )
    parallelism = require_mapping(
        aihub.get("crossModelParallelism"),
        "aihub.crossModelParallelism",
    )

    max_parallel_agents = required_non_negative_int(
        limits.get("maxParallelAgents"),
        "fleet.limits.maxParallelAgents",
    )
    max_parallel_cloud_agents = required_non_negative_int(
        limits.get("maxParallelCloudAgents"),
        "fleet.limits.maxParallelCloudAgents",
    )
    max_task_minutes = required_non_negative_int(
        limits.get("maxTaskMinutes"),
        "fleet.limits.maxTaskMinutes",
    )
    max_mcp_output_tokens = required_non_negative_int(
        limits.get("maxMcpOutputTokens"),
        "fleet.limits.maxMcpOutputTokens",
    )
    max_language_lanes = required_non_negative_int(
        parallelism.get("maxConcurrentLanguageLanes"),
        "aihub.crossModelParallelism.maxConcurrentLanguageLanes",
    )
    max_model_calls = required_non_negative_int(
        parallelism.get("maxConcurrentModelCalls"),
        "aihub.crossModelParallelism.maxConcurrentModelCalls",
    )
    max_cloud_calls = required_non_negative_int(
        parallelism.get("maxConcurrentCloudCalls"),
        "aihub.crossModelParallelism.maxConcurrentCloudCalls",
    )

    configured_cloud_agents = sum(
        1
        for provider in providers
        if isinstance(provider, dict) and bool(provider.get("enabledWhenConfigured"))
    )
    desired_local_agents = min(max_parallel_agents, len(agents), max_language_lanes)
    desired_cloud_agents = min(max_parallel_cloud_agents, configured_cloud_agents, max_cloud_calls)
    desired_xcore_runners = min(len(xcore_runners), max_parallel_agents, max_model_calls)
    scale_out_threshold = max(1, desired_local_agents)

    return {
        "schemaVersion": 1,
        "generatedAt": utc_timestamp(),
        "environment": environment,
        "executionMode": "automatic-local",
        "strategy": "bounded-local-fleet-autoscaling",
        "targets": {
            "localAgentsDesired": desired_local_agents,
            "cloudAgentsDesiredWhenConfigured": desired_cloud_agents,
            "xcore9RunnersDesired": desired_xcore_runners,
        },
        "limits": {
            "maxParallelAgents": max_parallel_agents,
            "maxParallelCloudAgents": max_parallel_cloud_agents,
            "maxTaskMinutes": max_task_minutes,
            "maxMcpOutputTokens": max_mcp_output_tokens,
        },
        "queuePolicy": {
            "scaleOutPendingTaskThreshold": scale_out_threshold,
            "scaleInIdleMinutes": 10,
            "targetLatencySeconds": 90,
        },
        "parallelismContract": {
            "maxConcurrentLanguageLanes": max_language_lanes,
            "maxConcurrentModelCalls": max_model_calls,
            "maxConcurrentCloudCalls": max_cloud_calls,
            "scheduler": parallelism.get("scheduler"),
            "backpressure": parallelism.get("backpressure"),
        },
        "forbidden": require_list(fleet.get("forbidden"), "fleet.forbidden"),
        "cloudMutation": "disabled-without-protected-approval",
    }


def activate_local_contract(
    contract_name: str,
    payload: dict[str, Any],
    environment: str,
) -> dict[str, Any]:
    validate_environment(environment)
    autoscaling = build_local_autoscaling_contract(environment)
    activated_at = utc_timestamp()

    state_dir = LOCAL_AUTOMATION_ROOT / environment
    contract_path = state_dir / f"{contract_name}.auto.json"
    autoscaling_path = state_dir / "autoscaling.auto.json"

    write_json_file(
        contract_path,
        {
            "schemaVersion": 1,
            "contract": contract_name,
            "environment": environment,
            "activatedAt": activated_at,
            "payload": payload,
        },
    )
    write_json_file(autoscaling_path, autoscaling)

    return {
        **payload,
        "executionMode": "automatic-local",
        "automation": {
            "mode": "automatic-local",
            "activatedAt": activated_at,
            "stateDirectory": to_repo_relative(state_dir),
            "contractArtifact": to_repo_relative(contract_path),
            "autoscalingArtifact": to_repo_relative(autoscaling_path),
            "autoscalingTargets": autoscaling["targets"],
            "cloudMutation": "not-automatic",
        },
    }


def setup_all(
    environment: str,
    api_reader: GitHubApiReader | None = None,
    auto_local: bool = False,
) -> dict[str, Any]:
    validate_environment(environment)
    payload = {
        "environment": environment,
        "executionMode": "plan-only",
        "doctor": doctor(),
        "targets": read_targets(),
        "plan": release_plan(environment),
        "oidc": oidc_contract(environment, api_reader=api_reader),
        "devopsSync": devops_sync_plan(),
        "runners": runner_plan(),
        "edge": edge_plan(environment),
        "integration": integration_plan(),
        "fleet": fleet_plan(),
        "hermes": hermes_plan(),
        "xcore9": xcore9_plan(),
        "aihub": aihub_plan(),
        "codeEngine": code_engine_plan(),
        "benchmarking": benchmarking_plan(),
    }
    if auto_local:
        for section in (
            "integration",
            "fleet",
            "hermes",
            "xcore9",
            "aihub",
            "codeEngine",
            "benchmarking",
        ):
            payload[section]["executionMode"] = "automatic-local"
        return activate_local_contract("setup-all", payload, environment)
    return payload


def print_human(payload: dict[str, Any]) -> None:
    if all(
        key in payload
        for key in (
            "doctor",
            "targets",
            "plan",
            "oidc",
            "devopsSync",
            "runners",
            "edge",
            "integration",
            "fleet",
            "hermes",
            "xcore9",
            "aihub",
            "codeEngine",
            "benchmarking",
        )
    ):
        groups = payload["runners"].get("selfHosted", {}).get("groups", [])
        configured = 0
        for group in groups:
            runners = group.get("runners") if isinstance(group, dict) else None
            if isinstance(runners, list):
                configured += len(runners)
        xcore_runners = payload["xcore9"]["runnerGroup"].get("runners", [])
        print(f"HELIOS full setup bundle ({payload['environment']})")
        print(f"  required tools ready: {str(payload['doctor']['requiredToolsReady']).lower()}")
        print(f"  authorities tracked: {len(payload['targets'].get('authorities', {}))}")
        print(f"  oidc subject: {payload['oidc']['selectedSubject']}")
        print(f"  self-hosted runners: {configured} configured")
        print(
            "  integration routes enabled: "
            + str(payload["integration"]["routes"]["enabled"])
            + "/"
            + str(payload["integration"]["routes"]["total"])
        )
        print(
            "  code engines: "
            + str(len(payload["codeEngine"].get("engines", [])))
        )
        print(
            "  language coverage: "
            + str(len(payload["aihub"].get("languageCoverage", [])))
        )
        print(
            "  Hermes runtime: "
            + payload["hermes"]["runtime"]
            + " ("
            + payload["hermes"]["runtimeState"]
            + ")"
        )
        print(f"  XCore9 runners: {len(xcore_runners)} configured")
        print(
            "  benchmarking metrics: "
            + str(len(payload["benchmarking"].get("metrics", [])))
        )
        print(
            "  edge target: "
            + payload["edge"]["targetEdge"]["service"]
            + " ("
            + payload["edge"]["targetEdge"]["connectivity"]
            + ")"
        )
        if isinstance(payload.get("automation"), dict):
            print(
                "  local automation artifact: "
                + str(payload["automation"].get("contractArtifact", "unspecified"))
            )
        print("  No cloud mutation was performed.")
    elif "tools" in payload:
        print("HELIOS doctor (read-only)")
        for tool in payload["tools"]:
            mark = "OK" if tool["available"] and tool.get("healthy", False) else (
                "OPTIONAL" if not tool["required"] else "MISSING"
            )
            print(f"  [{mark:8}] {tool['command']}")
        print("  Cloud authentication: not checked")
        print("  Azure runtime: not live")
    elif "authorities" in payload:
        print("HELIOS authorities")
        for name, value in payload["authorities"].items():
            print(f"  {name}: {value}")
    elif "selectedSubject" in payload:
        print(f"HELIOS OIDC contract ({payload['environment']})")
        print(f"  issuer: {payload['issuer']}")
        print(f"  audience: {payload['audience']}")
        print(f"  subject: {payload['selectedSubject']}")
        print(f"  immutable subject: {str(payload['useImmutableSubject']).lower()}")
        print("  No identity or RBAC mutation was performed.")
    elif "sourceOfTruth" in payload:
        print("HELIOS Azure DevOps sync plan")
        print(f"  source: {payload['sourceOfTruth']}")
        print(f"  direction: {payload['synchronization']['direction']}")
        print("  writes: disabled")
    elif "selfHosted" in payload:
        print("HELIOS runner topology")
        print(f"  validation: {payload['validation']['provider']}")
        print(f"  release environment: {payload['release']['environment']}")
        self_hosted = payload["selfHosted"]
        if self_hosted.get("enabled"):
            groups = self_hosted.get("groups", [])
            configured = 0
            for group in groups:
                runners = group.get("runners") if isinstance(group, dict) else None
                if isinstance(runners, list):
                    configured += len(runners)
            print(f"  self-hosted runners: enabled ({configured} configured)")
        else:
            print("  self-hosted runners: disabled")
    elif "targetEdge" in payload:
        print(f"HELIOS Azure Edge plan ({payload['environment']})")
        print(f"  service: {payload['targetEdge']['service']}")
        print(f"  connectivity: {payload['targetEdge']['connectivity']}")
        print("  automatic apply: false")
    elif "integrationExecutionMode" in payload and "routes" in payload:
        print("HELIOS integration contract")
        print(f"  integration mode: {payload['integrationExecutionMode']}")
        print(
            "  routes enabled: "
            + str(payload["routes"]["enabled"])
            + "/"
            + str(payload["routes"]["total"])
        )
        if isinstance(payload.get("automation"), dict):
            print(
                "  local automation artifact: "
                + str(payload["automation"].get("contractArtifact", "unspecified"))
            )
            print("  local writes: enabled (cloud writes remain gated)")
        else:
            print("  writes: disabled")
    elif "fleetExecutionMode" in payload and "providers" in payload:
        print("HELIOS local fleet plan")
        print(f"  fleet mode: {payload['fleetExecutionMode']}")
        print(f"  providers: {len(payload['providers'])}")
        if isinstance(payload.get("automation"), dict):
            print(
                "  local automation artifact: "
                + str(payload["automation"].get("contractArtifact", "unspecified"))
            )
        print("  automatic provider runs: false")
    elif "orchestrator" in payload and "learningPolicy" in payload:
        print("HELIOS Hermes plan")
        print(f"  runtime: {payload['runtime']}")
        print(f"  runtime state: {payload['runtimeState']}")
        print(f"  orchestrator: {payload['orchestrator']['id']}")
        if isinstance(payload.get("automation"), dict):
            print(
                "  local automation artifact: "
                + str(payload["automation"].get("contractArtifact", "unspecified"))
            )
        print("  automatic production mutation: false")
    elif "runnerGroup" in payload and "teacher" in payload and "evaluator" in payload:
        print("HELIOS XCore9 plan")
        print(f"  state: {payload['state']}")
        print(f"  runner group: {payload['runnerGroup']['name']}")
        print(f"  runners: {len(payload['runnerGroup']['runners'])}")
        print(
            "  agents: "
            + payload["teacher"]["id"]
            + ", "
            + payload["evaluator"]["id"]
        )
        if isinstance(payload.get("automation"), dict):
            print(
                "  local automation artifact: "
                + str(payload["automation"].get("contractArtifact", "unspecified"))
            )
    elif payload.get("contract") == "aihub-learning-matrix":
        print("HELIOS AIHub learning matrix")
        print(f"  state: {payload['state']}")
        print(f"  providers: {len(payload['providerEngines'])}")
        if "selectedLanguage" in payload:
            print(f"  selected language: {payload['selectedLanguage']}")
            print(f"  primary engines: {len(payload['playbook'].get('primaryEngines', []))}")
        else:
            print(f"  language coverage: {len(payload['languageCoverage'])}")
        if isinstance(payload.get("automation"), dict):
            print(
                "  local automation artifact: "
                + str(payload["automation"].get("contractArtifact", "unspecified"))
            )
        print("  cloud mutation: approval gated")
    elif "engines" in payload and "defaultWorkflow" in payload:
        print("HELIOS code-engine plan")
        print(f"  engines: {len(payload['engines'])}")
        print(f"  workflow stages: {len(payload['defaultWorkflow'])}")
        if isinstance(payload.get("automation"), dict):
            print(
                "  local automation artifact: "
                + str(payload["automation"].get("contractArtifact", "unspecified"))
            )
        print("  writes: bounded by fleet contract")
    elif "metrics" in payload and "stores" in payload:
        print("HELIOS benchmarking plan")
        print(f"  owner: {payload['owner']}")
        print(f"  metrics: {len(payload['metrics'])}")
        print(
            "  cloud stores implemented: "
            + str(payload["stores"]["cloudStoresImplemented"]).lower()
        )
        if isinstance(payload.get("automation"), dict):
            print(
                "  local automation artifact: "
                + str(payload["automation"].get("contractArtifact", "unspecified"))
            )
        print("  production mutation: disabled")
    elif "environment" in payload and "administratorGates" in payload:
        print(f"HELIOS {payload['environment']} release plan")
        for index, gate in enumerate(payload["administratorGates"], start=1):
            print(f"  GATE {index}: {gate}")
        print("  No cloud mutation was performed.")
    else:
        print("HELIOS contract")


def add_environment_argument(parser: argparse.ArgumentParser) -> None:
    parser.add_argument(
        "--environment",
        default="azure-dev",
        choices=("azure-dev", "azure-test", "azure-prod"),
    )
    parser.add_argument("--json", action="store_true")


def add_local_automation_arguments(parser: argparse.ArgumentParser) -> None:
    parser.add_argument(
        "--environment",
        default="azure-dev",
        choices=("azure-dev", "azure-test", "azure-prod"),
    )
    parser.add_argument("--auto-local", action="store_true")
    parser.add_argument("--json", action="store_true")


def add_language_argument(parser: argparse.ArgumentParser) -> None:
    parser.add_argument(
        "--language",
        choices=SUPPORTED_LANGUAGES,
    )


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description="HELIOS plan-first setup helper")
    subparsers = parser.add_subparsers(dest="command", required=True)
    doctor_parser = subparsers.add_parser("doctor", help="Check local prerequisites")
    doctor_parser.add_argument("--json", action="store_true")
    targets_parser = subparsers.add_parser("targets", help="Show canonical authorities")
    targets_parser.add_argument("--json", action="store_true")
    setup_parser = subparsers.add_parser(
        "setup-all",
        help="Show complete HELIOS setup contracts (read-only default, optional --auto-local)",
    )
    add_environment_argument(setup_parser)
    setup_parser.add_argument("--auto-local", action="store_true")
    plan_parser = subparsers.add_parser("plan", help="Create a non-executing release plan")
    add_environment_argument(plan_parser)
    oidc_parser = subparsers.add_parser("oidc", help="Show the exact GitHub-to-Azure OIDC contract")
    add_environment_argument(oidc_parser)
    sync_parser = subparsers.add_parser("devops-sync", help="Show the read-only Azure DevOps sync plan")
    sync_parser.add_argument("--json", action="store_true")
    runner_parser = subparsers.add_parser("runners", help="Show the governed GitHub runner topology")
    runner_parser.add_argument("--json", action="store_true")
    edge_parser = subparsers.add_parser("edge", help="Show the Azure Front Door private-edge activation plan")
    add_environment_argument(edge_parser)
    integration_parser = subparsers.add_parser(
        "integration",
        help="Show HELIOS integration routing and destination contract",
    )
    add_local_automation_arguments(integration_parser)
    fleet_parser = subparsers.add_parser(
        "fleet",
        help="Show HELIOS local multi-agent fleet plan",
    )
    add_local_automation_arguments(fleet_parser)
    hermes_parser = subparsers.add_parser(
        "hermes",
        help="Show Hermes orchestrator runtime and learning contract",
    )
    add_local_automation_arguments(hermes_parser)
    xcore_parser = subparsers.add_parser(
        "xcore9",
        help="Show XCore9 runner and evaluation contract",
    )
    add_local_automation_arguments(xcore_parser)
    aihub_parser = subparsers.add_parser(
        "aihub",
        help="Show AIHub language/model routing and benchmark matrix",
    )
    add_local_automation_arguments(aihub_parser)
    add_language_argument(aihub_parser)
    code_engine_parser = subparsers.add_parser(
        "code-engine",
        help="Show provider code-engine matrix and bounded workflow",
    )
    add_local_automation_arguments(code_engine_parser)
    benchmark_parser = subparsers.add_parser(
        "benchmark",
        help="Show candidate-only benchmarking contract",
    )
    add_local_automation_arguments(benchmark_parser)
    return parser


def main(argv: list[str] | None = None) -> int:
    args = build_parser().parse_args(argv)
    try:
        if args.command == "doctor":
            payload = doctor()
        elif args.command == "targets":
            payload = read_targets()
        elif args.command == "setup-all":
            payload = setup_all(args.environment, auto_local=args.auto_local)
        elif args.command == "plan":
            payload = release_plan(args.environment)
        elif args.command == "oidc":
            payload = oidc_contract(args.environment)
        elif args.command == "devops-sync":
            payload = devops_sync_plan()
        elif args.command == "runners":
            payload = runner_plan()
        elif args.command == "edge":
            payload = edge_plan(args.environment)
        elif args.command == "integration":
            payload = integration_plan()
            if args.auto_local:
                payload = activate_local_contract("integration", payload, args.environment)
        elif args.command == "fleet":
            payload = fleet_plan()
            if args.auto_local:
                payload = activate_local_contract("fleet", payload, args.environment)
        elif args.command == "hermes":
            payload = hermes_plan()
            if args.auto_local:
                payload = activate_local_contract("hermes", payload, args.environment)
        elif args.command == "xcore9":
            payload = xcore9_plan()
            if args.auto_local:
                payload = activate_local_contract("xcore9", payload, args.environment)
        elif args.command == "aihub":
            payload = aihub_plan(args.language)
            if args.auto_local:
                payload = activate_local_contract("aihub", payload, args.environment)
        elif args.command == "code-engine":
            payload = code_engine_plan()
            if args.auto_local:
                payload = activate_local_contract("code-engine", payload, args.environment)
        elif args.command == "benchmark":
            payload = benchmarking_plan()
            if args.auto_local:
                payload = activate_local_contract("benchmark", payload, args.environment)
        else:
            raise ValueError(f"unsupported command '{args.command}'")
    except (RuntimeError, ValueError) as error:
        print(f"HELIOS: {error}", file=sys.stderr)
        return 2
    if args.json:
        print(json.dumps(payload, indent=2, sort_keys=True))
    else:
        print_human(payload)
    return 0


if __name__ == "__main__":
    sys.exit(main())
