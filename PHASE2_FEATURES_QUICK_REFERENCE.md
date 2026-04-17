# 🎯 PHASE 2 NEW FEATURES - QUICK REFERENCE GUIDE

## AI & LLM INTEGRATION

### Local LLM Models Supported
```csharp
// Automatically supported models
var llm = new LlmFramework();

// Small, fast models (< 5GB VRAM)
llm.RegisterModel("gpt2", new GptModel(quantize: true));           // 1.5GB
llm.RegisterModel("phi-2.7b", new PhiModel(quantize: true));       // 3GB

// Medium models (5-10GB VRAM)
llm.RegisterModel("mistral-7b", new MistralModel(quantize: true)); // 7GB
llm.RegisterModel("alpaca-7b", new AlpacaModel(quantize: true));   // 7GB

// Large models (10GB+ VRAM) - multi-GPU
llm.RegisterModel("llama-13b", new LlamaModel(size: "13b"));       // 13GB
llm.RegisterModel("llama-70b", new LlamaModel(size: "70b"));       // 70GB (multi-GPU)

// Auto-select best model for hardware
var response = await llm.InferAsync("What is HELIOS?", model: "auto");
// → Selects best available model (Phi if 3GB, Mistral if 7GB, etc)
```

### Token Optimization
```csharp
var tokenMgr = new TokenOptimization();

// Budget-aware inference
var result = await tokenMgr.InferWithBudgetAsync(
    prompt: "Long prompt with lots of context...",
    maxTokens: 512,
    compressionLevel: CompressionLevel.Medium
);
// → Automatically compresses context to fit budget
```

---

## HARDWARE INTEGRATION

### CUDA Acceleration
```csharp
var cuda = new CudaRuntime();

// Detect GPU capabilities
var devices = cuda.DetectDevices();
foreach (var device in devices)
{
    Console.WriteLine($"GPU: {device.Name}, VRAM: {device.Memory}GB, Compute: {device.ComputeCapability}");
}

// Multi-GPU operations
var multiGpu = new MultiGpuOrchestrator();
await multiGpu.DistributeWorkload(tasks, devices);
// → Automatically splits work across GPUs
```

### Driver Management
```csharp
var driverMgr = new DriverManager();

// Scan for driver updates
var updates = await driverMgr.ScanForUpdatesAsync();
foreach (var update in updates)
{
    Console.WriteLine($"Update available: {update.Name} v{update.NewVersion}");
}

// Automatic installation
await driverMgr.InstallUpdatesAsync(updates, 
    rollbackOnFailure: true,
    silentMode: true
);
```

### Razer Integration
```csharp
var razer = new RazerDeviceManager();

// Get connected devices
var devices = razer.GetConnectedDevices();
// → Returns mice, keyboards, headsets, mousepads

var chroma = new ChromaController();

// Set lighting based on system status
if (systemHealth.Status == SystemHealthStatus.Critical)
{
    await chroma.SetColorAllDevicesAsync(Color.Red, BreathingMode);
}
else if (systemHealth.Status == SystemHealthStatus.Warning)
{
    await chroma.SetColorAllDevicesAsync(Color.Yellow, SpectrumMode);
}
else
{
    await chroma.SetColorAllDevicesAsync(Color.Green, StaticMode);
}
```

---

## SAFETY & RECOVERY

### DevDrive Sandbox
```csharp
var sandbox = new SandboxManager();

// Create isolated environment
var snapshot = await sandbox.CreateSnapshotAsync("dev-env-v1");
// → Creates complete environment snapshot

// Make changes (safe - isolated)
// ...installation, configuration, testing...

if (somethingWentWrong)
{
    // Rollback instantly
    await sandbox.RestoreSnapshotAsync(snapshot);
    // → Environment restored to exact state
}
```

### Vault Recovery
```csharp
var vault = new VaultManager();

// Schedule incremental backups
vault.ScheduleBackup(
    frequency: BackupFrequency.Daily,
    time: "02:00 AM",
    target: BackupTarget.Cloud,
    encryption: EncryptionType.AES256
);

// Point-in-time recovery
var recovery = await vault.RecoverAsync(
    timestamp: DateTime.Now.AddDays(-7),
    paths: new[] { "C:\\ImportantData\\*" }
);
```

---

## SOFTWARE AUTOMATION

### Package Management
```csharp
var software = new SoftwareManager();

// Install software with configuration
var config = new SoftwareInstallConfig
{
    Packages = new[]
    {
        new Package { Name = "VSCode", Version = "latest", Config = new { Extensions = new[] { "csharp", "remote-wsl" } } },
        new Package { Name = "Python", Version = "3.11", Config = new { AddToPath = true } },
        new Package { Name = "Git", Version = "latest" }
    },
    ParallelInstalls = 3,
    RollbackOnFailure = true
};

await software.BulkInstallAsync(config);
// → Installs all packages with configuration
```

