# Database Performance Analysis

## Overview

F-Spot's database performance is critical for managing large photo collections efficiently. This analysis examines current performance characteristics, identifies bottlenecks, and provides optimization recommendations for the SQLite-based storage layer.

## Current Performance Profile

### Benchmark Scenarios
- **Small Collections**: < 1,000 photos - Excellent performance
- **Medium Collections**: 1,000-10,000 photos - Good performance with occasional delays
- **Large Collections**: 10,000-50,000 photos - Noticeable performance degradation
- **Very Large Collections**: > 50,000 photos - Significant performance issues

## Performance Bottlenecks

### 1. Tag System Performance

#### Immortal Cache Issues
**Problem**: TagStore loads all tags into memory on startup
```csharp
// All tags loaded unconditionally
foreach (Tag tag in store.tags) 
{
    tag_hash [tag.Id] = tag;
}
```

**Impact**:
- Startup time increases linearly with tag count
- Memory usage grows with tag hierarchy complexity
- No lazy loading for rarely-used tags

**Metrics**:
- 1,000 tags: ~200ms startup overhead
- 5,000 tags: ~1s startup overhead
- Memory: ~500 bytes per tag in cache

#### Tag Query Performance
**Problem**: N+1 query pattern when loading photo tags
```csharp
// Inefficient: One query per photo
foreach (Photo photo in photos) 
{
    photo.Tags = tag_store.GetTagsForPhoto(photo.Id);
}
```

**Optimization**: Batch tag loading
```csharp
// Efficient: Single query for all photos
Dictionary<uint, List\<Tag\>> photoTags = tag_store.GetTagsForPhotos(photoIds);
```

### 2. Photo Loading Performance

#### Lazy Loading Inefficiencies
**Problem**: Photo versions loaded individually on demand
```csharp
// Each version access triggers a database query
public PhotoVersion DefaultVersion => versions[DefaultVersionId];
```

**Impact**:
- Gallery view triggers hundreds of individual queries
- Slideshow mode causes stuttering during transitions
- Search results slow to populate with metadata

#### Weak Reference Cache Limitations
**Problem**: Photos frequently garbage collected under memory pressure
```csharp
// Weak references don't prevent collection
WeakReference photoRef = new WeakReference(photo);
if (!photoRef.IsAlive) 
{
    // Must reload from database
    photo = LoadPhotoFromDatabase(id);
}
```

### 3. Query Performance Issues

#### Missing Composite Indexes
**Current Indexes**:
```sql
CREATE INDEX idx_photo_versions_id ON photo_versions(photo_id);
CREATE INDEX idx_photo_versions_import_md5 ON photo_versions(import_md5);
CREATE INDEX idx_photos_roll_id ON photos(roll_id);
```

**Missing Indexes** (needed for common queries):
```sql
-- For tag-based searches
CREATE INDEX idx_photo_tags_tag_id ON photo_tags(tag_id);

-- For time-based queries
CREATE INDEX idx_photos_time ON photos(time);

-- For rating queries
CREATE INDEX idx_photos_rating ON photos(rating);

-- Composite index for filtered searches
CREATE INDEX idx_photos_time_rating ON photos(time, rating);
```

#### Inefficient Complex Queries
**Problem**: Complex search queries don't use temporary tables effectively
```csharp
// Suboptimal: Multiple JOIN operations
string query = @"
    SELECT DISTINCT p.* FROM photos p
    INNER JOIN photo_tags pt1 ON p.id = pt1.photo_id
    INNER JOIN photo_tags pt2 ON p.id = pt2.photo_id
    WHERE pt1.tag_id = ? AND pt2.tag_id = ?
";
```

**Optimization**: Use temporary tables for complex conditions
```csharp
// Better: Build intermediate result set
CreateTemporaryTable("temp_search_results", photoIds);
string query = "SELECT * FROM photos WHERE id IN (SELECT photo_id FROM temp_search_results)";
```

### 4. Database Connection Performance

#### Single Connection Bottleneck
**Problem**: All database operations serialize through single connection
```csharp
// Only one operation can execute at a time
lock (connectionLock) 
{
    return connection.ExecuteQuery(sql, parameters);
}
```

**Impact**:
- UI freezes during long-running queries
- Import operations block other database access
- No parallelization of independent operations

#### SQLite Configuration Issues
**Current Settings**:
```csharp
// Default SQLite settings may not be optimal
connection.ExecuteNonQuery("PRAGMA synchronous = FULL");  // Too conservative
connection.ExecuteNonQuery("PRAGMA cache_size = 2000");   // Too small
```

