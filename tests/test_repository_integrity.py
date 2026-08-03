import importlib.util
import json
import subprocess
import sys
from types import SimpleNamespace
from pathlib import Path

import pytest


ROOT = Path(__file__).resolve().parents[1]
SCRIPT = ROOT / "scripts/integrations/validate_repository_integrity.py"
SPEC = importlib.util.spec_from_file_location("repository_integrity", SCRIPT)
MODULE = importlib.util.module_from_spec(SPEC)
sys.modules[SPEC.name] = MODULE
SPEC.loader.exec_module(MODULE)


def run_git(root: Path, *args: str) -> None:
    subprocess.run(["git", *args], cwd=root, check=True, capture_output=True)


@pytest.fixture
def repository(tmp_path: Path) -> Path:
    (tmp_path / "config/integrations").mkdir(parents=True)
    registry = {
        "repositories": [
            {
                "name": "M0nado/example",
                "role": "example",
                "integrationMode": "pinned-submodule",
                "authority": ["tests"],
            }
        ]
    }
    (tmp_path / "config/integrations/repositories.json").write_text(json.dumps(registry))
    (tmp_path / ".gitmodules").write_text(
        '[submodule "modules/example"]\n'
        "\tpath = modules/example\n"
        "\turl = https://github.com/M0nado/example.git\n"
    )
    run_git(tmp_path, "init")
    run_git(tmp_path, "update-index", "--add", "--cacheinfo", "160000," + "1" * 40 + ",modules/example")
    return tmp_path


def test_github_name_normalizes_canonical_url():
    assert MODULE.github_name("https://github.com/M0nado/helios-ai-hub.git") == "m0nado/helios-ai-hub"


def test_valid_repository_has_no_errors(repository: Path):
    assert MODULE.validate(repository) == []


def test_missing_gitlink_is_reported(repository: Path):
    run_git(repository, "update-index", "--force-remove", "modules/example")
    assert MODULE.validate(repository) == [
        "declared submodule has no 160000 gitlink: modules/example"
    ]


def test_declarations_only_supports_access_bootstrap(repository: Path):
    run_git(repository, "update-index", "--force-remove", "modules/example")
    assert MODULE.validate(repository, require_gitlinks=False) == []


def test_bootstrap_switches_to_full_validation_when_any_gitlink_exists(repository: Path):
    run_git(repository, "update-index", "--add", "--cacheinfo", "160000," + "2" * 40 + ",modules/orphan")
    assert "orphan 160000 gitlink: modules/orphan" in MODULE.validate_bootstrap(repository)


def test_malformed_gitmodules_is_reported(repository: Path):
    (repository / ".gitmodules").write_text("[broken\n")
    errors = MODULE.validate(repository, require_gitlinks=False)
    assert len(errors) == 1
    assert errors[0].startswith("cannot read .gitmodules:")


def test_malformed_registry_is_reported(repository: Path):
    (repository / "config/integrations/repositories.json").write_text("{")
    errors = MODULE.validate(repository, require_gitlinks=False)
    assert len(errors) == 1
    assert errors[0].startswith("cannot read repository registry:")


@pytest.mark.parametrize(
    ("registry", "message"),
    [
        ([], "$: expected object"),
        ({}, "$.repositories: expected array"),
        ({"repositories": [None]}, "$.repositories[0]: expected object"),
        ({"repositories": [{}]}, "$.repositories[0].name: expected nonempty string"),
        (
            {"repositories": [{"name": "x", "role": "x", "integrationMode": "bad", "authority": []}]},
            "$.repositories[0].integrationMode: unsupported value",
        ),
    ],
)
def test_registry_structure_is_validated(repository: Path, registry: object, message: str):
    (repository / "config/integrations/repositories.json").write_text(json.dumps(registry))
    assert message in MODULE.validate(repository, require_gitlinks=False)


def test_unmerged_index_entry_is_rejected(repository: Path, monkeypatch):
    output = "160000 " + "1" * 40 + " 2\tmodules/example\n"
    monkeypatch.setattr(MODULE.subprocess, "run", lambda *args, **kwargs: SimpleNamespace(stdout=output))
    with pytest.raises(ValueError, match="unmerged index entry at stage 2"):
        MODULE.gitlinks(repository)


@pytest.mark.parametrize("path", ["../example", "/modules/example", "example"])
def test_unsafe_path_is_reported(repository: Path, path: str):
    modules = repository / ".gitmodules"
    modules.write_text(modules.read_text().replace("modules/example", path))
    assert f"unsafe submodule path: {path!r}" in MODULE.validate(
        repository, require_gitlinks=False
    )
