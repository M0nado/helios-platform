"""Hermes/XCore specialist integration adapter.

The core package keeps imports lazy so lightweight metadata checks do not require
analytics dependencies such as NumPy or pandas until the corresponding modules
are used.
"""

__all__ = [
    "ContextualBanditRouter",
    "Experience",
    "FeaturePipeline",
    "FeatureSet",
    "HermesOrchestrator",
    "HermesTask",
    "HermesVectorStore",
    "RewardEvent",
    "EpsilonGreedyPolicy",
    "FeatureContextAdapter",
    "OfflineReplayBuffer",
    "PolicyDecision",
    "SearchResult",
    "describe",
]


def describe() -> str:
    return "Hermes/XCore adapter with imported fleet-platform X-tier modules"


def __getattr__(name: str):
    if name in {"FeaturePipeline", "FeatureSet"}:
        from .feature_pipeline import FeaturePipeline, FeatureSet

        return {"FeaturePipeline": FeaturePipeline, "FeatureSet": FeatureSet}[name]
    if name in {"HermesOrchestrator", "HermesTask"}:
        from .orchestrator import HermesOrchestrator, HermesTask

        return {"HermesOrchestrator": HermesOrchestrator, "HermesTask": HermesTask}[name]
    if name in {"ContextualBanditRouter", "Experience"}:
        from .routing_policy import ContextualBanditRouter, Experience

        return {"ContextualBanditRouter": ContextualBanditRouter, "Experience": Experience}[name]
    if name in {"EpsilonGreedyPolicy", "FeatureContextAdapter", "OfflineReplayBuffer", "PolicyDecision", "RewardEvent"}:
        from .reinforcement_learning import (
            EpsilonGreedyPolicy,
            FeatureContextAdapter,
            OfflineReplayBuffer,
            PolicyDecision,
            RewardEvent,
        )

        return {
            "EpsilonGreedyPolicy": EpsilonGreedyPolicy,
            "FeatureContextAdapter": FeatureContextAdapter,
            "OfflineReplayBuffer": OfflineReplayBuffer,
            "PolicyDecision": PolicyDecision,
            "RewardEvent": RewardEvent,
        }[name]
    if name in {"HermesVectorStore", "SearchResult"}:
        from .vector_store import HermesVectorStore, SearchResult

        return {"HermesVectorStore": HermesVectorStore, "SearchResult": SearchResult}[name]
    raise AttributeError(f"module 'hermes_xcore' has no attribute {name!r}")
