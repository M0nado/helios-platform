#Requires -Version 7.0
#Requires -RunAsAdministrator
# =============================================================
# 03_phase3_hyperv_services.ps1
# Phase 3: Hyper-V, Windows Sandbox, Postgres, Qdrant,
#          and Hermes Runtime Skeleton
# NOT FOR BLIND EXECUTION — Review placeholder values first.
# =============================================================

$ErrorActionPreference = "Stop"
$LogFile = "D:\DevDrive\logs\phase3_services_{0}.log" -f (Get-Date -Format "yyyyMMdd_HHmmss")
function Log($msg) { $line = "[$(Get-Date -Format 's')] $msg"; Write-Host $line; $line | Out-File $LogFile -Append }

Log "=== PHASE 3: HYPER-V, SANDBOX, POSTGRES, QDRANT, HERMES ==="

# -----------------------------------------------------------
# ENABLE HYPER-V
# -----------------------------------------------------------
Log "=== ENABLING HYPER-V ==="
Enable-WindowsOptionalFeature -Online -FeatureName Microsoft-Hyper-V-All -NoRestart
Log "Hyper-V enabled. Reboot may be required."

# -----------------------------------------------------------
# ENABLE WINDOWS SANDBOX
# -----------------------------------------------------------
Log "=== ENABLING WINDOWS SANDBOX ==="
Enable-WindowsOptionalFeature -Online -FeatureName Containers-DisposableClientVM -NoRestart
Log "Windows Sandbox enabled."

# -----------------------------------------------------------
# HYPER-V VIRTUAL SWITCHES
# -----------------------------------------------------------
Log "=== HYPER-V VIRTUAL SWITCHES ==="
$physicalNic = "{your-physical-nic-adapter-name}"  # REVIEW: Get-NetAdapter to find name
# External vSwitch (for internet/Azure testing in Mode A)
New-VMSwitch -Name "HermesNet" -NetAdapterName $physicalNic -AllowManagementOS $true -ErrorAction SilentlyContinue
Log "External vSwitch 'HermesNet' created (bound to $physicalNic)"
# Internal vSwitch (isolated VM communication, used in Mode B)
New-VMSwitch -Name "HermesInternal" -SwitchType Internal -ErrorAction SilentlyContinue
Log "Internal vSwitch 'HermesInternal' created"

# -----------------------------------------------------------
# POSTGRES SETUP (Docker)
# -----------------------------------------------------------
Log "=== POSTGRES SETUP ==="
# Pull image
docker pull postgres:16
Log "postgres:16 image pulled."

# Create data directory
New-Item -ItemType Directory -Path "D:\DevDrive\postgres\data" -Force | Out-Null

# NOTE: POSTGRES_PASSWORD must be set as an environment variable before running this script.
# Never hardcode credentials. Set via: $env:POSTGRES_PASSWORD = (Read-Host -AsSecureString "Postgres Password")
docker run -d `
    --name hermes-postgres `
    --restart unless-stopped `
    -e POSTGRES_PASSWORD=$env:POSTGRES_PASSWORD `
    -v "D:\DevDrive\postgres\data:/var/lib/postgresql/data" `
    -p 127.0.0.1:5432:5432 `
    postgres:16
Log "Postgres container 'hermes-postgres' started."

# Create databases
Start-Sleep -Seconds 8
$dbs = @("hermes_logs", "hermes_features", "hermes_models", "hermes_feedback")
foreach ($db in $dbs) {
    docker exec hermes-postgres psql -U postgres -c "CREATE DATABASE $db;"
    Log "Database created: $db"
}
Log "PSEUDO: Apply DDL from Deliverable 2.1 to initialize table schemas."
Log "PSEUDO: Edit pg_hba.conf in D:\DevDrive\postgres\data to restrict to local access only."

# -----------------------------------------------------------
# QDRANT SETUP (Docker)
# -----------------------------------------------------------
Log "=== QDRANT SETUP ==="
docker pull qdrant/qdrant
Log "qdrant/qdrant image pulled."

New-Item -ItemType Directory -Path "D:\DevDrive\qdrant\storage" -Force | Out-Null
New-Item -ItemType Directory -Path "D:\DevDrive\qdrant\config" -Force | Out-Null

# Write Qdrant config
$qdrantConfig = @"
storage:
  storage_path: /qdrant/storage
service:
  host: 127.0.0.1
  http_port: 6333
  grpc_port: 6334
log_level: INFO
"@
$qdrantConfig | Set-Content "D:\DevDrive\qdrant\config\config.yaml" -Encoding UTF8
Log "Qdrant config written."

