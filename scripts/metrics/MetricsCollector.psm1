#Requires -Version 7.0
<#
.SYNOPSIS
Metrics collection module for HELIOS Platform - All 120+ variables
.DESCRIPTION
Central metrics collection for execution, performance, quality, deployment, cost, security, team, business, and data quality metrics.
#>

# Execution Metrics (22 variables)
function Get-ExecutionMetrics {
    param([string]$Database = "metrics.db")
    @{
        timestamp = (Get-Date -Format 'o')
        metrics = @{
            agents_active = 0
            agents_idle = 0
            agents_error = 0
            tasks_pending = 0
            tasks_running = 0
            tasks_completed = 0
            tasks_failed = 0
            task_success_rate = 0
            avg_task_duration_ms = 0
            queue_depth = 0
            message_rate = 0
            coordination_overhead_pct = 0
            system_load_avg = 0
            agent_cpu_usage_avg = 0
            agent_memory_usage_avg = 0
            orchestrator_health_pct = 0
            communication_latency_ms = 0
            data_sync_latency_ms = 0
            heartbeat_missed = 0
            system_uptime_hours = 0
            last_restart = (Get-Date -Format 'o')
            restart_count = 0
        }
    }
}

# Performance Metrics (18 variables)
function Get-PerformanceMetrics {
    param([string]$Database = "metrics.db")
    @{
        timestamp = (Get-Date -Format 'o')
        metrics = @{
            build_time_ms = 0
            build_cache_hit_rate = 0
            workflow_execution_time_ms = 0
            deployment_time_ms = 0
            boot_time_ms = 0
            test_execution_time_ms = 0
            avg_latency_ms = 0
            p95_latency_ms = 0
            p99_latency_ms = 0
            throughput_rps = 0
            memory_usage_mb = 0
            cpu_usage_pct = 0
            disk_io_mbps = 0
            network_latency_ms = 0
            garbage_collection_ms = 0
            cache_utilization_pct = 0
            db_query_time_ms = 0
            concurrent_operations = 0
        }
    }
}

# Quality Metrics (15 variables)
function Get-QualityMetrics {
    param([string]$Database = "metrics.db")
    @{
        timestamp = (Get-Date -Format 'o')
        metrics = @{
            test_pass_rate = 0
            code_coverage_pct = 0
            critical_bugs = 0
            high_priority_bugs = 0
            technical_debt_hours = 0
            security_vulnerabilities = 0
            compliance_violations = 0
            code_smell_count = 0
            cyclomatic_complexity_avg = 0
            documentation_coverage_pct = 0
            error_rate_pct = 0
            null_reference_exceptions = 0
            timeout_errors = 0
            api_contract_violations = 0
            dependency_conflicts = 0
        }
    }
}

# Deployment Metrics (16 variables)
function Get-DeploymentMetrics {
    param([string]$Database = "metrics.db")
    @{
        timestamp = (Get-Date -Format 'o')
        metrics = @{
            deployment_success_rate = 0
            deployment_duration_min = 0
            rollback_count = 0
            rollback_success_rate = 0
            deployment_frequency = 0
            lead_time_days = 0
            mttr_minutes = 0
            mtbf_hours = 0
            environment_parity_score = 0
            deployment_automation_pct = 0
            manual_steps_count = 0
            database_migrations_pending = 0
            config_drift_detected = 0
            deployment_blast_radius = 0
            canary_deployment_success = 0
            blue_green_switch_time_sec = 0
        }
    }
}

# Cost Metrics (14 variables)
function Get-CostMetrics {
    param([string]$Database = "metrics.db")
    @{
        timestamp = (Get-Date -Format 'o')
        metrics = @{
            cloud_cost_daily = 0
            cloud_cost_monthly = 0
            cloud_cost_trend = 0
            compute_cost_pct = 0
            storage_cost_pct = 0
            bandwidth_cost_pct = 0
            licensing_cost_pct = 0
            infrastructure_cost_per_user = 0
            cost_per_transaction = 0
            resource_utilization_avg = 0
            over_provisioning_waste_pct = 0
            reserved_capacity_savings = 0
            spot_instance_usage_pct = 0
            auto_scaling_events = 0
        }
    }
}

