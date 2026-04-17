using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace HELIOS.Platform.Core.Administration;

public class FailoverPolicy
{
    public string PolicyId { get; set; }
    public string Name { get; set; }
    public List<string> PrimaryMachines { get; set; } = new();
    public List<string> SecondaryMachines { get; set; } = new();
    public int HealthCheckIntervalSeconds { get; set; }
    public int FailoverThresholdSeconds { get; set; }
    public bool AutomaticFailover { get; set; }
    public string State { get; set; } // Active, Failover, Recovered
    public DateTime CreatedAt { get; set; }
}

public class FailoverEvent
{
    public string EventId { get; set; }
    public string PolicyId { get; set; }
    public string FromMachine { get; set; }
    public string ToMachine { get; set; }
    public string Reason { get; set; }
    public DateTime OccurredAt { get; set; }
    public int DurationSeconds { get; set; }
    public bool Successful { get; set; }
}

public class LoadBalancerConfig
{
    public string ConfigId { get; set; }
    public string Name { get; set; }
    public List<string> TargetMachines { get; set; } = new();
    public LoadBalancingAlgorithm Algorithm { get; set; }
    public int SessionTimeoutSeconds { get; set; }
    public bool StickySession { get; set; }
    public Dictionary<string, int> MachineWeights { get; set; } = new();
    public bool HealthCheckEnabled { get; set; }
    public int HealthCheckIntervalSeconds { get; set; }
}

public enum LoadBalancingAlgorithm
{
    RoundRobin,
    LeastConnections,
    IPHash,
    WeightedRoundRobin,
    Random
}

public class LoadBalancerStats
{
    public string ConfigId { get; set; }
    public long TotalRequests { get; set; }
    public Dictionary<string, long> RequestsPerMachine { get; set; } = new();
    public Dictionary<string, double> AverageResponseTime { get; set; } = new();
    public Dictionary<string, int> ActiveConnections { get; set; } = new();
    public DateTime LastUpdated { get; set; }
}

public interface IFailoverManager
{
    Task<FailoverPolicy> CreatePolicyAsync(string name, List<string> primary, List<string> secondary);
    Task<FailoverPolicy> GetPolicyAsync(string policyId);
    Task<List<FailoverPolicy>> ListPoliciesAsync();
    Task<bool> UpdatePolicyAsync(string policyId, FailoverPolicy policy);
    Task<bool> DeletePolicyAsync(string policyId);
    Task<bool> TriggerFailoverAsync(string policyId);
    Task<bool> RecoverAsync(string policyId);
    Task<List<FailoverEvent>> GetFailoverHistoryAsync(int limit = 100);
    Task<string> GetCurrentStateAsync(string policyId);
    Task<bool> EnableAutomaticFailoverAsync(string policyId);
    Task<bool> DisableAutomaticFailoverAsync(string policyId);
}

public interface ILoadBalancer
{
    Task<LoadBalancerConfig> CreateConfigAsync(string name, List<string> machines, LoadBalancingAlgorithm algorithm);
    Task<LoadBalancerConfig> GetConfigAsync(string configId);
    Task<List<LoadBalancerConfig>> ListConfigsAsync();
    Task<bool> UpdateConfigAsync(string configId, LoadBalancerConfig config);
    Task<bool> DeleteConfigAsync(string configId);
    Task<string> SelectTargetMachineAsync(string configId);
    Task<LoadBalancerStats> GetStatsAsync(string configId);
    Task<bool> RegisterMachineAsync(string configId, string machine, int weight = 1);
    Task<bool> UnregisterMachineAsync(string configId, string machine);
    Task<bool> SetMachineHealthAsync(string configId, string machine, bool healthy);
}

public class FailoverManager : IFailoverManager
{
    private readonly List<FailoverPolicy> _policies = new();
    private readonly List<FailoverEvent> _events = new();

    public FailoverManager()
    {
    }

