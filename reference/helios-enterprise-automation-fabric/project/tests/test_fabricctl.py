from __future__ import annotations

import json
from pathlib import Path

import pytest

from helios_fabric_cli.cli import ValidationError, evidence, simulate, validate

ROOT = Path(__file__).resolve().parents[1]
FIXTURE = ROOT / "tests/fixtures/deployment-plan.json"


def test_recovered_project_validates() -> None:
    result = validate(ROOT)
    assert result["status"] == "ok"
    assert result["externalConnectorsEnabled"] == 0


def test_simulation_is_offline_and_deterministic() -> None:
    result = simulate(ROOT, FIXTURE, include_disabled=True)
    assert result["writesPerformed"] == []
    assert result["routes"][0]["routeId"] == "production-plan-approval"
    assert "sharepoint-evidence" in result["selectedConnectors"]


def test_disabled_connectors_are_not_selected_by_default() -> None:
    result = simulate(ROOT, FIXTURE, include_disabled=False)
    assert result["selectedConnectors"] == []


def test_evidence_is_redacted_and_checksum_locked(tmp_path: Path) -> None:
    fixture = json.loads(FIXTURE.read_text(encoding="utf-8"))
    fixture["payload"]["accessToken"] = "must-not-survive"
    custom = tmp_path / "fixture.json"
    custom.write_text(json.dumps(fixture), encoding="utf-8")
    result = evidence(ROOT, custom, tmp_path / "out", include_disabled=True)
    rendered = (tmp_path / "out/evidence.json").read_text(encoding="utf-8")
    assert "must-not-survive" not in rendered
    assert "[REDACTED]" in rendered
    assert len(result["sha256"]) == 64


def test_external_connector_activation_fails_closed(tmp_path: Path) -> None:
    project = tmp_path / "project"
    project.mkdir()
    for path in ROOT.rglob("*"):
        if path.is_file() and not any(part in {".venv", "__pycache__", "artifacts", "bin", "obj"} for part in path.parts):
            target = project / path.relative_to(ROOT)
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_bytes(path.read_bytes())
    registry_path = project / "config/fabric/connector-registry.json"
    registry = json.loads(registry_path.read_text(encoding="utf-8"))
    next(item for item in registry["connectors"] if item["id"] == "slack-ops")["enabled"] = True
    registry_path.write_text(json.dumps(registry), encoding="utf-8")
    with pytest.raises(ValidationError, match="external connectors must default disabled"):
        validate(project)
