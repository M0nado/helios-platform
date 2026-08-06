#!/usr/bin/env python3
"""Validate the non-secret Slack/ZNC integration policy."""

import json
import sys
from pathlib import Path


def validate(document: dict) -> list[str]:
    errors: list[str] = []
    slack = document.get("slack", {})
    znc = document.get("znc", {})
    delivery = document.get("delivery", {})

    if slack.get("inboundExecutionAllowed") is not False:
        errors.append("Slack must never be an execution source")
    if znc.get("inboundExecutionAllowed") is not False:
        errors.append("ZNC must never be an execution source")
    if znc.get("notificationOnly") is not True:
        errors.append("ZNC must be notification-only")
    if znc.get("tlsRequired") is not True:
        errors.append("ZNC must require TLS")
    if not 1 <= int(slack.get("maximumMessageLength", 0)) <= 3000:
        errors.append("Slack maximumMessageLength must be between 1 and 3000")
    if delivery.get("maximumNotificationsPerCorrelationPerHour") != 3:
        errors.append("notification rate must match the HELIOS route guard")
    for name in ("tokenSecretName", "signingSecretName"):
        value = slack.get(name, "")
        if not value or value.startswith(("xox", "http")):
            errors.append(f"slack.{name} must be a Key Vault secret name, not a credential")
    return errors


def main() -> int:
    path = Path(sys.argv[1] if len(sys.argv) > 1 else "config/integrations/slack-znc.example.json")
    errors = validate(json.loads(path.read_text(encoding="utf-8")))
    for error in errors:
        print(f"ERROR: {error}", file=sys.stderr)
    return 1 if errors else 0


if __name__ == "__main__":
    raise SystemExit(main())
