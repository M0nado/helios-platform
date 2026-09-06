#!/usr/bin/env python3
from __future__ import annotations

import argparse
import hashlib
import json
import os
import random
import time
import urllib.error
import urllib.parse
import urllib.request
from dataclasses import asdict, dataclass
from pathlib import Path, PurePosixPath
from typing import Any

GRAPH_ROOT = "https://graph.microsoft.com/v1.0"
SMALL_UPLOAD_LIMIT = 4 * 1024 * 1024
CHUNK_SIZE = 10 * 1024 * 1024  # 32 x 320 KiB; accepted by Graph upload sessions.


@dataclass(frozen=True)
class ManifestEntry:
    source: str
    destination: str
    bytes: int
    sha256: str


def sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest()


def quote_path(value: str) -> str:
    return "/".join(urllib.parse.quote(part, safe="") for part in PurePosixPath(value).parts if part not in {"/", ""})


def collect(root: Path, destination_root: str) -> list[ManifestEntry]:
    candidates: list[Path] = []
    for relative in ("README.md", "RUN_THIS_FIRST.md", "CHANGELOG.md", "HELIOS_ENTERPRISE_AUTOMATION_FABRIC_REPORT.md"):
        path = root / relative
        if path.is_file():
            candidates.append(path)
    for directory, patterns in (
        ("docs", ("*.md", "*.json")),
        ("config", ("*.json", "*.yaml", "*.yml")),
        ("infra/bicep", ("*.bicep", "*.bicepparam", "*.json", "*.md")),
        (".github/workflows", ("*.yml", "*.yaml")),
        ("power-platform", ("*.json", "*.md")),
    ):
        base = root / directory
        if not base.exists():
            continue
        for pattern in patterns:
            candidates.extend(base.rglob(pattern))
    unique = sorted({path.resolve() for path in candidates})
    entries: list[ManifestEntry] = []
    for path in unique:
        relative = path.relative_to(root.resolve()).as_posix()
        if any(part.startswith("runtime-") or part.endswith(".local.json") for part in path.parts):
            continue
        destination = str(PurePosixPath(destination_root) / relative)
        entries.append(ManifestEntry(relative, destination, path.stat().st_size, sha256_file(path)))
    return entries


def request_json(
    method: str,
    url: str,
    *,
    token: str | None,
    payload: dict[str, Any] | None = None,
    body: bytes | None = None,
    headers: dict[str, str] | None = None,
    attempts: int = 6,
) -> dict[str, Any]:
    encoded = body
    request_headers = {"Accept": "application/json", **(headers or {})}
    if payload is not None:
        encoded = json.dumps(payload, separators=(",", ":")).encode("utf-8")
        request_headers["Content-Type"] = "application/json"
    if token:
        request_headers["Authorization"] = f"Bearer {token}"
    for attempt in range(1, attempts + 1):
        request = urllib.request.Request(url, data=encoded, method=method, headers=request_headers)
        try:
            with urllib.request.urlopen(request, timeout=90) as response:
                raw = response.read()
                return json.loads(raw) if raw else {}
        except urllib.error.HTTPError as exc:
            retryable = exc.code in {408, 429, 500, 502, 503, 504}
            if not retryable or attempt == attempts:
                details = exc.read().decode("utf-8", errors="replace")[:2000]
                raise RuntimeError(f"Graph request failed ({exc.code}) for {method} {url}: {details}") from exc
            retry_after = exc.headers.get("Retry-After")
            delay = float(retry_after) if retry_after and retry_after.isdigit() else min(60.0, (2 ** attempt) + random.random())
            time.sleep(delay)
    raise AssertionError("unreachable")


def upload_small(token: str, site_id: str, drive_id: str, destination: str, source: Path) -> dict[str, Any]:
    url = f"{GRAPH_ROOT}/sites/{urllib.parse.quote(site_id, safe='')}/drives/{urllib.parse.quote(drive_id, safe='')}/root:/{quote_path(destination)}:/content"
    return request_json(
        "PUT",
        url,
        token=token,
        body=source.read_bytes(),
        headers={"Content-Type": "application/octet-stream"},
    )


