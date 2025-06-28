# Testing Strategy

## Overview

F-Spot's testing strategy requires comprehensive modernization to support the application's revival and ensure long-term maintainability. This document outlines current testing gaps, security testing requirements, and a roadmap for implementing a robust testing framework that supports both legacy code maintenance and modern development practices.

## Current Testing State Analysis

### Existing Test Infrastructure

**Test Projects**:
```
tests/
├── FSpot.UnitTest/           # Core business logic tests
├── FSpot.Gtk.UnitTest/       # UI component tests  
├── Hyena.UnitTest/           # Framework library tests
└── data/                     # Test databases and sample files
```

**Testing Frameworks**:
- **NUnit 3.13.3**: Primary test framework
- **Moq 4.17.2**: Mocking framework
- **Shouldly 4.0.3**: Assertion library
- **Microsoft.NET.Test.Sdk**: Test hosting

### Coverage Analysis

**Current Test Coverage**:
```
Overall Coverage: ~28%
├── Core Domain Logic: ~40%
├── Database Layer: ~30%
├── UI Components: ~15%
├── Import System: ~20%
├── Plugin System: ~10%
├── Security Tests: ~0%
└── Integration Tests: ~5%

Critical Gaps:
├── Security vulnerability testing
├── Performance regression testing
├── Cross-platform compatibility testing
├── Plugin system testing
└── Error handling edge cases
```

**Test Quality Issues**:
- Many tests only verify happy path scenarios
- Limited edge case and error condition testing
- No security-focused testing
- Insufficient integration testing
- Missing performance benchmarks

## Testing Strategy Framework

### 1. Unit Testing Strategy

#### Core Domain Testing

**Photo Entity Testing**:
```csharp
[TestFixture]
public class PhotoTests
{
    [Test]
    public void Photo_Constructor_InitializesCorrectly()
    {
        var uri = new SafeUri("file:///test/photo.jpg");
        var photo = new Photo(1, uri);
        
        photo.Id.Should().Be(1);
        photo.DefaultVersion.Uri.Should().Be(uri);
        photo.Tags.Should().BeEmpty();
    }
    
    [Test]
    public void Photo_AddTag_UpdatesTagCollection()
    {
        var photo = CreateTestPhoto();
        var tag = CreateTestTag("landscape");
        
        photo.AddTag(tag);
        
        photo.Tags.Should().Contain(tag);
        photo.HasTag(tag).Should().BeTrue();
    }
    
    [Test]
    [TestCase(null)]
    [TestCase("")]
    public void Photo_SetDescription_InvalidInput_ThrowsException(string description)
    {
        var photo = CreateTestPhoto();
        
        Action act = () => photo.Description = description;
        
        act.Should().Throw<ArgumentException>();
    }
}
```

#### Database Layer Testing

**Repository Pattern Testing**:
```csharp
[TestFixture]
public class PhotoStoreTests
{
    private PhotoStore photoStore;
    private IDb mockDatabase;
    
    [SetUp]
    public void SetUp()
    {
        mockDatabase = CreateInMemoryDatabase();
        photoStore = new PhotoStore(mockDatabase);
    }
    
    [Test]
    public void Add_ValidPhoto_InsertsToDatabase()
    {
        var photo = CreateTestPhoto();
        
        photoStore.Add(photo);
        
        var retrieved = photoStore.Get(photo.Id);
        retrieved.Should().BeEquivalentTo(photo);
    }
    
    [Test]
    public void Query_WithConditions_ReturnsMatchingPhotos()
    {
        var photos = CreateTestPhotos(10);
        photos.ForEach(p => photoStore.Add(p));
        
        var query = new PhotoQuery { Tags = new[] { "landscape" } };
        var results = photoStore.Query(query);
        
        results.Should().OnlyContain(p => p.HasTag("landscape"));
    }
    
    [Test]
    public void Remove_ExistingPhoto_DeletesFromDatabase()
    {
        var photo = CreateTestPhoto();
        photoStore.Add(photo);
        
        photoStore.Remove(photo);
        
        var retrieved = photoStore.Get(photo.Id);
        retrieved.Should().BeNull();
    }
}
```

### 2. Integration Testing Strategy

#### Database Integration Testing

