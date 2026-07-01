# vector_store.py — Hermes XCore Vector Store (Qdrant)
# Pseudo-code: review and implement before use.

from __future__ import annotations
import logging
import time
import uuid
from dataclasses import dataclass
from typing import Optional

import numpy as np
from qdrant_client import QdrantClient
from qdrant_client.models import (
    Distance, VectorParams, PointStruct,
    Filter, FieldCondition, MatchValue
)

logger = logging.getLogger("hermes.vector_store")

@dataclass
class SearchResult:
    id:      str
    score:   float
    payload: dict

class HermesVectorStore:
    """
    Manages all Qdrant interactions for the Hermes fleet:
    upsert, ANN search, soft delete, and collection management.
    """

    def __init__(self, qdrant_url: str, collection_name: str, vector_size: int):
        self.client          = QdrantClient(url=qdrant_url)
        self.collection_name = collection_name
        self.vector_size     = vector_size
        self.create_collection_if_missing(vector_size)

    def create_collection_if_missing(
        self, vector_size: int, distance_metric: str = "Cosine"
    ):
        existing = [c.name for c in self.client.get_collections().collections]
        if self.collection_name not in existing:
            dist = Distance.COSINE if distance_metric == "Cosine" else Distance.DOT
            self.client.create_collection(
                collection_name = self.collection_name,
                vectors_config  = VectorParams(size=vector_size, distance=dist)
            )
            logger.info("Created Qdrant collection '%s' (dim=%d, metric=%s)",
                        self.collection_name, vector_size, distance_metric)
        else:
            logger.debug("Collection '%s' already exists.", self.collection_name)

    def upsert_embeddings(
        self,
        ids:      list,
        vectors:  np.ndarray,
        payloads: list[dict],
        max_retries: int = 3
    ):
        """Batch upsert with exponential backoff retry on failure."""
        points = [
            PointStruct(
                id      = str(ids[i]),
                vector  = vectors[i].tolist(),
                payload = payloads[i]
            )
            for i in range(len(ids))
        ]
        for attempt in range(max_retries):
            try:
                self.client.upsert(collection_name=self.collection_name, points=points)
                logger.info("Upserted %d vectors into '%s'", len(points), self.collection_name)
                return
            except Exception as e:
                if attempt < max_retries - 1:
                    wait = 2 ** attempt
                    logger.warning("Upsert attempt %d failed (%s) — retrying in %ds", attempt+1, e, wait)
                    time.sleep(wait)
                else:
                    logger.error("Upsert FAILED after %d attempts: %s", max_retries, e)
                    raise

    def search(
        self,
        query_vector:   np.ndarray,
        top_k:          int = 10,
        filter_payload: Optional[dict] = None
    ) -> list[SearchResult]:
        query_filter = None
        if filter_payload:
            conditions = [
                FieldCondition(key=k, match=MatchValue(value=v))
                for k, v in filter_payload.items()
            ]
            query_filter = Filter(must=conditions)
        hits = self.client.search(
            collection_name = self.collection_name,
            query_vector    = query_vector.tolist(),
            limit           = top_k,
            query_filter    = query_filter
        )
        return [SearchResult(id=str(h.id), score=h.score, payload=h.payload) for h in hits]

    def delete_by_id(self, ids: list):
        """Soft delete: set deleted=True in payload rather than physical removal."""
        for pid in ids:
            self.client.set_payload(
                collection_name = self.collection_name,
                payload         = {"deleted": True},
                points          = [str(pid)]
            )
        logger.info("Soft-deleted %d vectors from '%s'", len(ids), self.collection_name)

    def get_collection_stats(self) -> dict:
        info = self.client.get_collection(self.collection_name)
        return {
            "vectors_count":     info.vectors_count,
            "indexed_vectors":   info.indexed_vectors_count,
            "segments_count":    info.segments_count,
            "status":            str(info.status)
        }
2.9 — Orchestration Across Hermes Fleet Nodes
