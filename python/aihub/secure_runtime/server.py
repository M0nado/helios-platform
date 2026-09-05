from __future__ import annotations

import argparse
from collections import defaultdict, deque
from dataclasses import asdict
from datetime import datetime, timezone
import hmac
from http import HTTPStatus
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
import json
import logging
from pathlib import Path
import threading
import time
from typing import Any
from urllib.parse import parse_qs, urlparse
import uuid

from .config import ConfigurationError, SecureRuntimeConfig
from .state import AtomicJsonStore, StateError


LOGGER = logging.getLogger("helios.aihub.secure_runtime")
RUNTIME_VERSION = "1.0.0"
MAX_JSON_DEPTH = 20
MAX_PROMPT_CHARACTERS = 16_384
ALLOWED_PRIORITIES = {"low", "normal", "high"}
ALLOWED_TASK_TYPES = {
    "general",
    "inference",
    "analysis",
    "training-proposal",
    "documentation",
    "validation",
}


def utc_now() -> str:
    return datetime.now(timezone.utc).isoformat()


def _json_depth(value: Any, current: int = 0) -> int:
    if current > MAX_JSON_DEPTH:
        return current
    if isinstance(value, dict):
        if not value:
            return current + 1
        return max(_json_depth(item, current + 1) for item in value.values())
    if isinstance(value, list):
        if not value:
            return current + 1
        return max(_json_depth(item, current + 1) for item in value)
    return current


def _safe_client(client_address: tuple[str, int] | Any) -> str:
    if isinstance(client_address, tuple) and client_address:
        return str(client_address[0])
    return "unknown"


class SlidingWindowRateLimiter:
    """Small process-local rate limiter for the loopback compatibility API."""

    def __init__(self, requests_per_minute: int) -> None:
        self.limit = requests_per_minute
        self.window_seconds = 60.0
        self._events: dict[str, deque[float]] = defaultdict(deque)
        self._lock = threading.Lock()

    def allow(self, key: str) -> tuple[bool, int]:
        now = time.monotonic()
        cutoff = now - self.window_seconds
        with self._lock:
            events = self._events[key]
            while events and events[0] <= cutoff:
                events.popleft()
            if len(events) >= self.limit:
                retry_after = max(1, int(self.window_seconds - (now - events[0])))
                return False, retry_after
            events.append(now)
            return True, 0


class SecureAIHubServer(ThreadingHTTPServer):
    """Loopback-only, authenticated, queue/proposal-only AIHub HTTP server."""

    daemon_threads = True
    allow_reuse_address = True

    def __init__(
        self,
        config: SecureRuntimeConfig,
        token: str,
        handler_class: type[BaseHTTPRequestHandler] | None = None,
    ) -> None:
        if len(token) < 32:
            raise ConfigurationError(
                "AIHub API token must contain at least 32 characters."
            )
        self.config = config
        self.token = token
        self.started_at = utc_now()
        self.rate_limiter = SlidingWindowRateLimiter(config.requests_per_minute)
        self.tasks = AtomicJsonStore(
            config.state_directory / "tasks.json",
            default_factory=lambda: {"schemaVersion": 1, "tasks": []},
        )
        self.admin_requests = AtomicJsonStore(
            config.state_directory / "admin-requests.json",
            default_factory=lambda: {"schemaVersion": 1, "requests": []},
        )
        super().__init__(
            (config.host, config.port),
            handler_class or SecureAIHubRequestHandler,
        )


