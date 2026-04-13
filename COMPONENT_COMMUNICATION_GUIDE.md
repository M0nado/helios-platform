# HELIOS Platform - Component Communication Guide

Complete reference for inter-component communication, API endpoints, data flow, event handling, and protocol specifications.

---

## 🔌 Communication Architecture

### Overview
```
┌──────────────────────────────────────────────────┐
│           API Gateway / Orchestrator              │
│    (Main helios-platform coordination layer)      │
└──────────────────────────────────────────────────┘
              │         │         │
    ┌─────────┼─────────┼─────────┴─────────┐
    │         │         │                   │
┌───▼──┐  ┌──▼──┐  ┌──▼──┐  ┌──────┐  ┌───▼──┐
│ Dev  │  │Build│  │Monado│ │ AI   │  │Security
│ Hub  │  │Agts │  │Blade │  │ Hub  │  │Setup
└──────┘  └──────┘  └──────┘  └──────┘  └────────
    │         │         │         │         │
    └─────────┼─────────┼─────────┼─────────┘
              │
          ┌───▼───┐
          │  GUI  │
          │ Frame │
          └───────┘
```

### Communication Protocols

| Protocol | Port | Purpose | Usage |
|----------|------|---------|-------|
| **HTTPS/REST** | 443 | Primary API communication | All components |
| **WebSocket** | 8443 | Real-time data streaming | GUI ↔ All |
| **gRPC** | 5000 | High-performance RPC | Agent ↔ Agent |
| **AMQP** | 5672 | Message queue | Event distribution |
| **SSH** | 22 | Secure shell | Dev/debugging |
| **Local IPC** | N/A | Inter-process | Local agents |

---

## 📡 REST API Endpoints

### Orchestrator API (Main Platform)

**Base URL:** `https://localhost:5000/api/v1`

#### System Control

```http
POST /system/start
Content-Type: application/json

{
  "phase": 1,
  "components": ["all"],
  "dryRun": false
}

Response: 202 Accepted
{
  "deploymentId": "uuid-1234",
  "status": "initiating",
  "estimatedTime": "35 minutes",
  "components": [
    {"name": "dev-ai-hub", "status": "queued", "eta": "4 min"},
    {"name": "build-agents", "status": "queued", "eta": "25 min"}
  ]
}
```

```http
GET /system/status
Response: 200 OK
{
  "overallStatus": "running",
  "phase": 2,
  "progress": 45,
  "components": [
    {
      "name": "build-agents",
      "status": "active",
      "healthScore": 98,
      "uptime": "12m 35s"
    }
  ],
  "nextPhase": 3,
  "estimatedCompletion": "2024-01-20T15:45:00Z"
}
```

#### Component Management

```http
GET /components
Response: 200 OK
{
  "components": [
    {
      "id": "monado-blade",
      "name": "Monado Blade",
      "version": "1.0.0",
      "status": "active",
      "phase": 2,
      "healthScore": 100,
      "endpoints": [
        "https://localhost:5001/api/v1/patterns",
        "https://localhost:5001/api/v1/profiles"
      ]
    }
  ]
}
```

```http
POST /components/{id}/restart
Response: 202 Accepted
{
  "componentId": "build-agents",
  "action": "restart",
  "status": "in_progress",
  "duration": "30 seconds"
}
```

---

### Build Agents API

**Base URL:** `https://localhost:5001/api/v1`

#### Agent Control

```http
GET /agents
Response: 200 OK
{
  "agents": [
    {
      "id": 1,
      "name": "Storage",
      "status": "completed",
      "duration": "8m 12s",
      "tasks": 15,
      "tasksCompleted": 15,
      "healthScore": 100
    },
    {
      "id": 2,
      "name": "Security",
      "status": "active",
      "duration": "6m 45s",
      "tasks": 28,
      "tasksCompleted": 18,
      "healthScore": 98,
      "currentTask": "Applying AppLocker rules"
    }
  ],
  "queue": {
    "total": 156,
    "pending": 89,
    "inProgress": 8,
    "completed": 59
  }
}
```

```http
POST /agents/{id}/pause
Response: 200 OK
{
  "agentId": 2,
  "action": "paused",
  "status": "success",
  "currentTask": "stopped_gracefully"
}
```

#### Task Management

```http
GET /queue/status
Response: 200 OK
{
  "depth": 156,
  "throughput": "12 tasks/sec",
  "averageWait": "45 seconds",
  "highPriority": 8,
  "normalPriority": 120,
  "lowPriority": 28
}
```

---

### Monado Blade API

**Base URL:** `https://localhost:5002/api/v1`