docker run -d `
    --name hermes-qdrant `
    --restart unless-stopped `
    -p 127.0.0.1:6333:6333 `
    -p 127.0.0.1:6334:6334 `
    -v "D:\DevDrive\qdrant\storage:/qdrant/storage" `
    -v "D:\DevDrive\qdrant\config:/qdrant/config" `
    qdrant/qdrant
Log "Qdrant container 'hermes-qdrant' started."

# Create collections
Start-Sleep -Seconds 5
$collectionsPayload = @(
    @{ name = "embeddings_v1";       vectors = @{ size = 128; distance = "Cosine" } },
    @{ name = "routing_contexts";    vectors = @{ size = 128; distance = "Cosine" } },
    @{ name = "feature_signatures";  vectors = @{ size = 768; distance = "Cosine" } }
)
foreach ($col in $collectionsPayload) {
    $body = $col | ConvertTo-Json -Depth 5
    Invoke-RestMethod -Uri "http://127.0.0.1:6333/collections/$($col.name)" -Method Put -Body $body -ContentType "application/json"
    Log "Qdrant collection created: $($col.name)"
}

# -----------------------------------------------------------
# HERMES RUNTIME SKELETON
# -----------------------------------------------------------
Log "=== HERMES RUNTIME SKELETON ==="
$hermesBase = "D:\DevDrive\hermes"

# Orchestrator skeleton
$orchestratorPy = @"
# orchestrator.py — Hermes XCore Orchestrator Skeleton
# See Deliverable 2.9 for full class design.
import logging
import asyncio
from pathlib import Path

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger("hermes.orchestrator")

class HermesOrchestrator:
    def __init__(self, config_path: str):
        self.config_path = Path(config_path)
        self.nodes = {}
        self.running = False
        logger.info("HermesOrchestrator initialized from %s", config_path)

    async def run_orchestration_loop(self):
        self.running = True
        logger.info("Orchestration loop started.")
        while self.running:
            await asyncio.sleep(1)

if __name__ == "__main__":
    orch = HermesOrchestrator("D:/DevDrive/hermes/configs/hermes_global.yaml")
    asyncio.run(orch.run_orchestration_loop())
"@
$orchestratorPy | Set-Content "$hermesBase\orchestrator\orchestrator.py" -Encoding UTF8
Log "Skeleton written: orchestrator.py"

# Node template
New-Item -ItemType Directory -Path "$hermesBase\nodes\node_template" -Force | Out-Null
$nodeRunner = @"
# runner.py — Hermes Node Runner Template
# See Deliverable 2.9 for full design.
import logging
import torch
from fastapi import FastAPI

logger = logging.getLogger("hermes.node")
app = FastAPI()

@app.get("/health")
def health():
    gpu_ok = torch.cuda.is_available()
    mem = torch.cuda.memory_reserved() if gpu_ok else 0
    return {"status": "ok", "gpu": gpu_ok, "gpu_mem_reserved_bytes": mem}

@app.post("/v1/infer")
async def infer(payload: dict):
    # Task execution stub
    logger.info("Received inference task: %s", payload.get("task_id"))
    return {"status": "accepted", "task_id": payload.get("task_id")}
"@
$nodeRunner | Set-Content "$hermesBase\nodes\node_template\runner.py" -Encoding UTF8
Log "Skeleton written: nodes\node_template\runner.py"

# Global Hermes config YAML
$globalConfig = @"
hermes:
  version: "1.0.0"
  orchestrator:
    host: "127.0.0.1"
    port: 8700
    log_level: INFO
  nodes:
    - id: node_01
      type: inference
      backend: wsl2
      gpu: true
      port: 8701
    - id: node_02
      type: feature_eng
      backend: wsl2
      gpu: false
      port: 8702
    - id: node_03
      type: training
      backend: hyper-v
      gpu: true
      port: 8703
  postgres:
    host: "127.0.0.1"
    port: 5432
    db: hermes_logs
  qdrant:
    host: "127.0.0.1"
    port: 6333
"@
$globalConfig | Set-Content "$hermesBase\configs\hermes_global.yaml" -Encoding UTF8
Log "hermes_global.yaml written."

Log "Phase 3 COMPLETE. Review log at $LogFile"
Deliverable 2 — Hermes XCore Training Loop Design
This deliverable provides the complete software design for the Hermes XCore continuous training pipeline. All modules are Python pseudo-code intended to be implemented, tested, and deployed by a qualified engineer. Module files are deployed to D:\DevDrive\hermes\ and installed into the D:\DevDrive\ai-hub\venvs\hermes-core virtual environment.

2.1 — Logging Schema (SQL DDL)
