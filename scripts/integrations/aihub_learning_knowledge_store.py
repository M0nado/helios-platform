#!/usr/bin/env python3
from __future__ import annotations
import json, hashlib
from datetime import datetime, timezone
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
CONFIG=ROOT/'config/aihub-learning-knowledge-store.example.json'
OUT=ROOT/'reports/learning/aihub-learning-knowledge-store.json'
MD=ROOT/'reports/learning/aihub-learning-knowledge-store.md'
SQL=ROOT/'reports/learning/aihub-learning-knowledge-store.sql'
VECTOR=ROOT/'reports/learning/aihub-learning-vector-manifest.json'

def load(path, default):
    try: return json.loads(path.read_text()) if path.exists() else default
    except Exception: return default

def stable_id(*parts):
    return hashlib.sha256('|'.join(str(p) for p in parts).encode()).hexdigest()[:16]

CANONICAL_FIELDS=[
    {'name':'id','type':'string','purpose':'Stable hash for dedupe across SQL/vector/document/graph stores.'},
    {'name':'tenantId','type':'string','purpose':'Future enterprise partition key.'},
    {'name':'repoId','type':'string','purpose':'Repository or workspace identifier.'},
    {'name':'knowledgeType','type':'enum','purpose':'code, branch, agent, model, test, doc, security, cloud, operator.'},
    {'name':'subject','type':'string','purpose':'Path, branch, agent id, model id, or cloud resource.'},
    {'name':'summary','type':'text','purpose':'Human-readable learned fact.'},
    {'name':'evidence','type':'json','purpose':'Commands, reports, commits, metrics, and source artifacts.'},
    {'name':'embeddingText','type':'text','purpose':'Normalized text used for vector embeddings.'},
    {'name':'embeddingRef','type':'string','purpose':'Pointer to vector row/index entry, never embedded inline in SQL by default.'},
    {'name':'confidence','type':'real','purpose':'0-1 score from F#/AIHub ranking.'},
    {'name':'freshnessUtc','type':'datetime','purpose':'When the learning should be considered current.'},
    {'name':'securityLabel','type':'enum','purpose':'public, internal, secret-redacted, restricted.'},
    {'name':'lineage','type':'json','purpose':'derived_from/supersedes/validated_by links.'},
]

PIPELINE=[
    {'step':'capture','owner':'Python AIHub glue','action':'Read reports, commits, tests, branch packets, GUI events, agent notes, and operator decisions into canonical JSONL.'},
    {'step':'redact','owner':'C# security frame','action':'Remove secret fields, hash sensitive identifiers, attach access policy labels.'},
    {'step':'score','owner':'F# analytics','action':'Assign confidence, novelty, reuse, decay, and promotion scores.'},
    {'step':'project-sql','owner':'C# data service','action':'Upsert normalized tables for joins, filters, audit, and dashboards.'},
    {'step':'project-vector','owner':'Python/provider bridge','action':'Create embedding text chunks and write vector manifest for pgvector/Azure AI Search/Qdrant/sqlite-vec.'},
    {'step':'project-graph','owner':'C++/F# assist','action':'Store edges for dependency, ownership, contradiction, supersession, and route decisions.'},
    {'step':'retrieve','owner':'AIHub agents','action':'Hybrid retrieve by SQL filters + vector similarity + graph neighborhood + freshness/confidence.'},
    {'step':'feedback','owner':'Hermes/XCore fleet','action':'Record if retrieval helped or failed so future rankings improve.'},
]

SQL_DDL='''-- AIHub learning knowledge store (portable SQL shape; adapt vector column/index per provider)
CREATE TABLE IF NOT EXISTS aihub_knowledge_items (
    id TEXT PRIMARY KEY,
    tenant_id TEXT NOT NULL,
    repo_id TEXT NOT NULL,
    knowledge_type TEXT NOT NULL,
    subject TEXT NOT NULL,
    summary TEXT NOT NULL,
    embedding_text TEXT NOT NULL,
    embedding_ref TEXT,
    confidence REAL NOT NULL DEFAULT 0.5,
    freshness_utc TEXT NOT NULL,
    security_label TEXT NOT NULL DEFAULT 'internal',
    evidence_json TEXT NOT NULL DEFAULT '{}',
    lineage_json TEXT NOT NULL DEFAULT '{}',
    created_utc TEXT NOT NULL,
    updated_utc TEXT NOT NULL
);
CREATE INDEX IF NOT EXISTS ix_aihub_knowledge_lookup ON aihub_knowledge_items(repo_id, knowledge_type, subject);
CREATE INDEX IF NOT EXISTS ix_aihub_knowledge_rank ON aihub_knowledge_items(repo_id, confidence DESC, freshness_utc DESC);

CREATE TABLE IF NOT EXISTS aihub_knowledge_edges (
    id TEXT PRIMARY KEY,
    from_item_id TEXT NOT NULL,
    to_item_id TEXT NOT NULL,
    edge_type TEXT NOT NULL,
    weight REAL NOT NULL DEFAULT 1.0,
    evidence_json TEXT NOT NULL DEFAULT '{}',
    created_utc TEXT NOT NULL
);
CREATE INDEX IF NOT EXISTS ix_aihub_knowledge_edges_from ON aihub_knowledge_edges(from_item_id, edge_type);
CREATE INDEX IF NOT EXISTS ix_aihub_knowledge_edges_to ON aihub_knowledge_edges(to_item_id, edge_type);

CREATE TABLE IF NOT EXISTS aihub_agent_memory (
    id TEXT PRIMARY KEY,
    agent_id TEXT NOT NULL,
    knowledge_item_id TEXT NOT NULL,
    role TEXT NOT NULL,
    xp_delta REAL NOT NULL DEFAULT 0,
    feedback TEXT,
    created_utc TEXT NOT NULL
);
CREATE INDEX IF NOT EXISTS ix_aihub_agent_memory_agent ON aihub_agent_memory(agent_id, created_utc DESC);

CREATE TABLE IF NOT EXISTS aihub_retrieval_feedback (
    id TEXT PRIMARY KEY,
    query_hash TEXT NOT NULL,
    knowledge_item_id TEXT NOT NULL,
    helpful INTEGER NOT NULL,
    reason TEXT,
    created_utc TEXT NOT NULL
);
'''

