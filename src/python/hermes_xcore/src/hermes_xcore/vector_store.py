from __future__ import annotations

import logging
from dataclasses import dataclass
from typing import Optional

import numpy as np

logger = logging.getLogger("hermes.vector_store")


@dataclass
class SearchResult:
    id: str
    score: float
    payload: dict


class HermesVectorStore:
    """
    Lightweight integrated vector store facade.
    Uses in-memory fallback when Qdrant client is unavailable.
    """

    def __init__(self, vector_size: int):
        self.vector_size = vector_size
        self._ids: list[str] = []
        self._vectors: list[np.ndarray] = []
        self._payloads: list[dict] = []

    def upsert_embeddings(self, ids: list[str], vectors: np.ndarray, payloads: list[dict]) -> None:
        for i, item_id in enumerate(ids):
            self._ids.append(str(item_id))
            self._vectors.append(vectors[i].astype(np.float64))
            self._payloads.append(payloads[i])
        logger.info("Upserted %d vectors (in-memory mode)", len(ids))

    def search(self, query_vector: np.ndarray, top_k: int = 10) -> list[SearchResult]:
        if not self._vectors:
            return []
        q = query_vector.astype(np.float64)
        q_norm = np.linalg.norm(q) + 1e-9
        scored: list[tuple[int, float]] = []
        for i, vec in enumerate(self._vectors):
            score = float(np.dot(q, vec) / ((np.linalg.norm(vec) + 1e-9) * q_norm))
            scored.append((i, score))
        scored.sort(key=lambda t: t[1], reverse=True)
        return [
            SearchResult(id=self._ids[i], score=score, payload=self._payloads[i])
            for i, score in scored[:top_k]
        ]

    def delete_by_id(self, ids: list[str]) -> None:
        doomed = set(map(str, ids))
        kept = [(i, v, p) for i, v, p in zip(self._ids, self._vectors, self._payloads) if i not in doomed]
        self._ids = [x[0] for x in kept]
        self._vectors = [x[1] for x in kept]
        self._payloads = [x[2] for x in kept]

    def get_collection_stats(self) -> dict:
        return {
            "vectors_count": len(self._vectors),
            "vector_size": self.vector_size,
            "backend": "in_memory",
        }