#### Pattern Analysis

```http
POST /patterns/analyze
Content-Type: application/json

{
  "systemMetrics": {
    "cpuUsage": 45.2,
    "memoryUsage": 62.8,
    "diskIO": 125.5,
    "networkBandwidth": 850.3
  },
  "timeWindow": "1h"
}

Response: 200 OK
{
  "patterns": [
    {
      "type": "cpu_spike",
      "confidence": 0.92,
      "trigger": "Every hour at :30",
      "severity": "low",
      "recommendation": "Schedule non-critical tasks at :00"
    }
  ],
  "anomalies": []
}
```

#### Profile Generation

```http
POST /profiles/generate
Content-Type: application/json

{
  "baselineDataHours": 24,
  "profileType": "performance"
}

Response: 201 Created
{
  "profileId": "profile-abc123",
  "profileType": "performance",
  "baselineMetrics": {
    "avgCPU": 42.5,
    "avgMemory": 58.3,
    "avgDiskIO": 102.1,
    "p95Latency": 245
  },
  "createdAt": "2024-01-20T14:23:00Z",
  "validityPeriod": "30 days"
}
```

#### Predictions

```http
GET /metrics/predict?profileId=profile-abc123&horizon=1h
Response: 200 OK
{
  "predictions": [
    {
      "timestamp": "2024-01-20T16:00:00Z",
      "predictedCPU": 48.2,
      "confidence": 0.85,
      "anomalyProbability": 0.05
    },
    {
      "timestamp": "2024-01-20T17:00:00Z",
      "predictedCPU": 52.1,
      "confidence": 0.78,
      "anomalyProbability": 0.12
    }
  ]
}
```

---

### Security Setup API

**Base URL:** `https://localhost:5003/api/v1`

#### Security Verification

```http
POST /security/verify
Content-Type: application/json

{
  "layers": [1, 2, 3, 4, 5, 6, 7, 8],
  "strict": true
}

Response: 200 OK
{
  "overallStatus": "protected",
  "layers": [
    {
      "layer": 1,
      "name": "Physical",
      "status": "verified",
      "tpmVersion": "2.0",
      "attestationStatus": "valid"
    },
    {
      "layer": 2,
      "name": "Authentication",
      "status": "verified",
      "mfaEnabled": true,
      "entraIdConnected": true
    }
  ],
  "vulnerabilities": 0,
  "lastAudit": "2024-01-20T10:30:00Z"
}
```

#### Policy Management

```http
GET /policies/list
Response: 200 OK
{
  "policies": [
    {
      "id": "applock-001",
      "name": "Block Unauthorized Executables",
      "type": "AppLocker",
      "rules": 52,
      "status": "active",
      "violations": 3,
      "lastModified": "2024-01-19T08:00:00Z"
    },
    {
      "id": "firewall-001",
      "name": "Network Segmentation",
      "type": "Firewall",
      "rules": 28,
      "status": "active",
      "blocks": 127,
      "lastModified": "2024-01-20T09:30:00Z"
    }
  ]
}
```

#### Vault Operations

```http
POST /vault/secret
Content-Type: application/json

{
  "name": "db-connection-string",
  "value": "Server=...;Password=...",
  "expiresIn": "90 days",
  "tags": ["database", "production"]
}

Response: 201 Created
{
  "secretId": "secret-xyz789",
  "name": "db-connection-string",
  "created": "2024-01-20T14:25:00Z",
  "expires": "2024-04-20T14:25:00Z",
  "rotationSchedule": "every 90 days"
}
```

---

### AI Hub API

**Base URL:** `https://localhost:5004/api/v1`

#### Query Routing

```http
POST /query
Content-Type: application/json

{
  "prompt": "Analyze system performance trends",
  "maxCost": 0.50,
  "models": ["gpt-4", "claude-3", "gemini"],
  "timeout": 30
}

Response: 200 OK
{
  "queryId": "query-xyz123",
  "selectedModel": "claude-3",
  "cost": 0.015,
  "latency": 1245,
  "response": "System shows consistent performance improvement...",
  "quality": 0.92,
  "alternatives": [
    {
      "model": "gpt-4",
      "estimatedCost": 0.03,
      "estimatedLatency": 2100
    }
  ]
}
```

#### Model Management

```http
GET /models/list
Response: 200 OK
{
  "models": [
    {
      "id": "gpt-4",
      "provider": "openai",
      "cost": 0.03,
      "rateLimit": 100,
      "availability": "available",
      "latency": 2100,
      "quality": 0.95
    },
    {
      "id": "local-mistral",
      "provider": "ollama",
      "cost": 0,
      "rateLimit": 1000,
      "availability": "available",
      "latency": 500,
      "quality": 0.82
    }
  ],
  "activeModels": 12,
  "totalCostToday": 145.32
}
```