class SecureAIHubRequestHandler(BaseHTTPRequestHandler):
    server: SecureAIHubServer
    protocol_version = "HTTP/1.1"
    server_version = "HELIOS-Secure-AIHub"
    sys_version = ""

    def log_message(self, format_string: str, *args: object) -> None:
        # BaseHTTPRequestHandler never logs headers, but keep the format bounded
        # and omit query strings so caller-provided values do not enter logs.
        path = urlparse(self.path).path[:256]
        LOGGER.info(
            "%s %s %s",
            _safe_client(self.client_address),
            self.command,
            path,
        )

    def _security_headers(self) -> None:
        self.send_header("Cache-Control", "no-store")
        self.send_header("Pragma", "no-cache")
        self.send_header("X-Content-Type-Options", "nosniff")
        self.send_header("X-Frame-Options", "DENY")
        self.send_header("Referrer-Policy", "no-referrer")
        self.send_header("Content-Security-Policy", "default-src 'none'; frame-ancestors 'none'")
        self.send_header("Permissions-Policy", "camera=(), microphone=(), geolocation=()")
        self.send_header("Cross-Origin-Resource-Policy", "same-origin")
        self.send_header("Connection", "close")

    def _json_response(
        self,
        payload: dict[str, Any],
        status: int | HTTPStatus = HTTPStatus.OK,
        *,
        extra_headers: dict[str, str] | None = None,
    ) -> None:
        encoded = json.dumps(
            payload,
            ensure_ascii=False,
            separators=(",", ":"),
        ).encode("utf-8")
        self.send_response(int(status))
        self.send_header("Content-Type", "application/json; charset=utf-8")
        self.send_header("Content-Length", str(len(encoded)))
        self._security_headers()
        for name, value in (extra_headers or {}).items():
            self.send_header(name, value)
        self.end_headers()
        self.wfile.write(encoded)
        self.close_connection = True

    def _error(
        self,
        status: int | HTTPStatus,
        code: str,
        message: str,
        *,
        correlation_id: str | None = None,
        extra_headers: dict[str, str] | None = None,
    ) -> None:
        self._json_response(
            {
                "status": "error",
                "error": {
                    "code": code,
                    "message": message,
                    "correlationId": correlation_id or str(uuid.uuid4()),
                },
            },
            status,
            extra_headers=extra_headers,
        )

    def _rate_limit(self) -> bool:
        client = _safe_client(self.client_address)
        allowed, retry_after = self.server.rate_limiter.allow(client)
        if allowed:
            return True
        self._error(
            HTTPStatus.TOO_MANY_REQUESTS,
            "rate_limit_exceeded",
            "Request rate exceeded the local AIHub policy.",
            extra_headers={"Retry-After": str(retry_after)},
        )
        return False

    def _authorized(self) -> bool:
        header = self.headers.get("Authorization", "")
        prefix = "Bearer "
        if not header.startswith(prefix):
            self._error(
                HTTPStatus.UNAUTHORIZED,
                "authentication_required",
                "A bearer token is required.",
                extra_headers={"WWW-Authenticate": 'Bearer realm="helios-aihub"'},
            )
            return False
        supplied = header[len(prefix) :].strip()
        if not supplied or not hmac.compare_digest(supplied, self.server.token):
            self._error(
                HTTPStatus.UNAUTHORIZED,
                "invalid_token",
                "The bearer token is invalid.",
                extra_headers={
                    "WWW-Authenticate": 'Bearer realm="helios-aihub", error="invalid_token"'
                },
            )
            return False
        return True

    def _guard(self, *, public: bool = False) -> bool:
        if not self._rate_limit():
            return False
        if public:
            return True
        return self._authorized()

    def _read_json_object(self) -> dict[str, Any] | None:
        raw_length = self.headers.get("Content-Length")
        if raw_length is None:
            self._error(
                HTTPStatus.LENGTH_REQUIRED,
                "content_length_required",
                "Content-Length is required for JSON requests.",
            )
            return None
        try:
            length = int(raw_length)
        except ValueError:
            self._error(
                HTTPStatus.BAD_REQUEST,
                "invalid_content_length",
                "Content-Length must be an integer.",
            )
            return None
        if length < 0:
            self._error(
                HTTPStatus.BAD_REQUEST,
                "invalid_content_length",
                "Content-Length cannot be negative.",
            )
            return None
        if length > self.server.config.max_request_bytes:
            self._error(
                HTTPStatus.REQUEST_ENTITY_TOO_LARGE,
                "request_too_large",
                "Request body exceeds the configured limit.",
            )
            return None

        raw = self.rfile.read(length)
        try:
            decoded = raw.decode("utf-8")
        except UnicodeDecodeError:
            self._error(
                HTTPStatus.BAD_REQUEST,
                "invalid_encoding",
                "Request body must be UTF-8 JSON.",
            )
            return None
        try:
            value = json.loads(decoded or "{}")
        except json.JSONDecodeError:
            self._error(
                HTTPStatus.BAD_REQUEST,
                "invalid_json",
                "Request body must contain valid JSON.",
            )
            return None
        if not isinstance(value, dict):
            self._error(
                HTTPStatus.BAD_REQUEST,
                "object_required",
                "Request JSON root must be an object.",
            )
            return None
        if _json_depth(value) > MAX_JSON_DEPTH:
            self._error(
                HTTPStatus.BAD_REQUEST,
                "json_too_deep",
                "Request JSON exceeds the maximum nesting depth.",
            )
            return None
        return value

    def _task_list(self, task_id: str | None = None) -> list[dict[str, Any]]:
        value = self.server.tasks.read()
        tasks = value.get("tasks", []) if isinstance(value, dict) else []
        clean = [item for item in tasks if isinstance(item, dict)]
        if task_id:
            return [item for item in clean if item.get("taskId") == task_id]
        return clean

    def _admin_request_list(self) -> list[dict[str, Any]]:
        value = self.server.admin_requests.read()
        requests = value.get("requests", []) if isinstance(value, dict) else []
        return [item for item in requests if isinstance(item, dict)]

    def do_GET(self) -> None:  # noqa: N802
        parsed = urlparse(self.path)
        route = parsed.path.rstrip("/") or "/"
        query = parse_qs(parsed.query)

        if route in {"/health", "/api/health"}:
            if not self._guard(public=True):
                return
            self._json_response(
                {
                    "status": "ok",
                    "service": self.server.config.service_name,
                    "version": RUNTIME_VERSION,
                    "bind": self.server.config.host,
                    "startedAt": self.server.started_at,
                    "timestamp": utc_now(),
                    "executionMode": "queue-and-proposal-only",
                }
            )
            return

        if not self._guard():
            return

        try:
            if route in {"/status", "/api/status", "/meta"}:
                tasks = self._task_list()
                admin_requests = self._admin_request_list()
                self._json_response(
                    {
                        "status": "ok",
                        "service": self.server.config.service_name,
                        "version": RUNTIME_VERSION,
                        "timestamp": utc_now(),
                        "tasks": {
                            "total": len(tasks),
                            "queued": sum(item.get("status") == "queued" for item in tasks),
                        },
                        "adminRequests": {
                            "total": len(admin_requests),
                            "pending": sum(
                                item.get("status") == "pending-approval"
                                for item in admin_requests
                            ),
                        },
                        "capabilities": {
                            "taskExecution": False,
                            "administrativeExecution": False,
                            "secretReadback": False,
                            "cloudDeployment": False,
                        },
                    }
                )
                return

            if route in {"/tasks", "/api/tasks", "/tasks/status"}:
                task_id = query.get("task_id", [None])[0]
                tasks = self._task_list(task_id)
                self._json_response(
                    {"status": "ok", "data": {"count": len(tasks), "tasks": tasks}}
                )
                return

            if route in {"/admin-requests", "/api/admin-requests"}:
                requests = self._admin_request_list()
                self._json_response(
                    {
                        "status": "ok",
                        "data": {"count": len(requests), "requests": requests},
                    }
                )
                return
        except StateError:
            LOGGER.exception("AIHub state read failed")
            self._error(
                HTTPStatus.INTERNAL_SERVER_ERROR,
                "state_unavailable",
                "Runtime state is temporarily unavailable.",
            )
            return

        self._error(HTTPStatus.NOT_FOUND, "not_found", "The requested route does not exist.")

    def do_POST(self) -> None:  # noqa: N802
        parsed = urlparse(self.path)
        route = parsed.path.rstrip("/") or "/"

        if not self._guard():
            return
        body = self._read_json_object()
        if body is None:
            return

        correlation_id = str(uuid.uuid4())
        try:
            if route in {"/tasks", "/api/tasks", "/tasks/create"}:
                prompt = str(body.get("prompt", "")).strip()
                task_type = str(body.get("task_type", "general")).strip().lower()
                priority = str(body.get("priority", "normal")).strip().lower()
                payload = body.get("payload", {})

                if not prompt:
                    self._error(
                        HTTPStatus.BAD_REQUEST,
                        "prompt_required",
                        "prompt is required.",
                        correlation_id=correlation_id,
                    )
                    return
                if len(prompt) > MAX_PROMPT_CHARACTERS:
                    self._error(
                        HTTPStatus.BAD_REQUEST,
                        "prompt_too_long",
                        "prompt exceeds the configured character limit.",
                        correlation_id=correlation_id,
                    )
                    return
                if task_type not in ALLOWED_TASK_TYPES:
                    self._error(
                        HTTPStatus.BAD_REQUEST,
                        "unsupported_task_type",
                        "task_type is not in the secure runtime allowlist.",
                        correlation_id=correlation_id,
                    )
                    return
                if priority not in ALLOWED_PRIORITIES:
                    self._error(
                        HTTPStatus.BAD_REQUEST,
                        "unsupported_priority",
                        "priority must be low, normal, or high.",
                        correlation_id=correlation_id,
                    )
                    return
                if not isinstance(payload, dict):
                    self._error(
                        HTTPStatus.BAD_REQUEST,
                        "payload_object_required",
                        "payload must be an object.",
                        correlation_id=correlation_id,
                    )
                    return

                task = {
                    "taskId": str(uuid.uuid4()),
                    "correlationId": correlation_id,
                    "createdAt": utc_now(),
                    "updatedAt": utc_now(),
                    "status": "queued",
                    "executionAuthorized": False,
                    "taskType": task_type,
                    "priority": priority,
                    "prompt": prompt,
                    "payload": payload,
                }

                def add_task(value: Any) -> dict[str, Any]:
                    root = value if isinstance(value, dict) else {}
                    tasks = root.get("tasks")
                    if not isinstance(tasks, list):
                        tasks = []
                    tasks.append(task)
                    return {"schemaVersion": 1, "tasks": tasks}

                self.server.tasks.update(add_task)
                self._json_response(
                    {
                        "status": "queued",
                        "data": {
                            "task": task,
                            "note": "The secure compatibility runtime records tasks but does not execute them.",
                        },
                    },
                    HTTPStatus.CREATED,
                )
                return

            if route in {"/train", "/api/train", "/api/train/trigger"}:
                task = {
                    "taskId": str(uuid.uuid4()),
                    "correlationId": correlation_id,
                    "createdAt": utc_now(),
                    "updatedAt": utc_now(),
                    "status": "queued",
                    "executionAuthorized": False,
                    "taskType": "training-proposal",
                    "priority": "normal",
                    "prompt": "Review and approve a bounded AIHub training cycle.",
                    "payload": {
                        "source": "legacy-compatible-training-route",
                        "requested": body,
                    },
                }

                def add_training(value: Any) -> dict[str, Any]:
                    root = value if isinstance(value, dict) else {}
                    tasks = root.get("tasks")
                    if not isinstance(tasks, list):
                        tasks = []
                    tasks.append(task)
                    return {"schemaVersion": 1, "tasks": tasks}

                self.server.tasks.update(add_training)
                self._json_response(
                    {
                        "status": "proposal-recorded",
                        "data": {
                            "task": task,
                            "executionStarted": False,
                            "approvalRequired": True,
                        },
                    },
                    HTTPStatus.ACCEPTED,
                )
                return

            if route in {"/admin-requests", "/api/admin-requests"}:
                operation = str(body.get("operation", "")).strip()
                reason = str(body.get("reason", "")).strip()
                if not operation or not reason:
                    self._error(
                        HTTPStatus.BAD_REQUEST,
                        "operation_and_reason_required",
                        "operation and reason are required.",
                        correlation_id=correlation_id,
                    )
                    return
                if len(operation) > 256 or len(reason) > 4096:
                    self._error(
                        HTTPStatus.BAD_REQUEST,
                        "proposal_too_long",
                        "operation or reason exceeds the configured limit.",
                        correlation_id=correlation_id,
                    )
                    return

                request = {
                    "requestId": str(uuid.uuid4()),
                    "correlationId": correlation_id,
                    "createdAt": utc_now(),
                    "status": "pending-approval",
                    "operation": operation,
                    "reason": reason,
                    "executionAuthorized": False,
                }

                def add_request(value: Any) -> dict[str, Any]:
                    root = value if isinstance(value, dict) else {}
                    requests = root.get("requests")
                    if not isinstance(requests, list):
                        requests = []
                    requests.append(request)
                    return {"schemaVersion": 1, "requests": requests}

                self.server.admin_requests.update(add_request)
                self._json_response(
                    {
                        "status": "pending-approval",
                        "data": {
                            "request": request,
                            "executionStarted": False,
                        },
                    },
                    HTTPStatus.ACCEPTED,
                )
                return
        except StateError:
            LOGGER.exception("AIHub state write failed")
            self._error(
                HTTPStatus.INTERNAL_SERVER_ERROR,
                "state_unavailable",
                "Runtime state is temporarily unavailable.",
                correlation_id=correlation_id,
            )
            return

        self._error(
            HTTPStatus.NOT_FOUND,
            "not_found",
            "The requested route does not exist.",
            correlation_id=correlation_id,
        )

    def do_PUT(self) -> None:  # noqa: N802
        self._method_not_allowed()

    def do_PATCH(self) -> None:  # noqa: N802
        self._method_not_allowed()

    def do_DELETE(self) -> None:  # noqa: N802
        self._method_not_allowed()

    def _method_not_allowed(self) -> None:
        if not self._guard():
            return
        self._error(
            HTTPStatus.METHOD_NOT_ALLOWED,
            "method_not_allowed",
            "This compatibility runtime does not support the requested method.",
            extra_headers={"Allow": "GET, POST"},
        )


