# Vendor Comparison Matrix: Cloud Providers vs. On-Premise

**Report Date:** January 2025  
**Scope:** Comprehensive comparison of AWS, Azure, GCP, and on-premise  
**Evaluation Criteria:** Cost, performance, features, support, scalability  

---

## Executive Summary: Vendor Scorecard

### Overall Winner by Use Case

| Use Case | Winner | Score | Rationale |
|---|---|---|---|
| **Cost (any fleet)** | GCP | 9.5/10 | 35% cheaper compute, aggressive pricing |
| **Startup (<20 agents)** | GCP + Azure | 9/10 | Both cheap; GCP cheaper, Azure more integrated |
| **Enterprise (100+ agents)** | AWS | 8/10 | Better support, more services, compliance options |
| **Compliance (GDPR/HIPAA)** | Azure | 8.5/10 | Built-in compliance, simpler governance |
| **Multi-Cloud Strategy** | AWS + Azure | 8/10 | Broadest ecosystem, avoid single vendor lock-in |
| **Serverless Workloads** | GCP Functions | 9.5/10 | 20-30x cheaper than AWS Lambda |
| **On-Premise + Hybrid** | None (need on-prem partner) | - | Implement yourself using open standards |

---

## Detailed Vendor Comparison

### 1. PRICING COMPARISON

#### Fleet of 24 Agents - Monthly Cost Breakdown

```
                AWS         Azure       GCP         On-Premise
────────────────────────────────────────────────────────────────────────
Compute       $ 730        $ 400      $ 295        $  325 (amortized)
Database      $1,058       $1,542      $900        $ 1,500
Storage       $   40       $ 368       $  25        $ 1,200 (SSD cost)
Network       $   65       $   65      $   65       $   500 (cooling+power)
Monitoring    $ 120        $ 150      $ 120        $ 1,200 (staff)
Support       $ 194        $ 272      $   0        $   300 (contracts)
────────────────────────────────────────────────────────────────────────
TOTAL        $2,082       $2,762     $1,468        $11,795
────────────────────────────────────────────────────────────────────────
Per Agent    $ 86.78      $ 115.09    $ 61.16       $ 491.45
────────────────────────────────────────────────────────────────────────
Savings vs GCP            88% more    Baseline      705% more
────────────────────────────────────────────────────────────────────────
```

**Key Finding:** GCP is 31% cheaper than AWS, 47% cheaper than Azure for this workload.

---

### 2. COST COMPONENTS DEEP DIVE

#### A. Compute Pricing (per GB-second of execution)

| Provider | Price/GB-sec | Included Free | Calculation |
|---|---|---|---|
| **AWS Lambda** | $0.0000166667 | 400K GB-sec | (240 GB-sec * $0.0000166667) = $4/month |
| **Azure Functions** | $0.000011 | 400K GB-sec | 30% cheaper than AWS |
| **GCP Functions** | $0.0000041 | 400K GB-sec | 75% cheaper than AWS |
| **AWS EC2 (t3.large)** | Hourly rate | — | $0.0832/hr = $60/month |
| **Azure VMs** | Hourly rate | — | $0.456/hr = $41/month |
| **GCP Compute** | Per-minute billing | — | $0.0336/hr = $24/month |

**Winner:** GCP for compute (per-minute vs. hourly billing).

---

#### B. Database Pricing (for 24-agent fleet, 100GB total data)

| Provider | Instance Type | Hourly Rate | Monthly Cost | Notes |
|---|---|---|---|---|
| **AWS RDS Aurora** | db.t3.2xlarge | $1.453 | $1,058 | Multi-AZ required for HA |
| **Azure PostgreSQL** | Standard D16s v3 | $1.441 | $1,052 | Zone-redundant option |
| **GCP Cloud SQL** | db-highmem-16 | $0.899 | $657 | Regional HA built-in |
| **AWS DynamoDB** | On-demand | $1.25/M writes | $300-1000 | NoSQL alternative; cheaper if <1M writes |
| **Azure Cosmos DB** | Provisioned 1200 RU | $0.012/RU/hr | $394 | **Best price for variable workload** |
| **GCP Firestore** | On-demand | $0.06/100K reads | $50-200 | Serverless alternative |

