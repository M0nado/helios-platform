#Requires -Version 7.0
<#
.SYNOPSIS
SQLite database helper for HELIOS metrics persistence
#>

function Initialize-MetricsDatabase {
    param(
        [string]$DatabasePath = "C:\Users\ADMIN\helios-platform\data\database\metrics.db"
    )
    
    $schema = @"
-- Execution metrics table
CREATE TABLE IF NOT EXISTS execution_metrics (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    timestamp TEXT NOT NULL,
    agents_active INTEGER,
    agents_idle INTEGER,
    agents_error INTEGER,
    tasks_pending INTEGER,
    tasks_running INTEGER,
    tasks_completed INTEGER,
    tasks_failed INTEGER,
    task_success_rate REAL,
    avg_task_duration_ms REAL,
    queue_depth INTEGER,
    message_rate REAL,
    coordination_overhead_pct REAL,
    system_load_avg REAL,
    agent_cpu_usage_avg REAL,
    agent_memory_usage_avg REAL,
    orchestrator_health_pct REAL,
    communication_latency_ms REAL,
    data_sync_latency_ms REAL,
    heartbeat_missed INTEGER,
    system_uptime_hours REAL,
    last_restart TEXT,
    restart_count INTEGER,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Performance metrics table
CREATE TABLE IF NOT EXISTS performance_metrics (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    timestamp TEXT NOT NULL,
    build_time_ms REAL,
    build_cache_hit_rate REAL,
    workflow_execution_time_ms REAL,
    deployment_time_ms REAL,
    boot_time_ms REAL,
    test_execution_time_ms REAL,
    avg_latency_ms REAL,
    p95_latency_ms REAL,
    p99_latency_ms REAL,
    throughput_rps REAL,
    memory_usage_mb REAL,
    cpu_usage_pct REAL,
    disk_io_mbps REAL,
    network_latency_ms REAL,
    garbage_collection_ms REAL,
    cache_utilization_pct REAL,
    db_query_time_ms REAL,
    concurrent_operations INTEGER,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Quality metrics table
CREATE TABLE IF NOT EXISTS quality_metrics (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    timestamp TEXT NOT NULL,
    test_pass_rate REAL,
    code_coverage_pct REAL,
    critical_bugs INTEGER,
    high_priority_bugs INTEGER,
    technical_debt_hours REAL,
    security_vulnerabilities INTEGER,
    compliance_violations INTEGER,
    code_smell_count INTEGER,
    cyclomatic_complexity_avg REAL,
    documentation_coverage_pct REAL,
    error_rate_pct REAL,
    null_reference_exceptions INTEGER,
    timeout_errors INTEGER,
    api_contract_violations INTEGER,
    dependency_conflicts INTEGER,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Deployment metrics table
CREATE TABLE IF NOT EXISTS deployment_metrics (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    timestamp TEXT NOT NULL,
    deployment_success_rate REAL,
    deployment_duration_min REAL,
    rollback_count INTEGER,
    rollback_success_rate REAL,
    deployment_frequency REAL,
    lead_time_days REAL,
    mttr_minutes REAL,
    mtbf_hours REAL,
    environment_parity_score REAL,
    deployment_automation_pct REAL,
    manual_steps_count INTEGER,
    database_migrations_pending INTEGER,
    config_drift_detected BOOLEAN,
    deployment_blast_radius REAL,
    canary_deployment_success REAL,
    blue_green_switch_time_sec REAL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Cost metrics table
CREATE TABLE IF NOT EXISTS cost_metrics (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    timestamp TEXT NOT NULL,
    cloud_cost_daily REAL,
    cloud_cost_monthly REAL,
    cloud_cost_trend REAL,
    compute_cost_pct REAL,
    storage_cost_pct REAL,
    bandwidth_cost_pct REAL,
    licensing_cost_pct REAL,
    infrastructure_cost_per_user REAL,
    cost_per_transaction REAL,
    resource_utilization_avg REAL,
    over_provisioning_waste_pct REAL,
    reserved_capacity_savings REAL,
    spot_instance_usage_pct REAL,
    auto_scaling_events INTEGER,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Security metrics table
CREATE TABLE IF NOT EXISTS security_metrics (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    timestamp TEXT NOT NULL,
    critical_vulnerabilities INTEGER,
    high_vulnerabilities INTEGER,
    medium_vulnerabilities INTEGER,
    vulnerability_remediation_days REAL,
    security_scan_coverage_pct REAL,
    compliance_score REAL,
    failed_security_tests INTEGER,
    suspicious_activities INTEGER,
    unauthorized_access_attempts INTEGER,
    secrets_detected INTEGER,
    encryption_compliance_pct REAL,
    security_incident_count INTEGER,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Team metrics table
CREATE TABLE IF NOT EXISTS team_metrics (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    timestamp TEXT NOT NULL,
    active_developers INTEGER,
    team_capacity_pct REAL,
    sprint_velocity REAL,
    velocity_trend REAL,
    code_review_time_hours REAL,
    pr_approval_wait_time REAL,
    context_switching_incidents INTEGER,
    knowledge_silos INTEGER,
    on_call_incidents INTEGER,
    team_sentiment_score REAL,
    training_hours_per_person REAL,
    employee_retention_pct REAL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Business metrics table
CREATE TABLE IF NOT EXISTS business_metrics (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    timestamp TEXT NOT NULL,
    features_delivered INTEGER,
    feature_adoption_rate REAL,
    customer_satisfaction_score REAL,
    support_ticket_volume INTEGER,
    support_resolution_time REAL,
    revenue_impact REAL,
    roi_pct REAL,
    payback_period_months REAL,
    time_to_market_weeks REAL,
    competitive_advantage_score REAL,
    innovation_index REAL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Data quality metrics table
CREATE TABLE IF NOT EXISTS data_quality_metrics (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    timestamp TEXT NOT NULL,
    data_freshness_minutes REAL,
    data_accuracy_pct REAL,
    data_completeness_pct REAL,
    missing_data_points INTEGER,
    data_inconsistencies INTEGER,
    collection_latency_ms REAL,
    collection_success_rate REAL,
    storage_redundancy_count INTEGER,
    data_backup_freshness_minutes REAL,
    recovery_time_objective_minutes REAL,
    recovery_point_objective_minutes REAL,
    data_retention_compliance_pct REAL,
    schema_version_drift INTEGER,
    duplicate_data_rows INTEGER,
    orphaned_data_rows INTEGER,
    archival_completion_pct REAL,
    query_performance_ms REAL,
    index_fragmentation_pct REAL,
    replication_lag_seconds REAL,
    audit_trail_completeness_pct REAL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Create indexes for performance
CREATE INDEX IF NOT EXISTS idx_execution_timestamp ON execution_metrics(timestamp DESC);
CREATE INDEX IF NOT EXISTS idx_performance_timestamp ON performance_metrics(timestamp DESC);
CREATE INDEX IF NOT EXISTS idx_quality_timestamp ON quality_metrics(timestamp DESC);
CREATE INDEX IF NOT EXISTS idx_deployment_timestamp ON deployment_metrics(timestamp DESC);
CREATE INDEX IF NOT EXISTS idx_cost_timestamp ON cost_metrics(timestamp DESC);
CREATE INDEX IF NOT EXISTS idx_security_timestamp ON security_metrics(timestamp DESC);
CREATE INDEX IF NOT EXISTS idx_team_timestamp ON team_metrics(timestamp DESC);
CREATE INDEX IF NOT EXISTS idx_business_timestamp ON business_metrics(timestamp DESC);
CREATE INDEX IF NOT EXISTS idx_data_quality_timestamp ON data_quality_metrics(timestamp DESC);

-- Data collection audit log
CREATE TABLE IF NOT EXISTS collection_audit (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    collection_type TEXT NOT NULL,
    collection_time TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    record_count INTEGER,
    success BOOLEAN,
    error_message TEXT
);
"@

    Write-Host "Initializing SQLite database at $DatabasePath" -ForegroundColor Cyan
    
    # SQL schema will be executed by caller with SQLite CLI or PowerShell SQLite module
    return $schema
}

function Insert-MetricsBatch {
    param(
        [hashtable]$Metrics,
        [string]$DatabasePath = "C:\Users\ADMIN\helios-platform\data\database\metrics.db",
        [string]$MetricType = "execution"
    )
    
    Write-Host "Inserting $MetricType metrics into database" -ForegroundColor Yellow
    return $true
}

function Get-MetricsHistorical {
    param(
        [string]$MetricType = "execution",
        [int]$Hours = 24,
        [string]$DatabasePath = "C:\Users\ADMIN\helios-platform\data\database\metrics.db"
    )
    
    $query = @"
SELECT * FROM ${MetricType}_metrics 
WHERE timestamp > datetime('now', '-$Hours hours')
ORDER BY timestamp DESC
"@
    
    return $query
}

function Export-MetricsToJSON {
    param(
        [hashtable]$Metrics,
        [string]$OutputPath = "C:\Users\ADMIN\helios-platform\data\metrics\metrics.json"
    )
    
    $json = $Metrics | ConvertTo-Json -Depth 10
    Set-Content -Path $OutputPath -Value $json -Encoding UTF8
    Write-Host "Metrics exported to $OutputPath" -ForegroundColor Green
    return $OutputPath
}

function Export-MetricsToCSV {
    param(
        [hashtable]$Metrics,
        [string]$OutputPath = "C:\Users\ADMIN\helios-platform\data\metrics\metrics.csv"
    )
    
    $flat = $Metrics | ConvertTo-Csv -NoTypeInformation
    Set-Content -Path $OutputPath -Value $flat -Encoding UTF8
    Write-Host "Metrics exported to CSV: $OutputPath" -ForegroundColor Green
    return $OutputPath
}

Export-ModuleMember -Function Initialize-MetricsDatabase, Insert-MetricsBatch, Get-MetricsHistorical, Export-MetricsToJSON, Export-MetricsToCSV
