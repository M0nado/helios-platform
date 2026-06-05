from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "tools"))

from helios_orchestrator import load_manifest, summarize, validate_manifest  # noqa: E402


def test_manifest_is_valid() -> None:
    manifest = load_manifest()
    result = validate_manifest(manifest)
    assert result.ok, result.messages


def test_summary_includes_priority_components() -> None:
    manifest = load_manifest()
    summary = summarize(manifest)
    assert "helios-control" in summary
    assert "hermes-fleet-production" in summary
    assert "hermes-xcore" in summary
    assert "aihub-integration" in summary
