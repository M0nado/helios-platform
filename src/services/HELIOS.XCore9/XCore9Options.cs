namespace HELIOS.XCore9;

public sealed record XCore9Options(
    int MaxTotalInstances = 9,
    int MaxCpuUnits = 18,
    int MaxMemoryMiB = 18432,
    int MaxFeaturesPerRun = 64,
    int MaxRoutesPerScoringRequest = 256,
    int MaxRunHistoryEntries = 2048,
    int MaxNegotiationEntries = 1024,
    int MaxEvidenceLinks = 16,
    int MaxEvidenceLinkLength = 2048,
    int MaxPolicyRules = 256,
    int MaxPolicyRuleKeyLength = 128,
    int MaxPolicyRuleValueLength = 1024,
    int MinimumHoldoutSamples = 100,
    double MinimumImprovement = 0.01,
    string AuditEnvironment = "local",
    TimeSpan? LeaseDuration = null)
{
    public TimeSpan EffectiveLeaseDuration => LeaseDuration ?? TimeSpan.FromMinutes(15);
}
