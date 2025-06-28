# Database Layer Analysis

## Overview

F-Spot implements a sophisticated SQLite-based database layer using a Repository/Store pattern with comprehensive migration support. The database architecture has evolved through 18 major versions, managing complex relationships between photos, tags, rolls, and metadata.

## Architecture Pattern

### Core Components

- **Database Interface (`IDb`)**: Central abstraction defining all database stores
- **Database Implementation (`Db`)**: Main database coordinator implementing `IDb`
- **Store Pattern (`DbStore<T>`)**: Abstract base class for all data stores
- **Connection Layer (`FSpotDatabaseConnection`)**: Extends Hyena's SQLite connection
- **Migration System (`Updater`)**: Handles database schema versioning and upgrades

### Design Patterns

#### 1. Repository Pattern
Each store acts as a repository for its domain objects, providing abstracted access through the `IDb` interface with centralized query building and execution.

#### 2. Active Record Pattern (Partial)
- `DbItem` base class provides identity
- Domain objects (`Photo`, `Tag`) contain business logic
- Store classes handle persistence separately

#### 3. Observer Pattern
Event-driven notifications (`ItemsAdded`, `ItemsChanged`, `ItemsRemoved`) with thread-safe event marshaling via `ThreadAssist.ProxyToMain`.

## Database Stores

### PhotoStore - Primary Entity Store

**Purpose**: Manages photo records, their versions, and file locations

**Key Features**:
- Supports multiple versions per photo (Original + edited versions)
- Stores file paths as `base_uri` + `filename` (split since v17.0)
- Implements duplicate detection via MD5 hashing
- Provides complex querying with conditions and temporary tables
- Handles batch operations with transactions

**Schema**:
```sql
-- Main photos table
CREATE TABLE photos (
    id                  INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
    time                INTEGER NOT NULL,
    base_uri            STRING NOT NULL,
    filename            STRING NOT NULL,
    description         TEXT NOT NULL,
    roll_id             INTEGER NOT NULL,
    default_version_id  INTEGER NOT NULL,
    rating              INTEGER NULL
);

-- Photo versions (original + edits)
CREATE TABLE photo_versions (
    photo_id      INTEGER,
    version_id    INTEGER,
    name          STRING,
    base_uri      STRING NOT NULL,
    filename      STRING NOT NULL,
    import_md5    TEXT NULL,
    protected     BOOLEAN,
    UNIQUE (photo_id, version_id)
);

-- Photo-tag relationships
CREATE TABLE photo_tags (
    photo_id    INTEGER,
    tag_id      INTEGER,
    UNIQUE (photo_id, tag_id)
);
```

### TagStore - Hierarchical Tag Management

**Purpose**: Manages tags and categories in a tree structure

**Key Features**:
- Supports hierarchical categories with parent-child relationships
- Immortal caching (all tags kept in memory)
- Icon support (theme icons or custom pixbufs)
- Popularity tracking based on usage count
- Built-in system tags (Hidden, Favorites, People, Places, Events)

**Schema**:
```sql
CREATE TABLE tags (
    id            INTEGER PRIMARY KEY NOT NULL,
    name          TEXT UNIQUE,
    category_id   INTEGER,
    is_category   BOOLEAN,
    sort_priority INTEGER,
    icon          TEXT
);
```

### RollStore - Import Session Management

**Purpose**: Groups photos by import session/batch

**Key Features**:
- Simple time-based grouping
- Immutable once created
- Used for organizing imports and batch operations

### MetaStore - Configuration and Metadata

**Purpose**: Stores application metadata and configuration

**Key Items**:
- F-Spot Version tracking
- Database schema version
- Hidden tag ID reference
- Extensible key-value storage

### ExportStore - Export History Tracking

**Purpose**: Tracks which photos were exported where

**Export Types**: Flickr, Folder, Picasa, SmugMug, Gallery2

### JobStore - Background Task Management

**Purpose**: Manages persistent background jobs

**Job Types**: Hash calculation, metadata synchronization

**Integration**: Uses TinyIoC container for job resolution

## Performance Characteristics

### Caching Strategy
- **TagStore**: Immortal cache (all tags in memory)
- **PhotoStore**: Weak reference cache for photos
- **Lazy Loading**: Photo versions and tags loaded on demand
- **Bulk Loading**: Optimized batch queries for multiple photos

### Query Optimization
- **Indexing**:
  - `idx_photo_versions_id` on photo_versions(photo_id)
  - `idx_photo_versions_import_md5` on photo_versions(import_md5)
  - `idx_photos_roll_id` on photos(roll_id)
- **Temporary Tables**: Complex queries use temporary tables for better performance
- **Parameterized Queries**: All SQL uses HyenaSqliteCommand with parameters

### Connection Management
- **Single Connection**: One connection per database instance
- **Transaction Support**: Nested transaction handling
- **Threading**: Hyena provides thread-safe database access
- **PRAGMA Settings**: Configurable synchronous mode for performance vs. durability

## SQLite Usage Patterns

### Connection Features
- **Database Path**: File-based SQLite databases
- **Encoding**: UTF-8 (changed from UTF-16 in older versions)
- **Page Size**: 1024 bytes
- **Foreign Keys**: Not explicitly enabled (relies on application logic)

### Query Patterns
- **Prepared Statements**: All queries use parameterized commands
- **Bulk Operations**: Efficient batch inserts/updates
- **Complex Queries**: JOIN operations for photo-tag relationships
- **Aggregate Queries**: COUNT, GROUP BY for statistics

## Technical Debt and Issues

### Current Problems
1. **No Foreign Key Constraints**: Referential integrity handled in application code
2. **Legacy Migration Code**: Accumulated complexity from 18 migration versions
3. **Mixed Responsibilities**: Some business logic embedded in store classes
4. **Error Handling**: Limited database-specific exception handling
5. **Testing**: Database layer testing relies on actual SQLite files

### Performance Bottlenecks
1. **Full Tag Loading**: All tags loaded into memory on startup
2. **N+1 Query Problems**: Individual queries for photo versions/tags
3. **Large Result Sets**: No built-in pagination for large photo collections
4. **Index Coverage**: Some queries might benefit from additional composite indexes

### Scalability Concerns
1. **Single File Database**: No sharding or distribution support
2. **Memory Usage**: Tag cache and photo cache can grow large
3. **Concurrent Access**: Limited to single application instance
4. **Backup Strategy**: No built-in backup/restore functionality

## Recommendations

### Short-term Improvements
1. **Add Foreign Key Constraints**: Enable SQLite foreign key support
2. **Enhance Error Handling**: Implement specific database exceptions
3. **Optimize Queries**: Add query profiling and optimization
4. **Improve Testing**: Add unit tests with in-memory databases

### Long-term Considerations
1. **Schema Modernization**: Consider breaking migration chain with new schema version
2. **Connection Pooling**: Evaluate multi-connection support for better concurrency
3. **Query Builder**: Implement type-safe query construction
4. **Database Abstraction**: Consider supporting other database engines

## File Locations

### Core Database Files
- `src/Core/FSpot/Database/Db.cs` - Main database implementation
- `src/Core/FSpot/Database/DbStore.cs` - Base store class
- `src/Core/FSpot/Database/PhotoStore.cs` - Photo management
- `src/Core/FSpot/Database/TagStore.cs` - Tag management
- `src/Core/FSpot/Database/Updater.cs` - Migration system

### Connection Layer
- `src/Core/FSpot/Database/FSpotDatabaseConnection.cs` - SQLite connection wrapper

### Test Data
- `tests/data/` - Test databases for different F-Spot versions