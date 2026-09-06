from __future__ import annotations

import http.client
import json
from pathlib import Path
import tempfile
import threading
import unittest

from python.aihub.secure_runtime.config import RuntimeConfig
from python.aihub.secure_runtime.server import SlidingWindowRateLimiter, create_server
from python.aihub.secure_runtime.storage import AtomicTaskStore


class RuntimeConfigTests(unittest.TestCase):
    def test_rejects_public_listener(self) -> None:
        config = RuntimeConfig(host="0.0.0.0", api_token="x" * 32)
        with self.assertRaisesRegex(ValueError, "Non-loopback"):
            config.validate()

    def test_requires_nontrivial_token(self) -> None:
        config = RuntimeConfig(api_token="short")
        with self.assertRaisesRegex(ValueError, "at least 24"):
            config.validate()


class AtomicTaskStoreTests(unittest.TestCase):
    def test_queue_round_trip_is_atomic_and_json_safe(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "tasks.json"
            store = AtomicTaskStore(path)
            record = store.enqueue(
                task_type="security.audit",
                priority="high",
                payload={"target": "local", "execute": False},
            )

            self.assertEqual(store.count(), 1)
            self.assertEqual(record.status, "queued")
            self.assertEqual(store.list_tasks()[0]["task_id"], record.task_id)
            data = json.loads(path.read_text(encoding="utf-8"))
            self.assertEqual(data["schemaVersion"], 1)
            self.assertEqual(len(data["tasks"]), 1)
            self.assertFalse(any(path.parent.glob("*.tmp")))

    def test_rejects_invalid_task_contract(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            store = AtomicTaskStore(Path(directory) / "tasks.json")
            with self.assertRaises(ValueError):
                store.enqueue(task_type="INVALID TYPE", priority="normal", payload={})
            with self.assertRaises(ValueError):
                store.enqueue(task_type="valid", priority="urgent", payload={})
            with self.assertRaises(ValueError):
                store.enqueue(task_type="valid", priority="normal", payload={"n": float("nan")})


class RateLimiterTests(unittest.TestCase):
    def test_sliding_window_limit(self) -> None:
        limiter = SlidingWindowRateLimiter(requests_per_minute=2)
        self.assertTrue(limiter.allow("127.0.0.1", now=10.0))
        self.assertTrue(limiter.allow("127.0.0.1", now=11.0))
        self.assertFalse(limiter.allow("127.0.0.1", now=12.0))
        self.assertTrue(limiter.allow("127.0.0.1", now=71.1))


class SecureRuntimeHttpTests(unittest.TestCase):
    TOKEN = "test-token-that-is-at-least-32-characters"

    def setUp(self) -> None:
        self.temp_directory = tempfile.TemporaryDirectory()
        config = RuntimeConfig(
            host="127.0.0.1",
            port=0,
            state_directory=Path(self.temp_directory.name),
            api_token=self.TOKEN,
            max_request_bytes=1024,
            requests_per_minute=100,
            max_tasks_returned=25,
        )
        self.server = create_server(config)
        self.thread = threading.Thread(target=self.server.serve_forever, daemon=True)
        self.thread.start()
        self.host, self.port = self.server.server_address[:2]

    def tearDown(self) -> None:
        self.server.shutdown()
        self.server.server_close()
        self.thread.join(timeout=5)
        self.temp_directory.cleanup()

    def request(
        self,
        method: str,
        path: str,
        *,
        token: str | None = None,
        payload: object | None = None,
        raw_body: bytes | None = None,
        content_type: str = "application/json",
    ) -> tuple[int, dict, dict[str, str]]:
        connection = http.client.HTTPConnection(self.host, self.port, timeout=5)
        headers: dict[str, str] = {}
        if token is not None:
            headers["Authorization"] = f"Bearer {token}"
        body = raw_body
        if payload is not None:
            body = json.dumps(payload).encode("utf-8")
        if body is not None:
            headers["Content-Type"] = content_type
        connection.request(method, path, body=body, headers=headers)
        response = connection.getresponse()
        response_body = response.read()
        response_headers = {name.lower(): value for name, value in response.getheaders()}
        connection.close()
        return response.status, json.loads(response_body), response_headers

    def test_health_is_public_but_reveals_no_secret(self) -> None:
        status, payload, headers = self.request("GET", "/api/health")
        self.assertEqual(status, 200)
        self.assertEqual(payload["status"], "ok")
        self.assertTrue(payload["listener"]["loopbackOnly"])
        self.assertNotIn(self.TOKEN, json.dumps(payload))
        self.assertEqual(headers["cache-control"], "no-store")
        self.assertEqual(headers["x-content-type-options"], "nosniff")

    def test_protected_route_rejects_missing_and_wrong_token(self) -> None:
        status, payload, headers = self.request("GET", "/api/status")
        self.assertEqual(status, 401)
        self.assertEqual(payload["error"]["code"], "unauthorized")
        self.assertIn("Bearer", headers["www-authenticate"])

        status, payload, _ = self.request(
            "GET",
            "/api/status",
            token="wrong-token-that-is-long-enough-but-invalid",
        )
        self.assertEqual(status, 401)
        self.assertEqual(payload["error"]["code"], "unauthorized")

    def test_valid_token_queues_but_does_not_execute(self) -> None:
        status, payload, _ = self.request(
            "POST",
            "/api/tasks",
            token=self.TOKEN,
            payload={
                "task_type": "docs.generate",
                "priority": "normal",
                "payload": {"document": "checkpoint"},
            },
        )
        self.assertEqual(status, 202)
        self.assertEqual(payload["status"], "queued")
        self.assertFalse(payload["automaticExecution"])

        status, listing, _ = self.request("GET", "/api/tasks", token=self.TOKEN)
        self.assertEqual(status, 200)
        self.assertEqual(listing["count"], 1)
        self.assertEqual(listing["tasks"][0]["task_type"], "docs.generate")

    def test_training_trigger_is_proposal_only(self) -> None:
        status, payload, _ = self.request(
            "POST",
            "/api/train/trigger",
            token=self.TOKEN,
            payload={"cycles": 1},
        )
        self.assertEqual(status, 202)
        self.assertEqual(payload["task"]["task_type"], "training.proposal")
        self.assertTrue(payload["task"]["payload"]["proposalOnly"])
        self.assertFalse(payload["automaticExecution"])

    def test_rejects_oversized_and_non_json_requests(self) -> None:
        status, payload, _ = self.request(
            "POST",
            "/api/tasks",
            token=self.TOKEN,
            raw_body=b"x" * 2048,
        )
        self.assertEqual(status, 413)
        self.assertEqual(payload["error"]["code"], "request_too_large")

        status, payload, _ = self.request(
            "POST",
            "/api/tasks",
            token=self.TOKEN,
            raw_body=b"hello",
            content_type="text/plain",
        )
        self.assertEqual(status, 415)
        self.assertEqual(payload["error"]["code"], "unsupported_media_type")

    def test_unknown_route_fails_closed(self) -> None:
        status, payload, _ = self.request("GET", "/api/shell", token=self.TOKEN)
        self.assertEqual(status, 404)
        self.assertEqual(payload["error"]["code"], "not_found")


if __name__ == "__main__":
    unittest.main()
