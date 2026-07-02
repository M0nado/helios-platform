# USB Builder Architecture
## Monado Blade Multi-Stage USB Builder Wizard
**Phase 1D** | Production-Ready Design | v1.0

---

## Executive Summary

Complete multi-step USB builder wizard for Monado Blade system deployment. 8-stage process managing:
- USB device detection and validation
- 9-partition intelligent setup with configurable sizes
- Windows OS deployment
- Driver installation (GPU, chipset, Razer, audio)
- Program ecosystem deployment
- AI/ML hub configuration
- System hardening and optimization
- Complete readiness validation

**Target Build Time**: 2-4 hours | **Deployment Target**: External USB/NVMe (500GB-2TB)

---

## Stage 1: USB Detection & Preparation (Duration: 5-10 minutes)

### Purpose
Safely identify target USB device, validate capacity, prevent accidental data loss, and prepare for formatting.

### Process Flow

#### 1.1 Device Detection
```
Phase: Detection
Input: None
Actions:
  1. Scan connected USB/NVMe devices
  2. Filter out system drive (Windows installation drive)
  3. List detected drives with:
     - Device name (e.g., "Kingston DataTraveler")
     - Serial number
     - Capacity (GB/TB)
     - Current format (NTFS/FAT32/exFAT)
     - USB version (USB 2.0/3.0/3.1/USB-C)
     - Speed tier (5Gbps/10Gbps/20Gbps)
  4. Filter drives < 500GB (reject)
  5. Filter drives > 2TB (warn - may be slow)
Output: List of valid target devices
```

#### 1.2 Device Selection & Validation
```
Phase: User Selection
Input: User selects target device
Actions:
  1. Display selected device details
  2. Show warning if:
     - Device contains data
     - Device is partition of system disk
     - Device is write-protected
  3. Require explicit confirmation (type device name to confirm)
  4. Validate selection (prevent accidental selection)
Output: Confirmed target device
```

#### 1.3 Capacity Analysis
```
Phase: Capacity Planning
Input: Selected device capacity
Actions:
  1. Calculate total capacity: C_total
  2. Reserve system overhead: 2% (for MBR/GPT, firmware)
  3. Usable capacity: C_usable = C_total × 0.98
  4. Display breakdown:
     - Total capacity
     - System reserved
     - Usable capacity
     - Default partition schema (9 partitions)
  5. Suggest adjustments if capacity < 500GB
Output: Capacity confirmation
```

#### 1.4 Current State Backup Offer
```
Phase: Data Protection
Input: Device contains existing data
Actions:
  1. If device has data:
     - Offer backup to system drive
     - Show backup location
     - Show estimated backup time
     - Confirm backup consent
  2. If backup accepted:
     - Copy data to: C:\MonadoBackup\[TIMESTAMP]\
     - Show progress
     - Verify backup integrity
Output: Backup complete or skipped
```

#### 1.5 Format Confirmation
```
Phase: Final Confirmation
Input: All previous steps complete
Actions:
  1. Display summary:
     - Device: [name]
     - Capacity: [X] GB
     - Partitions: 9 (list schema)
     - Format: GUID Partition Table (GPT)
     - Bootable: Yes
  2. Final warning:
     "ALL DATA WILL BE DELETED. This cannot be undone."
  3. Require dual confirmation:
     - Checkbox: "I understand data will be lost"
     - Type: "FORMAT [DEVICE_NAME]" exactly
Output: Format authorization
```

### Validation Checkpoints

```
Checkpoint 1.1: Device Detection
  ✓ At least 1 valid USB device detected
  ✓ System drive excluded from list
  ✓ Capacity displayed accurately

Checkpoint 1.2: User Selection
  ✓ Selected device != system drive
  ✓ Device capacity >= 500GB
  ✓ User provided explicit confirmation

Checkpoint 1.3: Format Authorization
  ✓ Dual confirmation provided
  ✓ User typed exact device name
  ✓ Backup completed (if data existed)
```

### Error Handling

```
Error: No USB devices detected
  → Message: "No valid USB devices found. Connect a USB drive (≥500GB)"
  → Action: Retry button with 5-second refresh
  → Recovery: User connects device and retries

Error: All devices < 500GB
  → Message: "All connected drives < 500GB. Need ≥500GB"
  → Action: Suggest drive upgrade, provide Crucial/Samsung links
  → Recovery: User upgrades drive

Error: Selected device is system drive
  → Message: "Cannot format system drive. Select different device"
  → Action: Highlight safe devices, prevent confirmation
  → Recovery: Auto-select safest non-system device

Error: Device is write-protected
  → Message: "Device is write-protected. Check physical write-protect switch"
  → Action: Halt, cannot proceed
  → Recovery: User disables write protection

Error: Backup failed
  → Message: "Backup failed. Insufficient space in C:\MonadoBackup\"
  → Action: Show available space, offer alternate backup location
  → Recovery: User specifies backup location or skips
```

### User Interface Mockup

```
═══════════════════════════════════════════════════════════════════
  MONADO BLADE USB BUILDER - Stage 1: Device Preparation
═══════════════════════════════════════════════════════════════════

📱 CONNECTED DEVICES:

  [✓] Kingston DataTraveler Exodia
      Size: 512 GB | USB 3.1 | Serial: 12F3K9J0
      Current: exFAT | Status: Ready

  [✓] SanDisk Ultra NVMe
      Size: 1.0 TB | USB-C | Serial: 8H2K9L4M
      Current: NTFS | Status: Ready

  [✗] Samsung T7 (BLOCKED: system drive detected)
      Size: 2.0 TB | USB-C | Serial: 9K3L0P5Q
      Current: NTFS | Status: Cannot use

═══════════════════════════════════════════════════════════════════

📊 CAPACITY ANALYSIS:

  Total Capacity:        512 GB
  System Reserved:       10 GB (2%)
  Available for setup:   502 GB

  Default 9-partition schema will fit.

═══════════════════════════════════════════════════════════════════

🔐 FINAL CONFIRMATION:

  ☐ I understand all data on this device will be permanently deleted
  
  Type to confirm: FORMAT KINGSTON DATATRAVELER EXODIA

  [Continue] [Cancel] [Back]

═══════════════════════════════════════════════════════════════════
```

---

## Stage 2: Partition Setup (Duration: 10-15 minutes)

### Purpose
Create 9 intelligent partitions with user-configurable sizes, setup encryption (BitLocker), and establish file systems.

### 9-Partition Schema

#### Standard Configuration (512 GB device)
```
Partition 1: Windows OS
  Type: System Partition (EFI + OS)
  Size: 60 GB (11.6%)
  Format: NTFS
  BitLocker: Yes (mandatory)
  Purpose: Windows 11 + system files

Partition 2: Application Data
  Type: Data Partition
  Size: 40 GB (7.8%)
  Format: NTFS
  BitLocker: Yes (mandatory)
  Purpose: App configs, caches, logs

Partition 3: AI/ML Models
  Type: Data Partition (High-speed)
  Size: 150 GB (29.3%)
  Format: NTFS
  BitLocker: Yes (optional, slower)
  Purpose: Large LLMs (Llama, Mistral, etc.)

Partition 4: AI Cache
  Type: Cache Partition (Very High-speed)
  Size: 80 GB (15.6%)
  Format: NTFS or custom volatile
  BitLocker: No (performance priority)
  Purpose: VRAM spill, GPU cache, working memory

Partition 5: Programs & Development
  Type: Application Partition
  Size: 150 GB (29.3%)
  Format: NTFS
  BitLocker: Yes (optional)
  Purpose: VS2022, Python, Node, Docker, etc.

Partition 6: Cache & Temporary
  Type: Temp Partition (Very High-speed)
  Size: 30 GB (5.8%)
  Format: NTFS volatile
  BitLocker: No (performance priority)
  Purpose: %TEMP%, Windows cache, artifacts

Partition 7: Backup & Archive
  Type: Data Partition (Cold storage)
  Size: 0 GB (skip on 512GB device)
  Format: NTFS
  BitLocker: Yes (mandatory)
  Purpose: System backups, version history

Partition 8: Recovery/WinRE
  Type: System Partition
  Size: 8 GB (1.6%)
  Format: NTFS
  BitLocker: No (recovery access required)
  Purpose: Windows Recovery Environment + tools

Partition 9: User Profiles
  Type: Data Partition
  Size: 20 GB (3.9%)
  Format: NTFS
  BitLocker: Yes (PII)
  Purpose: User documents, Desktop, Pictures
```

#### Large Configuration (1 TB device)
```
Partition 1: Windows OS               60 GB
Partition 2: Application Data         50 GB
Partition 3: AI/ML Models            250 GB
Partition 4: AI Cache                100 GB
Partition 5: Programs & Development  200 GB
Partition 6: Cache & Temporary        50 GB
Partition 7: Backup & Archive        150 GB
Partition 8: Recovery/WinRE           10 GB
Partition 9: User Profiles            30 GB
```

#### Extra-Large Configuration (2 TB device)
```
Partition 1: Windows OS               70 GB
Partition 2: Application Data         70 GB
Partition 3: AI/ML Models            500 GB
Partition 4: AI Cache                200 GB
Partition 5: Programs & Development  400 GB
Partition 6: Cache & Temporary       100 GB
Partition 7: Backup & Archive        400 GB
Partition 8: Recovery/WinRE           15 GB
Partition 9: User Profiles            50 GB
```

### Process Flow

#### 2.1 Partition Schema Selection
```
Phase: Schema Selection
Input: Device capacity, user preferences
Actions:
  1. Auto-select schema based on capacity:
     - 500-700 GB → Standard (with P7 skipped)
     - 700-1.2 TB → Large
     - 1.2+ TB → Extra-Large
  2. Display schema with:
     - Partition number and name
     - Recommended size
     - Purpose description
     - Encryption requirement
  3. Offer "Advanced" mode for custom sizing
Output: Selected partition schema
```

#### 2.2 Custom Partition Sizing
```
Phase: Advanced Sizing (Optional)
Input: Selected schema, user customization
Actions:
  1. Display constraint rules:
     - P1 (Windows OS): 50-100 GB
     - P2 (App Data): 30-70 GB
     - P3 (AI Models): 100-500 GB
     - P4 (AI Cache): 50-200 GB
     - P5 (Programs): 100-300 GB
     - P6 (Temp): 20-100 GB
     - P7 (Backup): 0-400 GB
     - P8 (Recovery): 5-15 GB
     - P9 (Profiles): 10-50 GB
  2. Allow user to adjust sliders
  3. Validate total = usable capacity
  4. Show visual partition distribution
Output: Custom partition sizes
```

#### 2.3 Encryption Configuration
```
Phase: BitLocker Setup
Input: Partition schema with sizes
Actions:
  1. For each partition, present encryption choice:
     
     Mandatory Encryption:
       - P1 (Windows OS): FORCE encrypted
       - P2 (App Data): FORCE encrypted
       - P7 (Backup): FORCE encrypted
       - P9 (Profiles): FORCE encrypted
     
     Optional Encryption:
       - P3 (AI Models): Offer choice
         ∟ Benefits: Data privacy
         ∟ Cost: ~5-10% speed reduction
         ∟ Recommended: Enable
       - P5 (Programs): Offer choice
         ∟ Benefits: App code privacy
         ∟ Cost: Minimal (<2%)
         ∟ Recommended: Enable
     
     No Encryption (performance):
       - P4 (AI Cache): Cannot encrypt
       - P6 (Temp): Cannot encrypt
       - P8 (Recovery): Cannot encrypt
  
  2. Ask about encryption key storage:
     ☐ Store in TPM 2.0 (recommended - automatic unlock)
     ☐ Store in USB key (requires key on boot)
     ☐ Store both (best security, requires 2FA)
  
  3. Display BitLocker group policies to apply
Output: Encryption configuration
```