**Database Migration Testing**:
```csharp
[TestFixture]
public class DatabaseMigrationTests
{
    [Test]
    [TestCase("tests/data/f-spot-v16.db", "16.0")]
    [TestCase("tests/data/f-spot-v17.db", "17.0")]
    public void DatabaseMigration_FromVersion_UpdatesToLatest(string dbPath, string fromVersion)
    {
        var testDb = CopyTestDatabase(dbPath);
        
        var db = new Db(testDb);
        var updater = new Updater(db);
        
        updater.Run();
        
        var currentVersion = db.Meta.DatabaseVersion;
        currentVersion.Should().Be(Updater.LatestVersion);
    }
    
    [Test]
    public void DatabaseMigration_PreservesExistingData()
    {
        var testDb = CopyTestDatabase("tests/data/f-spot-v16.db");
        var originalPhotos = CountPhotosInDatabase(testDb);
        var originalTags = CountTagsInDatabase(testDb);
        
        var db = new Db(testDb);
        new Updater(db).Run();
        
        var migratedPhotos = db.Photos.TotalCount;
        var migratedTags = db.Tags.TotalCount;
        
        migratedPhotos.Should().Be(originalPhotos);
        migratedTags.Should().Be(originalTags);
    }
}
```

#### Import System Integration Testing

```csharp
[TestFixture]
public class ImportIntegrationTests
{
    [Test]
    public async Task ImportPhotos_ValidFiles_AddsToDatabase()
    {
        var testFiles = CreateTestImageFiles(5);
        var importController = new ImportController(database);
        
        var result = await importController.ImportPhotosAsync(testFiles);
        
        result.Success.Should().BeTrue();
        result.ImportedCount.Should().Be(5);
        database.Photos.TotalCount.Should().Be(5);
    }
    
    [Test]
    public void ImportPhotos_DuplicateFiles_HandlesCorrectly()
    {
        var testFile = CreateTestImageFile("duplicate.jpg");
        var importController = new ImportController(database);
        
        // Import same file twice
        importController.ImportPhoto(testFile);
        var result = importController.ImportPhoto(testFile);
        
        result.Status.Should().Be(ImportStatus.Duplicate);
        database.Photos.TotalCount.Should().Be(1);
    }
}
```

### 3. Security Testing Strategy

#### SQL Injection Testing

```csharp
[TestFixture]
public class SecurityTests
{
    [Test]
    [TestCase("'; DROP TABLE photos; --")]
    [TestCase("' OR '1'='1")]
    [TestCase("'; UPDATE photos SET description='hacked'; --")]
    public void PhotoStore_Query_PreventsSqlInjection(string maliciousInput)
    {
        var store = new PhotoStore(database);
        
        // Should not throw or execute malicious SQL
        Action act = () => store.QueryByDescription(maliciousInput);
        
        act.Should().NotThrow();
        // Verify tables still exist
        database.TableExists("photos").Should().BeTrue();
    }
    
    [Test]
    public void TagStore_Search_PreventsSqlInjection()
    {
        var maliciousTag = "test'; DELETE FROM tags; --";
        var store = new TagStore(database);
        
        var results = store.FindByName(maliciousTag);
        
        results.Should().BeEmpty();
        database.Tags.TotalCount.Should().BeGreaterThan(0);
    }
}
```

#### Path Traversal Testing

```csharp
[TestFixture]
public class PathTraversalTests
{
    [Test]
    [TestCase("../../../etc/passwd")]
    [TestCase("..\\..\\..\\windows\\system32\\config\\sam")]
    [TestCase("/etc/shadow")]
    public void SafeUri_Constructor_RejectsPathTraversal(string maliciousPath)
    {
        Action act = () => new SafeUri(maliciousPath);
        
        act.Should().Throw<SecurityException>()
           .WithMessage("*path traversal*");
    }
    
    [Test]
    public void ImportController_ImportPhoto_ValidatesPath()
    {
        var maliciousPath = CreateMaliciousFilePath();
        var controller = new ImportController(database);
        
        Action act = () => controller.ImportPhoto(maliciousPath);
        
        act.Should().Throw<SecurityException>();
    }
}
```

#### Authentication Security Testing

```csharp
[TestFixture]
public class AuthenticationSecurityTests
{
    [Test]
    public void FlickrAccount_Save_EncryptsCredentials()
    {
        var account = new FlickrAccount("test", "password123");
        var xmlOutput = new StringWriter();
        
        account.Save(new XmlTextWriter(xmlOutput));
        
        var xml = xmlOutput.ToString();
        xml.Should().NotContain("password123"); // Should be encrypted
    }
    
    [Test]
    public void OAuthToken_Storage_IsSecure()
    {
        var token = new OAuthToken("secret_token");
        var storage = new SecureTokenStorage();
        
        storage.StoreToken(token);
        var configFile = File.ReadAllText(storage.ConfigPath);
        
        configFile.Should().NotContain("secret_token"); // Should be encrypted
    }
}
```

