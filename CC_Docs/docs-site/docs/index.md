---
layout: home

hero:
  name: "F-Spot Photo Manager"
  text: "Personal Photo Management for GNOME"
  tagline: "Revival Documentation & Technical Analysis"
  image:
    src: data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' fill='%232563eb'%3E%3Cpath d='M21 19V5c0-1.1-.9-2-2-2H5c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2zM8.5 13.5l2.5 3.01L14.5 12l4.5 6H5l3.5-4.5z'/%3E%3C/svg%3E
    alt: F-Spot Logo
  actions:
    - theme: brand
      text: Revival Roadmap
      link: /revival-strategy/
    - theme: alt
      text: Architecture Overview
      link: /architecture/core-architecture-analysis

features:
  - icon: 🖼️
    title: Photo Management
    details: Organize, tag, and rate your photo collection with powerful search and filtering capabilities built on SQLite database with hierarchical tags and 5-star rating system.

  - icon: 🎨
    title: Built-in Editing
    details: Crop, rotate, adjust colors, and apply effects with non-destructive versioning. Advanced image processing with ICC color profiles and comprehensive metadata support.

  - icon: 🔌
    title: Extensible Architecture
    details: Mono.Addins-based plugin system with 25+ extensions including editors, exporters, tools, and transitions. Support for Flickr, Facebook, SmugMug, and more.

  - icon: 🚀
    title: Revival in Progress
    details: Active modernization effort targeting .NET 8, modern UI frameworks, and enhanced performance. Comprehensive technical analysis and migration strategy included.

  - icon: 🏗️
    title: Modern Architecture
    details: Clean layered architecture with domain-driven design, repository patterns, and separation of concerns. Extensive documentation of current state and future plans.

  - icon: 🔍
    title: Comprehensive Analysis
    details: In-depth technical documentation covering architecture, performance, security, internationalization, and migration strategies across 29+ analysis documents.
---

## Project Status

::: warning Revival in Progress
F-Spot is undergoing active modernization. The application currently has **startup issues** and **broken dialogs**. See our [Revival Roadmap](/revival-strategy/) for detailed modernization plans.
:::

## Quick Navigation

### 🔬 Technical Analysis
- [**System Architecture**](/architecture/core-architecture-analysis) - Comprehensive system overview
- [**Database Layer**](/database/database-layer-analysis) - SQLite schema and repositories  
- [**Plugin System**](/plugins/plugin-system-analysis) - Mono.Addins architecture
- [**Performance Analysis**](/performance/performance-bottlenecks) - Bottlenecks and optimization

### 📚 Core Systems
- [**Import/Export System**](/import-export/import-export-system-analysis) - Photo import and export workflows
- [**Image Processing**](/image-processing/image-processing-and-editing-analysis) - Editing capabilities and algorithms
- [**Search & Query**](/search-query/search-and-query-system-analysis) - Advanced search implementation
- [**UI Framework**](/ui-framework/gtk-sharp-analysis) - GTK# analysis and modernization

### 🛠️ Development
- [**Build System Issues**](/build-system/build-system-critical-issues) - Critical build problems
- [**Development Setup**](/build-system/development-setup-guide) - Getting started guide
- [**Security Analysis**](/security/security-analysis) - Security assessment
- [**Testing Strategy**](/testing/testing-infrastructure-and-patterns-analysis) - Test infrastructure

## Architecture Overview

F-Spot follows a **layered architecture** with clear separation of concerns:

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

## Key Metrics

| Component | Status | Coverage |
|-----------|--------|----------|
| **Core Architecture** | ✅ Analyzed | 29 documents |
| **Database Schema** | ✅ Documented | SQLite + migrations |
| **Plugin System** | ✅ Mapped | 25+ extensions |
| **Performance** | ⚠️ Issues identified | Memory/async concerns |
| **Security** | ⚠️ Assessment complete | Modernization needed |
| **Build System** | ❌ Critical issues | Autotools/MSBuild hybrid |

## Getting Started

1. **Understand the Architecture**: Start with [System Architecture](/architecture/core-architecture-analysis)
2. **Review Current Issues**: Check [Build System Issues](/build-system/build-system-critical-issues) 
3. **Follow Revival Plan**: See [Revival Roadmap](/revival-strategy/) for modernization strategy
4. **Development Setup**: Use [Development Guide](/build-system/development-setup-guide) for environment setup

## Community & Contributing

F-Spot is an open-source project welcoming contributors:

- **GitHub Repository**: [f-spot/f-spot](https://github.com/f-spot/f-spot)
- **Issue Tracking**: GitHub Issues for bugs and feature requests
- **Documentation**: This site contains comprehensive technical analysis
- **Revival Effort**: Active modernization targeting .NET 8 and modern UI frameworks

---

*This documentation provides comprehensive technical analysis of the F-Spot Photo Manager revival project, making complex architectural information accessible for developers, project managers, and stakeholders.*
