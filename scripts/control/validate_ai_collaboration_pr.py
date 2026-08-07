#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import os
import re
import sys
from pathlib import Path

TITLE_PATTERN = re.compile(
    r"^(feat|fix|docs|refactor|perf|test|ci|chore)\([^)]+\): .+\| (?P<relation>Fixes|Relates to) #(?P<issue>\d+)$"
)
BRANCH_PATTERN = re.compile(
    r"^(?:[a-z0-9]+-)?issue-(?P<issue>\d+)-[a-z0-9]+(?:-[a-z0-9]+)*$"
)
CORRELATION_PATTERN = re.compile(
    r"\bhc-(?P<issue>\d+)-(?P<scope>[a-z0-9]+(?:-[a-z0-9]+)*)\b"
)
ISSUE_DECLARATION_PATTERN = re.compile(
    r"\b(?P<relation>Fixes|Relates to)\s+#(?P<issue>\d+)\b"
)
SECTION_PATTERN = re.compile(r"^#{2,3}\s+(.+?)\s*$")
URL_OR_REF_PATTERN = re.compile(r"https?://|#\d+|/actions/runs/\d+")
HTML_COMMENT_PATTERN = re.compile(r"<!--.*?(?:-->|$)", re.DOTALL)
FENCED_CODE_BLOCK_PATTERN = re.compile(r"(?s)```.*?```|~~~.*?~~~")
APPROVAL_GATE_PATTERN = re.compile(
    r"(?:#\d+|https?://github\.com/\S+/(?:issues|pull)/\d+|https?://dev\.azure\.com/\S+/_workitems/edit/\d+)"
)
PLACEHOLDER_PATTERN = re.compile(
    r"^(?:n/?a|na|none|tbd|pending|<issue-number>|<kebab-scope>)(?:[\s\.:;,_-].*)?$",
    re.IGNORECASE,
)

PLACEHOLDER_VALUES = {
    "",
    "n/a",
    "na",
    "none",
    "tbd",
    "<issue-number>",
    "<kebab-scope>",
    "pending",
}

PRIVILEGED_PATH_PREFIXES = (
    "infra/",
    "scripts/windows/security/",
    "scripts/windows/firewall/",
    "scripts/windows/wdac/",
    "scripts/windows/bitlocker/",
    "scripts/azure/",
    "scripts/security/",
    "scripts/microsoft-enterprise/",
    "microsoft-ecosystem/scripts/",
    "scripts/entra/",
    "scripts/purview/",
    "monado/helios-control/scripts/",
)
PRIVILEGED_WORKFLOW_TOKENS = (
    "deploy",
    "azure",
    "security",
    "enterprise-bicep",
    "rbac",
    "entra",
    "wdac",
    "firewall",
    "bitlocker",
    "purview",
)


def parse_h3_sections(body: str) -> dict[str, str]:
    sections: dict[str, str] = {}
    current: str | None = None
    buffer: list[str] = []

    for line in body.splitlines():
        match = SECTION_PATTERN.match(line.strip())
        if match:
            if current is not None:
                sections[current] = "\n".join(buffer).strip()
            current = match.group(1).strip()
            buffer = []
            continue
        if current is not None:
            buffer.append(line)

    if current is not None:
        sections[current] = "\n".join(buffer).strip()
    return sections


def strip_non_visible_markdown(text: str) -> str:
    without_comments = HTML_COMMENT_PATTERN.sub("", text)
    return FENCED_CODE_BLOCK_PATTERN.sub("", without_comments)


def is_checked(section: str, label: str) -> bool:
    pattern = re.compile(
        rf"^- \[[xX]\]\s+{re.escape(label)}\s*$",
        re.MULTILINE,
    )
    return bool(pattern.search(section))


def strip_checkbox_lines(section: str) -> str:
    lines = []
    for line in section.splitlines():
        if re.match(r"^\s*-\s+\[[ xX]\]\s+", line):
            continue
        lines.append(line)
    return "\n".join(lines).strip()


def extract_branch_exception_reason(section: str) -> str:
    lines = section.splitlines()
    for index, raw in enumerate(lines):
        line = raw.strip()
        if line.lower().startswith("branch naming notes:"):
            inline_reason = line.split(":", 1)[1].strip()
            if inline_reason:
                return inline_reason
            following_reason = "\n".join(
                candidate.strip() for candidate in lines[index + 1 :] if candidate.strip()
            ).strip()
            return following_reason
    return strip_checkbox_lines(section)


def is_meaningful_text(value: str) -> bool:
    normalized = re.sub(r"\s+", " ", value.strip()).lower().strip("`*_")
    if not normalized:
        return False
    if PLACEHOLDER_PATTERN.match(normalized):
        return False
    if normalized in PLACEHOLDER_VALUES:
        return False
    return True