### Update Management
```csharp
var updates = await software.CheckForUpdatesAsync();
// → Scans all installed software for updates

await software.ApplyUpdatesAsync(
    updates: updates,
    schedule: ScheduleType.OffPeak,
    notification: true
);
// → Schedules updates for off-peak hours
```

---

## WSL2 & CROSS-PLATFORM

### WSL2 Management
```csharp
var wsl2 = new Wsl2Manager();

// Set up Linux environment
await wsl2.ProvisionDistributionAsync(
    distribution: "Ubuntu",
    version: "22.04",
    defaultUser: "helios"
);

// Run Linux commands from Windows
var result = await wsl2.ExecuteCommandAsync("apt update && apt upgrade -y");
```

### Hermes Agent Framework
```csharp
var agentHost = new HermesAgentHost();

// Deploy Linux agents
var agents = new[]
{
    new HermesAgent { Name = "processor", Role = AgentRole.DataProcessing },
    new HermesAgent { Name = "analytics", Role = AgentRole.Analytics },
    new HermesAgent { Name = "background", Role = AgentRole.BackgroundJobs }
};

await agentHost.DeployAgentsAsync(agents);
// → Agents run in WSL2 Linux environment
// ↔ Communicate with Windows main system
```

---

## CLI COMMANDS FOR NEW FEATURES

### AI & LLM Commands
```bash
helios-cli ai models list                                    # List available models
helios-cli ai model download --name "llama-7b"              # Download a model
helios-cli ai infer --model "auto" --prompt "What is AI?"   # Run inference
helios-cli ai token-budget set --tokens 512                 # Set token limit
helios-cli ai agent profile                                 # Profile agent performance
```

### Hardware Commands
```bash
helios-cli hardware gpu list                                # List GPUs
helios-cli hardware driver scan                             # Scan for driver updates
helios-cli hardware driver install --gpu nvidia             # Install GPU drivers
helios-cli hardware razer list                              # List Razer devices
helios-cli hardware razer color --device all --color red    # Set Razer lighting
```

### Safety Commands
```bash
helios-cli devdrive snapshot create --name "backup-2026"    # Create snapshot
helios-cli devdrive snapshot restore --name "backup-2026"   # Restore snapshot
helios-cli vault backup start --target cloud                # Start backup
helios-cli vault recover --timestamp "2026-04-16 15:00"     # Recover from time
```

### Software Commands
```bash
helios-cli software scan                                    # Scan installed software
helios-cli software check-updates                           # Check for updates
helios-cli software install --file packages.yaml            # Bulk install
helios-cli software update --all                            # Update all packages
```

### WSL2 Commands
```bash
helios-cli wsl2 provision --distro ubuntu                   # Set up WSL2
helios-cli wsl2 agent deploy --agent processor              # Deploy Linux agent
helios-cli wsl2 command "apt update"                        # Run Linux command
```

---

## INTEGRATION ARCHITECTURE

```
┌─────────────────────────────────────────────────────────────┐
│                  AI DASHBOARD GUI (Central)                  │
│                                                             │
│  ├─ Real-time Model Management                              │
│  ├─ Performance Monitoring                                  │
│  ├─ Workflow Builder                                        │
│  └─ System Status Display                                   │
└─────────────────────────────────────────────────────────────┘
                           ↕
     ┌─────────────────────────────────────────────────┐
     │           INTEGRATION LAYER                     │
     │                                                 │
     ├─ LLM Framework ← CUDA Acceleration             │
     ├─ Driver Manager → GPU Optimization             │
     ├─ Token Manager → Context Management             │
     ├─ Agent Profiler → Performance Learning          │
     └─ Razer Controller → Device Sync                │
     │
     ├─────────────────────────────────────────────────┤
     │         SYSTEM SERVICES                         │
     │                                                 │
     ├─ DevDrive Sandbox Protection                    │
     ├─ Vault Backup & Recovery                        │
     ├─ Software Lifecycle Automation                  │
     └─ WSL2 Cross-Platform Bridge                     │
     │
     └─────────────────────────────────────────────────┘
```

---

## PERFORMANCE EXPECTATIONS

### AI/LLM Performance
```
GPT-2 (quantized):        50-100 tokens/sec  (1.5GB VRAM)
Phi 2.7B (quantized):     30-50 tokens/sec   (3GB VRAM)
Mistral 7B (quantized):   20-40 tokens/sec   (7GB VRAM)
LLAMA 13B (quantized):    10-20 tokens/sec   (13GB VRAM)
LLAMA 70B (multi-GPU):    5-15 tokens/sec    (70GB VRAM)

Token optimization:       30-40% overhead    (compression)
Agent profiling:          <1ms per sample
```

