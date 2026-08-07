namespace HELIOS.XCore9;

public sealed record XCore9Options(
    int MaxTotalInstances = 9,
    int MaxCpuUnits = 18,
    int MaxMemoryMiB = 18432,
    int MaxFeaturesPerRun = 64,
    int MaxRunHistoryEntries = 2048,
    int MaxNegotiationEntries = 1024,
    int MinimumHoldoutSamples = 100,
    double MinimumImprovement = 0.01,
    TimeSpan? LeaseDuration = null)
{
    public TimeSpan EffectiveLeaseDuration => LeaseDuration ?? TimeSpan.FromMinutes(15);
}
