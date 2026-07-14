#!/usr/bin/env python3
"""Fail-closed, dependency-free validation for the HELIOS route registry."""

from __future__ import annotations

import argparse
import json
import re
import sys
import unicodedata
from collections.abc import Iterable, Mapping
from pathlib import Path
from urllib.parse import urlsplit


REPOSITORY_ROOT = Path(__file__).resolve().parents[2]
DEFAULT_REGISTRY = REPOSITORY_ROOT / "config" / "HELIOS_AUTOMATION_ROUTES_V1.json"
EXPECTED_SCHEMA_PATH = "schemas/automation-routes-v1.schema.json"

KNOWN_PROVIDERS = {"github", "azure", "linear", "slack", "teams", "sharepoint"}
DESTINATION_PROVIDERS = {"linear", "slack", "teams", "sharepoint"}
TERMINAL_NOTIFICATION_PROVIDERS = {"slack", "teams"}
KNOWN_OPERATIONS = {
    "broker.normalize-and-persist": "azure",
    "broker.normalize-redact-and-persist": "azure",
    "evidence.stage": "azure",
    "evidence.compose-and-hash": "azure",
    "linear.issue.upsert": "linear",
    "linear.issue.link-or-update": "linear",
    "linear.issue.update": "linear",
    "slack.message.send": "slack",
    "teams.channelMessage.send": "teams",
    "sharepoint.file.upload-immutable": "sharepoint",
}
FIXED_TARGET_PROVIDERS = {
    "azure-service-bus-broker": "azure",
    "azure-artifact-evidence-store": "azure",
}
APPROVAL_CLASSES = set(range(5))
IDEMPOTENCY_INPUTS = [
    "routeId",
    "source.deliveryId",
    "target.logicalId",
    "operation",
]
TEMPLATE_KEYS = {"template", "pathTemplate", "subjectKey", "externalKey", "keyTemplate"}
PLACEHOLDER_PATTERN = re.compile(r"\{([^{}]+)\}")
ENV_TARGET_PATTERN = re.compile(r"^HELIOS_[A-Z0-9_]+_TARGET$")
ROUTE_ID_PATTERN = re.compile(r"^[a-z0-9]+(?:[a-z0-9-]*[a-z0-9])?$")
STATIC_URL_PATTERN = re.compile(r"(?i)\b(?:https?|javascript|data):[^\s]+")
MENTION_PATTERN = re.compile(r"(?i)(?:^|\s)@(?:channel|here|everyone)\b|<![^>]+>|<@[^>]+>")
RAW_HTML_PATTERN = re.compile(r"</?[A-Za-z][^>]*>")
FORBIDDEN_TEMPLATE_SYNTAX = ("${{", "$(", "`", "javascript:", "data:")
BIDI_CONTROLS = {
    "\u061c",
    "\u200e",
    "\u200f",
    "\u202a",
    "\u202b",
    "\u202c",
    "\u202d",
    "\u202e",
    "\u2066",
    "\u2067",
    "\u2068",
    "\u2069",
}


def _is_int(value: object) -> bool:
    return isinstance(value, int) and not isinstance(value, bool)


def _template_values(value: object, path: str = "$.") -> Iterable[tuple[str, str]]:
    if isinstance(value, Mapping):
        for key, child in value.items():
            child_path = f"{path}{key}"
            if key in TEMPLATE_KEYS and isinstance(child, str):
                yield child_path, child
            yield from _template_values(child, f"{child_path}.")
    elif isinstance(value, list):
        for index, child in enumerate(value):
            yield from _template_values(child, f"{path}[{index}].")


def _host_allowed(host: str, allowlist: set[str]) -> bool:
    host = host.lower().rstrip(".")
    for allowed in allowlist:
        allowed = allowed.lower().rstrip(".")
        if allowed.startswith("*."):
            suffix = allowed[1:]
            if host.endswith(suffix) and host != suffix[1:]:
                return True
        elif host == allowed:
            return True
    return False


def _target_provider(
    target: object, destinations: Mapping[str, object]
) -> str | None:
    if not isinstance(target, str):
        return None
    if target in FIXED_TARGET_PROVIDERS:
        return FIXED_TARGET_PROVIDERS[target]
    destination = destinations.get(target)
    if isinstance(destination, Mapping):
        provider = destination.get("provider")
        return provider if isinstance(provider, str) else None
    return None


