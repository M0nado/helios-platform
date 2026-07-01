YAML: correlation config block
correlation:
  pearson_threshold:  0.20
  spearman_threshold: 0.15
  mi_threshold:       0.10
  p_value_cutoff:     0.05
  max_secondary_vars: 20
  lasso_alpha_range:  [0.001, 0.01, 0.1]
Python pseudo-code:
# correlation_search.py — Hermes XCore Correlation and Secondary Variable Search
# Pseudo-code: review and implement before use.

from __future__ import annotations
import logging
import json
from dataclasses import dataclass
from typing import Optional

import numpy as np
import pandas as pd
from scipy import stats as sp_stats
from sklearn.feature_selection import mutual_info_regression
from sklearn.linear_model import LassoCV

logger = logging.getLogger("hermes.correlation_search")

@dataclass
class SecondaryVar:
    var_name:          str
    correlation_score: float       # composite score
    p_value:           float
    method:            str         # "pearson" | "spearman" | "mi" | "partial"
    interaction_chain: list        # ordered chain of variable interactions

class CorrelationSearch:
    """
    Discovers secondary variables correlated with target that are
    not currently in the model. Uses multiple statistical methods
    and ranks by composite significance. All computations are performed
    ONLY on the training split to prevent data leakage.
    """

    def __init__(self, db_conn, threshold: float = 0.15, p_threshold: float = 0.05):
        self.db_conn     = db_conn
        self.threshold   = threshold
        self.p_threshold = p_threshold
        logger.info("CorrelationSearch initialized (threshold=%.2f, p=%.2f)",
                    threshold, p_threshold)

    def compute_pearson_matrix(self, df: pd.DataFrame) -> pd.DataFrame:
        """Pearson r for all numeric column pairs. Returns (n_cols x n_cols) DataFrame."""
        return df.corr(method="pearson")

    def compute_spearman_matrix(self, df: pd.DataFrame) -> pd.DataFrame:
        """Spearman rank correlation for non-normal or ordinal features."""
        return df.corr(method="spearman")

    def compute_mutual_information(self, df: pd.DataFrame) -> pd.DataFrame:
        """
        Pairwise mutual information for mixed-type features.
        For each column pair, estimate MI using sklearn.
        Returns DataFrame with columns: [feature_a, feature_b, mi_score].
        """
        numeric_cols = df.select_dtypes(include="number").columns.tolist()
        records = []
        for i, col_a in enumerate(numeric_cols):
            X = df[numeric_cols].drop(columns=[col_a]).values
            y = df[col_a].values
            mi_scores = mutual_info_regression(X, y, random_state=42)
            for j, col_b in enumerate([c for c in numeric_cols if c != col_a]):
                records.append({"feature_a": col_a, "feature_b": col_b, "mi_score": mi_scores[j]})
        return pd.DataFrame(records)

    def search_secondary_variables(
        self, target: str, feature_df: pd.DataFrame
    ) -> list[SecondaryVar]:
        """
        Finds variables correlated with target that are not currently
        in the model. Uses LASSO path to identify important interactions.
        Operates ONLY on training split — caller is responsible for split.
        """
        if target not in feature_df.columns:
            raise ValueError(f"Target '{target}' not found in feature_df columns.")

        candidates = [c for c in feature_df.select_dtypes(include="number").columns if c != target]
        y = feature_df[target].values
        X = feature_df[candidates].fillna(0).values

        # LASSO path for variable importance
        lasso = LassoCV(alphas=[0.001, 0.01, 0.1], cv=5, max_iter=5000, random_state=42)
        lasso.fit(X, y)
        lasso_selected = [candidates[i] for i, c in enumerate(lasso.coef_) if abs(c) > 1e-6]
        logger.info("LASSO selected %d candidates: %s", len(lasso_selected), lasso_selected)

        results = []
        for var in lasso_selected:
            r, p   = sp_stats.pearsonr(feature_df[var].fillna(0), feature_df[target].fillna(0))
            rho, _ = sp_stats.spearmanr(feature_df[var].fillna(0), feature_df[target].fillna(0))
            if abs(r) >= self.threshold and p <= data-id="174" self.p_threshold:
                results.append(SecondaryVar(
                    var_name          = var,
                    correlation_score = float(abs(r)),
                    p_value           = float(p),
                    method            = "pearson+lasso",
                    interaction_chain = [var, target]
                ))
        return results

    def compute_partial_correlations(
        self, df: pd.DataFrame, target: str, candidates: list[str]
    ) -> pd.DataFrame:
        """
        Computes partial correlations between each candidate and target,
        controlling for all other candidates (OLS residuals method).
        Returns DataFrame: [var, partial_r, p_value].
        """
        from sklearn.linear_model import LinearRegression
        records = []
        y = df[target].fillna(0).values
        for var in candidates:
            controls = [c for c in candidates if c != var]
            if not controls:
                continue
            Xc = df[controls].fillna(0).values
            # Residualize target
            lm_y = LinearRegression().fit(Xc, y)
            res_y = y - lm_y.predict(Xc)
            # Residualize candidate
            xv = df[var].fillna(0).values
            lm_x = LinearRegression().fit(Xc, xv)
            res_x = xv - lm_x.predict(Xc)
            r, p = sp_stats.pearsonr(res_x, res_y)
            records.append({"var": var, "partial_r": r, "p_value": p})
        return pd.DataFrame(records)

    def rank_and_filter(self, results: list[SecondaryVar]) -> list[SecondaryVar]:
        """
        Ranks by composite score (MI + Spearman + partial r),
        filters by significance, caps at max_secondary_vars.
        """
        filtered = [v for v in results if v.p_value <= data-id="175" self.p_threshold
                    and v.correlation_score >= self.threshold]
        filtered.sort(key=lambda v: v.correlation_score, reverse=True)
        return filtered[:20]  # max_secondary_vars from config

    def persist_findings(self, vars: list[SecondaryVar], run_id: str):
        """Writes findings to hermes_secondary_vars and hermes_feature_log."""
        cursor = self.db_conn.cursor()
        for var in vars:
            cursor.execute(
                """
                INSERT INTO hermes_secondary_vars
                    (var_name, correlation_chain, significance_score, included_in_model)
                VALUES (%s, %s, %s, %s)
                """,
                (var.var_name, json.dumps(var.interaction_chain),
                 var.correlation_score, False)
            )
            cursor.execute(
                """
                INSERT INTO hermes_feature_log
                    (run_id, feature_name, feature_type, correlation_score, p_value)
                VALUES (%s, %s, %s, %s, %s)
                """,
                (run_id, var.var_name, "numeric", var.correlation_score, var.p_value)
            )
        self.db_conn.commit()
        logger.info("Persisted %d secondary variable findings.", len(vars))

    def generate_correlation_report(self, vars: list[SecondaryVar]) -> dict:
        """Returns JSON-serializable report of all secondary variable findings."""
        return {
            "secondary_variables": [
                {
                    "var_name":          v.var_name,
                    "correlation_score": v.correlation_score,
                    "p_value":           v.p_value,
                    "method":            v.method,
                    "interaction_chain": v.interaction_chain
                }
                for v in vars
            ],
            "total_found": len(vars)
        }
Data Leakage Prevention
All correlation and MI computations in CorrelationSearch must be performed exclusively on the training split of data. Never pass the full dataset including validation or test rows. The caller (ContinuousRetrainingOrchestrator.prepare_training_data()) is responsible for enforcing the train/test split before calling any CorrelationSearch method.
2.4 — Autoencoder Shape and Compression Pipeline
YAML: autoencoder config block
autoencoder:
  input_dim:                 1024
  latent_dim:                128
  hidden_dims:               [512, 256]
  dropout:                   0.2
  batch_size:                512
  epochs:                    100
  learning_rate:             0.001
  weight_decay:              0.0001
  early_stopping_patience:   10
  device:                    cuda
  checkpoint_dir:            D:/DevDrive/ai-hub/checkpoints
