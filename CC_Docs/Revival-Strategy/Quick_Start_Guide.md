# F-Spot Revival Quick Start Guide

## For Developers Who Want to Start Contributing Now

This guide provides immediate steps to get F-Spot building and start contributing to the revival effort.

## 🚨 Current Reality Check

**F-Spot cannot currently build or run on modern systems.** This guide will help you set up a working development environment and understand the immediate priorities.

## Prerequisites

### Option 1: Legacy Development Environment (Fastest Start)

**Use Ubuntu 18.04 Docker Container** (Recommended for quick testing):
```bash
# Run Ubuntu 18.04 with F-Spot dependencies
docker run -it --rm \
  -v $(pwd):/workspace \
  -e DISPLAY=$DISPLAY \
  -v /tmp/.X11-unix:/tmp/.X11-unix \
  ubuntu:18.04

# Inside container - install dependencies
apt-get update && apt-get install -y \
  mono-complete \
  gtk-sharp2-dev \
  libgtk2.0-dev \
  build-essential \
  git \
  autotools-dev \
  intltool
```

### Option 2: Modern Development Setup (Recommended for Long-term)

**System Requirements:**
- .NET 6+ SDK
- Visual Studio 2022 or JetBrains Rider
- Git with submodules support
- Docker (for legacy testing)

## 🏃‍♂️ Quick Setup (5 Minutes)

### 1. Clone and Initialize Repository
```bash
git clone https://github.com/f-spot/f-spot.git
cd f-spot
git submodule update --init --recursive
```

### 2. Check Current Build Status
```bash
# Try modern build (will likely fail)
dotnet build F-Spot.sln

# Check for GTK# dependencies
ls /usr/lib/mono/gac/gtk-sharp/ 2>/dev/null || echo "GTK# not found"
```

### 3. Set Up Legacy Build Environment
```bash
# Create legacy build container
docker build -t fspot-legacy -f - . <<EOF
FROM ubuntu:18.04
RUN apt-get update && apt-get install -y \\
    mono-complete gtk-sharp2-dev libgtk2.0-dev \\
    build-essential git autotools-dev intltool
WORKDIR /workspace
EOF

# Test legacy build
docker run --rm -v $(pwd):/workspace fspot-legacy bash -c "
  cd /workspace && 
  ./autogen.sh && 
  make
"
```

## 🎯 Immediate Priorities (Choose Your Focus)

### Priority 1: Critical Security Fixes (HIGH IMPACT, 1-2 weeks)

**Why**: Application has critical SQL injection vulnerabilities

**Quick Start**:
```bash
# Find all SQL injection vulnerabilities
grep -r "string.Format.*SELECT\|INSERT\|UPDATE\|DELETE" src/
grep -r "\$.*SELECT\|INSERT\|UPDATE\|DELETE" src/

# Example fix locations:
# src/Core/FSpot/Database/Updater.cs
# src/Core/FSpot/Database/PhotoStore.cs
```

**Example Fix**:
```csharp
// BEFORE (vulnerable)
Execute(string.Format("SELECT COUNT(*) FROM tags WHERE category_id = {0}", id));

// AFTER (secure)
var cmd = new HyenaSqliteCommand("SELECT COUNT(*) FROM tags WHERE category_id = ?", id);
Execute(cmd);
```

### Priority 2: Build System Restoration (MEDIUM IMPACT, 1-2 weeks)

**Why**: Get F-Spot building on modern systems

**Quick Start**:
1. **Fix platform restrictions** in `src/Clients/FSpot.Gtk/FSpot/Main.cs:161`:
   ```csharp
   // Comment out this line:
   // if (Environment.Is64BitProcess) throw new ApplicationException("GtkSharp does not support running 64bit");
   ```

2. **Create modern build configuration**:
   ```xml
   <!-- Update Directory.Build.props -->
   <Project>
     <PropertyGroup>
       <TargetFramework>net472</TargetFramework>
       <LangVersion>latest</LangVersion>
       <Nullable>enable</Nullable>
     </PropertyGroup>
   </Project>
   ```

### Priority 3: Broken Dialog Fixes (LOW IMPACT, 1 week)

**Why**: Restore basic application functionality

**Quick Start**: Fix AdjustTimeDialog in `src/Clients/FSpot.Gtk/FSpot.UI.Dialog/AdjustTimeDialog.cs`:
```csharp
// Replace broken GnomeDateEdit with standard GTK controls
public class AdjustTimeDialog : BuilderDialog {
    private Gtk.Calendar calendar;
    private Gtk.SpinButton hour_spin, minute_spin;
    
    public AdjustTimeDialog() : base("AdjustTimeDialog.ui", "adjust_time_dialog") {
        calendar = new Gtk.Calendar();
        hour_spin = new Gtk.SpinButton(0, 23, 1);
        minute_spin = new Gtk.SpinButton(0, 59, 1);
        // Wire up to UI...
    }
}
```

## 🛠️ Development Workflow

### Testing Your Changes

**Security Fix Testing**:
```csharp
[Test]
public void SqlInjection_Prevention() {
    var maliciousInput = "'; DROP TABLE photos; --";
    Assert.DoesNotThrow(() => photoStore.SearchByDescription(maliciousInput));
    
    // Verify database integrity
    Assert.That(Database.TableExists("photos"), Is.True);
}
```

**Build Testing**:
```bash
# Test in legacy environment
docker run --rm -v $(pwd):/workspace fspot-legacy bash -c "
  cd /workspace && 
  make clean && 
  make && 
  echo 'Build successful!'
"
```

### Code Quality Guidelines

**Immediate Standards**:
- Fix security issues with parameterized queries
- Use `var` for local variables
- Add null checks for safety
- Include TODO comments for future modernization

