# Migration System Analysis

## Overview

F-Spot implements a comprehensive database migration system that has successfully evolved the schema through 18 major versions while maintaining backward compatibility and data integrity. The system handles complex schema transformations, data migrations, and incremental updates.

## Migration Architecture

### Core Components

- **Updater Class**: Central migration coordinator in `src/Core/FSpot/Database/Updater.cs`
- **Version Management**: Sequential versioning from 1.0 to 18.0 (current)
- **Transaction Safety**: All migrations wrapped in transactions
- **Backup Strategy**: Temporary table creation before destructive changes
- **Progress Tracking**: UI feedback for long-running migrations

### Version Storage

The current database version is stored in the `meta` table:
```sql
INSERT INTO meta (name, data) VALUES ("F-Spot Database Version", "18.0");
```

## Migration History

### Major Schema Evolution Milestones

#### v1.0 → v5.0: Early Schema Stabilization
- **v2.0**: Basic photo and tag table structure
- **v3.0**: Added photo descriptions
- **v4.0**: Introduced photo versions concept
- **v5.0**: Added `roll_id` to photos, renamed imports → rolls

#### v6.0 → v10.0: URI and Path Management
- **v7.0**: Changed from directory paths to URIs
- **v8.0**: Stored full version URIs
- **v9.0**: Tag system improvements
- **v10.0**: Made photo IDs auto-increment

#### v11.0 → v15.0: Feature Expansion
- **v11.0**: Added rating system (1-5 stars)
- **v12.0**: Enhanced tag categories
- **v13.0**: Improved version management
- **v14.0**: Export tracking system
- **v15.0**: Job queue system

#### v16.0 → v18.0: Modern Optimizations
- **v16.0**: Added MD5 sum support for duplicate detection
- **v17.0**: Split URIs into `base_uri` + `filename` for better performance
- **v18.0**: Renamed `md5_sum` → `import_md5` for clarity

## Migration Patterns

### Safe Schema Changes

#### 1. Table Backup Pattern
```csharp
// Move existing table to temporary backup
ExecuteNonQuery("ALTER TABLE photos RENAME TO photos_backup");

// Create new table with updated schema
ExecuteNonQuery(CreatePhotosTable());

// Migrate data with transformations
ExecuteNonQuery(@"INSERT INTO photos 
    SELECT id, time, directory_path, name, description, roll_id, default_version_id 
    FROM photos_backup");

// Drop backup table
ExecuteNonQuery("DROP TABLE photos_backup");
```

#### 2. Column Addition Pattern
```csharp
// Simple column additions
ExecuteNonQuery("ALTER TABLE photos ADD COLUMN rating INTEGER");
```

#### 3. Index Management Pattern
```csharp
// Add performance indexes
ExecuteNonQuery("CREATE INDEX idx_photo_versions_id ON photo_versions(photo_id)");
ExecuteNonQuery("CREATE INDEX idx_photos_roll_id ON photos(roll_id)");
```

### Data Transformation Patterns

#### URI Conversion (v7.0)
```csharp
// Convert file paths to proper URIs
foreach (DataRow row in Select("SELECT id, directory_path, name FROM photos"))
{
    string path = Path.Combine(row["directory_path"], row["name"]);
    string uri = UriUtils.PathToFileUri(path);
    ExecuteNonQuery("UPDATE photos SET uri = ? WHERE id = ?", uri, row["id"]);
}
```

#### Path Splitting (v17.0)
```csharp
// Split full URIs into base_uri + filename
foreach (DataRow row in Select("SELECT id, uri FROM photos"))
{
    Uri uri = new Uri(row["uri"]);
    string baseUri = uri.GetLeftPart(UriPartial.Path).TrimEnd(Path.DirectorySeparatorChar);
    string filename = Path.GetFileName(uri.LocalPath);
    
    ExecuteNonQuery("UPDATE photos SET base_uri = ?, filename = ? WHERE id = ?", 
                   baseUri, filename, row["id"]);
}
```

