#!/usr/bin/env python3
"""Validate the HELIOS tools, plugins, and services implementation contracts."""

from __future__ import annotations

import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
CONFIG = ROOT / "config" / "integrations"


def load_json(path: Path) -> dict:
    with path.open("r", encoding="utf-8") as handle:
        value = json.load(handle)
    if not isinstance(value, dict):
        raise ValueError(f"{path} must contain a JSON object")
    return value


def require(condition: bool, message: str) -> None:
    if not condition:
        raise ValueError(message)


def unique_names(items: list[dict], kind: str) -> None:
    names = [item.get("name") for item in items]
    require(all(isinstance(name, str) and name for name in names), f"Every {kind} requires a name")
    require(len(names) == len(set(names)), f"Duplicate {kind} names are not allowed")


def validate_tools(document: dict) -> list[dict]:
    require(document.get("schemaVersion") == "1.0", "Tool catalog schemaVersion must be 1.0")
    tools = document.get("tools")
    require(isinstance(tools, list) and tools, "Tool catalog must contain tools")
    unique_names(tools, "tool")

    allowed_approvals = {"none", "review", "environment", "production"}
    allowed_environments = {"local", "development", "test", "staging", "production"}

    for tool in tools:
        for field in ("title", "description", "namespace", "inputSchema"):
            require(field in tool, f"Tool {tool['name']} is missing {field}")
        for field in ("readOnly", "openWorld", "destructive"):
            require(isinstance(tool.get(field), bool), f"Tool {tool['name']} requires boolean {field}")
        require(tool.get("approval") in allowed_approvals, f"Tool {tool['name']} has an invalid approval")
        environments = tool.get("allowedEnvironments")
        require(isinstance(environments, list) and environments, f"Tool {tool['name']} needs environments")
        require(set(environments) <= allowed_environments, f"Tool {tool['name']} has an invalid environment")
        require(tool.get("destructive") is False, f"Broker v1 must not publish destructive tool {tool['name']}")
        if not tool.get("readOnly"):
            require(tool.get("approval") != "none", f"Write tool {tool['name']} must require approval")

    return tools


def validate_services(document: dict) -> list[dict]:
    require(document.get("schemaVersion") == "1.0", "Service catalog schemaVersion must be 1.0")
    services = document.get("services")
    require(isinstance(services, list) and services, "Service catalog must contain services")
    unique_names(services, "service")
    for service in services:
        for field in ("owner", "kind", "status", "authentication", "secretSource"):
            require(isinstance(service.get(field), str) and service[field], f"Service {service['name']} is missing {field}")
        require(service["secretSource"] != "source-code", f"Service {service['name']} cannot source secrets from code")
    return services


def validate_plugins(document: dict) -> list[dict]:
    require(document.get("schemaVersion") == "1.0", "Plugin catalog schemaVersion must be 1.0")
    plugins = document.get("plugins")
    require(isinstance(plugins, list) and plugins, "Plugin catalog must contain plugins")
    unique_names(plugins, "plugin")
    for plugin in plugins:
        path = plugin.get("path")
        require(isinstance(path, str) and path, f"Plugin {plugin['name']} requires a path")
        require((ROOT / path).exists(), f"Plugin path does not exist: {path}")
    return plugins


def validate_paths() -> None:
    required = [
        ROOT / "src/services/HELIOS.IntegrationBroker/HELIOS.IntegrationBroker.csproj",
        ROOT / "src/services/HELIOS.IntegrationBroker/Program.cs",
        ROOT / "plugins/openai/helios-mcp/package.json",
        ROOT / "plugins/openai/helios-mcp/src/server.mjs",
        ROOT / "plugins/openai/helios-mcp/src/http.mjs",
        ROOT / "plugins/copilot-studio/helios-openapi.yaml",
        ROOT / "docs/integrations/TOOLS_PLUGINS_SERVICES.md",
    ]
    for path in required:
        require(path.exists(), f"Required implementation path is missing: {path.relative_to(ROOT)}")

    package = load_json(ROOT / "plugins/openai/helios-mcp/package.json")
    sdk_version = package.get("dependencies", {}).get("@modelcontextprotocol/sdk")
    require(sdk_version == "1.29.0", "MCP SDK must remain explicitly pinned to reviewed v1.29.0")

    openapi = (ROOT / "plugins/copilot-studio/helios-openapi.yaml").read_text(encoding="utf-8")
    for operation in ("getHeliosStatus", "listHeliosTools", "previewHeliosAction", "requestHeliosAction"):
        require(f"operationId: {operation}" in openapi, f"Copilot Studio contract is missing {operation}")
    require("unrestricted command execution" not in openapi.lower(), "Copilot Studio contract must not expose unrestricted command execution")


def main() -> None:
    tools = validate_tools(load_json(CONFIG / "tool-catalog.json"))
    services = validate_services(load_json(CONFIG / "service-catalog.json"))
    plugins = validate_plugins(load_json(CONFIG / "plugin-catalog.json"))
    validate_paths()
    print(f"validated {len(tools)} tools, {len(services)} services, and {len(plugins)} plugins")


if __name__ == "__main__":
    main()
