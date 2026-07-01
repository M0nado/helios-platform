namespace HELIOS.Platform.Contracts.ReinforcementLearning;

public sealed record RewardEventContract(
    string Action,
    double Reward,
    IReadOnlyDictionary<string, double> Context,
    IReadOnlyDictionary<string, string> Metadata);

public sealed record PolicyDecisionContract(
    string Action,
    double Score,
    IReadOnlyDictionary<string, double> Context,
    string Reason);
