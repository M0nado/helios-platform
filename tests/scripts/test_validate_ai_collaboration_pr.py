from __future__ import annotations

import importlib.util
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[2]
SPEC = importlib.util.spec_from_file_location(
    "validate_ai_collaboration_pr",
    ROOT / "scripts/control/validate_ai_collaboration_pr.py",
)
validator = importlib.util.module_from_spec(SPEC)
assert SPEC.loader is not None
SPEC.loader.exec_module(validator)


def mark(checked: bool) -> str:
    return "x" if checked else " "


def build_body(
    *,
    issue_number: int = 231,
    standard_branch: bool = True,
    branch_exception_notes: str = "",
    ai_codex: bool = False,
    ai_claude: bool = False,
    ai_copilot: bool = False,
    human_only: bool = True,
    correlation_id: str = "hc-231-governed-ai-collab-contract",
    workflow_link: str = "https://github.com/M0nado/helios-platform/actions/runs/123",
    test_link: str = "https://github.com/M0nado/helios-platform/actions/runs/123#artifacts",
    additional_link: str = "https://example.com/evidence",
    declare_privileged: bool = False,
    safeguard_direct_apply: bool = False,
    safeguard_what_if: bool = False,
    safeguard_approval: bool = False,
    approval_link: str = "N/A",
    rollback_plan: str = "N/A",
    security_checks: bool = True,
    security_heading_level: int = 3,
) -> str:
    security_heading = "#" * security_heading_level
    return f"""## Summary
Contract validation test payload.

## Governance Metadata

### Issue Link
Fixes #{issue_number}

### Branch Naming
- [{mark(standard_branch)}] Branch matches `issue-<issue-number>-<scope>` or `<owner>-issue-<issue-number>-<scope>`
- [{mark(not standard_branch)}] Branch uses an approved exception and the reason is documented below

Branch naming notes: {branch_exception_notes}

### PR Title Conformance
- [x] PR title follows `<type>(<scope>): <summary> | Fixes #<issue-number>`

### AI Participation
- [{mark(ai_codex)}] Codex
- [{mark(ai_claude)}] Claude Code
- [{mark(ai_copilot)}] GitHub Copilot
- [{mark(human_only)}] Human-only (no AI implementation assistance)

### Correlation ID
{correlation_id}

### Evidence Links
- Workflow run: {workflow_link}
- Test evidence: {test_link}
- Additional evidence: {additional_link}

## Proposal-Only and Privileged Operations

### Privileged Change Declaration
- [{mark(not declare_privileged)}] No privileged files or operations are changed in this PR
- [{mark(declare_privileged)}] Privileged files or operations are changed and this PR remains proposal-only

### Proposal-Only Safeguards
- [{mark(safeguard_direct_apply)}] No direct apply path was added or enabled
- [{mark(safeguard_what_if)}] Required what-if or dry-run evidence is linked above
- [{mark(safeguard_approval)}] Required human approval gate is linked below

### Approval Gate Link
{approval_link}

### Rollback Plan
{rollback_plan}

{security_heading} Security and Data Handling
- [{mark(security_checks)}] No credentials, tokens, recovery keys, private endpoint keys, or tenant secrets were committed
- [{mark(security_checks)}] No prohibited data flows were introduced (including secret leakage to docs, PR text, or logs)
"""


def build_event_payload(title: str, body: str, branch: str) -> dict[str, object]:
    return {
        "pull_request": {
            "title": title,
            "body": body,
            "head": {"ref": branch},
        }
    }


