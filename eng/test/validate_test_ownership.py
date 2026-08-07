#!/usr/bin/env python3
"""Validate the exhaustive, one-owner HELIOS test inventory."""
from __future__ import annotations
import argparse, json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
MANIFEST = ROOT / "eng/test/test-ownership.json"
LAYERS = {"portable", "windows", "privileged", "integration", "performance", "end-to-end"}
IGNORED_DIRS = {"obj", "bin", ".git", ".vs"}

def sources():
    roots = [ROOT / "tests", ROOT / "src/tests", ROOT / "monado/helios-control/tests"]
    found = {
        p.relative_to(ROOT).as_posix()
        for root in roots
        for ext in ("*.cs", "*.fs")
        for p in root.rglob(ext)
        if not any(part in IGNORED_DIRS for part in p.parts)
    }
    core = ROOT / "src/core/HELIOS.Platform"
    found |= {
        p.relative_to(ROOT).as_posix()
        for p in core.rglob("*.cs")
        if ("Tests" in p.parts or p.name.endswith("Tests.cs")) and
        not any(part in IGNORED_DIRS for part in p.parts)
    }
    return sorted(found)

def owner(path):
    if path.startswith("monado/helios-control/tests/"): return "monado/helios-control/tests/Helios.Connect.Tests/Helios.Connect.Tests.csproj"
    if path.startswith("src/core/HELIOS.Platform/Phase10/Users/Tests/"): return "src/core/HELIOS.Platform/Phase10/Users/Tests/HELIOS.Platform.Phase10.Users.Tests.csproj"
    if path.startswith("src/tests/"): return "src/tests/HELIOS.Platform.Tests.csproj"
    if path.startswith("tests/analytics/"): return "tests/analytics/HELIOS.Analytics.FSharp.Tests/HELIOS.Analytics.FSharp.Tests.fsproj"
    if path == "tests/SecurityValidationTests.cs": return "tests/SecurityValidationTests.csproj"
    if path.startswith("tests/HELIOS.Platform.Tests/Phase10/Quarantine/"): return "tests/HELIOS.Platform.Tests/Phase10/Quarantine/HELIOS.Platform.Tests.Phase10.Quarantine.csproj"
    return "tests/HELIOS.Platform.Tests/HELIOS.Platform.Tests.csproj"

def layer(path):
    p = path.lower()
    if "performance" in p or "scalingtest" in p: return "performance"
    if "endtoend" in p or "/system/" in p or "e2etest" in p: return "end-to-end"
    if "integration" in p or p.startswith("monado/helios-control/tests/"): return "integration"
    if any(x in p for x in ("security", "vault", "driver", "quarantine", "malware", "/users/tests/", "deploymenttests")): return "privileged"
    if p.endswith(".fs") or p.startswith("tests/analytics/"): return "portable"
    return "windows"

def generated():
    return {"schemaVersion": 1, "tests": [{"path": p, "project": owner(p), "layer": layer(p)} for p in sources()]}

def main():
    ap=argparse.ArgumentParser(); ap.add_argument("--write", action="store_true"); args=ap.parse_args()
    expected=generated()
    if args.write:
        MANIFEST.write_text(json.dumps(expected, indent=2) + "\n")
        print(f"wrote {len(expected['tests'])} test ownership records")
        return
    actual=json.loads(MANIFEST.read_text())
    errors=[]
    paths=[x.get("path") for x in actual.get("tests", [])]
    if len(paths) != len(set(paths)): errors.append("a test file is mapped more than once")
    if actual != expected: errors.append("manifest is stale; run: python3 eng/test/validate_test_ownership.py --write")
    for item in actual.get("tests", []):
        if item.get("layer") not in LAYERS: errors.append(f"invalid layer: {item}")
        if not (ROOT/item.get("project", "")).is_file(): errors.append(f"missing project: {item}")
    if errors: raise SystemExit("\n".join(errors))
    print(f"validated {len(paths)} test files: one project and one layer each")
if __name__ == "__main__": main()
