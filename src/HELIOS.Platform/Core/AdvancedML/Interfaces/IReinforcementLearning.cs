namespace HELIOS.Platform.Core.AdvancedML.Interfaces;

/// <summary>
/// Reinforcement learning for adaptive system optimization.
/// Uses reward-based learning to improve system behavior over time.
/// </summary>
public interface IReinforcementLearning
{
    /// <summary>
    /// Selects action based on current state using learned policy.
    /// </summary>
    /// <param name="state">Current system state (normalized features).</param>
    /// <param name="explorationRate">Epsilon for epsilon-greedy strategy (0-1).</param>
    /// <returns>Selected action index.</returns>
    Task<RLAction> SelectActionAsync(double[] state, double explorationRate = 0.1);

    /// <summary>
    /// Records experience and updates Q-values.
    /// </summary>
    /// <param name="state">Previous state.</param>
    /// <param name="action">Action taken.</param>
    /// <param name="reward">Reward received.</param>
    /// <param name="nextState">Resulting state.</param>
    /// <param name="isTerminal">Whether episode ended.</param>
    Task LearnAsync(double[] state, int action, double reward, double[] nextState, bool isTerminal);

    /// <summary>
    /// Trains agent on batch of experiences.
    /// </summary>
    Task TrainBatchAsync(List<Experience> experiences);

    /// <summary>
    /// Gets current policy statistics.
    /// </summary>
    Task<RLPolicyStats> GetPolicyStatsAsync();

    /// <summary>
    /// Resets agent to initial state.
    /// </summary>
    Task ResetAsync();
}

/// <summary>
/// RL action selection result.
/// </summary>
public class RLAction
{
    /// <summary>Selected action index.</summary>
    public int ActionIndex { get; set; }

    /// <summary>Q-value of selected action.</summary>
    public double QValue { get; set; }

    /// <summary>Confidence score (0-1).</summary>
    public double Confidence { get; set; }

    /// <summary>Was action exploration (random) or exploitation (learned).</summary>
    public bool IsExploration { get; set; }

    /// <summary>All available Q-values for reference.</summary>
    public double[] AllQValues { get; set; } = Array.Empty<double>();

    /// <summary>Action selection timestamp.</summary>
    public DateTime SelectedAt { get; set; }
}

/// <summary>
/// Experience tuple for replay buffer.
/// </summary>
public class Experience
{
    /// <summary>State before action.</summary>
    public double[] State { get; set; } = Array.Empty<double>();

    /// <summary>Action taken.</summary>
    public int Action { get; set; }

    /// <summary>Reward received.</summary>
    public double Reward { get; set; }

    /// <summary>Next state after action.</summary>
    public double[] NextState { get; set; } = Array.Empty<double>();

    /// <summary>Whether episode terminated.</summary>
    public bool IsTerminal { get; set; }

    /// <summary>Discount factor for this experience.</summary>
    public double Gamma { get; set; } = 0.99;

    /// <summary>Experience priority for prioritized replay.</summary>
    public double Priority { get; set; } = 1.0;

    /// <summary>Timestamp of experience.</summary>
    public DateTime CollectedAt { get; set; }
}

/// <summary>
/// RL policy statistics.
/// </summary>
public class RLPolicyStats
{
    /// <summary>Number of actions available.</summary>
    public int ActionCount { get; set; }

    /// <summary>Number of states explored.</summary>
    public int StatesExplored { get; set; }

    /// <summary>Cumulative reward collected.</summary>
    public double CumulativeReward { get; set; }

    /// <summary>Average reward per episode.</summary>
    public double AverageRewardPerEpisode { get; set; }

    /// <summary>Episodes completed.</summary>
    public int EpisodesCompleted { get; set; }

    /// <summary>Exploration rate (epsilon).</summary>
    public double ExplorationRate { get; set; }

    /// <summary>Learning rate (alpha).</summary>
    public double LearningRate { get; set; }

    /// <summary>Discount factor (gamma).</summary>
    public double DiscountFactor { get; set; }

    /// <summary>Q-table convergence indicator (0-1).</summary>
    public double ConvergenceIndicator { get; set; }

    /// <summary>Experience replay buffer size.</summary>
    public int ReplayBufferSize { get; set; }
}