#### 2.4 Partition Creation
```
Phase: Creating Partitions
Input: Partition schema, encryption config
Actions:
  1. Initialize disk as GPT (GUID Partition Table)
  2. Create EFI System Partition (100 MB, hidden)
  3. For each partition:
     - Calculate start sector
     - Create partition with size
     - Assign drive letter (D:, E:, F:, etc.)
     - Label with partition name
     - Set file system (NTFS)
     - Format volume
     - Display progress bar
  4. Show status:
     - Partition 1/9: Creating "Windows OS"... ████░░░░░ 45%
     - Partition 2/9: Queued...
     - Partition 3/9: Queued...
  5. Verify each partition created
Output: All 9 partitions created and formatted
```

#### 2.5 BitLocker Encryption
```
Phase: Enabling BitLocker
Input: Formatted partitions, encryption config
Actions:
  1. For each partition with encryption enabled:
     - Enable BitLocker
     - Set encryption method:
       ∟ XTS-AES 256-bit (recommended)
       ∟ XTS-AES 128-bit (if legacy required)
     - Store recovery key:
       ∟ If TPM+USB: Save to USB encrypted\bitlocker_keys\
       ∟ If TPM only: Store TPM-backed key
     - Display encryption progress
  2. For TPM-backed partitions:
     - Clear TPM (fresh state)
     - Initialize TPM 2.0
     - Create TPM seal
  3. Show recovery codes:
     - Generate 3x recovery codes
     - Save to: C:\MonadoBackup\bitlocker_recovery.txt
     - Print QR code for emergency recovery
Output: All partitions encrypted and sealed
```

#### 2.6 Partition Verification
```
Phase: Validation
Input: All partitions created and encrypted
Actions:
  1. For each partition, verify:
     - Partition exists and is accessible
     - File system is intact (chkdsk /scan)
     - BitLocker status (if encrypted)
     - Free space = expected
     - Drive letter assigned
  2. Verify mount points:
     - D: = Windows OS (60 GB, NTFS)
     - E: = App Data (40 GB, NTFS, encrypted)
     - F: = AI Models (150 GB, NTFS)
     - G: = AI Cache (80 GB, NTFS)
     - H: = Programs (150 GB, NTFS, encrypted)
     - I: = Temp (30 GB, NTFS)
     - J: = (Backup, encrypted)
     - K: = Recovery (8 GB, NTFS)
     - L: = Profiles (20 GB, NTFS, encrypted)
  3. Display summary table
Output: All partitions validated ✓
```

### Validation Checkpoints

```
Checkpoint 2.1: Schema Selection
  ✓ Schema matches device capacity
  ✓ All 9 partitions listed with sizes
  ✓ Total partition size = available capacity
  ✓ Constraints respected

Checkpoint 2.2: Partition Creation
  ✓ 9 partitions created successfully
  ✓ Each partition formatted (NTFS)
  ✓ Each partition assigned drive letter
  ✓ Each partition accessible via Explorer
  ✓ Free space on each partition matches expected

Checkpoint 2.3: BitLocker Encryption
  ✓ Mandatory partitions encrypted (P1, P2, P7, P9)
  ✓ Selected optional partitions encrypted
  ✓ Recovery keys saved to backup location
  ✓ TPM seal verified
  ✓ Encryption progress 100% for all encrypted partitions

Checkpoint 2.4: Full Partition Validation
  ✓ chkdsk /scan passes on all partitions
  ✓ All 9 mount points correct
  ✓ Total capacity = capacity detected
  ✓ BitLocker status correct per partition
```

### Error Handling

```
Error: Cannot create partition (disk is full)
  → Message: "Cannot allocate partition (total exceeds device capacity)"
  → Action: Auto-recalculate partition sizes proportionally
  → Recovery: Display adjusted schema, offer to proceed

Error: BitLocker encryption failed
  → Message: "BitLocker encryption failed on Partition [X]: [reason]"
  → Possible reasons:
     - TPM not detected (but mandatory)
     - Insufficient disk space for encryption metadata
     - Drive is write-protected
  → Action: If mandatory partition fails: HALT, cannot continue
  → Recovery: Fix TPM, retry, or use USB key backup

Error: Partition verification failed (bad sectors)
  → Message: "Partition [X] failed verification: Bad sectors detected"
  → Action: Offer to repair with chkdsk /F
  → Recovery: Run repair, re-verify, or reject partition

Error: Drive letter assignment failed
  → Message: "Cannot assign drive letter [X:] (already in use)"
  → Action: Auto-select next available letter
  → Recovery: Automatically continue with adjusted letter

Error: BitLocker recovery key save failed
  → Message: "Cannot save recovery key to C:\MonadoBackup\"
  → Action: Offer alternate save location (USB, cloud, email)
  → Recovery: User specifies location and provides path
```

### User Interface Mockup

```
═══════════════════════════════════════════════════════════════════
  MONADO BLADE USB BUILDER - Stage 2: Partition Setup
═══════════════════════════════════════════════════════════════════

📊 PARTITION SCHEMA (512 GB Device - Auto-Selected):

  Partition 1:  Windows OS              60 GB  🔒 Encrypted
  Partition 2:  Application Data        40 GB  🔒 Encrypted
  Partition 3:  AI/ML Models           150 GB  ○ Optional
  Partition 4:  AI Cache                80 GB  ✗ No encryption
  Partition 5:  Programs               150 GB  ○ Optional
  Partition 6:  Cache/Temp              30 GB  ✗ No encryption
  Partition 7:  Backup                   0 GB  (Skipped on 512GB)
  Partition 8:  Recovery/WinRE           8 GB  ✗ No encryption
  Partition 9:  User Profiles           20 GB  🔒 Encrypted
                                        ─────
                                   Total: 538 GB (exceeds by 36 GB)

  [Advanced Mode] [Use Defaults] [Cancel]

═══════════════════════════════════════════════════════════════════

🔐 ENCRYPTION SETTINGS:

  Optional partitions - enable encryption?

  Partition 3 (AI Models):
    Size: 150 GB | Speed impact: ~5% slower | Recommended: YES
    ☑ Enable encryption

  Partition 5 (Programs):
    Size: 150 GB | Speed impact: <2% | Recommended: YES
    ☑ Enable encryption

═══════════════════════════════════════════════════════════════════

⏳ CREATING PARTITIONS:

  ████████████████████░░░░░░░░░░░░░░░░░░░░░░░░░░░░░  45%

  Partition 3/9: Formatting "AI/ML Models"...
  Partition 4/9: Queued...
  Partition 5/9: Queued...

  Time elapsed: 4:23 | Estimated: 8:45 remaining

═══════════════════════════════════════════════════════════════════
```

---

## Stage 3: OS Installation (Duration: 15-25 minutes)

### Purpose
Deploy Windows 11 (Pro or Enterprise) to Partition 1, install boot loader, configure UEFI, and enable Secure Boot.

### Process Flow

#### 3.1 Windows Installation Media Preparation
```
Phase: Media Preparation
Input: Windows 11 ISO (must be provided by user)
Actions:
  1. Verify Windows 11 ISO file:
     - Check file exists: C:\MonadoISO\Win11.iso (or user-specified)
     - Verify file size: 5-7 GB
     - Verify SHA256 hash matches official Microsoft hash
  2. If ISO not found:
     - Offer to download from Microsoft Media Creation Tool
     - Show progress
     - Verify downloaded ISO
  3. Mount ISO as virtual drive
  4. Extract Windows installation files
Output: Windows installation files ready
```

#### 3.2 Partition 1 Preparation
```
Phase: Target Preparation
Input: Windows installation files, Partition 1 (60 GB)
Actions:
  1. Verify Partition 1 (D:) is ready:
     - Size >= 50 GB
     - Formatted as NTFS
     - BitLocker encrypted
     - Free space >= 45 GB
  2. Create Windows directory structure:
     - D:\Windows\ (system files)
     - D:\Program Files\ (legacy apps)
     - D:\Program Files (x86)\ (legacy apps 32-bit)
     - D:\Windows\System32\ (system DLLs)
     - D:\Windows\Drivers\ (hardware drivers)
  3. Reserve space for:
     - Windows updates (15 GB)
     - System restore (5 GB)
Output: Partition 1 prepared with directory structure
```

#### 3.3 Windows File Copy
```
Phase: File Installation
Input: Windows files, Partition 1 prepared
Actions:
  1. Copy core Windows files to D:\Windows\:
     - System files (kernel, drivers, etc.)
     - UI components
     - Registry hives
     - System configuration
  2. Show progress:
     ║ Copying Windows files... ████████░░░░░░░░░░░░░░ 40%
     ║ Files: 15,000+ / 40,000
     ║ Time elapsed: 5:30 | Remaining: ~8:00
  3. Verify each critical file:
     - kernel.exe (Windows kernel)
     - ntoskrnl.exe (NT kernel components)
     - drivers\* (critical drivers)
Output: All Windows files copied to Partition 1
```

#### 3.4 Boot Loader Installation
```
Phase: Boot Loader Setup
Input: Windows files on Partition 1
Actions:
  1. Install Windows Boot Manager (bootmgr)
     - Location: EFI System Partition (100 MB, hidden)
     - Configuration: C:\Boot\BCD (Boot Configuration Data)
  2. Create Boot Configuration Data (BCD):
     - Default entry: Windows 11 on D:\
     - Boot timeout: 3 seconds
     - Safe mode entries (for recovery)
     - Recovery mode entry
  3. Install Windows PE boot environment
  4. Configure boot options in BCD:
     - Safe Mode
     - Safe Mode with Command Prompt
     - Safe Mode with Networking
     - Last Known Good Configuration
     - System Repair Disk
Output: Boot manager installed and configured
```

#### 3.5 UEFI Configuration
```
Phase: UEFI Boot Setup
Input: Boot loader installed
Actions:
  1. Configure UEFI firmware settings:
     - Set boot device order:
       1st: Windows Boot Manager (on target device)
       2nd: USB/Fallback (for recovery)
       3rd: Network boot (disabled by default)
     - Disable legacy BIOS (pure UEFI mode)
     - Enable Secure Boot (with Microsoft certificates)
     - Set boot timeout: 3 seconds
  2. TPM Configuration:
     - Enable TPM 2.0 in UEFI
     - Set TPM to Full Mode (not Reduced)
     - Enable TPM auto-clear on power loss
  3. Memory Protection:
     - Enable XD Execution Prevention
     - Enable Data Execution Prevention (DEP)
     - Enable Address Space Layout Randomization (ASLR)
  4. Store UEFI configuration
Output: UEFI configured for Windows Boot Manager
```

#### 3.6 Secure Boot Configuration
```
Phase: Secure Boot
Input: UEFI configured with boot loader
Actions:
  1. Enable Secure Boot:
     - Mode: Strict (reject unsigned code)
     - Database: Microsoft + OEM keys
     - Clear any conflicting 3rd-party keys
  2. Register Windows Boot Manager signature:
     - Microsoft Windows Production PCA 2011
     - Microsoft Windows Verification PCA
  3. Configure Secure Boot policy:
     - Allow: Windows boot files
     - Allow: Critical drivers (chipset, GPU, audio)
     - Deny: Old bootkit signatures
  4. Test Secure Boot:
     - Boot device
     - Verify Secure Boot active (no warnings)
Output: Secure Boot enabled and verified
```