def parse_evidence_links(section: str) -> dict[str, str]:
    parsed: dict[str, str] = {}
    for raw in section.splitlines():
        line = raw.strip()
        if not line.startswith("- "):
            continue
        item = line[2:].strip()
        if ":" not in item:
            continue
        key, value = item.split(":", 1)
        parsed[key.strip().lower()] = value.strip()
    return parsed


def parse_issue_number(text: str) -> int | None:
    match = ISSUE_DECLARATION_PATTERN.search(text)
    if not match:
        return None
    return int(match.group("issue"))


def is_privileged_path(path: str) -> bool:
    lowered = path.strip().lower()
    if any(lowered.startswith(prefix) for prefix in PRIVILEGED_PATH_PREFIXES):
        return True
    if lowered.startswith(".github/workflows/"):
        file_name = lowered.rsplit("/", 1)[-1]
        return any(token in file_name for token in PRIVILEGED_WORKFLOW_TOKENS)
    return False


def load_changed_files(path: Path | None) -> list[str]:
    if path is None or not path.exists():
        return []
    return [line.strip() for line in path.read_text(encoding="utf-8").splitlines() if line.strip()]


def validate_event_payload(payload: dict[str, object], changed_files: list[str]) -> tuple[list[str], list[str]]:
    errors: list[str] = []
    notes: list[str] = []

    pull_request = payload.get("pull_request")
    if not isinstance(pull_request, dict):
        return ["Event payload is missing pull_request data."], notes

    title = str(pull_request.get("title") or "")
    body = str(pull_request.get("body") or "")
    head = pull_request.get("head")
    branch = ""
    if isinstance(head, dict):
        branch = str(head.get("ref") or "")

    if not body.strip():
        return ["Pull request body is empty. Use the governed pull request template."], notes

    sections = parse_h3_sections(strip_non_visible_markdown(body))
    required_sections = (
        "Issue Link",
        "Branch Naming",
        "PR Title Conformance",
        "AI Participation",
        "Correlation ID",
        "Evidence Links",
        "Privileged Change Declaration",
        "Proposal-Only Safeguards",
        "Approval Gate Link",
        "Rollback Plan",
        "Security and Data Handling",
    )
    for section_name in required_sections:
        if section_name not in sections:
            errors.append(f'Missing required PR section: "### {section_name}"')

    if errors:
        return errors, notes

    issue_section = sections["Issue Link"]
    issue_number = parse_issue_number(issue_section)
    if issue_number is None:
        errors.append("Issue Link must include `Fixes #<issue-number>` or `Relates to #<issue-number>`.")

    title_section = sections["PR Title Conformance"]
    title_checkbox_checked = any(
        (
            is_checked(title_section, "PR title follows `<type>(<scope>): <summary> | Fixes #<issue-number>`"),
            is_checked(title_section, "PR title follows `<type>(<scope>): <summary> | Relates to #<issue-number>`"),
            is_checked(
                title_section,
                "PR title follows `<type>(<scope>): <summary> | (Fixes|Relates to) #<issue-number>`",
            ),
        )
    )
    if not title_checkbox_checked:
        errors.append("PR Title Conformance checkbox must be checked.")
    title_match = TITLE_PATTERN.match(title)
    if not title_match:
        errors.append(
            "PR title must follow `<type>(<scope>): <summary> | Fixes #<issue-number>` "
            "or `<type>(<scope>): <summary> | Relates to #<issue-number>`."
        )
    elif issue_number is not None and int(title_match.group("issue")) != issue_number:
        errors.append("PR title issue number must match the linked issue number in Issue Link.")

    branch_section = sections["Branch Naming"]
    standard_branch_checked = is_checked(
        branch_section,
        "Branch matches `issue-<issue-number>-<scope>` or `<owner>-issue-<issue-number>-<scope>`",
    )
    exception_branch_checked = is_checked(
        branch_section,
        "Branch uses an approved exception and the reason is documented below",
    )
    if standard_branch_checked == exception_branch_checked:
        errors.append("Branch Naming must check exactly one branch declaration option.")
    branch_match = BRANCH_PATTERN.match(branch)
    if standard_branch_checked and not branch_match:
        errors.append(f'Branch "{branch}" does not match the governed branch naming pattern.')
    elif standard_branch_checked and issue_number is not None and int(branch_match.group("issue")) != issue_number:
        errors.append("Branch issue number must match the linked issue number in Issue Link.")
    if exception_branch_checked and not is_meaningful_text(extract_branch_exception_reason(branch_section)):
        errors.append("Branch Naming exception is checked but no reason was provided.")

    ai_section = sections["AI Participation"]
    codex_checked = is_checked(ai_section, "Codex")
    claude_checked = is_checked(ai_section, "Claude Code")
    copilot_checked = is_checked(ai_section, "GitHub Copilot")
    human_only_checked = is_checked(ai_section, "Human-only (no AI implementation assistance)")
    if not any((codex_checked, claude_checked, copilot_checked, human_only_checked)):
        errors.append("AI Participation must select at least one option.")
    if human_only_checked and any((codex_checked, claude_checked, copilot_checked)):
        errors.append("AI Participation cannot select Human-only together with AI-assisted options.")

    correlation_section = sections["Correlation ID"]
    correlation_match = CORRELATION_PATTERN.search(correlation_section)
    if not correlation_match:
        errors.append("Correlation ID must match `hc-<issue-number>-<kebab-scope>`.")
    elif issue_number is not None and int(correlation_match.group("issue")) != issue_number:
        errors.append("Correlation ID issue number must match the linked issue number.")

    evidence_section = sections["Evidence Links"]
    evidence = parse_evidence_links(evidence_section)
    for required_key in ("workflow run", "test evidence"):
        value = evidence.get(required_key, "")
        if not is_meaningful_text(value):
            errors.append(f"Evidence Links must provide a value for `{required_key}`.")
        elif not URL_OR_REF_PATTERN.search(value):
            errors.append(
                f"Evidence Links `{required_key}` must include a concrete URL or run/issue reference."
            )

    privileged_paths = [path for path in changed_files if is_privileged_path(path)]
    privileged_changed = bool(privileged_paths)
    if privileged_changed:
        notes.append("Detected privileged paths:")
        notes.extend([f"- {path}" for path in privileged_paths])

    privileged_decl_section = sections["Privileged Change Declaration"]
    declared_no_privileged = is_checked(
        privileged_decl_section,
        "No privileged files or operations are changed in this PR",
    )
    declared_privileged = is_checked(
        privileged_decl_section,
        "Privileged files or operations are changed and this PR remains proposal-only",
    )
    if declared_no_privileged == declared_privileged:
        errors.append("Privileged Change Declaration must check exactly one declaration.")
    if privileged_changed and not declared_privileged:
        errors.append(
            "Privileged files were detected in this PR, but the privileged proposal-only declaration was not selected."
        )

    enforce_strict_proposal_checks = privileged_changed or declared_privileged
    if enforce_strict_proposal_checks:
        safeguards_section = sections["Proposal-Only Safeguards"]
        safeguard_labels = (
            "No direct apply path was added or enabled",
            "Required what-if or dry-run evidence is linked above",
            "Required human approval gate is linked below",
        )
        for label in safeguard_labels:
            if not is_checked(safeguards_section, label):
                errors.append(f'Proposal-Only Safeguards must check "{label}".')

        approval_section = sections["Approval Gate Link"]
        if not is_meaningful_text(approval_section):
            errors.append("Approval Gate Link is required when privileged changes are declared or detected.")
        elif not APPROVAL_GATE_PATTERN.search(approval_section):
            errors.append(
                "Approval Gate Link must reference a GitHub issue/pull, Azure DevOps work item, or `#<issue-number>`."
            )

        rollback_section = sections["Rollback Plan"]
        if not is_meaningful_text(rollback_section):
            errors.append("Rollback Plan is required when privileged changes are declared or detected.")

        evidence_values = [value for value in evidence.values() if is_meaningful_text(value)]
        if not any(URL_OR_REF_PATTERN.search(value) for value in evidence_values):
            errors.append("Evidence Links must include at least one concrete URL or issue/run reference.")

    security_section = sections["Security and Data Handling"]
    security_required_checks = (
        "No credentials, tokens, recovery keys, private endpoint keys, or tenant secrets were committed",
        "No prohibited data flows were introduced (including secret leakage to docs, PR text, or logs)",
    )
    for label in security_required_checks:
        if not is_checked(security_section, label):
            errors.append(f'Security and Data Handling must check "{label}".')

    return errors, notes


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Validate governed Codex/Claude pull request metadata and proposal-only boundaries."
    )
    parser.add_argument(
        "--event-path",
        type=Path,
        default=None,
        help="Path to a GitHub pull_request event payload JSON. Defaults to GITHUB_EVENT_PATH.",
    )
    parser.add_argument(
        "--changed-files",
        type=Path,
        default=None,
        help="Path to newline-delimited changed files for privileged scope detection.",
    )
    args = parser.parse_args()

    event_path = args.event_path
    if event_path is None:
        environment_value = os.environ.get("GITHUB_EVENT_PATH")
        if environment_value:
            event_path = Path(environment_value)
        else:
            event_path = Path.cwd() / ".github" / "events" / "pull_request.json"

    if not event_path.exists():
        print(
            f"Event payload not found at {event_path}. Provide --event-path or run in a GitHub pull_request context.",
            file=sys.stderr,
        )
        return 1

    payload = json.loads(event_path.read_text(encoding="utf-8"))
    changed_files = load_changed_files(args.changed_files)
    errors, notes = validate_event_payload(payload, changed_files)

    if errors:
        print("AI collaboration governance check failed:", file=sys.stderr)
        for error in errors:
            print(f"- {error}", file=sys.stderr)
        if notes:
            print("", file=sys.stderr)
            for note in notes:
                print(note, file=sys.stderr)
        return 1

    print("AI collaboration governance check passed.")
    if notes:
        for note in notes:
            print(note)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
