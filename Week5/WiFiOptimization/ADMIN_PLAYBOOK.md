# WiFi Security Setup Playbook
## Administrator Guide for Enterprise Deployment

---

## Table of Contents
1. [Pre-Deployment Checklist](#pre-deployment-checklist)
2. [WPA3 Configuration](#wpa3-configuration)
3. [Network Security Hardening](#network-security-hardening)
4. [VPN Integration](#vpn-integration)
5. [Monitoring Setup](#monitoring-setup)
6. [Incident Response](#incident-response)
7. [Troubleshooting Guide](#troubleshooting-guide)

---

## Pre-Deployment Checklist

- [ ] WiFi hardware supports WPA3 (check router/adapter specs)
- [ ] Active Directory or LDAP available (for corporate networks)
- [ ] VPN infrastructure in place or VPN service configured
- [ ] DNS servers configured and responding
- [ ] Certificate authority (CA) available for certificate pinning
- [ ] Monitoring/alerting system prepared
- [ ] Network documentation updated
- [ ] Backup network access point configured
- [ ] Rollback plan documented
- [ ] Stakeholders notified

---

## WPA3 Configuration

### Step 1: Verify WPA3 Support

**Check on Router**:
```
Admin Console → Wireless → Security Settings
Verify: WPA3 option available
Version: WPA3 certification present
```

**Check on Client Devices**:
```
Windows: Settings → Network & Internet → WiFi → Properties
macOS: System Preferences → Network → WiFi Details
Linux: nmcli dev wifi list
```

**If WPA3 Not Supported**:
- For older routers: Firmware update may enable WPA3
- For newer routers: WPA3 likely available but disabled
- Fallback: Use WPA2 with very strong password as temporary measure

### Step 2: Enable WPA3 on Router

**Typical Router Admin Steps**:

1. **Access Router Admin Interface**
   ```
   URL: 192.168.1.1 or 192.168.0.1
   Login: admin/password (check documentation)
   ```

2. **Navigate to Wireless Security**
   ```
   Path: Wireless → Security
   Find: Security Type / Encryption dropdown
   ```

3. **Select WPA3**
   ```
   Option 1: WPA3 Personal (for home/small office)
   Option 2: WPA3-Enterprise (for corporate networks)
   ```

4. **Configure Encryption**
   ```
   WPA3 Personal: 128-bit (automatic)
   WPA3 Enterprise: 192-bit (enterprise mode)
   ```

5. **Set Password** (if personal mode)
   ```
   Minimum: 16 characters
   Recommended: 25+ characters
   Include: Mixed case, numbers, symbols
   Example: "Tr0p!c4lFruit@2024x3"
   ```

6. **Save Configuration**
   ```
   Click: Apply/Save
   Wait: Router will restart (1-2 minutes)
   Verify: WiFi drops briefly then reconnects
   ```

### Step 3: Disable Legacy Protocols

In same wireless security menu:

```
[ ] Enable 802.11a (802.11 legacy @ 5GHz) - UNCHECK
[ ] Enable 802.11b (802.11 legacy @ 2.4GHz) - UNCHECK
[ ] Enable 802.11g (802.11 legacy @ 2.4GHz) - UNCHECK
[✓] Enable 802.11n (WiFi 4 @ 2.4/5GHz) - CHECK
[✓] Enable 802.11ac (WiFi 5 @ 5GHz) - CHECK
[✓] Enable 802.11ax (WiFi 6 @ 2.4/5GHz) - CHECK
[ ] Enable 802.11be (WiFi 7) - If available, CHECK
```

### Step 4: Disable WPS

In same security menu:

```
[ ] WPS (WiFi Protected Setup) - UNCHECK
   Reason: Security vulnerability (brute-force susceptible)
```

### Step 5: Test WPA3 Connection

**On Windows Client**:
```powershell
# Remove old network profile if exists
netsh wlan delete profile name="YourSSID"

# Connect to WPA3 network
# Settings → WiFi → Network
# Select SSID, enter password
# Status should show: WPA3-Personal or WPA3-Enterprise
```

**Verify Connection**:
```powershell
netsh wlan show interfaces
# Look for: Authentication = WPA3-Personal/Enterprise
# Cipher = CCMP (for 128-bit) or GCMP-256 (for 192-bit)
```

---

## Network Security Hardening

### Step 1: SSID Configuration

**Option A: Broadcast SSID (Standard)**
```
[✓] Enable SSID broadcast
Allows easy discovery but visible to nearby devices
```

**Option B: Hidden SSID (Enhanced Security)**
```
[ ] Enable SSID broadcast - UNCHECK
Requires manual SSID entry
Not foolproof (still discoverable via probe requests)
Recommended: Combine with MAC filtering
```

**Recommendation**: Use hidden SSID + MAC filtering for sensitive environments

### Step 2: MAC Filtering Setup

**Get MAC Addresses**:
```
Windows: ipconfig /all → Physical Address
macOS: ifconfig | grep HWaddr
Linux: ip link show
```

**Configure Router**:
```
Path: Security → MAC Filtering
Mode: Whitelist allowed devices
Add MAC addresses:
  AA:BB:CC:DD:EE:01 - John's Laptop
  AA:BB:CC:DD:EE:02 - Server Room Device
  AA:BB:CC:DD:EE:03 - Conference Room iPad
  etc.
Save: Apply changes
```

**Note**: MAC addresses can be spoofed; use as additional layer, not sole protection

### Step 3: Channel Configuration

**2.4GHz Band**:
```
Non-overlapping channels: 1, 6, or 11
Recommended: 1 (if 2.4GHz used for legacy devices)
Auto: System selects based on congestion
```

**5GHz Band**:
```
Channel width: 80MHz or 160MHz (40MHz only if must)
Wider channels = faster but more interference
Recommended: 80MHz for best balance
```

**6GHz Band (WiFi 6E)**:
```
If available on your router: Enable
Many channels available, less congestion
Future-proof choice
```

### Step 4: DHCP & IP Configuration

```
Router Admin → Network Settings → DHCP
DHCP Range: 192.168.1.100 to 192.168.1.254
Lease Time: 24 hours (86400 seconds)
Gateway: 192.168.1.1
DNS: See DNS section below
```

---

## VPN Integration

### Step 1: Verify VPN Availability

- [ ] VPN service subscribed or on-premises VPN configured
- [ ] VPN client installed (WireGuard, OpenVPN, Cisco AnyConnect, etc.)
- [ ] VPN credentials available
- [ ] Kill-switch capability available in client
- [ ] Split tunneling rules defined

### Step 2: Configure VPN Policy

**For Public Networks**:
```json
{
  "networkType": "public",
  "vpnRequired": true,
  "killSwitch": true,
  "allowSplitTunneling": false,
  "timeoutSeconds": 30
}
```

**For Corporate Networks**:
```json
{
  "networkType": "corporate",
  "vpnRequired": false,
  "reason": "802.1X enterprise security sufficient",
  "fallbackToVPN": "if-802.1X-unavailable"
}
```

**For Home Networks**:
```json
{
  "networkType": "home",
  "vpnRequired": false,
  "vpnOptional": true,
  "killSwitch": false
}
```

### Step 3: Test VPN Connection

**Connect to VPN**:
```powershell
# Command line (example for WireGuard)
wg-quick up "config-name"

# Or use GUI client
VPN App → Connect
```

**Verify Connection**:
```
Check: External IP address changed
Verify: DNS leaks - visit dnsleaktest.com
Test: Kill-switch by disconnecting VPN
Expected: Internet access blocked immediately
```

**Reconnect**:
```
VPN should auto-reconnect within 30 seconds
If failed: Alert administrator
```

---

## Monitoring Setup

### Step 1: Configure Monitoring

**In monitoring system**:
```
Path: Monitoring → Network → WiFi
Add thresholds:
  Signal Strength Alert: < -75 dBm
  Latency Alert: > 150 ms
  Packet Loss Alert: > 2%
  Uptime Target: > 95%
```

### Step 2: Setup Alerting

**Alert Recipients**:
```
Primary: Network Team Email
Secondary: On-Call Engineer
Escalation: Network Manager (if > 1 hour down)
```

**Alert Examples**:
```
Subject: WiFi Signal Critical - Building A
Level: High
Time: 2:15 PM
Location: Conference Room 2B
Signal: -85 dBm (threshold: -75)
Action: Move closer to AP or deploy extender

---

Subject: WiFi Latency High - Entire Network
Level: Medium
Time: 4:42 PM
Latency: 185 ms (threshold: 150 ms)
Source: Likely interference detected
Action: Check for microwaves, check channel congestion
```

### Step 3: Configure Dashboard

**Dashboard Elements**:
```
1. Signal Strength (24h graph)
   - Current: -45 dBm (Excellent)
   - Average: -52 dBm
   - Trend: Stable

2. Connection Quality
   - Bandwidth: 150 Mbps
   - Latency: 25 ms
   - Packet Loss: 0.1%

3. Device Count
   - Connected: 24 devices
   - Active: 18 devices
   - Idle: 6 devices

4. Recent Alerts
   - None active
   - Last 7 days: 2 high latency alerts
```

---

## Incident Response

### Scenario 1: Signal Strength Critical

**Symptoms**:
- Signal: < -80 dBm
- Connections dropping
- Users complaining "slow WiFi"

**Response Steps**:
1. **Check Location**: Is affected area expected to have weak signal?
2. **Check for Interference**: 
   ```
   Use WiFi analyzer app to check for overlapping networks
   Check for microwaves, cordless phones running
   ```
3. **Check Access Points**:
   ```
   Verify all APs powered on and responsive
   Check for APs with failed status lights
   Restart any unresponsive APs
   ```
4. **Short Term**: 
   - Advise users to use wired connection or move closer to AP
   - Consider temporary mobile hotspot for critical users
5. **Long Term**:
   - Deploy WiFi extender or additional AP in weak area
   - Optimize AP channel selection
   - Upgrade to higher-power AP if budget allows

### Scenario 2: High Packet Loss

**Symptoms**:
- Packet Loss: > 3%
- Video calls dropping
- Timeouts on file transfers

**Response Steps**:
1. **Identify Cause**:
   - Check WiFi congestion (too many devices on same channel)
   - Check for interference (microwave in use, baby monitor, etc.)
   - Check for interference from neighboring networks (channel overlap)
2. **Immediate Actions**:
   - Switch affected users to 5GHz band (less congestion)
   - Reduce number of connected devices
   - Disable non-essential WiFi on nearby devices
3. **Investigation**:
   ```
   Use spectrum analyzer to identify interference
   Check AP logs for error rates
   Monitor signal strength trending
   ```
4. **Resolution**:
   - Change channel to less-congested option
   - Move interfering devices away from WiFi
   - If interference from neighboring network, coordinate with IT neighbor

### Scenario 3: VPN Connection Failed

**Symptoms**:
- VPN drops intermittently
- "Kill-switch activated" alert
- No internet access

**Response Steps**:
1. **Check VPN Status**:
   ```
   VPN Client → Status
   Show: Last connection attempt time
   Error: Any error messages?
   ```
2. **Check Connectivity**:
   - Disconnect VPN
   - Test basic internet access (can you reach google.com?)
   - If no access, it's network problem, not VPN
3. **Restart VPN Client**:
   ```
   Close VPN application completely
   Wait 10 seconds
   Reopen and reconnect
   ```
4. **Check VPN Server**:
   - Is VPN server up? (Check provider status page)
   - Any network maintenance in progress?
   - Try alternate VPN server if available
5. **Check Firewall Rules**:
   - Port for VPN protocol open? (check firewall logs)
   - ISP blocking VPN? (rare but possible on public networks)
6. **Escalate if Needed**:
   - Contact VPN provider technical support
   - Provide connection logs for troubleshooting

### Scenario 4: MAC Filtering Blocking Legitimate Device

**Symptoms**:
- Device can see SSID but can't connect
- Error: "Cannot connect to network"
- Other identical device model connects fine

**Response Steps**:
1. **Get MAC Address** (user's device):
   ```
   Windows: ipconfig /all
   macOS: System Preferences → Network → WiFi Details → MAC
   ```
2. **Verify MAC Whitelisted**:
   - Access router admin console
   - Check MAC filtering whitelist
   - Is MAC listed?
3. **If Not Listed**:
   - Add MAC to whitelist
   - Save configuration
   - User should be able to connect within 1 minute
4. **If Listed But Still Failing**:
   - MAC address might have changed (re-clone on boot?)
   - Try removing/re-adding MAC
   - Check if device has multiple network interfaces (virtual adapters)

---

## Troubleshooting Guide

### General Connection Issues

**Problem**: "Cannot see SSID"
```
Cause: SSID broadcast disabled or AP offline
Solution: 
  1. Verify SSID broadcast enabled on router
  2. Check AP power and network connection
  3. Try manual SSID entry if configured as hidden
  4. Restart WiFi adapter on client device
```

**Problem**: "Connected but no internet"
```
Cause: Network connected but no WAN access
Solution:
  1. Restart router and modem
  2. Check WAN connection light on router
  3. Verify DHCP is active and assigning addresses
  4. Check if client got valid IP (ipconfig)
  5. Try pinging gateway IP address
```

**Problem**: "Slow internet speed"
```
Cause: Multiple factors
Solution:
  1. Run speed test (speedtest.net)
  2. Compare to expected speed
  3. Check WiFi signal strength (-50dBm = excellent)
  4. Switch to less congested channel
  5. Move closer to AP
  6. Use 5GHz band instead of 2.4GHz
  7. Check for interference (WiFi analyzer)
```

### Security-Related Issues

**Problem**: "WPA3 authentication fails"
```
Cause: Password error or client doesn't support WPA3
Solution:
  1. Verify password entered correctly (case sensitive)
  2. Check if client device supports WPA3
  3. Temporarily switch to WPA2 to test
  4. If client doesn't support WPA3, update drivers
```

**Problem**: "MAC filtering blocking access"
```
Cause: Device MAC not whitelisted
Solution:
  1. Get device MAC address
  2. Add to whitelist in router admin console
  3. Restart both router and client
  4. Try connecting again
```

**Problem**: "DoH/DNS resolution slow"
```
Cause: Selected DoH server is far or slow
Solution:
  1. Use DNS latency measurement tool
  2. Select faster DNS server
  3. Clear DNS cache
  4. Check if ISP is interfering with DNS
  5. Fall back to regular DNS temporarily
```

### Performance Optimization

**Problem**: "Band steering not working"
```
Cause: Band steering disabled or not configured
Solution:
  1. Verify band steering enabled on router
  2. Check if device supports 5GHz
  3. Ensure 5GHz and 2.4GHz using same SSID
  4. Manually force 5GHz connection as test
  5. Check router documentation for band steering settings
```

**Problem**: "Compression not reducing data size"
```
Cause: Content already compressed (video, images)
Solution:
  1. Verify content type
  2. Skip compression for pre-compressed formats
  3. Enable compression only for text/JSON
  4. Monitor actual compression rate
  5. Consider if compression overhead worth it
```

---

## Regular Maintenance

### Daily Tasks
- [ ] Monitor alert dashboard
- [ ] Check for critical alerts
- [ ] Verify VPN status active
- [ ] Quick spot-check of signal strength

### Weekly Tasks
- [ ] Review WiFi performance metrics
- [ ] Check for interference trends
- [ ] Verify all APs operational
- [ ] Review connection logs

### Monthly Tasks
- [ ] Generate performance report
- [ ] Review security logs
- [ ] Update firmware if available
- [ ] Test failover scenarios
- [ ] Review recommendations

### Quarterly Tasks
- [ ] Full security audit
- [ ] Penetration testing
- [ ] Update WPA3 configuration if needed
- [ ] Review and update MAC whitelist
- [ ] Check certificate validity (if used)

---

## Emergency Procedures

### Network Down - Emergency Access

**If Primary WiFi Completely Down**:
1. Activate backup WiFi SSID (separate radio)
2. Provide credentials to critical users only
3. Troubleshoot primary network
4. Provide updates every 30 minutes
5. Switch back to primary when operational

**Documented Credentials** (stored securely):
```
SSID: BackupWiFi-Temporary
Password: [ENCRYPTED - SEE VAULT]
Valid: Until primary restored
Access: Critical users only
```

### Rollback Procedure

**If WPA3 Implementation Causes Widespread Issues**:
1. **Immediate**: Switch back to WPA2 via router admin
2. **Notify Users**: Email explaining change
3. **Troubleshoot**: Identify what caused issue
4. **Update Timeline**: Plan phased WPA3 rollout
5. **Post-Mortem**: Document lessons learned

---

## Compliance & Audit

### Security Standards
- [ ] WPA3 or WPA2 (WPA2 acceptable interim measure)
- [ ] Minimum 128-bit encryption
- [ ] MAC filtering on sensitive networks
- [ ] VPN for public network access
- [ ] DNSSEC validation enabled
- [ ] DoH for DNS security
- [ ] Monthly security reviews

### Documentation
- [ ] Network topology diagram
- [ ] AP location map
- [ ] Channel assignment plan
- [ ] MAC whitelist (stored securely)
- [ ] VPN configuration (stored securely)
- [ ] Incident logs
- [ ] Performance metrics archive

---

## Sign-Off

**Deployment Checklist Completed**: _____ (Date)
**By**: _____ (Name/Title)
**Verified By**: _____ (Manager Name/Title)
**Approved By**: _____ (CISO/Security Officer)

---

**Document Version**: 1.0
**Last Updated**: 2025-01-15
**Next Review**: 2025-04-15