#### 3.7 OS Verification
```
Phase: Installation Verification
Input: Windows on Partition 1, Boot manager configured
Actions:
  1. Verify Windows installation:
     - Check critical files exist:
       ✓ D:\Windows\kernel.exe
       ✓ D:\Windows\System32\config\SAM (user database)
       ✓ D:\Windows\System32\drivers\ (driver database)
       ✓ Registry hives in D:\Windows\System32\config\
  2. Verify boot configuration:
     - Check BCD stored correctly
     - Verify boot manager can read Windows installation
  3. Verify UEFI settings:
     - Confirm Secure Boot enabled
     - Confirm boot device order set
     - Confirm TPM configured
  4. Dry boot test (if possible):
     - Attempt to boot (may fail without drivers)
     - Verify no errors in firmware logs
Output: OS installation verified ✓
```

### Validation Checkpoints

```
Checkpoint 3.1: Windows Files
  ✓ Windows files exist on Partition 1
  ✓ Critical system files present
  ✓ File count matches expected (40,000+ files)
  ✓ Total size ≈ 30-35 GB

Checkpoint 3.2: Boot Loader
  ✓ Boot manager installed on EFI partition
  ✓ BCD present and readable
  ✓ Boot entries configured
  ✓ Boot timeout set to 3 seconds

Checkpoint 3.3: UEFI Configuration
  ✓ Boot device order correct
  ✓ TPM enabled and detected
  ✓ Legacy BIOS disabled
  ✓ Boot timeout: 3 seconds

Checkpoint 3.4: Secure Boot
  ✓ Secure Boot enabled
  ✓ Boot Manager signature registered
  ✓ No Secure Boot warnings
  ✓ Firmware recognizes Windows Boot Manager
```

### Error Handling

```
Error: Windows ISO not found or invalid
  → Message: "Windows 11 ISO not found or corrupted (hash mismatch)"
  → Action: Offer to download from Microsoft Media Creation Tool
  → Recovery: Download ISO, verify hash, continue

Error: Partition 1 insufficient space
  → Message: "Partition 1 has [X] GB free, need 45+ GB for Windows"
  → Action: Show option to expand Partition 1
  → Recovery: User increases P1 size, continues

Error: Boot loader installation failed
  → Message: "Failed to install Windows Boot Manager: [error]"
  → Possible reasons:
     - EFI partition corrupted
     - Insufficient space for boot files
     - File system error
  → Action: Re-format EFI partition, retry
  → Recovery: If retry fails, HALT and require manual recovery

Error: UEFI configuration failed
  → Message: "Cannot access UEFI firmware settings: [error]"
  → Action: May require manual UEFI configuration
  → Recovery: Provide manual UEFI configuration instructions

Error: Secure Boot conflicts with drivers
  → Message: "Secure Boot enabled but drivers unsigned. Allow anyway?"
  → Action: Offer to disable Secure Boot for driver compatibility
  → Recovery: User confirms, Secure Boot disabled (reduced security)
```

---

## Stage 4: Driver Installation (Duration: 10-20 minutes)

### Purpose
Install critical hardware drivers for GPU, chipset, network, audio, and Razer devices to ensure full hardware functionality.

### Driver Installation Sequence

#### 4.1 Chipset Drivers (Critical)
```
Priority: CRITICAL
Duration: 3-5 minutes
Purpose: Core system stability and device communication

Drivers to install:
  1. Chipset drivers (Intel/AMD based on motherboard)
     - Chipset System Software
     - SATA drivers
     - LPC drivers (for TPM communication)
     - USB 3.x drivers
  2. Motherboard drivers
     - Power management
     - System management interface
  3. Temperature/monitoring
     - Sensor drivers (for Razer RGB feedback)

Installation process:
  1. Identify motherboard chipset from system info
  2. Download drivers from manufacturer (Intel/AMD)
  3. Install with silent mode: /S /D=<path>
  4. Verify each driver loaded in Device Manager
  5. Reboot if required

Verification:
  ✓ All chipset devices show as "working properly"
  ✓ No yellow exclamation marks
  ✓ Device Manager shows correct version
```

#### 4.2 GPU Drivers - NVIDIA RTX 5090 (Critical)
```
Priority: CRITICAL (entire Monado Blade depends on GPU)
Duration: 5-10 minutes
Purpose: CUDA acceleration, rendering, AI inference

Drivers to install:
  1. NVIDIA RTX 5090 drivers
     - Version: Latest stable (528.x or newer)
     - Type: Studio or Game-ready (recommend Studio for stability)
     - Options:
       ∟ CUDA 12.x toolkit (for PyTorch)
       ∟ cuDNN (for neural networks)
       ∟ NVIDIA drivers (for rendering)
       ∟ NVIDIA Control Panel
  2. NVIDIA PhysX
  3. NVIDIA DirectX 12 drivers

Installation process:
  1. Download NVIDIA Studio Driver from nvidia.com
  2. Extract to Partition 5 (Programs): H:\NVIDIA_Drivers\
  3. Run installer: NvidiaInstaller.exe /S
  4. Install options:
     ☑ GPU drivers
     ☑ CUDA toolkit
     ☑ cuDNN
     ☑ NVIDIA Control Panel
     ☐ GFXBench (optional)
  5. Verify GPU detected: nvidia-smi.exe
     Output should show:
       NVIDIA RTX 5090 | 32GB VRAM | CUDA capability 9.2
  6. Reboot

GPU-specific verification:
  1. nvidia-smi shows GPU with 32 GB VRAM
  2. CUDA Capability: 9.2 (Blackwell architecture)
  3. Driver version: 528.x or newer
  4. VRAM all accessible: 32,000 MB
  5. GPU memory test passes
  6. DirectX 12 support confirmed

GPU Performance Baseline:
  - Boost Clock: Should reach 2.5+ GHz under load
  - Memory Clock: Should reach 19.5+ GHz
  - Thermal: Should maintain <85°C under load
  - Power draw: Should reach 500W+ under full load
```

#### 4.3 Network Drivers (Critical)
```
Priority: CRITICAL (needed for internet access)
Duration: 2-3 minutes
Purpose: Ethernet/WiFi connectivity

Drivers to install:
  1. Ethernet adapter driver
     - Motherboard LAN chip (Realtek/Intel)
     - Latest stable version
  2. WiFi adapter (if applicable)
     - WiFi chip drivers (Intel/Qualcomm/Realtek)
     - Latest stable version
  3. Optional: Mobile broadband drivers

Installation process:
  1. Identify network adapters (from Device Manager)
  2. Download from manufacturer
  3. Install with: /S /D=<path>
  4. Verify in Device Manager: "working properly"
  5. Test connectivity (ping 8.8.8.8)

Verification:
  ✓ Ethernet adapter enabled and connected
  ✓ IP address assigned (DHCP or static)
  ✓ DNS resolves correctly
  ✓ Internet test (ping/browser) works
```

#### 4.4 Audio Drivers - THX Spatial Audio (Critical)
```
Priority: CRITICAL (boot animation requires audio sync)
Duration: 3-5 minutes
Purpose: THX spatial audio, 3D positioning, real-time feedback

Drivers to install:
  1. High-definition audio drivers
     - Realtek/NVIDIA audio codec
     - Latest version
  2. THX Spatial Audio drivers
     - THX Spatial Audio SDK
     - THX Optimizer
  3. Audio enhancement drivers
     - Windows Sonic (Spatial Audio)
     - Dolby Atmos (if supported)

Installation process:
  1. Download audio drivers from motherboard manufacturer
  2. Download THX Spatial Audio from thx.com
  3. Install audio drivers with /S /D=<path>
  4. Install THX Spatial Audio: THz_Installer.exe /S
  5. Configure THX:
     - Enable Spatial Audio
     - Enable 3D positioning
     - Set audio format: 7.1 (if available)
  6. Reboot

Verification:
  ✓ Audio device shows in Sound Settings
  ✓ THX Spatial Audio enabled
  ✓ Volume levels correct
  ✓ Test audio file plays in 3D (if headphones available)
  ✓ THX app shows "Ready"
```

#### 4.5 Razer Hardware Drivers (Critical for RGB/Firmware)
```
Priority: CRITICAL (Monado Blade custom hardware)
Duration: 5-8 minutes
Purpose: RGB control, device firmware, hardware monitoring

Drivers to install:
  1. Razer Synapse 3 (main driver suite)
     - Razer device controller
     - RGB management
     - Device firmware updater
  2. Razer Chroma SDK
     - API for RGB control from Monado Blade app
     - Color profile management
  3. Device-specific drivers:
     - Mouse drivers
     - Keyboard drivers
     - Headset drivers
  4. Firmware packages:
     - RGB controller firmware
     - Device firmware versions

Installation process:
  1. Download Razer Synapse 3 from razer.com
  2. Download Razer Chroma SDK
  3. Install Synapse 3: RazerSynapse3_Installer.exe /S
  4. Install Chroma SDK: RazerChromaSDK_Installer.exe /S
  5. Launch Synapse 3 and let it detect all Razer devices
  6. For each device, check for firmware updates:
     - Click on device
     - Check firmware version
     - If update available, install and reboot
  7. Configure default profiles:
     - Set "Monado Blade" as default profile
     - Configure RGB colors (Monado blue + cyan accent)
  8. Verify Chroma SDK installed:
     - Check C:\Program Files (x86)\Razer\Chroma SDK\
     - Verify DLLs present

Verification:
  ✓ Synapse 3 launches without errors
  ✓ All Razer devices detected and listed
  ✓ Device firmware at latest version
  ✓ Chroma SDK DLLs available for Monado Blade app
  ✓ Test RGB: Set all devices to monado blue color
  ✓ Test firmware hash validation (see Stage 3 boot sequence)
```

#### 4.6 Storage & USB Drivers
```
Priority: HIGH
Duration: 1-2 minutes
Purpose: External storage, backup device access

Drivers to install:
  1. USB 3.x drivers (chipset already includes, but verify)
  2. NVMe drivers (for fast storage)
  3. Storage drivers (SATA, SAS if applicable)

Installation process:
  1. Verify in Device Manager under "Storage"
  2. Install any missing drivers
  3. Update existing drivers to latest

Verification:
  ✓ All storage devices visible in Device Manager
  ✓ All USB ports functional
```

#### 4.7 Graphics API Support (DirectX, Vulkan)
```
Priority: HIGH
Duration: 1-2 minutes
Purpose: Rendering API support for Monado Blade UI

Prerequisites:
  ✓ GPU drivers installed (RTX 5090)

Drivers to verify:
  1. DirectX 12 support (GPU drivers provide this)
  2. Vulkan 1.3+ support (GPU drivers provide this)
  3. OpenGL 4.6+ support (GPU drivers provide this)

Verification:
  ✓ DirectX 12 Feature Level: 12_1 (RTX 5090 capable)
  ✓ Vulkan Version: 1.3 or newer
  ✓ OpenGL Version: 4.6 or newer
  ✓ Test rendering with Vulkan application
```

### Driver Installation Order

