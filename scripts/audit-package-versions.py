#!/usr/bin/env python3
"""Fail when shared NuGet package versions diverge from Directory.Packages.props."""
from __future__ import annotations

import sys
import xml.etree.ElementTree as ET
from collections import defaultdict
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CENTRAL = ROOT / "Directory.Packages.props"


def parse_xml(path: Path) -> ET.Element:
    try:
        return ET.parse(path).getroot()
    except ET.ParseError as exc:
        raise SystemExit(f"{path.relative_to(ROOT)} is not valid XML: {exc}") from exc


def attr(element: ET.Element, name: str) -> str | None:
    return element.attrib.get(name) or element.attrib.get(f"{{http://schemas.microsoft.com/developer/msbuild/2003}}{name}")


def main() -> int:
    if not CENTRAL.exists():
        print("Directory.Packages.props is required for centralized package versions.", file=sys.stderr)
        return 1

    central_versions: dict[str, str] = {}
    duplicate_central: dict[str, set[str]] = defaultdict(set)
    for package_version in parse_xml(CENTRAL).iter():
        if package_version.tag.endswith("PackageVersion"):
            name = attr(package_version, "Include") or attr(package_version, "Update")
            version = attr(package_version, "Version")
            if name and version:
                if name in central_versions and central_versions[name] != version:
                    duplicate_central[name].update({central_versions[name], version})
                central_versions[name] = version

    inline_versions: dict[str, dict[str, set[str]]] = defaultdict(lambda: defaultdict(set))
    missing_central: dict[str, set[str]] = defaultdict(set)
    for project in sorted(ROOT.rglob("*.csproj")):
        if any(part in {"bin", "obj"} for part in project.relative_to(ROOT).parts):
            continue
        for package_reference in parse_xml(project).iter():
            if not package_reference.tag.endswith("PackageReference"):
                continue
            name = attr(package_reference, "Include") or attr(package_reference, "Update")
            version = attr(package_reference, "Version")
            if not name:
                continue
            rel = str(project.relative_to(ROOT))
            if version:
                inline_versions[name][version].add(rel)
            if name not in central_versions:
                missing_central[name].add(rel)

    errors: list[str] = []
    for name, versions in sorted(duplicate_central.items()):
        errors.append(f"{name} has multiple central versions: {', '.join(sorted(versions))}")
    for name, versions in sorted(inline_versions.items()):
        version_list = ", ".join(f"{version} ({', '.join(sorted(paths))})" for version, paths in sorted(versions.items()))
        errors.append(f"{name} uses inline PackageReference Version metadata: {version_list}")
    for name, paths in sorted(missing_central.items()):
        errors.append(f"{name} is referenced without a central PackageVersion: {', '.join(sorted(paths))}")

    if errors:
        print("Package version audit failed:", file=sys.stderr)
        for error in errors:
            print(f"- {error}", file=sys.stderr)
        return 1

    print(f"Package version audit passed for {len(central_versions)} centrally managed packages.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
