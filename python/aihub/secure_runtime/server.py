from __future__ import annotations

from collections import defaultdict, deque
from dataclasses import asdict
from datetime import datetime, timezone
from http import HTTPStatus
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
import hmac
import json
import logging
from pathlib import Path
import threading
import time
from typing import Any, Final
from urllib.parse import parse_qs, urlparse

from .config import RuntimeConfig
from .storage import AtomicTaskStore


_LOG = logging.getLogger("helios.aihub.secure_runtime")
_PUBLIC_ROUTES: Final = frozenset({"/health", "/api/health"})
_PROTECTED_GET_ROUTES: Final = frozenset({"/api/status", "/api/tasks"})
_PROTECTED_POST_ROUTES: Final = frozenset({"/api/tasks", "/api/train/trigger"})


class SlidingWindowRateLimiter:
    def __init__(self, *, requests_per_minute: int):
        self._limit = requests_per_minute
        self._events: dict[str, deque[float]] = defaultdict(deque)
        self._lock = threading.Lock()

    def allow(self, identity: str, *, now: float | None = None) -> bool:
        current = time.monotonic() if now is None else now
        cutoff = current - 60.0
        with self._lock:
            events = self._events[identity]
            while events and events[0] <= cutoff:
                events.popleft()
            if len(events) >= self._limit:
                return False
            events.append(current)
            return True


class SecureThreadingHTTPServer(ThreadingHTTPServer):
    daemon_threads = True
    allow_reuse_address = True

    def __init__(
        self,
        server_address: tuple[str, int],
        handler_class: type[BaseHTTPRequestHandler],
        *,
        runtime_config: RuntimeConfig,
        task_store: AtomicTaskStore,
    ):
        self.runtime_config = runtime_config
        self.task_store = task_store
        self.rate_limiter = SlidingWindowRateLimiter(
            requests_per_minute=runtime_config.requests_per_minute
        )
        super().__init__(server_address, handler_class)