**Winner:** GCP for relational, Azure Cosmos DB for NoSQL variable workloads.

---

#### C. Storage Pricing (per GB per month)

| Storage Type | AWS | Azure | GCP | On-Prem |
|---|---|---|---|---|
| **Hot/Standard Tier** | $0.023 | $0.021 | $0.02 | N/A |
| **Warm Tier** | $0.0125 | $0.01 | $0.016 | N/A |
| **Archive/Cold** | $0.004 | $0.002 | $0.004 | Tape backup |
| **Snapshots** | $0.05 | $0.035 | $0.03 | Disk storage |

**Winner:** Azure for archive tier (50% cheaper cold storage).

---

#### D. Data Transfer Pricing

| Scenario | AWS | Azure | GCP | Winner |
|---|---|---|---|---|
| **Outbound to Internet** | $0.09/GB | $0.09/GB | $0.09/GB | Tied |
| **Outbound to On-Prem** | $0.09/GB | $0.09/GB | $0.09/GB | Tied |
| **Cross-Region Replication** | $0.02/GB | $0.02/GB | $0.01/GB | **GCP 50% cheaper** |
| **Inbound (all free)** | Free | Free | Free | Tied |
| **CDN Egress** | $0.085/GB | $0.087/GB | $0.065/GB | **GCP 23% cheaper** |

**Winner:** GCP for data transfer, especially cross-region.

---

### 3. FEATURE COMPARISON

#### A. Auto-Scaling Capabilities

| Feature | AWS | Azure | GCP | On-Prem |
|---|---|---|---|---|
| **Auto-scale by CPU** | ✅ Native | ✅ Native | ✅ Native | ⚠️ Limited |
| **Auto-scale by Custom Metrics** | ✅ CloudWatch | ✅ Monitor | ✅ Stackdriver | ⚠️ Manual |
| **Predictive Scaling** | ✅ (new) | ⚠️ (beta) | ⚠️ (beta) | ❌ None |
| **Multi-zone** | ✅ Full | ✅ Full | ✅ Full | ❌ Requires HA kit |
| **Container Orchestration** | ✅ ECS/EKS | ✅ AKS | ✅ GKE | ⚠️ Manual Kubernetes |
| **Serverless Scaling** | ✅ (expensive) | ✅ (moderate) | ✅ (cheap) | ❌ N/A |

**Winner:** AWS for feature breadth, GCP for cost-efficiency.

---

#### B. Reliability & SLA

| Metric | AWS | Azure | GCP | On-Prem |
|---|---|---|---|---|
| **Compute SLA** | 99.95% | 99.95% | 99.95% | Manual |
| **Database SLA** | 99.95% | 99.95% | 99.95% | Manual |
| **Multi-region HA** | ✅ Easy | ✅ Easy | ✅ Easy | ⚠️ Complex |
| **DR SLA (RTO/RPO)** | 1hr/15min | 1hr/15min | 1hr/15min | Manual |
| **Compliance Certs** | 100+ | 100+ | 95+ | Depends |
| **Incident Response** | 24/7 | 24/7 | 24/7 | Staff on call |

**Winner:** AWS for breadth of compliance, all equivalent on SLA.

---

#### C. Monitoring & Observability

| Capability | AWS | Azure | GCP | On-Prem |
|---|---|---|---|---|
| **Metrics** | CloudWatch | Monitor | Stackdriver | Prometheus |
| **Logs** | CloudWatch Logs | Log Analytics | Logging | ELK/Splunk |
| **Tracing** | X-Ray | App Insights | Cloud Trace | Jaeger |
| **Cost per metric** | $0.30 | $0.013 | $0.015 | Self-hosted |
| **APM Included** | ❌ Extra | ✅ App Insights | ❌ Extra | ❌ Extra |
| **ML Anomaly Detection** | ⚠️ (new) | ✅ | ✅ | ❌ None |

