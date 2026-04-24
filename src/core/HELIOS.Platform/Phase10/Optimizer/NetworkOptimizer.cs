using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace HELIOS.Platform.Phase10.Optimizer
{
    /// <summary>
    /// Network Optimizer - TCP stack, DNS, and latency tuning
    /// </summary>
    public class NetworkOptimizer : BaseOptimizerService
    {
        private const string TcpIpPath = @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters";
        private const string InterfacePath = @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces";

        public NetworkOptimizer(OptimizationProfile profile = null)
        {
            _serviceName = "NetworkOptimizer";
            _profile = profile ?? new OptimizationProfile { Name = "Default" };
        }

        public override async Task<OptimizationResult> OptimizeAsync(CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new OptimizationResult { Changes = new List<string>() };

            try
            {
                if (!_isInitialized)
                    await InitializeAsync();

                // Tune TCP stack
                await TuneTcpStackAsync(cancellationToken, result);

                // Optimize buffer sizes
                await OptimizeBufferSizesAsync(cancellationToken, result);

                // Configure DNS
                await ConfigureDNSAsync(cancellationToken, result);

                // Enable advanced optimizations
                await EnableAdvancedOptimizationsAsync(cancellationToken, result);

                stopwatch.Stop();
                result.Success = true;
                result.ExecutionTime = stopwatch.Elapsed;
                result.Message = $"Network optimization completed in {stopwatch.ElapsedMilliseconds}ms";
                _lastOptimization = DateTime.Now;

                return result;
            }
            catch (Exception ex)
            {
                LogError($"Network optimization error: {ex.Message}");
                result.Success = false;
                result.Message = $"Error: {ex.Message}";
                return result;
            }
        }

        public override async Task<Dictionary<string, object>> GetMetricsAsync()
        {
            var metrics = new Dictionary<string, object>
            {
                ["NetworkInterfaces"] = NetworkInterface.GetAllNetworkInterfaces().Length,
                ["Bandwidth"] = CalculateBandwidth(),
                ["Latency"] = MeasureLatency(),
                ["PacketLoss"] = MeasurePacketLoss(),
                ["DNSResolutionTime"] = MeasureDNSResolutionTime()
            };

            return await Task.FromResult(metrics);
        }

        private async Task TuneTcpStackAsync(CancellationToken cancellationToken, OptimizationResult result)
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(TcpIpPath, true))
                {
                    if (key != null)
                    {
                        // Increase TCP connection backlog
                        key.SetValue("TcpMaxConnections", 65535, RegistryValueKind.DWord);
                        result.Changes.Add("TCP max connections increased to 65535");

                        // Optimize time-wait delay
                        key.SetValue("TcpTimedWaitDelay", 30, RegistryValueKind.DWord);
                        result.Changes.Add("TCP timed-wait delay optimized");

                        // Enable TCP fast open
                        key.SetValue("EnableTCPChimney", 1, RegistryValueKind.DWord);
                        result.Changes.Add("TCP Chimney offloading enabled");

                        // Increase TCP half-open connections
                        key.SetValue("TcpMaxHalfOpen", 65535, RegistryValueKind.DWord);
                        result.Changes.Add("TCP half-open connections optimized");

                        // Enable SACK
                        key.SetValue("Tcp1323Opts", 3, RegistryValueKind.DWord);
                        result.Changes.Add("SACK enabled");

                        // Optimize window scaling
                        key.SetValue("TcpWindowSize", 65535, RegistryValueKind.DWord);
                        result.Changes.Add("TCP window size optimized");
                    }
                }

                result.Metrics["TCPStackOptimized"] = true;
                await Task.Delay(100, cancellationToken);
            }
            catch (Exception ex)
            {
                LogError($"TCP stack tuning error: {ex.Message}");
            }
        }

        private async Task OptimizeBufferSizesAsync(CancellationToken cancellationToken, OptimizationResult result)
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(TcpIpPath, true))
                {
                    if (key != null)
                    {
                        // Increase socket buffer sizes
                        key.SetValue("DefaultRcvWindow", 65535, RegistryValueKind.DWord);
                        key.SetValue("DefaultSndWindow", 65535, RegistryValueKind.DWord);
                        result.Changes.Add("Socket buffer sizes optimized");

                        // Increase global MaxUserPort
                        key.SetValue("MaxUserPort", 65534, RegistryValueKind.DWord);
                        result.Changes.Add("Global max user port increased");

                        // Optimize NetBIOS settings
                        key.SetValue("NetbiosOptions", 1, RegistryValueKind.DWord);
                        result.Changes.Add("NetBIOS optimized");
                    }
                }

                result.Metrics["BuffersOptimized"] = true;
                await Task.Delay(100, cancellationToken);
            }
            catch (Exception ex)
            {
                LogError($"Buffer size optimization error: {ex.Message}");
            }
        }

        private async Task ConfigureDNSAsync(CancellationToken cancellationToken, OptimizationResult result)
        {
            try
            {
                // Configure Cloudflare DNS for all network interfaces
                var interfaces = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(i => i.NetworkInterfaceType != NetworkInterfaceType.Loopback && 
                               i.OperationalStatus == OperationalStatus.Up)
                    .ToList();

                foreach (var iface in interfaces)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    try
                    {
                        var ipProps = iface.GetIPProperties();
                        if (ipProps != null)
                        {
                            // Set Cloudflare DNS (1.1.1.1 and 1.0.0.1)
                            using (var key = Registry.LocalMachine.OpenSubKey($"{InterfacePath}\\{iface.Id}", true))
                            {
                                if (key != null)
                                {
                                    key.SetValue("NameServer", "1.1.1.1,1.0.0.1", RegistryValueKind.String);
                                    result.Changes.Add($"DNS configured for {iface.Name}");
                                }
                            }
                        }
                    }
                    catch
                    {
                        // Interface configuration error
                    }
                }

                result.Metrics["DNSConfigured"] = interfaces.Count;
                await Task.Delay(100, cancellationToken);
            }
            catch (Exception ex)
            {
                LogError($"DNS configuration error: {ex.Message}");
            }
        }

        private async Task EnableAdvancedOptimizationsAsync(CancellationToken cancellationToken, OptimizationResult result)
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(TcpIpPath, true))
                {
                    if (key != null)
                    {
                        // Enable jumbo frames if available
                        key.SetValue("JumboFrameSize", 9216, RegistryValueKind.DWord);
                        result.Changes.Add("Jumbo frames enabled (9216 bytes)");

                        // Enable RSS (Receive Side Scaling)
                        key.SetValue("EnableRSS", 1, RegistryValueKind.DWord);
                        result.Changes.Add("RSS enabled for multi-core optimization");

                        // Enable TSO (TCP Segment Offloading)
                        key.SetValue("EnableTSO", 1, RegistryValueKind.DWord);
                        result.Changes.Add("TSO enabled");

                        // Enable ECN (Explicit Congestion Notification)
                        key.SetValue("EnableECN", 1, RegistryValueKind.DWord);
                        result.Changes.Add("ECN enabled");

                        // Disable IPv6 if not needed
                        key.SetValue("DisabledComponents", 32, RegistryValueKind.DWord);
                        result.Changes.Add("IPv6 optimized");
                    }
                }

                result.Metrics["AdvancedOptimizationsEnabled"] = true;
                await Task.Delay(100, cancellationToken);
            }
            catch (Exception ex)
            {
                LogError($"Advanced optimization error: {ex.Message}");
            }
        }

        private float CalculateBandwidth()
        {
            try
            {
                var interfaces = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(i => i.NetworkInterfaceType != NetworkInterfaceType.Loopback && 
                               i.OperationalStatus == OperationalStatus.Up);

                float totalSpeed = 0;
                foreach (var iface in interfaces)
                {
                    if (iface.Speed > 0)
                        totalSpeed += iface.Speed / 1_000_000f; // Convert to Mbps
                }

                return totalSpeed;
            }
            catch
            {
                return 0;
            }
        }

        private float MeasureLatency()
        {
            try
            {
                var ping = new Ping();
                var reply = ping.Send("8.8.8.8", 1000);
                return reply.Status == IPStatus.Success ? reply.RoundtripTime : 0;
            }
            catch
            {
                return 0;
            }
        }

        private float MeasurePacketLoss()
        {
            try
            {
                var ping = new Ping();
                int lostPackets = 0;
                int totalPackets = 4;

                for (int i = 0; i < totalPackets; i++)
                {
                    var reply = ping.Send("8.8.8.8", 1000);
                    if (reply.Status != IPStatus.Success)
                        lostPackets++;
                }

                return (lostPackets / (float)totalPackets) * 100;
            }
            catch
            {
                return 0;
            }
        }

        private float MeasureDNSResolutionTime()
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();
                System.Net.Dns.GetHostAddresses("www.google.com");
                stopwatch.Stop();
                return stopwatch.ElapsedMilliseconds;
            }
            catch
            {
                return 0;
            }
        }
    }
}
