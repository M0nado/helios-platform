# routing_policy.py — Hermes XCore Contextual Bandit Routing Policy
# LinUCB algorithm with MAML-style meta-learning wrapper.
# Pseudo-code: review and implement before use.

from __future__ import annotations
import json
import logging
import uuid
from dataclasses import dataclass
from typing import Optional

import numpy as np
import httpx

logger = logging.getLogger("hermes.routing_policy")

@dataclass
class TaskEpisode:
    support_contexts: np.ndarray   # (K, context_dim)
    support_rewards:  np.ndarray   # (K,)
    arm_selected:     list[str]

@dataclass
class Experience:
    context: np.ndarray
    arm:     str
    reward:  float

# -----------------------------------------------------------
# ContextualBanditRouter (LinUCB)
# -----------------------------------------------------------
class ContextualBanditRouter:
    """
    LinUCB contextual bandit. Maintains per-arm ridge regression
    parameters (A matrix, b vector) for upper confidence bound selection.
    Sherman-Morrison rank-1 update for efficient online learning.
    """

    def __init__(
        self,
        arms:        list[str],
        context_dim: int,
        db_conn,
        qdrant_client,
        alpha:       float = 1.0
    ):
        self.arms        = arms
        self.d           = context_dim
        self.alpha       = alpha
        self.db_conn     = db_conn
        self.qdrant      = qdrant_client
        # Per-arm parameters
        self.A      = {arm: np.eye(self.d, dtype=np.float64)        for arm in arms}
        self.A_inv  = {arm: np.eye(self.d, dtype=np.float64)        for arm in arms}
        self.b      = {arm: np.zeros((self.d, 1), dtype=np.float64) for arm in arms}
        logger.info("ContextualBanditRouter initialized: %d arms, context_dim=%d", len(arms), context_dim)

    def select_arm(self, context: np.ndarray) -> tuple[str, float]:
        """
        Computes UCB score for each arm:
            theta_hat = A^-1 @ b
            p_t(a) = theta_hat.T @ x + alpha * sqrt(x.T @ A^-1 @ x)
        Returns (arm_name, confidence).
        """
        x = context.reshape(-1, 1).astype(np.float64)
        ucb_scores = {}
        for arm in self.arms:
            theta_hat  = self.A_inv[arm] @ self.b[arm]
            mean       = float(theta_hat.T @ x)
            variance   = float(x.T @ self.A_inv[arm] @ x)
            ucb_scores[arm] = mean + self.alpha * np.sqrt(max(variance, 0.0))
        best_arm    = max(ucb_scores, key=ucb_scores.__getitem__)
        confidence  = float(ucb_scores[best_arm])
        logger.debug("UCB selected arm=%s confidence=%.4f", best_arm, confidence)
        return best_arm, confidence

    def update(self, arm: str, context: np.ndarray, reward: float):
        """
        Online update using Sherman-Morrison rank-1 formula for A_inv.
        A <- data-id="192" A + x x^T
        b <- b + r x
        A_inv <- A_inv - (A_inv x)(A_inv x)^T / (1 + x^T A_inv x)
        """
        x   = context.reshape(-1, 1).astype(np.float64)
        Ax  = self.A_inv[arm] @ x
        denom = 1.0 + float(x.T @ Ax)
        self.A_inv[arm] -= (Ax @ Ax.T) / denom
        self.b[arm]     += reward * x
        logger.debug("Updated arm=%s reward=%.4f", arm, reward)

    def log_decision(
        self, session_id: str, context: np.ndarray,
        arm: str, confidence: float, reward: Optional[float] = None
    ):
        cursor = self.db_conn.cursor()
        cursor.execute(
            """
            INSERT INTO hermes_routing_decisions
                (session_id, input_context, selected_arm, confidence, actual_reward)
            VALUES (%s, %s, %s, %s, %s)
            """,
            (session_id, json.dumps(context.tolist()), arm, confidence, reward)
        )
        self.db_conn.commit()

    def save_policy(self, path: str):
        state = {
            "arms":  self.arms,
            "alpha": self.alpha,
            "d":     self.d,
            "A_inv": {k: v.tolist() for k, v in self.A_inv.items()},
            "b":     {k: v.tolist() for k, v in self.b.items()}
        }
        with open(path, "w") as f:
            json.dump(state, f, indent=2)
        logger.info("Policy saved to %s", path)

    def load_policy(self, path: str):
        with open(path) as f:
            state = json.load(f)
        self.arms   = state["arms"]
        self.alpha  = state["alpha"]
        self.d      = state["d"]
        self.A_inv  = {k: np.array(v) for k, v in state["A_inv"].items()}
        self.b      = {k: np.array(v) for k, v in state["b"].items()}
        logger.info("Policy loaded from %s", path)

    def export_for_deployment(self) -> dict:
        return {
            "arms":  self.arms,
            "alpha": self.alpha,
            "d":     self.d,
            "A_inv": {k: v.tolist() for k, v in self.A_inv.items()},
            "b":     {k: v.tolist() for k, v in self.b.items()}
        }

