# Search and Query System Analysis

## Overview

F-Spot implements a sophisticated search and query system that enables complex photo filtering and organization through a modular query condition framework. The system supports SQL-based queries with composable conditions including tag filtering, date ranges, rating searches, and logical operators, providing powerful search capabilities for large photo collections.

## Architecture Overview

### System Components

```
Search and Query Architecture
├── Query Condition Framework
│   ├── IQueryCondition Interface (SQL clause generation)
│   ├── Logical Terms (Base classes for conditions)
│   ├── Logical Operators (AND, OR, NOT combinations)
│   └── Order Conditions (Sorting and ordering)
├── Search Condition Types
│   ├── Tag-Based Queries (Tag hierarchy and categories)
│   ├── Date Range Queries (Time-based filtering)
│   ├── Rating Queries (Star rating filtering)
│   ├── Text Queries (Description search)
│   └── Special Conditions (Hidden, untagged photos)
├── Query Execution Engine
│   ├── SQL Query Builder (Dynamic query construction)
│   ├── Query Optimization (Temporary tables, indexing)
│   ├── Result Caching (Photo object caching)
│   └── Batch Processing (Efficient large result sets)
└── Integration Points
    ├── Photo Store (Primary query interface)
    ├── User Interface (Filter widgets and dialogs)
    ├── Database Layer (SQLite query execution)
    └── Performance Monitoring (Query timing and optimization)
```

## Query Condition Framework

### Core Interface (`src/Core/FSpot/Query/IQueryCondition.cs`)

**Base Query Condition Contract**:
```csharp
public interface IQueryCondition
{
    string SqlClause();  // Generate SQL WHERE clause fragment
}

// Order-specific conditions for sorting
public interface IOrderCondition : IQueryCondition
{
    // Inherits SqlClause() for ORDER BY clauses
}
```

### Logical Term Base Class (`src/Core/FSpot/Query/LogicalTerm.cs`)

**Abstract Foundation for Search Terms**:
```csharp
public abstract class LogicalTerm : IQueryCondition
{
    public abstract string SqlClause();
    
    // Logical terms can be combined with operators
    // Examples: TagTerm, DateRange, RatingRange, TextTerm
}
```

## Search Condition Types

### Tag-Based Queries (`src/Core/FSpot/Query/TagTerm.cs`)

**Hierarchical Tag Filtering**:
```csharp
public class TagTerm : LogicalTerm
{
    public Tag Tag { get; private set; }
    
    public TagTerm(Tag tag)
    {
        Tag = tag;
    }
    
    public override string SqlClause()
    {
        return SqlClause(this);
    }
    
    internal static string SqlClause(params TagTerm[] tags)
    {
        return SqlClause(GetTagIds(tags.Select(t => t.Tag)));
    }
    
    // Recursive tag hierarchy expansion
    static IList<string> GetTagIds(IEnumerable\<Tag\> tags)
    {
        var tagList = new List\<Tag\>();
        foreach (var tag in tags)
        {
            tagList.Add(tag);
            
            // Include child tags in category search
            var category = tag as Category;
            if (category != null)
            {
                category.AddDescendentsTo(tagList);
            }
        }
        return tagList.Select(t => t.Id.ToString()).ToList();
    }
    
    // SQL generation for tag filtering
    static string SqlClause(IList<string> tagids)
    {
        if (tagids.Count == 0) return null;
        
        if (tagids.Count == 1)
        {
            return string.Format(
                " (photos.id IN (SELECT photo_id FROM photo_tags WHERE tag_id = {0})) ",
                tagids[0]);
        }
        
        return string.Format(
            " (photos.id IN (SELECT photo_id FROM photo_tags WHERE tag_id IN ({0}))) ",
            string.Join(", ", tagids));
    }
}
```

### Date Range Queries (`src/Core/FSpot/Query/DateRange.cs`)

**Time-Based Photo Filtering**:
```csharp
public class DateRange : IQueryCondition
{
    public DateTime Start { get; private set; }
    public DateTime End { get; private set; }
    
    // Flexible date range constructors
    public DateRange(DateTime start, DateTime end)
    {
        Start = start;
        End = end;
    }
    
    // Month-based filtering convenience constructor
    public DateRange(int year, int month)
    {
        Start = new DateTime(year, month, 1);
        End = new DateTime(
            month < 12 ? year : year + 1,
            month < 12 ? month + 1 : 1,
            1);
    }
    
    public string SqlClause()
    {
        return string.Format(
            " photos.time >= {0} AND photos.time <= {1} ",
            DateTimeUtil.FromDateTime(Start),
            DateTimeUtil.FromDateTime(End));
    }
}
```

