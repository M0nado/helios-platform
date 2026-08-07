#!/usr/bin/env python3
"""Validate the consolidated HELIOS capability backlog and dependency graph."""

import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
PATH = ROOT / "config" / "capabilities" / "major-capabilities.v1.json"
SCHEMA_PATH = ROOT / "config" / "capabilities" / "major-capabilities.schema.json"
OWNERS = {"csharp", "cpp", "fsharp", "python", "platform"}
GATES = {"standard", "approval", "what-if-and-approval"}
ID_PATTERN = re.compile(r"^CAP-\d{4}$")


def find_cycle(graph: dict[str, list[str]]) -> list[str] | None:
    visiting: set[str] = set()
    visited: set[str] = set()

    def visit(node: str, path: list[str]) -> list[str] | None:
        if node in visiting:
            return path[path.index(node):] + [node]
        if node in visited:
            return None
        visiting.add(node)
        for dependency in graph[node]:
            cycle = visit(dependency, path + [dependency])
            if cycle:
                return cycle
        visiting.remove(node)
        visited.add(node)
        return None

    return next((cycle for node in graph if (cycle := visit(node, [node]))), None)


def validate(data: dict) -> list[str]:
    items = data.get("capabilities", [])
    errors: list[str] = []
    ids = [item.get("id") for item in items]
    known_ids = set(ids)
    if data.get("schemaVersion") != "1.1":
        errors.append("schemaVersion must be 1.1")
    if len(items) != 50:
        errors.append(f"expected 50 capabilities, found {len(items)}")
    if len(known_ids) != len(ids):
        errors.append("capability IDs must be unique")
    for item in items:
        item_id = item.get("id", "<missing>")
        if not isinstance(item_id, str) or not ID_PATTERN.fullmatch(item_id):
            errors.append(f"{item_id}: invalid ID")
        if item.get("owner") not in OWNERS:
            errors.append(f"{item_id}: invalid owner")
        if item.get("gate") not in GATES:
            errors.append(f"{item_id}: invalid gate")
        if not 1 <= item.get("priority", 0) <= 5:
            errors.append(f"{item_id}: priority must be 1..5")
        if len(item.get("acceptanceCriteria", [])) < 2:
            errors.append(f"{item_id}: at least two acceptance criteria are required")
        unknown = set(item.get("dependencies", [])) - known_ids
        if unknown:
            errors.append(f"{item_id}: unknown dependencies: {', '.join(sorted(unknown))}")
        if item_id in item.get("dependencies", []):
            errors.append(f"{item_id}: cannot depend on itself")
    if not errors:
        graph = {item["id"]: item["dependencies"] for item in items}
        if cycle := find_cycle(graph):
            errors.append("dependency cycle: " + " -> ".join(cycle))
    defaults = data.get("defaults", {})
    if not defaults.get("checks") or not defaults.get("evidence"):
        errors.append("default checks and evidence are required")
    return errors


def main() -> int:
    data = json.loads(PATH.read_text(encoding="utf-8"))
    json.loads(SCHEMA_PATH.read_text(encoding="utf-8"))
    errors = validate(data)
    if errors:
        print("\n".join(errors))
        return 1
    print(f"validated {len(data['capabilities'])} governed capabilities and an acyclic dependency graph")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