### 4. Performance Testing Strategy

#### Performance Benchmark Testing

```csharp
[TestFixture]
public class PerformanceTests
{
    [Test]
    [Timeout(5000)] // 5 seconds max
    public void PhotoStore_Query_LargeDatabase_PerformsWithinLimit()
    {
        var store = CreateLargeDatabaseStore(10000); // 10K photos
        var query = new PhotoQuery { Tags = new[] { "landscape" } };
        
        var stopwatch = Stopwatch.StartNew();
        var results = store.Query(query);
        stopwatch.Stop();
        
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000); // < 1 second
        results.Length.Should().BeGreaterThan(0);
    }
    
    [Test]
    public void ImageLoader_LoadThumbnail_MemoryUsage()
    {
        var initialMemory = GC.GetTotalMemory(true);
        
        for (int i = 0; i < 100; i++)
        {
            using (var thumbnail = imageLoader.LoadThumbnail(testImage, 256))
            {
                // Process thumbnail
            }
        }
        
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        var finalMemory = GC.GetTotalMemory(false);
        var memoryIncrease = finalMemory - initialMemory;
        
        // Should not leak more than 10MB
        memoryIncrease.Should().BeLessThan(10 * 1024 * 1024);
    }
}
```

#### Load Testing

```csharp
[TestFixture]
public class LoadTests
{
    [Test]
    public async Task ConcurrentImport_MultipleThreads_HandlesCorrectly()
    {
        var importTasks = Enumerable.Range(0, 10)
            .Select(i => Task.Run(() => ImportTestPhoto($"photo{i}.jpg")));
        
        var results = await Task.WhenAll(importTasks);
        
        results.Should().OnlyContain(r => r.Success);
        database.Photos.TotalCount.Should().Be(10);
    }
    
    [Test]
    public void DatabaseOperations_HighConcurrency_MaintainsIntegrity()
    {
        var tasks = new List<Task>();
        
        // Concurrent reads
        for (int i = 0; i < 50; i++)
        {
            tasks.Add(Task.Run(() => database.Photos.GetRandom()));
        }
        
        // Concurrent writes
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(() => database.Photos.Add(CreateTestPhoto())));
        }
        
        Task.WaitAll(tasks.ToArray());
        
        // Database should remain consistent
        database.VerifyIntegrity().Should().BeTrue();
    }
}
```

### 5. UI Testing Strategy

#### GTK# Widget Testing

```csharp
[TestFixture]
public class UIWidgetTests
{
    [SetUp]
    public void SetUp()
    {
        // Initialize GTK for testing
        if (!Gtk.Application.InitCheck())
        {
            Gtk.Application.Init();
        }
    }
    
    [Test]
    public void PhotoImageView_SetPhoto_DisplaysCorrectly()
    {
        var photoView = new PhotoImageView();
        var testPhoto = CreateTestPhoto();
        
        photoView.Photo = testPhoto;
        
        photoView.Photo.Should().Be(testPhoto);
        photoView.Pixbuf.Should().NotBeNull();
    }
    
    [Test]
    public void TagEditor_AddTag_UpdatesDisplay()
    {
        var tagEditor = new TagEditor();
        var tag = CreateTestTag("test-tag");
        
        tagEditor.AddTag(tag);
        
        tagEditor.Tags.Should().Contain(tag);
        tagEditor.GetDisplayedTags().Should().Contain(tag.Name);
    }
}
```

### 6. Plugin System Testing

#### Plugin Loading Testing

```csharp
[TestFixture]
public class PluginTests
{
    [Test]
    public void PluginManager_LoadEditors_LoadsAllValidPlugins()
    {
        var pluginManager = new PluginManager();
        
        pluginManager.LoadPlugins();
        
        var editors = pluginManager.GetEditors();
        editors.Should().HaveCountGreaterThan(0);
        editors.Should().OnlyContain(e => e != null);
    }
    
    [Test]
    public void Plugin_Execute_IsolatedFromMainApplication()
    {
        var maliciousPlugin = CreateMaliciousPlugin();
        var sandbox = new PluginSandbox();
        
        Action act = () => sandbox.Execute(maliciousPlugin);
        
        // Should not allow access to system resources
        act.Should().Throw<SecurityException>();
    }
    
    [Test]
    public void PluginManager_HandleFailedPlugin_ContinuesOperation()
    {
        var pluginManager = new PluginManager();
        var faultyPlugin = CreateFaultyPlugin();
        
        // Should not crash the application
        Action act = () => pluginManager.LoadPlugin(faultyPlugin);
        
        act.Should().NotThrow();
        pluginManager.GetValidPlugins().Should().NotContain(faultyPlugin);
    }
}
```

