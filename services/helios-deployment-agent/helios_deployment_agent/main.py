"""HTTP entry point for the HELIOS deployment planning service."""

from __future__ import annotations

import json
import logging
import os
from typing import Any

from fastapi import FastAPI, HTTPException
from pydantic import BaseModel, Field

from . import __version__
from .agent import run_plan
from .policy import MAX_MANIFEST_BYTES, contains_sensitive_key

LOGGER = logging.getLogger("helios.deployment_agent")

app = FastAPI(
    title="HELIOS Deployment Planner",
    version=__version__,
    description="Plan-only OpenAI agent protected by deterministic action policy.",
)


class PlanRequest(BaseModel):
    objective: str = Field(min_length=3, max_length=4_000)
    manifest: dict[str, Any] = Field(default_factory=dict)
    controls: dict[str, bool] = Field(default_factory=dict)


class PlanResponse(BaseModel):
    execution_mode: str
    plan: str


@app.get("/health")
async def health() -> dict[str, Any]:
    return {
        "status": "ok",
        "version": __version__,
        "execution_mode": "plan-only",
        "openai_configured": bool(os.getenv("OPENAI_API_KEY")),
    }


@app.post("/plan", response_model=PlanResponse)
async def plan(request: PlanRequest) -> PlanResponse:
    manifest_json = json.dumps(request.manifest, sort_keys=True)
    if len(manifest_json.encode("utf-8")) > MAX_MANIFEST_BYTES:
        raise HTTPException(status_code=413, detail="Manifest is too large")
    if contains_sensitive_key(request.manifest):
        raise HTTPException(status_code=400, detail="Sensitive manifest fields are not accepted")
    payload = {
        "objective": request.objective,
        "manifest": request.manifest,
        "controls": request.controls,
    }
    prompt = (
        "Create a governed implementation plan for this request. First inspect the manifest, then "
        "evaluate every proposed action. Return scope, ordered steps, evidence, controls, rollback, "
        "and explicit blockers. Input JSON follows:\n" + json.dumps(payload, sort_keys=True)
    )
    try:
        output = await run_plan(prompt)
    except Exception as exc:  # the response intentionally omits provider details
        LOGGER.warning("planning request failed: %s", type(exc).__name__)
        raise HTTPException(status_code=503, detail="Planning service unavailable") from None
    return PlanResponse(execution_mode="plan-only", plan=output)
