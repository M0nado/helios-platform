from __future__ import annotations

import random
from dataclasses import dataclass, field
from typing import Mapping, MutableMapping


@dataclass(frozen=True)
class RewardEvent:
    """Auditable reward feedback for a selected Hermes/XCore action."""

    action: str
    reward: float
    context: Mapping[str, float] = field(default_factory=dict)
    metadata: Mapping[str, str] = field(default_factory=dict)


@dataclass
class ActionValue:
    """Incremental value estimate for one action."""

    count: int = 0
    value: float = 0.0

    def update(self, reward: float) -> None:
        self.count += 1
        self.value += (reward - self.value) / self.count


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

    def record_reward(self, event: RewardEvent) -> None:
        if event.action not in self._values:
            raise KeyError(f"Unknown action: {event.action}")
        self._values[event.action].update(float(event.reward))

    def snapshot(self) -> dict[str, dict[str, float | int]]:
        return {
            action: {"count": state.count, "value": state.value}
            for action, state in self._values.items()
        }
