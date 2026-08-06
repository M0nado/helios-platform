from __future__ import annotations

import os
import sys
import unittest
from pathlib import Path
from unittest.mock import AsyncMock, patch

from httpx import ASGITransport, AsyncClient

SERVICE_ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(SERVICE_ROOT))

from helios_deployment_agent.agent import GovernedPlan, PlanStep  # noqa: E402
from helios_deployment_agent.main import app  # noqa: E402


class HttpBoundaryTests(unittest.IsolatedAsyncioTestCase):
    async def asyncSetUp(self) -> None:
        self.original_token = os.environ.get("HELIOS_PLANNER_BEARER_TOKEN")
        self.original_key = os.environ.get("OPENAI_API_KEY")
        os.environ["HELIOS_PLANNER_BEARER_TOKEN"] = "unit-test-token"
        os.environ["OPENAI_API_KEY"] = "configured-for-readiness-only"
        self.client = AsyncClient(
            transport=ASGITransport(app=app),
            base_url="http://test",
        )

    async def asyncTearDown(self) -> None:
        await self.client.aclose()
        for name, value in (
            ("HELIOS_PLANNER_BEARER_TOKEN", self.original_token),
            ("OPENAI_API_KEY", self.original_key),
        ):
            if value is None:
                os.environ.pop(name, None)
            else:
                os.environ[name] = value

    async def test_liveness_and_readiness_are_distinct(self) -> None:
        self.assertEqual(200, (await self.client.get("/health/live")).status_code)
        self.assertEqual(200, (await self.client.get("/health/ready")).status_code)

    async def test_plan_requires_authentication(self) -> None:
        response = await self.client.post(
            "/api/v1/plans",
            json={"objective": "compare safely", "manifest": {}},
        )
        self.assertEqual(401, response.status_code)

    async def test_unknown_fields_and_sensitive_objective_fail_before_provider(self) -> None:
        headers = {"Authorization": "Bearer unit-test-token"}
        extra = await self.client.post(
            "/api/v1/plans",
            headers=headers,
            json={"objective": "compare safely", "manifest": {"arbitrary": "value"}},
        )
        self.assertEqual(422, extra.status_code)
        sensitive = await self.client.post(
            "/api/v1/plans",
            headers=headers,
            json={"objective": "use api_key=abcdefghijklmnop", "manifest": {}},
        )
        self.assertEqual(400, sensitive.status_code)

    async def test_server_computes_policy_and_never_forwards_private_names(self) -> None:
        plan = GovernedPlan(
            execution_mode="plan-only",
            summary="A bounded plan.",
            scope=["one repository"],
            ordered_steps=[PlanStep(order=1, action="compare", outcome="Record evidence")],
            required_controls=[],
            rollback=["No mutation occurred"],
            blockers=[],
        )
        payload = {
            "objective": "compare safely",
            "manifest": {
                "repositories": ["private-owner/private-repository"],
                "branches": ["private/customer-branch"],
                "languages": ["Python"],
                "proposed_actions": ["compare_branches", "deploy_production"],
            },
        }
        with patch(
            "helios_deployment_agent.main.run_plan", new=AsyncMock(return_value=plan)
        ) as mocked:
            response = await self.client.post(
                "/api/v1/plans",
                headers={
                    "Authorization": "Bearer unit-test-token",
                    "X-Correlation-ID": "test-correlation-1",
                },
                json=payload,
            )
        self.assertEqual(200, response.status_code)
        self.assertEqual("test-correlation-1", response.headers["X-Correlation-ID"])
        prompt = mocked.await_args.args[0]
        self.assertNotIn("private-owner", prompt)
        self.assertNotIn("private/customer-branch", prompt)
        verdicts = response.json()["authoritative_verdicts"]
        self.assertEqual(["allow_plan", "requires_controls"], [v["decision"] for v in verdicts])
        self.assertTrue(all(not verdict["execution_permitted"] for verdict in verdicts))


if __name__ == "__main__":
    unittest.main()