# Security Metrics (12 variables)
function Get-SecurityMetrics {
    param([string]$Database = "metrics.db")
    @{
        timestamp = (Get-Date -Format 'o')
        metrics = @{
            critical_vulnerabilities = 0
            high_vulnerabilities = 0
            medium_vulnerabilities = 0
            vulnerability_remediation_days = 0
            security_scan_coverage_pct = 0
            compliance_score = 0
            failed_security_tests = 0
            suspicious_activities = 0
            unauthorized_access_attempts = 0
            secrets_detected = 0
            encryption_compliance_pct = 0
            security_incident_count = 0
        }
    }
}

# Team Metrics (12 variables)
function Get-TeamMetrics {
    param([string]$Database = "metrics.db")
    @{
        timestamp = (Get-Date -Format 'o')
        metrics = @{
            active_developers = 0
            team_capacity_pct = 0
            sprint_velocity = 0
            velocity_trend = 0
            code_review_time_hours = 0
            pr_approval_wait_time = 0
            context_switching_incidents = 0
            knowledge_silos = 0
            on_call_incidents = 0
            team_sentiment_score = 0
            training_hours_per_person = 0
            employee_retention_pct = 0
        }
    }
}

# Business Metrics (11 variables)
function Get-BusinessMetrics {
    param([string]$Database = "metrics.db")
    @{
        timestamp = (Get-Date -Format 'o')
        metrics = @{
            features_delivered = 0
            feature_adoption_rate = 0
            customer_satisfaction_score = 0
            support_ticket_volume = 0
            support_resolution_time = 0
            revenue_impact = 0
            roi_pct = 0
            payback_period_months = 0
            time_to_market_weeks = 0
            competitive_advantage_score = 0
            innovation_index = 0
        }
    }
}

# Data Quality Metrics (20 variables)
function Get-DataQualityMetrics {
    param([string]$Database = "metrics.db")
    @{
        timestamp = (Get-Date -Format 'o')
        metrics = @{
            data_freshness_minutes = 0
            data_accuracy_pct = 0
            data_completeness_pct = 0
            missing_data_points = 0
            data_inconsistencies = 0
            collection_latency_ms = 0
            collection_success_rate = 0
            storage_redundancy_count = 0
            data_backup_freshness_minutes = 0
            recovery_time_objective_minutes = 0
            recovery_point_objective_minutes = 0
            data_retention_compliance_pct = 0
            schema_version_drift = 0
            duplicate_data_rows = 0
            orphaned_data_rows = 0
            archival_completion_pct = 0
            query_performance_ms = 0
            index_fragmentation_pct = 0
            replication_lag_seconds = 0
            audit_trail_completeness_pct = 0
        }
    }
}

# Aggregate all metrics
function Get-AllMetrics {
    param([string]$Database = "metrics.db", [string]$OutputFormat = "json")
    $allMetrics = @{
        timestamp = (Get-Date -Format 'o')
        execution = (Get-ExecutionMetrics -Database $Database).metrics
        performance = (Get-PerformanceMetrics -Database $Database).metrics
        quality = (Get-QualityMetrics -Database $Database).metrics
        deployment = (Get-DeploymentMetrics -Database $Database).metrics
        cost = (Get-CostMetrics -Database $Database).metrics
        security = (Get-SecurityMetrics -Database $Database).metrics
        team = (Get-TeamMetrics -Database $Database).metrics
        business = (Get-BusinessMetrics -Database $Database).metrics
        data_quality = (Get-DataQualityMetrics -Database $Database).metrics
    }
    if ($OutputFormat -eq "json") { return ($allMetrics | ConvertTo-Json -Depth 10) } 
    else { return $allMetrics }
}

Export-ModuleMember -Function Get-ExecutionMetrics, Get-PerformanceMetrics, Get-QualityMetrics, Get-DeploymentMetrics, Get-CostMetrics, Get-SecurityMetrics, Get-TeamMetrics, Get-BusinessMetrics, Get-DataQualityMetrics, Get-AllMetrics