    public async Task<FailoverPolicy> CreatePolicyAsync(string name, List<string> primary, List<string> secondary)
    {
        var policy = new FailoverPolicy
        {
            PolicyId = Guid.NewGuid().ToString(),
            Name = name,
            PrimaryMachines = primary,
            SecondaryMachines = secondary,
            HealthCheckIntervalSeconds = 30,
            FailoverThresholdSeconds = 60,
            AutomaticFailover = true,
            State = "Active",
            CreatedAt = DateTime.UtcNow
        };

        _policies.Add(policy);
        return await Task.FromResult(policy);
    }

    public async Task<FailoverPolicy> GetPolicyAsync(string policyId)
    {
        var policy = _policies.FirstOrDefault(p => p.PolicyId == policyId);
        return await Task.FromResult(policy);
    }

    public async Task<List<FailoverPolicy>> ListPoliciesAsync()
    {
        return await Task.FromResult(new List<FailoverPolicy>(_policies));
    }

    public async Task<bool> UpdatePolicyAsync(string policyId, FailoverPolicy policy)
    {
        var existing = _policies.FirstOrDefault(p => p.PolicyId == policyId);
        if (existing == null)
            return await Task.FromResult(false);

        existing.Name = policy.Name;
        existing.PrimaryMachines = policy.PrimaryMachines;
        existing.SecondaryMachines = policy.SecondaryMachines;
        existing.AutomaticFailover = policy.AutomaticFailover;

        return await Task.FromResult(true);
    }

    public async Task<bool> DeletePolicyAsync(string policyId)
    {
        var policy = _policies.FirstOrDefault(p => p.PolicyId == policyId);
        if (policy == null)
            return await Task.FromResult(false);

        _policies.Remove(policy);
        return await Task.FromResult(true);
    }

    public async Task<bool> TriggerFailoverAsync(string policyId)
    {
        var policy = _policies.FirstOrDefault(p => p.PolicyId == policyId);
        if (policy == null)
            return await Task.FromResult(false);

        var @event = new FailoverEvent
        {
            EventId = Guid.NewGuid().ToString(),
            PolicyId = policyId,
            FromMachine = policy.PrimaryMachines.FirstOrDefault(),
            ToMachine = policy.SecondaryMachines.FirstOrDefault(),
            Reason = "Manual failover triggered",
            OccurredAt = DateTime.UtcNow,
            DurationSeconds = 5,
            Successful = true
        };

        _events.Add(@event);
        policy.State = "Failover";

        return await Task.FromResult(true);
    }

    public async Task<bool> RecoverAsync(string policyId)
    {
        var policy = _policies.FirstOrDefault(p => p.PolicyId == policyId);
        if (policy == null)
            return await Task.FromResult(false);

        policy.State = "Active";
        return await Task.FromResult(true);
    }

    public async Task<List<FailoverEvent>> GetFailoverHistoryAsync(int limit = 100)
    {
        return await Task.FromResult(_events.OrderByDescending(e => e.OccurredAt).Take(limit).ToList());
    }

    public async Task<string> GetCurrentStateAsync(string policyId)
    {
        var policy = _policies.FirstOrDefault(p => p.PolicyId == policyId);
        return await Task.FromResult(policy?.State ?? "Unknown");
    }

    public async Task<bool> EnableAutomaticFailoverAsync(string policyId)
    {
        var policy = _policies.FirstOrDefault(p => p.PolicyId == policyId);
        if (policy == null)
            return await Task.FromResult(false);

        policy.AutomaticFailover = true;
        return await Task.FromResult(true);
    }

    public async Task<bool> DisableAutomaticFailoverAsync(string policyId)
    {
        var policy = _policies.FirstOrDefault(p => p.PolicyId == policyId);
        if (policy == null)
            return await Task.FromResult(false);

        policy.AutomaticFailover = false;
        return await Task.FromResult(true);
    }
}

public class LoadBalancer : ILoadBalancer
{
    private readonly List<LoadBalancerConfig> _configs = new();
    private readonly Dictionary<string, LoadBalancerStats> _stats = new();
    private int _roundRobinIndex = 0;

    public LoadBalancer()
    {
    }