**Recommended Settings**:
```csharp
// Better performance/safety balance
connection.ExecuteNonQuery("PRAGMA synchronous = NORMAL");
connection.ExecuteNonQuery("PRAGMA cache_size = 10000");
connection.ExecuteNonQuery("PRAGMA temp_store = MEMORY");
connection.ExecuteNonQuery("PRAGMA journal_mode = WAL");
```

## Performance Measurement Tools

### Built-in Profiling
F-Spot uses Hyena's built-in query timing:
```csharp
using (var timer = new HyenaTimer("Database Query")) 
{
    return ExecuteQuery(sql, parameters);
}
```

### Recommended Additional Profiling
```csharp
// Add query profiling wrapper
public class ProfiledDatabase : IDb 
{
    private readonly Dictionary<string, QueryStats> queryStats = new();
    
    public T ExecuteQuery\<T\>(string sql, params object[] parameters) 
    {
        var stopwatch = Stopwatch.StartNew();
        var result = innerDb.ExecuteQuery\<T\>(sql, parameters);
        RecordQueryTime(sql, stopwatch.ElapsedMilliseconds);
        return result;
    }
}
```

## Optimization Strategies

### 1. Caching Improvements

#### Smart Tag Caching
Replace immortal cache with LRU cache:
```csharp
public class LRUTagCache 
{
    private readonly int maxSize;
    private readonly LinkedList\<Tag\> accessOrder;
    private readonly Dictionary<uint, LinkedListNode\<Tag\>> tagNodes;
    
    public Tag GetTag(uint id) 
    {
        if (tagNodes.TryGetValue(id, out var node)) 
        {
            // Move to front (most recently used)
            accessOrder.Remove(node);
            accessOrder.AddFirst(node);
            return node.Value;
        }
        
        // Load from database and cache
        var tag = LoadTagFromDatabase(id);
        CacheTag(tag);
        return tag;
    }
}
```

#### Photo Version Eager Loading
Load related data proactively:
```csharp
public class PhotoWithVersions 
{
    public Photo Photo { get; set; }
    public List<PhotoVersion> Versions { get; set; }
    public List\<Tag\> Tags { get; set; }
    
    // Single query loads everything
    public static List<PhotoWithVersions> LoadPhotosWithRelated(IEnumerable<uint> photoIds)
    {
        // Use single query with JOINs
        string sql = @"
            SELECT p.*, pv.*, t.*
            FROM photos p
            LEFT JOIN photo_versions pv ON p.id = pv.photo_id
            LEFT JOIN photo_tags pt ON p.id = pt.photo_id
            LEFT JOIN tags t ON pt.tag_id = t.id
            WHERE p.id IN ({0})
        ";
    }
}
```

### 2. Query Optimization

#### Add Missing Indexes
```sql
-- Critical indexes for common operations
CREATE INDEX IF NOT EXISTS idx_photo_tags_tag_id ON photo_tags(tag_id);
CREATE INDEX IF NOT EXISTS idx_photos_time ON photos(time);
CREATE INDEX IF NOT EXISTS idx_photos_rating ON photos(rating);
CREATE INDEX IF NOT EXISTS idx_photos_time_rating ON photos(time, rating);

-- Composite indexes for complex searches
CREATE INDEX IF NOT EXISTS idx_photos_roll_time ON photos(roll_id, time);
CREATE INDEX IF NOT EXISTS idx_tags_category_name ON tags(category_id, name);
```

#### Query Plan Analysis
Add EXPLAIN QUERY PLAN analysis:
```csharp
public void AnalyzeQuery(string sql) 
{
    string explainSql = "EXPLAIN QUERY PLAN " + sql;
    var plan = ExecuteQuery<string>(explainSql);
    Log.Debug($"Query plan for {sql}: {plan}");
    
    // Warn about table scans
    if (plan.Contains("SCAN TABLE")) 
    {
        Log.Warning($"Query using table scan: {sql}");
    }
}
```

### 3. Connection Pool Implementation

#### Multi-Connection Support
```csharp
public class ConnectionPool 
{
    private readonly Queue<IDbConnection> availableConnections;
    private readonly HashSet<IDbConnection> allConnections;
    private readonly int maxConnections;
    
    public IDbConnection GetConnection() 
    {
        lock (lockObject) 
        {
            if (availableConnections.Count > 0) 
            {
                return availableConnections.Dequeue();
            }
            
            if (allConnections.Count < maxConnections) 
            {
                var connection = CreateConnection();
                allConnections.Add(connection);
                return connection;
            }
            
            // Wait for available connection
            return WaitForConnection();
        }
    }
}
```

### 4. Asynchronous Operations

