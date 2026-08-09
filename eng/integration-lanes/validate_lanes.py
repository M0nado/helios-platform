#!/usr/bin/env python3
"""Fail closed when preview dependencies or unsupported integration contracts leak."""
from __future__ import annotations

import argparse
import json
import re
import subprocess
import sys
import tomllib
from pathlib import Path
from xml.etree import ElementTree

ROOT = Path(__file__).resolve().parents[2]
PREVIEW = ROOT / "eng/integration-lanes/preview"
PRERELEASE = re.compile(r"(?<![A-Za-z])(?:alpha|beta|preview|prerelease|rc)(?:[.-]?\d+)?(?![A-Za-z])", re.I)
REQUIRED = {"dotnet", "azure-ai-foundry", "microsoft-agent-framework", "openai", "anthropic", "langchain", "github", "microsoft-graph", "fabric", "purview", "copilot-studio"}

def fail(message: str, errors: list[str]) -> None:
    errors.append(message)

def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--contracts", action="store_true", help="also execute normalized event contract tests")
    args = parser.parse_args()
    errors: list[str] = []
    manifest = json.loads((ROOT / "eng/integration-lanes/stable-lane.json").read_text())
    allowed_manifest_keys = {"$schema", "schemaVersion", "lane", "supportedTargetFrameworks", "releaseProjects", "releasePolicy", "integrations"}
    if unknown := set(manifest) - allowed_manifest_keys:
        fail(f"stable manifest contains unsupported properties: {sorted(unknown)}", errors)
    names = {item["name"] for item in manifest["integrations"]}
    if len(names) != len(manifest["integrations"]):
        fail("stable manifest integration names must be unique", errors)
    if missing := REQUIRED - names:
        fail(f"stable manifest missing integrations: {sorted(missing)}", errors)
    for item in manifest["integrations"]:
        if not item.get("apiVersions") or not item.get("authentication") or not item.get("deprecationNotice"):
            fail(f"{item['name']} lacks API, authentication, or deprecation metadata", errors)
        if PRERELEASE.search(item["sdk"]):
            fail(f"prerelease SDK in stable manifest: {item['name']}={item['sdk']}", errors)
        if any(PRERELEASE.search(version) for version in item["apiVersions"]):
            fail(f"preview API version in stable manifest: {item['name']}", errors)
        if unsupported_auth := set(item["authentication"]) - set(manifest["releasePolicy"]["authentication"]):
            fail(f"{item['name']} uses unapproved authentication: {sorted(unsupported_auth)}", errors)
    if set(manifest["releasePolicy"]["authentication"]) & set(manifest["releasePolicy"]["forbiddenAuthentication"]):
        fail("authentication cannot be both approved and forbidden", errors)

    global_json = json.loads((ROOT / "global.json").read_text())
    if global_json["sdk"].get("allowPrerelease") is not False or PRERELEASE.search(global_json["sdk"]["version"]):
        fail("global.json must pin a non-preview SDK and set allowPrerelease=false", errors)
    preview_policy = json.loads((PREVIEW / "preview-lane.json").read_text())
    if preview_policy.get("lane") != "preview" or preview_policy.get("publish") is not False:
        fail("preview policy must identify a non-publishing preview lane", errors)
    docker_from = (PREVIEW / "Dockerfile").read_text().splitlines()[0]
    if not re.fullmatch(r"FROM\s+[^\s]+@sha256:[0-9a-f]{64}", docker_from):
        fail("preview container base image must be pinned by sha256 digest", errors)
    for preview_lock in (PREVIEW / "pyproject.toml", PREVIEW / "uv.lock", PREVIEW / "requirements-preview.txt"):
        if not preview_lock.is_file():
            fail(f"isolated preview dependency file is required: {preview_lock.relative_to(ROOT)}", errors)
    preview_requirements = (PREVIEW / "requirements-preview.txt").read_text()
    if "--hash=sha256:" not in preview_requirements:
        fail("preview Python requirements must contain cryptographic hashes", errors)
    if not (ROOT / "services/helios-deployment-agent/uv.lock").is_file():
        fail("stable Python uv.lock is required", errors)
    if not (ROOT / "plugins/openai/helios-mcp/package-lock.json").is_file():
        fail("stable npm package-lock.json is required", errors)
    else:
        npm_lock = json.loads((ROOT / "plugins/openai/helios-mcp/package-lock.json").read_text())
        for package, metadata in npm_lock.get("packages", {}).items():
            if PRERELEASE.search(str(metadata.get("version", ""))):
                fail(f"prerelease npm transitive dependency: {package}={metadata['version']}", errors)
    for lock in ROOT.rglob("packages.lock.json"):
        if PREVIEW in lock.parents:
            continue
        data = json.loads(lock.read_text())
        for tfm in data.get("dependencies", {}).values():
            for package, metadata in tfm.items():
                resolved = str(metadata.get("resolved", ""))
                if PRERELEASE.search(resolved):
                    fail(f"prerelease NuGet transitive dependency: {package}={resolved}", errors)
    uv_lock_path = ROOT / "services/helios-deployment-agent/uv.lock"
    uv_lock = uv_lock_path.read_text()
    for version in re.findall(r'^version = "([^"]+)"$', uv_lock, re.M):
        if PRERELEASE.search(version):
            fail(f"prerelease Python transitive dependency: {version}", errors)
    uv_data = tomllib.loads(uv_lock)
    locked_python = {package["name"]: package["version"] for package in uv_data["package"]}
    openai_pin = next(item["sdk"] for item in manifest["integrations"] if item["name"] == "openai")
    if openai_pin != f"openai-agents=={locked_python.get('openai-agents')}":
        fail("OpenAI stable SDK manifest must exactly match uv.lock", errors)

    for project_name in manifest["releaseProjects"]:
        project = ROOT / project_name
        lock = project.parent / "packages.lock.json"
        if not project.is_file() or not lock.is_file():
            fail(f"release project and lock file are required: {project_name}", errors)

    supported = set(manifest["supportedTargetFrameworks"])
    for project in ROOT.rglob("*.csproj"):
        if PREVIEW in project.parents:
            continue
        tree = ElementTree.parse(project)
        values = [node.text or "" for tag in ("TargetFramework", "TargetFrameworks") for node in tree.iter(tag)]
        for tfm in {x for value in values for x in value.split(";") if x}:
            compatible = tfm in supported or any(tfm.startswith(base) for base in supported if base.endswith("-windows"))
            if tfm.startswith("net") and not compatible:
                fail(f"unsupported target framework {tfm} in {project.relative_to(ROOT)}", errors)

    candidates = [ROOT / "Directory.Packages.props"] + list(ROOT.rglob("*.csproj")) + list(ROOT.rglob("pyproject.toml")) + list(ROOT.rglob("package.json"))
    for path in candidates:
        if PREVIEW in path.parents or {"node_modules", "bin", "obj"} & set(path.parts):
            continue
        for match in PRERELEASE.finditer(path.read_text(errors="ignore")):
            fail(f"preview marker '{match.group()}' in stable file {path.relative_to(ROOT)}", errors)

    if args.contracts and not errors:
        result = subprocess.run([sys.executable, "tests/integration/validate_event_contract.py"], cwd=ROOT)
        if result.returncode:
            fail("normalized integration event contract tests failed", errors)
    if errors:
        print("\n".join(f"ERROR: {error}" for error in errors), file=sys.stderr)
        return 1
    print("Stable/preview lane compatibility checks passed.")
    return 0

if __name__ == "__main__":
    raise SystemExit(main())
