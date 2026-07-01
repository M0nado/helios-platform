from __future__ import annotations

import asyncio
import logging
import uuid
from dataclasses import dataclass, field
from typing import Optional

import numpy as np

from .routing_policy import ContextualBanditRouter

logger = logging.getLogger("hermes.orchestrator")


@dataclass
class HermesTask:
    task_id: str = field(default_factory=lambda: str(uuid.uuid4()))
    task_type: str = "inference"
    payload: dict = field(default_factory=dict)
    priority: int = 5
    timeout_seconds: int = 60


@dataclass
class DispatchResult:
    task_id: str
    node_id: str
    status: str
    error: Optional[str] = None


class HermesOrchestrator:
    """
    Python integration of the pasted fleet orchestration design.
    """

    def __init__(self, policy: ContextualBanditRouter):
        self.policy = policy
        self.node_registry: dict[str, dict] = {}
        self.task_queue: asyncio.Queue[HermesTask] = asyncio.Queue()
        self.running = False

    def register_node(self, node_id: str, endpoint: str, capabilities: dict) -> None:
        self.node_registry[node_id] = {
            "endpoint": endpoint,
            "capabilities": capabilities,
            "healthy": True,
        }

    async def dispatch_task(self, task: HermesTask) -> DispatchResult:
        context = np.zeros(self.policy.d, dtype=np.float64)
        context[0] = {"inference": 0, "training": 1, "feature_eng": 2}.get(task.task_type, 0)
        context[1] = task.priority / 10.0
        arm, confidence = self.policy.select_arm(context)
        node_id = arm.split("_")[0] + "_" + arm.split("_")[1] if "_" in arm else arm
        if node_id not in self.node_registry:
            return DispatchResult(task_id=task.task_id, node_id=node_id, status="failed", error="node not registered")
        logger.info("Dispatched task=%s arm=%s confidence=%.4f", task.task_id, arm, confidence)
        self.policy.update(arm, context, reward=0.1)
        return DispatchResult(task_id=task.task_id, node_id=node_id, status="success")

    async def run_orchestration_loop(self) -> None:
        self.running = True
        while self.running:
            task = await self.task_queue.get()
            try:
                await self.dispatch_task(task)
            except Exception as exc:  # noqa: BLE001
                logger.exception("Task dispatch failed: %s", exc)

