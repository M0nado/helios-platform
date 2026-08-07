#!/usr/bin/env python3
"""Validate one-owner inventory for HELIOS .NET test source files."""
from __future__ import annotations

import argparse
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
MANIFEST = ROOT / "eng/test/test-ownership.json"
LAYERS = {"portable", "windows", "privileged", "integration", "performance", "end-to-end"}
DOTNET_TEST_ROOTS = (
    ROOT / "tests/HELIOS.Platform.Tests",
    ROOT / "tests/analytics/HELIOS.Analytics.FSharp.Tests",
    ROOT / "tests/contracts/HELIOS.Platform.Contracts.Tests",
    ROOT / "tests/Plugins",
    ROOT / "tests/unit",
    ROOT / "src/tests",
    ROOT / "src/core/HELIOS.Platform",
    ROOT / "monado/helios-control/tests/Helios.Connect.Tests",
)
EXCLUDED_DIRS = {"bin", "obj"}


def sources() -> list[str]:
    found: set[str] = set()
    for root in DOTNET_TEST_ROOTS:
        for ext in ("*.cs", "*.fs"):
            for source in root.rglob(ext):
                if EXCLUDED_DIRS.intersection(source.parts):
                    continue
                rel = source.relative_to(ROOT).as_posix()
                if rel.startswith("src/core/HELIOS.Platform/"):
                    if "/Tests/" not in rel and not rel.endswith("Tests.cs"):
                        continue
                found.add(rel)
    for standalone in (
        ROOT / "tests/Phase3Phase5IntegrationTests.cs",
        ROOT / "tests/SandboxTests.cs",
        ROOT / "tests/SecurityValidationTests.cs",
    ):
        if standalone.is_file():
            found.add(standalone.relative_to(ROOT).as_posix())
    return sorted(found)


def owner(path: str) -> str:
    if path.startswith("monado/helios-control/tests/Helios.Connect.Tests/"):
        return "monado/helios-control/tests/Helios.Connect.Tests/Helios.Connect.Tests.csproj"
    if path.startswith("src/core/HELIOS.Platform/Phase10/Users/Tests/"):
        return "src/core/HELIOS.Platform/Phase10/Users/Tests/HELIOS.Platform.Phase10.Users.Tests.csproj"
    if path.startswith("src/tests/"):
        return "src/tests/HELIOS.Platform.Tests.csproj"
    if path.startswith("tests/analytics/HELIOS.Analytics.FSharp.Tests/"):
        return "tests/analytics/HELIOS.Analytics.FSharp.Tests/HELIOS.Analytics.FSharp.Tests.fsproj"
    if path.startswith("tests/contracts/HELIOS.Platform.Contracts.Tests/"):
        return "tests/contracts/HELIOS.Platform.Contracts.Tests/HELIOS.Platform.Contracts.Tests.csproj"
    if path in {"tests/Phase3Phase5IntegrationTests.cs", "tests/SandboxTests.cs"}:
        return "tests/HELIOS.Platform.Tests/HELIOS.Platform.Tests.csproj"
    if path.startswith("tests/Plugins/") or path.startswith("tests/unit/"):
        return "tests/HELIOS.Platform.Tests/HELIOS.Platform.Tests.csproj"
    if path == "tests/SecurityValidationTests.cs":
        return "tests/SecurityValidationTests.csproj"
    if path.startswith("src/core/HELIOS.Platform/"):
        return "tests/HELIOS.Platform.Tests/HELIOS.Platform.Tests.csproj"
    if path.startswith("tests/HELIOS.Platform.Tests/Phase10/Quarantine/"):
        return "tests/HELIOS.Platform.Tests/Phase10/Quarantine/HELIOS.Platform.Tests.Phase10.Quarantine.csproj"
    if path.startswith("tests/HELIOS.Platform.Tests/"):
        return "tests/HELIOS.Platform.Tests/HELIOS.Platform.Tests.csproj"
    raise ValueError(f"unmapped .NET test source: {path}")


def layer(path: str) -> str:
    p = path.lower()
    if "performance" in p or "scalingtest" in p:
        return "performance"
    if "endtoend" in p or "/system/" in p or "e2etest" in p:
        return "end-to-end"
    if "integration" in p or p.startswith("monado/helios-control/tests/"):
        return "integration"
    if any(
        x in p
        for x in (
            "security",
            "vault",
            "driver",
            "quarantine",
            "malware",
            "/users/tests/",
            "deploymenttests",
        )
    ):
        return "privileged"
    if p.endswith(".fs") or p.startswith("tests/analytics/"):
        return "portable"
    return "windows"


def generated() -> dict[str, object]:
    return {
        "schemaVersion": 1,
        "tests": [{"path": p, "project": owner(p), "layer": layer(p)} for p in sources()],
    }


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--write", action="store_true")
    args = parser.parse_args()
    expected = generated()

    if args.write:
        MANIFEST.write_text(json.dumps(expected, indent=2) + "\n", encoding="utf-8")
        print(f"wrote {len(expected['tests'])} test ownership records")
        return

    actual = json.loads(MANIFEST.read_text(encoding="utf-8"))
    errors: list[str] = []
    paths = [item.get("path") for item in actual.get("tests", [])]
    if len(paths) != len(set(paths)):
        errors.append("a test file is mapped more than once")
    if actual != expected:
        errors.append("manifest is stale; run: python3 eng/test/validate_test_ownership.py --write")
    for item in actual.get("tests", []):
        if item.get("layer") not in LAYERS:
            errors.append(f"invalid layer: {item}")
        if not (ROOT / item.get("project", "")).is_file():
            errors.append(f"missing project: {item}")
    if errors:
        raise SystemExit("\n".join(errors))
    print(f"validated {len(paths)} .NET test source files: one project and one layer each")


if __name__ == "__main__":
    main()