```
1. Chipset drivers        (3-5 min)   [MUST complete before GPU]
2. GPU drivers - RTX 5090 (5-10 min)  [CRITICAL - foundation]
3. Audio drivers          (3-5 min)   [For boot animation audio]
4. Network drivers        (2-3 min)   [For internet access]
5. Razer drivers          (5-8 min)   [For RGB control]
6. Storage drivers        (1-2 min)   [For external devices]

Total: 19-33 minutes

Critical path:
  Chipset drivers → GPU drivers → Audio → Network
  (Razer parallel)
  (Storage drivers after GPU)
```

### Validation Checkpoints

```
Checkpoint 4.1: Chipset Drivers
  ✓ All chipset devices in Device Manager show "working properly"
  ✓ No unknown devices
  ✓ System information shows correct chipset

Checkpoint 4.2: GPU Drivers
  ✓ nvidia-smi.exe runs and shows RTX 5090
  ✓ VRAM: 32,000 MB all accessible
  ✓ CUDA Capability: 9.2
  ✓ Driver version: 528.x or newer
  ✓ GPU can boost to 2.5+ GHz

Checkpoint 4.3: Network Drivers
  ✓ Ethernet adapter enabled
  ✓ IP address assigned (ping -c 1 8.8.8.8 succeeds)
  ✓ DNS resolves

Checkpoint 4.4: Audio Drivers
  ✓ Audio device shows in Sound Settings
  ✓ THX Spatial Audio enabled
  ✓ Test sound plays

Checkpoint 4.5: Razer Hardware
  ✓ Razer Synapse 3 launches
  ✓ All Razer devices detected
  ✓ Firmware at latest version
  ✓ Chroma SDK DLLs available
  ✓ RGB test: All devices turn monado blue

Checkpoint 4.6: Full Driver Validation
  ✓ Device Manager: 0 unknown devices, 0 errors
  ✓ All critical drivers installed
  ✓ All drivers latest version
```

### Error Handling

```
Error: GPU driver installation failed
  → Message: "GPU driver installation failed: [error]"
  → Action: CRITICAL - Retry installation 2 times
  → If persistent: Offer rollback to previous driver version
  → Recovery: Download driver again, reinstall with clean slate

Error: Network driver not found
  → Message: "Network adapter detected but no driver available"
  → Action: Show network adapter model, offer manual download
  → Recovery: User downloads driver from manufacturer, continues

Error: Razer device not detected by Synapse 3
  → Message: "Razer device connected but not detected in Synapse"
  → Action: Offer USB reset, reinstall Synapse
  → Recovery: Uninstall/reinstall Razer Synapse 3

Error: Audio driver causes no sound
  → Message: "Audio driver installed but no sound detected"
  → Action: Verify audio device in Sound Settings
  → Recovery: Reinstall audio driver, try alternative version

Error: Device signature verification failure (Secure Boot conflict)
  → Message: "Driver signature verification failed [driver.sys]"
  → Action: Offer to disable Secure Boot for driver installation
  → Recovery: User confirms, Secure Boot disabled, driver installed
```

---

## Stage 5: Program Installation (Duration: 45-90 minutes)

### Purpose
Deploy complete development and AI ecosystem: Visual Studio 2022, Python, Node.js, Docker, Ollama, and supporting tools to Partition 5 (Programs).

### Program Installation List

#### 5.1 Core Development Tools (30-40 minutes)

**Visual Studio 2022 Professional** (12-15 GB)
```
Purpose: IDE for C#/.NET, C++, web development
Installation:
  1. Download: vs_professional.exe from visualstudio.microsoft.com
  2. Run installer: vs_professional.exe --quiet --add Microsoft.VisualStudio.Workload.ManagedDesktop
  3. Components to include:
     ☑ .NET 8 runtime
     ☑ C# language support
     ☑ C++ tools
     ☑ Web development tools
     ☑ Git integration
     ☑ NuGet package manager
  4. Installation path: H:\Programs\VisualStudio2022\
  5. Target framework: .NET 8.0
  6. Reboot if required

Verification:
  ✓ devenv.exe launches
  ✓ .NET SDK available (dotnet --version)
  ✓ C# language services available
  ✓ Git integration functional
```

**Python 3.12.x with pip** (500 MB)
```
Purpose: ML/AI scripting, Ollama integration, data processing
Installation:
  1. Download: python-3.12.x-amd64.exe from python.org
  2. Run installer: python-3.12.x-amd64.exe /quiet InstallAllUsers=1 PrependPath=1
  3. Installation path: H:\Programs\Python312\
  4. Configuration:
     - Add to PATH: Yes
     - Install pip: Yes
     - Install venv: Yes
     - Install IDLE: No
  5. Verify: python --version → Python 3.12.x
  6. Upgrade pip: python -m pip install --upgrade pip

Key Python packages to pre-install:
  pip install numpy pandas scikit-learn matplotlib jupyter tensorflow pytorch

Verification:
  ✓ python --version shows 3.12.x
  ✓ pip --version functional
  ✓ venv works: python -m venv test_env
  ✓ Key packages installed
```

**Node.js + npm** (300 MB)
```
Purpose: Web development, Electron apps, npm ecosystem
Installation:
  1. Download: node-v20.x.x-x64.msi from nodejs.org
  2. Run installer: node-v20.x.x-x64.msi /quiet
  3. Installation path: H:\Programs\NodeJS\
  4. Configuration:
     - npm included automatically
     - Add to PATH: Yes
  5. Verify: node --version → v20.x.x
  6. Upgrade npm: npm install -g npm@latest

Key npm packages to pre-install globally:
  npm install -g typescript @angular/cli react-native-cli webpack

Verification:
  ✓ node --version shows v20.x.x
  ✓ npm --version shows 10.x.x or newer
  ✓ npx works: npx webpack --version
```

**Git + GitHub Desktop** (200 MB)
```
Purpose: Version control, GitHub integration
Installation:
  1. Download: GitInstaller.exe from git-scm.com
  2. Run: GitInstaller.exe /SILENT /COMPONENTS="icons,assoc,assoc_sh"
  3. Installation path: H:\Programs\Git\
  4. Configuration:
     - Line endings: Checkout as-is, commit Unix-style
     - Default editor: Visual Studio Code
     - Use OpenSSL: Yes
  5. Download GitHub Desktop from github.com/desktop
  6. Run: GitHubDesktopSetup.exe /S
  7. Installation path: H:\Programs\GitHubDesktop\

Configuration:
  - Configure git user:
    git config --global user.name "Monado Blade"
    git config --global user.email "blade@monado.ai"
  - Add GitHub credentials to Desktop

Verification:
  ✓ git --version shows 2.40.x or newer
  ✓ GitHub Desktop launches
  ✓ git clone works with personal repo
```

**Docker Desktop** (2 GB)
```
Purpose: Container orchestration, isolated environments, Linux containers
Installation:
  1. Download: Docker Desktop Installer.exe from docker.com
  2. Run: "Docker Desktop Installer.exe" install --quiet
  3. Installation path: C:\Program Files\Docker\
  4. Configuration:
     - WSL2 backend (already available on Windows 11 Pro)
     - Resource allocation:
       * CPUs: 8 (of 16 available)
       * Memory: 16 GB (of 32 GB available)
       * Disk: 64 GB (on Partition 6/Temp)
       * GPU: Enable NVIDIA GPU support
  5. Reboot

Verification:
  ✓ docker --version shows 24.x.x or newer
  ✓ docker run hello-world succeeds
  ✓ WSL2 integration active
  ✓ GPU passthrough working: docker run --gpus all nvidia/cuda:12.x nvidia-smi
```

#### 5.2 AI/ML Tools (20-40 minutes)

**Ollama** (2-3 GB base)
```
Purpose: Local LLM inference, model management
Installation:
  1. Download: OllamaInstaller.exe from ollama.ai
  2. Run: OllamaInstaller.exe /S /D=H:\Programs\Ollama\
  3. Configuration:
     - Models directory: F:\AIModels\ (Partition 3)
     - GPU acceleration: NVIDIA CUDA 12.x
     - Server port: 11434
     - Bind address: 127.0.0.1 (localhost only initially)
  4. Start service: net start "Ollama Service"
  5. Verify: ollama --version

Pre-download models (to Partition 3):
  (These will be done in Stage 6, but prepare space now)
  - llama2:7b-chat (4 GB)
  - mistral:7b-instruct (4 GB)
  - neural-chat:7b (5 GB)
  - dolphin-mixtral (14 GB)
  - Other models as needed

Verification:
  ✓ ollama --version shows latest
  ✓ ollama list shows available models
  ✓ ollama serve listens on port 11434
```

**Python ML Stack** (500 MB packages)
```
Purpose: Machine learning, neural networks, data processing
Installation (via pip):
  
  pip install \
    pytorch::pytorch::py3.12_cuda11.8_cudnn8.9.1* \
    pytorch::pytorch-gpu \
    tensorflow[and-cuda] \
    scikit-learn \
    pandas \
    numpy \
    matplotlib \
    seaborn \
    scipy \
    plotly \
    transformers \
    torch-vision \
    torchaudio

Verification:
  ✓ python -c "import torch; print(torch.cuda.is_available())" → True
  ✓ python -c "import tensorflow as tf; print(tf.config.list_physical_devices('GPU'))" → Shows GPU
```

**Jupyter Lab** (100 MB)
```
Purpose: Interactive notebooks for ML development
Installation:
  pip install jupyterlab ipykernel

Configuration:
  jupyter lab --generate-config
  # Configure in ~/.jupyter/jupyter_lab_config.py:
    c.ServerApp.port = 8888
    c.ServerApp.open_browser = False
    c.ServerApp.allow_root = True

Verification:
  ✓ jupyter lab --version shows 4.x.x or newer
  ✓ jupyter lab launches at http://localhost:8888
```

#### 5.3 Development Tools (10-15 minutes)

**Visual Studio Code** (300 MB)
```
Purpose: Lightweight code editor, extensions ecosystem
Installation:
  1. Download: VSCodeUserSetup-x64-1.x.x.exe from code.visualstudio.com
  2. Run: VSCodeUserSetup-x64-1.x.x.exe /SILENT
  3. Installation path: C:\Users\[user]\AppData\Local\Programs\Microsoft VS Code\

Core Extensions to Install:
  code --install-extension ms-python.python
  code --install-extension ms-vscode.cpptools
  code --install-extension hashicorp.terraform
  code --install-extension ms-vscode-remote.remote-ssh
  code --install-extension GitHub.copilot
  code --install-extension esbenp.prettier-vscode
  code --install-extension dbaeumer.vscode-eslint

Verification:
  ✓ code --version shows 1.x.x
  ✓ Extensions installed: code --list-extensions
```

**CMake & Build Tools** (50 MB)
```
Purpose: C++ project building, cross-platform compilation
Installation:
  1. Download CMake: cmake-x.x.x-windows-x86_64.msi
  2. Run: cmake-x.x.x-windows-x86_64.msi /quiet
  3. Installation path: H:\Programs\CMake\
  4. Add to PATH: Yes
  5. Verify: cmake --version

MinGW/Clang (for C++):
  Windows: MSVC included with Visual Studio 2022 (use that)
  Option: Install Clang: winget install llvm.llvm
```

**Development Tools Bundle** (200 MB)
```
Additional tools:
  - curl: Download from curl.se
  - wget: Windows native (available in Windows 11)
  - ffmpeg: For audio/video processing (Partition 5)
  - ImageMagick: Image processing
  - Graphviz: Diagram generation

Installation:
  winget install curl ffmpeg.ffmpeg imagemagick graphviz
```

#### 5.4 System Utilities (5-10 minutes)

