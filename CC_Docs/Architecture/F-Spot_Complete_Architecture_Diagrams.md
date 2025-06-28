# F-Spot Complete Architecture Diagrams and Component Analysis

This document provides comprehensive architectural diagrams and detailed analysis of the F-Spot Photo Manager system, based on systematic examination of the entire codebase.

## Table of Contents
1. [Overall System Architecture](#overall-system-architecture)
2. [Domain Model Relationships](#domain-model-relationships)
3. [Database Layer Architecture](#database-layer-architecture)
4. [Service Layer Components](#service-layer-components)
5. [UI Layer Hierarchy](#ui-layer-hierarchy)
6. [Plugin System Architecture](#plugin-system-architecture)
7. [Data Flow Patterns](#data-flow-patterns)
8. [Component Dependencies](#component-dependencies)
9. [Detailed Component Analysis](#detailed-component-analysis)

## Overall System Architecture

### Layered Architecture Overview
```
┌─────────────────────────────────────────────────────────────────┐
│                     PRESENTATION LAYER                          │
├─────────────────────────────────────────────────────────────────┤
│   GTK# UI Framework (Legacy)                                   │
│   ┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐  │
│   │   MainWindow    │ │     Dialogs     │ │    Widgets      │  │
│   │   (God Class)   │ │   (Builder)     │ │   (Custom)      │  │
│   └─────────────────┘ └─────────────────┘ └─────────────────┘  │
├─────────────────────────────────────────────────────────────────┤
│                    PLUGIN SYSTEM LAYER                          │
├─────────────────────────────────────────────────────────────────┤
│   Mono.Addins Framework                                        │
│   ┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐  │
│   │    Editors      │ │   Exporters     │ │     Tools       │  │
│   │  (Extensions)   │ │  (Extensions)   │ │  (Extensions)   │  │
│   └─────────────────┘ └─────────────────┘ └─────────────────┘  │
├─────────────────────────────────────────────────────────────────┤
│                     SERVICE LAYER                               │
├─────────────────────────────────────────────────────────────────┤
│   Business Logic & Cross-Cutting Concerns                      │
│   ┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐  │
│   │     Import      │ │    Imaging      │ │   Thumbnail     │  │
│   │    Service      │ │    Service      │ │    Service      │  │
│   └─────────────────┘ └─────────────────┘ └─────────────────┘  │
│   ┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐  │
│   │   FileSystem    │ │     Query       │ │   Job Scheduler │  │
│   │   Abstraction   │ │    System       │ │   (Banshee)     │  │
│   └─────────────────┘ └─────────────────┘ └─────────────────┘  │
├─────────────────────────────────────────────────────────────────┤
│                     DOMAIN LAYER                                │
├─────────────────────────────────────────────────────────────────┤
│   Core Business Entities & Logic                               │
│   ┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐  │
│   │      Photo      │ │       Tag       │ │      Roll       │  │
│   │   (Aggregate)   │ │  (Hierarchy)    │ │   (Grouping)    │  │
│   └─────────────────┘ └─────────────────┘ └─────────────────┘  │
│   ┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐  │
│   │  PhotoVersion   │ │   Category      │ │  PhotoChanges   │  │
│   │ (Value Object)  │ │  (Composite)    │ │ (Change Track)  │  │
│   └─────────────────┘ └─────────────────┘ └─────────────────┘  │
├─────────────────────────────────────────────────────────────────┤
│                    DATA ACCESS LAYER                            │
├─────────────────────────────────────────────────────────────────┤
│   Repository Pattern & Database Abstraction                    │
│   ┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐  │
│   │   PhotoStore    │ │    TagStore     │ │    RollStore    │  │
│   │ (Repository)    │ │ (Repository)    │ │ (Repository)    │  │
│   └─────────────────┘ └─────────────────┘ └─────────────────┘  │
│   ┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐  │
│   │    DbStore<T>   │ │    Updater      │ │   Connection    │  │
│   │  (Base Class)   │ │  (Migrations)   │ │   Management    │  │
│   └─────────────────┘ └─────────────────┘ └─────────────────┘  │
├─────────────────────────────────────────────────────────────────┤
│                  INFRASTRUCTURE LAYER                           │
├─────────────────────────────────────────────────────────────────┤
│   External Dependencies & Platform Services                    │
│   ┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐  │
│   │     SQLite      │ │     Hyena       │ │     Cairo       │  │
│   │   Database      │ │   Framework     │ │   Graphics      │  │
│   └─────────────────┘ └─────────────────┘ └─────────────────┘  │
│   ┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐  │
│   │   TagLib#       │ │    TinyIoC      │ │    Platform     │  │
│   │   Metadata      │ │      DI         │ │    Services     │  │
│   └─────────────────┘ └─────────────────┘ └─────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
```

## Domain Model Relationships

### Core Entity Relationship Diagram
```
┌─────────────────────────────────────────────────────────────────┐
│                     DOMAIN MODEL                                │
└─────────────────────────────────────────────────────────────────┘

                        DbItem (Abstract)
                            ↑
            ┌───────────────┼───────────────┐
            │               │               │
        ┌───────┐       ┌───────┐       ┌───────┐
        │ Photo │       │  Tag  │       │ Roll  │
        └───────┘       └───────┘       └───────┘
            │               ↑               │
            │          ┌────────┐           │
            │          │Category│           │
            │          └────────┘           │
            │               │               │
    ┌───────────────┐       │       ┌───────────────┐
    │ PhotoVersion  │       │       │ Import Rolls  │
    │ (1:N)         │       │       │ (Temporal     │
    └───────────────┘       │       │  Grouping)    │
            │               │       └───────────────┘
    ┌───────────────┐       │
    │ PhotoChanges  │       │
    │ (Change       │       │
    │  Tracking)    │       │
    └───────────────┘       │
                            │
                    ┌───────────────┐
                    │ Tag Hierarchy │
                    │ (Parent/Child)│
                    └───────────────┘
```

### Entity Relationships Detail
```
Photo Entity (Aggregate Root)
├── Properties:
│   ├── Id (uint) - Primary Key
│   ├── Time (DateTime) - UTC timestamp
│   ├── Description (string) - User description
│   ├── Rating (uint) - 0-5 stars
│   ├── RollId (uint) - Foreign Key to Roll
│   └── DefaultVersionId (uint) - Current version
├── Collections:
│   ├── Versions (1:N) - PhotoVersion collection
│   ├── Tags (N:N) - Tag associations
│   └── Changes - Change tracking object
├── Business Rules:
│   ├── Rating must be 0-5
│   ├── Must have at least one version
│   ├── Default version must exist
│   └── Original version cannot be deleted
└── Methods:
    ├── AddVersion(), DeleteVersion()
    ├── AddTag(), RemoveTag()
    └── CopyAttributesFrom()

Tag Entity (Hierarchical)
├── Properties:
│   ├── Id (uint) - Primary Key
│   ├── Name (string) - Display name
│   ├── Popularity (int) - Usage tracking
│   ├── SortPriority (int) - Custom ordering
│   ├── ThemeIconName (string) - Icon reference
│   └── CategoryId (uint) - Parent reference
├── Hierarchy:
│   ├── Parent (Category) - Parent tag
│   └── Children (List<Tag>) - Child tags
├── Business Rules:
│   ├── Names must be unique
│   ├── Circular references prevented
│   └── Icon disposal on cleanup
└── Methods:
    ├── IsAncestorOf(), IsDescendentOf()
    └── Recursive traversal methods

PhotoVersion (Value Object)
├── Properties:
│   ├── VersionId (uint) - Version identifier
│   ├── Name (string) - Version name
│   ├── BaseUri (SafeUri) - Directory path
│   ├── Filename (string) - File name
│   ├── ImportMD5 (string) - Integrity hash
│   └── IsProtected (bool) - Delete protection
├── Computed Properties:
│   └── Uri (SafeUri) - BaseUri + Filename
├── Business Rules:
│   ├── Version IDs are unique within Photo
│   ├── Names must be unique within Photo
│   └── Protected versions resist deletion
└── Immutability:
    └── VersionId and Photo reference immutable
```

## Database Layer Architecture

### Repository Pattern Implementation
```
┌─────────────────────────────────────────────────────────────────┐
│                   DATABASE LAYER ARCHITECTURE                   │
└─────────────────────────────────────────────────────────────────┘

Application Layer
       │
       ▼
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   PhotoStore    │    │    TagStore     │    │   RollStore     │
│                 │    │                 │    │                 │
│ - WeakRef Cache │    │ - Immortal      │    │ - Simple CRUD   │
│ - Lazy Loading  │    │   Cache         │    │ - Time-based    │
│ - Query Builder │    │ - Hierarchy     │    │   Grouping      │
│                 │    │   Support       │    │                 │
└─────────────────┘    └─────────────────┘    └─────────────────┘
       │                        │                        │
       └────────────────────────┼────────────────────────┘
                                │
                                ▼
                    ┌─────────────────┐
                    │   DbStore<T>    │
                    │   (Base Class)  │
                    │                 │
                    │ - Generic CRUD  │
                    │ - Event System  │
                    │ - Cache Mgmt    │
                    │ - Thread Safety │
                    └─────────────────┘
                                │
                                ▼
                ┌─────────────────────────────────┐
                │      Connection Layer           │
                ├─────────────────────────────────┤
                │ FSpotDatabaseConnection         │
                │ ├─ HyenaSqliteConnection        │
                │ ├─ Async Queue Processing       │
                │ ├─ Transaction Management       │
                │ └─ Thread Safety                │
                └─────────────────────────────────┘
                                │
                                ▼
                ┌─────────────────────────────────┐
                │        SQLite Database          │
                ├─────────────────────────────────┤
                │ Tables:                         │
                │ ├─ photos                       │
                │ ├─ tags                         │
                │ ├─ photo_tags (M:N)             │
                │ ├─ photo_versions               │
                │ ├─ rolls                        │
                │ ├─ jobs                         │
                │ └─ meta (system config)         │
                │                                 │
                │ Indexes:                        │
                │ ├─ idx_photo_versions_id        │
                │ ├─ idx_photos_roll_id           │
                │ └─ Performance optimizations    │
                └─────────────────────────────────┘
```

### Database Migration System
```
Version Evolution Timeline
└─ Version 1.0  - Base schema
   └─ Version 5.0  - Added roll_id, renamed imports to rolls
      └─ Version 7.0  - URI-based file paths
         └─ Version 8.0  - Full version URI storage
            └─ Version 10.0 - Auto-increment primary keys
               └─ Version 11.0 - Photo rating system
                  └─ Version 16.0 - MD5 hash and job system
                     └─ Version 17.0 - Split URI into base + filename
                        └─ Version 18.0 - Import MD5 tracking

Migration Framework (Updater.cs)
├── Version Detection
├── Progressive Updates (chained migrations)
├── Transaction Safety (rollback on failure)
├── Data Preservation (temp table patterns)
├── Index Management
└── Schema Validation
```

## Service Layer Components

### Service Architecture Overview
```
┌─────────────────────────────────────────────────────────────────┐
│                      SERVICE LAYER                              │
└─────────────────────────────────────────────────────────────────┘

TinyIoC Container (Dependency Injection)
├── Service Registration by Module Controllers
├── Singleton/Multi-instance Lifecycle Management
├── Interface-based Service Discovery
└── Constructor Injection Patterns

┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐
│ Import Services │  │ Imaging Services│  │Thumbnail Service│
├─────────────────┤  ├─────────────────┤  ├─────────────────┤
│ImportController │  │ImageFileFactory │  │ThumbnailService │
│MetadataImporter │  │Format Handlers: │  │├─ XDG Compliant │
│FileImportSource │  │├─ BaseImageFile │  │├─ Cache Mgmt    │
│PhotoFileTracker │  │├─ DCRawImageFile│  │├─ Hash-based    │
│MultiFileImport  │  │├─ JpegImageFile │  │└─ Size Variants │
│├─ RAW+JPEG Pair │  │├─ NefImageFile  │  │                 │
│├─ Duplicate Det │  │└─ CiffImageFile │  │XdgDirectoryServ │
│├─ Transaction   │  │                 │  │├─ Standard Dirs │
│├─ Progress Rpt  │  │Processing:      │  │├─ Environment   │
│└─ Error Rollback│  │├─ EXIF/XMP      │  ││   Variables    │
└─────────────────┘  │├─ Color Profiles│  │└─ Fallback Logic│
                     │├─ Orientation   │  └─────────────────┘
┌─────────────────┐  │└─ Stream Loading│  ┌─────────────────┐
│FileSystem Abstr │  └─────────────────┘  │ Query Services  │
├─────────────────┤                      ├─────────────────┤
│IFileSystem      │  ┌─────────────────┐  │IQueryCondition  │
│├─ DotNetFS Impl │  │Job Scheduler    │  │├─ SQL Generation│
│├─ Cross-platform│  │(Banshee.Kernel) │  │├─ Composable    │
│└─ URI Handling  │  ├─────────────────┤  │└─ Type Safe     │
│                 │  │Scheduler        │  │                 │
│IDirectory       │  │├─ Priority Queue│  │Implementations: │
│├─ Enumeration   │  │├─ Background    │  │├─ DateRange     │
│├─ Recursive     │  ││   Thread       │  │├─ TagTerm       │
│└─ Sorted        │  │├─ Job Lifecycle │  │├─ RatingRange   │
│                 │  │└─ Instance Crit │  │├─ TextTerm      │
│RecursiveFileEnum│  │                 │  │└─ LogicalTerm   │
│SortedFileEnum   │  │Job Types:       │  │                 │
│├─ Error Handling│  │├─ IJob          │  │Query Builder:   │
│├─ Symlink Policy│  │├─ IInstanceCrit │  │├─ WHERE Clauses │
│└─ Performance  │  │└─ Background    │  │├─ ORDER BY      │
└─────────────────┘  └─────────────────┘  │└─ Performance   │
                                          └─────────────────┘
```

## UI Layer Hierarchy

### User Interface Component Structure
```
┌─────────────────────────────────────────────────────────────────┐
│                       UI LAYER HIERARCHY                        │
└─────────────────────────────────────────────────────────────────┘

MainWindow (God Class - 3000+ lines)
├── Core UI Management
├── Mode Switching (IconView ↔ PhotoView)
├── Event Coordination
├── Database Integration
└── Component Lifecycle

┌─────────────────────────────────────────────────────────────────┐
│                    MAIN WINDOW STRUCTURE                        │
├─────────────────────────────────────────────────────────────────┤
│ MenuBar (UIManager)                                             │
│ ├─ Photo Menu (Import, Export, Print, Email)                   │
│ ├─ Edit Menu (Selection, Rotation, Metadata, Preferences)      │
│ ├─ View Menu (Slideshow, Fullscreen, Zoom, Layout)             │
│ ├─ Find Menu (Tags, Rating, Date, Import Roll Filters)         │
│ ├─ Tags Menu (Create, Edit, Delete, Attach/Remove)             │
│ └─ Tools Menu (Plugin-provided extensions)                     │
├─────────────────────────────────────────────────────────────────┤
│ Toolbar (Dynamic Construction)                                  │
│ ├─ Import Button                                               │
│ ├─ Mode Toggle (Browse ↔ Edit)                                 │
│ ├─ Navigation Controls                                          │
│ └─ Action Buttons                                               │
├─────────────────────────────────────────────────────────────────┤
│ Main Content Area (HPaned)                                     │
│ ├─ Left Panel (info_vbox)                                      │
│ │  ├─ Sidebar (Notebook)                                       │
│ │  │  ├─ Tag Selection Widget                                  │
│ │  │  ├─ Folder Tree Page                                      │
│ │  │  ├─ Editor Page (Plugin Extensions)                      │
│ │  │  └─ Plugin-provided Pages                                │
│ │  └─ InfoBox (Metadata Display)                              │
│ │     ├─ EXIF Information                                      │
│ │     ├─ File Properties                                       │
│ │     └─ Histogram Display                                     │
│ └─ Right Panel (view_vbox)                                     │
│    ├─ GroupSelector (Timeline/Date Navigation)                 │
│    ├─ FindBar (Search Interface)                              │
│    ├─ QueryWidget (Logical Search Builder)                    │
│    └─ View Notebook (Main Content)                            │
│       ├─ Tab 0: IconView (QueryView)                          │
│       │  ├─ Grid-based Photo Browser                          │
│       │  ├─ Virtualized Scrolling                             │
│       │  ├─ Thumbnail Display                                 │
│       │  ├─ Multi-selection Support                           │
│       │  └─ Context Menu Integration                          │
│       └─ Tab 1: PhotoView (PhotoImageView)                    │
│          ├─ Single Photo Display                              │
│          ├─ Zoom/Pan Controls                                 │
│          ├─ Navigation (Prev/Next)                            │
│          ├─ Editor Integration                                │
│          └─ Metadata Overlay                                  │
├─────────────────────────────────────────────────────────────────┤
│ StatusBar                                                       │
│ ├─ Status Text                                                 │
│ ├─ Progress Bar                                                │
│ ├─ Zoom Controls                                               │
│ └─ Selection Information                                        │
└─────────────────────────────────────────────────────────────────┘
```

### Custom Widget Hierarchy
```
FSpot.Widgets Namespace
├── Base Widgets
│   ├── ImageView (Container)
│   │   ├─ Zoom/Pan/Selection Support
│   │   ├─ Cairo-based Drawing
│   │   ├─ Multiple PointerModes
│   │   └─ Transparency Handling
│   └── CellGridView (Layout)
│       ├─ Virtual Scrolling
│       ├─ Cell-based Organization
│       └─ Selection Management
├── Specialized Views
│   ├── PhotoImageView : ImageView
│   │   ├─ BrowsablePointer Integration
│   │   ├─ Async Image Loading
│   │   ├─ Loupe Tool Support
│   │   └─ Editor Integration
│   ├── QueryView : SelectionCollectionGridView
│   │   ├─ Photo Grid Display
│   │   ├─ Context Menu Integration
│   │   ├─ Zoom Controls
│   │   └─ PhotoPopup Support
│   └── Filmstrip
│       ├─ Horizontal Photo Strip
│       ├─ Navigation Support
│       └─ Sync with Main View
├── UI Components
│   ├── Sidebar : Notebook
│   │   ├─ Plugin Extensibility
│   │   ├─ Context Switching
│   │   └─ State Management
│   ├── TagSelectionWidget
│   │   ├─ Hierarchical Tag Display
│   │   ├─ Search Functionality
│   │   └─ Selection Management
│   └── InfoBox
│       ├─ EXIF Data Display
│       ├─ File Information
│       └─ Histogram Rendering
└── Specialized Controls
    ├── FindBar (Search Interface)
    ├── GroupSelector (Timeline)
    ├── QueryWidget (Logical Search)
    └── SlideShow (Fullscreen)
```

## Plugin System Architecture

### Extension Framework Overview
```
┌─────────────────────────────────────────────────────────────────┐
│                    PLUGIN SYSTEM ARCHITECTURE                   │
└─────────────────────────────────────────────────────────────────┘

Mono.Addins Framework
├── AddinManager (Discovery & Loading)
├── Extension Points Registry
├── Plugin Dependency Resolution
├── Dynamic Loading/Unloading
└── Conditional Activation System

Extension Points Hierarchy
├── /FSpot/Editors
│   ├─ EditorNode → Editor Classes
│   ├─ Built-in Editors (8): Crop, RedEye, Desaturate, etc.
│   ├─ Plugin Editors (5): BW, Blackout, Flip, Pixelate, Resize
│   ├─ Configuration Widget Support
│   ├─ Preview Generation
│   └─ Batch Processing
├── /FSpot/Menus
│   ├─ MenuNode Hierarchy
│   ├─ ExportMenuItemNode → IExporter
│   ├─ CommandMenuItemNode → Commands
│   ├─ ComplexMenuItemNode → Custom Widgets
│   └─ Conditional Visibility
├── /FSpot/Sidebar
│   ├─ SidebarPageNode → SidebarPage
│   ├─ ViewMode Conditions (Library/Single)
│   ├─ Built-in Pages: Tags, Folders, Editor
│   └─ Plugin Extension Points
├── /FSpot/Services
│   ├─ ServiceNode → IService
│   ├─ Background Service Management
│   ├─ Lifecycle Management (Start/Stop)
│   └─ Error Isolation
└── /FSpot/SlideShow
    ├─ TransitionNode → SlideShowTransition
    ├─ Visual Effect Plugins
    ├─ Cairo-based Rendering
    └─ Timing Control

Plugin Categories
├── Editors (Image Processing)
│   ├─ Single/Batch Processing
│   ├─ Preview Support
│   ├─ Configuration Widgets
│   └─ Progress Reporting
├── Exporters (External Services)
│   ├─ Flickr/23hq/Zooomr
│   ├─ Facebook/PicasaWeb/SmugMug
│   ├─ CD/Gallery/Folder/Zip
│   ├─ Dialog-based Configuration
│   └─ Background Processing
├── Tools (Database/Utilities)
│   ├─ ChangePhotoPath
│   ├─ DevelopInUFraw
│   ├─ LiveWebGallery
│   ├─ MergeDb/RawPlusJpeg
│   └─ Direct Database Access
└── Transitions (Visual Effects)
    ├─ Cover/Dissolve/Push
    ├─ Cairo-based Animation
    └─ Slideshow Integration

Plugin Integration Patterns
├── UI Integration
│   ├─ Automatic Menu Generation
│   ├─ Context Menu Support
│   ├─ Dialog Management
│   └─ Progress Reporting
├── State Access
│   ├─ Current Photo Selection
│   ├─ Application View Mode
│   ├─ Database Services
│   └─ User Preferences
├── Error Handling
│   ├─ Plugin Isolation
│   ├─ Graceful Degradation
│   ├─ Error Logging
│   └─ Recovery Mechanisms
└── Dynamic Behavior
    ├─ Runtime Loading/Unloading
    ├─ Conditional Activation
    ├─ Extension Change Notifications
    └─ Hot-plug Support
```

## Data Flow Patterns

### Primary Data Flow Chains
```
┌─────────────────────────────────────────────────────────────────┐
│                        DATA FLOW PATTERNS                       │
└─────────────────────────────────────────────────────────────────┘

1. Photo Import Flow
   User Selection → FileImportSource → RecursiveFileEnumerator
        │
        ▼
   File Discovery → ImageFileFactory → Format-specific Handler
        │
        ▼
   Metadata Extraction → MetadataImporter → Database Transaction
        │
        ▼
   Photo Creation → ThumbnailGeneration → UI Notification

2. Photo Browsing Flow
   UI Navigation → BrowsablePointer → Query Conditions
        │
        ▼
   SQL Generation → PhotoStore → Database Query
        │
        ▼
   Result Caching → Event Notification → UI Update

3. Photo Editing Flow
   User Action → Editor Selection → Configuration Widget
        │
        ▼
   Parameter Setup → Image Processing → Preview Generation
        │
        ▼
   User Confirmation → Version Creation → File Operations
        │
        ▼
   Database Update → Thumbnail Regeneration → UI Refresh

4. Tag Management Flow
   User Input → TagStore → Hierarchy Validation
        │
        ▼
   Database Update → Cache Invalidation → Event Notification
        │
        ▼
   UI Refresh → Query Re-evaluation → View Update

5. Plugin Execution Flow
   User Trigger → Extension Point Lookup → Plugin Loading
        │
        ▼
   Parameter Gathering → Plugin Execution → Progress Reporting
        │
        ▼
   Result Processing → Database Update → UI Notification

6. Search/Filter Flow
   User Input → Query Widget → IQueryCondition Creation
        │
        ▼
   Condition Composition → SQL Generation → Database Query
        │
        ▼
   Result Collection → IBrowsableCollection → UI Display

Event Propagation Patterns
├── Database Changes
│   Database → Store Events → UI Components → View Updates
├── Selection Changes
│   UI Interaction → Selection Events → Info Updates → Menu States
├── Plugin Events
│   Plugin Actions → Extension Events → UI Integration → State Sync
└── Application State
    Mode Changes → Component Visibility → Menu Updates → Toolbar States
```

## Component Dependencies

### Comprehensive Dependency Graph
```
┌─────────────────────────────────────────────────────────────────┐
│                   COMPONENT DEPENDENCY GRAPH                    │
└─────────────────────────────────────────────────────────────────┘

                         Application Entry Point
                                   │
                                   ▼
                            ┌─────────────┐
                            │     App     │
                            │ (Main Class)│
                            └─────────────┘
                                   │
                    ┌──────────────┼──────────────┐
                    │              │              │
                    ▼              ▼              ▼
            ┌─────────────┐ ┌─────────────┐ ┌─────────────┐
            │  TinyIoC    │ │AddinManager │ │ Database    │
            │ Container   │ │(Mono.Addins)│ │ (FSpotDb)   │
            └─────────────┘ └─────────────┘ └─────────────┘
                    │              │              │
                    │              │        ┌─────┴─────┐
                    │              │        │           │
                    │              │        ▼           ▼
                    │              │  ┌─────────┐ ┌─────────┐
                    │              │  │PhotoStor│ │TagStore │
                    │              │  └─────────┘ └─────────┘
                    │              │        │           │
                    │              │        └─────┬─────┘
                    │              │              │
                    │              │              ▼
                    │              │        ┌─────────┐
                    │              │        │DbStore<T│
                    │              │        └─────────┘
                    │              │              │
                    │              │              ▼
                    │              │     ┌─────────────────┐
                    │              │     │HyenaSqliteConn  │
                    │              │     └─────────────────┘
                    │              │              │
                    │              │              ▼
                    │              │        ┌─────────┐
                    │              │        │ SQLite  │
                    │              │        └─────────┘
                    │              │
                    ▼              ▼
         ┌─────────────────────────────────────────┐
         │           SERVICE LAYER                 │
         │  ┌─────────────┐  ┌─────────────┐      │
         │  │ImportControl│  │ThumbnailServ│      │
         │  └─────────────┘  └─────────────┘      │
         │  ┌─────────────┐  ┌─────────────┐      │
         │  │ImageFileFact│  │FileSystemAbs│      │
         │  └─────────────┘  └─────────────┘      │
         └─────────────────────────────────────────┘
                              │
                              ▼
         ┌─────────────────────────────────────────┐
         │              UI LAYER                   │
         │                                         │
         │        ┌─────────────┐                  │
         │        │ MainWindow  │                  │
         │        │(God Class)  │                  │
         │        └─────────────┘                  │
         │              │                          │
         │      ┌───────┼───────┐                  │
         │      │       │       │                  │
         │      ▼       ▼       ▼                  │
         │ ┌─────────┐ ┌───┐ ┌─────────┐           │
         │ │QueryView│ │...│ │PhotoView│           │
         │ └─────────┘ └───┘ └─────────┘           │
         │      │       │       │                  │
         │      └───────┼───────┘                  │
         │              │                          │
         │              ▼                          │
         │    ┌─────────────────┐                  │
         │    │Custom Widgets   │                  │
         │    │(ImageView, etc.)│                  │
         │    └─────────────────┘                  │
         └─────────────────────────────────────────┘
                              │
                              ▼
         ┌─────────────────────────────────────────┐
         │            PLUGIN LAYER                 │
         │                                         │
         │  ┌─────────┐ ┌─────────┐ ┌─────────┐    │
         │  │ Editors │ │Exporter │ │  Tools  │    │
         │  └─────────┘ └─────────┘ └─────────┘    │
         │              │                          │
         │              ▼                          │
         │        ┌─────────────┐                  │
         │        │ExtensionNode│                  │
         │        └─────────────┘                  │
         └─────────────────────────────────────────┘

Dependency Injection Flow:
1. App initializes TinyIoC container
2. ModuleControllers register services
3. Services resolve dependencies through container
4. UI components receive services via constructor injection
5. Plugins access services through AddinManager integration

Key Dependency Relationships:
├── Database Layer → Infrastructure (SQLite, Hyena)
├── Service Layer → Database Layer + Infrastructure
├── UI Layer → Service Layer + Database Layer
├── Plugin Layer → Service Layer + UI Layer
└── All Layers → Cross-cutting (Logging, Configuration, DI)
```

## Detailed Component Analysis

### Critical Component Details

#### MainWindow Component Analysis
- **Size**: 3000+ lines (God Class anti-pattern)
- **Responsibilities**: Window management, view switching, event coordination, database integration
- **Dependencies**: Every major system component
- **Key Methods**: SetViewMode(), HandleSelectionChanged(), InitializeUI()
- **Refactoring Need**: HIGH - Needs decomposition into focused components

#### Database Components
- **DbStore<T>**: 450 lines - Generic repository with caching and events
- **PhotoStore**: 800+ lines - Complex querying, version management, caching
- **Updater**: 600+ lines - 18+ schema versions, transaction safety
- **Connection Management**: Thread-safe queuing, async processing

#### Service Components
- **ImportController**: 500+ lines - Transactional import with rollback
- **ImageFileFactory**: Format detection, handler registration
- **ThumbnailService**: XDG-compliant caching, multiple sizes

#### UI Components
- **ImageView**: 1000+ lines - Complex Cairo rendering, zoom/pan/selection
- **QueryView**: Grid display with virtualization
- **Sidebar**: Plugin-extensible tabbed interface

#### Plugin Components
- **EditorNode**: Type-safe editor instantiation
- **Extension Points**: 5 major extension categories
- **Dynamic Loading**: Runtime plugin management

### Architecture Quality Assessment

**Strengths:**
- Clear layered architecture with appropriate separation of concerns
- Robust repository pattern with intelligent caching
- Sophisticated plugin system with multiple extension points
- Comprehensive domain model with proper business rules
- Transaction-safe database operations with migration support

**Weaknesses:**
- MainWindow god class violates single responsibility principle
- Heavy coupling between UI and business logic in places
- Synchronous-only operations limit scalability
- Legacy GTK# framework prevents modernization
- Limited unit testing coverage

**Modernization Priorities:**
1. Decompose MainWindow into focused components
2. Implement async/await patterns throughout
3. Migrate to modern UI framework (Avalonia recommended)
4. Add comprehensive unit testing
5. Implement proper MVVM/MVP separation

This architecture demonstrates solid engineering principles that have aged well, with a modernization path that preserves the excellent core design while updating the technical implementation for current standards.