## Migration Safety Features

### Transaction Management
- **Atomic Updates**: Each migration version runs in a single transaction
- **Rollback Support**: Automatic rollback on any failure
- **Nested Transactions**: Support for complex multi-step migrations

### Backup and Recovery
- **Temporary Tables**: `MoveTableToTemp()` creates safety backups
- **Schema Validation**: Post-migration integrity checks
- **Error Logging**: Comprehensive error tracking and reporting

### Progress Feedback
- **UI Integration**: Progress bars for long-running migrations
- **Slow Operation Detection**: User feedback for operations > 5 seconds
- **Cancellation Support**: User can abort long migrations

## Performance Considerations

### Migration Speed Optimization
- **Bulk Operations**: Use batch inserts/updates where possible
- **Index Timing**: Drop indexes before bulk changes, recreate after
- **PRAGMA Settings**: Optimize SQLite settings during migration
- **Memory Management**: Stream large datasets to avoid memory pressure

### Backwards Compatibility
- **Read-Only Access**: Newer versions can read older databases
- **Graceful Degradation**: Missing features don't break core functionality
- **Version Checking**: Prevent downgrades that could cause data loss

## Testing Strategy

### Test Database Versions
The `tests/data/` directory contains sample databases for testing migrations:
- Historical versions from different F-Spot releases
- Edge cases and corrupted database scenarios
- Performance test databases with large datasets

### Migration Testing Process
1. **Load Test Database**: Start with known version
2. **Run Migration**: Execute updater to latest version
3. **Validate Schema**: Verify table structure matches expected
4. **Validate Data**: Check data integrity and transformations
5. **Performance Check**: Ensure migration completes in reasonable time

## Current Challenges

### Technical Debt
1. **Migration Chain Length**: 18 versions create complex interdependencies
2. **Legacy Code**: Old migration code must be maintained for compatibility
3. **Test Coverage**: Not all migration paths are thoroughly tested
4. **Error Recovery**: Limited recovery options for failed migrations

### Performance Issues
1. **Large Database Migrations**: Can take hours for databases with 100K+ photos
2. **Index Rebuilding**: Time-consuming index recreation during schema changes
3. **Memory Usage**: Some migrations require loading large datasets into memory
4. **Disk Space**: Temporary tables can double disk space requirements during migration

## Best Practices

### Adding New Migrations
1. **Increment Version**: Update `LatestVersion` constant
2. **Add Migration Method**: Create `UpdateToVersionXX()` method
3. **Use Transactions**: Wrap all changes in transaction
4. **Test Thoroughly**: Test with various database sizes and states
5. **Document Changes**: Update schema documentation

### Migration Code Quality
1. **Idempotent Operations**: Migrations should be safe to run multiple times
2. **Error Handling**: Provide clear error messages and recovery guidance
3. **Performance Awareness**: Consider impact on large databases
4. **Backwards Compatibility**: Don't break ability to read older databases

## Recommendations

### Short-term Improvements
1. **Enhanced Testing**: Add automated migration tests for all version paths
2. **Progress Reporting**: Improve user feedback during long migrations
3. **Error Recovery**: Add partial migration recovery mechanisms
4. **Performance Profiling**: Identify and optimize slow migration operations

### Long-term Considerations
1. **Migration Chain Pruning**: Consider "fast-forward" migrations for very old versions
2. **Parallel Processing**: Implement parallel migration steps where safe
3. **Streaming Migrations**: Reduce memory usage for large dataset transformations
4. **Schema Versioning**: Implement more sophisticated version management

## File Locations

### Core Migration Files
- `src/Core/FSpot/Database/Updater.cs` - Main migration coordinator
- `src/Core/FSpot/Database/Db.cs` - Database initialization and version checking

### Test Resources
- `tests/data/` - Historical test databases
- `tests/FSpot.UnitTest/Database/` - Migration unit tests

### Version Constants
- Latest version defined in `Updater.cs` as `LatestVersion = new Version("18.0")`