**Example Pattern**:
```csharp
public Photo[] SearchPhotos(string searchText) {
    // TODO: Modernize to async/await in Phase 2
    if (string.IsNullOrWhiteSpace(searchText)) {
        return Array.Empty<Photo>();
    }
    
    // SECURE: Use parameterized query
    var cmd = new HyenaSqliteCommand(
        "SELECT * FROM photos WHERE description LIKE ?",
        $"%{searchText}%");
    
    return ExecuteQuery(cmd);
}
```

## 🔬 Understanding the Codebase

### Key File Locations

**Critical Files to Understand**:
- `src/Core/FSpot/Core/Photo.cs` - Core photo entity
- `src/Core/FSpot/Database/PhotoStore.cs` - Photo data access
- `src/Clients/FSpot.Gtk/FSpot/MainWindow.cs` - Main UI (3000+ lines!)
- `src/Core/FSpot/Database/Updater.cs` - Database migrations

**Quick Architecture Overview**:
```
┌─────────────────────┐
│   FSpot.Gtk (UI)    │  ← GTK# 2.12 (BROKEN)
├─────────────────────┤
│  FSpot.Core (Logic) │  ← Good architecture
├─────────────────────┤
│  Database (SQLite)  │  ← Solid database layer
└─────────────────────┘
```

### Finding Your Way Around

**Search Patterns for Common Tasks**:
```bash
# Find photo import logic
grep -r "ImportPhoto\|ImportSource" src/

# Find tag management
grep -r "TagStore\|CreateTag" src/

# Find image editing
grep -r "Editor\|Pixbuf" src/

# Find database operations
grep -r "DbStore\|Execute.*SELECT" src/
```

## 📋 Ready-to-Work Issues

### Beginner-Friendly Tasks

1. **Fix Empty Catch Blocks** (185+ instances):
   ```bash
   grep -r "catch.*{.*}" src/ | wc -l
   ```

2. **Add Input Validation**:
   ```csharp
   // Add validation to Photo.Description setter
   public string Description {
       get => description;
       set {
           if (value?.Length > 10000) {
               throw new ArgumentException("Description too long");
           }
           description = value?.Trim() ?? string.Empty;
       }
   }
   ```

3. **Convert to Modern C# Patterns**:
   ```csharp
   // BEFORE
   if (photo.Rating != null) {
       var rating = (int)photo.Rating;
   }
   
   // AFTER  
   if (photo.Rating is int rating) {
       // Use rating...
   }
   ```

### Intermediate Tasks

1. **Implement Async Database Operations**:
   ```csharp
   public async Task<Photo[]> GetPhotosAsync() {
       return await Task.Run(() => GetPhotos());
   }
   ```

2. **Add Comprehensive Unit Tests**:
   ```csharp
   [Test]
   public void PhotoStore_GetPhoto_ReturnsCorrectPhoto() {
       var photo = photoStore.Get(1);
       Assert.That(photo.Id, Is.EqualTo(1));
   }
   ```

3. **Create Modern Configuration System**:
   ```csharp
   public class FSpotConfiguration {
       public string DatabasePath { get; set; }
       public int ThumbnailSize { get; set; }
       public bool ImportCopyFiles { get; set; }
   }
   ```

## 🚀 Contribution Workflow

### 1. Pick an Issue
- Start with security fixes (highest priority)
- Choose build system issues for immediate impact
- Select UI fixes for user-visible improvements

### 2. Create Feature Branch
```bash
git checkout -b fix/sql-injection-vulnerabilities
# or
git checkout -b feature/modern-configuration-system
```

### 3. Make Focused Changes
- **One concern per PR** - don't mix security fixes with feature additions
- **Add tests** for new functionality
- **Update documentation** if needed

### 4. Test Thoroughly
```bash
# Run existing tests
dotnet test

# Test in legacy environment
docker run --rm -v $(pwd):/workspace fspot-legacy bash -c "
  cd /workspace && make && echo 'SUCCESS'
"
```

### 5. Submit Pull Request
- Clear description of changes
- Reference any related issues
- Include testing instructions

## 📚 Essential Reading

### Must-Read Documentation
1. **[Security Analysis](../Security/Security_Analysis.md)** - Critical vulnerabilities
2. **[Build System Issues](../Build-System/Build_System_Critical_Issues.md)** - Why it doesn't build
3. **[Architecture Analysis](../Architecture/Core_Architecture_Analysis.md)** - Understanding the codebase

### Quick Reference
- **CLAUDE.md** - Claude Code guidance
- **HACKING** - Original coding standards
- **README.md** - Project overview (acknowledges current broken state)

## 🆘 Getting Help

### Common Problems

**"GTK# assemblies not found"**:
- Use Ubuntu 18.04 container for immediate development
- Plan migration to modern UI framework (Avalonia recommended)

**"Build fails with MSBuild"**:
- Use legacy autotools build for now: `./autogen.sh && make`
- Modern build system needs complete rework

**"Application crashes on startup"**:
- Remove 64-bit restriction in Main.cs
- Check GTK# installation in container

### Resources
- **Issues**: GitHub issues for F-Spot
- **Documentation**: CC_Docs directory
- **Original Documentation**: HACKING file for coding standards

## 🎯 Success Metrics

### Phase 1 Goals (Next 1-2 Months)
- [ ] All critical security vulnerabilities fixed
- [ ] Application builds and runs in development environment  
- [ ] Basic photo browsing functionality works
- [ ] Core unit tests passing

### Know You're Making Progress When...
- Security scanners show no critical vulnerabilities
- F-Spot launches without immediate crashes
- You can import and view photos
- Database operations work correctly

---

**Ready to contribute? Start with security fixes - they're the highest impact and most critical for the project's future.**