#### Cost Tracking

```http
GET /cost/current
Response: 200 OK
{
  "timeframe": "today",
  "totalCost": 145.32,
  "costByModel": {
    "gpt-4": 78.50,
    "claude-3": 45.20,
    "gemini": 18.75,
    "ollama": 0.00
  },
  "costByHour": [
    {"hour": "14:00", "cost": 12.50},
    {"hour": "15:00", "cost": 18.75}
  ],
  "projectedDaily": 348.77,
  "forecastWeekly": 2441.39,
  "budget": 5000,
  "remaining": 4854.68
}
```

---

### GUI Framework API

**Base URL:** `https://localhost:5005/api/v1`

#### Dashboard Data

```http
GET /dashboard/overview
Response: 200 OK
{
  "timestamp": "2024-01-20T14:30:00Z",
  "systemHealth": 98,
  "activeAgents": 6,
  "tasksCompleted": 1200,
  "uptime": "12d 8h 45m",
  "alerts": 2,
  "criticalIssues": 0,
  "warnings": 3,
  "info": 8
}
```

```http
GET /dashboard/performance
Response: 200 OK
{
  "timestamp": "2024-01-20T14:30:00Z",
  "cpu": {
    "current": 45.2,
    "average": 42.1,
    "peak": 78.5,
    "cores": [45, 46, 44, 45]
  },
  "memory": {
    "used": 16.2,
    "total": 32,
    "percentage": 50.6,
    "trend": "stable"
  },
  "disk": {
    "used": 250,
    "total": 500,
    "percentage": 50,
    "readSpeed": 125.5,
    "writeSpeed": 98.2
  }
}
```

#### Alert Management

```http
POST /alerts/acknowledge
Content-Type: application/json

{
  "alertId": "alert-123",
  "acknowledgedBy": "admin@company.com",
  "action": "acknowledged"
}

Response: 200 OK
{
  "alertId": "alert-123",
  "status": "acknowledged",
  "acknowledgedAt": "2024-01-20T14:31:00Z",
  "resolvedAt": null
}
```

---

## 📨 Event Streaming (WebSocket)

### WebSocket Connection

**Endpoint:** `wss://localhost:8443/events`

```javascript
// Connect
const ws = new WebSocket('wss://localhost:8443/events');

// Subscribe to events
ws.send(JSON.stringify({
  action: 'subscribe',
  channels: [
    'agent:all',
    'security:violations',
    'ai:cost',
    'performance:alerts'
  ]
}));

// Receive events
ws.onmessage = (event) => {
  const message = JSON.parse(event.data);
  console.log('Event:', message);
};
```

### Event Types

#### Agent Events
```json
{
  "type": "agent:statusChange",
  "timestamp": "2024-01-20T14:32:15Z",
  "agent": "Security",
  "agentId": 2,
  "status": "completed",
  "duration": "12m 45s",
  "tasksCompleted": 28,
  "result": "success"
}
```

#### Security Events
```json
{
  "type": "security:policyViolation",
  "timestamp": "2024-01-20T14:32:20Z",
  "layer": 5,
  "severity": "high",
  "description": "Unauthorized executable execution attempt",
  "process": "unknown.exe",
  "user": "system",
  "action": "blocked"
}
```

#### AI Events
```json
{
  "type": "ai:costAlert",
  "timestamp": "2024-01-20T14:32:30Z",
  "currentCost": 342.50,
  "budgetLimit": 500,
  "percentageUsed": 68.5,
  "projectedDaily": 823.20,
  "message": "Current pace exceeds daily budget projection"
}
```

---

## 🔄 Data Flow Diagrams

### Phase 2 Data Flow (Agent Fleet)
```
User Input
    │
    └──→ Orchestrator
         │
         ├─→ Build Agents (orchestrate 11 agents)
         │   │
         │   ├─→ Storage Agent (disk metrics)
         │   │   └──→ Monado Blade (pattern learning)
         │   │       └──→ Optimization Agent (tuning)
         │   │
         │   ├─→ Security Agent (policy data)
         │   │   └──→ Security Setup (configuration)
         │   │
         │   └─→ Software Agent (tool installation)
         │       └──→ Software Stack (execution)
         │
         └──→ GUI Framework (display live updates)
```

