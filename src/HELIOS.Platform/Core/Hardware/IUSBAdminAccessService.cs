using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace HELIOS.Platform.Core.Hardware;

public class USBDevice
{
    public string DeviceId { get; set; }
    public string ProductName { get; set; }
    public string Manufacturer { get; set; }
    public string SerialNumber { get; set; }
    public USBDeviceStatus Status { get; set; }
    public DateTime ConnectedTime { get; set; }
    public bool IsAuthenticated { get; set; }
    public bool IsTrusted { get; set; }
}

public enum USBDeviceStatus
{
    Connected,
    Disconnected,
    Blocked,
    Restricted,
    Trusted,
    Unauthorized
}

public class USBAccessPolicy
{
    public string PolicyId { get; set; }
    public string PolicyName { get; set; }
    public bool AllowUSBDevices { get; set; }
    public bool AllowRemovableStorage { get; set; }
    public bool AllowUSBHubs { get; set; }
    public List<string> TrustedDevices { get; set; } = new();
    public List<string> BlacklistedDevices { get; set; } = new();
    public bool RequireAuthentication { get; set; }
    public bool AuditUSBActivity { get; set; }
}

public class USBActivityLog
{
    public int Id { get; set; }
    public string DeviceId { get; set; }
    public string ActivityType { get; set; } // Connect, Disconnect, Read, Write, Eject
    public DateTime ActivityTime { get; set; }
    public string User { get; set; }
    public bool Authorized { get; set; }
    public string Details { get; set; }
}

public interface IUSBAdminAccessService
{
    Task<List<USBDevice>> ListConnectedDevicesAsync();
    Task<USBDevice> GetDeviceInfoAsync(string deviceId);
    Task<bool> AuthorizeDeviceAsync(string deviceId);
    Task<bool> BlockDeviceAsync(string deviceId);
    Task<bool> UnblockDeviceAsync(string deviceId);
    Task<bool> TrustDeviceAsync(string deviceId);
    Task<bool> RemoveTrustAsync(string deviceId);
    Task<USBAccessPolicy> CreateAccessPolicyAsync(string policyName, USBAccessPolicy policy);
    Task<bool> ApplyAccessPolicyAsync(string policyId);
    Task<List<USBAccessPolicy>> ListPoliciesAsync();
    Task<List<USBActivityLog>> GetActivityLogAsync(int limit = 100);
    Task<bool> EjectDeviceAsync(string deviceId);
    Task<bool> ClearActivityLogAsync();
}

public class USBAdminAccessService : IUSBAdminAccessService
{
    private readonly List<USBDevice> _connectedDevices = new();
    private readonly List<USBAccessPolicy> _accessPolicies = new();
    private readonly List<USBActivityLog> _activityLogs = new();
    private USBAccessPolicy _activePolicy;
    private int _logIdCounter = 1;

    public USBAdminAccessService()
    {
        InitializeDefaults();
    }

    private void InitializeDefaults()
    {
        var defaultPolicy = new USBAccessPolicy
        {
            PolicyId = Guid.NewGuid().ToString(),
            PolicyName = "Default USB Policy",
            AllowUSBDevices = true,
            AllowRemovableStorage = true,
            AllowUSBHubs = true,
            RequireAuthentication = false,
            AuditUSBActivity = true
        };

        _accessPolicies.Add(defaultPolicy);
        _activePolicy = defaultPolicy;

        // Add sample connected devices
        _connectedDevices.Add(new USBDevice
        {
            DeviceId = "USB\\VID_0951&PID_1666\\9A8C4E0C",
            ProductName = "Kingston DataTraveler",
            Manufacturer = "Kingston",
            SerialNumber = "9A8C4E0C",
            Status = USBDeviceStatus.Connected,
            ConnectedTime = DateTime.UtcNow.AddMinutes(-5),
            IsAuthenticated = true,
            IsTrusted = false
        });
    }

    public async Task<List<USBDevice>> ListConnectedDevicesAsync()
    {
        return await Task.FromResult(new List<USBDevice>(_connectedDevices));
    }

    public async Task<USBDevice> GetDeviceInfoAsync(string deviceId)
    {
        var device = _connectedDevices.FirstOrDefault(d => d.DeviceId == deviceId);
        return await Task.FromResult(device);
    }

