# scoring_feedback.py — Hermes XCore Scoring and Feedback Ingestion
# Pseudo-code: review and implement before use.

from __future__ import annotations
import hashlib
import json
import logging
from dataclasses import dataclass
from typing import Optional

import numpy as np

from feature_pipeline import FeatureSet

logger = logging.getLogger("hermes.scoring_feedback")

@dataclass
class ScoringResult:
    model_id:    str
    output:      dict
    confidence:  float
    input_hash:  str
    output_hash: str

@dataclass
class FeedbackStats:
    mean_reward_per_model: dict[str, float]
    mean_reward_per_arm:   dict[str, float]
    total_feedback_count:  int
    lookback_hours:        int

class HermesScorer:
    def __init__(self, model_registry, device: str = "cuda"):
        self.model_registry = model_registry
        self.device         = device
        self._cache         = {}

    def _load_model(self, model_id: str):
        if model_id not in self._cache:
            artifact_path = self.model_registry.get_artifact_path(model_id)
            import torch
            model = torch.load(artifact_path, map_location=self.device)
            model.eval()
            self._cache[model_id] = model
        return self._cache[model_id]

    def hash_io(self, input_features: FeatureSet, output: dict) -> tuple[str, str]:
        """SHA-256 of serialized input and output for audit trail."""
        in_bytes  = json.dumps(
            input_features.numeric_features.to_dict(), sort_keys=True
        ).encode()
        out_bytes = json.dumps(output, sort_keys=True).encode()
        in_hash   = hashlib.sha256(in_bytes).hexdigest()
        out_hash  = hashlib.sha256(out_bytes).hexdigest()
        return in_hash, out_hash

    def score(self, input_features: FeatureSet, model_id: str) -> ScoringResult:
        import torch
        model = self._load_model(model_id)
        x     = torch.tensor(
            input_features.numeric_features.fillna(0).values,
            dtype=torch.float32
        ).to(self.device)
        with torch.no_grad():
            raw_output = model(x)
        output     = {"predictions": raw_output.cpu().numpy().tolist()}
        confidence = float(raw_output.softmax(dim=-1).max().item()) if raw_output.ndim > 1 else 1.0
        in_h, out_h = self.hash_io(input_features, output)
        return ScoringResult(
            model_id   = model_id,
            output     = output,
            confidence = confidence,
            input_hash = in_h,
            output_hash= out_h
        )

    def batch_score(
        self, batch: list[FeatureSet], model_id: str
    ) -> list[ScoringResult]:
        return [self.score(fs, model_id) for fs in batch]

class FeedbackIngester:
    def __init__(self, db_conn, bandit_router):
        self.db_conn      = db_conn
        self.bandit_router = bandit_router

    def validate_reward(self, reward: float) -> bool:
        import math
        return isinstance(reward, (int, float)) and not math.isnan(reward) and -1.0 <= data-id="201" reward <= 1.0

    def ingest(
        self,
        session_id:     str,
        model_id:       str,
        input_hash:     str,
        output_hash:    str,
        reward_signal:  float,
        context_vector: list
    ):
        if not self.validate_reward(reward_signal):
            raise ValueError(f"Invalid reward signal: {reward_signal}")
        cursor = self.db_conn.cursor()
        cursor.execute(
            """
            INSERT INTO hermes_feedback_log
                (session_id, model_id, input_hash, output_hash, reward_signal, context_vector)
            VALUES (%s, %s, %s, %s, %s, %s)
            """,
            (session_id, model_id, input_hash, output_hash,
             reward_signal, json.dumps(context_vector))
        )
        self.db_conn.commit()
        logger.info("Feedback ingested: session=%s reward=%.4f", session_id, reward_signal)

    def aggregate_feedback(self, lookback_hours: int = 24) -> FeedbackStats:
        cursor = self.db_conn.cursor()
        cursor.execute(
            """
            SELECT model_id, AVG(reward_signal), COUNT(*)
            FROM hermes_feedback_log
            WHERE created_at >= NOW() - INTERVAL '%s hours'
            GROUP BY model_id
            """,
            (lookback_hours,)
        )
        rows = cursor.fetchall()
        mean_by_model = {str(r[0]): float(r[1]) for r in rows}
        cursor.execute(
            """
            SELECT rd.selected_arm, AVG(fb.reward_signal)
            FROM hermes_routing_decisions rd
            JOIN hermes_feedback_log fb ON rd.session_id = fb.session_id
            WHERE rd.ts >= NOW() - INTERVAL '%s hours'
            GROUP BY rd.selected_arm
            """,
            (lookback_hours,)
        )
        arm_rows = cursor.fetchall()
        mean_by_arm = {r[0]: float(r[1]) for r in arm_rows}
        return FeedbackStats(
            mean_reward_per_model = mean_by_model,
            mean_reward_per_arm   = mean_by_arm,
            total_feedback_count  = sum(int(r[2]) for r in rows),
            lookback_hours        = lookback_hours
        )

    def trigger_policy_update(self, stats: FeedbackStats):
        """
        Calls ContextualBanditRouter.update() for each recent arm decision
        with its resolved reward signal.
        """
        cursor = self.db_conn.cursor()
        cursor.execute(
            """
            SELECT rd.selected_arm, fb.reward_signal, fb.context_vector
            FROM hermes_routing_decisions rd
            JOIN hermes_feedback_log fb ON rd.session_id = fb.session_id
            WHERE rd.actual_reward IS NULL
            LIMIT 500
            """
        )
        rows = cursor.fetchall()
        for arm, reward, ctx_json in rows:
            ctx = np.array(json.loads(ctx_json))
            self.bandit_router.update(arm, ctx, reward)
        logger.info("Policy update triggered for %d unresolved decisions.", len(rows))
2.8 — Vector DB Indexing and Retrieval (Qdrant)
YAML: Qdrant collection schema — embeddings_v1
collection: embeddings_v1
vectors:
  size:     128
  distance: Cosine
payload_schema:
  run_id:        keyword
  node_id:       keyword
  feature_type:  keyword
  created_at:    datetime
  model_version: keyword