def deploy_policy(policy_dict: dict, node_endpoints: list[str]):
    """
    POST updated policy to each node's /policy/update endpoint.
    Rolls back if any node returns non-200.
    """
    successful = []
    for endpoint in node_endpoints:
        url = f"{endpoint}/policy/update"
        try:
            r = httpx.post(url, json=policy_dict, timeout=10.0)
            r.raise_for_status()
            successful.append(endpoint)
            logger.info("Policy deployed to %s", endpoint)
        except Exception as e:
            logger.error("Policy deployment FAILED for %s: %s", endpoint, e)
            logger.warning("Rolling back successfully deployed nodes: %s", successful)
            # Rollback: POST previous policy (caller should pass previous_policy_dict)
            raise RuntimeError(f"Policy deployment failed at {endpoint}. Rollback required.") from e

# -----------------------------------------------------------
# MetaLearningWrapper (MAML-style)
# -----------------------------------------------------------
class MetaLearningWrapper:
    """
    Wraps ContextualBanditRouter with MAML-style fast adaptation.
    Inner loop: few-step gradient on support set.
    Outer loop: meta-gradient over task batch.
    """

    def __init__(self, base_router: ContextualBanditRouter, inner_lr=0.01, outer_lr=0.001):
        self.base_router = base_router
        self.inner_lr    = inner_lr
        self.outer_lr    = outer_lr

    def meta_update(self, task_batch: list[TaskEpisode]):
        """
        For each task episode: compute fast-adapted policy params,
        evaluate on query set, accumulate meta-gradient, apply outer update.
        (Simplified placeholder — full MAML requires differentiable inner loop.)
        """
        meta_grads = {arm: np.zeros_like(self.base_router.b[arm])
                      for arm in self.base_router.arms}
        for episode in task_batch:
            for i, arm in enumerate(episode.arm_selected):
                ctx    = episode.support_contexts[i]
                reward = episode.support_rewards[i]
                meta_grads[arm] += self.outer_lr * reward * ctx.reshape(-1, 1)
        for arm in self.base_router.arms:
            self.base_router.b[arm] += meta_grads[arm]
        logger.info("Meta-update applied over %d task episodes.", len(task_batch))

    def adapt(self, support_set: list[Experience]) -> ContextualBanditRouter:
        """Few-shot adapts a copy of the base policy to the support set distribution."""
        import copy
        adapted = copy.deepcopy(self.base_router)
        for exp in support_set:
            adapted.update(exp.arm, exp.context, exp.reward)
        logger.info("Policy adapted on %d support experiences.", len(support_set))
        return adapted
2.6 — Continuous Retraining Loop
YAML: retraining config block
retraining:
  drift_psi_threshold:       0.20
  reward_decline_threshold:  0.10
  schedule_interval_hours:   24
  lookback_days:             30
  champion_challenger_margin: 0.02
  max_concurrent_jobs:       2
  training_backend:          wsl2
  gpu_passthrough:           true
  model_checkpoint_dir:      D:/DevDrive/ai-hub/checkpoints
  rollback_enabled:          true
  rollback_versions_to_keep: 3
