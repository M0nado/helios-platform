#!/usr/bin/env python3
from __future__ import annotations
import argparse
import http.server
import json
import shutil
import socketserver
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
STATUS = ROOT / "status-site"
REPORTS = ROOT / "reports"


def run(cmd: list[str]) -> tuple[int, str]:
    proc = subprocess.run(cmd, cwd=ROOT, text=True, capture_output=True)
    return proc.returncode, (proc.stdout + proc.stderr).strip()


def rebuild() -> None:
    commands = [
        ["python3", "scripts/integrations/check-connections.py"],
        ["python3", "scripts/integrations/cross_access_profiles.py"],
        ["python3", "scripts/integrations/readiness_score.py"],
        ["python3", "scripts/analysis/branch_intelligence.py"],
        ["python3", "scripts/graphs/generate_graphs.py"],
        ["python3", "scripts/github/update-wiki-from-reports.py"],
        ["python3", "scripts/control/doctor.py"],
        ["python3", "scripts/github/update-pr-from-reports.py", "--dry-run"],
        ["python3", "scripts/dashboard/generate-actions.py"],
        ["python3", "scripts/dashboard/generate-gui.py"],
    ]
    results = []
    for cmd in commands:
        code, output = run(cmd)
        results.append({"command": " ".join(cmd), "exitCode": code, "tail": output.splitlines()[-12:]})
    (REPORTS / "local-setup").mkdir(parents=True, exist_ok=True)
    (REPORTS / "local-setup" / "web-rebuild.json").write_text(json.dumps(results, indent=2) + "\n")
    (STATUS / "reports").mkdir(parents=True, exist_ok=True)
    branch_dir = REPORTS / "branch-intelligence"
    if branch_dir.exists():
        for path in branch_dir.glob("*.md"):
            shutil.copy2(path, STATUS / "reports" / path.name)
    for report_dir_name in ["control-plane", "project-inventory", "build-graph", "codex", "integrations"]:
        report_dir = REPORTS / report_dir_name
        if report_dir.exists():
            for path in report_dir.glob("*.md"):
                shutil.copy2(path, STATUS / "reports" / path.name)


class Handler(http.server.SimpleHTTPRequestHandler):
    def __init__(self, *args, **kwargs):
        super().__init__(*args, directory=str(STATUS), **kwargs)

    def do_POST(self) -> None:  # noqa: N802
        if self.path != "/rebuild":
            self.send_error(404, "Use POST /rebuild")
            return
        try:
            rebuild()
            body = b'{"ok": true}\n'
            self.send_response(200)
        except Exception as exc:  # pragma: no cover - defensive server path
            body = json.dumps({"ok": False, "error": str(exc)}).encode() + b"\n"
            self.send_response(500)
        self.send_header("Content-Type", "application/json")
        self.send_header("Content-Length", str(len(body)))
        self.end_headers()
        self.wfile.write(body)


def main() -> int:
    parser = argparse.ArgumentParser(description="Serve the HELIOS local dashboard.")
    parser.add_argument("--host", default="127.0.0.1")
    parser.add_argument("--port", type=int, default=8787)
    parser.add_argument("--no-rebuild", action="store_true", help="Serve existing files without regenerating reports first.")
    args = parser.parse_args()
    STATUS.mkdir(parents=True, exist_ok=True)
    if not args.no_rebuild:
        rebuild()
    with socketserver.TCPServer((args.host, args.port), Handler) as httpd:
        print(f"HELIOS dashboard: http://{args.host}:{args.port}/")
        print("Refresh generated reports with: curl -X POST http://%s:%s/rebuild" % (args.host, args.port))
        httpd.serve_forever()
    return 0


if __name__ == "__main__":
    sys.exit(main())
