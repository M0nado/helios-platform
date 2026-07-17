from __future__ import annotations

import argparse
import copy
import fnmatch
import hashlib
import json
import re
from pathlib import Path
from typing import Any, Iterable

import jsonschema
import yaml

SEVERITIES = {"debug": 0, "info": 1, "notice": 2, "warning": 3, "error": 4, "critical": 5}
SENSITIVE_KEY = re.compile(r"(secret|token|password|private[_-]?key|authorization)", re.IGNORECASE)
EXTERNAL_CONNECTORS = {
    "github-control",
    "slack-ops",
    "linear-work",
    "sharepoint-evidence",
    "teams-ops",
    "openai-optional",
}


class ValidationError(RuntimeError):
    pass


def load_json(path: Path) -> Any:
    try:
        return json.loads(path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as exc:
        raise ValidationError(f"{path}: {exc}") from exc


def load_yaml(path: Path) -> Any:
    try:
        return yaml.safe_load(path.read_text(encoding="utf-8"))
    except (OSError, yaml.YAMLError) as exc:
        raise ValidationError(f"{path}: {exc}") from exc


def required_paths(root: Path) -> list[Path]:
    relative = [
        "pyproject.toml",
        "config/fabric/event-envelope.schema.json",
        "config/fabric/connector-registry.json",
        "config/fabric/routing-policy.json",
        "config/fabric/approval-policy.json",
        "infra/bicep/main.bicep",
        "infra/bicep/modules/foundation.bicep",
        "infra/bicep/modules/private-endpoint.bicep",
        "src/dotnet/HELIOS.Fabric.sln",
        "src/dotnet/HELIOS.Fabric.Contracts/HELIOS.Fabric.Contracts.csproj",
        "src/dotnet/HELIOS.Fabric.Broker/HELIOS.Fabric.Broker.csproj",
        "src/dotnet/HELIOS.Fabric.Worker/HELIOS.Fabric.Worker.csproj",
        "src/dotnet/HELIOS.Fabric.Tests/HELIOS.Fabric.Tests.csproj",
        "docker/broker.Dockerfile",
        "docker/worker.Dockerfile",
        "scripts/bootstrap/Install-HeliosFabricOverlay.ps1",
        "tests/fixtures/deployment-plan.json",
    ]
    return [root / value for value in relative]


def validate(root: Path) -> dict[str, Any]:
    root = root.resolve()
    errors: list[str] = []
    for path in required_paths(root):
        if not path.is_file():
            errors.append(f"missing required file: {path.relative_to(root)}")

    json_files = sorted(
        path for path in root.rglob("*.json")
        if not any(part in {".git", ".venv", "artifacts", "bin", "obj"} for part in path.parts)
    )
    yaml_files = sorted(
        path for suffix in ("*.yml", "*.yaml") for path in root.rglob(suffix)
        if not any(part in {".git", ".venv", "artifacts", "bin", "obj"} for part in path.parts)
    )
    for path in json_files:
        try:
            load_json(path)
        except ValidationError as exc:
            errors.append(str(exc))
    for path in yaml_files:
        try:
            load_yaml(path)
        except ValidationError as exc:
            errors.append(str(exc))

    registry_path = root / "config/fabric/connector-registry.json"
    routes_path = root / "config/fabric/routing-policy.json"
    approvals_path = root / "config/fabric/approval-policy.json"
    if registry_path.is_file() and routes_path.is_file():
        registry = load_json(registry_path)
        routes = load_json(routes_path)
        if registry.get("defaultMode") != "deny":
            errors.append("connector registry must remain deny-by-default")
        connectors = {item["id"]: item for item in registry.get("connectors", [])}
        duplicates = len(connectors) != len(registry.get("connectors", []))
        if duplicates:
            errors.append("connector IDs must be unique")
        unexpectedly_enabled = sorted(
            item["id"] for item in registry.get("connectors", [])
            if item.get("id") in EXTERNAL_CONNECTORS and item.get("enabled") is not False
        )
        if unexpectedly_enabled:
            errors.append(f"external connectors must default disabled: {', '.join(unexpectedly_enabled)}")
        for route in routes.get("routes", []):
            missing = sorted(set(route.get("sinks", [])) - connectors.keys())
            if missing:
                errors.append(f"route {route.get('id')} references unknown sinks: {', '.join(missing)}")
            if len(route.get("sinks", [])) > int(routes.get("defaults", {}).get("maximumFanout", 5)):
                errors.append(f"route {route.get('id')} exceeds maximum fanout")

    if approvals_path.is_file():
        approvals = load_json(approvals_path)
        if approvals.get("authoritativeSystem") != "github-protected-environments":
            errors.append("GitHub protected environments must remain the approval authority")

    module_refs = []
    main_bicep = root / "infra/bicep/main.bicep"
    if main_bicep.is_file():
        module_refs.extend(re.findall(r"module\s+\w+\s+'([^']+)'", main_bicep.read_text(encoding="utf-8")))
    for source in (root / "infra/bicep/modules").glob("*.bicep") if (root / "infra/bicep/modules").exists() else []:
        module_refs.extend(re.findall(r"module\s+\w+\s+'([^']+)'", source.read_text(encoding="utf-8")))
    for reference in module_refs:
        candidates = [main_bicep.parent / reference, root / "infra/bicep/modules" / reference]
        if not any(candidate.resolve().is_file() for candidate in candidates):
            errors.append(f"missing Bicep module: {reference}")

    if errors:
        raise ValidationError("\n".join(errors))
    return {
        "mode": "validate",
        "root": str(root),
        "jsonFiles": len(json_files),
        "yamlFiles": len(yaml_files),
        "externalConnectorsEnabled": 0,
        "status": "ok",
    }


def validate_event(root: Path, event: dict[str, Any]) -> None:
    schema = load_json(root / "config/fabric/event-envelope.schema.json")
    jsonschema.Draft202012Validator(schema, format_checker=jsonschema.FormatChecker()).validate(event)


def route_matches(route: dict[str, Any], event: dict[str, Any]) -> bool:
    match = route.get("match", {})
    if event.get("environment") not in match.get("environments", []):
        return False
    minimum = SEVERITIES.get(match.get("minimumSeverity", "debug"), 0)
    if SEVERITIES.get(event.get("severity", "debug"), -1) < minimum:
        return False
    if not any(fnmatch.fnmatchcase(event.get("eventType", ""), pattern) for pattern in match.get("eventTypes", [])):
        return False
    required_actions = set(match.get("requestedActions", []))
    if required_actions and not required_actions.intersection(event.get("requestedActions", [])):
        return False
    return True


def simulate(root: Path, fixture: Path, include_disabled: bool) -> dict[str, Any]:
    root = root.resolve()
    event = load_json(fixture.resolve())
    validate_event(root, event)
    registry = load_json(root / "config/fabric/connector-registry.json")
    connector_state = {item["id"]: bool(item.get("enabled")) for item in registry["connectors"]}
    policy = load_json(root / "config/fabric/routing-policy.json")
    selected: list[dict[str, Any]] = []
    delivered: list[str] = []
    for route in sorted(policy["routes"], key=lambda value: int(value.get("priority", 0)), reverse=True):
        if not route.get("enabled", False) or not route_matches(route, event):
            continue
        sinks = [
            sink for sink in route.get("sinks", [])
            if include_disabled or connector_state.get(sink, False)
        ]
        selected.append(
            {
                "routeId": route["id"],
                "sinks": sinks,
                "approvalPolicy": route.get("approvalPolicy"),
                "transform": route.get("transform"),
            }
        )
        delivered.extend(sinks)
        if not route.get("continue", False):
            break
    return {
        "mode": "simulate",
        "eventId": event["eventId"],
        "correlationId": event["correlationId"],
        "includeDisabled": include_disabled,
        "routes": selected,
        "selectedConnectors": sorted(set(delivered)),
        "writesPerformed": [],
    }


def redact(value: Any) -> Any:
    if isinstance(value, dict):
        return {
            key: ("[REDACTED]" if SENSITIVE_KEY.search(key) else redact(item))
            for key, item in value.items()
        }
    if isinstance(value, list):
        return [redact(item) for item in value]
    return value


def evidence(root: Path, fixture: Path, output: Path, include_disabled: bool) -> dict[str, Any]:
    event = load_json(fixture.resolve())
    validate_event(root.resolve(), event)
    simulation = simulate(root, fixture, include_disabled)
    output = output.resolve()
    output.mkdir(parents=True, exist_ok=True)
    payload = {
        "specVersion": "1.0",
        "event": redact(copy.deepcopy(event)),
        "simulation": simulation,
        "generatedBy": "fabricctl",
        "writesPerformed": [],
    }
    data = (json.dumps(payload, indent=2, sort_keys=True) + "\n").encode("utf-8")
    evidence_path = output / "evidence.json"
    evidence_path.write_bytes(data)
    digest = hashlib.sha256(data).hexdigest()
    manifest = {
        "evidence": evidence_path.name,
        "sha256": digest,
        "bytes": len(data),
        "connectorWrites": 0,
    }
    (output / "manifest.json").write_text(json.dumps(manifest, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    return {"mode": "evidence", "output": str(output), **manifest}


def parser() -> argparse.ArgumentParser:
    result = argparse.ArgumentParser(prog="fabricctl", description="HELIOS plan-first fabric tooling.")
    commands = result.add_subparsers(dest="command", required=True)
    validate_parser = commands.add_parser("validate")
    validate_parser.add_argument("--root", type=Path, default=Path.cwd())
    simulate_parser = commands.add_parser("simulate")
    simulate_parser.add_argument("--root", type=Path, default=Path.cwd())
    simulate_parser.add_argument("--fixture", type=Path, required=True)
    simulate_parser.add_argument("--include-disabled", action="store_true")
    evidence_parser = commands.add_parser("evidence")
    evidence_parser.add_argument("--root", type=Path, default=Path.cwd())
    evidence_parser.add_argument("--fixture", type=Path, required=True)
    evidence_parser.add_argument("--output", type=Path, required=True)
    evidence_parser.add_argument("--include-disabled", action="store_true")
    return result


def main(argv: Iterable[str] | None = None) -> int:
    args = parser().parse_args(list(argv) if argv is not None else None)
    try:
        if args.command == "validate":
            result = validate(args.root)
        elif args.command == "simulate":
            result = simulate(args.root, args.fixture, args.include_disabled)
        else:
            result = evidence(args.root, args.fixture, args.output, args.include_disabled)
    except (ValidationError, jsonschema.ValidationError) as exc:
        print(json.dumps({"status": "error", "error": str(exc)}, indent=2))
        return 2
    print(json.dumps(result, indent=2, sort_keys=True))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