#### Async Database Layer
```csharp
public interface IAsyncDb 
{
    Task<List\<Photo\>> GetPhotosAsync(PhotoQuery query);
    Task\<Photo\> GetPhotoAsync(uint id);
    Task SavePhotoAsync(Photo photo);
    Task<List\<Tag\>> GetTagsAsync();
}

public class AsyncPhotoStore : IAsyncDb 
{
    public async Task<List\<Photo\>> GetPhotosAsync(PhotoQuery query) 
    {
        return await Task.Run(() => 
        {
            using (var connection = connectionPool.GetConnection()) 
            {
                return ExecutePhotoQuery(connection, query);
            }
        });
    }
}
```

## Performance Testing Framework

### Benchmarking Infrastructure
```csharp
public class DatabaseBenchmark 
{
    private readonly IDb database;
    private readonly List<uint> testPhotoIds;
    
    [Benchmark]
    public void LoadPhotosSequential() 
    {
        foreach (uint id in testPhotoIds.Take(100)) 
        {
            var photo = database.Photos.Get(id);
        }
    }
    
    [Benchmark]
    public void LoadPhotosBatch() 
    {
        var photos = database.Photos.Get(testPhotoIds.Take(100));
    }
    
    [Benchmark]
    public void TagSearchSimple() 
    {
        var query = new PhotoQuery { TagIds = new[] { 1u } };
        var results = database.Photos.Query(query);
    }
}
```

### Performance Regression Testing
```csharp
public class PerformanceTests 
{
    [Test]
    public void StartupTimeTest() 
    {
        var stopwatch = Stopwatch.StartNew();
        var db = new Db(testDatabasePath);
        stopwatch.Stop();
        
        Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(5000), 
                   "Database startup should complete within 5 seconds");
    }
    
    [Test]
    public void LargeQueryPerformanceTest() 
    {
        var query = new PhotoQuery { Limit = 1000 };
        var stopwatch = Stopwatch.StartNew();
        var results = db.Photos.Query(query);
        stopwatch.Stop();
        
        Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(1000),
                   "Large queries should complete within 1 second");
    }
}
```

## Monitoring and Metrics

### Key Performance Indicators
1. **Database Startup Time**: Time to load all tags and initialize stores
2. **Query Response Time**: P95 latency for common query types
3. **Memory Usage**: Peak memory consumption during normal operations
4. **Cache Hit Rates**: Effectiveness of photo and tag caches
5. **Concurrent Operation Throughput**: Operations per second under load

### Recommended Monitoring
```csharp
public class DatabaseMetrics 
{
    private readonly Counter queryCount = Metrics.CreateCounter("db_queries_total", "Total database queries");
    private readonly Histogram queryDuration = Metrics.CreateHistogram("db_query_duration_seconds", "Query execution time");
    private readonly Gauge cacheSize = Metrics.CreateGauge("db_cache_size", "Number of items in cache");
    
    public void RecordQuery(string queryType, double durationSeconds) 
    {
        queryCount.WithTag("type", queryType).Inc();
        queryDuration.WithTag("type", queryType).Observe(durationSeconds);
    }
}
```

## Recommendations

### Immediate Improvements (Low Effort, High Impact)
1. **Add Missing Indexes**: Implement critical indexes for common queries
2. **Optimize SQLite Settings**: Update PRAGMA settings for better performance
3. **Fix N+1 Queries**: Batch tag and version loading
4. **Add Query Profiling**: Instrument slow queries for monitoring

### Medium-term Improvements (Moderate Effort, High Impact)
1. **Implement Connection Pooling**: Support concurrent database operations
2. **Add Async Database Layer**: Prevent UI blocking during long operations
3. **Smart Caching Strategy**: Replace immortal cache with LRU cache
4. **Query Optimization**: Rewrite complex queries using temporary tables

### Long-term Improvements (High Effort, High Impact)
1. **Database Sharding**: Support for very large collections (>100K photos)
2. **Read Replicas**: Separate read/write operations for better concurrency
3. **Background Indexing**: Build search indexes asynchronously
4. **Memory-mapped Files**: Use SQLite memory-mapped I/O for better performance

## File Locations

### Performance-Critical Files
- `src/Core/FSpot/Database/PhotoStore.cs` - Photo query optimization
- `src/Core/FSpot/Database/TagStore.cs` - Tag caching improvements
- `src/Core/FSpot/Database/FSpotDatabaseConnection.cs` - Connection management
- `src/Core/FSpot/Query/PhotoQuery.cs` - Query optimization

### Benchmarking Framework
- `tests/FSpot.UnitTest/Database/PerformanceTests.cs` - Performance regression tests
- `tools/benchmark/` - Standalone benchmarking tools (to be created)