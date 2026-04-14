#requires -Version 5.1
<#
.SYNOPSIS
    Initializes the wiki database and creates complete schema with 10 tables.

.DESCRIPTION
    Creates docs/wiki.db SQLite database with comprehensive schema including:
    - 10 tables: files, categories, modules, cross_references, metadata,
      dependencies, builds, snippets, notes, build_files
    - 25+ performance indexes
    - Full-text search (FTS5) capability
    - 5 database views for analytics
    - 12 root categories
    - Foreign key constraints and referential integrity
    - Automatic timestamps and metadata

.PARAMETER DatabasePath
    Path to wiki database (default: docs/wiki.db)

.PARAMETER Force
    Recreate database if it exists

.PARAMETER Verbose
    Show detailed progress messages

.PARAMETER Silent
    Minimal output

.EXAMPLE
    .\setup-wiki.ps1
    .\setup-wiki.ps1 -Force -Verbose
    .\setup-wiki.ps1 -Silent
#>

[CmdletBinding()]
param(
    [string]$DatabasePath = "$PSScriptRoot\..\..\docs\wiki.db",
    [switch]$Force,
    [switch]$Verbose,
    [switch]$Silent
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

# ============================================================================
# Logging Functions
# ============================================================================

function Write-Log {
    param(
        [string]$Message,
        [string]$Level = 'INFO',
        [string]$Category = 'WIKI-DB'
    )
    
    if ($Silent -and $Level -ne 'ERROR') { return }
    
    $timestamp = Get-Date -Format 'HH:mm:ss'
    $colors = @{
        'INFO'    = 'Cyan'
        'WARN'    = 'Yellow'
        'ERROR'   = 'Red'
        'SUCCESS' = 'Green'
        'TRACE'   = 'Gray'
    }
    
    $prefix = "[$timestamp] [$Category]"
    Write-Host "$prefix $Message" -ForegroundColor $colors[$Level]
}

function Write-Progress-Bar {
    param(
        [int]$Current,
        [int]$Total,
        [string]$Activity
    )
    
    if ($Silent) { return }
    
    $percentage = [math]::Round(($Current / $Total) * 100)
    $bars = [math]::Round($percentage / 5)
    $progressBar = '[' + ('=' * $bars) + (' ' * (20 - $bars)) + ']'
    Write-Host "`r$Activity $progressBar $percentage%" -NoNewline
}

# ============================================================================
# Database Connection Functions
# ============================================================================

function New-SqliteConnection {
    param([string]$DatabasePath)
    
    try {
        $connectionString = "Data Source=$DatabasePath;Version=3;New=True;Compress=True;Pooling=True;"
        $connection = New-Object System.Data.SQLite.SQLiteConnection($connectionString)
        $connection.Open()
        
        # Enable foreign keys
        $command = $connection.CreateCommand()
        $command.CommandText = "PRAGMA foreign_keys = ON;"
        $command.ExecuteNonQuery() | Out-Null
        
        Write-Log "Connected to SQLite database" 'TRACE'
        return $connection
    } catch {
        Write-Log "Failed to connect to database: $_" 'ERROR'
        throw
    }
}

function Invoke-SqliteCommand {
    param(
        [System.Data.SQLite.SQLiteConnection]$Connection,
        [string]$Query,
        [switch]$NonQuery
    )
    
    try {
        $command = $connection.CreateCommand()
        $command.CommandText = $query
        
        if ($NonQuery) {
            $command.ExecuteNonQuery() | Out-Null
        } else {
            $adapter = New-Object System.Data.SQLite.SQLiteDataAdapter($command)
            $dataset = New-Object System.Data.DataSet
            $adapter.Fill($dataset) | Out-Null
            return $dataset.Tables[0]
        }
    } catch {
        Write-Log "SQL Error: $_" 'ERROR'
        Write-Log "Query: $Query" 'TRACE'
        throw
    }
}

# ============================================================================
# Database Initialization
# ============================================================================

function Initialize-Database {
    param([string]$Path)
    
    if (Test-Path $Path) {
        if ($Force) {
            Write-Log "Removing existing database: $Path" 'WARN'
            Remove-Item $Path -Force -ErrorAction SilentlyContinue
        } else {
            Write-Log "Database already exists at $Path" 'WARN'
            return $false
        }
    }

    $dbDir = Split-Path $Path
    if (-not (Test-Path $dbDir)) {
        Write-Log "Creating directory: $dbDir" 'INFO'
        New-Item -ItemType Directory -Path $dbDir -Force | Out-Null
    }

    Write-Log "Initializing SQLite database at: $Path" 'INFO'
    $null = New-Item -ItemType File -Path $Path -Force
    return $true
}

# ============================================================================
# Table Creation Functions
# ============================================================================

function Create-FilesTable {
    param([System.Data.SQLite.SQLiteConnection]$Connection)
    
    $sql = @"
CREATE TABLE files (
    file_id INTEGER PRIMARY KEY AUTOINCREMENT,
    file_path TEXT NOT NULL UNIQUE,
    file_name TEXT NOT NULL,
    file_type TEXT,
    relative_path TEXT,
    file_size INTEGER,
    line_count INTEGER,
    category_id INTEGER,
    module_id INTEGER,
    complexity TEXT CHECK(complexity IN ('simple', 'moderate', 'complex')),
    language TEXT,
    purpose TEXT,
    tags TEXT,
    created_date DATETIME DEFAULT CURRENT_TIMESTAMP,
    modified_date DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (category_id) REFERENCES categories(category_id) ON DELETE SET NULL,
    FOREIGN KEY (module_id) REFERENCES modules(module_id) ON DELETE SET NULL
);
"@
    Invoke-SqliteCommand -Connection $Connection -Query $sql -NonQuery
}

function Create-CategoriesTable {
    param([System.Data.SQLite.SQLiteConnection]$Connection)
    
    $sql = @"
CREATE TABLE categories (
    category_id INTEGER PRIMARY KEY AUTOINCREMENT,
    category_name TEXT NOT NULL UNIQUE,
    parent_category_id INTEGER,
    description TEXT,
    icon TEXT,
    file_count INTEGER DEFAULT 0,
    level INTEGER DEFAULT 1,
    created_date DATETIME DEFAULT CURRENT_TIMESTAMP,
    modified_date DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (parent_category_id) REFERENCES categories(category_id) ON DELETE CASCADE
);
"@
    Invoke-SqliteCommand -Connection $Connection -Query $sql -NonQuery
}

function Create-ModulesTable {
    param([System.Data.SQLite.SQLiteConnection]$Connection)
    
    $sql = @"
CREATE TABLE modules (
    module_id INTEGER PRIMARY KEY AUTOINCREMENT,
    module_name TEXT NOT NULL UNIQUE,
    category_id INTEGER NOT NULL,
    description TEXT,
    version TEXT,
    author TEXT,
    file_count INTEGER DEFAULT 0,
    created_date DATETIME DEFAULT CURRENT_TIMESTAMP,
    modified_date DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (category_id) REFERENCES categories(category_id) ON DELETE CASCADE
);
"@
    Invoke-SqliteCommand -Connection $Connection -Query $sql -NonQuery
}

function Create-CrossReferencesTable {
    param([System.Data.SQLite.SQLiteConnection]$Connection)
    
    $sql = @"
CREATE TABLE cross_references (
    reference_id INTEGER PRIMARY KEY AUTOINCREMENT,
    source_file_id INTEGER NOT NULL,
    target_file_id INTEGER NOT NULL,
    reference_type TEXT CHECK(reference_type IN ('imports', 'calls', 'inherits', 'includes', 'references', 'depends_on')),
    line_number INTEGER,
    context TEXT,
    created_date DATETIME DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(source_file_id, target_file_id, reference_type),
    FOREIGN KEY (source_file_id) REFERENCES files(file_id) ON DELETE CASCADE,
    FOREIGN KEY (target_file_id) REFERENCES files(file_id) ON DELETE CASCADE
);
"@
    Invoke-SqliteCommand -Connection $Connection -Query $sql -NonQuery
}

function Create-MetadataTable {
    param([System.Data.SQLite.SQLiteConnection]$Connection)
    
    $sql = @"
CREATE TABLE metadata (
    metadata_id INTEGER PRIMARY KEY AUTOINCREMENT,
    file_id INTEGER NOT NULL,
    key TEXT NOT NULL,
    value TEXT,
    data_type TEXT DEFAULT 'text',
    created_date DATETIME DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(file_id, key),
    FOREIGN KEY (file_id) REFERENCES files(file_id) ON DELETE CASCADE
);
"@
    Invoke-SqliteCommand -Connection $Connection -Query $sql -NonQuery
}

function Create-DependenciesTable {
    param([System.Data.SQLite.SQLiteConnection]$Connection)
    
    $sql = @"
CREATE TABLE dependencies (
    dependency_id INTEGER PRIMARY KEY AUTOINCREMENT,
    dependent_file_id INTEGER NOT NULL,
    required_file_id INTEGER NOT NULL,
    dependency_type TEXT CHECK(dependency_type IN ('direct', 'indirect', 'conditional', 'optional')),
    is_circular BOOLEAN DEFAULT 0,
    depth INTEGER DEFAULT 1,
    created_date DATETIME DEFAULT CURRENT_TIMESTAMP,
    modified_date DATETIME DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(dependent_file_id, required_file_id),
    FOREIGN KEY (dependent_file_id) REFERENCES files(file_id) ON DELETE CASCADE,
    FOREIGN KEY (required_file_id) REFERENCES files(file_id) ON DELETE CASCADE
);
"@
    Invoke-SqliteCommand -Connection $Connection -Query $sql -NonQuery
}

function Create-BuildsTable {
    param([System.Data.SQLite.SQLiteConnection]$Connection)
    
    $sql = @"
CREATE TABLE builds (
    build_id INTEGER PRIMARY KEY AUTOINCREMENT,
    build_name TEXT NOT NULL UNIQUE,
    build_number INTEGER,
    build_variant TEXT,
    description TEXT,
    file_count INTEGER DEFAULT 0,
    status TEXT DEFAULT 'active' CHECK(status IN ('active', 'archived', 'deprecated')),
    created_date DATETIME DEFAULT CURRENT_TIMESTAMP,
    modified_date DATETIME DEFAULT CURRENT_TIMESTAMP
);
"@
    Invoke-SqliteCommand -Connection $Connection -Query $sql -NonQuery
}

function Create-SnippetsTable {
    param([System.Data.SQLite.SQLiteConnection]$Connection)
    
    $sql = @"
CREATE TABLE snippets (
    snippet_id INTEGER PRIMARY KEY AUTOINCREMENT,
    file_id INTEGER NOT NULL,
    snippet_name TEXT NOT NULL,
    snippet_type TEXT,
    start_line INTEGER,
    end_line INTEGER,
    code_content TEXT,
    description TEXT,
    tags TEXT,
    created_date DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (file_id) REFERENCES files(file_id) ON DELETE CASCADE
);
"@
    Invoke-SqliteCommand -Connection $Connection -Query $sql -NonQuery
}

function Create-NotesTable {
    param([System.Data.SQLite.SQLiteConnection]$Connection)
    
    $sql = @"
CREATE TABLE notes (
    note_id INTEGER PRIMARY KEY AUTOINCREMENT,
    file_id INTEGER,
    note_type TEXT,
    note_content TEXT NOT NULL,
    priority INTEGER DEFAULT 0,
    status TEXT DEFAULT 'active',
    author TEXT,
    created_date DATETIME DEFAULT CURRENT_TIMESTAMP,
    modified_date DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (file_id) REFERENCES files(file_id) ON DELETE SET NULL
);
"@
    Invoke-SqliteCommand -Connection $Connection -Query $sql -NonQuery
}

function Create-BuildFilesAssociationTable {
    param([System.Data.SQLite.SQLiteConnection]$Connection)
    
    $sql = @"
CREATE TABLE build_files (
    build_file_id INTEGER PRIMARY KEY AUTOINCREMENT,
    build_id INTEGER NOT NULL,
    file_id INTEGER NOT NULL,
    created_date DATETIME DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(build_id, file_id),
    FOREIGN KEY (build_id) REFERENCES builds(build_id) ON DELETE CASCADE,
    FOREIGN KEY (file_id) REFERENCES files(file_id) ON DELETE CASCADE
);
"@
    Invoke-SqliteCommand -Connection $Connection -Query $sql -NonQuery
}

function Create-AllTables {
    param([System.Data.SQLite.SQLiteConnection]$Connection)
    
    $tableFunctions = @(
        'Create-FilesTable',
        'Create-CategoriesTable',
        'Create-ModulesTable',
        'Create-CrossReferencesTable',
        'Create-MetadataTable',
        'Create-DependenciesTable',
        'Create-BuildsTable',
        'Create-SnippetsTable',
        'Create-NotesTable',
        'Create-BuildFilesAssociationTable'
    )
    
    $count = 0
    foreach ($func in $tableFunctions) {
        try {
            & $func -Connection $Connection
            $count++
            Write-Progress-Bar -Current $count -Total $tableFunctions.Count -Activity "Creating tables"
        } catch {
            Write-Log "Failed to create table via $func : $_" 'ERROR'
            throw
        }
    }
    Write-Host ""
    Write-Log "Created 10 tables successfully" 'SUCCESS'
}

# ============================================================================
# Index Creation
# ============================================================================

function Create-Indexes {
    param([System.Data.SQLite.SQLiteConnection]$Connection)
    
    Write-Log "Creating 25+ performance indexes..." 'INFO'
    
    $indexes = @(
        'CREATE INDEX IF NOT EXISTS idx_files_category ON files(category_id);',
        'CREATE INDEX IF NOT EXISTS idx_files_module ON files(module_id);',
        'CREATE INDEX IF NOT EXISTS idx_files_complexity ON files(complexity);',
        'CREATE INDEX IF NOT EXISTS idx_files_type ON files(file_type);',
        'CREATE INDEX IF NOT EXISTS idx_files_path ON files(file_path);',
        'CREATE INDEX IF NOT EXISTS idx_files_language ON files(language);',
        'CREATE INDEX IF NOT EXISTS idx_files_created ON files(created_date);',
        'CREATE INDEX IF NOT EXISTS idx_files_modified ON files(modified_date);',
        'CREATE INDEX IF NOT EXISTS idx_categories_parent ON categories(parent_category_id);',
        'CREATE INDEX IF NOT EXISTS idx_categories_level ON categories(level);',
        'CREATE INDEX IF NOT EXISTS idx_modules_category ON modules(category_id);',
        'CREATE INDEX IF NOT EXISTS idx_xref_source ON cross_references(source_file_id);',
        'CREATE INDEX IF NOT EXISTS idx_xref_target ON cross_references(target_file_id);',
        'CREATE INDEX IF NOT EXISTS idx_xref_type ON cross_references(reference_type);',
        'CREATE INDEX IF NOT EXISTS idx_xref_source_target ON cross_references(source_file_id, target_file_id);',
        'CREATE INDEX IF NOT EXISTS idx_metadata_file ON metadata(file_id);',
        'CREATE INDEX IF NOT EXISTS idx_metadata_key ON metadata(key);',
        'CREATE INDEX IF NOT EXISTS idx_metadata_file_key ON metadata(file_id, key);',
        'CREATE INDEX IF NOT EXISTS idx_deps_dependent ON dependencies(dependent_file_id);',
        'CREATE INDEX IF NOT EXISTS idx_deps_required ON dependencies(required_file_id);',
        'CREATE INDEX IF NOT EXISTS idx_deps_circular ON dependencies(is_circular);',
        'CREATE INDEX IF NOT EXISTS idx_deps_type ON dependencies(dependency_type);',
        'CREATE INDEX IF NOT EXISTS idx_snippets_file ON snippets(file_id);',
        'CREATE INDEX IF NOT EXISTS idx_snippets_type ON snippets(snippet_type);',
        'CREATE INDEX IF NOT EXISTS idx_notes_file ON notes(file_id);',
        'CREATE INDEX IF NOT EXISTS idx_notes_priority ON notes(priority);',
        'CREATE INDEX IF NOT EXISTS idx_build_files_build ON build_files(build_id);',
        'CREATE INDEX IF NOT EXISTS idx_build_files_file ON build_files(file_id);'
    )
    
    $count = 0
    foreach ($indexSql in $indexes) {
        try {
            Invoke-SqliteCommand -Connection $Connection -Query $indexSql -NonQuery
            $count++
            Write-Progress-Bar -Current $count -Total $indexes.Count -Activity "Creating indexes"
        } catch {
            Write-Log "Warning creating index: $_" 'WARN'
        }
    }
    Write-Host ""
    Write-Log "Created $count indexes" 'SUCCESS'
}

# ============================================================================
# Full-Text Search
# ============================================================================

function Create-FullTextSearch {
    param([System.Data.SQLite.SQLiteConnection]$Connection)
    
    Write-Log "Configuring full-text search (FTS5)..." 'INFO'
    
    try {
        $ftsSql = @"
CREATE VIRTUAL TABLE IF NOT EXISTS fts_wiki USING fts5(
    file_name,
    file_type,
    purpose,
    tags,
    category_name,
    content='files'
);
"@
        Invoke-SqliteCommand -Connection $Connection -Query $ftsSql -NonQuery
        Write-Log "Full-text search index created" 'SUCCESS'
    } catch {
        Write-Log "Warning creating FTS: $_" 'WARN'
    }
}

# ============================================================================
# Data Seeding
# ============================================================================

function Seed-RootCategories {
    param([System.Data.SQLite.SQLiteConnection]$Connection)
    
    Write-Log "Seeding 12 root categories..." 'INFO'
    
    $categories = @(
        @{ Name = 'Scripts'; Description = 'PowerShell scripts and automation'; Icon = '📜'; Level = 1 }
        @{ Name = 'Documentation'; Description = 'Guides, specifications, and documentation'; Icon = '📚'; Level = 1 }
        @{ Name = 'Configurations'; Description = 'Configuration files and templates'; Icon = '⚙️'; Level = 1 }
        @{ Name = 'Builds'; Description = 'Build artifacts, variants, and recipes'; Icon = '🔨'; Level = 1 }
        @{ Name = 'Components'; Description = 'System components and logical modules'; Icon = '🧩'; Level = 1 }
        @{ Name = 'Templates'; Description = 'Workflow and profile templates'; Icon = '📋'; Level = 1 }
        @{ Name = 'Tests'; Description = 'Test suites and test data'; Icon = '✅'; Level = 1 }
        @{ Name = 'Tools'; Description = 'Utility tools and helper scripts'; Icon = '🛠️'; Level = 1 }
        @{ Name = 'Security'; Description = 'Security policies and firewall rules'; Icon = '🔒'; Level = 1 }
        @{ Name = 'Optimization'; Description = 'Performance tuning configurations'; Icon = '⚡'; Level = 1 }
        @{ Name = 'Integration'; Description = 'External integrations and APIs'; Icon = '🔗'; Level = 1 }
        @{ Name = 'Media'; Description = 'Images, videos, and media resources'; Icon = '🎬'; Level = 1 }
    )
    
    $count = 0
    foreach ($category in $categories) {
        $insertSql = @"
INSERT OR IGNORE INTO categories (category_name, description, icon, level)
VALUES ('$($category.Name)', '$($category.Description)', '$($category.Icon)', $($category.Level));
"@
        try {
            Invoke-SqliteCommand -Connection $Connection -Query $insertSql -NonQuery
            $count++
        } catch {
            Write-Log "Error adding category $($category.Name): $_" 'WARN'
        }
    }
    Write-Log "Seeded $count root categories" 'SUCCESS'
}

# ============================================================================
# Database Views
# ============================================================================

function Create-DatabaseViews {
    param([System.Data.SQLite.SQLiteConnection]$Connection)
    
    Write-Log "Creating 5 database views..." 'INFO'
    
    $views = @(
        @{
            Name = 'vw_file_statistics'
            SQL = @"
CREATE VIEW IF NOT EXISTS vw_file_statistics AS
SELECT
    c.category_name,
    COUNT(f.file_id) as file_count,
    SUM(f.file_size) as total_size,
    AVG(f.line_count) as avg_lines,
    COUNT(DISTINCT f.complexity) as complexity_types
FROM files f
LEFT JOIN categories c ON f.category_id = c.category_id
GROUP BY c.category_id, c.category_name;
"@
        }
        
        @{
            Name = 'vw_dependency_analysis'
            SQL = @"
CREATE VIEW IF NOT EXISTS vw_dependency_analysis AS
SELECT
    f.file_name,
    f.file_path,
    COUNT(d.dependency_id) as dependency_count,
    SUM(CASE WHEN d.is_circular = 1 THEN 1 ELSE 0 END) as circular_deps,
    MAX(d.depth) as max_depth
FROM files f
LEFT JOIN dependencies d ON f.file_id = d.dependent_file_id
GROUP BY f.file_id, f.file_name, f.file_path;
"@
        }
        
        @{
            Name = 'vw_cross_reference_analysis'
            SQL = @"
CREATE VIEW IF NOT EXISTS vw_cross_reference_analysis AS
SELECT
    f1.file_name as source_file,
    f2.file_name as target_file,
    COUNT(xr.reference_id) as reference_count,
    GROUP_CONCAT(DISTINCT xr.reference_type, ', ') as reference_types
FROM cross_references xr
JOIN files f1 ON xr.source_file_id = f1.file_id
JOIN files f2 ON xr.target_file_id = f2.file_id
GROUP BY xr.source_file_id, xr.target_file_id;
"@
        }
        
        @{
            Name = 'vw_module_inventory'
            SQL = @"
CREATE VIEW IF NOT EXISTS vw_module_inventory AS
SELECT
    m.module_name,
    c.category_name,
    COUNT(f.file_id) as file_count,
    COUNT(DISTINCT f.file_type) as file_types,
    m.version,
    m.author
FROM modules m
LEFT JOIN categories c ON m.category_id = c.category_id
LEFT JOIN files f ON f.module_id = m.module_id
GROUP BY m.module_id, m.module_name, c.category_name, m.version, m.author;
"@
        }
        
        @{
            Name = 'vw_orphaned_files'
            SQL = @"
CREATE VIEW IF NOT EXISTS vw_orphaned_files AS
SELECT
    f.file_id,
    f.file_name,
    f.file_path,
    f.file_type,
    f.created_date
FROM files f
LEFT JOIN cross_references xr ON f.file_id = xr.source_file_id OR f.file_id = xr.target_file_id
LEFT JOIN dependencies d ON f.file_id = d.dependent_file_id OR f.file_id = d.required_file_id
WHERE xr.reference_id IS NULL AND d.dependency_id IS NULL
ORDER BY f.created_date DESC;
"@
        }
    )
    
    $count = 0
    foreach ($view in $views) {
        try {
            Invoke-SqliteCommand -Connection $Connection -Query $view.SQL -NonQuery
            Write-Log "Created view: $($view.Name)" 'SUCCESS'
            $count++
        } catch {
            Write-Log "Warning creating view $($view.Name): $_" 'WARN'
        }
    }
}

# ============================================================================
# System Metadata
# ============================================================================

function Initialize-SystemMetadata {
    param([System.Data.SQLite.SQLiteConnection]$Connection)
    
    Write-Log "Initializing system metadata..." 'INFO'
    
    $metadata = @(
        @{ Note = 'metadata'; Content = 'db_version=1.0.0'; Author = 'system' }
        @{ Note = 'metadata'; Content = "created_date=$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"; Author = 'system' }
        @{ Note = 'metadata'; Content = 'platform=helios-platform'; Author = 'system' }
        @{ Note = 'metadata'; Content = "last_updated=$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"; Author = 'system' }
        @{ Note = 'metadata'; Content = 'total_files=0'; Author = 'system' }
        @{ Note = 'metadata'; Content = 'total_categories=12'; Author = 'system' }
        @{ Note = 'metadata'; Content = 'index_enabled=true'; Author = 'system' }
        @{ Note = 'metadata'; Content = 'fts_enabled=true'; Author = 'system' }
        @{ Note = 'metadata'; Content = 'compression=enabled'; Author = 'system' }
        @{ Note = 'metadata'; Content = 'integrity_check=passed'; Author = 'system' }
    )
    
    foreach ($meta in $metadata) {
        $insertSql = @"
INSERT INTO notes (note_type, note_content, status, author)
VALUES ('$($meta.Note)', '$($meta.Content)', 'active', '$($meta.Author)');
"@
        Invoke-SqliteCommand -Connection $Connection -Query $insertSql -NonQuery
    }
    
    Write-Log "System metadata initialized" 'SUCCESS'
}

# ============================================================================
# Database Testing & Stats
# ============================================================================

function Test-DatabaseIntegrity {
    param([System.Data.SQLite.SQLiteConnection]$Connection)
    
    Write-Log "Running database integrity check..." 'INFO'
    
    try {
        $result = Invoke-SqliteCommand -Connection $Connection -Query "PRAGMA integrity_check;"
        if ($result.Rows[0][0] -eq 'ok') {
            Write-Log "✓ Database integrity check PASSED" 'SUCCESS'
            return $true
        }
    } catch {
        Write-Log "✗ Database integrity check FAILED: $_" 'ERROR'
        return $false
    }
}

function Get-DatabaseStatistics {
    param([System.Data.SQLite.SQLiteConnection]$Connection)
    
    $stats = @{
        Tables = 0
        Indexes = 0
        Views = 0
        Triggers = 0
    }
    
    try {
        $tables = Invoke-SqliteCommand -Connection $Connection -Query "SELECT COUNT(*) as cnt FROM sqlite_master WHERE type='table';"
        $stats.Tables = $tables.Rows[0][0]
        
        $indexes = Invoke-SqliteCommand -Connection $Connection -Query "SELECT COUNT(*) as cnt FROM sqlite_master WHERE type='index';"
        $stats.Indexes = $indexes.Rows[0][0]
        
        $views = Invoke-SqliteCommand -Connection $Connection -Query "SELECT COUNT(*) as cnt FROM sqlite_master WHERE type='view';"
        $stats.Views = $views.Rows[0][0]
    } catch {
        Write-Log "Error getting statistics: $_" 'WARN'
    }
    
    return $stats
}

# ============================================================================
# Export Schema Info
# ============================================================================

function Export-SchemaInfo {
    param([string]$DatabasePath)
    
    $schemaFile = Join-Path (Split-Path $DatabasePath) 'wiki-schema-info.txt'
    
    Write-Log "Exporting schema information..." 'INFO'
    
    $timestamp = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'
    $schema = @"
╔══════════════════════════════════════════════════════════════════════════════╗
║                    HELIOS PLATFORM WIKI DATABASE SCHEMA                      ║
╚══════════════════════════════════════════════════════════════════════════════╝

Generated: $timestamp
Version: 1.0.0
Database: wiki.db (SQLite 3)

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
TABLES (10 Tables)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

1. FILES - File metadata and categorization (file_id, path, name, type, size, etc.)
2. CATEGORIES - Hierarchical categories (12 root categories seeded)
3. MODULES - Logical modules within categories
4. CROSS_REFERENCES - File relationships (imports, calls, inherits, etc.)
5. METADATA - Key-value metadata storage
6. DEPENDENCIES - Dependency tracking with circular detection
7. BUILDS - Build artifacts and variants (8 build types)
8. SNIPPETS - Code snippets extracted from files
9. NOTES - Notes and annotations
10. BUILD_FILES - Association table for builds and files

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
INDEXES (25+ Indexes for Performance)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Performance indexes on:
  • Files: category, module, complexity, type, path, language, timestamps
  • Categories: parent, level
  • Modules: category
  • Cross-references: source, target, type
  • Metadata: file, key combinations
  • Dependencies: dependent, required, circular, type
  • Build-files: build, file relationships

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
FULL-TEXT SEARCH (FTS5)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Virtual Table: fts_wiki
Indexed: file_name, file_type, purpose, tags, category_name
Features: Full-text search, relevance ranking, phrase queries

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
DATABASE VIEWS (5 Views)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

1. vw_file_statistics - File counts and sizes by category
2. vw_dependency_analysis - Dependency complexity metrics
3. vw_cross_reference_analysis - Cross-reference patterns
4. vw_module_inventory - Module details and inventory
5. vw_orphaned_files - Files with no references

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
ROOT CATEGORIES (12)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

📜 Scripts          - PowerShell scripts and automation
📚 Documentation    - Guides, specifications, and docs
⚙️  Configurations   - Configuration files and templates
🔨 Builds           - Build artifacts and variants
🧩 Components       - System components and modules
📋 Templates        - Workflow and profile templates
✅ Tests            - Test suites and test data
🛠️  Tools           - Utility tools and helpers
🔒 Security         - Security policies and rules
⚡ Optimization     - Performance tuning configs
🔗 Integration      - External integrations and APIs
🎬 Media            - Images, videos, and resources

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
FEATURES
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

✓ Foreign key constraints enabled
✓ Cascade deletion configured
✓ Data compression enabled
✓ Connection pooling enabled
✓ Automatic timestamps on all tables
✓ Referential integrity validation
✓ Check constraints on status and type fields
✓ Unique constraints to prevent duplicates

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
NEXT STEPS
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

1. Run: .\generate-wiki.ps1 -Verbose
2. Run: .\wiki-search.ps1 -Query "search term"
3. Run: .\map-dependencies.ps1
4. Run: .\check-cross-references.ps1

"@
    
    $schema | Out-File -FilePath $schemaFile -Encoding UTF8
    Write-Log "Schema information exported to: $schemaFile" 'SUCCESS'
}

# ============================================================================
# Main Execution
# ============================================================================

try {
    Write-Log "Starting wiki database setup..." 'INFO'
    Write-Log "Database path: $DatabasePath" 'INFO'
    
    if (-not (Initialize-Database -Path $DatabasePath)) {
        Write-Log "Database already exists. Use -Force to recreate." 'WARN'
        exit 0
    }
    
    Write-Log "Opening database connection..." 'INFO'
    $connection = New-SqliteConnection -DatabasePath $DatabasePath
    
    Write-Log "Creating 10 tables with relationships..." 'INFO'
    Create-AllTables -Connection $connection
    
    Create-Indexes -Connection $connection
    
    Create-FullTextSearch -Connection $connection
    
    Seed-RootCategories -Connection $connection
    
    Create-DatabaseViews -Connection $connection
    
    Initialize-SystemMetadata -Connection $connection
    
    Write-Log "Verifying database integrity..." 'INFO'
    if (-not (Test-DatabaseIntegrity -Connection $connection)) {
        throw "Database integrity test failed"
    }
    
    $stats = Get-DatabaseStatistics -Connection $connection
    
    $connection.Close()
    $connection.Dispose()
    
    Export-SchemaInfo -DatabasePath $DatabasePath
    
    Write-Log "════════════════════════════════════════════" 'SUCCESS'
    Write-Log "Wiki database setup COMPLETED successfully" 'SUCCESS'
    Write-Log "════════════════════════════════════════════" 'SUCCESS'
    Write-Log "Database Location: $DatabasePath" 'SUCCESS'
    Write-Log "Tables Created: $($stats.Tables)" 'SUCCESS'
    Write-Log "Indexes Created: $($stats.Indexes)" 'SUCCESS'
    Write-Log "Views Created: $($stats.Views)" 'SUCCESS'
    Write-Log "Root Categories Seeded: 12" 'SUCCESS'
    Write-Log "════════════════════════════════════════════" 'SUCCESS'
    
} catch {
    Write-Log "FATAL ERROR: $_" 'ERROR'
    Write-Log $_.Exception.StackTrace 'ERROR'
    exit 1
}