    public async Task<bool> AuthorizeDeviceAsync(string deviceId)
    {
        var device = _connectedDevices.FirstOrDefault(d => d.DeviceId == deviceId);
        if (device == null)
            return await Task.FromResult(false);

        device.IsAuthenticated = true;
        device.Status = USBDeviceStatus.Connected;

        LogActivity(deviceId, "Authorize", true, $"Device {device.ProductName} authorized");

        return await Task.FromResult(true);
    }

    public async Task<bool> BlockDeviceAsync(string deviceId)
    {
        var device = _connectedDevices.FirstOrDefault(d => d.DeviceId == deviceId);
        if (device == null)
            return await Task.FromResult(false);

        device.Status = USBDeviceStatus.Blocked;
        device.IsAuthenticated = false;

        LogActivity(deviceId, "Block", true, $"Device {device.ProductName} blocked");

        return await Task.FromResult(true);
    }

    public async Task<bool> UnblockDeviceAsync(string deviceId)
    {
        var device = _connectedDevices.FirstOrDefault(d => d.DeviceId == deviceId);
        if (device == null)
            return await Task.FromResult(false);

        device.Status = USBDeviceStatus.Connected;

        LogActivity(deviceId, "Unblock", true, $"Device {device.ProductName} unblocked");

        return await Task.FromResult(true);
    }

    public async Task<bool> TrustDeviceAsync(string deviceId)
    {
        var device = _connectedDevices.FirstOrDefault(d => d.DeviceId == deviceId);
        if (device == null)
            return await Task.FromResult(false);

        device.IsTrusted = true;
        device.Status = USBDeviceStatus.Trusted;
        _activePolicy?.TrustedDevices.Add(deviceId);

        LogActivity(deviceId, "Trust", true, $"Device {device.ProductName} added to trusted devices");

        return await Task.FromResult(true);
    }

    public async Task<bool> RemoveTrustAsync(string deviceId)
    {
        var device = _connectedDevices.FirstOrDefault(d => d.DeviceId == deviceId);
        if (device == null)
            return await Task.FromResult(false);

        device.IsTrusted = false;
        device.Status = USBDeviceStatus.Connected;
        _activePolicy?.TrustedDevices.Remove(deviceId);

        LogActivity(deviceId, "RemoveTrust", true, $"Device {device.ProductName} removed from trusted devices");

        return await Task.FromResult(true);
    }

    public async Task<USBAccessPolicy> CreateAccessPolicyAsync(string policyName, USBAccessPolicy policy)
    {
        policy.PolicyId = Guid.NewGuid().ToString();
        policy.PolicyName = policyName;
        _accessPolicies.Add(policy);

        return await Task.FromResult(policy);
    }

    public async Task<bool> ApplyAccessPolicyAsync(string policyId)
    {
        var policy = _accessPolicies.FirstOrDefault(p => p.PolicyId == policyId);
        if (policy == null)
            return await Task.FromResult(false);

        _activePolicy = policy;
        return await Task.FromResult(true);
    }

    public async Task<List<USBAccessPolicy>> ListPoliciesAsync()
    {
        return await Task.FromResult(new List<USBAccessPolicy>(_accessPolicies));
    }

    public async Task<List<USBActivityLog>> GetActivityLogAsync(int limit = 100)
    {
        return await Task.FromResult(_activityLogs.OrderByDescending(l => l.ActivityTime).Take(limit).ToList());
    }

    public async Task<bool> EjectDeviceAsync(string deviceId)
    {
        var device = _connectedDevices.FirstOrDefault(d => d.DeviceId == deviceId);
        if (device == null)
            return await Task.FromResult(false);

        device.Status = USBDeviceStatus.Disconnected;
        LogActivity(deviceId, "Eject", true, $"Device {device.ProductName} ejected");
        _connectedDevices.Remove(device);

        return await Task.FromResult(true);
    }

    public async Task<bool> ClearActivityLogAsync()
    {
        _activityLogs.Clear();
        return await Task.FromResult(true);
    }

    private void LogActivity(string deviceId, string activityType, bool authorized, string details)
    {
        var log = new USBActivityLog
        {
            Id = _logIdCounter++,
            DeviceId = deviceId,
            ActivityType = activityType,
            ActivityTime = DateTime.UtcNow,
            User = System.Environment.UserName,
            Authorized = authorized,
            Details = details
        };

        _activityLogs.Add(log);
    }
}
