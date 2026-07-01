# orchestrator.py — Hermes XCore Orchestrator (Full Design)
# Pseudo-code: review and implement before use.

from __future__ import annotations
import asyncio
import logging
import uuid
from dataclasses import dataclass, field
from typing import Optional

import httpx

from routing_policy import ContextualBanditRouter

logger = logging.getLogger("hermes.orchestrator")

@dataclass
class HermesTask:
    task_id:         str = field(default_factory=lambda: str(uuid.uuid4()))
    task_type:       str = "inference"   # "inference" | "training" | "feature_eng"
    payload:         dict = field(default_factory=dict)
    priority:        int  = 5            # 1 (highest) to 10 (lowest)
    timeout_seconds: int  = 60

@dataclass
class DispatchResult:
    task_id:      str
    node_id:      str
    node_endpoint: str
    sent_at:      float

@dataclass
class TaskResult:
    task_id:   str
    node_id:   str
    status:    str          # "success" | "failed" | "timeout"
    output:    Optional[dict] = None
    error:     Optional[str]  = None

class HermesOrchestrator:
    """
    Central coordination point for the Hermes fleet.
    Maintains node registry, dispatches tasks via bandit policy,
    aggregates results, and triggers retraining on schedule.
    """

    def __init__(
        self,
        config_path:   str,
        db_conn,
        qdrant_client,
        policy:        ContextualBanditRouter
    ):
        import yaml
        with open(config_path) as f:
            self.config   = yaml.safe_load(f)
        self.db_conn       = db_conn
        self.qdrant        = qdrant_client
        self.policy        = policy
        self.node_registry = {}      # node_id -> {endpoint, capabilities, healthy}
        self.task_queue    = asyncio.Queue()
        self.running       = False

    def register_node(self, node_id: str, endpoint: str, capabilities: dict):
        self.node_registry[node_id] = {
            "endpoint":     endpoint,
            "capabilities": capabilities,
            "healthy":      True,
            "failure_count": 0
        }
        logger.info("Node registered: %s at %s (caps=%s)", node_id, endpoint, capabilities)

    async def health_check_all(self) -> dict[str, bool]:
        results = {}
        async with httpx.AsyncClient(timeout=5.0) as client:
            for node_id, info in self.node_registry.items():
                try:
                    r = await client.get(f"{info['endpoint']}/health")
                    healthy = r.status_code == 200
                except Exception:
                    healthy = False
                self.node_registry[node_id]["healthy"] = healthy
                results[node_id] = healthy
                if not healthy:
                    logger.warning("Node %s is UNHEALTHY", node_id)
        return results

    async def dispatch_task(self, task: HermesTask) -> DispatchResult:
        import numpy as np, time
        # Build context vector from task payload
        context = np.zeros(self.policy.d, dtype=np.float64)
        context[0] = {"inference": 0, "training": 1, "feature_eng": 2}.get(task.task_type, 0)
        context[1] = task.priority / 10.0
        arm, confidence = self.policy.select_arm(context)
        node_id  = arm.split("_inference")[0].split("_training")[0].split("_feature_eng")[0]
        node_info = self.node_registry.get(node_id, {})
        endpoint  = node_info.get("endpoint", "")
        async with httpx.AsyncClient(timeout=task.timeout_seconds) as client:
            r = await client.post(
                f"{endpoint}/v1/{task.task_type}",
                json={"task_id": task.task_id, "payload": task.payload}
            )
            r.raise_for_status()
        self.policy.log_decision(
            session_id = task.task_id,
            context    = context,
            arm        = arm,
            confidence = confidence
        )
        return DispatchResult(
            task_id       = task.task_id,
            node_id       = node_id,
            node_endpoint = endpoint,
            sent_at       = time.time()
        )

    async def handle_node_failure(self, node_id: str, task: HermesTask) -> DispatchResult:
        self.node_registry[node_id]["failure_count"] += 1
        self.node_registry[node_id]["healthy"] = False
        logger.error("Node %s FAILED for task %s — re-routing.", node_id, task.task_id)
        # Exclude failed node by zeroing its arm's confidence
        return await self.dispatch_task(task)

    async def run_orchestration_loop(self):
        self.running = True
        logger.info("Orchestration loop started.")
        while self.running:
            try:
                task = await asyncio.wait_for(self.task_queue.get(), timeout=5.0)
                try:
                    result = await self.dispatch_task(task)
                    logger.info("Task %s dispatched to node %s", task.task_id, result.node_id)
                except Exception as e:
                    logger.error("Dispatch error for task %s: %s", task.task_id, e)
            except asyncio.TimeoutError:
                await self.health_check_all()
Deliverable 3 — Security Mode Scripts (C / B / A)
3.1 — Security Philosophy and Mode Definitions
The Hermes workstation operates across three security modes. Mode selection is driven by operational context: full air-gap-like lockdown for sensitive data handling (Mode C), normal developer workflow with curated outbound access (Mode B), and full cloud integration for Azure/GitHub operations (Mode A). All three modes maintain local-only inbound access at all times — no RDP, WinRM, or inbound SSH is permitted under any mode.

Feature    Mode C — Locked    Mode B — Balanced (Default)    Mode A — Active
Outbound Network    Windows Update + Defender only    Curated allowlist (GitHub, Azure, PyPI, Docker)    Standard outbound (allow established)
Inbound Network    Blocked (all)    Localhost + WSL2 adapter only    Localhost only; no public inbound
WSL2    Shutdown + adapter disabled    Running, internal network only    Running, full localhost forwarding
Docker    Stopped; network=none    Running, internal bridge only    Running, bridge + localhost port exposure
Hyper-V VMs    Powered off or no NIC    HermesInternal vSwitch only    HermesNet (external) for Azure testing
BitLocker    TPM + PIN (startup PIN required)    TPM-only unlock    TPM-only unlock
Windows Sandbox    Disabled    Enabled (restricted folders)    Enabled (full config)
GitHub / Azure CLI    Tokens revoked and cleared    Authenticated, outbound only    Fully active
Purview / Entra    Offline (no cloud reach)    Read-only context cached    Fully active
Defender    Maximum protection + maximum audit    Default + enhanced real-time    Default protection
3.2 — Mode Toggle Script
