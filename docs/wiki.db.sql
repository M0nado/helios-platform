-- Helios Platform Wiki Database Schema
-- SQLite3 database for comprehensive documentation

-- Files table: Master registry of all documented files
CREATE TABLE IF NOT EXISTS files (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    path TEXT NOT NULL UNIQUE,
    name TEXT NOT NULL,
    category_id INTEGER,
    file_type TEXT,
    purpose TEXT,
    complexity TEXT CHECK(complexity IN ('simple', 'moderate', 'complex', 'advanced')),
    status TEXT DEFAULT 'active' CHECK(status IN ('active', 'deprecated', 'experimental', 'archived')),
    version TEXT,
    build_inclusion TEXT DEFAULT 'standard' CHECK(build_inclusion IN ('standard', 'optional', 'enterprise', 'internal')),
    last_modified DATETIME DEFAULT CURRENT_TIMESTAMP,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    documented BOOLEAN DEFAULT 0,
    FOREIGN KEY (category_id) REFERENCES categories(id)
);

-- Categories table: Hierarchical documentation structure
CREATE TABLE IF NOT EXISTS categories (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL UNIQUE,
    parent_id INTEGER,
    level INTEGER,
    description TEXT,
    icon TEXT,
    order_index INTEGER,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (parent_id) REFERENCES categories(id)
);

-- Cross-references table: Links between files and concepts
CREATE TABLE IF NOT EXISTS cross_references (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    source_file_id INTEGER NOT NULL,
    target_file_id INTEGER,
    target_concept TEXT,
    reference_type TEXT CHECK(reference_type IN ('depends_on', 'used_by', 'related', 'extends', 'implements', 'conflicts')),
    conflict_potential BOOLEAN DEFAULT 0,
    conflict_notes TEXT,
    validated BOOLEAN DEFAULT 0,
    validation_date DATETIME,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (source_file_id) REFERENCES files(id),
    FOREIGN KEY (target_file_id) REFERENCES files(id)
);

-- Notes table: Team annotations and change history
CREATE TABLE IF NOT EXISTS notes (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    file_id INTEGER,
    note_type TEXT CHECK(note_type IN ('observation', 'warning', 'todo', 'deprecated', 'optimization', 'security', 'performance')),
    content TEXT NOT NULL,
    author TEXT,
    priority TEXT DEFAULT 'medium' CHECK(priority IN ('low', 'medium', 'high', 'critical')),
    resolved BOOLEAN DEFAULT 0,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (file_id) REFERENCES files(id)
);

-- Metadata table: Key-value store for additional information
CREATE TABLE IF NOT EXISTS metadata (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    file_id INTEGER,
    key TEXT NOT NULL,
    value TEXT,
    data_type TEXT DEFAULT 'string',
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(file_id, key),
    FOREIGN KEY (file_id) REFERENCES files(id)
);

-- Dependencies table: Component relationship tracking
CREATE TABLE IF NOT EXISTS dependencies (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    source_id INTEGER NOT NULL,
    target_id INTEGER NOT NULL,
    dependency_type TEXT CHECK(dependency_type IN ('hard', 'soft', 'optional', 'conditional')),
    is_circular BOOLEAN DEFAULT 0,
    depth INTEGER,
    description TEXT,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(source_id, target_id),
    FOREIGN KEY (source_id) REFERENCES files(id),
    FOREIGN KEY (target_id) REFERENCES files(id)
);

