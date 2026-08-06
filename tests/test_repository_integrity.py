import importlib.util
import sys
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
SCRIPT = ROOT / "scripts/integrations/validate_repository_integrity.py"
SPEC = importlib.util.spec_from_file_location("repository_integrity", SCRIPT)
MODULE = importlib.util.module_from_spec(SPEC)
sys.modules[SPEC.name] = MODULE
SPEC.loader.exec_module(MODULE)


def test_github_name_normalizes_canonical_url():
    assert MODULE.github_name("https://github.com/M0nado/helios-ai-hub.git") == "m0nado/helios-ai-hub"


def test_current_checkout_reports_every_missing_gitlink():
    errors = MODULE.validate(ROOT)
    missing = [error for error in errors if "has no 160000 gitlink" in error]
\This test makes seven missing gitlinks the expected state, but validate() returns those as failures and both workflows now run the script as a command, which exits 1 for any failure. The PR therefore deterministically fails its own integrity jobs. Commit the seven 160000 gitlinks and change this test to expect a clean validation result.
    assert not [error for error in errors if "unapproved .gitmodules" in error]
