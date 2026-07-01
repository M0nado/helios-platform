# feature_pipeline.py — Hermes XCore Feature Extraction Pipeline
# Pseudo-code: review and implement before use.

from __future__ import annotations
import hashlib
import logging
import warnings
from dataclasses import dataclass, field
from pathlib import Path
from typing import Optional

import numpy as np
import pandas as pd
import torch
from sentence_transformers import SentenceTransformer
from qdrant_client import QdrantClient

logger = logging.getLogger("hermes.feature_pipeline")

@dataclass
class FeatureSet:
    numeric_features:      pd.DataFrame
    text_embeddings:       Optional[np.ndarray]  # shape (N, 768)
    categorical_features:  pd.DataFrame
    metadata:              dict = field(default_factory=dict)
    run_id:                str  = ""

class FeaturePipeline:
    """
    Orchestrates raw feature extraction, statistics computation,
    text embedding, and DB logging for one input batch.
    """

    def __init__(self, config_path: str, db_conn, gpu_device: str = "cuda"):
        import yaml
        with open(config_path) as f:
            self.config = yaml.safe_load(f)
        self.db_conn   = db_conn
        self.device    = torch.device(gpu_device if torch.cuda.is_available() else "cpu")
        self.qdrant    = QdrantClient(
            host=self.config["qdrant"]["host"],
            port=self.config["qdrant"]["port"]
        )
        model_name = self.config.get("embedding_model", "all-MiniLM-L6-v2")
        self.embedder  = SentenceTransformer(model_name, device=str(self.device))
        logger.info("FeaturePipeline initialized on device=%s", self.device)

    def extract_raw(self, input_batch: pd.DataFrame) -> dict:
        """
        Splits input columns into numeric, text, and categorical buckets
        based on dtype inference and config column type mappings.
        """
        text_cols        = self.config.get("text_columns", [])
        categorical_cols = self.config.get("categorical_columns", [])
        numeric_cols     = [
            c for c in input_batch.columns
            if c not in text_cols + categorical_cols
               and pd.api.types.is_numeric_dtype(input_batch[c])
        ]
        return {
            "numeric":     input_batch[numeric_cols],
            "text":        input_batch[text_cols],
            "categorical": input_batch[categorical_cols]
        }

    def compute_statistics(self, features: dict) -> pd.DataFrame:
        """
        Computes descriptive statistics for numeric and categorical features.
        Numeric: mean, std, skew, kurtosis, p25, p50, p75.
        Categorical: mode, entropy (normalized), frequency table.
        """
        from scipy.stats import skew, kurtosis, entropy as sp_entropy

        rows = []
        numeric_df = features.get("numeric", pd.DataFrame())
        for col in numeric_df.columns:
            series = numeric_df[col].dropna()
            rows.append({
                "feature_name":  col,
                "feature_type":  "numeric",
                "mean":          series.mean(),
                "std":           series.std(),
                "skew":          skew(series),
                "kurtosis":      kurtosis(series),
                "p25":           series.quantile(0.25),
                "p50":           series.quantile(0.50),
                "p75":           series.quantile(0.75),
                "null_rate":     numeric_df[col].isnull().mean()
            })
        cat_df = features.get("categorical", pd.DataFrame())
        for col in cat_df.columns:
            series   = cat_df[col].dropna()
            vc       = series.value_counts(normalize=True)
            ent      = float(sp_entropy(vc.values, base=2)) if len(vc) > 1 else 0.0
            rows.append({
                "feature_name":  col,
                "feature_type":  "categorical",
                "entropy_bits":  ent,
                "top_category":  vc.index[0] if len(vc) > 0 else None,
                "n_unique":      int(vc.nunique()),
                "null_rate":     cat_df[col].isnull().mean()
            })
        return pd.DataFrame(rows)

    def embed_text_fields(self, texts: list[str]) -> np.ndarray:
        """
        Encodes list of text strings to (N, 768) embeddings using
        sentence-transformers on GPU; upserts into Qdrant.
        GPU OOM is caught and falls back to CPU.
        """
        if not texts:
            return np.empty((0, 768), dtype=np.float32)
        try:
            embeddings = self.embedder.encode(
                texts,
                batch_size=64,
                show_progress_bar=False,
                device=str(self.device),
                convert_to_numpy=True
            )
        except RuntimeError as e:
            if "CUDA out of memory" in str(e):
                warnings.warn("CUDA OOM in embed_text_fields — falling back to CPU.")
                logger.warning("GPU OOM: falling back to CPU for embedding step.")
                torch.cuda.empty_cache()
                embeddings = self.embedder.encode(
                    texts, batch_size=32, device="cpu", convert_to_numpy=True
                )
            else:
                raise
        finally:
            torch.cuda.empty_cache()
        return embeddings.astype(np.float32)

    def log_features_to_db(self, feature_df: pd.DataFrame, run_id: str):
        """
        Inserts one row per feature into hermes_feature_log.
        Uses batch INSERT for efficiency.
        """
        cursor = self.db_conn.cursor()
        rows = [
            (
                run_id,
                row.get("feature_name"),
                row.get("feature_type"),
                row.get("correlation_score"),
                row.get("p_value"),
                row.get("source_table")
            )
            for _, row in feature_df.iterrows()
        ]
        cursor.executemany(
            """
            INSERT INTO hermes_feature_log
                (run_id, feature_name, feature_type, correlation_score, p_value, source_table)
            VALUES (%s, %s, %s, %s, %s, %s)
            """,
            rows
        )
        self.db_conn.commit()
        logger.info("Logged %d features for run_id=%s", len(rows), run_id)

    def run(self, input_batch: pd.DataFrame, run_id: str = "") -> FeatureSet:
        """
        Master orchestration method. Extracts, stats, embeds, logs.
        Returns populated FeatureSet.
        """
        import uuid
        if not run_id:
            run_id = str(uuid.uuid4())
        logger.info("FeaturePipeline.run() start — run_id=%s, batch_rows=%d", run_id, len(input_batch))

        raw     = self.extract_raw(input_batch)
        stats   = self.compute_statistics(raw)

        text_cols = self.config.get("text_columns", [])
        texts = []
        if text_cols:
            for col in text_cols:
                if col in input_batch.columns:
                    texts.extend(input_batch[col].fillna("").tolist())
        embeddings = self.embed_text_fields(texts)

        self.log_features_to_db(stats, run_id)

        return FeatureSet(
            numeric_features     = raw["numeric"],
            text_embeddings      = embeddings if embeddings.size > 0 else None,
            categorical_features = raw["categorical"],
            metadata             = {"stats": stats.to_dict(orient="records")},
            run_id               = run_id
        )
2.3 — Correlation and Secondary Variable Search
