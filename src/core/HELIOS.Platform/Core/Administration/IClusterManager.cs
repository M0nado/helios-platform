using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace HELIOS.Platform.Core.Administration;

public class ClusterNode
{
    public string NodeId { get; set; }
    public string NodeName { get; set; }
    public string IPAddress { get; set; }
    public NodeRole Role { get; set; }
    public NodeStatus Status { get; set; }
    public DateTime JoinedAt { get; set; }
    public DateTime LastHeartbeat { get; set; }
    public int CPUCores { get; set; }
    public long MemoryMB { get; set; }
    public double CPUUsagePercent { get; set; }
    public double MemoryUsagePercent { get; set; }
}

public enum NodeRole
{
    Master,
    Worker,
    Observer
}

public enum NodeStatus
{
    Healthy,
    Degraded,
    Unhealthy,
    Offline,
    Initializing
}

public class ClusterInfo
{
    public string ClusterId { get; set; }
    public string ClusterName { get; set; }
    public List<ClusterNode> Nodes { get; set; } = new();
    public int TotalNodes { get; set; }
    public int HealthyNodes { get; set; }
    public ClusterStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public string MasterNodeId { get; set; }
}

public enum ClusterStatus
{
    Healthy,
    Degraded,
    Critical,
    Initializing,
    Offline
}

public interface IClusterManager
{
    Task<ClusterInfo> InitializeClusterAsync(string name, List<string> nodeNames);
    Task<ClusterInfo> GetClusterInfoAsync();
    Task<List<ClusterNode>> ListNodesAsync();
    Task<ClusterNode> GetNodeInfoAsync(string nodeId);
    Task<bool> AddNodeToClusterAsync(string nodeName, string ipAddress);
    Task<bool> RemoveNodeFromClusterAsync(string nodeId);
    Task<bool> PromoteNodeToMasterAsync(string nodeId);
    Task<ClusterStatus> GetClusterStatusAsync();
    Task<List<string>> GetClusterMetricsAsync();
    Task<bool> RebalanceClusterAsync();
    Task<bool> HealthCheckAsync();
}

public class ClusterManager : IClusterManager
{
    private ClusterInfo _cluster;
    private List<ClusterNode> _nodes = new();

    public ClusterManager()
    {
    }

    public async Task<ClusterInfo> InitializeClusterAsync(string name, List<string> nodeNames)
    {
        _cluster = new ClusterInfo
        {
            ClusterId = Guid.NewGuid().ToString(),
            ClusterName = name,
            Status = ClusterStatus.Initializing,
            CreatedAt = DateTime.UtcNow
        };

        for (int i = 0; i < nodeNames.Count; i++)
        {
            var node = new ClusterNode
            {
                NodeId = Guid.NewGuid().ToString(),
                NodeName = nodeNames[i],
                IPAddress = $"192.168.1.{100 + i}",
                Role = i == 0 ? NodeRole.Master : NodeRole.Worker,
                Status = NodeStatus.Initializing,
                JoinedAt = DateTime.UtcNow,
                LastHeartbeat = DateTime.UtcNow,
                CPUCores = 8,
                MemoryMB = 16384,
                CPUUsagePercent = 10,
                MemoryUsagePercent = 30
            };

            _nodes.Add(node);
            _cluster.Nodes.Add(node);

            if (i == 0)
                _cluster.MasterNodeId = node.NodeId;
        }

        _cluster.TotalNodes = _nodes.Count;
        _cluster.HealthyNodes = _nodes.Count;
        _cluster.Status = ClusterStatus.Healthy;

        return await Task.FromResult(_cluster);
    }

    public async Task<ClusterInfo> GetClusterInfoAsync()
    {
        return await Task.FromResult(_cluster);
    }

    public async Task<List<ClusterNode>> ListNodesAsync()
    {
        return await Task.FromResult(new List<ClusterNode>(_nodes));
    }

    public async Task<ClusterNode> GetNodeInfoAsync(string nodeId)
    {
        var node = _nodes.FirstOrDefault(n => n.NodeId == nodeId);
        return await Task.FromResult(node);
    }

