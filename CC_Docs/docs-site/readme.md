# F-Spot Documentation Site

Modern VitePress-powered documentation for the F-Spot Photo Manager revival project.

## Overview

This VitePress site provides comprehensive technical documentation covering all aspects of the F-Spot revival effort, including architecture analysis, modernization strategies, and development guides.

## Features

- **📚 Comprehensive Documentation** - 29+ technical analysis documents
- **🔍 Built-in Search** - Fast local search across all content
- **📱 Responsive Design** - Optimized for desktop, tablet, and mobile
- **🎨 Custom Theme** - Matching the original F-Spot design system
- **⚡ Fast Loading** - Static site generation with modern optimization
- **🌙 Dark Mode** - Automatic theme switching support

## Content Organization

### Architecture & Core Systems
- **System Architecture** - Comprehensive technical overview
- **Database Layer** - SQLite schema and performance analysis
- **Plugin System** - Mono.Addins architecture and migration
- **Core Business Logic** - Domain models and repositories

### User Interface & Experience
- **GTK# Analysis** - Current UI framework assessment
- **Custom Widgets** - Component analysis and modernization
- **UI Modernization Strategy** - Migration to modern frameworks

### Development & Operations
- **Build System Issues** - Critical problems and solutions
- **Development Setup** - Environment configuration guides
- **Testing Infrastructure** - Patterns and best practices
- **Security Analysis** - Assessment and modernization needs

### Specialized Systems
- **Image Processing** - Editing capabilities and algorithms
- **Import/Export System** - Photo workflow management
- **Search & Query** - Advanced search implementation
- **Metadata & EXIF** - Photo metadata handling
- **Color Management** - ICC profiles and color accuracy
- **Internationalization** - Multi-language support (66+ languages)

## Quick Start

### Prerequisites
- Node.js 18+ 
- npm or yarn

### Local Development
```bash
# Navigate to the docs site
cd CC_Docs/docs-site

# Install dependencies
npm install

# Start development server
npm run dev

# Build for production
npm run build

# Preview production build
npm run serve
```

### Updating Content

1. **Edit Markdown Files** - All content is in standard markdown format
2. **Update Navigation** - Modify `.vitepress/config.js` for sidebar/nav changes
3. **Custom Styling** - Edit `.vitepress/theme/custom.css` for design updates
4. **Add New Sections** - Create new directories and update config navigation

## Deployment

### GitHub Pages (Automatic)
The site automatically deploys to GitHub Pages when changes are pushed to the main branch in the `CC_Docs/` directory.

### Manual Deployment
```bash
# Build the site
npm run build

# Deploy the .vitepress/dist directory to your hosting provider
```

## Configuration

### Site Configuration
- **File**: `.vitepress/config.js`
- **Purpose**: Navigation, theme options, plugins
- **Key Settings**: Sidebar structure, search configuration, social links

### Custom Theme
- **File**: `.vitepress/theme/index.js`
- **Styles**: `.vitepress/theme/custom.css`
- **Purpose**: F-Spot brand colors, custom components, responsive design

## Migration from Previous Site

This VitePress site replaces the previous custom HTML/JavaScript documentation site with several advantages:

### Benefits
- **Maintainability** - Standard markdown editing workflow
- **Performance** - Static site generation with CDN optimization
- **Search** - Built-in full-text search functionality
- **Mobile** - Responsive design out of the box
- **SEO** - Pre-rendered HTML for search engines
- **Modern Development** - Hot reload, TypeScript support

### Preserved Features
- **Content Structure** - All existing documentation migrated
- **Design System** - Original color scheme and branding maintained
- **Navigation** - Hierarchical organization preserved
- **Search Functionality** - Enhanced with VitePress built-in search

## Contributing

### Content Updates
1. Edit markdown files directly in the appropriate directories
2. Follow existing naming conventions (lowercase with hyphens)
3. Update navigation in config.js if adding new top-level sections
4. Test locally before submitting changes

### Technical Improvements
1. **Performance** - Optimize loading and rendering
2. **Accessibility** - Enhance keyboard navigation and screen readers
3. **Features** - Add custom Vue components for interactive elements
4. **SEO** - Improve meta tags and structured data

## File Structure

```
docs-site/
├── docs/                           # Documentation content
│   ├── .vitepress/                # VitePress configuration
│   │   ├── config.js              # Main configuration
│   │   └── theme/                 # Custom theme
│   │       ├── index.js           # Theme entry point
│   │       └── custom.css         # Custom styles
│   ├── index.md                   # Home page
│   ├── architecture/              # Architecture documentation
│   ├── database/                  # Database analysis
│   ├── plugins/                   # Plugin system docs
│   ├── ui-framework/              # UI framework analysis
│   ├── performance/               # Performance documentation
│   ├── security/                  # Security analysis
│   ├── revival-strategy/          # Revival roadmap
│   └── [other-categories]/        # Additional documentation
├── package.json                   # Node.js dependencies
└── README.md                      # This file
```

## Browser Support

- **Chrome/Edge**: 88+
- **Firefox**: 85+
- **Safari**: 14+
- **Mobile**: Modern mobile browsers

## Performance

- **Build Time**: ~30 seconds for full site
- **Load Time**: ~500ms initial page load
- **Search**: Sub-second result display
- **Bundle Size**: ~200KB gzipped

---

*This documentation site provides a modern, maintainable platform for the F-Spot revival project's comprehensive technical analysis and development guides.*