VECTOR_PROVIDERS=[
    {'provider':'pgvector','sql':'ALTER TABLE aihub_knowledge_items ADD COLUMN IF NOT EXISTS embedding vector(1536); CREATE INDEX ON aihub_knowledge_items USING ivfflat (embedding vector_cosine_ops);','bestFor':'Postgres joins + vector similarity in one transactional store.'},
    {'provider':'azure-ai-search-vector','index':'knowledge_semantic','bestFor':'Cloud hybrid keyword/vector search with filters for repo, type, security label, and freshness.'},
    {'provider':'qdrant','collection':'aihub_knowledge','bestFor':'Local/cloud vector service for high-throughput agent retrieval.'},
    {'provider':'sqlite-vec','table':'aihub_knowledge_vec','bestFor':'Local/offline development without a server.'},
]

RETRIEVAL_PLAN=[
    'Filter SQL by repoId, knowledgeType, securityLabel, freshnessUtc, and confidence threshold.',
    'Run vector similarity over embeddingText chunks for semantic matches.',
    'Expand graph one to two hops through validates, supersedes, routes_to, and owned_by_agent edges.',
    'Rerank with F# score fusion: similarity, confidence, freshness, agent XP, test validation, and cost.',
    'Return provenance with each answer so agents can cite reports, commits, commands, and schemas.',
]

def main():
    cfg=load(CONFIG,{})
    generated=datetime.now(timezone.utc).isoformat()
    sample=[]
    for kind,subject,summary in [
        ('code','scripts/agents/agent_fleet_autopilot.py','Fleet autopilot owns report-only agent selection and smart party planning.'),
        ('dashboard','scripts/dashboard/generate-gui.py','Dashboard renders SQL/vector/graph learning store controls and report links.'),
        ('agent','hermes-xcore-fleet','Hermes and XCore agents learn through canonical facts, vector recall, SQL filters, and feedback rows.'),
        ('cloud','azure-cosmos-vector-sql','Cloud projection supports Cosmos/document, Azure SQL/Postgres, Azure AI Search vector, and blob artifacts.'),
    ]:
        sample.append({'id':stable_id(kind,subject,summary),'tenantId':'local','repoId':'helios-platform','knowledgeType':kind,'subject':subject,'summary':summary,'embeddingText':f'{kind} {subject} {summary}','embeddingRef':f'vector://knowledge_semantic/{stable_id(kind,subject)}','confidence':0.82,'freshnessUtc':generated,'securityLabel':'internal','evidence':{'source':'generated knowledge-store plan'},'lineage':{'derived_from':['reports/*','config/aihub-learning-knowledge-store.example.json']}})
    payload={'generatedUtc':generated,'config':cfg,'canonicalFields':CANONICAL_FIELDS,'pipeline':PIPELINE,'sqlDdlPath':str(SQL.relative_to(ROOT)),'vectorManifestPath':str(VECTOR.relative_to(ROOT)),'vectorProviders':VECTOR_PROVIDERS,'retrievalPlan':RETRIEVAL_PLAN,'sampleKnowledgeItems':sample,'safeCommands':['python3 scripts/integrations/aihub_learning_knowledge_store.py','python3 scripts/dashboard/generate-gui.py','python3 scripts/security/secret_preflight.py'],'liveApplyNotes':['Report-only by default; no database is created by this script.','Use Key Vault/environment secrets for connection strings.','Run SQL/vector provisioning only after cloud-live or repo-live review.']}
    OUT.parent.mkdir(parents=True,exist_ok=True)
    OUT.write_text(json.dumps(payload,indent=2)+'\n')
    SQL.write_text(SQL_DDL)
    VECTOR.write_text(json.dumps({'generatedUtc':generated,'dimensions':cfg.get('stores',{}).get('vector',{}).get('embeddingDimensions',1536),'providers':VECTOR_PROVIDERS,'indexes':cfg.get('stores',{}).get('vector',{}).get('indexes',[])},indent=2)+'\n')
    md=['# AIHub Learning Knowledge Store','',f'Generated: `{generated}`','',cfg.get('principle','Canonical learning store with SQL/vector/document/graph projections.'),'','## Pipeline']
    md += [f"- **{p['step']}** ({p['owner']}): {p['action']}" for p in PIPELINE]
    md += ['','## Canonical fields']+[f"- `{f['name']}` ({f['type']}): {f['purpose']}" for f in CANONICAL_FIELDS]
    md += ['','## Vector providers']+[f"- **{v['provider']}**: {v['bestFor']}" for v in VECTOR_PROVIDERS]
    md += ['','## Retrieval plan']+[f'- {x}' for x in RETRIEVAL_PLAN]
    md += ['','## Safe commands']+[f'- `{c}`' for c in payload['safeCommands']]
    MD.write_text('\n'.join(md)+'\n')
    print(f'Wrote {OUT.relative_to(ROOT)}'); print(f'Wrote {MD.relative_to(ROOT)}'); print(f'Wrote {SQL.relative_to(ROOT)}'); print(f'Wrote {VECTOR.relative_to(ROOT)}')
if __name__=='__main__': main()
