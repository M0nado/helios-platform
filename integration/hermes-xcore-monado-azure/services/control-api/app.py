from __future__ import annotations

import json
import os
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
from typing import Protocol
from urllib.parse import unquote, urlsplit

ROOT = Path(__file__).resolve().parent
WWWROOT = (ROOT / "wwwroot").resolve()


class HermesAdapter(Protocol):
    def status(self) -> dict[str, str | bool]: ...


class UnboundHermesAdapter:
    def status(self) -> dict[str, str | bool]:
        return {"mode": "adapter", "status": "unbound", "healthy": False}


HERMES_ADAPTER: HermesAdapter = UnboundHermesAdapter()


class XCoreAdapter(Protocol):
    def status(self) -> dict[str, str | bool]: ...


class StandbyXCoreAdapter:
    def status(self) -> dict[str, str | bool]:
        return {
            "mode": "adapter",
            "status": "safe-standby",
            "trainingEnabled": False,
            "healthy": False,
        }


XCORE_ADAPTER: XCoreAdapter = StandbyXCoreAdapter()


class Handler(BaseHTTPRequestHandler):
    server_version = "HELIOS"
    sys_version = ""

    def version_string(self) -> str:
        return self.server_version

    def log_message(self, format: str, *args: object) -> None:
        return

    def security_headers(self) -> None:
        self.send_header("Content-Security-Policy", "default-src 'self'; style-src 'self' 'unsafe-inline'; script-src 'self' 'unsafe-inline'; connect-src 'self'; frame-ancestors 'none'; base-uri 'none'; form-action 'none'")
        self.send_header("Cross-Origin-Resource-Policy", "same-origin")
        self.send_header("Referrer-Policy", "no-referrer")
        self.send_header("X-Content-Type-Options", "nosniff")
        self.send_header("X-Frame-Options", "DENY")

    def send_json(self, payload: dict, status: int = 200) -> None:
        body = json.dumps(payload, separators=(",", ":")).encode()
        self.send_response(status)
        self.send_header("Content-Type", "application/json")
        self.send_header("Cache-Control", "no-store")
        self.security_headers()
        self.send_header("Content-Length", str(len(body)))
        self.end_headers()
        self.wfile.write(body)

    def send_static(self, path: Path) -> None:
        body = path.read_bytes()
        content_type = "text/html; charset=utf-8" if path.suffix == ".html" else "application/octet-stream"
        self.send_response(200)
        self.send_header("Content-Type", content_type)
        self.send_header("Cache-Control", "no-store")
        self.security_headers()
        self.send_header("Content-Length", str(len(body)))
        self.end_headers()
        self.wfile.write(body)

    def do_GET(self) -> None:
        request_path = unquote(urlsplit(self.path).path)
        if request_path in ("/health", "/health/live"):
            self.send_json({"status": "alive"})
            return
        if request_path == "/health/ready":
            self.send_json({"status": "ready", "scope": "read-only-control-center"})
            return
        if request_path == "/api/status":
            hermes = HERMES_ADAPTER.status()
            hermes_bound = hermes["status"] == "bound" and hermes["healthy"] is True
            xcore = XCORE_ADAPTER.status()
            self.send_json({
                "status": "ready" if hermes_bound else "standby",
                "environment": os.getenv("HELIOS_ENVIRONMENT", "local"),
                "hermes": hermes,
                "xcore": xcore,
                "controlCenter": "monado",
                "capabilities": ["status-read"],
            })
            return

        relative_path = "index.html" if request_path == "/" else request_path.lstrip("/")
        static_path = (WWWROOT / relative_path).resolve()
        if static_path.is_relative_to(WWWROOT) and static_path.is_file():
            self.send_static(static_path)
            return
        self.send_json({"error": "not found"}, 404)


if __name__ == "__main__":
    ThreadingHTTPServer(("0.0.0.0", 8080), Handler).serve_forever()
