#!/usr/bin/env python3
from __future__ import annotations

import argparse
import hashlib
import hmac
import json
import os
import time
import urllib.error
import urllib.request
from pathlib import Path
from urllib.parse import urlparse


def main() -> int:
    parser = argparse.ArgumentParser(description="Publish one replay-bounded signed event to the HELIOS broker.")
    parser.add_argument("--event", required=True, type=Path)
    parser.add_argument("--apply", action="store_true")
    args = parser.parse_args()
    body = args.event.read_bytes()
    event = json.loads(body)
    preview = {
        "mode": "apply" if args.apply else "preview",
        "eventId": event.get("eventId"),
        "eventType": event.get("eventType"),
        "sha256": hashlib.sha256(body).hexdigest(),
    }
    if not args.apply:
        print(json.dumps(preview, indent=2))
        return 0
    url = os.getenv("HELIOS_FABRIC_INGRESS_URL", "")
    secret = os.getenv("HELIOS_FABRIC_INGRESS_HMAC", "")
    parsed = urlparse(url)
    if parsed.scheme != "https" or not parsed.netloc:
        raise SystemExit("HELIOS_FABRIC_INGRESS_URL must be an absolute HTTPS URL")
    if not secret:
        raise SystemExit("HELIOS_FABRIC_INGRESS_HMAC is required")
    timestamp = str(int(time.time()))
    signature = hmac.new(secret.encode(), timestamp.encode() + b"." + body, hashlib.sha256).hexdigest()
    request = urllib.request.Request(
        url,
        data=body,
        method="POST",
        headers={
            "Content-Type": "application/json",
            "X-HELIOS-Timestamp": timestamp,
            "X-HELIOS-Signature": f"sha256={signature}",
            "User-Agent": "helios-enterprise-fabric/2.0",
        },
    )
    try:
        with urllib.request.urlopen(request, timeout=30) as response:
            result = response.read().decode("utf-8", errors="replace")
            if response.status not in {200, 202}:
                raise RuntimeError(f"unexpected broker status {response.status}: {result[:500]}")
    except urllib.error.HTTPError as exc:
        details = exc.read().decode("utf-8", errors="replace")[:1000]
        raise SystemExit(f"broker rejected event ({exc.code}): {details}") from exc
    preview["status"] = "accepted"
    print(json.dumps(preview, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