**Process & Performance Monitoring**
```
Tools:
  - Process Explorer: sysinternals.com
  - GPU-Z: techpowerup.com (GPU monitoring)
  - CPU-Z: cpuid.com (CPU monitoring)
  - 7-Zip: File compression (compares to WinRAR)

Installation:
  - Download and extract to H:\Programs\SystemTools\
  - Add to PATH if desired
```

### Installation Process Flow

```
Installation sequence (sequential, critical order):

Phase 1: Core Development (30-40 min)
  1. Visual Studio 2022        ████░░░░░░ 10%
  2. Python 3.12              ██░░░░░░░░ 12%
  3. Node.js + npm            ██░░░░░░░░ 14%
  4. Git + GitHub Desktop     ███░░░░░░░ 16%
  5. Docker Desktop           ███░░░░░░░ 20%
     ↓ Reboot if required

Phase 2: AI/ML Stack (20-40 min)
  6. Ollama                   ████░░░░░░ 23%
  7. Python ML packages       ████████░░ 35%
  8. Jupyter Lab              █████░░░░░ 37%

Phase 3: Development Tools (10-15 min)
  9. Visual Studio Code       █████░░░░░ 38%
  10. CMake & Build Tools     █████░░░░░ 40%
  11. System Tools Bundle     ██████░░░░ 42%

Total: 60-95 minutes
```

### Validation Checkpoints

```
Checkpoint 5.1: Core Tools
  ✓ Visual Studio 2022 launches (devenv.exe)
  ✓ Python 3.12 installed (python --version)
  ✓ pip works (pip --version)
  ✓ Node.js v20.x (node --version)
  ✓ npm works (npm --version)
  ✓ Git functional (git --version)
  ✓ GitHub Desktop launches

Checkpoint 5.2: Docker
  ✓ docker --version shows 24.x or newer
  ✓ docker run hello-world succeeds
  ✓ WSL2 integration active
  ✓ GPU passthrough: docker run --gpus all nvidia/cuda nvidia-smi

Checkpoint 5.3: AI/ML Stack
  ✓ ollama --version works
  ✓ PyTorch CUDA available: torch.cuda.is_available() = True
  ✓ TensorFlow GPU available
  ✓ Jupyter Lab accessible at http://localhost:8888

Checkpoint 5.4: Development Tools
  ✓ Visual Studio Code launches (code --version)
  ✓ Code extensions installed (code --list-extensions)
  ✓ CMake available (cmake --version)
  ✓ C++ compiler working (cl.exe /? for MSVC)
```

### Error Handling

```
Error: Visual Studio 2022 installation fails
  → Message: "Visual Studio 2022 installation failed: [error]"
  → Action: Retry installation, check disk space (need 20 GB free)
  → Recovery: Partial install (skip optional components), continue

Error: Python package installation fails (pip)
  → Message: "Failed to install [package]: [error]"
  → Action: Check network connection, upgrade pip
  → Recovery: Skip problematic package, continue with others

Error: Docker Desktop fails to install (WSL2 not available)
  → Message: "WSL2 not found. Install Windows Subsystem for Linux 2"
  → Action: Cannot proceed without WSL2
  → Recovery: Install WSL2 first, then retry Docker

Error: Ollama GPU acceleration not detected
  → Message: "GPU not detected in Ollama. Running on CPU"
  → Action: Verify NVIDIA drivers installed (Stage 4)
  → Recovery: Install GPU drivers, restart Ollama service

Error: Insufficient disk space for installations
  → Message: "Insufficient space on Partition 5: Only [X] GB available"
  → Action: Calculate space needed, offer to resize partitions
  → Recovery: User resizes Partition 5, continues installation
```

---

## Stage 6: AI Hub Setup (Duration: 30-45 minutes)

### Purpose
Deploy containerized AI/ML environment with Hyper-V/WSL2, Linux container, Ollama integration, and GPU passthrough.

### Process Flow

#### 6.1 WSL2 Environment Setup
```
Phase: Linux Container Foundation
Input: Docker Desktop installed, WSL2 available
Actions:
  1. Verify WSL2 installed and active:
     - wsl --list --verbose
     - Should show: Ubuntu-22.04 with WSL version 2
  2. If not present, install Ubuntu 22.04:
     - wsl --install -d Ubuntu-22.04
     - Follow first-run setup
  3. Configure WSL2 resources:
     - Edit ~/.wslconfig (in Windows user home):
       [interop]
       enabled=true
       appendWindowsPath=true
       
       [wsl2]
       kernel=<latest>
       memory=16GB
       processors=8
       swap=4GB
       localhostForwarding=true
       
       [interop]
       enabled=true
       appendWindowsPath=true
  4. Verify NVIDIA GPU passthrough in WSL2:
     - wsl -d Ubuntu-22.04
     - nvidia-smi
     - Should show RTX 5090 with 32 GB VRAM

Verification:
  ✓ WSL2 version 2 active
  ✓ Ubuntu 22.04 installed
  ✓ nvidia-smi works in WSL2
  ✓ GPU memory accessible: 32,000 MB
```

#### 6.2 Docker Container Configuration
```
Phase: Container Setup for Ollama
Input: Docker Desktop with GPU support, WSL2 active
Actions:
  1. Create docker-compose.yml for Monado AI Hub:
     
     version: '3.8'
     services:
       ollama:
         image: ollama/ollama:latest
         container_name: monado-ai-hub
         ports:
           - "11434:11434"
         environment:
           - OLLAMA_MODELS=/models
           - CUDA_VISIBLE_DEVICES=0
         volumes:
           - F:\AIModels:/models
           - ~/ollama:/root/.ollama
         deploy:
           resources:
             reservations:
               devices:
                 - driver: nvidia
                   count: 1
                   capabilities: [gpu]
         restart: unless-stopped
  
  2. Create container:
     docker-compose -f docker-compose.yml up -d
  
  3. Verify container running:
     docker ps | grep monado-ai-hub
     docker exec monado-ai-hub ollama list

Verification:
  ✓ Container running (docker ps)
  ✓ ollama service accessible on port 11434
  ✓ GPU detected in container (docker exec monado-ai-hub nvidia-smi)
  ✓ Models directory mounted: /models (shows F:\AIModels)
```

#### 6.3 Ollama Model Deployment
```
Phase: LLM Model Download
Input: Container running with GPU, Partition 3 (150 GB available)
Actions:
  1. Download initial LLM models to F:\AIModels\ (Partition 3):
     
     Priority 1 (Essential):
       - ollama pull llama2:7b-chat
         * Size: 4 GB
         * Recommended for chat/dialogue
         * Fast inference (~100ms)
       
       - ollama pull mistral:7b-instruct
         * Size: 4 GB
         * Better reasoning, creative tasks
         * Fast inference (~100ms)
     
     Priority 2 (Recommended):
       - ollama pull neural-chat:7b
         * Size: 5 GB
         * Optimized for instruction-following
         * Medium speed (~150ms)
       
       - ollama pull dolphin-mixtral:8x7b
         * Size: 14 GB (note: Large!)
         * State-of-the-art performance
         * Slower inference (~500ms)
     
     Priority 3 (Optional):
       - ollama pull yi:34b-chat (optional, 20 GB)
       - ollama pull nous-hermes:70b (optional, 40 GB)
  
  2. Download process:
     - Issue from container: docker exec monado-ai-hub ollama pull llama2:7b-chat
     - Monitor progress: docker logs monado-ai-hub
     - Expected time per model: 5-15 minutes (depends on internet)
  
  3. Verify models downloaded:
     docker exec monado-ai-hub ollama list
     Expected output:
       NAME                              ID              SIZE      MODIFIED
       llama2:7b-chat                    c075a1f38bfe    4.0 GB    2 hours ago
       mistral:7b-instruct               2c3c0e5c7c5e    4.0 GB    1 hour ago
       neural-chat:7b                    8f77c8e3c5b7    5.0 GB    30 min ago
       dolphin-mixtral:8x7b              7a8c3f5e2b1a    14.0 GB   10 min ago

Duration estimate:
  Priority 1 (2 models): ~20 minutes
  Priority 2 (2 models): ~40 minutes
  Priority 3 (depends on selection): ~60+ minutes
  Total: 30-90 minutes (auto-continue in background)

Verification:
  ✓ All selected models downloaded to F:\AIModels\
  ✓ ollama list shows all models
  ✓ Model files: ollama ls -la /models/
  ✓ Each model accessible for inference
```

#### 6.4 GPU Passthrough Validation
```
Phase: GPU Performance Verification
Input: Container with GPU, Models downloaded
Actions:
  1. Test GPU accessibility in container:
     docker exec monado-ai-hub nvidia-smi
     
     Expected output:
       NVIDIA RTX 5090 | 32GB VRAM
       Docker container: Full GPU access
       Memory usage: [X] MB / 32,000 MB
  
  2. Test inference performance:
     docker exec monado-ai-hub ollama run llama2:7b-chat "Say 'Hello' in 5 words"
     
     Timing:
       - First token: ~500ms (model load + prompt processing)
       - Tokens per second: 50-150 tokens/sec (depends on model size)
       - Full response: 2-5 seconds for short responses
  
  3. GPU load test (memory and compute):
     docker exec monado-ai-hub ollama run dolphin-mixtral:8x7b "Write 500 words about AI"
     
     Monitor GPU:
       - GPU memory should ramp up to 20-30 GB
       - GPU utilization: 80-95% during inference
       - Temperature should stabilize at 70-80°C
  
  4. Concurrent request test:
     docker exec monado-ai-hub ollama run mistral:7b-instruct &
     docker exec monado-ai-hub ollama run llama2:7b-chat &
     
     Expected:
       - Both requests queued (Ollama single-process)
       - One processes while other waits
       - Queue handling graceful

Verification:
  ✓ GPU fully accessible (32 GB VRAM visible)
  ✓ Inference latency acceptable (first token < 1s)
  ✓ Throughput adequate (50+ tokens/sec on 7B models)
  ✓ Memory management correct
  ✓ No CUDA errors in logs
  ✓ Temperature within safe range (<85°C)
```

#### 6.5 API Integration Setup
```
Phase: Monado Blade API Backend Integration
Input: Ollama running, models loaded
Actions:
  1. Configure Ollama API endpoint:
     - Base URL: http://localhost:11434/api
     - Fallback URL: http://ollama:11434/api (from inside container)
     - Health check: GET /api/tags (list models)
  
  2. Setup API router (for Phase 2):
     - Route inference requests to appropriate model
     - Implement request queuing
     - Implement response streaming
     - Implement error recovery
  
  3. Create configuration file: H:\Programs\MonadoConfig\ai_providers.json
     
     {
       "ai_providers": [
         {
           "name": "ollama_local",
           "type": "ollama",
           "enabled": true,
           "base_url": "http://localhost:11434/api",
           "models": {
             "chat": "llama2:7b-chat",
             "reasoning": "mistral:7b-instruct",
             "instruction": "neural-chat:7b",
             "advanced": "dolphin-mixtral:8x7b"
           },
           "fallback_order": ["llama2:7b-chat", "mistral:7b-instruct"]
         }
       ],
       "timeout_seconds": 300,
       "max_concurrent": 1,
       "gpu_memory_reserve": 2000
     }
  
  4. Test API endpoint:
     curl http://localhost:11434/api/tags
     
     Expected response:
       {"models": ["llama2:7b-chat", "mistral:7b-instruct", ...]}

Verification:
  ✓ API endpoint responds to health check
  ✓ All models listed in API response
  ✓ Configuration file valid JSON
  ✓ Monado app can read configuration
```