**Winner:** Azure for integrated monitoring, GCP for cost.

---

### 4. SUPPORT COMPARISON

#### A. Support Pricing & Response Time

| Tier | AWS | Azure | GCP | On-Prem |
|---|---|---|---|---|
| **Basic (Free)** | Limited | Limited | Limited | N/A |
| **Developer** | $29/mo | N/A | N/A | N/A |
| **Business** | 7% of bill | $500/mo | Negotiated | Support contract |
| **Enterprise** | 15% of bill | Negotiated | Negotiated | $100-500K/yr |
| **Critical Response** | 15 min | 15 min | 30 min | Depends |
| **Dedicated TAM** | $15K+/yr | Included | Negotiated | Included |

**Winner:** Azure includes dedicated support; AWS most expensive.

---

#### B. Documentation & Community

| Aspect | AWS | Azure | GCP | On-Prem |
|---|---|---|---|---|
| **Official Documentation** | Excellent | Excellent | Excellent | Depends |
| **Community Size** | Largest | Large | Medium | Depends |
| **Training Programs** | AWS Training | Azure Learn | Google Learn | Varies |
| **Certified Engineers** | 1M+ | 500K+ | 100K+ | Varies |
| **Third-party Integrations** | 1000+ | 500+ | 300+ | Limited |

**Winner:** AWS for community size and resources.

---

### 5. VENDOR LOCK-IN ANALYSIS

#### A. Switching Cost to Exit

| Migration Aspect | AWS | Azure | GCP | On-Prem |
|---|---|---|---|---|
| **Data Export Cost** | $3,888 | $3,888 | $1,944 | N/A (own it) |
| **Code Refactor** | 10-30% effort | 5-10% effort | <5% effort | High (always) |
| **Database Migration** | 20-30% effort | 10-15% effort | 5-10% effort | High |
| **Integration Rewrites** | 30-50% effort | 10-20% effort | 5-15% effort | Low |
| **Training Required** | 40+ hours | 30+ hours | 20+ hours | Varies |
| **Total Exit Cost Estimate** | $100K | $50K | $25K | N/A |

**Findings:**
- AWS has highest lock-in (proprietary services)
- GCP has lowest lock-in (standards-based)
- Azure middle ground
- On-premise requires re-platforming to cloud (high exit cost too)

---

#### B. Strategic Recommendations to Reduce Lock-In

1. **Use Managed Open Standards**
   - PostgreSQL (not Aurora)
   - Kubernetes (not ECS)
   - Docker containers (not proprietary runtimes)

2. **Avoid Proprietary Services**
   - ❌ AWS Aurora (use PostgreSQL on RDS)
   - ❌ AWS DynamoDB (use DynamoDB but export ability)
   - ❌ Azure Cosmos DB (nice to have, but harder exit)
   - ✅ Cloud SQL (all three providers support)

3. **Multi-Cloud Strategy**
   - 60% workload on primary (cheapest)
   - 40% workload on secondary (failover)
   - Forces portability, reduces cost risk

---

### 6. HIDDEN COSTS & SURPRISES

#### A. Egress Costs (Data Transfer Out)

**Scenario:** 24 agents, 30GB/month outbound

```
AWS:    30GB × $0.09 = $2.70/month (appears small...)
Azure:  30GB × $0.09 = $2.70/month
GCP:    30GB × $0.09 = $2.70/month

But if you add:
  - Replication (30GB):   +$2.70/month
  - Backup (30GB):        +$2.70/month
  - CDN distribution:     +$2.70/month
  
Total becomes: $10.80/month in egress
Annual: $130 (not visible in bill until you analyze)
```

**Prevention:** Architect for data locality, use CDN, enable compression.

---

#### B. Monitoring & Logging Creep

**Typical Cost Escalation:**

```
Month 1: 10 custom metrics        → $3/month
Month 6: 100 custom metrics       → $30/month (10x!)
Month 12: 500 custom metrics      → $150/month (50x!)

Root cause: Every new feature adds metrics, nobody removes old ones
```

