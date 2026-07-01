from __future__ import annotations

import csv
import random
from dataclasses import dataclass, field
from pathlib import Path
from typing import Iterable, Mapping, MutableMapping, Sequence


@dataclass(frozen=True)
class RewardEvent:
    """Auditable reward feedback for a selected Hermes/XCore action."""

    action: str
    reward: float
    context: Mapping[str, float] = field(default_factory=dict)
    metadata: Mapping[str, str] = field(default_factory=dict)


@dataclass(frozen=True)
class PolicyDecision:
    """Auditable policy decision produced before dispatching a Hermes/XCore task."""

    action: str
    score: float
    context: Mapping[str, float] = field(default_factory=dict)
    reason: str = "epsilon-greedy"


@dataclass
class ActionValue:
    """Incremental value estimate for one action."""

    count: int = 0
    value: float = 0.0

    def update(self, reward: float) -> None:
        self.count += 1
        self.value += (reward - self.value) / self.count


class OfflineReplayBuffer:
    """Replay reward events from Hermes fleet logs or CSV exports."""

    def __init__(self, events: Iterable[RewardEvent] | None = None):
        self._events = list(events or [])

    @classmethod
    def from_csv(cls, path: str | Path) -> "OfflineReplayBuffer":
        events: list[RewardEvent] = []
        with Path(path).open(newline="", encoding="utf-8") as handle:
            reader = csv.DictReader(handle)
            for row in reader:
                action = row.pop("action")
                reward = float(row.pop("reward"))
                context = {
                    key.removeprefix("context."): float(value)
                    for key, value in row.items()
                    if key.startswith("context.") and value not in (None, "")
                }
                metadata = {
                    key.removeprefix("metadata."): value
                    for key, value in row.items()
                    if key.startswith("metadata.") and value not in (None, "")
                }
                events.append(RewardEvent(action=action, reward=reward, context=context, metadata=metadata))
        return cls(events)

    def append(self, event: RewardEvent) -> None:
        self._events.append(event)

    def replay_into(self, policy: "EpsilonGreedyPolicy") -> None:
        for event in self._events:
            policy.record_reward(event)

    def __iter__(self):
        return iter(self._events)

    def __len__(self) -> int:
        return len(self._events)


class FeatureContextAdapter:
    """Convert feature dictionaries into stable numeric RL context vectors."""

    def __init__(self, keys: Sequence[str]):
        self.keys = tuple(keys)

    def transform(self, features: Mapping[str, float | int | bool]) -> dict[str, float]:
        return {key: float(features.get(key, 0.0)) for key in self.keys}


class EpsilonGreedyPolicy:
    """Small, deterministic-friendly epsilon-greedy RL policy.

    This is intended for offline Hermes/XCore experimentation and safe routing
    simulations before a production RL policy is promoted into C# orchestration.
    """

    def __init__(self, actions: list[str], epsilon: float = 0.1, seed: int | None = None):
        if not actions:
            raise ValueError("At least one action is required")
        if not 0.0 <= epsilon <= 1.0:
            raise ValueError("epsilon must be between 0.0 and 1.0")

        self.actions = list(dict.fromkeys(actions))
        self.epsilon = epsilon
        self._rng = random.Random(seed)
        self._values: MutableMapping[str, ActionValue] = {action: ActionValue() for action in self.actions}

    def select_action(self) -> str:
        if self._rng.random() < self.epsilon:
            return self._rng.choice(self.actions)

        return max(self.actions, key=lambda action: (self._values[action].value, -self.actions.index(action)))

    def decide(self, context: Mapping[str, float] | None = None) -> PolicyDecision:
        action = self.select_action()
        return PolicyDecision(
            action=action,
            score=self._values[action].value,
            context=dict(context or {}),
        )

    def record_reward(self, event: RewardEvent) -> None:
        if event.action not in self._values:
            raise KeyError(f"Unknown action: {event.action}")
        self._values[event.action].update(float(event.reward))

    def replay(self, events: Iterable[RewardEvent]) -> None:
        for event in events:
            self.record_reward(event)

    def snapshot(self) -> dict[str, dict[str, float | int]]:
        return {
            action: {"count": state.count, "value": state.value}
            for action, state in self._values.items()
        }