    public async Task<LoadBalancerConfig> CreateConfigAsync(string name, List<string> machines, LoadBalancingAlgorithm algorithm)
    {
        var config = new LoadBalancerConfig
        {
            ConfigId = Guid.NewGuid().ToString(),
            Name = name,
            TargetMachines = machines,
            Algorithm = algorithm,
            SessionTimeoutSeconds = 3600,
            HealthCheckEnabled = true,
            HealthCheckIntervalSeconds = 30
        };

        foreach (var machine in machines)
        {
            config.MachineWeights[machine] = 1;
        }

        _configs.Add(config);
        _stats[config.ConfigId] = new LoadBalancerStats
        {
            ConfigId = config.ConfigId,
            LastUpdated = DateTime.UtcNow
        };

        return await Task.FromResult(config);
    }

    public async Task<LoadBalancerConfig> GetConfigAsync(string configId)
    {
        var config = _configs.FirstOrDefault(c => c.ConfigId == configId);
        return await Task.FromResult(config);
    }

    public async Task<List<LoadBalancerConfig>> ListConfigsAsync()
    {
        return await Task.FromResult(new List<LoadBalancerConfig>(_configs));
    }

    public async Task<bool> UpdateConfigAsync(string configId, LoadBalancerConfig config)
    {
        var existing = _configs.FirstOrDefault(c => c.ConfigId == configId);
        if (existing == null)
            return await Task.FromResult(false);

        existing.Name = config.Name;
        existing.Algorithm = config.Algorithm;
        existing.StickySession = config.StickySession;

        return await Task.FromResult(true);
    }

    public async Task<bool> DeleteConfigAsync(string configId)
    {
        var config = _configs.FirstOrDefault(c => c.ConfigId == configId);
        if (config == null)
            return await Task.FromResult(false);

        _configs.Remove(config);
        _stats.Remove(configId);

        return await Task.FromResult(true);
    }

    public async Task<string> SelectTargetMachineAsync(string configId)
    {
        var config = _configs.FirstOrDefault(c => c.ConfigId == configId);
        if (config == null || config.TargetMachines.Count == 0)
            return await Task.FromResult<string>(null);

        string selected = config.Algorithm switch
        {
            LoadBalancingAlgorithm.RoundRobin => config.TargetMachines[_roundRobinIndex++ % config.TargetMachines.Count],
            LoadBalancingAlgorithm.Random => config.TargetMachines[new Random().Next(config.TargetMachines.Count)],
            LoadBalancingAlgorithm.LeastConnections => config.TargetMachines[0],
            _ => config.TargetMachines[0]
        };

        if (_stats[configId].RequestsPerMachine.TryGetValue(selected, out var count))
            _stats[configId].RequestsPerMachine[selected] = count + 1;
        else
            _stats[configId].RequestsPerMachine[selected] = 1;

        return await Task.FromResult(selected);
    }

    public async Task<LoadBalancerStats> GetStatsAsync(string configId)
    {
        _stats.TryGetValue(configId, out var stats);
        return await Task.FromResult(stats);
    }

    public async Task<bool> RegisterMachineAsync(string configId, string machine, int weight = 1)
    {
        var config = _configs.FirstOrDefault(c => c.ConfigId == configId);
        if (config == null)
            return await Task.FromResult(false);

        config.TargetMachines.Add(machine);
        config.MachineWeights[machine] = weight;

        return await Task.FromResult(true);
    }

    public async Task<bool> UnregisterMachineAsync(string configId, string machine)
    {
        var config = _configs.FirstOrDefault(c => c.ConfigId == configId);
        if (config == null)
            return await Task.FromResult(false);

        config.TargetMachines.Remove(machine);
        config.MachineWeights.Remove(machine);

        return await Task.FromResult(true);
    }

    public async Task<bool> SetMachineHealthAsync(string configId, string machine, bool healthy)
    {
        var config = _configs.FirstOrDefault(c => c.ConfigId == configId);
        if (config == null || !config.TargetMachines.Contains(machine))
            return await Task.FromResult(false);

        return await Task.FromResult(true);
    }
}
