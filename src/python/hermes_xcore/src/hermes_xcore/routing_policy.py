from __future__ import annotations

import json
import logging
from dataclasses import dataclass
from pathlib import Path
from typing import Optional

import numpy as np

logger = logging.getLogger("hermes.routing_policy")


@dataclass
class Experience:
    context: np.ndarray
    arm: str
    reward: float


class ContextualBanditRouter:
    """
    LinUCB routing policy integrated from the pasted Hermes XCore design.
    """

    def __init__(self, arms: list[str], context_dim: int, alpha: float = 1.0):
        self.arms = arms
        self.d = context_dim
        self.alpha = alpha
        self.A_inv = {arm: np.eye(self.d, dtype=np.float64) for arm in arms}
        self.b = {arm: np.zeros((self.d, 1), dtype=np.float64) for arm in arms}

    def select_arm(self, context: np.ndarray) -> tuple[str, float]:
        x = context.reshape(-1, 1).astype(np.float64)
        ucb_scores: dict[str, float] = {}
        for arm in self.arms:
            theta_hat = self.A_inv[arm] @ self.b[arm]
            mean = float(theta_hat.T @ x)
            variance = float(x.T @ self.A_inv[arm] @ x)
            ucb_scores[arm] = mean + self.alpha * np.sqrt(max(variance, 0.0))
        best_arm = max(ucb_scores, key=ucb_scores.__getitem__)
        return best_arm, float(ucb_scores[best_arm])

    def update(self, arm: str, context: np.ndarray, reward: float) -> None:
        x = context.reshape(-1, 1).astype(np.float64)
        Ax = self.A_inv[arm] @ x
        denom = 1.0 + float(x.T @ Ax)
        self.A_inv[arm] -= (Ax @ Ax.T) / denom
        self.b[arm] += reward * x

    def save(self, path: str | Path) -> None:
        serializable = {
            "arms": self.arms,
            "context_dim": self.d,
            "alpha": self.alpha,
            "A_inv": {k: v.tolist() for k, v in self.A_inv.items()},
            "b": {k: v.tolist() for k, v in self.b.items()},
        }
        Path(path).write_text(json.dumps(serializable, indent=2), encoding="utf-8")
        logger.info("Routing policy saved: %s", path)

    @classmethod
    def load(cls, path: str | Path) -> "ContextualBanditRouter":
        data = json.loads(Path(path).read_text(encoding="utf-8"))
        router = cls(arms=data["arms"], context_dim=data["context_dim"], alpha=data["alpha"])
        router.A_inv = {k: np.array(v, dtype=np.float64) for k, v in data["A_inv"].items()}
        router.b = {k: np.array(v, dtype=np.float64) for k, v in data["b"].items()}
        return router


def reward_from_status(status: str, latency_ms: Optional[float] = None) -> float:
    base = 1.0 if status == "success" else -1.0
    if latency_ms is None:
        return base
    penalty = min(latency_ms / 5000.0, 1.0)
    return float(max(-1.0, min(1.0, base - penalty)))