def _find_cycle(graph: Mapping[str, set[str]]) -> list[str] | None:
    visiting: set[str] = set()
    visited: set[str] = set()
    stack: list[str] = []

    def visit(node: str) -> list[str] | None:
        if node in visiting:
            start = stack.index(node)
            return stack[start:] + [node]
        if node in visited:
            return None
        visiting.add(node)
        stack.append(node)
        for neighbor in sorted(graph.get(node, set())):
            cycle = visit(neighbor)
            if cycle:
                return cycle
        stack.pop()
        visiting.remove(node)
        visited.add(node)
        return None

    for node in sorted(graph):
        cycle = visit(node)
        if cycle:
            return cycle
    return None


def _validate_template(
    path: str,
    template: str,
    allowed_placeholders: set[str],
    allowed_hosts: set[str],
    max_characters: int,
    max_lines: int,
) -> list[str]:
    errors: list[str] = []
    if len(template) > max_characters:
        errors.append(f"{path}: template exceeds {max_characters} characters")
    if template.count("\n") + 1 > max_lines:
        errors.append(f"{path}: template exceeds {max_lines} lines")
    if "\r" in template:
        errors.append(f"{path}: carriage returns are prohibited; source must use LF")
    if unicodedata.normalize("NFC", template) != template:
        errors.append(f"{path}: template is not Unicode NFC-normalized")
    for character in template:
        ordinal = ord(character)
        if character in BIDI_CONTROLS:
            errors.append(f"{path}: bidirectional control character is prohibited")
            break
        if ordinal < 32 and character not in {"\t", "\n"}:
            errors.append(f"{path}: prohibited C0 control character")
            break
    lowered = template.lower()
    for syntax in FORBIDDEN_TEMPLATE_SYNTAX:
        if syntax.lower() in lowered:
            errors.append(f"{path}: unsafe template syntax {syntax!r}")
    if MENTION_PATTERN.search(template):
        errors.append(f"{path}: literal or rendered mention syntax is prohibited")
    if RAW_HTML_PATTERN.search(template):
        errors.append(f"{path}: raw HTML is prohibited")

    placeholders = PLACEHOLDER_PATTERN.findall(template)
    stripped = PLACEHOLDER_PATTERN.sub("", template)
    if "{" in stripped or "}" in stripped:
        errors.append(f"{path}: malformed or nested placeholder syntax")
    for placeholder in placeholders:
        if placeholder not in allowed_placeholders:
            errors.append(f"{path}: unregistered placeholder {{{placeholder}}}")

    for raw_url in STATIC_URL_PATTERN.findall(template):
        url = raw_url.rstrip(".,);]")
        parsed = urlsplit(url)
        if parsed.scheme.lower() != "https":
            errors.append(f"{path}: URL must use https")
            continue
        try:
            port = parsed.port
        except ValueError:
            port = -1
        if parsed.username or parsed.password or parsed.fragment or port not in {None, 443}:
            errors.append(f"{path}: URL credentials, fragments, or non-default ports are prohibited")
        if not parsed.hostname or not _host_allowed(parsed.hostname, allowed_hosts):
            errors.append(f"{path}: URL host is not allowlisted")
    return errors


