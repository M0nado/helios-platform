from __future__ import annotations

import io
import json
import os
from pathlib import Path
import tempfile
import threading
import unittest
from unittest.mock import patch

from python.aihub.secure_cli import _load_token, _validate_base_url, main
from python.aihub.secure_runtime.catalog_server import create_catalog_server
from python.aihub.secure_runtime.config import RuntimeConfig


class SecureCliContractTests(unittest.TestCase):
    TOKEN = "secure-cli-test-token-at-least-32-characters"

    def test_base_url_is_loopback_only(self) -> None:
        self.assertEqual(
            _validate_base_url("http://127.0.0.1:8787"),
            "http://127.0.0.1:8787",
        )
        self.assertEqual(
            _validate_base_url("http://localhost:8787/"),
            "http://localhost:8787",
        )
        with self.assertRaisesRegex(ValueError, "non-loopback"):
            _validate_base_url("http://0.0.0.0:8787")
        with self.assertRaisesRegex(ValueError, "http://"):
            _validate_base_url("https://127.0.0.1:8787")
        with self.assertRaisesRegex(ValueError, "embedded"):
            _validate_base_url("http://user:password@127.0.0.1:8787")

    def test_token_loads_from_environment_or_local_file(self) -> None:
        with patch.dict(os.environ, {"AIHUB_API_KEY": self.TOKEN}, clear=True):
            self.assertEqual(_load_token(), self.TOKEN)

        with tempfile.TemporaryDirectory() as directory:
            token_path = Path(directory) / "token"
            token_path.write_text(self.TOKEN + "\n", encoding="utf-8")
            with patch.dict(
                os.environ,
                {"AIHUB_API_KEY_FILE": str(token_path)},
                clear=True,
            ):
                self.assertEqual(_load_token(), self.TOKEN)

        with patch.dict(os.environ, {}, clear=True):
            with self.assertRaisesRegex(RuntimeError, "AIHUB_API_KEY"):
                _load_token()


class SecureCliIntegrationTests(unittest.TestCase):
    TOKEN = "secure-cli-integration-token-at-least-32-chars"

    def setUp(self) -> None:
        self.temp_directory = tempfile.TemporaryDirectory()
        config = RuntimeConfig(
            host="127.0.0.1",
            port=0,
            state_directory=Path(self.temp_directory.name),
            api_token=self.TOKEN,
            max_request_bytes=4096,
            requests_per_minute=100,
            max_tasks_returned=100,
        )
        self.server = create_catalog_server(config)
        self.thread = threading.Thread(target=self.server.serve_forever, daemon=True)
        self.thread.start()
        host, port = self.server.server_address[:2]
        self.base_url = f"http://{host}:{port}"

    def tearDown(self) -> None:
        self.server.shutdown()
        self.server.server_close()
        self.thread.join(timeout=5)
        self.temp_directory.cleanup()

    def run_cli(self, *args: str) -> tuple[int, dict]:
        stdout = io.StringIO()
        stderr = io.StringIO()
        with patch.dict(os.environ, {"AIHUB_API_KEY": self.TOKEN}, clear=True):
            with patch("sys.stdout", stdout), patch("sys.stderr", stderr):
                code = main(["--base-url", self.base_url, *args])
        stream = stdout.getvalue() if code == 0 else stderr.getvalue()
        return code, json.loads(stream)

    def test_status_and_queue_round_trip(self) -> None:
        code, status = self.run_cli("status")
        self.assertEqual(code, 0)
        self.assertEqual(status["executionMode"], "queue-only")
        self.assertFalse(status["productionEnabled"])

        code, queued = self.run_cli(
            "queue",
            "docs.checkpoint",
            "--priority",
            "high",
            "--payload-json",
            '{"target":"sharepoint","execute":false}',
        )
        self.assertEqual(code, 0)
        self.assertEqual(queued["status"], "queued")
        self.assertFalse(queued["automaticExecution"])

        code, tasks = self.run_cli("tasks", "--limit", "10")
        self.assertEqual(code, 0)
        self.assertEqual(tasks["count"], 1)
        self.assertEqual(tasks["tasks"][0]["task_type"], "docs.checkpoint")

    def test_training_command_creates_proposal_only(self) -> None:
        code, result = self.run_cli("training-proposal", "--cycles", "3")
        self.assertEqual(code, 0)
        self.assertEqual(result["task"]["task_type"], "training.proposal")
        self.assertTrue(result["task"]["payload"]["proposalOnly"])
        self.assertFalse(result["automaticExecution"])

    def test_catalog_commands_are_protected_and_proposal_only(self) -> None:
        code, security = self.run_cli("security-plan", "--profile", "offline")
        self.assertEqual(code, 0)
        self.assertEqual(security["plan"]["egress_policy"], "deny")
        self.assertTrue(security["proposalOnly"])

        code, recommendation = self.run_cli(
            "recommend",
            "--no-cuda",
            "--security-profile",
            "paranoid",
            "--optimization-pressure",
            "0.8",
            "--fleet-size",
            "250",
        )
        self.assertEqual(code, 0)
        self.assertTrue(recommendation["proposalOnly"])
        self.assertFalse(recommendation["productionEnabled"])
        self.assertFalse(
            any(engine["requires_cuda"] for engine in recommendation["selectedEngines"])
        )

    def test_invalid_payload_and_remote_url_fail_closed(self) -> None:
        stdout = io.StringIO()
        stderr = io.StringIO()
        with patch.dict(os.environ, {"AIHUB_API_KEY": self.TOKEN}, clear=True):
            with patch("sys.stdout", stdout), patch("sys.stderr", stderr):
                code = main(
                    [
                        "--base-url",
                        self.base_url,
                        "queue",
                        "docs.test",
                        "--payload-json",
                        "[]",
                    ]
                )
        self.assertEqual(code, 1)
        self.assertIn("JSON object", json.loads(stderr.getvalue())["error"])

        with patch.dict(os.environ, {"AIHUB_API_KEY": self.TOKEN}, clear=True):
            with patch("sys.stdout", io.StringIO()), patch("sys.stderr", stderr := io.StringIO()):
                code = main(["--base-url", "http://example.com:8787", "status"])
        self.assertEqual(code, 1)
        self.assertIn("non-loopback", json.loads(stderr.getvalue())["error"])


if __name__ == "__main__":
    unittest.main()
