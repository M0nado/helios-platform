from __future__ import annotations

import http.client
import json
from pathlib import Path
import tempfile
import threading
import unittest

from python.aihub.secure_runtime.catalog_server import create_catalog_server
from python.aihub.secure_runtime.config import RuntimeConfig


class CatalogServerTests(unittest.TestCase):
    TOKEN = "catalog-test-token-at-least-32-characters"

    def setUp(self) -> None:
        self.temp_directory = tempfile.TemporaryDirectory()
        config = RuntimeConfig(
            host="127.0.0.1",
            port=0,
            state_directory=Path(self.temp_directory.name),
            api_token=self.TOKEN,
            max_request_bytes=4096,
            requests_per_minute=100,
            max_tasks_returned=25,
        )
        self.server = create_catalog_server(config)
        self.thread = threading.Thread(target=self.server.serve_forever, daemon=True)
        self.thread.start()
        self.host, self.port = self.server.server_address[:2]

    def tearDown(self) -> None:
        self.server.shutdown()
        self.server.server_close()
        self.thread.join(timeout=5)
        self.temp_directory.cleanup()

    def get(self, path: str, *, token: str | None = None) -> tuple[int, dict]:
        connection = http.client.HTTPConnection(self.host, self.port, timeout=5)
        headers = {"Authorization": f"Bearer {token}"} if token else {}
        connection.request("GET", path, headers=headers)
        response = connection.getresponse()
        payload = json.loads(response.read())
        connection.close()
        return response.status, payload

    def test_catalog_routes_require_authentication(self) -> None:
        status, payload = self.get("/api/engines/catalog")
        self.assertEqual(status, 401)
        self.assertEqual(payload["error"]["code"], "unauthorized")

    def test_security_plan_is_proposal_only(self) -> None:
        status, payload = self.get(
            "/api/security/plan?profile=offline",
            token=self.TOKEN,
        )
        self.assertEqual(status, 200)
        self.assertEqual(payload["plan"]["egress_policy"], "deny")
        self.assertTrue(payload["proposalOnly"])
        self.assertFalse(payload["productionEnabled"])

    def test_vm_and_model_catalogs_are_disabled_for_production(self) -> None:
        status, topology = self.get("/api/vm/topology", token=self.TOKEN)
        self.assertEqual(status, 200)
        self.assertTrue(all(target["mutation_authority"] == "proposal-only" for target in topology["targets"]))

        status, registry = self.get("/api/models/registry", token=self.TOKEN)
        self.assertEqual(status, 200)
        self.assertGreaterEqual(registry["count"], 15)
        self.assertTrue(all(model["production_enabled"] is False for model in registry["models"]))

    def test_engine_catalog_and_recommendation_are_bounded(self) -> None:
        status, catalog = self.get("/api/engines/catalog?cuda=false", token=self.TOKEN)
        self.assertEqual(status, 200)
        self.assertFalse(catalog["cudaEnabled"])
        self.assertFalse(any(engine["requires_cuda"] for engine in catalog["engines"]))

        status, recommendation = self.get(
            "/api/engines/recommend?cuda=true&security_profile=paranoid&optimization_pressure=0.9&fleet_size=250",
            token=self.TOKEN,
        )
        self.assertEqual(status, 200)
        names = {engine["name"] for engine in recommendation["selectedEngines"]}
        self.assertIn("security-anomaly-core", names)
        self.assertIn("mesh-consensus-engine", names)
        self.assertTrue(recommendation["proposalOnly"])

    def test_invalid_catalog_inputs_fail_closed(self) -> None:
        status, payload = self.get(
            "/api/engines/recommend?cuda=maybe",
            token=self.TOKEN,
        )
        self.assertEqual(status, 400)
        self.assertEqual(payload["error"]["code"], "invalid_catalog_request")

        status, payload = self.get(
            "/api/security/plan?profile=unrestricted",
            token=self.TOKEN,
        )
        self.assertEqual(status, 400)
        self.assertEqual(payload["error"]["code"], "invalid_catalog_request")


if __name__ == "__main__":
    unittest.main()