### Rating Range Queries (`src/Core/FSpot/Query/RatingRange.cs`)

**Star Rating Filtering**:
```csharp
public class RatingRange : IQueryCondition
{
    public uint MinRating { get; private set; }
    public uint MaxRating { get; private set; }
    
    public RatingRange(uint min_rating, uint max_rating)
    {
        MinRating = min_rating;
        MaxRating = max_rating;
    }
    
    public string SqlClause()
    {
        return string.Format(
            " (photos.rating >= {0} AND photos.rating <= {1}) ",
            MinRating, MaxRating);
    }
}
```

## Logical Operators

### AND Operator (`src/Core/FSpot/Query/AndOperator.cs`)

**Intersection of Multiple Conditions**:
```csharp
public class AndOperator : NAryOperator
{
    public AndOperator(params LogicalTerm[] terms)
    {
        this.terms = new List<LogicalTerm>(terms.Length);
        foreach (LogicalTerm term in terms)
            Add(term);
    }
    
    // Automatic flattening of nested AND operations
    void Add(LogicalTerm term)
    {
        var andTerm = term as AndOperator;
        if (andTerm != null)
        {
            // Flatten nested AND operators for efficiency
            foreach (LogicalTerm t in andTerm.Terms)
                Add(t);
        }
        else
        {
            terms.Add(term);
        }
    }
    
    public override string SqlClause()
    {
        return SqlClause("AND", ToStringArray());
    }
}
```

### OR Operator (`src/Core/FSpot/Query/OrOperator.cs`)

**Union of Multiple Conditions**:
```csharp
public class OrOperator : NAryOperator
{
    public OrOperator(params LogicalTerm[] terms)
    {
        this.terms = new List<LogicalTerm>(terms.Length);
        foreach (LogicalTerm term in terms)
            Add(term);
    }
    
    public override string SqlClause()
    {
        return SqlClause("OR", ToStringArray());
    }
}
```

### NOT Operator (`src/Core/FSpot/Query/NotTerm.cs`)

**Negation of Conditions**:
```csharp
public class NotTerm : LogicalTerm
{
    LogicalTerm term;
    
    public NotTerm(LogicalTerm term)
    {
        this.term = term;
    }
    
    public override string SqlClause()
    {
        return " NOT (" + term.SqlClause() + ") ";
    }
}
```

## Query Execution Engine

### SQL Query Builder (`src/Core/FSpot/Database/PhotoStore.cs`)

**Dynamic Query Construction**:
```csharp
public static string BuildQuery(params IQueryCondition[] conditions)
{
    var query_builder = new StringBuilder("SELECT * FROM photos ");
    
    bool where_added = false;
    bool hidden_contained = false;
    
    // Process WHERE conditions
    foreach (IQueryCondition condition in conditions)
    {
        if (condition == null) continue;
        
        hidden_contained |= condition is HiddenTag;
        
        if (condition is IOrderCondition) continue;
        
        string sql_clause = condition.SqlClause();
        
        if (sql_clause == null || sql_clause.Trim() == string.Empty)
            continue;
            
        query_builder.Append(where_added ? " AND " : " WHERE ");
        query_builder.Append(sql_clause);
        where_added = true;
    }
    
    // Automatically hide hidden photos unless explicitly requested
    if (!hidden_contained)
    {
        string sql_clause = HiddenTag.HideHiddenTag.SqlClause();
        
        if (sql_clause != null && sql_clause.Trim() != string.Empty)
        {
            query_builder.Append(where_added ? " AND " : " WHERE ");
            query_builder.Append(sql_clause);
        }
    }
    
    // Process ORDER BY conditions
    bool order_added = false;
    foreach (IQueryCondition condition in conditions)
    {
        if (condition == null) continue;
        
        if (!(condition is IOrderCondition)) continue;
        
        string sql_clause = condition.SqlClause();
        
        if (sql_clause == null || sql_clause.Trim() == string.Empty)
            continue;
            
        query_builder.Append(order_added ? " , " : "ORDER BY ");
        query_builder.Append(sql_clause);
        order_added = true;
    }
    
    return query_builder.ToString();
}
```

### Query Execution Methods