    public async Task<bool> AddNodeToClusterAsync(string nodeName, string ipAddress)
    {
        var node = new ClusterNode
        {
            NodeId = Guid.NewGuid().ToString(),
            NodeName = nodeName,
            IPAddress = ipAddress,
            Role = NodeRole.Worker,
            Status = NodeStatus.Healthy,
            JoinedAt = DateTime.UtcNow,
            LastHeartbeat = DateTime.UtcNow,
            CPUCores = 8,
            MemoryMB = 16384
        };

        _nodes.Add(node);
        _cluster.Nodes.Add(node);
        _cluster.TotalNodes++;
        _cluster.HealthyNodes++;

        return await Task.FromResult(true);
    }

    public async Task<bool> RemoveNodeFromClusterAsync(string nodeId)
    {
        var node = _nodes.FirstOrDefault(n => n.NodeId == nodeId);
        if (node == null || node.Role == NodeRole.Master)
            return await Task.FromResult(false);

        _nodes.Remove(node);
        _cluster.Nodes.Remove(node);
        _cluster.TotalNodes--;

        if (node.Status == NodeStatus.Healthy)
            _cluster.HealthyNodes--;

        return await Task.FromResult(true);
    }

    public async Task<bool> PromoteNodeToMasterAsync(string nodeId)
    {
        var node = _nodes.FirstOrDefault(n => n.NodeId == nodeId);
        if (node == null)
            return await Task.FromResult(false);

        var currentMaster = _nodes.FirstOrDefault(n => n.Role == NodeRole.Master);
        if (currentMaster != null)
            currentMaster.Role = NodeRole.Worker;

        node.Role = NodeRole.Master;
        _cluster.MasterNodeId = nodeId;

        return await Task.FromResult(true);
    }

    public async Task<ClusterStatus> GetClusterStatusAsync()
    {
        if (_cluster == null)
            return await Task.FromResult(ClusterStatus.Offline);

        var healthyCount = _nodes.Count(n => n.Status == NodeStatus.Healthy);
        
        if (healthyCount == _nodes.Count)
            _cluster.Status = ClusterStatus.Healthy;
        else if (healthyCount >= _nodes.Count / 2)
            _cluster.Status = ClusterStatus.Degraded;
        else if (healthyCount > 0)
            _cluster.Status = ClusterStatus.Critical;
        else
            _cluster.Status = ClusterStatus.Offline;

        _cluster.HealthyNodes = healthyCount;

        return await Task.FromResult(_cluster.Status);
    }

    public async Task<List<string>> GetClusterMetricsAsync()
    {
        var metrics = new List<string>();

        if (_cluster == null)
            return await Task.FromResult(metrics);

        metrics.Add($"Cluster: {_cluster.ClusterName}");
        metrics.Add($"Total Nodes: {_cluster.TotalNodes}");
        metrics.Add($"Healthy Nodes: {_cluster.HealthyNodes}");
        metrics.Add($"Status: {_cluster.Status}");
        metrics.Add($"Master: {_nodes.FirstOrDefault(n => n.NodeId == _cluster.MasterNodeId)?.NodeName ?? "Unknown"}");

        var avgCPU = _nodes.Average(n => n.CPUUsagePercent);
        var avgMemory = _nodes.Average(n => n.MemoryUsagePercent);
        metrics.Add($"Avg CPU: {avgCPU:F1}%");
        metrics.Add($"Avg Memory: {avgMemory:F1}%");

        return await Task.FromResult(metrics);
    }

    public async Task<bool> RebalanceClusterAsync()
    {
        if (_cluster == null || _nodes.Count < 2)
            return await Task.FromResult(false);

        foreach (var node in _nodes)
        {
            node.CPUUsagePercent = Math.Max(5, node.CPUUsagePercent - 2);
            node.MemoryUsagePercent = Math.Max(20, node.MemoryUsagePercent - 3);
        }

        return await Task.FromResult(true);
    }

    public async Task<bool> HealthCheckAsync()
    {
        if (_cluster == null)
            return await Task.FromResult(false);

        foreach (var node in _nodes)
        {
            node.LastHeartbeat = DateTime.UtcNow;
            
            if (node.CPUUsagePercent > 80 || node.MemoryUsagePercent > 85)
                node.Status = NodeStatus.Degraded;
            else if (node.CPUUsagePercent < 20 && node.MemoryUsagePercent < 30)
                node.Status = NodeStatus.Healthy;
        }

        return await Task.FromResult(true);
    }
}