class ValidateAiCollaborationPrTests(unittest.TestCase):
    def test_human_only_non_privileged_pr_passes(self):
        body = build_body(
            human_only=True,
            safeguard_direct_apply=False,
            safeguard_what_if=False,
            safeguard_approval=False,
            approval_link="N/A",
            rollback_plan="N/A",
        )
        payload = build_event_payload(
            "docs(governance): define AI collaboration contract | Fixes #231",
            body,
            "issue-231-governed-ai-collab-contract",
        )
        errors, _ = validator.validate_event_payload(payload, changed_files=[])
        self.assertEqual(errors, [])

    def test_ai_assisted_requires_strict_safeguards(self):
        body = build_body(
            ai_codex=True,
            human_only=False,
            safeguard_direct_apply=False,
            safeguard_what_if=False,
            safeguard_approval=False,
            approval_link="N/A",
        )
        payload = build_event_payload(
            "docs(governance): define AI collaboration contract | Fixes #231",
            body,
            "issue-231-governed-ai-collab-contract",
        )
        errors, _ = validator.validate_event_payload(payload, changed_files=[])
        self.assertTrue(
            any('Proposal-Only Safeguards must check "No direct apply path was added or enabled".' in item for item in errors)
        )
        self.assertTrue(
            any("Approval Gate Link is required for AI-assisted or privileged changes." in item for item in errors)
        )

    def test_privileged_changes_require_privileged_declaration(self):
        body = build_body(
            human_only=True,
            declare_privileged=False,
            safeguard_direct_apply=True,
            safeguard_what_if=True,
            safeguard_approval=True,
            approval_link="#231",
            rollback_plan="Revert the privileged workflow changes and restore the previous reviewed manifest.",
        )
        payload = build_event_payload(
            "ci(governance): validate AI collaboration contract | Fixes #231",
            body,
            "issue-231-governed-ai-collab-contract",
        )
        errors, _ = validator.validate_event_payload(
            payload,
            changed_files=["infra/main.bicep"],
        )
        self.assertTrue(
            any("Privileged files were detected in this PR" in item for item in errors)
        )

    def test_branch_exception_requires_notes(self):
        body = build_body(
            standard_branch=False,
            branch_exception_notes="",
        )
        payload = build_event_payload(
            "docs(governance): define AI collaboration contract | Fixes #231",
            body,
            "feature/legacy-branch-name",
        )
        errors, _ = validator.validate_event_payload(payload, changed_files=[])
        self.assertTrue(
            any("Branch Naming exception is checked but no reason was provided." in item for item in errors)
        )

    def test_branch_exception_allows_multiline_reason(self):
        body = build_body(
            standard_branch=False,
            branch_exception_notes="\nThis workspace branch is pre-provisioned by the session system.",
        )
        payload = build_event_payload(
            "docs(governance): define AI collaboration contract | Fixes #231",
            body,
            "feature/preprovisioned-branch-name",
        )
        errors, _ = validator.validate_event_payload(payload, changed_files=[])
        self.assertEqual(errors, [])

    def test_correlation_id_must_match_linked_issue(self):
        body = build_body(
            correlation_id="hc-999-governed-ai-collab-contract",
        )
        payload = build_event_payload(
            "docs(governance): define AI collaboration contract | Fixes #231",
            body,
            "issue-231-governed-ai-collab-contract",
        )
        errors, _ = validator.validate_event_payload(payload, changed_files=[])
        self.assertTrue(
            any("Correlation ID issue number must match the linked issue number." in item for item in errors)
        )

    def test_title_must_follow_governed_pattern(self):
        body = build_body()
        payload = build_event_payload(
            "Define governed collaboration contract",
            body,
            "issue-231-governed-ai-collab-contract",
        )
        errors, _ = validator.validate_event_payload(payload, changed_files=[])
        self.assertTrue(
            any("PR title must follow" in item for item in errors)
        )

    def test_security_section_accepts_h2_heading(self):
        body = build_body(
            security_heading_level=2,
        )
        payload = build_event_payload(
            "docs(governance): define AI collaboration contract | Fixes #231",
            body,
            "issue-231-governed-ai-collab-contract",
        )
        errors, _ = validator.validate_event_payload(payload, changed_files=[])
        self.assertEqual(errors, [])


if __name__ == "__main__":
    unittest.main()