**Primary Query Interface**:
```csharp
public Photo[] Query(params IQueryCondition[] conditions)
{
    return Query(BuildQuery(conditions));
}

public Photo[] Query(string query)
{
    return Query(new HyenaSqliteCommand(query));
}

Photo[] Query(HyenaSqliteCommand query)
{
    using var op = Operation.Begin($"PhotoStore.Query {query}");
    var new_photos = new List\<Photo\>();
    var query_result = new List\<Photo\>();
    
    using (var reader = Database.Query(query))
    {
        while (reader.Read())
        {
            uint id = Convert.ToUInt32(reader["id"]);
            Photo photo = LookupInCache(id);
            
            if (photo == null)
            {
                // Create new photo object
                photo = new Photo(imageFileFactory, thumbnailService, id,
                    Convert.ToInt64(reader["time"]));
                photo.Description = reader["description"].ToString();
                photo.RollId = Convert.ToUInt32(reader["roll_id"]);
                photo.DefaultVersionId = Convert.ToUInt32(reader["default_version_id"]);
                photo.Rating = Convert.ToUInt32(reader["rating"]);
                
                new_photos.Add(photo);
                AddToCache(photo);
            }
            
            query_result.Add(photo);
        }
    }
    
    // Batch load tags and versions for efficiency
    if (new_photos.Count > 0)
    {
        string photos_to_load = string.Join(",", new_photos.Select(p => p.Id));
        GetAllTags($"({photos_to_load})");
        GetAllVersions($"({photos_to_load})");
    }
    
    return query_result.ToArray();
}
```

## Query Optimization Features

### Temporary Table Operations

**Efficient Large Result Set Handling**:
```csharp
public void QueryToTemp(string tempTable, params IQueryCondition[] conditions)
{
    QueryToTemp(tempTable, BuildQuery(conditions));
}

public void QueryToTemp(string tempTable, string query)
{
    using var op = Operation.Begin($"QueryToTemp");
    Logger.Log.Debug($"Query Started : {query}");
    
    Database.BeginTransaction();
    Database.Execute($"DROP TABLE IF EXISTS {tempTable}");
    Database.Execute($"CREATE TEMPORARY TABLE {tempTable} AS {query}");
    Database.CommitTransaction();
    
    op.Complete();
}

public Photo[] QueryFromTemp(string tempTable, int offset, int limit)
{
    return Query($"SELECT * FROM {tempTable} LIMIT {limit} OFFSET {offset}");
}
```

### Monthly Photo Statistics

**Aggregated Photo Count Queries**:
```csharp
public Dictionary<int, int[]> PhotosPerMonth(params IQueryCondition[] conditions)
{
    lock (populationTableLock)
    {
        using var op = Operation.Begin($"PhotosPerMonth");
        var val = new Dictionary<int, int[]>();
        
        // SQLite optimization: query to temp table then group
        Database.Execute("DROP TABLE IF EXISTS population");
        var query_builder = new StringBuilder(
            "CREATE TEMPORARY TABLE population AS " +
            "SELECT strftime('%Y%m', datetime(time, 'unixepoch')) AS month " +
            "FROM photos");
            
        bool where_added = false;
        foreach (IQueryCondition condition in conditions)
        {
            if (condition == null) continue;
            if (condition is IOrderCondition) continue;
            
            query_builder.Append(where_added ? " AND " : " WHERE ");
            query_builder.Append(condition.SqlClause());
            where_added = true;
        }
        
        Database.Execute(query_builder.ToString());
        
        int minyear = int.MaxValue;
        int maxyear = int.MinValue;
        
        using (var reader = Database.Query(
            "SELECT COUNT(*) as count, month " +
            "FROM population GROUP BY month"))
        {
            while (reader.Read())
            {
                string yyyymm = reader["month"].ToString();
                int count = Convert.ToInt32(reader["count"]);
                int year = Convert.ToInt32(yyyymm.Substring(0, 4));
                int month = Convert.ToInt32(yyyymm.Substring(4));
                
                maxyear = Math.Max(year, maxyear);
                minyear = Math.Min(year, minyear);
                
                if (!val.ContainsKey(year))
                    val.Add(year, new int[12]);
                val[year][month - 1] = count;
            }
        }
        
        // Fill in missing years with zero counts
        for (int i = minyear; i <= maxyear; i++)
            if (!val.ContainsKey(i))
                val.Add(i, new int[12]);
                
        return val;
    }
}
```

## Special Query Conditions

### Hidden Photo Filtering (`src/Core/FSpot/Query/HiddenTag.cs`)

**Automatic Hidden Photo Exclusion**:
```csharp
public class HiddenTag : IQueryCondition
{
    public static readonly HiddenTag HideHiddenTag = new HiddenTag();
    
    public string SqlClause()
    {
        // Exclude photos tagged with special hidden tag
        return " photos.id NOT IN " +
               " (SELECT photo_id FROM photo_tags " +
               "  WHERE tag_id = (SELECT id FROM tags WHERE name LIKE '%Hidden%')) ";
    }
}
```