#### 6.6 System Integration & Networking
```
Phase: Final Integration
Input: Ollama running, models loaded, API configured
Actions:
  1. Configure Ollama network access:
     Current: localhost only (secure)
     Option for future: Bind to 0.0.0.0:11434 (allow remote)
     
     Current config (secure):
       OLLAMA_HOST=127.0.0.1:11434
     
     For future multi-machine setup:
       OLLAMA_HOST=0.0.0.0:11434
       (Then require authentication/TLS)
  
  2. Setup automated startup:
     - Docker daemon auto-starts container on boot
     - Verify: docker update --restart=unless-stopped monado-ai-hub
  
  3. Configure logging:
     - docker logs monado-ai-hub
     - Log location: docker inspect monado-ai-hub (LogPath)
     - Archive logs: /var/lib/docker/containers/[id]/
  
  4. Setup monitoring:
     - Health check endpoint: http://localhost:11434/api/tags
     - Resource monitoring: docker stats monado-ai-hub
     - Prometheus export (optional for advanced monitoring)

Verification:
  ✓ Container auto-restarts on boot
  ✓ Ollama logs accessible
  ✓ Health check endpoint responsive
  ✓ docker stats shows GPU usage
  ✓ Monado app can connect to API
```

### Validation Checkpoints

```
Checkpoint 6.1: WSL2 & Docker
  ✓ WSL2 version 2 active
  ✓ Docker Desktop running
  ✓ nvidia-smi works in WSL2
  ✓ GPU passthrough confirmed

Checkpoint 6.2: Container & Ollama
  ✓ monado-ai-hub container running
  ✓ Container has GPU access (nvidia-smi inside)
  ✓ Ollama service accessible on port 11434
  ✓ Ollama list returns valid model list

Checkpoint 6.3: Models Downloaded
  ✓ Minimum 2 models downloaded (llama2 + mistral)
  ✓ Total model size <= 150 GB (Partition 3)
  ✓ F:\AIModels\ contains model files
  ✓ Each model can be invoked without errors

Checkpoint 6.4: GPU Performance
  ✓ GPU inference working (tokens generated)
  ✓ First token latency < 1 second
  ✓ Throughput >= 50 tokens/sec
  ✓ GPU temperature 70-80°C under load
  ✓ Memory usage shows GPU allocated

Checkpoint 6.5: API Integration
  ✓ API health check succeeds (HTTP 200)
  ✓ Configuration file valid JSON
  ✓ curl to API endpoint returns models list
  ✓ Monado app can query API successfully

Checkpoint 6.6: System Integration
  ✓ Container auto-starts on reboot
  ✓ Logs accessible and non-empty
  ✓ Monitoring tools report correct usage
  ✓ No connection errors between Monado app and Ollama
```

### Error Handling

```
Error: WSL2 not available or installed
  → Message: "WSL2 not detected. Cannot proceed without WSL2"
  → Action: Cannot proceed
  → Recovery: Go back to Stage 1, enable WSL2 (Windows feature)

Error: GPU not detected in container
  → Message: "GPU not accessible in Docker container"
  → Action: Verify NVIDIA Docker runtime installed
  → Recovery: Install nvidia-docker, restart Docker daemon

Error: Model download fails (network timeout)
  → Message: "Model download failed: [model name] - Network timeout"
  → Action: Retry download from container
  → Recovery: Check internet, retry, or skip model

Error: Container fails to start
  → Message: "Container failed to start: [error]"
  → Action: Check Docker logs (docker logs monado-ai-hub)
  → Recovery: Remove container, recreate, restart

Error: Ollama inference crashes
  → Message: "Ollama crashed during inference: [error]"
  → Action: Check container logs for CUDA errors
  → Recovery: Restart container, update NVIDIA drivers if needed

Error: API port 11434 already in use
  → Message: "Port 11434 already bound. Cannot start Ollama"
  → Action: Check what's using port (netstat -ano | findstr 11434)
  → Recovery: Change Ollama port in docker-compose.yml or kill process
```

---

## Stage 7: System Configuration (Duration: 20-30 minutes)

### Purpose
Finalize system security, user profiles, network, Razer firmware validation, and performance tuning.

### Process Flow

#### 7.1 User Profile Creation
```
Phase: User Account Setup
Input: Windows OS installed and operational
Actions:
  1. Create primary user account:
     - Username: "Monado" (or user-specified)
     - Account type: Administrator
     - Password: User-specified (required)
     - Password hint: "Monado Blade AI Hub"
  
  2. Configure user profile:
     - Set profile picture (Monado Blade logo)
     - Configure languages: English (primary)
     - Configure regional settings:
       * Date format: YYYY-MM-DD
       * Time format: 24-hour
       * Timezone: User-specified (default: UTC)
  
  3. Create additional profiles if desired:
     - Guest profile (limited, for demos)
     - Development profile (full admin)
     - Restricted profile (AI inference only)
  
  4. Migrate user data:
     - Documents: E:\User Profiles\Documents\
     - Desktop: E:\User Profiles\Desktop\
     - Downloads: E:\User Profiles\Downloads\

Verification:
  ✓ Primary user created with correct name
  ✓ User can login with password
  ✓ User directory created: C:\Users\[username]\
  ✓ User has admin privileges
  ✓ User profile accessible from login screen
```

#### 7.2 Network Configuration
```
Phase: Network Connectivity
Input: Network drivers installed
Actions:
  1. Configure Ethernet (primary):
     - Connection: DHCP (automatic IP assignment)
     - DNS: 8.8.8.8 (Google) + 1.1.1.1 (Cloudflare)
     - Verify: ipconfig /all (should show IP address)
     - Test: ping 8.8.8.8 (should succeed)
  
  2. WiFi configuration (if applicable):
     - Enable WiFi adapter
     - Connect to available network (if configured)
     - Or: Leave disconnected for manual connection
  
  3. Firewall configuration:
     - Windows Firewall: ENABLED (critical for security)
     - Inbound rules: Block all except:
       * Windows Updates
       * Essential services
       * Ollama API (localhost only)
       * SSH (if enabled)
     - Outbound rules: Allow all (can restrict later)
  
  4. Network namespacing (security):
     - Virtual network adapter for Monado app
     - Isolated from system critical services
     - Restricts damage in case of compromise

Verification:
  ✓ ipconfig /all shows valid IP address
  ✓ ping 8.8.8.8 succeeds
  ✓ nslookup google.com returns valid IP
  ✓ Windows Firewall enabled
  ✓ Windows Update can reach servers
```

#### 7.3 Security Hardening
```
Phase: System Hardening
Input: System configured with network
Actions:
  1. Windows Defender:
     - Ensure enabled: Get-MpPreference | Select RealTimeProtectionEnabled
     - Full scan: Start-MpScan -ScanType FullScan
     - Update definitions: Update-MpSignature
  
  2. Windows Update:
     - Set to auto-update (install at 2 AM)
     - Check for pending updates
     - Install critical updates
     - Reboot if necessary
  
  3. User Access Control (UAC):
     - Enable UAC prompts for admin actions
     - Prompt on: Admin account actions
     - Prompt on: Standard user account actions
  
  4. Credential Manager:
     - Setup Windows Credential Manager
     - Store cloud credentials securely
     - Configure auto-unlock (optional)
  
  5. Account Security:
     - Set password policy:
       * Minimum length: 12 characters
       * Complexity: Required (uppercase, lowercase, numbers, symbols)
       * History: Remember last 10 passwords
       * Expiry: 90 days (optional, configurable)
     - Enable login audit
     - Configure account lockout (5 failed attempts → lock 30 min)
  
  6. File Encryption:
     - BitLocker already enabled on encrypted partitions
     - Verify encryption status: manage-bde -status
     - Ensure recovery key backed up

Verification:
  ✓ Windows Defender enabled and signatures current
  ✓ Full scan completed successfully
  ✓ UAC enabled
  ✓ Windows Update configured for auto-install
  ✓ BitLocker status shows encryption active on P1, P2, P7, P9
  ✓ Password policy applied
  ✓ Account lockout policy active
```

#### 7.4 Razer Hardware & Firmware Validation
```
Phase: Hardware Verification
Input: Razer Synapse 3 installed with latest firmware
Actions:
  1. Launch Razer Synapse 3:
     - Verify all Razer devices detected:
       ✓ RGB Controller
       ✓ Keyboard
       ✓ Mouse
       ✓ Headset (if applicable)
       ✓ Other Razer peripherals
  
  2. Firmware validation (4-tier process):
     
     Tier 1: Hash Verification
       - Get firmware version for each device
       - Calculate firmware file hash (SHA256)
       - Verify against known-good hash database
       - If mismatch: Flag for manual review
     
     Tier 2: Signature Verification
       - Verify firmware signed by Razer private key
       - Check signature against Razer public certificate
       - If unsigned or invalid: Reject firmware
     
     Tier 3: Known-Good Database Comparison
       - Maintain database of known-good firmware versions
       - Compare current version against database
       - Warn if firmware version is unknown/untested
       - Option to update to known-good version
     
     Tier 4: Device Functional Test
       - Test RGB functionality (cycle through colors)
       - Test input devices (keyboard/mouse responsive)
       - Test audio devices (sound output functional)
       - If any fail: Flag for firmware re-flash or replacement
  
  3. RGB Profile Configuration:
     - Set default profile: "Monado Blade"
     - Primary color: Monado Blue (#0099FF)
     - Secondary: Cyan accent (#00FFFF)
     - Effect: Breathing + reactive (pulses with system activity)
     - Sync: All devices synchronized

Verification:
  ✓ Synapse 3 launches and detects all devices
  ✓ All firmware passes 4-tier validation
  ✓ All firmware signatures valid
  ✓ No firmware warnings or errors
  ✓ All devices functional (RGB, input, audio)
  ✓ Default RGB profile configured and active
  ✓ RGB color matches expected (Monado Blue)
```

#### 7.5 Performance Tuning
```
Phase: System Optimization
Input: All hardware installed and configured
Actions:
  1. Power Settings:
     - Power plan: High Performance
     - Idle timeout: Never (system stays active)
     - Lid close: Do nothing (ignore lid action)
     - Power button: Shutdown (standard behavior)
  
  2. GPU Optimization:
     - NVIDIA Control Panel → Performance Settings:
       * Power management: Maximum performance
       * Preferred graphics processor: NVIDIA RTX 5090
       * Max frame rate: Unlimited (for Monado Blade)
       * Texture filtering: High quality
       * Antialiasing: High performance
  
  3. Network Optimization:
     - DNS over HTTPS (DoH): Enabled
     - TCP window size: Auto-tuning enabled
     - Network adapter offload: Enabled
     - Enable TSO (TCP Segmentation Offload)
  
  4. System Responsiveness:
     - Disable unnecessary background services:
       * Xbox Gaming Service: Disabled (unless needed)
       * Windows P2P Update: Disabled (use Windows Update instead)
       * OneDrive Sync: Disabled (if not using)
       * Cortana: Disabled (not needed)
  
  5. Boot Optimization:
     - Startup programs: Only essentials
       * Docker Desktop (if AI Hub needed)
       * Razer Synapse (for RGB)
       * Ollama (if always-on AI needed)
     - Disable slow startup items
  
  6. Disk Optimization:
     - Defragmentation: Enable on NTFS (weekly)
     - Partition 4, 6 (SSD cache): Disable fragmentation service
     - Partition 3 (models): Disable defragmentation

Verification:
  ✓ Power plan set to High Performance
  ✓ NVIDIA GPU running in max performance mode
  ✓ GPU boosts to 2.5+ GHz under load
  ✓ System responsiveness excellent
  ✓ Boot time < 1 minute
  ✓ Background service count minimized
```

