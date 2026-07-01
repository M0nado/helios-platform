from hermes_xcore import EpsilonGreedyPolicy, RewardEvent
from hermes_xcore.reinforcement_learning import FeatureContextAdapter, OfflineReplayBuffer


def test_replay_promotes_best_action():
    policy = EpsilonGreedyPolicy(["core", "hermes"], epsilon=0.0, seed=7)
    buffer = OfflineReplayBuffer(
        [
            RewardEvent(action="core", reward=0.2),
            RewardEvent(action="hermes", reward=1.0),
            RewardEvent(action="hermes", reward=0.8),
        ]
    )

    buffer.replay_into(policy)

    assert policy.select_action() == "hermes"
    assert policy.snapshot()["hermes"]["count"] == 2


def test_context_adapter_and_decision_are_auditable():
    adapter = FeatureContextAdapter(["latency_ms", "queue_depth"])
    context = adapter.transform({"latency_ms": 25, "queue_depth": 3, "ignored": 100})
    policy = EpsilonGreedyPolicy(["core", "hermes"], epsilon=0.0, seed=11)

    decision = policy.decide(context)

    assert decision.action == "core"
    assert decision.context == {"latency_ms": 25.0, "queue_depth": 3.0}
