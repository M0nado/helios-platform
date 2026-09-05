from __future__ import annotations

from http import HTTPStatus
from pathlib import Path
import json
import tempfile
import threading
import time
import unittest
from urllib import error, request

from python.aihub.secure_runtime.config import ConfigurationError, SecureRuntimeConfig
from python.aihub.secure_runtime.server import create_server
from python.aihub.secure_runtime.state import AtomicJsonStore


class SecureRuntimeHttpTests(unittest.TestCase):
    TOKEN = "test-token-0123456789-abcdefghijklmnopqrstuvwxyz"

    def setUp(self) -> None:
        self.temporary = tempfile.TemporaryDirectory()
        config = SecureRuntimeConfig(
            host="127.0.0.1",
            port=0,
            state_directory=Path(self.temporary.name),
            max_request_bytes=1024,
            requests_per_minute=100,
        )
        self.server = create_server(config, token=self.TOKEN)
        self.thread = threading.Thread(
            target=self.server.serve_forever,
            kwargs={"poll_interval": 0.01},
            daemon=True,
        )
        self.thread.start()
        self.base_url = f"http://127.0.0.1:{self.server.server_address[1]}"

    def tearDown(self) -> None:
        self.server.shutdown()
        self.server.server_close()
        self.thread.join(timeout=2)
        self.temporary.cleanup()

    def call(
        self,
        path: str,
        *,
        method: str = "GET",
        body: dict | bytes | None = None,
        authenticated: bool = False,
    ) -> tuple[int, dict]:
        headers = {"Accept": "application/json"}
        if authenticated:
            headers["Authorization"] = f"Bearer {self.TOKEN}"
        data: bytes | None = None
        if isinstance(body, dict):
            data = json.dumps(body).encode("utf-8")
            headers["Content-Type"] = "application/json"
        elif isinstance(body, bytes):
            data = body
            headers["Content-Type"] = "application/json"

        req = request.Request(
            f"{self.base_url}{path}",
            method=method,
            data=data,
            headers=headers,
        )
        try:
            with request.urlopen(req, timeout=3) as response:
                return response.status, json.loads(response.read().decode("utf-8"))
        except error.HTTPError as exc:
            return exc.code, json.loads(exc.read().decode("utf-8"))

    def test_health_is_public_and_redacted(self) -> None:
        status, payload = self.call("/api/health")
        self.assertEqual(status, HTTPStatus.OK)
        self.assertEqual(payload["status"], "ok")
        self.assertEqual(payload["bind"], "127.0.0.1")
        self.assertNotIn(self.TOKEN, json.dumps(payload))

    def test_protected_route_rejects_missing_token(self) -> None:
        status, payload = self.call("/api/status")
        self.assertEqual(status, HTTPStatus.UNAUTHORIZED)
        self.assertEqual(payload["error"]["code"], "authentication_required")

    def test_queue_only_task_roundtrip(self) -> None:
        status, created = self.call(
            "/api/tasks",
            method="POST",
            authenticated=True,
            body={
                "prompt": "Validate the current HELIOS source tree.",
                "task_type": "validation",
                "priority": "high",
                "payload": {"execute": False},
            },
        )
        self.assertEqual(status, HTTPStatus.CREATED)
        task = created["data"]["task"]
        self.assertEqual(task["status"], "queued")
        self.assertFalse(task["executionAuthorized"])

        status, listed = self.call("/api/tasks", authenticated=True)
        self.assertEqual(status, HTTPStatus.OK)
        self.assertEqual(listed["data"]["count"], 1)
        self.assertEqual(listed["data"]["tasks"][0]["taskId"], task["taskId"])

    def test_legacy_training_route_records_proposal_only(self) -> None:
        status, payload = self.call(
            "/api/train/trigger",
            method="POST",
            authenticated=True,
            body={"cycles": 1},
        )
        self.assertEqual(status, HTTPStatus.ACCEPTED)
        self.assertEqual(payload["status"], "proposal-recorded")
        self.assertFalse(payload["data"]["executionStarted"])
        self.assertTrue(payload["data"]["approvalRequired"])

    def test_admin_request_never_executes(self) -> None:
        status, payload = self.call(
            "/api/admin-requests",
            method="POST",
            authenticated=True,
            body={
                "operation": "run-development-what-if",
                "reason": "Capture a reviewed plan before deployment.",
            },
        )
        self.assertEqual(status, HTTPStatus.ACCEPTED)
        proposal = payload["data"]["request"]
        self.assertEqual(proposal["status"], "pending-approval")
        self.assertFalse(proposal["executionAuthorized"])
        self.assertFalse(payload["data"]["executionStarted"])

    def test_oversized_request_is_rejected(self) -> None:
        status, payload = self.call(
            "/api/tasks",
            method="POST",
            authenticated=True,
            body=b'{' + (b'"x":"' + b'a' * 2000 + b'"}') + b'}',
        )
        self.assertEqual(status, HTTPStatus.REQUEST_ENTITY_TOO_LARGE)
        self.assertEqual(payload["error"]["code"], "request_too_large")


class SecureRuntimePolicyTests(unittest.TestCase):
    def test_public_bind_is_rejected(self) -> None:
        with self.assertRaises(ConfigurationError):
            SecureRuntimeConfig(host="0.0.0.0")

    def test_short_token_is_rejected(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            config = SecureRuntimeConfig(
                port=0,
                state_directory=Path(temporary),
            )
            with self.assertRaises(ConfigurationError):
                create_server(config, token="short")

    def test_atomic_store_replaces_complete_document(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            path = Path(temporary) / "state.json"
            store = AtomicJsonStore(path, default_factory=lambda: {"items": []})
            store.write({"items": [{"id": 1}]})
            store.update(
                lambda value: {
                    "items": [*value["items"], {"id": 2}],
                }
            )
            self.assertEqual(store.read(), {"items": [{"id": 1}, {"id": 2}]})
            self.assertEqual(list(Path(temporary).glob("*.tmp")), [])


if __name__ == "__main__":
    unittest.main()