#### 7.6 AI Provider Credentials Setup (Encrypted)
```
Phase: API Keys & Credentials
Input: System secured and hardened
Actions:
  1. Create credentials directory:
     - Location: E:\AppData\Credentials\ (Partition 2, encrypted)
     - Permissions: Only current user can access
     - Encryption: BitLocker (already enabled on P2)
  
  2. Setup credential management:
     - Use Windows Credential Manager (secure storage)
     - Or: Encrypted JSON file (AES-256)
     - Or: Environment variables (Windows vault)
  
  3. Add API credentials (if user has them):
     - OpenAI API key (optional)
     - Anthropic API key (optional)
     - Google Cloud API key (optional)
     - Azure AI credentials (optional)
     
     Format (encrypted storage):
       {
         "ai_provider_keys": {
           "openai": "sk-...",
           "anthropic": "sk-ant-...",
           "google": "{...json...}",
           "azure": "..."
         },
         "encryption": "AES-256-CBC",
         "created": "2024-01-15T10:30:00Z",
         "expires": null
       }
  
  4. Set environment variables (masked):
     - MONADO_AI_KEYS_PATH=E:\AppData\Credentials\ai_keys.enc
     - MONADO_CREDENTIALS_PASSWORD=[secure password]
     - These are NOT visible in command prompt or logs

Verification:
  ✓ Credentials directory created and encrypted
  ✓ File permissions restrict access to user only
  ✓ Credentials file encrypted with AES-256
  ✓ Environment variables set and hidden
  ✓ Credential manager has no exposed keys in plaintext
```

#### 7.7 Final System Validation
```
Phase: Comprehensive Verification
Input: All configuration complete
Actions:
  1. System startup test:
     - Restart system
     - Verify Windows boots in under 30 seconds (target: 25s)
     - Verify all services started
     - Verify no errors in Event Viewer
  
  2. Hardware inventory:
     - GPU: RTX 5090, 32 GB VRAM visible
     - CPU: [X] cores, [X] GHz
     - RAM: [X] GB total, [X] GB free on boot
     - Disk partitions: All 9 present and accessible
  
  3. Connectivity:
     - Ethernet: Active, valid IP
     - DNS: Resolves correctly
     - Firewall: Enabled, allowing legitimate traffic
     - Windows Update: Checking/updated
  
  4. Security:
     - BitLocker status: All encrypted partitions locked
     - Windows Defender: Active, signatures current
     - UAC: Enabled
     - Account lockout: Configured
  
  5. Software verification:
     - Visual Studio 2022: Launches, .NET SDK available
     - Python: pip works, ML packages installed
     - Node.js: npm functional
     - Docker: Container running, GPU accessible
     - Ollama: Service accessible, models loaded
     - VS Code: Launches, extensions present
     - Razer Synapse: Detects all devices, RGB active
  
  6. Performance baseline:
     - Boot time: Record baseline
     - GPU performance: Run quick benchmark
     - AI inference: Test Ollama latency
     - Network: Record bandwidth (speedtest)

Verification:
  ✓ Boot time under 30 seconds
  ✓ All hardware detected and functional
  ✓ All software launches without errors
  ✓ Network connectivity confirmed
  ✓ Security features active
  ✓ Performance baseline recorded
```

### Validation Checkpoints

```
Checkpoint 7.1: User Profile
  ✓ User account created
  ✓ User can login with password
  ✓ User profile directory exists
  ✓ User has admin privileges (if needed)

Checkpoint 7.2: Network & Security
  ✓ Ethernet connected with valid IP
  ✓ DNS resolves correctly
  ✓ Firewall enabled
  ✓ Windows Defender active
  ✓ BitLocker encryption verified on P1, P2, P7, P9

Checkpoint 7.3: Razer Hardware
  ✓ Synapse 3 detects all devices
  ✓ All firmware validates (4-tier check)
  ✓ RGB profile set to Monado Blue
  ✓ All devices functional (RGB, input, audio)

Checkpoint 7.4: Performance Tuning
  ✓ Power plan: High Performance
  ✓ GPU running in max perf mode
  ✓ Background services minimized
  ✓ Startup programs essential only

Checkpoint 7.5: Final System State
  ✓ Boot time: <30 seconds (target 25s)
  ✓ All software launches successfully
  ✓ All hardware functional and detected
  ✓ Performance baseline recorded
```

---

## Stage 8: Post-Install Verification & Readiness (Duration: 10-15 minutes)

### Purpose
Complete final system validation, readiness checks, benchmarking, and welcome configuration before deployment.

### Process Flow

#### 8.1 System Readiness Check
```
Phase: Comprehensive System Validation
Input: All 7 previous stages complete
Actions:
  1. Boot integrity verification:
     - Boot time measurement: Record total time
     - All services started: Verify startup items completed
     - No boot errors: Check Event Viewer for critical errors
     - System stability: Run 5-minute stress test (no crashes)
  
  2. Disk integrity check:
     - All 9 partitions mount correctly: D:, E:, F:, G:, H:, I:, J:, K:, L:
     - File system integrity: chkdsk /scan on all partitions
     - Free space accurate: Compare against expected
     - BitLocker status: All encrypted partitions report encryption active
  
  3. Memory verification:
     - RAM detection: All [X] GB detected
     - Memory test: RAM stress test passes (e.g., MemTest86)
     - Swap/Virtual memory: Configured correctly
  
  4. CPU verification:
     - CPU detection: All cores detected (e.g., 16 cores)
     - CPU frequency: Boosts to rated speed under load
     - CPU test: All-core stress test for 5 minutes
  
  5. GPU verification:
     - GPU detection: nvidia-smi shows RTX 5090, 32 GB VRAM
     - GPU memory: All 32 GB accessible
     - GPU test: Benchmark shows target FPS
     - Temperature: Stable at 70-80°C under load
  
  6. Network verification:
     - Ethernet: Connected, IP address valid
     - DNS: nslookup google.com returns correct IP
     - Firewall: Enabled, Windows Update can connect
     - Network speed: Speed test shows expected speeds
  
  7. Software verification:
     - Critical apps launch: VS2022, Python, Node, Docker, Ollama
     - All services running: Docker, Ollama, Windows services
     - No startup errors: Event Viewer shows no critical errors
     - Update status: All critical updates installed

Verification:
  ✓ Boot time: <30 seconds
  ✓ All 9 partitions healthy
  ✓ No file system errors (chkdsk clean)
  ✓ All hardware verified and functional
  ✓ Network operational
  ✓ All software launched successfully
```

#### 8.2 Feature Verification
```
Phase: Feature Completeness Check
Input: All systems ready
Actions:
  1. GPU Acceleration:
     ✓ NVIDIA RTX 5090 detected and booting drivers
     ✓ CUDA 12.x toolkit functional
     ✓ PyTorch can access GPU (torch.cuda.is_available() = True)
     ✓ TensorFlow can access GPU
     ✓ Ollama using GPU for inference
  
  2. AI Hub:
     ✓ Docker container running (monado-ai-hub)
     ✓ Ollama service accessible on port 11434
     ✓ At least 2 LLM models downloaded and functional
     ✓ GPU passthrough confirmed in container
     ✓ Inference latency acceptable (<1s first token)
  
  3. Development Environment:
     ✓ Visual Studio 2022 functional with C#/.NET
     ✓ Python 3.12 with pip and ML packages
     ✓ Node.js with npm and global packages
     ✓ Git configured and GitHub Desktop working
     ✓ VS Code with extensions installed
     ✓ CMake and build tools available
  
  4. System Features:
     ✓ BitLocker encryption on P1, P2, P7, P9
     ✓ Windows Defender active and updated
     ✓ Firewall enabled with appropriate rules
     ✓ Windows Update configured
     ✓ User profile created and functional
  
  5. Hardware Integration:
     ✓ Razer Synapse 3 detects all devices
     ✓ RGB profiles available and customizable
     ✓ Firmware validation passed (4-tier)
     ✓ All devices functional
  
  6. Partition Setup:
     ✓ P1 (Windows OS): ~35 GB used, 25+ GB free
     ✓ P2 (App Data): Used for config, logs, caches
     ✓ P3 (AI Models): Contains downloaded LLMs
     ✓ P4 (AI Cache): Ready for runtime cache
     ✓ P5 (Programs): All dev tools installed
     ✓ P6 (Temp): Ready for Windows/app temp
     ✓ P8 (Recovery): WinRE present and functional
     ✓ P9 (Profiles): User directory created

Verification:
  ✓ All GPU features operational
  ✓ AI hub fully functional
  ✓ Development environment complete
  ✓ Security features active
  ✓ Hardware fully integrated
  ✓ All partitions configured correctly
```

#### 8.3 Performance Benchmarking
```
Phase: Baseline Performance Metrics
Input: System fully validated
Actions:
  1. Boot Performance:
     Metric: Boot time (power-on to desktop)
     Baseline target: 20-30 seconds
     Measurement:
       - Start: Power button pressed
       - End: Monado app available (or main UI ready)
       - Record: Total time, Stage breakdown
  
  2. CPU Performance:
     Test: Cinebench R23 multi-core
     Baseline target: [depends on CPU, e.g., 15,000+ points]
     Record: Multi-core score, thermal under load
  
  3. GPU Performance:
     Test: 3DMark Time Spy
     Baseline target: [depends on GPU/system, e.g., 25,000+ points]
     Record: Score, VRAM speed, temperature
     Specific to Monado: RTX 5090 Boost clock target 2.5+ GHz
  
  4. Memory Performance:
     Test: MemTest86 (quick 5-minute pass)
     Baseline target: 0 errors
     Record: Memory speed (MHz), latency (ns)
  
  5. AI Inference Performance:
     Test: Ollama inference latency
     Commands:
       - llama2:7b-chat: Benchmark 10 prompts, average latency
       - mistral:7b-instruct: Benchmark 10 prompts
       - dolphin-mixtral:8x7b: Benchmark 5 prompts
     Baseline target:
       - First token: <1000ms (latency to first response)
       - Throughput: 50-200 tokens/sec (depends on model)
     Record:
       - First token latency (avg, min, max)
       - Tokens per second
       - Temperature under load
  
  6. Network Performance:
     Test: Speedtest CLI
     Baseline target: Download >300 Mbps (depends on ISP)
     Record: Download, upload, ping, jitter
  
  7. Disk Performance:
     Test: ATTO Disk Benchmark
     Baseline target:
       - Sequential read: >1000 MB/s (NVMe)
       - Sequential write: >800 MB/s
     Record: Read/write speeds for 4K and large block sizes
  
  Benchmark results file: H:\Performance\baseline_benchmarks.json
  Format:
    {
      "timestamp": "2024-01-15T10:30:00Z",
      "system": {
        "cpu": "[CPU model]",
        "gpu": "NVIDIA RTX 5090",
        "ram": "32 GB"
      },
      "benchmarks": {
        "boot_time_sec": 24,
        "cpu_cinebench_r23": 15200,
        "gpu_3dmark_timespu": 25400,
        "ai_inference_llama2_firsttoken_ms": 450,
        "ai_inference_llama2_tokens_per_sec": 120,
        "network_download_mbps": 450,
        "disk_read_mbps": 1800
      }
    }

Verification:
  ✓ Boot time within acceptable range (20-30s)
  ✓ CPU performing to specification
  ✓ GPU at expected performance level
  ✓ Memory integrity verified
  ✓ AI inference latency acceptable
  ✓ Network connectivity fast enough
  ✓ Disk performance adequate
```