### 7. Cross-Platform Testing Strategy

#### Platform Compatibility Testing

```csharp
[TestFixture]
public class CrossPlatformTests
{
    [Test]
    [Platform("Linux")]
    public void FileSystem_Linux_HandlesPathsCorrectly()
    {
        var linuxPath = "/home/user/photos/test.jpg";
        var uri = new SafeUri(linuxPath);
        
        uri.LocalPath.Should().Be(linuxPath);
        uri.IsAbsolute.Should().BeTrue();
    }
    
    [Test]
    [Platform("Win")]
    public void FileSystem_Windows_HandlesPathsCorrectly()
    {
        var windowsPath = @"C:\Users\user\Photos\test.jpg";
        var uri = new SafeUri(windowsPath);
        
        uri.LocalPath.Should().Be(windowsPath);
        uri.IsAbsolute.Should().BeTrue();
    }
    
    [Test]
    [Platform("MacOsX")]
    public void FileSystem_MacOS_HandlesPathsCorrectly()
    {
        var macPath = "/Users/user/Pictures/test.jpg";
        var uri = new SafeUri(macPath);
        
        uri.LocalPath.Should().Be(macPath);
        uri.IsAbsolute.Should().BeTrue();
    }
}
```

## Test Data Management

### Test Database Strategy

**Test Database Templates**:
```csharp
public class TestDatabaseFactory
{
    public static IDb CreateInMemoryDatabase()
    {
        var connectionString = "Data Source=:memory:";
        var db = new Db(connectionString);
        db.Init();
        return db;
    }
    
    public static IDb CreateTestDatabase(string templateName)
    {
        var templatePath = $"tests/data/{templateName}.db";
        var testPath = Path.GetTempFileName();
        
        File.Copy(templatePath, testPath, true);
        return new Db(testPath);
    }
    
    public static void PopulateWithTestData(IDb db, int photoCount = 100)
    {
        var photos = GenerateTestPhotos(photoCount);
        var tags = GenerateTestTags(20);
        
        photos.ForEach(p => db.Photos.Add(p));
        tags.ForEach(t => db.Tags.Add(t));
        
        AssignRandomTags(photos, tags);
    }
}
```

### Test Image Generation

```csharp
public class TestImageFactory
{
    public static string CreateTestImage(int width = 800, int height = 600)
    {
        var path = Path.GetTempFileName() + ".jpg";
        using (var bitmap = new Bitmap(width, height))
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.FillRectangle(Brushes.Blue, 0, 0, width, height);
            graphics.DrawString("Test Image", SystemFonts.DefaultFont, 
                               Brushes.White, 10, 10);
            bitmap.Save(path, ImageFormat.Jpeg);
        }
        return path;
    }
    
    public static string CreateCorruptedImage()
    {
        var path = Path.GetTempFileName() + ".jpg";
        File.WriteAllBytes(path, new byte[] { 0xFF, 0xD8, 0xFF, 0x00 }); // Invalid JPEG
        return path;
    }
}
```

## Testing Infrastructure

### Continuous Integration Testing

**Azure Pipelines Test Configuration**:
```yaml
- task: DotNetCoreCLI@2
  displayName: 'Run Unit Tests'
  inputs:
    command: 'test'
    projects: 'tests/**/*.csproj'
    arguments: '--configuration Release --logger trx --collect:"XPlat Code Coverage"'
    
- task: DotNetCoreCLI@2
  displayName: 'Run Security Tests'
  inputs:
    command: 'test'
    projects: 'tests/SecurityTests/*.csproj'
    arguments: '--configuration Release --logger trx'
    
- task: DotNetCoreCLI@2
  displayName: 'Run Performance Tests'
  inputs:
    command: 'test'
    projects: 'tests/PerformanceTests/*.csproj'
    arguments: '--configuration Release --logger trx'
```

### Test Execution Environment

**Docker Test Environment**:
```dockerfile
# Dockerfile.test
FROM mcr.microsoft.com/dotnet/sdk:6.0

# Install GTK dependencies for UI tests
RUN apt-get update && apt-get install -y \
    libgtk-3-dev \
    xvfb \
    && rm -rf /var/lib/apt/lists/*

# Set up display for UI tests
ENV DISPLAY=:99

WORKDIR /app
COPY . .
RUN dotnet restore

# Run tests with virtual display
CMD ["sh", "-c", "Xvfb :99 -screen 0 1024x768x24 & dotnet test"]
```

## Quality Gates and Metrics

### Test Quality Metrics