### Untagged Photo Queries (`src/Core/FSpot/Query/UntaggedCondition.cs`)

**Photos Without Any Tags**:
```csharp
public class UntaggedCondition : IQueryCondition
{
    public string SqlClause()
    {
        return " photos.id NOT IN (SELECT photo_id FROM photo_tags) ";
    }
}
```

## Performance Considerations

### Query Optimization Strategies

**Efficient Database Operations**:
1. **Index Usage**: Proper indexes on `photo_tags.photo_id`, `photo_tags.tag_id`, `photos.time`
2. **Batch Loading**: Load tags and versions in batches to reduce query overhead
3. **Temporary Tables**: Use temp tables for complex aggregations
4. **Cache Management**: Photo object caching reduces redundant database queries

**Example Query Optimization**:
```csharp
// Instead of individual tag queries per photo
foreach (Photo photo in photos)
{
    GetTags(photo);  // Individual query per photo
}

// Batch load all tags at once
string photo_ids = string.Join(",", photos.Select(p => p.Id));
GetAllTags($"({photo_ids})");  // Single query for all photos
```

## Usage Examples

### Complex Search Queries

**Multi-Condition Search**:
```csharp
// Find photos from 2023 tagged with "Vacation" or "Travel", rated 4+ stars
var dateRange = new DateRange(new DateTime(2023, 1, 1), new DateTime(2023, 12, 31));
var vacationTag = new TagTerm(GetTagByName("Vacation"));
var travelTag = new TagTerm(GetTagByName("Travel"));
var highRating = new RatingRange(4, 5);

var tagFilter = new OrOperator(vacationTag, travelTag);
var fullQuery = new AndOperator(dateRange, tagFilter, highRating);

Photo[] results = photoStore.Query(fullQuery);
```

**Text and Tag Combination**:
```csharp
// Find photos with "sunset" in description, taken in summer months
var textTerm = new TextTerm("sunset");
var summerMonths = new OrOperator(
    new DateRange(2023, 6),   // June
    new DateRange(2023, 7),   // July  
    new DateRange(2023, 8)    // August
);

Photo[] sunsetPhotos = photoStore.Query(
    new AndOperator(textTerm, summerMonths)
);
```

## Limitations and Modernization Opportunities

### Current Limitations

1. **No Full-Text Search**: Limited to simple text matching in descriptions
2. **No Fuzzy Search**: Exact match requirements for text searches
3. **Limited Aggregation**: Basic statistical queries only
4. **No Geographic Search**: No location-based query support

### Modernization Recommendations

**Enhanced Search Capabilities**:
```csharp
// Full-text search with ranking
public interface IFullTextCondition : IQueryCondition
{
    float RelevanceScore { get; }
    string[] SearchTerms { get; }
}

public class FullTextTerm : IFullTextCondition
{
    public string SearchQuery { get; private set; }
    public float RelevanceScore => CalculateRelevance();
    
    public string SqlClause()
    {
        // Use SQLite FTS5 for full-text search
        return $" photos.id IN (SELECT rowid FROM photos_fts WHERE photos_fts MATCH '{SearchQuery}') ";
    }
}

// Geographic search support
public class LocationRadius : IQueryCondition
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double RadiusKm { get; set; }
    
    public string SqlClause()
    {
        // Use spatial indexing for location-based queries
        return $" ST_Distance_Sphere(POINT(longitude, latitude), POINT({Longitude}, {Latitude})) <= {RadiusKm * 1000} ";
    }
}
```

**Async Query Support**:
```csharp
public async Task<Photo[]> QueryAsync(params IQueryCondition[] conditions)
{
    return await Task.Run(() => Query(conditions));
}

public async Task<Photo[]> QueryStreamAsync(IQueryCondition[] conditions, 
                                           IProgress<Photo[]> progress, 
                                           CancellationToken cancellationToken)
{
    // Stream results for large queries
    var results = new List\<Photo\>();
    int batchSize = 100;
    int offset = 0;
    
    while (!cancellationToken.IsCancellationRequested)
    {
        var batch = await QueryBatchAsync(conditions, offset, batchSize);
        if (batch.Length == 0) break;
        
        results.AddRange(batch);
        progress?.Report(batch);
        offset += batchSize;
    }
    
    return results.ToArray();
}
```

## Summary

F-Spot's search and query system provides a flexible and powerful framework for photo organization and retrieval through composable query conditions, logical operators, and efficient SQL generation. The system supports complex multi-criteria searches while maintaining good performance through caching, batching, and query optimization. Modern enhancements could include full-text search, geographic queries, and async operations to further improve the search experience.