### Hardware Operations
```
GPU detection:           <1 second
Driver scan:             30-60 seconds
Driver install:          2-10 minutes (depends on driver)
CUDA kernel compilation: 5-30 seconds (first run cached)
Razer device detection:  <500ms
```

### Recovery Operations
```
Snapshot creation:       2-5 minutes
Snapshot restore:        2-5 minutes
Backup (incremental):    1-2 minutes
Full system restore:     5-15 minutes
Point-in-time recovery:  <1 minute (file selection)
```

### Software Operations
```
Software scan:           30-60 seconds (depends on count)
Update check:            2-3 minutes
Single install:          30 seconds - 5 minutes
Bulk install (10 apps):  10-30 minutes (parallel)
```

---

## CONFIGURATION TEMPLATES

### LLM Configuration
```yaml
# ~/.helios/config/llm.yaml
models:
  gpt2:
    enabled: true
    quantize: true
    cacheSize: 2GB
  
  mistral-7b:
    enabled: true
    quantize: true
    cacheSize: 6GB
    gpu: auto

tokenOptimization:
  defaultBudget: 512
  compressionLevel: medium
  contextWindow: 256

inference:
  batchSize: 1
  maxConcurrent: 2
  timeout: 60s
```

### Hardware Configuration
```yaml
# ~/.helios/config/hardware.yaml
cuda:
  enabled: true
  defaultDevice: 0
  memoryPool: true

drivers:
  autoUpdate: true
  checkInterval: 7d
  rollbackOnFailure: true
  silent: true

razer:
  enabled: true
  syncWithSystem: true
  brightnessModes:
    idle: 50%
    working: 100%
    alerting: 100%

gpu:
  optimization: aggressive
  powerMode: balanced
  monitoringInterval: 5s
```

### Backup Configuration
```yaml
# ~/.helios/config/backup.yaml
devdrive:
  enabled: true
  maxSnapshots: 10
  autoCleanup: true
  
vault:
  enabled: true
  encryption: aes256
  compression: zip
  
backups:
  - schedule: daily
    time: "02:00 AM"
    target: cloud
    retention: 30d
    
  - schedule: weekly
    time: "Sunday 00:00"
    target: external
    retention: 90d
```

---

## TROUBLESHOOTING GUIDE

### LLM Issues
```
Problem: Model inference slow
Solution: 
  1. Check GPU usage: helios-cli hardware gpu status
  2. Reduce token budget: helios-cli ai token-budget set --tokens 256
  3. Switch to smaller model: helios-cli ai infer --model "phi-2.7b"
  4. Enable quantization: config llm.yaml → quantize: true

Problem: Out of memory
Solution:
  1. Reduce batch size: config llm.yaml → batchSize: 1
  2. Reduce context window: config llm.yaml → contextWindow: 128
  3. Enable memory pooling: config hardware.yaml → memoryPool: true
```

### Hardware Issues
```
Problem: GPU not detected
Solution:
  1. Check drivers: helios-cli hardware driver scan
  2. Install CUDA: helios-cli hardware cuda install
  3. Verify GPU: nvidia-smi (if NVIDIA)

Problem: Driver installation failed
Solution:
  1. Check logs: ~\.helios\logs\driver-install.log
  2. Manual rollback: helios-cli hardware driver rollback
  3. Try different version: helios-cli hardware driver install --version 525.125
```

### WSL2 Issues
```
Problem: WSL2 not found
Solution:
  1. Enable WSL: wsl --install
  2. Restart computer
  3. Provision: helios-cli wsl2 provision --distro ubuntu

Problem: Agent not responding
Solution:
  1. Check WSL2: wsl -l -v
  2. Restart agent: helios-cli wsl2 agent restart --agent processor
  3. Check logs: wsl -d Ubuntu -- tail -f /var/log/helios-agent.log
```

---

## NEXT STEPS AFTER PHASE 2

After Phase 2 completes (T+60 min), you will have:

✅ **AI Intelligence Ready**
- 7 LLM models available
- Token-efficient inference
- Agent learning system
- Visual workflow builder

✅ **Hardware Optimized**
- CUDA acceleration enabled
- All drivers current
- WSL2 operational
- Razer devices integrated

✅ **System Protected**
- DevDrive sandbox ready
- Vault backups automated
- Recovery tested
- Disaster recovery plan

✅ **Software Managed**
- 500+ packages available
- Auto-updates configured
- License tracking active
- Dependency resolution working

---

## 📞 SUPPORT RESOURCES

- **Documentation**: `~\.helios\docs\` (230,000+ words)
- **Examples**: `~\.helios\examples\`
- **Configuration**: `~\.helios\config\`
- **Logs**: `~\.helios\logs\`
- **Troubleshooting**: `~\.helios\docs\TROUBLESHOOTING.md`