def create_server(
    config: SecureRuntimeConfig,
    *,
    token: str | None = None,
) -> SecureAIHubServer:
    return SecureAIHubServer(config, token or config.read_token())


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Run the hardened local HELIOS AIHub compatibility API."
    )
    parser.add_argument("--config", type=Path)
    parser.add_argument("--health-check", action="store_true")
    parser.add_argument("--log-level", default="INFO")
    args = parser.parse_args()

    logging.basicConfig(
        level=getattr(logging, str(args.log_level).upper(), logging.INFO),
        format="%(asctime)s %(levelname)s %(name)s %(message)s",
    )

    try:
        config = SecureRuntimeConfig.load(args.config)
        if args.health_check:
            print(
                json.dumps(
                    {
                        "status": "ok",
                        "service": config.service_name,
                        "version": RUNTIME_VERSION,
                        "host": config.host,
                        "port": config.port,
                        "executionMode": "queue-and-proposal-only",
                    },
                    indent=2,
                )
            )
            return 0
        server = create_server(config)
    except ConfigurationError as exc:
        LOGGER.error("Configuration rejected: %s", exc)
        return 2

    LOGGER.info(
        "HELIOS secure AIHub listening on http://%s:%s",
        config.host,
        server.server_address[1],
    )
    try:
        server.serve_forever(poll_interval=0.25)
    except KeyboardInterrupt:
        LOGGER.info("Shutdown requested")
    finally:
        server.shutdown()
        server.server_close()
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
