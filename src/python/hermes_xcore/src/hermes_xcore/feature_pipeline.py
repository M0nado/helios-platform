from __future__ import annotations

import logging
from dataclasses import dataclass, field
from typing import Optional

import numpy as np
import pandas as pd

logger = logging.getLogger("hermes.feature_pipeline")


@dataclass
class FeatureSet:
    numeric_features: pd.DataFrame
    text_embeddings: Optional[np.ndarray]
    categorical_features: pd.DataFrame
    metadata: dict = field(default_factory=dict)
    run_id: str = ""


class FeaturePipeline:
    """
    Python integration of the pasted Hermes XCore feature extraction design.
    """

    def __init__(self, config: dict):
        self.config = config

    def extract_raw(self, input_batch: pd.DataFrame) -> dict:
        text_cols = self.config.get("text_columns", [])
        categorical_cols = self.config.get("categorical_columns", [])
        numeric_cols = [
            c
            for c in input_batch.columns
            if c not in text_cols + categorical_cols and pd.api.types.is_numeric_dtype(input_batch[c])
        ]
        return {
            "numeric": input_batch[numeric_cols],
            "text": input_batch[text_cols] if text_cols else pd.DataFrame(index=input_batch.index),
            "categorical": input_batch[categorical_cols]
            if categorical_cols
            else pd.DataFrame(index=input_batch.index),
        }

    def compute_statistics(self, features: dict) -> pd.DataFrame:
        numeric_df = features.get("numeric", pd.DataFrame())
        rows: list[dict] = []
        for col in numeric_df.columns:
            series = numeric_df[col].dropna()
            rows.append(
                {
                    "feature_name": col,
                    "feature_type": "numeric",
                    "mean": float(series.mean()) if len(series) else 0.0,
                    "std": float(series.std()) if len(series) else 0.0,
                    "null_rate": float(numeric_df[col].isnull().mean()),
                }
            )
        return pd.DataFrame(rows)

    def build_feature_set(self, input_batch: pd.DataFrame, run_id: str) -> FeatureSet:
        raw = self.extract_raw(input_batch)
        stats = self.compute_statistics(raw)
        feature_set = FeatureSet(
            numeric_features=raw["numeric"],
            text_embeddings=None,
            categorical_features=raw["categorical"],
            metadata={"statistics": stats.to_dict(orient="records")},
            run_id=run_id,
        )
        logger.info("Built FeatureSet run_id=%s rows=%d", run_id, len(input_batch))
        return feature_set

