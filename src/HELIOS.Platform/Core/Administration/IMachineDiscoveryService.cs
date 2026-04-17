using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace HELIOS.Platform.Core.Administration;

public class MachineInfo
{
    public string MachineId { get; set; }
    public string MachineName { get; set; }
    public string IPAddress { get; set; }
    public string OSVersion { get; set; }
    public string ProcessorInfo { get; set; }
    public long TotalMemoryMB { get; set; }
    public long AvailableMemoryMB { get; set; }
    public bool IsOnline { get; set; }
    public DateTime LastSeenAt { get; set; }
    public DateTime DiscoveredAt { get; set; }
    public Dictionary<string, object> CustomProperties { get; set; } = new();
}

public class MachineHealth
{
    public string MachineId { get; set; }
    public int HealthScore { get; set; } // 0-100
    public int CPUUsagePercent { get; set; }
    public int MemoryUsagePercent { get; set; }
    public int DiskUsagePercent { get; set; }
    public List<string> Issues { get; set; } = new();
    public DateTime CheckTime { get; set; }
}

public class DiscoveryOptions
{
    public string NetworkRange { get; set; }
    public int TimeoutSeconds { get; set; }
    public bool IncludeOfflineMachines { get; set; }
    public List<string> ExcludePatterns { get; set; } = new();
}

public interface IMachineDiscoveryService
{
    Task<List<MachineInfo>> DiscoverMachinesAsync(DiscoveryOptions options);
    Task<MachineInfo> GetMachineInfoAsync(string machineId);
    Task<List<MachineInfo>> GetAllMachinesAsync();
    Task<List<MachineInfo>> GetOnlineMachinesAsync();
    Task<MachineHealth> GetMachineHealthAsync(string machineId);
    Task<List<MachineHealth>> GetAllMachineHealthAsync();
    Task<bool> RegisterMachineAsync(MachineInfo machine);
    Task<bool> UnregisterMachineAsync(string machineId);
    Task<bool> PingMachineAsync(string machineId);
    Task<bool> UpdateMachineStatusAsync(string machineId, bool isOnline);
}

public class MachineDiscoveryService : IMachineDiscoveryService
{
    private readonly List<MachineInfo> _discoveredMachines = new();
    private readonly Dictionary<string, MachineHealth> _machineHealth = new();

    public MachineDiscoveryService()
    {
        InitializeDefaultMachines();
    }

    private void InitializeDefaultMachines()
    {
        var localMachine = new MachineInfo
        {
            MachineId = Guid.NewGuid().ToString(),
            MachineName = System.Environment.MachineName,
            IPAddress = "127.0.0.1",
            OSVersion = System.Environment.OSVersion.ToString(),
            ProcessorInfo = "Intel Core i7",
            TotalMemoryMB = 16384,
            AvailableMemoryMB = 8192,
            IsOnline = true,
            DiscoveredAt = DateTime.UtcNow,
            LastSeenAt = DateTime.UtcNow
        };

        _discoveredMachines.Add(localMachine);

        _machineHealth[localMachine.MachineId] = new MachineHealth
        {
            MachineId = localMachine.MachineId,
            HealthScore = 85,
            CPUUsagePercent = 25,
            MemoryUsagePercent = 50,
            DiskUsagePercent = 40,
            CheckTime = DateTime.UtcNow
        };
    }

    public async Task<List<MachineInfo>> DiscoverMachinesAsync(DiscoveryOptions options)
    {
        var discovered = new List<MachineInfo>();

        // Simulate discovery
        for (int i = 0; i < 3; i++)
        {
            var machine = new MachineInfo
            {
                MachineId = Guid.NewGuid().ToString(),
                MachineName = $"Server-{i + 1}",
                IPAddress = $"192.168.1.{100 + i}",
                OSVersion = "Windows Server 2022",
                ProcessorInfo = "Intel Xeon",
                TotalMemoryMB = 32768,
                AvailableMemoryMB = 16384,
                IsOnline = i < 2,
                DiscoveredAt = DateTime.UtcNow,
                LastSeenAt = DateTime.UtcNow
            };

            if (!options.ExcludePatterns.Any(p => machine.MachineName.Contains(p)))
            {
                discovered.Add(machine);
                if (!_discoveredMachines.Any(m => m.MachineId == machine.MachineId))
                {
                    _discoveredMachines.Add(machine);
                    _machineHealth[machine.MachineId] = new MachineHealth
                    {
                        MachineId = machine.MachineId,
                        HealthScore = i < 2 ? 80 : 40,
                        CPUUsagePercent = i < 2 ? 30 : 65,
                        MemoryUsagePercent = i < 2 ? 45 : 80,
                        DiskUsagePercent = i < 2 ? 35 : 85,
                        CheckTime = DateTime.UtcNow,
                        Issues = i < 2 ? new List<string>() : new List<string> { "Machine offline", "High disk usage" }
                    };
                }
            }
        }

        return await Task.FromResult(discovered);
    }

    public async Task<MachineInfo> GetMachineInfoAsync(string machineId)
    {
        var machine = _discoveredMachines.FirstOrDefault(m => m.MachineId == machineId);
        return await Task.FromResult(machine);
    }

    public async Task<List<MachineInfo>> GetAllMachinesAsync()
    {
        return await Task.FromResult(new List<MachineInfo>(_discoveredMachines));
    }

    public async Task<List<MachineInfo>> GetOnlineMachinesAsync()
    {
        return await Task.FromResult(_discoveredMachines.Where(m => m.IsOnline).ToList());
    }

    public async Task<MachineHealth> GetMachineHealthAsync(string machineId)
    {
        _machineHealth.TryGetValue(machineId, out var health);
        return await Task.FromResult(health);
    }

    public async Task<List<MachineHealth>> GetAllMachineHealthAsync()
    {
        return await Task.FromResult(_machineHealth.Values.ToList());
    }

    public async Task<bool> RegisterMachineAsync(MachineInfo machine)
    {
        machine.MachineId = Guid.NewGuid().ToString();
        machine.DiscoveredAt = DateTime.UtcNow;
        machine.LastSeenAt = DateTime.UtcNow;

        _discoveredMachines.Add(machine);

        _machineHealth[machine.MachineId] = new MachineHealth
        {
            MachineId = machine.MachineId,
            HealthScore = 75,
            CheckTime = DateTime.UtcNow
        };

        return await Task.FromResult(true);
    }

    public async Task<bool> UnregisterMachineAsync(string machineId)
    {
        var machine = _discoveredMachines.FirstOrDefault(m => m.MachineId == machineId);
        if (machine == null)
            return await Task.FromResult(false);

        _discoveredMachines.Remove(machine);
        _machineHealth.Remove(machineId);

        return await Task.FromResult(true);
    }

    public async Task<bool> PingMachineAsync(string machineId)
    {
        var machine = _discoveredMachines.FirstOrDefault(m => m.MachineId == machineId);
        if (machine == null)
            return await Task.FromResult(false);

        machine.LastSeenAt = DateTime.UtcNow;
        machine.IsOnline = true;

        return await Task.FromResult(true);
    }

    public async Task<bool> UpdateMachineStatusAsync(string machineId, bool isOnline)
    {
        var machine = _discoveredMachines.FirstOrDefault(m => m.MachineId == machineId);
        if (machine == null)
            return await Task.FromResult(false);

        machine.IsOnline = isOnline;
        machine.LastSeenAt = DateTime.UtcNow;

        return await Task.FromResult(true);
    }
}