def upload_large(token: str, site_id: str, drive_id: str, destination: str, source: Path) -> dict[str, Any]:
    session_url = f"{GRAPH_ROOT}/sites/{urllib.parse.quote(site_id, safe='')}/drives/{urllib.parse.quote(drive_id, safe='')}/root:/{quote_path(destination)}:/createUploadSession"
    session = request_json(
        "POST",
        session_url,
        token=token,
        payload={"item": {"@microsoft.graph.conflictBehavior": "replace", "name": source.name}},
    )
    upload_url = session.get("uploadUrl")
    if not upload_url:
        raise RuntimeError("Microsoft Graph did not return an uploadUrl.")
    total = source.stat().st_size
    result: dict[str, Any] = {}
    with source.open("rb") as stream:
        start = 0
        while start < total:
            chunk = stream.read(CHUNK_SIZE)
            end = start + len(chunk) - 1
            result = request_json(
                "PUT",
                upload_url,
                token=None,  # Upload-session URLs are pre-authorized; do not attach the bearer token.
                body=chunk,
                headers={
                    "Content-Length": str(len(chunk)),
                    "Content-Range": f"bytes {start}-{end}/{total}",
                    "Content-Type": "application/octet-stream",
                },
            )
            start = end + 1
    return result


def main() -> int:
    parser = argparse.ArgumentParser(description="Plan or apply a checksum-locked HELIOS governance sync to SharePoint.")
    parser.add_argument("--root", default=".")
    parser.add_argument("--site-id", default=os.getenv("HELIOS_SHAREPOINT_SITE_ID", ""))
    parser.add_argument("--drive-id", default=os.getenv("HELIOS_SHAREPOINT_DRIVE_ID", ""))
    parser.add_argument("--destination", default="HELIOS/Governance/AutomationFabric")
    parser.add_argument("--manifest", default="artifacts/sharepoint-sync-manifest.json")
    parser.add_argument("--apply", action="store_true")
    parser.add_argument("--confirmation", default="")
    args = parser.parse_args()

    root = Path(args.root).resolve()
    entries = collect(root, args.destination)
    manifest_path = root / args.manifest
    manifest_path.parent.mkdir(parents=True, exist_ok=True)
    manifest = {
        "specVersion": "1.0",
        "mode": "apply" if args.apply else "plan",
        "destination": args.destination,
        "fileCount": len(entries),
        "entries": [asdict(item) for item in entries],
    }
    manifest_bytes = (json.dumps(manifest, indent=2, sort_keys=True) + "\n").encode("utf-8")
    manifest["manifestSha256"] = hashlib.sha256(manifest_bytes).hexdigest()
    manifest_path.write_text(json.dumps(manifest, indent=2, sort_keys=True) + "\n", encoding="utf-8")

    if not args.apply:
        print(json.dumps({"mode": "plan", "fileCount": len(entries), "manifest": str(manifest_path), "manifestSha256": manifest["manifestSha256"]}, indent=2))
        return 0
    if args.confirmation != "PUBLISH HELIOS SHAREPOINT GOVERNANCE":
        raise SystemExit("--confirmation 'PUBLISH HELIOS SHAREPOINT GOVERNANCE' is required")
    if not args.site_id or not args.drive_id:
        raise SystemExit("SharePoint site ID and drive ID are required")
    token = os.getenv("GRAPH_ACCESS_TOKEN")
    if not token:
        raise SystemExit("GRAPH_ACCESS_TOKEN is required for --apply")

    results = []
    for entry in entries:
        source = root / entry.source
        result = upload_small(token, args.site_id, args.drive_id, entry.destination, source) if entry.bytes <= SMALL_UPLOAD_LIMIT else upload_large(token, args.site_id, args.drive_id, entry.destination, source)
        results.append({"source": entry.source, "destination": entry.destination, "webUrl": result.get("webUrl"), "sha256": entry.sha256})
    result_path = manifest_path.with_name("sharepoint-sync-result.json")
    result_path.write_text(json.dumps({"uploaded": len(results), "items": results}, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    print(json.dumps({"uploaded": len(results), "manifest": str(manifest_path), "result": str(result_path)}, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
