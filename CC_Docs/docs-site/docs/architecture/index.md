# Architecture Overview

F-Spot follows a **layered architecture** with clear separation of concerns, implementing domain-driven design principles and clean architectural patterns.

## System Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Presentation Layer                       │
│         GTK# UI Components & Extension Points              │
├─────────────────────────────────────────────────────────────┤
│                     Service Layer                          │
│     Business Logic, Import/Export, Image Processing        │
├─────────────────────────────────────────────────────────────┤
│                      Data Layer                            │
│        Repository Pattern, SQLite Database                 │
├─────────────────────────────────────────────────────────────┤
│                  Infrastructure Layer                      │
│    File System, External Services, Plugin Framework       │
└─────────────────────────────────────────────────────────────┘
```

## Core Components

### 1. Presentation Layer (GTK# UI)
- **Main Window**: Photo browsing and management interface
- **Dialog System**: Configuration, editing, and import dialogs
- **Custom Widgets**: Specialized controls for photo display and editing
- **Extension Points**: Plugin integration for UI components

### 2. Service Layer (Business Logic)
- **Photo Management**: Core photo operations and workflow
- **Import/Export**: File system integration and web service exports
- **Image Processing**: Editing, filtering, and transformation services
- **Query System**: Advanced search and filtering capabilities

### 3. Data Layer (Repository Pattern)
- **PhotoStore**: Photo metadata and file system mapping
- **TagStore**: Hierarchical tagging system
- **RollStore**: Import roll and batch management
- **Database Migrations**: Version management and schema updates

### 4. Infrastructure Layer
- **File System**: Photo storage and organization
- **Plugin Framework**: Mono.Addins-based extensibility
- **External Services**: Web API integrations (Flickr, Facebook, etc.)
- **Configuration**: Settings and preferences management

## Key Design Patterns

### Repository Pattern
- **Clean Data Access**: Abstraction layer over SQLite database
- **Testable Design**: Mockable interfaces for unit testing
- **Consistent API**: Uniform data access patterns across stores

### Plugin Architecture
- **Extension Points**: Well-defined interfaces for plugins
- **Dynamic Loading**: Runtime plugin discovery and loading
- **Categorized Extensions**: Editors, Exporters, Tools, Transitions

### Domain-Driven Design
- **Rich Domain Model**: Business logic encapsulated in domain entities
- **Value Objects**: Immutable objects for complex data types
- **Aggregate Roots**: Consistency boundaries around related entities

## Module Structure

### Core Modules (`src/Core/`)
- **FSpot**: Main business logic and domain model
- **FSpot.Resources**: Embedded resources and localization
- **FSpot.UnitTest**: Core unit tests and test infrastructure

### Client Applications (`src/Clients/`)
- **FSpot.Gtk**: Primary GTK# desktop application
- **FSpot.Console**: Command-line interface (stub implementation)

### Extension System (`src/Extensions/`)
- **Editors**: Image editing plugins (25+ implementations)
- **Exporters**: Web service and file export plugins
- **Tools**: Utility and maintenance tools
- **Transitions**: Slideshow transition effects

### Support Libraries (`lib/`)
- **Hyena**: UI framework and utilities (from Banshee project)
- **gtk-sharp-beans**: Custom GTK# extensions
- **libfspot**: Native C utilities for screen management

## Data Flow Architecture

```
User Interface → Service Layer → Repository → SQLite Database
     ↓              ↓             ↓              ↓
   GTK# UI    → Business     → Data Access → Photo Metadata
   Dialogs    → Logic        → Layer       → File References
   Extensions → Import/Export → Queries     → Configuration
```

## Component Dependencies

### External Dependencies
- **GTK# 2.12+**: Primary UI framework
- **SQLite 2.8+**: Database backend
- **TagLib#**: Metadata reading/writing
- **Mono.Addins**: Plugin framework
- **Cairo**: Graphics rendering

### Internal Dependencies
- Core modules provide foundation for all other components
- Client applications depend on core business logic
- Extensions use well-defined interfaces from core
- Support libraries provide shared utilities

## Architecture Strengths

### Separation of Concerns
- **Clear Boundaries**: Each layer has distinct responsibilities
- **Loose Coupling**: Minimal dependencies between layers
- **High Cohesion**: Related functionality grouped together

### Extensibility
- **Plugin System**: 25+ extensions demonstrate extensibility
- **Extension Points**: Well-defined interfaces for customization
- **Dynamic Loading**: Runtime plugin discovery and activation

### Maintainability
- **Layered Design**: Easy to understand and modify
- **Repository Pattern**: Data access abstraction
- **Unit Testing**: Testable architecture with dependency injection

## Architecture Weaknesses

### Technology Debt
- **GTK# 2.12**: End-of-life UI framework
- **Synchronous Operations**: Blocking UI thread operations
- **Mixed Patterns**: Inconsistent architectural approaches in some areas

### Performance Concerns
- **Memory Management**: Resource disposal issues
- **Database Queries**: Some inefficient query patterns
- **UI Responsiveness**: Blocking operations affect user experience

## Modernization Opportunities

### UI Framework Migration
- **Target**: Move to Avalonia, MAUI, or WPF
- **Benefits**: Modern UI capabilities, better performance
- **Challenge**: Significant refactoring required

### Async/Await Pattern
- **Current**: Synchronous operations throughout
- **Target**: Modern async patterns for I/O operations
- **Benefits**: Improved responsiveness and scalability

### Dependency Injection
- **Current**: Manual dependency management
- **Target**: Modern DI container (Microsoft.Extensions.DependencyInjection)
- **Benefits**: Better testability and configuration management

## Related Documentation

### Detailed Analysis
- [**Core Architecture Analysis**](core-architecture-analysis) - In-depth architectural examination
- [**Module Structure**](module-structure) - Detailed module breakdown
- [**Dependency Analysis**](dependency-analysis) - Component relationship analysis
- [**Core Business Logic**](/core-architecture/core-business-logic-analysis) - Domain model analysis

### Technical Implementation
- [**Database Layer**](/database/) - Data access and storage architecture
- [**Plugin System**](/plugins/) - Extension framework details
- [**UI Framework**](/ui-framework/) - User interface implementation
- [**Performance Analysis**](/performance/) - Performance characteristics

---

The F-Spot architecture demonstrates solid design principles with clear separation of concerns and extensible design, making it well-suited for modern revival and enhancement efforts.