#### 8.4 Welcome Wizard & Configuration
```
Phase: Final User Configuration
Input: System fully benchmarked and verified
Actions:
  1. Launch Monado Blade Welcome Wizard:
     - User introduction: "Welcome to Monado Blade"
     - System summary: Display hardware specs
     - Quick setup options:
       ☐ Configure AI provider API keys
       ☐ Setup developer profiles
       ☐ Configure Razer RGB preferences
       ☐ Enable cloud sync (optional)
       ☐ Setup scheduled backups
  
  2. Display system readiness status:
     ✓ Windows OS: Ready
     ✓ Drivers: All installed and functional
     ✓ GPU (RTX 5090): Ready, 32 GB VRAM available
     ✓ AI Hub: Ready, 4 models loaded
     ✓ Development Environment: Ready
     ✓ Security: Enabled (BitLocker, Defender)
     ✓ Network: Connected
  
  3. Display key information:
     - System Name: [hostname]
     - User Account: [username]
     - Available Disk Space:
       * P1 (Windows): [X] GB free
       * P3 (Models): [X] GB free
       * P5 (Programs): [X] GB free
     - Network: [IP address], [ISP speed]
     - GPU Memory: 32 GB full capacity
  
  4. Offer post-setup options:
     - Create system restore point: [Yes/No]
     - Backup configuration: [Yes/No]
     - Setup scheduled optimization: [Yes/No]
     - Enable debug logging: [Yes/No]
  
  5. Create system restore point:
     If user agrees:
       - Create Windows System Restore point
       - Name: "Monado Blade - Stage 8 Complete"
       - Save location: System managed
       - Size: ~10-20 GB on system drive

Verification:
  ✓ Welcome wizard displays all system info
  ✓ All features showing "Ready"
  ✓ No errors or warnings
  ✓ User can proceed with configuration
  ✓ System restore point created (if selected)
```

#### 8.5 System Ready Notification
```
Phase: Deployment Ready
Input: All verifications complete
Actions:
  1. Display final status screen:
     
     ╔═══════════════════════════════════════════════════════════════╗
     ║           MONADO BLADE - SYSTEM READY FOR DEPLOYMENT          ║
     ╠═══════════════════════════════════════════════════════════════╣
     ║                                                               ║
     ║  ✓ Stage 1: USB Detection & Preparation         [COMPLETE]   ║
     ║  ✓ Stage 2: Partition Setup                     [COMPLETE]   ║
     ║  ✓ Stage 3: OS Installation                     [COMPLETE]   ║
     ║  ✓ Stage 4: Driver Installation                 [COMPLETE]   ║
     ║  ✓ Stage 5: Program Installation                [COMPLETE]   ║
     ║  ✓ Stage 6: AI Hub Setup                        [COMPLETE]   ║
     ║  ✓ Stage 7: System Configuration                [COMPLETE]   ║
     ║  ✓ Stage 8: Post-Install Verification           [COMPLETE]   ║
     ║                                                               ║
     ║  Total Build Time: 2 hours 45 minutes                        ║
     ║  System Boot Time: 24 seconds                                ║
     ║  AI Inference Ready: Yes                                     ║
     ║                                                               ║
     ║  Hardware Summary:                                           ║
     ║    GPU: NVIDIA RTX 5090 (32 GB VRAM) ✓                      ║
     ║    CPU: [X cores] ✓                                         ║
     ║    RAM: 32 GB ✓                                             ║
     ║    Disk: 9 partitions, total [X] GB ✓                       ║
     ║    Network: Connected @ [speed] Mbps ✓                      ║
     ║    Security: BitLocker + Defender ✓                         ║
     ║                                                               ║
     ║  Next Steps:                                                 ║
     ║    1. Review system information above                        ║
     ║    2. Configure optional settings in Monado app              ║
     ║    3. Add cloud credentials if desired                       ║
     ║    4. Start using Monado Blade!                              ║
     ║                                                               ║
     ║  [Finish] [Review Details] [Export System Report]            ║
     ║                                                               ║
     ╚═══════════════════════════════════════════════════════════════╝
  
  2. Export system report:
     - File: H:\SystemReport_[date].html
     - Contents:
       * Hardware specifications
       * Installed software list
       * Partition configuration
       * Network configuration
       * Performance baselines
       * Security status
  
  3. Create backup of configuration:
     - Backup location: E:\Backups\config_stage8_[date].bak
     - Contents:
       * Windows configuration
       * Installed programs list
       * BitLocker recovery keys
       * User profiles
       * Network settings
       * Razer profile configuration

Verification:
  ✓ All 8 stages show [COMPLETE]
  ✓ System report generated and saved
  ✓ Configuration backup created
  ✓ User can proceed with system usage
```

### Validation Checkpoints

```
Checkpoint 8.1: System Readiness
  ✓ Boot time: 20-30 seconds (target 25s)
  ✓ All 9 partitions healthy and accessible
  ✓ All hardware detected and functional
  ✓ No errors in Event Viewer critical/error logs
  ✓ All security features active

Checkpoint 8.2: Feature Completeness
  ✓ GPU acceleration working (CUDA, neural networks)
  ✓ AI Hub functional (Ollama + models)
  ✓ Development environment complete (all tools)
  ✓ Security features enabled
  ✓ Hardware integration complete
  ✓ All partition purposes functional

Checkpoint 8.3: Performance Validation
  ✓ Boot time within acceptable range
  ✓ CPU performance at expected level
  ✓ GPU performance at expected level
  ✓ AI inference latency acceptable
  ✓ Network speed adequate
  ✓ Disk performance acceptable

Checkpoint 8.4: Deployment Readiness
  ✓ Welcome wizard displays all info
  ✓ System report generated
  ✓ Configuration backup created
  ✓ All systems report "Ready"
  ✓ No outstanding issues or errors
```

### Error Handling

```
Error: One or more stages failed validation
  → Message: "Stage [X] failed validation. System not ready."
  → Action: Identify failing component
  → Recovery: Go back to failing stage, fix issue, re-validate

Error: Performance below expected baseline
  → Message: "Performance below baseline on [component]"
  → Action: Investigate cause (thermal, driver, resource)
  → Recovery: Fix underlying issue, re-benchmark

Error: Security features not active
  → Message: "Security feature [X] not active. Cannot proceed."
  → Action: Enable security feature
  → Recovery: Fix security configuration, verify again

Error: AI Hub cannot reach Ollama
  → Message: "AI Hub connection failed: Cannot reach Ollama service"
  → Action: Check Docker container status
  → Recovery: Restart container, verify GPU passthrough

Error: Insufficient disk space on any partition
  → Message: "Partition [X] has insufficient space"
  → Action: Calculate actual usage vs. allocated
  → Recovery: Resize partitions or clean up space
```

---

## Recovery & Rollback Procedures

### Complete Rollback to Stage N

If at any stage a critical failure occurs, rollback is possible to previous stage:

```
Rollback from Stage X to Stage Y:
  1. Identify failure point in Stage X
  2. Document error and cause
  3. Restore from backup (if available):
     - Windows System Restore: Revert to Stage Y restore point
     - Partition backup: Restore Partition [X] from backup
  4. Continue from Stage Y+1
  
Recovery is easiest if:
  - System Restore point created after each stage
  - Partition backups created after critical stages
  - Error logs preserved for analysis

Stages with most critical recovery needs:
  - Stage 3 (OS Installation): Keep backup of Windows files
  - Stage 4 (Drivers): Keep driver installers
  - Stage 6 (AI Hub): Keep model downloads
```

---

## Success Criteria & Validation

### All 8 Stages Complete

```
✅ Stage 1: USB Detection & Preparation
   ✓ USB device detected, validated, confirmed
   ✓ Backup created (if data existed)
   ✓ Ready for format

✅ Stage 2: Partition Setup
   ✓ 9 partitions created with correct sizes
   ✓ All partitions formatted (NTFS)
   ✓ BitLocker encryption enabled on P1, P2, P7, P9
   ✓ Recovery keys backed up
   ✓ All partitions verified and accessible

✅ Stage 3: OS Installation
   ✓ Windows 11 installed on Partition 1
   ✓ Boot loader configured
   ✓ UEFI settings correct
   ✓ Secure Boot enabled
   ✓ Boot time under 30 seconds

✅ Stage 4: Driver Installation
   ✓ Chipset drivers installed
   ✓ GPU drivers (NVIDIA RTX 5090) with CUDA
   ✓ Audio drivers (THX Spatial Audio)
   ✓ Network drivers
   ✓ Razer drivers (Synapse 3 + Chroma SDK)
   ✓ No unknown devices in Device Manager

✅ Stage 5: Program Installation
   ✓ Visual Studio 2022 operational
   ✓ Python 3.12 with ML packages
   ✓ Node.js + npm
   ✓ Git + GitHub Desktop
   ✓ Docker Desktop with GPU support
   ✓ Ollama installed and ready
   ✓ Jupyter Lab available
   ✓ VS Code with extensions

✅ Stage 6: AI Hub Setup
   ✓ WSL2 configured with Ubuntu
   ✓ Docker container running (monado-ai-hub)
   ✓ GPU passthrough verified in container
   ✓ Ollama service accessible on port 11434
   ✓ Minimum 2 LLM models downloaded
   ✓ Inference latency acceptable (<1s first token)

✅ Stage 7: System Configuration
   ✓ User profiles created
   ✓ Network configured (Ethernet, DNS)
   ✓ Security hardened (Defender, UAC, firewall)
   ✓ Razer firmware validated (4-tier check)
   ✓ Performance tuning applied
   ✓ Credentials securely stored
   ✓ Final validation passed

✅ Stage 8: Post-Install Verification
   ✓ Boot time: 20-30 seconds
   ✓ All hardware verified functional
   ✓ All software launches successfully
   ✓ Performance baselines recorded
   ✓ System ready for deployment
   ✓ Welcome wizard completed
```

---

## Total Build Time Estimate

```
Stage 1: USB Detection & Preparation        5-10 min
Stage 2: Partition Setup                    10-15 min
Stage 3: OS Installation                    15-25 min
Stage 4: Driver Installation                10-20 min
Stage 5: Program Installation               45-90 min
Stage 6: AI Hub Setup                       30-45 min
Stage 7: System Configuration               20-30 min
Stage 8: Post-Install Verification          10-15 min
                                           ─────────
Total Build Time                           145-250 min
                                           2.5-4.2 hours

Typical build (normal internet, SSD):       ~3 hours
Fast build (fast internet, cached):         ~2.5 hours
Slow build (slow internet, all fresh):      ~4 hours
```

---

## Next Phase: Implementation (Phase 2)

This architecture serves as blueprint for Phase 2 implementation:

1. **USB Builder Application** - Implement 8-stage wizard
2. **Boot Sequence Orchestration** - Implement 10-stage boot with animations
3. **Hardware Integration Modules** - GPU, Razer, THX drivers
4. **Deployment & Testing** - Full end-to-end system validation

---

**Document Complete**: USB Builder Architecture v1.0
**Status**: Ready for Phase 2 Implementation
**Date**: 2024
**Author**: Hermes-1D - USB Boot Architect