class SecureAIHubHandler(BaseHTTPRequestHandler):
    server: SecureThreadingHTTPServer
    protocol_version = "HTTP/1.1"

    def log_message(self, format: str, *args: object) -> None:
        # Never log request headers, bearer tokens, or request bodies.
        _LOG.info(
            "%s %s",
            self.client_address[0] if self.client_address else "unknown",
            format % args,
        )

    @staticmethod
    def _utc_now() -> str:
        return datetime.now(timezone.utc).isoformat()

    def _send_security_headers(self) -> None:
        self.send_header("Cache-Control", "no-store")
        self.send_header("Content-Security-Policy", "default-src 'none'; frame-ancestors 'none'")
        self.send_header("Cross-Origin-Resource-Policy", "same-origin")
        self.send_header("Referrer-Policy", "no-referrer")
        self.send_header("X-Content-Type-Options", "nosniff")
        self.send_header("X-Frame-Options", "DENY")
        self.send_header("X-Permitted-Cross-Domain-Policies", "none")

    def _json(self, payload: dict[str, Any], *, status: HTTPStatus = HTTPStatus.OK) -> None:
        encoded = (json.dumps(payload, indent=2, sort_keys=True) + "\n").encode("utf-8")
        self.send_response(status.value)
        self.send_header("Content-Type", "application/json; charset=utf-8")
        self.send_header("Content-Length", str(len(encoded)))
        self._send_security_headers()
        self.end_headers()
        self.wfile.write(encoded)

    def _error(
        self,
        *,
        status: HTTPStatus,
        code: str,
        message: str,
    ) -> None:
        self._json(
            {
                "error": {
                    "code": code,
                    "message": message,
                },
                "timestamp": self._utc_now(),
            },
            status=status,
        )

    def _rate_limit_identity(self) -> str:
        # The server is loopback-only, so client address is sufficient. We do not
        # trust proxy headers and never use them to weaken the local boundary.
        return self.client_address[0] if self.client_address else "unknown"

    def _passes_rate_limit(self) -> bool:
        if self.server.rate_limiter.allow(self._rate_limit_identity()):
            return True
        self._error(
            status=HTTPStatus.TOO_MANY_REQUESTS,
            code="rate_limit_exceeded",
            message="The local AIHub request limit has been exceeded.",
        )
        return False

    def _provided_bearer_token(self) -> str:
        header = self.headers.get("Authorization", "")
        scheme, separator, value = header.partition(" ")
        if not separator or scheme.lower() != "bearer":
            return ""
        return value.strip()

    def _is_authorized(self) -> bool:
        provided = self._provided_bearer_token()
        expected = self.server.runtime_config.api_token
        return bool(provided) and hmac.compare_digest(provided, expected)

    def _require_authorization(self) -> bool:
        if self._is_authorized():
            return True
        self.send_response(HTTPStatus.UNAUTHORIZED.value)
        self.send_header("Content-Type", "application/json; charset=utf-8")
        self.send_header("WWW-Authenticate", 'Bearer realm="HELIOS AIHub"')
        payload = (
            json.dumps(
                {
                    "error": {
                        "code": "unauthorized",
                        "message": "A valid local bearer token is required.",
                    },
                    "timestamp": self._utc_now(),
                },
                indent=2,
                sort_keys=True,
            )
            + "\n"
        ).encode("utf-8")
        self.send_header("Content-Length", str(len(payload)))
        self._send_security_headers()
        self.end_headers()
        self.wfile.write(payload)
        return False

    def _read_json_object(self) -> dict[str, Any] | None:
        content_type = self.headers.get("Content-Type", "").split(";", 1)[0].strip().lower()
        if content_type != "application/json":
            self._error(
                status=HTTPStatus.UNSUPPORTED_MEDIA_TYPE,
                code="unsupported_media_type",
                message="Content-Type must be application/json.",
            )
            return None

        raw_length = self.headers.get("Content-Length")
        if raw_length is None:
            self._error(
                status=HTTPStatus.LENGTH_REQUIRED,
                code="content_length_required",
                message="Content-Length is required.",
            )
            return None
        try:
            length = int(raw_length)
        except ValueError:
            self._error(
                status=HTTPStatus.BAD_REQUEST,
                code="invalid_content_length",
                message="Content-Length must be an integer.",
            )
            return None
        if length < 0:
            self._error(
                status=HTTPStatus.BAD_REQUEST,
                code="invalid_content_length",
                message="Content-Length cannot be negative.",
            )
            return None
        if length > self.server.runtime_config.max_request_bytes:
            self._error(
                status=HTTPStatus.REQUEST_ENTITY_TOO_LARGE,
                code="request_too_large",
                message="The request body exceeds the configured limit.",
            )
            return None

        raw = self.rfile.read(length)
        try:
            payload = json.loads(raw.decode("utf-8"))
        except (UnicodeDecodeError, json.JSONDecodeError):
            self._error(
                status=HTTPStatus.BAD_REQUEST,
                code="invalid_json",
                message="The request body must be valid UTF-8 JSON.",
            )
            return None
        if not isinstance(payload, dict):
            self._error(
                status=HTTPStatus.BAD_REQUEST,
                code="invalid_json_shape",
                message="The JSON body must be an object.",
            )
            return None
        return payload

    def _health(self) -> dict[str, Any]:
        config = self.server.runtime_config
        return {
            "status": "ok",
            "service": "helios-aihub-secure-runtime",
            "timestamp": self._utc_now(),
            "listener": {
                "host": config.host,
                "port": self.server.server_address[1],
                "loopbackOnly": True,
            },
            "security": {
                "protectedRoutesRequireBearer": True,
                "requestLimitBytes": config.max_request_bytes,
                "requestsPerMinute": config.requests_per_minute,
                "arbitraryShellEnabled": False,
                "automaticTaskExecution": False,
                "cloudDeploymentEnabled": False,
                "privilegedWindowsMutationEnabled": False,
            },
        }

    def do_GET(self) -> None:  # noqa: N802
        if not self._passes_rate_limit():
            return

        parsed = urlparse(self.path)
        route = parsed.path
        if route in _PUBLIC_ROUTES:
            self._json(self._health())
            return
        if route not in _PROTECTED_GET_ROUTES:
            self._error(
                status=HTTPStatus.NOT_FOUND,
                code="not_found",
                message="The requested route does not exist.",
            )
            return
        if not self._require_authorization():
            return

        if route == "/api/status":
            self._json(
                {
                    "status": "ok",
                    "service": "helios-aihub-secure-runtime",
                    "timestamp": self._utc_now(),
                    "queuedTaskCount": self.server.task_store.count(),
                    "executionMode": "queue-only",
                    "productionEnabled": False,
                }
            )
            return

        query = parse_qs(parsed.query)
        raw_limit = query.get("limit", [str(self.server.runtime_config.max_tasks_returned)])[0]
        try:
            limit = int(raw_limit)
        except ValueError:
            self._error(
                status=HTTPStatus.BAD_REQUEST,
                code="invalid_limit",
                message="limit must be an integer.",
            )
            return
        limit = max(1, min(limit, self.server.runtime_config.max_tasks_returned))
        tasks = self.server.task_store.list_tasks(limit=limit)
        self._json({"count": len(tasks), "tasks": tasks})

    def do_POST(self) -> None:  # noqa: N802
        if not self._passes_rate_limit():
            return

        route = urlparse(self.path).path
        if route not in _PROTECTED_POST_ROUTES:
            self._error(
                status=HTTPStatus.NOT_FOUND,
                code="not_found",
                message="The requested route does not exist.",
            )
            return
        if not self._require_authorization():
            return
        body = self._read_json_object()
        if body is None:
            return

        if route == "/api/train/trigger":
            task_type = "training.proposal"
            priority = "high"
            payload = {
                "requestedBy": "local-api",
                "proposalOnly": True,
                "parameters": body,
            }
        else:
            task_type = str(body.get("task_type", "general"))
            priority = str(body.get("priority", "normal"))
            raw_payload = body.get("payload", {})
            if not isinstance(raw_payload, dict):
                self._error(
                    status=HTTPStatus.BAD_REQUEST,
                    code="invalid_payload",
                    message="payload must be a JSON object.",
                )
                return
            payload = raw_payload

        try:
            record = self.server.task_store.enqueue(
                task_type=task_type,
                priority=priority,
                payload=payload,
            )
        except ValueError as exc:
            self._error(
                status=HTTPStatus.BAD_REQUEST,
                code="invalid_task",
                message=str(exc),
            )
            return
        except RuntimeError:
            _LOG.exception("Task-store operation failed")
            self._error(
                status=HTTPStatus.INTERNAL_SERVER_ERROR,
                code="task_store_failure",
                message="The task could not be persisted.",
            )
            return

        self._json(
            {
                "status": "queued",
                "task": asdict(record),
                "automaticExecution": False,
            },
            status=HTTPStatus.ACCEPTED,
        )


def create_server(
    config: RuntimeConfig,
    *,
    task_store: AtomicTaskStore | None = None,
) -> SecureThreadingHTTPServer:
    config.validate()
    store = task_store or AtomicTaskStore(config.state_directory / "tasks.json")
    return SecureThreadingHTTPServer(
        (config.host, config.port),
        SecureAIHubHandler,
        runtime_config=config,
        task_store=store,
    )


def run_server(config: RuntimeConfig | None = None) -> None:
    active_config = config or RuntimeConfig.from_environment()
    server = create_server(active_config)
    _LOG.info(
        "HELIOS AIHub secure runtime listening on http://%s:%s",
        active_config.host,
        server.server_address[1],
    )
    try:
        server.serve_forever(poll_interval=0.25)
    except KeyboardInterrupt:
        pass
    finally:
        server.shutdown()
        server.server_close()


if __name__ == "__main__":
    logging.basicConfig(level=logging.INFO)
    run_server()
