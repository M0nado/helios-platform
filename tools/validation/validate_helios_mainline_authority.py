#!/usr/bin/env python3
"""Validate the HELIOS mainline authority contract without mutating the repository."""

from __future__ import annotations

import argparse
import json
import re
import sys
from pathlib import Path
from typing import Any

EXPECTED_PROFILES = [
    ("core", "核"),
    ("developer", "創"),
    ("studio", "響"),
    ("gamer", "迅"),
    ("ai-server", "智"),
    ("sysadmin", "統"),
]
EXPECTED_AZURE_SEQUENCE = [
    "resolve-live-oidc-subject",
    "protect-github-environments",
    "establish-environment-bound-federation",
    "grant-scoped-rbac",
    "bind-managed-identity",
    "publish-immutable-image",
    "run-exact-development-what-if",
    "review-plan-and-hash",
    "request-separate-deployment-approval",
    "authorize-tenant-connectors",
]
SHA256_PATTERN = re.compile(r"^[0-9a-f]{64}$")


class ContractError(RuntimeError):
    """Raised when the authority contract violates a required invariant."""


def require(condition: bool, message: str) -> None:
    if not condition:
        raise ContractError(message)


def load_json(path: Path) -> dict[str, Any]:
    try:
        data = json.loads(path.read_text(encoding="utf-8"))
    except FileNotFoundError as exc:
        raise ContractError(f"Missing authority contract: {path}") from exc
    except json.JSONDecodeError as exc:
        raise ContractError(f"Invalid JSON in {path}: {exc}") from exc
    require(isinstance(data, dict), "Authority contract root must be an object.")
    return data


def validate(data: dict[str, Any]) -> dict[str, Any]:
    require(data.get("schemaVersion") == 1, "schemaVersion must be 1.")
    require(data.get("canonicalRepository") == "M0nado/helios-platform", "Unexpected canonical repository.")
    require(data.get("targetBranch") == "main", "Target branch must remain main.")

    merge = data.get("mergePolicy", {})
    require(merge.get("directPushToMain") is False, "Direct pushes to main must remain disabled.")
    require(merge.get("draftPullRequestFirst") is True, "Consolidation must begin as a draft PR.")
    require(merge.get("requireExactHead") is True, "Expected-head enforcement is required.")
    require(merge.get("requireConfiguredChecks") is True, "At least one configured required check is mandatory.")
    require(merge.get("requireAllConfiguredChecksSuccess") is True, "All configured checks must pass.")
    require(merge.get("requireApprovedReview") is True, "An approved review is required.")
    require(merge.get("requireWindowsPowerShellAst") is True, "Native Windows PowerShell parsing is required.")
    require(merge.get("conflictsDefault") == "deny", "Unreviewed conflicts must fail closed.")

    desktop = data.get("desktop", {})
    require(desktop.get("framework") == "WinUI 3", "Desktop framework must be WinUI 3.")
    require(desktop.get("language") == "C#", "Desktop language must be C#.")
    require(desktop.get("xamlNamespace") == "Microsoft.UI.Xaml", "WinUI 3 must use Microsoft.UI.Xaml.")
    require(desktop.get("wpfAllowedInActiveProduct") is False, "WPF must remain prohibited in active product code.")

    profiles = data.get("profiles")
    require(isinstance(profiles, list), "profiles must be a list.")
    actual_profiles = [(item.get("id"), item.get("glyph")) for item in profiles if isinstance(item, dict)]
    require(actual_profiles == EXPECTED_PROFILES, "Canonical profile IDs or glyph ordering changed.")
    require(len({item[0] for item in actual_profiles}) == 6, "Profile IDs must be unique.")
    require(len({item[1] for item in actual_profiles}) == 6, "Profile glyphs must be unique.")
    sysadmin = profiles[-1]
    require(sysadmin.get("privileged") is True, "Sysadmin must be the only privileged profile.")
    require(sysadmin.get("localOfflineOnly") is True, "Sysadmin must remain local/offline only.")
    require(all(item.get("privileged") is False for item in profiles[:-1]), "Normal profiles cannot be privileged.")

    workflows = data.get("nonIdentitySecurityWorkflows")
    require(workflows == ["recovery", "quarantine"], "Recovery and Quarantine must remain non-identity workflows.")

    aihub = data.get("aihub", {})
    require(aihub.get("defaultBind") == "127.0.0.1", "AIHub must default to loopback.")
    require(aihub.get("nonHealthAuthenticationRequired") is True, "Protected AIHub routes require authentication.")
    require(aihub.get("requestSizeBounded") is True, "AIHub requests must be size-bounded.")
    require(aihub.get("rateLimited") is True, "AIHub must be rate-limited.")
    require(aihub.get("atomicStateWrites") is True, "AIHub state writes must be atomic.")
    require(aihub.get("secretReadbackAllowed") is False, "Secret readback must remain denied.")
    require(aihub.get("administrativeExecutionDefault") == "proposal-only", "Admin actions must default to proposal-only.")

    azure = data.get("azure", {})
    require(azure.get("applyEnabled") is False, "Azure apply must remain disabled in source consolidation.")
    require(azure.get("productionEnabled") is False, "Production must remain disabled.")
    require(azure.get("liveStateRequiresExternalEvidence") is True, "Cloud-live claims require external evidence.")
    require(azure.get("requiredSequence") == EXPECTED_AZURE_SEQUENCE, "Azure activation sequence changed.")

    secrets = data.get("secrets", {})
    require(secrets.get("repositoryStorageAllowed") is False, "Repository secret storage must remain denied.")
    require(secrets.get("collaborationSurfaceStorageAllowed") is False, "Collaboration surfaces cannot store secrets.")
    require(secrets.get("readbackAllowed") is False, "Secret readback must remain denied.")
    require(secrets.get("previouslyExposedOpenAIKeyStatus") == "must-remain-revoked", "Exposed key status changed.")

    package = data.get("sourcePackage", {})
    require(SHA256_PATTERN.fullmatch(str(package.get("sha256", ""))) is not None, "Source package SHA-256 is invalid.")
    require(package.get("generatedOverlayFiles") == 145, "Unexpected generated overlay file count.")
    require(package.get("localPythonTestsPassed") == 18, "Unexpected local test count.")
    require(package.get("hostedValidationStillRequired") is True, "Hosted validation cannot be waived.")

    return {
        "status": "passed",
        "schemaVersion": data["schemaVersion"],
        "profiles": len(profiles),
        "desktop": desktop["framework"],
        "aihubBind": aihub["defaultBind"],
        "azureApplyEnabled": azure["applyEnabled"],
        "productionEnabled": azure["productionEnabled"],
        "sourcePackageSha256": package["sha256"],
    }


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--contract",
        type=Path,
        default=Path("config/governance/helios-mainline-authority.v1.json"),
    )
    args = parser.parse_args()

    try:
        result = validate(load_json(args.contract))
    except ContractError as exc:
        print(json.dumps({"status": "failed", "error": str(exc)}, indent=2), file=sys.stderr)
        return 1

    print(json.dumps(result, indent=2, ensure_ascii=False))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