def validate_registry(registry: object) -> list[str]:
    """Return every validation error. An empty list means the registry is safe to stage."""

    errors: list[str] = []
    if not isinstance(registry, Mapping):
        return ["$: registry must be a JSON object"]

    if registry.get("contractSchema") != EXPECTED_SCHEMA_PATH:
        errors.append(f"$.contractSchema: must equal {EXPECTED_SCHEMA_PATH!r}")
    if registry.get("schemaVersion") != "1.0.0":
        errors.append("$.schemaVersion: only 1.0.0 is accepted")
    if registry.get("status") != "proposed":
        errors.append("$.status: route registry must remain proposed")

    defaults = registry.get("defaults")
    if not isinstance(defaults, Mapping):
        errors.append("$.defaults: must be an object")
        defaults = {}
    if defaults.get("enabled") is not False:
        errors.append("$.defaults.enabled: must be false")
    if defaults.get("dryRun") is not True:
        errors.append("$.defaults.dryRun: must be true")

    destinations = registry.get("logicalDestinations")
    if not isinstance(destinations, Mapping) or not destinations:
        errors.append("$.logicalDestinations: must be a non-empty object")
        destinations = {}
    for logical_id, destination in destinations.items():
        path = f"$.logicalDestinations.{logical_id}"
        if not isinstance(destination, Mapping):
            errors.append(f"{path}: must be an object")
            continue
        extra = set(destination) - {"provider", "resolveFrom"}
        if extra:
            errors.append(f"{path}: public destination metadata is prohibited: {sorted(extra)}")
        provider = destination.get("provider")
        if not isinstance(provider, str) or provider not in DESTINATION_PROVIDERS:
            errors.append(f"{path}.provider: unknown destination provider {provider!r}")
        resolve_from = destination.get("resolveFrom")
        if not isinstance(resolve_from, str) or not ENV_TARGET_PATTERN.fullmatch(resolve_from):
            errors.append(f"{path}.resolveFrom: must name a HELIOS_*_TARGET setting")

    template_policy = registry.get("untrustedTemplatePolicy")
    if not isinstance(template_policy, Mapping):
        errors.append("$.untrustedTemplatePolicy: must be an object")
        template_policy = {}
    if template_policy.get("defaultMode") != "plain-text-only":
        errors.append("$.untrustedTemplatePolicy.defaultMode: must be plain-text-only")
    escaping = template_policy.get("providerPlainTextEscaping")
    if not isinstance(escaping, Mapping) or set(escaping) != DESTINATION_PROVIDERS:
        errors.append(
            "$.untrustedTemplatePolicy.providerPlainTextEscaping: must define only linear, slack, teams, and sharepoint"
        )
    elif any(not isinstance(value, str) or not value for value in escaping.values()):
        errors.append("$.untrustedTemplatePolicy.providerPlainTextEscaping: rules must be non-empty strings")

    mention_policy = template_policy.get("mentionSuppression")
    if not isinstance(mention_policy, Mapping):
        errors.append("$.untrustedTemplatePolicy.mentionSuppression: must be an object")
    else:
        if mention_policy.get("enabled") is not True:
            errors.append("$.untrustedTemplatePolicy.mentionSuppression.enabled: must be true")
        if mention_policy.get("rejectLiteralBroadcastMentions") is not True:
            errors.append("$.untrustedTemplatePolicy.mentionSuppression: broadcast mentions must be rejected")
        if mention_policy.get("rejectRenderedUserGroupAndUserMentions") is not True:
            errors.append("$.untrustedTemplatePolicy.mentionSuppression: rendered mentions must be rejected")
        if mention_policy.get("allowMentions") != []:
            errors.append("$.untrustedTemplatePolicy.mentionSuppression.allowMentions: must be empty")

    url_policy = template_policy.get("urls")
    if not isinstance(url_policy, Mapping):
        errors.append("$.untrustedTemplatePolicy.urls: must be an object")
        url_policy = {}
    if url_policy.get("scheme") != "https":
        errors.append("$.untrustedTemplatePolicy.urls.scheme: must be https")
    allowed_host_values = url_policy.get("allowedHosts")
    if not isinstance(allowed_host_values, list) or not all(
        isinstance(value, str) for value in allowed_host_values
    ):
        errors.append("$.untrustedTemplatePolicy.urls.allowedHosts: must be a string array")
        allowed_hosts: set[str] = set()
    else:
        allowed_hosts = set(allowed_host_values)
        if allowed_hosts != {"github.com", "linear.app", "*.sharepoint.com"}:
            errors.append("$.untrustedTemplatePolicy.urls.allowedHosts: contains an unapproved host policy")
    if url_policy.get("rejectCredentialsFragmentsAndNonDefaultPorts") is not True:
        errors.append("$.untrustedTemplatePolicy.urls: unsafe URL components must be rejected")

    length_policy = template_policy.get("length")
    if not isinstance(length_policy, Mapping):
        errors.append("$.untrustedTemplatePolicy.length: must be an object")
        length_policy = {}
    max_characters = length_policy.get("maxRenderedCharacters", 0)
    max_lines = length_policy.get("maxRenderedLines", 0)
    if not _is_int(max_characters) or not 1 <= max_characters <= 4000:
        errors.append("$.untrustedTemplatePolicy.length.maxRenderedCharacters: must be 1..4000")
        max_characters = 4000
    if not _is_int(max_lines) or not 1 <= max_lines <= 20:
        errors.append("$.untrustedTemplatePolicy.length.maxRenderedLines: must be 1..20")
        max_lines = 20

    unicode_policy = template_policy.get("unicodeAndNewlines")
    expected_unicode_policy = {
        "normalization": "NFC",
        "newline": "LF",
        "stripCarriageReturns": True,
        "rejectNulAndC0ControlsExceptTabAndLf": True,
        "rejectBidirectionalOverridesAndIsolates": True,
    }
    if not isinstance(unicode_policy, Mapping) or any(
        unicode_policy.get(key) != value for key, value in expected_unicode_policy.items()
    ):
        errors.append("$.untrustedTemplatePolicy.unicodeAndNewlines: fail-closed NFC/LF rules are required")

    allowed_placeholder_values = template_policy.get("allowedPlaceholders")
    if not isinstance(allowed_placeholder_values, list) or not all(
        isinstance(value, str) for value in allowed_placeholder_values
    ):
        errors.append("$.untrustedTemplatePolicy.allowedPlaceholders: must be a string array")
        allowed_placeholders: set[str] = set()
    else:
        allowed_placeholders = set(allowed_placeholder_values)
        if len(allowed_placeholders) != len(allowed_placeholder_values):
            errors.append("$.untrustedTemplatePolicy.allowedPlaceholders: duplicates are prohibited")

    idempotency_policy = template_policy.get("idempotency")
    expected_idempotency = {
        "inputFields": IDEMPOTENCY_INPUTS,
        "normalization": "NFC-trim-ASCII-casefold-for-registered-identifiers",
        "componentEncoding": "length-prefixed-UTF-8",
        "hashAlgorithm": "sha256",
        "output": "lowercase-hex",
        "rawUntrustedTextIncluded": False,
    }
    if not isinstance(idempotency_policy, Mapping) or any(
        idempotency_policy.get(key) != value for key, value in expected_idempotency.items()
    ):
        errors.append("$.untrustedTemplatePolicy.idempotency: normalized SHA-256 policy is required")
    default_idempotency = defaults.get("idempotency")
    if not isinstance(default_idempotency, Mapping) or not str(
        default_idempotency.get("keyTemplate", "")
    ).startswith("sha256(normalized-length-prefixed:"):
        errors.append("$.defaults.idempotency.keyTemplate: must hash normalized length-prefixed inputs")

    approval_classes = registry.get("approvalClasses")
    if not isinstance(approval_classes, Mapping) or set(approval_classes) != {
        str(value) for value in APPROVAL_CLASSES
    }:
        errors.append("$.approvalClasses: exactly classes 0 through 4 are required")

    routes = registry.get("routes")
    if not isinstance(routes, list) or not routes:
        errors.append("$.routes: must be a non-empty array")
        routes = []
    seen_route_ids: set[str] = set()
    graph: dict[str, set[str]] = {}
    reopened_covered = False
    for route_index, route in enumerate(routes):
        path = f"$.routes[{route_index}]"
        if not isinstance(route, Mapping):
            errors.append(f"{path}: must be an object")
            continue
        route_id = route.get("id")
        if not isinstance(route_id, str) or not ROUTE_ID_PATTERN.fullmatch(route_id):
            errors.append(f"{path}.id: invalid route id")
        elif route_id in seen_route_ids:
            errors.append(f"{path}.id: duplicate route id {route_id!r}")
        else:
            seen_route_ids.add(route_id)
        if route.get("enabled") is not False:
            errors.append(f"{path}.enabled: every route must remain false")
        if route.get("dryRun") is not True:
            errors.append(f"{path}.dryRun: every route must remain true")
        route_approval = route.get("approvalClass")
        if not _is_int(route_approval) or route_approval not in APPROVAL_CLASSES:
            errors.append(f"{path}.approvalClass: unknown approval class {route_approval!r}")

        trigger = route.get("trigger")
        if not isinstance(trigger, Mapping):
            errors.append(f"{path}.trigger: must be an object")
            continue
        source_provider = trigger.get("provider")
        if not isinstance(source_provider, str) or source_provider not in KNOWN_PROVIDERS:
            errors.append(f"{path}.trigger.provider: unknown provider {source_provider!r}")
            source_provider = None
        elif source_provider in TERMINAL_NOTIFICATION_PROVIDERS:
            errors.append(f"{path}.trigger.provider: notification provider must remain terminal")
        if trigger.get("event") == "pull_request":
            actions = trigger.get("actions")
            if isinstance(actions, list) and "reopened" in actions:
                reopened_covered = True

        steps = route.get("steps")
        if not isinstance(steps, list) or not steps:
            errors.append(f"{path}.steps: must be a non-empty array")
            continue
        orders: list[int] = []
        for step_index, step in enumerate(steps):
            step_path = f"{path}.steps[{step_index}]"
            if not isinstance(step, Mapping):
                errors.append(f"{step_path}: must be an object")
                continue
            order = step.get("order")
            if not _is_int(order) or order < 1:
                errors.append(f"{step_path}.order: must be a positive integer")
            else:
                orders.append(order)
            operation = step.get("operation")
            expected_provider = (
                KNOWN_OPERATIONS.get(operation) if isinstance(operation, str) else None
            )
            if expected_provider is None:
                errors.append(f"{step_path}.operation: unknown operation {operation!r}")
            approval = step.get("approvalClass")
            if not _is_int(approval) or approval not in APPROVAL_CLASSES:
                errors.append(f"{step_path}.approvalClass: unknown approval class {approval!r}")
            target = step.get("target")
            target_provider = _target_provider(target, destinations)
            if target_provider is None:
                errors.append(f"{step_path}.target: unknown logical or fixed target {target!r}")
            elif expected_provider and target_provider != expected_provider:
                errors.append(
                    f"{step_path}: operation provider {expected_provider!r} does not match target provider {target_provider!r}"
                )
            if source_provider and target_provider:
                if source_provider == target_provider:
                    errors.append(f"{step_path}: self-targeting route is prohibited")
                graph.setdefault(source_provider, set()).add(target_provider)
        if orders and sorted(orders) != list(range(1, len(steps) + 1)):
            errors.append(f"{path}.steps: order values must be unique and contiguous from 1")

    if not reopened_covered:
        errors.append("$.routes: pull_request routing must subscribe to reopened")
    cycle = _find_cycle(graph)
    if cycle:
        errors.append(f"$.routes: provider route graph contains a cycle: {' -> '.join(cycle)}")

    for path, template in _template_values(registry):
        errors.extend(
            _validate_template(
                path,
                template,
                allowed_placeholders,
                allowed_hosts,
                max_characters,
                max_lines,
            )
        )
    return errors


