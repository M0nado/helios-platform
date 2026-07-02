using System;
using System.Threading.Tasks;

namespace HELIOS.Platform.Database
{
    /// <summary>
    /// HELIOS Database schema migration V1
    /// Creates core HELIOS tables for pattern storage and management
    /// </summary>
    public class HELIOSMigrationV1
    {
        public static string GetUpScript()
        {
            return @"
-- HELIOS Pattern Storage Table
CREATE TABLE IF NOT EXISTS helios_patterns (
    pattern_id NVARCHAR(255) PRIMARY KEY,
    name NVARCHAR(255) NOT NULL,
    description NVARCHAR(MAX),
    category NVARCHAR(100) NOT NULL,
    confidence_score FLOAT NOT NULL,
    metadata NVARCHAR(MAX),
    discovered_at DATETIME2 DEFAULT GETUTCDATE(),
    updated_at DATETIME2 DEFAULT GETUTCDATE(),
    is_active BIT DEFAULT 1,
    INDEX idx_category (category),
    INDEX idx_confidence (confidence_score),
    INDEX idx_discovered (discovered_at)
);

-- HELIOS Pattern Feedback Table
CREATE TABLE IF NOT EXISTS helios_pattern_feedback (
    feedback_id NVARCHAR(255) PRIMARY KEY,
    pattern_id NVARCHAR(255) NOT NULL,
    was_effective BIT NOT NULL,
    effectiveness_score FLOAT,
    notes NVARCHAR(MAX),
    agent_id NVARCHAR(255),
    feedback_time DATETIME2 DEFAULT GETUTCDATE(),
    FOREIGN KEY (pattern_id) REFERENCES helios_patterns(pattern_id) ON DELETE CASCADE,
    INDEX idx_pattern (pattern_id),
    INDEX idx_time (feedback_time)
);

-- HELIOS Agent Registry Table
CREATE TABLE IF NOT EXISTS helios_agents (
    agent_id NVARCHAR(255) PRIMARY KEY,
    agent_name NVARCHAR(255) NOT NULL,
    agent_version NVARCHAR(50),
    agent_type NVARCHAR(100),
    registered_at DATETIME2 DEFAULT GETUTCDATE(),
    last_heartbeat DATETIME2,
    is_active BIT DEFAULT 1,
    metadata NVARCHAR(MAX),
    INDEX idx_type (agent_type),
    INDEX idx_active (is_active)
);

-- HELIOS Learning Records Table
CREATE TABLE IF NOT EXISTS helios_learning_records (
    record_id NVARCHAR(255) PRIMARY KEY,
    pattern_id NVARCHAR(255) NOT NULL,
    agent_id NVARCHAR(255),
    learning_context NVARCHAR(MAX),
    effectiveness_improvement FLOAT,
    recorded_at DATETIME2 DEFAULT GETUTCDATE(),
    FOREIGN KEY (pattern_id) REFERENCES helios_patterns(pattern_id) ON DELETE CASCADE,
    FOREIGN KEY (agent_id) REFERENCES helios_agents(agent_id),
    INDEX idx_pattern_learning (pattern_id),
    INDEX idx_agent_learning (agent_id)
);

-- HELIOS System Metrics Table
CREATE TABLE IF NOT EXISTS helios_system_metrics (
    metric_id NVARCHAR(255) PRIMARY KEY,
    metric_name NVARCHAR(255) NOT NULL,
    metric_value FLOAT,
    metric_unit NVARCHAR(50),
    context NVARCHAR(MAX),
    recorded_at DATETIME2 DEFAULT GETUTCDATE(),
    INDEX idx_name (metric_name),
    INDEX idx_time (recorded_at)
);
";
        }

        public static string GetDownScript()
        {
            return @"
DROP TABLE IF EXISTS helios_system_metrics;
DROP TABLE IF EXISTS helios_learning_records;
DROP TABLE IF EXISTS helios_agents;
DROP TABLE IF EXISTS helios_pattern_feedback;
DROP TABLE IF EXISTS helios_patterns;
";
        }
    }

    /// <summary>
    /// Database migration executor
    /// </summary>
    public class MigrationExecutor
    {
        private readonly IDbConnection _connection;
        private readonly ILogger<MigrationExecutor> _logger;