### Phase 3 Data Flow (AI Services)
```
Build Agents (from Phase 2)
    │
    ├─→ AI Hub (query distribution)
    │   │
    │   ├─→ GPT-4 (OpenAI)
    │   ├─→ Claude-3 (Anthropic)
    │   ├─→ Gemini (Google)
    │   └─→ Local (Ollama)
    │
    ├─→ Monado Blade (receive metrics)
    │   └──→ Optimization insights
    │
    └──→ GUI Framework (display AI metrics)
```

### Ongoing Data Flow (All Phases)
```
All Components
    │
    ├─→ Security Setup (protective layer)
    │
    ├─→ Monado Blade (learning)
    │
    ├─→ GUI Framework (monitoring)
    │
    └─→ Build Agents (orchestration)
```

---

## 🤝 Component Handshakes

### Component Startup Sequence

```
1. Dev AI Hub starts
   └─ Initialize infrastructure
   └─ Signal ready

2. Build Agents request templates
   ├─ GET /templates from Dev AI Hub
   └─ Receive policy/config templates

3. Build Agents start
   └─ Initialize task queue
   └─ Signal ready

4. Monado Blade request baseline
   ├─ Subscribe to Storage Agent events
   └─ Begin learning

5. AI Hub connects
   ├─ Initialize service connectors
   └─ Signal ready

6. Security Setup protects all
   ├─ Apply policies to running components
   └─ Enable audit logging

7. GUI Framework connects
   ├─ Subscribe to all component streams
   └─ Display live data
```

### Health Check Exchange

```http
All components → Orchestrator (every 30 seconds)

POST /health
Response: 200 OK
{
  "status": "healthy",
  "uptime": "2h 15m",
  "memory": 2048,
  "cpu": 45.2,
  "lastError": null,
  "version": "1.0.0"
}
```

---

## 🔐 Authentication & Authorization

### API Key Authentication

```http
POST /api/v1/query
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: application/json

{
  "prompt": "System analysis",
  "model": "gpt-4"
}
```

### Service-to-Service Authentication

```
Mutual TLS (mTLS)
├─ Client certificate: /etc/ssl/certs/client.crt
├─ Client key: /etc/ssl/private/client.key
└─ CA certificate: /etc/ssl/certs/ca-bundle.crt
```

### Vault Integration

```
Secret retrieval for inter-service calls:
1. Request token from Vault
2. Include token in Authorization header
3. Vault validates token
4. Grant access to requested secret
5. Token expires after configured TTL
```

---

## 📊 Rate Limiting & Throttling

### Rate Limits Per Component

| Component | Endpoint | Limit | Window |
|-----------|----------|-------|--------|
| Build Agents | /queue/status | 1000 req/sec | 1 min |
| Monado Blade | /patterns/analyze | 100 req/sec | 1 min |
| AI Hub | /query | 50 req/sec | 1 min |
| GUI Framework | /dashboard/* | 10000 req/sec | 1 sec |
| Security Setup | /vault/* | 100 req/sec | 1 min |

### Backpressure Handling

```
Client → Component at rate limit
└─ HTTP 429 (Too Many Requests)
   └─ Response header: Retry-After: 5
   └─ Client backs off 5 seconds
   └─ Retry request
```

---

## ✅ Integration Testing

### Test Connectivity Between Components

```bash
#!/bin/bash

# Test orchestrator
curl -H "Authorization: Bearer $TOKEN" https://localhost:5000/health

# Test Build Agents
curl -H "Authorization: Bearer $TOKEN" https://localhost:5001/agents

# Test Monado
curl -H "Authorization: Bearer $TOKEN" https://localhost:5002/patterns

# Test Security
curl -H "Authorization: Bearer $TOKEN" https://localhost:5003/security/verify

# Test AI Hub
curl -H "Authorization: Bearer $TOKEN" https://localhost:5004/models/list

# Test GUI
curl -H "Authorization: Bearer $TOKEN" https://localhost:5005/dashboard/overview
```

### Verify Data Flow

```bash
# Monitor events in real-time
wscat -c wss://localhost:8443/events

# Subscribe to all events
# {"action": "subscribe", "channels": ["*"]}

# Watch for event stream
# Should see events from all components
```

---

## 🔗 Related Documentation

- **COMPONENT_INTEGRATION_GUIDE.md** - Component details
- **COMPONENT_DEPLOYMENT_GUIDE.md** - Deployment procedures
- **COMPONENT_USAGE_MATRIX.md** - Usage in phases

---

**Last Updated:** 2024  
**Status:** ✅ Complete  
**API Version:** v1  
**Protocols Supported:** 6  
**Endpoints Documented:** 40+
