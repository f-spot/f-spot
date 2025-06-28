# Database Layer Overview

F-Spot's database layer provides robust data persistence using SQLite with a clean repository pattern implementation and comprehensive migration system.

## Database Architecture

### Core Data Stores
- **PhotoStore**: Photo metadata, file paths, and relationships
- **TagStore**: Hierarchical tagging system with category support
- **RollStore**: Import batches and roll management
- **JobStore**: Background job processing and status tracking
- **MetaStore**: Application metadata and configuration
- **ExportStore**: Export history and settings

### Schema Design
```sql
-- Core photo table
CREATE TABLE photos (
    id INTEGER PRIMARY KEY,
    time INTEGER NOT NULL,
    uri STRING NOT NULL,
    description TEXT,
    roll_id INTEGER,
    default_version_id INTEGER,
    rating INTEGER
);

-- Hierarchical tagging
CREATE TABLE tags (
    id INTEGER PRIMARY KEY,
    name STRING UNIQUE,
    category_id INTEGER,
    is_category BOOLEAN,
    sort_priority INTEGER
);

-- Photo-tag relationships
CREATE TABLE photo_tags (
    photo_id INTEGER,
    tag_id INTEGER,
    PRIMARY KEY (photo_id, tag_id)
);
```

## Key Features

### Repository Pattern Implementation
- **Abstraction Layer**: Clean separation between business logic and data access
- **Consistent API**: Uniform interface across all data stores
- **Transaction Support**: Atomic operations and rollback capabilities
- **Change Tracking**: Audit trail for data modifications

### Migration System
- **Version Management**: Automated database schema upgrades
- **Backward Compatibility**: Safe migration between F-Spot versions
- **Data Integrity**: Validation and constraint enforcement
- **Rollback Support**: Safe downgrade paths when possible

### Performance Optimization
- **Indexing Strategy**: Optimized indexes for common query patterns
- **Query Optimization**: Efficient SQL generation and caching
- **Connection Pooling**: Managed database connections
- **Bulk Operations**: Batch processing for large data sets

## Technical Implementation

### Database Technology
- **SQLite**: Embedded database for local storage
- **Hyena.Data**: Data access framework (from Banshee)
- **SQL Generation**: Dynamic query building with parameterization
- **Transaction Management**: ACID compliance and isolation

### Data Access Patterns
```csharp
// Repository pattern example
public class PhotoStore : DbStore<Photo> {
    public Photo[] GetPhotosWithTag(Tag tag) {
        return Query("SELECT * FROM photos p " +
                    "JOIN photo_tags pt ON p.id = pt.photo_id " +
                    "WHERE pt.tag_id = ?", tag.Id);
    }
}
```

## Related Documentation

### Detailed Analysis
- [**Database Layer Analysis**](database-layer-analysis) - Complete technical analysis
- [**Database Performance**](database-performance) - Performance characteristics
- [**Migration System**](migration-system-analysis) - Schema migration details

### Related Systems
- [**Core Business Logic**](/core-architecture/core-business-logic-analysis) - Domain model integration
- [**Search & Query System**](/search-query/search-and-query-system-analysis) - Query implementation
- [**Performance Analysis**](/performance/) - Database performance optimization

---

The database layer provides a solid foundation for F-Spot's data management needs with room for modern enhancements and performance optimizations.