        public MigrationExecutor(IDbConnection connection, ILogger<MigrationExecutor> logger)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Execute migration up
        /// </summary>
        public async Task<bool> MigrateUpAsync()
        {
            try
            {
                var script = HELIOSMigrationV1.GetUpScript();
                
                if (_connection.State != System.Data.ConnectionState.Open)
                    _connection.Open();

                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = script;
                    command.CommandTimeout = 300;
                    await command.ExecuteNonQueryAsync();
                }

                _logger.LogInformation("HELIOS database migration UP completed successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing migration UP");
                return false;
            }
        }

        /// <summary>
        /// Execute migration down
        /// </summary>
        public async Task<bool> MigrateDownAsync()
        {
            try
            {
                var script = HELIOSMigrationV1.GetDownScript();
                
                if (_connection.State != System.Data.ConnectionState.Open)
                    _connection.Open();

                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = script;
                    command.CommandTimeout = 300;
                    await command.ExecuteNonQueryAsync();
                }

                _logger.LogInformation("HELIOS database migration DOWN completed successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing migration DOWN");
                return false;
            }
        }
    }

    /// <summary>
    /// Schema validator
    /// </summary>
    public class SchemaValidator
    {
        private readonly IDbConnection _connection;
        private readonly ILogger<SchemaValidator> _logger;

        public SchemaValidator(IDbConnection connection, ILogger<SchemaValidator> logger)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Validate that all required HELIOS tables exist
        /// </summary>
        public async Task<SchemaValidationResult> ValidateSchemaAsync()
        {
            var result = new SchemaValidationResult();

            try
            {
                var requiredTables = new[]
                {
                    "helios_patterns",
                    "helios_pattern_feedback",
                    "helios_agents",
                    "helios_learning_records",
                    "helios_system_metrics"
                };

                if (_connection.State != System.Data.ConnectionState.Open)
                    _connection.Open();

                foreach (var table in requiredTables)
                {
                    var exists = await TableExistsAsync(table);
                    result.TableValidation[table] = exists;
                    
                    if (!exists)
                    {
                        result.IsValid = false;
                        _logger.LogWarning("Required table {Table} not found", table);
                    }
                }

                // Validate key columns
                if (result.TableValidation["helios_patterns"])
                {
                    var hasColumns = await ColumnsExistAsync("helios_patterns", 
                        new[] { "pattern_id", "name", "category", "confidence_score" });
                    result.ColumnValidation["helios_patterns"] = hasColumns;
                    if (!hasColumns)
                        result.IsValid = false;
                }

                _logger.LogInformation("Schema validation completed: IsValid={IsValid}", result.IsValid);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating schema");
                result.IsValid = false;
                result.ErrorMessage = ex.Message;
                return result;
            }
        }

        private async Task<bool> TableExistsAsync(string tableName)
        {
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = @"
                    SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES 
                    WHERE TABLE_NAME = @TableName
                ";
                command.Parameters.Add(new System.Data.SqlClient.SqlParameter("@TableName", tableName));
                var result = await command.ExecuteScalarAsync();
                return (int)result > 0;
            }
        }

        private async Task<bool> ColumnsExistAsync(string tableName, string[] columnNames)
        {
            using (var command = _connection.CreateCommand())
            {
                var placeholders = string.Join(",", columnNames.Select((_, i) => $"@Col{i}"));
                command.CommandText = $@"
                    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_NAME = @TableName AND COLUMN_NAME IN ({placeholders})
                ";
                command.Parameters.Add(new System.Data.SqlClient.SqlParameter("@TableName", tableName));
                
                for (int i = 0; i < columnNames.Length; i++)
                {
                    command.Parameters.Add(new System.Data.SqlClient.SqlParameter($"@Col{i}", columnNames[i]));
                }

                var result = await command.ExecuteScalarAsync();
                return (int)result == columnNames.Length;
            }
        }
    }

    public class SchemaValidationResult
    {
        public bool IsValid { get; set; } = true;
        public Dictionary<string, bool> TableValidation { get; set; } = new();
        public Dictionary<string, bool> ColumnValidation { get; set; } = new();
        public string ErrorMessage { get; set; }
    }
}
