---
name: xcore-runtime
description: Configure, validate, route, train, and observe the HELIOS XCore runtime without granting it Azure production or identity privileges.
---

# XCore Runtime

## Purpose

Operate XCore as the adaptive specialist layer behind Hermes and AIHub. XCore owns bounded routing, training-cycle evaluation, engine selection, worker topology, and redacted memory summaries. It does not own Azure deployment, Entra permissions, secrets, or production approvals.

## Runtime topology

- Docker: gateway/API and isolated worker services;
- WSL2: GPU-capable trainer and Python engine runtime;
- Hyper-V: security-isolation workers;
- C#: enterprise integration policy and mesh-consensus host;
- Python: routing, training, optimization, and model engines;
- C++: low-memory security and performance engines.

## Setup sequence

1. Validate Python, .NET, Docker, WSL2, Hyper-V, CUDA visibility, and repository paths.
2. Load the XCore agent, engine, security, and VM topology manifests.
3. Verify the AIHub health, task, fleet, engine, security, and knowledge endpoints.
4. Register workers with capabilities rather than broad generic roles.
5. Run a contextual-bandit route test using a non-production synthetic task.
6. Verify task history, reinforcement memory, and vector memory stores.
7. Confirm memory outputs are redacted and do not expose secrets or user data.
8. Run one approved training cycle in development mode.
9. Generate routing, score, reflection, engine, and capacity evidence.
10. Register the health state with Hermes and the deployment supervisor.

## Required contracts

- correlation ID on every task and result;
- stable worker and engine IDs;
- declared task type, difficulty, latency budget, and data classification;
- bounded retries and timeout;
- deterministic task/evaluation evidence;
- approval state for training, fleet scaling, engine enablement, and memory migrations.

## Allowed operations

- health checks and configuration validation;
- worker registration and bounded task routing;
- engine-catalog reads and recommendations;
- development training cycles;
- evaluation and telemetry submission;
- capacity recommendations.

## Prohibited operations

- Azure resource deployment;
- Entra, Graph, RBAC, or Key Vault writes;
- secret retrieval or output;
- raw private-memory publishing;
- unbounded self-modification;
- production training without approval;
- bypassing GitHub environment gates.

## Evidence

Return a normalized result containing worker inventory, route decision, score, reflection, engine selection, resource use, warnings, memory-redaction result, and evidence links.
