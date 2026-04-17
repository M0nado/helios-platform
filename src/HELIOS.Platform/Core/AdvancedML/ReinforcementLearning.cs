using HELIOS.Platform.Core.AdvancedML.Interfaces;
using HELIOS.Platform.Core.Performance;
using System.Collections.Concurrent;

namespace HELIOS.Platform.Core.AdvancedML;

/// <summary>
/// Reinforcement learning agent for adaptive system optimization.
/// </summary>
public class ReinforcementLearning : IReinforcementLearning
{
    private readonly Logging.ILogger _logger;
    private readonly IL1CacheService _cache;
    private readonly ConcurrentDictionary<string, double[]> _qTable = new();
    private readonly ConcurrentQueue<Experience> _replayBuffer = new();
    private int _actionCount = 0;
    private int _statesExplored = 0;
    private double _cumulativeReward = 0;
    private int _episodesCompleted = 0;
    private double _explorationRate = 0.1;
    private readonly double _learningRate = 0.1;
    private readonly double _discountFactor = 0.99;
    private const int ReplayBufferCapacity = 1000;

    public ReinforcementLearning(Logging.ILogger logger, IL1CacheService cache)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    public async Task<RLAction> SelectActionAsync(double[] state, double explorationRate = 0.1)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.Length == 0) throw new ArgumentException("State cannot be empty");

        try
        {
            _explorationRate = explorationRate;
            var stateKey = GetStateKey(state);

            // Initialize Q-values for this state if needed
            if (!_qTable.TryGetValue(stateKey, out var qValues))
            {
                qValues = new double[10]; // Assume 10 actions
                for (int i = 0; i < qValues.Length; i++)
                    qValues[i] = Random.Shared.NextDouble() * 0.1;
                _qTable[stateKey] = qValues;
                _statesExplored++;
                _actionCount = qValues.Length;
            }

            // Epsilon-greedy action selection
            int selectedAction;
            bool isExploration = false;

            if (Random.Shared.NextDouble() < explorationRate)
            {
                // Explore: random action
                selectedAction = Random.Shared.Next(qValues.Length);
                isExploration = true;
            }
            else
            {
                // Exploit: best action
                selectedAction = Array.IndexOf(qValues, qValues.Max());
            }

            var action = new RLAction
            {
                ActionIndex = selectedAction,
                QValue = qValues[selectedAction],
                Confidence = Math.Min(0.99, Math.Abs(qValues[selectedAction]) + 0.5),
                IsExploration = isExploration,
                AllQValues = (double[])qValues.Clone(),
                SelectedAt = DateTime.UtcNow
            };

            await Task.CompletedTask;
            return action;
        }
        catch (Exception ex)
        {
            _logger.Error($"Error in ReinforcementLearning.SelectActionAsync: {ex.Message}");
            throw;
        }
    }

    public async Task LearnAsync(double[] state, int action, double reward, double[] nextState, bool isTerminal)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(nextState);

        try
        {
            var experience = new Experience
            {
                State = state,
                Action = action,
                Reward = reward,
                NextState = nextState,
                IsTerminal = isTerminal,
                CollectedAt = DateTime.UtcNow
            };

            // Add to replay buffer
            _replayBuffer.Enqueue(experience);
            if (_replayBuffer.Count > ReplayBufferCapacity && _replayBuffer.TryDequeue(out _))
            {
                // Evicted old experience
            }

            // Update cumulative reward
            _cumulativeReward += reward;

            // Q-learning update
            var stateKey = GetStateKey(state);
            var nextStateKey = GetStateKey(nextState);

            if (_qTable.TryGetValue(stateKey, out var qValues))
            {
                double maxNextQ = 0;
                if (_qTable.TryGetValue(nextStateKey, out var nextQValues))
                    maxNextQ = nextQValues.Max();

                // Q(s,a) = Q(s,a) + alpha * (reward + gamma * max(Q(s',a')) - Q(s,a))
                double tdError = reward + (isTerminal ? 0 : _discountFactor * maxNextQ) - qValues[action];
                qValues[action] += _learningRate * tdError;
            }

            if (isTerminal)
                _episodesCompleted++;

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.Error($"Error in ReinforcementLearning.LearnAsync: {ex.Message}");
            throw;
        }
    }

    public async Task TrainBatchAsync(List<Experience> experiences)
    {
        ArgumentNullException.ThrowIfNull(experiences);
        if (experiences.Count == 0) throw new ArgumentException("Experiences list cannot be empty");

        try
        {
            foreach (var exp in experiences)
            {
                await LearnAsync(exp.State, exp.Action, exp.Reward, exp.NextState, exp.IsTerminal);
            }

            _logger.Info($"Batch training completed: {experiences.Count} experiences processed");
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.Error($"Error in TrainBatchAsync: {ex.Message}");
            throw;
        }
    }

    public async Task<RLPolicyStats> GetPolicyStatsAsync()
    {
        // Calculate convergence indicator
        double convergenceIndicator = 0;
        if (_qTable.Count > 0)
        {
            var allQValues = _qTable.Values.SelectMany(q => q).ToList();
            if (allQValues.Count > 0)
            {
                var variance = allQValues.Sum(q => q * q) / allQValues.Count;
                convergenceIndicator = Math.Min(1.0, Math.Abs(variance) / 10.0);
            }
        }

        var stats = new RLPolicyStats
        {
            ActionCount = _actionCount,
            StatesExplored = _statesExplored,
            CumulativeReward = _cumulativeReward,
            AverageRewardPerEpisode = _episodesCompleted > 0 ? _cumulativeReward / _episodesCompleted : 0,
            EpisodesCompleted = _episodesCompleted,
            ExplorationRate = _explorationRate,
            LearningRate = _learningRate,
            DiscountFactor = _discountFactor,
            ConvergenceIndicator = convergenceIndicator,
            ReplayBufferSize = _replayBuffer.Count
        };

        await Task.CompletedTask;
        return stats;
    }

    public async Task ResetAsync()
    {
        _qTable.Clear();
        while (_replayBuffer.TryDequeue(out _)) { }
        _cumulativeReward = 0;
        _episodesCompleted = 0;
        _statesExplored = 0;
        _actionCount = 0;
        await Task.CompletedTask;
    }

    private static string GetStateKey(double[] state)
    {
        if (state.Length == 0) return "empty";
        // Quantize state to string key
        var quantized = state.Select(x => Math.Round(x * 100) / 100).ToList();
        return string.Join("_", quantized);
    }
}
