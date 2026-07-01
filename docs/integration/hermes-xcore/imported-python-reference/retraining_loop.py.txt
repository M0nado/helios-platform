# retraining_loop.py — Hermes XCore Continuous Retraining Orchestrator
# Pseudo-code: review and implement before use.

from __future__ import annotations
import logging
import signal
import subprocess
import time
from dataclasses import dataclass
from typing import Optional

import numpy as np
import pandas as pd

logger = logging.getLogger("hermes.retraining_loop")

@dataclass
class DriftReport:
    feature_psi_scores:   dict[str, float]
    categorical_chi2:     dict[str, float]
    embedding_drift:      float
    overall_drift_detected: bool

@dataclass
class DataSplit:
    X_train: pd.DataFrame
    X_val:   pd.DataFrame
    X_test:  pd.DataFrame
    y_train: pd.Series
    y_val:   pd.Series
    y_test:  pd.Series

@dataclass
class TrainingResult:
    model_id:      str
    artifact_path: str
    train_loss:    float
    val_loss:      float
    eval_score:    float

@dataclass
class EvalReport:
    champion_score:   float
    challenger_score: float
    improvement:      float
    promote:          bool

class ContinuousRetrainingOrchestrator:
    """
    Monitors for data drift and reward decline; triggers retraining
    when conditions are met; promotes or rejects challenger models.
    """

    def __init__(self, config: dict, db_conn, model_registry):
        self.config         = config
        self.db_conn        = db_conn
        self.model_registry = model_registry
        self._shutdown      = False
        signal.signal(signal.SIGTERM, self._handle_shutdown)
        signal.signal(signal.SIGINT,  self._handle_shutdown)
        logger.info("ContinuousRetrainingOrchestrator initialized.")

    def _handle_shutdown(self, signum, frame):
        logger.info("Shutdown signal received — completing current iteration then stopping.")
        self._shutdown = True

    def check_drift(
        self,
        new_data:        pd.DataFrame,
        reference_stats: dict
    ) -> DriftReport:
        """
        PSI for numeric features, Chi-square for categorical,
        cosine drift for text embeddings.
        PSI formula: PSI = sum((actual% - expected%) * ln(actual% / expected%))
        """
        psi_scores = {}
        for col, ref_dist in reference_stats.get("numeric", {}).items():
            if col not in new_data.columns:
                continue
            bins   = ref_dist["bins"]
            ref_pct = np.array(ref_dist["pct"]) + 1e-8
            act_counts, _ = np.histogram(new_data[col].dropna(), bins=bins)
            act_pct = act_counts / (act_counts.sum() + 1e-8) + 1e-8
            psi = float(np.sum((act_pct - ref_pct) * np.log(act_pct / ref_pct)))
            psi_scores[col] = psi

        chi2_scores = {}
        for col, ref_freq in reference_stats.get("categorical", {}).items():
            if col not in new_data.columns:
                continue
            from scipy.stats import chi2_contingency
            actual_freq = new_data[col].value_counts()
            cats = list(set(list(ref_freq.keys()) + list(actual_freq.index)))
            obs  = np.array([actual_freq.get(c, 0) for c in cats])
            exp  = np.array([ref_freq.get(c, 1e-6)  for c in cats])
            exp  = exp / exp.sum() * obs.sum()
            stat, p, *_ = chi2_contingency(np.vstack([obs, exp]))
            chi2_scores[col] = float(stat)

        emb_drift = reference_stats.get("embedding_cosine_drift", 0.0)
        psi_threshold = self.config.get("drift_psi_threshold", 0.20)
        drift_detected = any(v > psi_threshold for v in psi_scores.values())

        return DriftReport(
            feature_psi_scores     = psi_scores,
            categorical_chi2       = chi2_scores,
            embedding_drift        = emb_drift,
            overall_drift_detected = drift_detected
        )

    def trigger_condition(
        self, drift_report: DriftReport, feedback_stats: dict
    ) -> bool:
        psi_trigger    = drift_report.overall_drift_detected
        reward_trigger = feedback_stats.get("mean_reward", 1.0) < (
            1.0 - self.config.get("reward_decline_threshold", 0.10)
        )
        return psi_trigger or reward_trigger

    def prepare_training_data(self, lookback_days: int = 30) -> DataSplit:
        """
        Queries Postgres for recent features + feedback.
        Performs stratified train/val/test split (70/15/15).
        """
        from sklearn.model_selection import train_test_split
        cursor = self.db_conn.cursor()
        cursor.execute(
            """
            SELECT fl.feature_name, fl.correlation_score, fb.reward_signal
            FROM hermes_feature_log fl
            JOIN hermes_feedback_log fb ON fl.run_id = fb.session_id
            WHERE fl.created_at >= NOW() - INTERVAL '%s days'
            """,
            (lookback_days,)
        )
        rows = cursor.fetchall()
        df   = pd.DataFrame(rows, columns=["feature_name", "correlation_score", "reward"])
        X    = df.drop(columns=["reward"])
        y    = df["reward"]
        X_tr, X_tmp, y_tr, y_tmp = train_test_split(X, y, test_size=0.30, random_state=42)
        X_va, X_te, y_va, y_te   = train_test_split(X_tmp, y_tmp, test_size=0.50, random_state=42)
        return DataSplit(X_train=X_tr, X_val=X_va, X_test=X_te,
                         y_train=y_tr, y_val=y_va,   y_test=y_te)

    def run_training_job(
        self, data: DataSplit, model_config: dict
    ) -> TrainingResult:
        """
        Launches training subprocess in WSL2 or Hyper-V VM with GPU passthrough.
        Monitors exit code; returns TrainingResult on success.
        """
        training_script = "D:/DevDrive/hermes/scripts/train_job.py"
        backend = self.config.get("training_backend", "wsl2")
        if backend == "wsl2":
            cmd = ["wsl", "python3", training_script,
                   "--config", model_config.get("config_path", ""),
                   "--run-id", model_config.get("run_id", "")]
        else:
            cmd = ["powershell", "-Command",
                   f"Start-VM HermesTrainer; Invoke-Command ..."]  # Hyper-V variant
        result = subprocess.run(cmd, capture_output=True, text=True, timeout=7200)
        if result.returncode != 0:
            logger.error("Training job FAILED: %s", result.stderr)
            raise RuntimeError("Training job failed.")
        return TrainingResult(
            model_id      = model_config.get("run_id", ""),
            artifact_path = f"D:/DevDrive/ai-hub/checkpoints/{model_config.get('run_id','')}.pt",
            train_loss    = 0.0,  # parsed from stdout in real implementation
            val_loss      = 0.0,
            eval_score    = 0.0
        )

    def evaluate_candidate(self, result: TrainingResult) -> EvalReport:
        """
        Compares challenger to current champion on holdout set.
        Promotes if challenger beats champion by champion_challenger_margin.
        """
        cursor = self.db_conn.cursor()
        cursor.execute(
            "SELECT eval_score FROM hermes_model_registry WHERE is_active = TRUE LIMIT 1"
        )
        row = cursor.fetchone()
        champion_score   = row[0] if row else 0.0
        challenger_score = result.eval_score
        margin           = self.config.get("champion_challenger_margin", 0.02)
        improvement      = challenger_score - champion_score
        return EvalReport(
            champion_score   = champion_score,
            challenger_score = challenger_score,
            improvement      = improvement,
            promote          = improvement >= margin
        )

    def promote_or_reject(self, eval_report: EvalReport, result: TrainingResult):
        if eval_report.promote:
            cursor = self.db_conn.cursor()
            cursor.execute("UPDATE hermes_model_registry SET is_active=FALSE WHERE is_active=TRUE")
            cursor.execute(
                """
                UPDATE hermes_model_registry SET is_active=TRUE, deployed_at=NOW()
                WHERE model_id=%s
                """,
                (result.model_id,)
            )
            self.db_conn.commit()
            logger.info("Challenger PROMOTED: model_id=%s improvement=%.4f",
                        result.model_id, eval_report.improvement)
        else:
            logger.info("Challenger REJECTED: improvement=%.4f below margin=%.4f",
                        eval_report.improvement,
                        self.config.get("champion_challenger_margin", 0.02))

    def rollback(self, to_model_id: str):
        cursor = self.db_conn.cursor()
        cursor.execute("UPDATE hermes_model_registry SET is_active=FALSE WHERE is_active=TRUE")
        cursor.execute(
            "UPDATE hermes_model_registry SET is_active=TRUE WHERE model_id=%s",
            (to_model_id,)
        )
        self.db_conn.commit()
        logger.info("ROLLBACK: restored champion model_id=%s", to_model_id)

    def run_loop(self, interval_seconds: int = 3600):
        """Main scheduling loop. Runs until shutdown signal received."""
        logger.info("Retraining loop started. Interval: %ds", interval_seconds)
        while not self._shutdown:
            try:
                logger.info("--- Retraining loop tick ---")
                # (1) Pull reference stats from last known good run
                # (2) Fetch recent serving data
                # (3) Check drift + feedback
                # (4) If trigger: prepare data, run job, evaluate, promote/reject
                # Abbreviated here — full implementation follows module design above
            except Exception as e:
                logger.error("Retraining loop error: %s", e, exc_info=True)
            time.sleep(interval_seconds)
        logger.info("Retraining loop shut down gracefully.")
2.7 — Model Scoring and Feedback Ingestion