**Prevention:** Implement metrics hygiene, quarterly cleanup, automation tests.

---

#### C. Unused Reserved Instances

**Scenario:** Buy 1-year RI, usage drops 50% mid-year

```
Committed: $6,569 (1-year prepay for 24 agents)
Actual need: 12 agents after cost cutting

Result: $3,285 spent on unused capacity
Wasted money: Sunk cost (can't refund, can only sell on RI marketplace)

Prevention: Start with on-demand, only RI proven stable 3+ months
```

---

### 7. DEPLOYMENT ARCHITECTURE RECOMMENDATIONS

#### Scenario: Startup with 8 agents, uncertain growth

```
RECOMMENDED: GCP Cloud + Azure for redundancy
  Primary:   GCP Cloud (cheapest)             $427.65/month
  Secondary: Azure Functions (warm standby)   $177/month
  Network:   VPN + sync                       $100/month
  ─────────────────────────────────────────────────
  Total:                                      $704.65/month
  
  Benefits:
    - Multi-cloud redundancy (no vendor lock-in)
    - Automatic failover if GCP outage
    - Leverage both vendors' strengths
    - Learn both platforms for team growth
```

---

#### Scenario: Mid-market with 24 stable agents

```
RECOMMENDED: GCP Cloud with hybrid DR
  Primary:   GCP Cloud (production)           $1,468/month
  Secondary: On-Premise (cold standby)          $300/month (minimal)
  Network:   Replication + monitoring           $200/month
  ─────────────────────────────────────────────────
  Total:                                      $1,968/month
  
  Benefits:
    - Optimal cost (GCP cheapest)
    - Compliance/data residency met
    - 15-min recovery to on-premise
    - 80% cheaper than pure on-premise
```

---

#### Scenario: Enterprise with 100 agents

```
RECOMMENDED: Multi-cloud with Azure primary
  Primary:   Azure (compliance friendly)      $16,315/month
  Secondary: GCP (cheaper, failover)           $8,517/month
  Tertiary:  AWS (enterprise contracts)       $13,837/month
  Network:   Global load balancing              $500/month
  ─────────────────────────────────────────────────
  Total:                                      $39,169/month
  
  Benefits:
    - Negotiate 20% discounts (volume + multi-cloud)
    - Regional presence (compliance)
    - Cross-cloud failover
    - Leverage each cloud's strengths
    
  Cost with discounts:                        $31,335/month (20% less)
```

---

## Scoring Matrix: Which Vendor for You?

```
Use Cloud If:                          Use On-Prem If:
─────────────────────────────────────────────────────────────
Fleet < 50 agents                      Fleet > 75 agents & stable
Growth > 20% YoY                       Growth < 10% YoY
No data residency requirements         Strict compliance needs
No dedicated ops staff                 Have ops team
Want rapid deployment                  Want control
Limited capital available              Have capital budget
─────────────────────────────────────────────────────────────
```

---

## Final Vendor Selection Flowchart

```
START: Choose Infrastructure Model
  │
  ├─ Need maximum cost savings?
  │  └─ YES → Use GCP (35% cheaper than AWS)
  │  └─ NO → Continue
  │
  ├─ Need strong compliance/certifications?
  │  └─ YES → Use Azure or AWS
  │  └─ NO → Continue
  │
  ├─ Need strong enterprise support?
  │  └─ YES → Use AWS or Azure
  │  └─ NO → Continue
  │
  ├─ Need minimal vendor lock-in?
  │  └─ YES → Use GCP or multi-cloud
  │  └─ NO → Use AWS for ecosystem
  │
  └─ DECISION: 
     - Startup:          GCP or Azure Functions
     - Mid-Market:       GCP with Azure/AWS backup
     - Enterprise:       Multi-cloud (AWS+Azure)
     - Compliance-Heavy: Azure or AWS
     - Cost-Obsessed:    GCP
```

---

**Document Version:** 1.0  
**Last Updated:** January 2025  
**Data Source:** Published cloud pricing (2025-01-01)  
**Valid Through:** June 2025 (pricing changes expected quarterly)