-- Builds table: Build configuration registry
CREATE TABLE IF NOT EXISTS builds (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL UNIQUE,
    description TEXT,
    environment TEXT,
    target_framework TEXT,
    include_optional BOOLEAN DEFAULT 0,
    include_enterprise BOOLEAN DEFAULT 0,
    components TEXT,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    modified_at DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- Build components junction table
CREATE TABLE IF NOT EXISTS build_components (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    build_id INTEGER NOT NULL,
    file_id INTEGER NOT NULL,
    inclusion_type TEXT DEFAULT 'required',
    order_index INTEGER,
    UNIQUE(build_id, file_id),
    FOREIGN KEY (build_id) REFERENCES builds(id),
    FOREIGN KEY (file_id) REFERENCES files(id)
);

-- Snippets table: Reusable code registry with compression
CREATE TABLE IF NOT EXISTS snippets (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL,
    category TEXT,
    language TEXT,
    code TEXT NOT NULL,
    compressed_code TEXT,
    description TEXT,
    file_id INTEGER,
    complexity TEXT CHECK(complexity IN ('simple', 'moderate', 'complex')),
    tags TEXT,
    usage_count INTEGER DEFAULT 0,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (file_id) REFERENCES files(id)
);

-- Create indexes for performance
CREATE INDEX IF NOT EXISTS idx_files_category ON files(category_id);
CREATE INDEX IF NOT EXISTS idx_files_type ON files(file_type);
CREATE INDEX IF NOT EXISTS idx_files_status ON files(status);
CREATE INDEX IF NOT EXISTS idx_files_complexity ON files(complexity);
CREATE INDEX IF NOT EXISTS idx_files_build ON files(build_inclusion);
CREATE INDEX IF NOT EXISTS idx_files_modified ON files(last_modified);

CREATE INDEX IF NOT EXISTS idx_categories_parent ON categories(parent_id);
CREATE INDEX IF NOT EXISTS idx_categories_level ON categories(level);

CREATE INDEX IF NOT EXISTS idx_xref_source ON cross_references(source_file_id);
CREATE INDEX IF NOT EXISTS idx_xref_target ON cross_references(target_file_id);
CREATE INDEX IF NOT EXISTS idx_xref_type ON cross_references(reference_type);
CREATE INDEX IF NOT EXISTS idx_xref_conflict ON cross_references(conflict_potential);

CREATE INDEX IF NOT EXISTS idx_notes_file ON notes(file_id);
CREATE INDEX IF NOT EXISTS idx_notes_type ON notes(note_type);
CREATE INDEX IF NOT EXISTS idx_notes_priority ON notes(priority);

CREATE INDEX IF NOT EXISTS idx_metadata_file ON metadata(file_id);
CREATE INDEX IF NOT EXISTS idx_metadata_key ON metadata(key);

CREATE INDEX IF NOT EXISTS idx_deps_source ON dependencies(source_id);
CREATE INDEX IF NOT EXISTS idx_deps_target ON dependencies(target_id);
CREATE INDEX IF NOT EXISTS idx_deps_circular ON dependencies(is_circular);

CREATE INDEX IF NOT EXISTS idx_build_components ON build_components(build_id);

CREATE INDEX IF NOT EXISTS idx_snippets_category ON snippets(category);
CREATE INDEX IF NOT EXISTS idx_snippets_language ON snippets(language);
CREATE INDEX IF NOT EXISTS idx_snippets_tags ON snippets(tags);

-- Views for common queries
CREATE VIEW IF NOT EXISTS active_files AS
SELECT f.* FROM files f
WHERE f.status = 'active'
ORDER BY f.category_id, f.name;

CREATE VIEW IF NOT EXISTS documented_files AS
SELECT f.* FROM files f
WHERE f.documented = 1 AND f.status = 'active'
ORDER BY f.complexity, f.category_id;

CREATE VIEW IF NOT EXISTS undocumented_files AS
SELECT f.* FROM files f
WHERE f.documented = 0 AND f.status IN ('active', 'experimental')
ORDER BY f.complexity DESC, f.category_id;

CREATE VIEW IF NOT EXISTS orphaned_files AS
SELECT f.* FROM files f
WHERE f.id NOT IN (SELECT source_file_id FROM cross_references)
AND f.id NOT IN (SELECT target_file_id FROM cross_references)
AND f.status = 'active';

CREATE VIEW IF NOT EXISTS circular_dependencies AS
SELECT d.* FROM dependencies d
WHERE d.is_circular = 1
ORDER BY d.depth DESC;

-- Metadata initialization
INSERT OR IGNORE INTO metadata (file_id, key, value, data_type) VALUES
(NULL, 'wiki_version', '1.0', 'string'),
(NULL, 'last_sync', datetime('now'), 'datetime'),
(NULL, 'documentation_level', '5', 'integer');