**Coverage Requirements**:
```yaml
Quality Gates:
  Unit Test Coverage: >= 80%
  Integration Test Coverage: >= 60%
  Security Test Coverage: >= 90% for security-critical components
  Performance Test Coverage: >= 70% for performance-critical paths
  
Branch Coverage: >= 75%
Line Coverage: >= 80%
Mutation Test Score: >= 70%
```

### Performance Testing Baselines

```csharp
public class PerformanceBaselines
{
    public static readonly Dictionary<string, TimeSpan> Baselines = new()
    {
        ["PhotoStore.Query.SmallDataset"] = TimeSpan.FromMilliseconds(100),
        ["PhotoStore.Query.LargeDataset"] = TimeSpan.FromMilliseconds(1000),
        ["ImportController.ImportPhoto"] = TimeSpan.FromMilliseconds(500),
        ["ThumbnailLoader.LoadThumbnail"] = TimeSpan.FromMilliseconds(200),
        ["DatabaseMigration.UpdateSchema"] = TimeSpan.FromMinutes(5)
    };
}
```

## Test Automation and Tooling

### Automated Test Generation

**Property-Based Testing**:
```csharp
[TestFixture]
public class PropertyBasedTests
{
    [Test]
    public void Photo_AddRemoveTag_IsInverse()
    {
        Prop.ForAll<Photo, Tag>((photo, tag) =>
        {
            photo.AddTag(tag);
            photo.RemoveTag(tag);
            
            return !photo.HasTag(tag);
        }).QuickCheckThrowOnFailure();
    }
    
    [Test]
    public void SafeUri_RoundTrip_PreservesValue()
    {
        Prop.ForAll<string>(validPath =>
        {
            if (IsValidPath(validPath))
            {
                var uri = new SafeUri(validPath);
                var roundTrip = new SafeUri(uri.ToString());
                
                return uri.Equals(roundTrip);
            }
            return true;
        }).QuickCheckThrowOnFailure();
    }
}
```

### Test Reporting and Analysis

```csharp
public class TestReporter
{
    public void GenerateTestReport(TestResults results)
    {
        var report = new TestReport
        {
            ExecutionTime = results.ExecutionTime,
            PassedTests = results.PassedTests,
            FailedTests = results.FailedTests,
            CodeCoverage = results.CodeCoverage,
            SecurityTestResults = results.SecurityTests,
            PerformanceMetrics = results.PerformanceMetrics
        };
        
        SaveReportAsHtml(report);
        SaveReportAsJson(report);
        
        if (results.HasRegressions)
        {
            SendAlertNotification(report);
        }
    }
}
```

## Implementation Roadmap

### Phase 1: Foundation (2-4 weeks)
1. **Security Test Implementation**
   - SQL injection prevention tests
   - Path traversal protection tests
   - Authentication security tests

2. **Test Infrastructure Setup**
   - Docker test environment
   - CI/CD test pipeline enhancement
   - Test data management framework

### Phase 2: Core Coverage (4-8 weeks)
1. **Unit Test Enhancement**
   - Increase core logic coverage to 80%
   - Add comprehensive edge case testing
   - Implement property-based testing

2. **Integration Test Development**
   - Database integration testing
   - Import/export system testing
   - Plugin system testing

### Phase 3: Advanced Testing (8-12 weeks)
1. **Performance Testing**
   - Benchmark test implementation
   - Load testing framework
   - Memory leak detection

2. **UI and Cross-Platform Testing**
   - GTK# widget testing
   - Cross-platform compatibility testing
   - Accessibility testing

### Phase 4: Automation and Quality (12-16 weeks)
1. **Test Automation**
   - Automated test generation
   - Continuous quality monitoring
   - Performance regression detection

2. **Quality Integration**
   - Quality gate enforcement
   - Automated reporting
   - Developer feedback loops

## Conclusion

F-Spot's testing strategy transformation requires comprehensive implementation across security, performance, integration, and quality dimensions. The current minimal testing coverage poses significant risks for the application's revival and long-term maintenance.

**Critical Success Factors**:
1. **Security-First Approach**: Immediate implementation of security testing
2. **Incremental Coverage**: Systematic increase in test coverage
3. **Quality Gates**: Enforcement of quality standards in CI/CD
4. **Developer Experience**: Integration with development workflow

**Expected Outcomes**:
- 80%+ test coverage across all critical components
- Zero security vulnerabilities in tested code paths
- Performance regression prevention
- Cross-platform compatibility assurance
- Reduced debugging time and increased development velocity

This testing strategy provides the foundation for F-Spot's successful modernization and long-term sustainability as a reliable, secure photo management application.