def validate_files(registry_path: Path) -> list[str]:
    errors: list[str] = []
    try:
        registry = json.loads(registry_path.read_text(encoding="utf-8"))
    except (OSError, UnicodeError, json.JSONDecodeError) as exc:
        return [f"{registry_path}: cannot load registry: {exc}"]
    errors.extend(validate_registry(registry))

    schema_path = REPOSITORY_ROOT / EXPECTED_SCHEMA_PATH
    try:
        schema = json.loads(schema_path.read_text(encoding="utf-8"))
    except (OSError, UnicodeError, json.JSONDecodeError) as exc:
        errors.append(f"{schema_path}: cannot load schema: {exc}")
    else:
        if schema.get("$schema") != "https://json-schema.org/draft/2020-12/schema":
            errors.append(f"{schema_path}: must declare JSON Schema draft 2020-12")
        if schema.get("$id") != "https://schemas.helios.invalid/automation-routes-v1.schema.json":
            errors.append(f"{schema_path}: unexpected schema identifier")
    return errors


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--registry", type=Path, default=DEFAULT_REGISTRY)
    arguments = parser.parse_args(argv)
    errors = validate_files(arguments.registry.resolve())
    if errors:
        print("Automation route validation failed:", file=sys.stderr)
        for error in errors:
            print(f"- {error}", file=sys.stderr)
        return 1
    print(f"Automation route registry valid: {arguments.registry}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
